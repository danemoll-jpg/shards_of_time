using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 1, item 11 (rule 8): Shards of Time > Render Cutscene to Video. Pick a cutscene (any prefab in Resources/Cutscenes); the editor
    // enters Play mode in the game's scene, the game plays that cutscene in the playground (Game.Film: no title, no save, no skipping),
    // and Unity Recorder writes Recordings/<Name>.mp4 (1920 x 1080, 30 fps, H.264, no audio) from the main camera; Play mode ends by
    // itself. Batch use (no window):
    //   Unity.exe -batchmode -projectPath . -executeMethod ShardsOfTime.CutsceneRender.RenderFromCommandLine -cutscene NecklaceWakes -logFile <file>
    // (not -nographics: the recorder needs the GPU). See Docs/CUTSCENES.md.
    public sealed class CutsceneRender : EditorWindow
    {
        const string Key = "ShardsOfTime.RenderCutscene", BatchKey = "ShardsOfTime.RenderBatch";
        public const int Width = 1920, Height = 1080, Fps = 30;
        static RecorderController controller;
        Vector2 scroll;

        [MenuItem("Shards of Time/Render Cutscene to Video")]
        static void Open() => GetWindow<CutsceneRender>(true, "Render Cutscene to Video").minSize = new Vector2(380, 240);

        void OnGUI()
        {
            EditorGUILayout.HelpBox($"Plays the cutscene in the game and records {Width} x {Height} MP4 at {Fps} fps to Recordings/<Name>.mp4 (muted).", MessageType.Info);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var name in Names())
                if (GUILayout.Button("Render " + name, GUILayout.Height(28))) { Close(); Render(name, false); GUIUtility.ExitGUI(); }
            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("Open the Recordings folder")) { Directory.CreateDirectory(OutDir); EditorUtility.RevealInFinder(OutDir); }
        }

        public static string[] Names() => Directory.Exists(CutsceneAuthoring.PrefabDir)
            ? Directory.GetFiles(CutsceneAuthoring.PrefabDir, "*.prefab").Select(Path.GetFileNameWithoutExtension).OrderBy(n => n).ToArray() : new string[0];
        public static string OutDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Recordings"));

        public static void Render(string name, bool batch)
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[Render] leave Play mode first"); return; }
            CutsceneAuthoring.EnsureAll();
            if (!File.Exists(PlayerBuild.ScenePath)) PlayerBuild.MakeScene();
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(PlayerBuild.ScenePath);
            SessionState.SetString(Key, name); SessionState.SetBool(BatchKey, batch);
            Debug.Log($"[Render] {name}: entering Play mode");
            EditorApplication.EnterPlaymode();
        }

        public static void RenderFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, "-cutscene");
            string name = i >= 0 && i + 1 < args.Length ? args[i + 1] : "NecklaceWakes";
            Render(name, true);
        }

        [InitializeOnLoadMethod]
        static void Hook()
        {
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorApplication.playModeStateChanged += OnPlayMode;
            // batch mode: a domain reload when entering Play mode can lose the event; check again on load
            if (EditorApplication.isPlayingOrWillChangePlaymode && !string.IsNullOrEmpty(SessionState.GetString(Key, ""))) EditorApplication.delayCall += () => OnPlayMode(PlayModeStateChange.EnteredPlayMode);
        }

        static bool started;
        static void OnPlayMode(PlayModeStateChange s)
        {
            string name = SessionState.GetString(Key, "");
            if (string.IsNullOrEmpty(name)) return;
            if (s == PlayModeStateChange.EnteredPlayMode && !started) { started = true; EditorApplication.update += WaitForGame; }
            if (s == PlayModeStateChange.EnteredEditMode)
            {
                bool batch = SessionState.GetBool(BatchKey, false);
                SessionState.EraseString(Key); started = false;
                string mp4 = Path.Combine(OutDir, name + ".mp4");
                Debug.Log(File.Exists(mp4) ? $"[Render] wrote {mp4} ({new FileInfo(mp4).Length / 1048576.0:0.0} MB)" : $"[Render] FAILED: no {mp4}");
                if (batch) EditorApplication.Exit(File.Exists(mp4) ? 0 : 1);
            }
        }

        static void WaitForGame()
        {
            if (!Game.I || !Party.I || !CameraRig.I) return;
            EditorApplication.update -= WaitForGame;
            string name = SessionState.GetString(Key, "");
            Game.I.StartCoroutine(Run(name));
        }

        static System.Collections.IEnumerator Run(string name)
        {
            // the game first places everyone; then record exactly the cutscene (from its first frame to its last)
            Cutscenes.Finished -= OnFinished; Cutscenes.Finished += OnFinished;
            Cutscenes.Started -= OnStarted; Cutscenes.Started += OnStarted;
            yield return Game.I.Film(name);
        }

        static void StartRecorder(string name)
        {
            Directory.CreateDirectory(OutDir);
            var cs = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movie.name = name; movie.Enabled = true;
            movie.EncoderSettings = new CoreEncoderSettings { Codec = CoreEncoderSettings.OutputCodec.MP4, EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High };
            movie.ImageInputSettings = new CameraInputSettings { Source = ImageSource.MainCamera, OutputWidth = Width, OutputHeight = Height, CaptureUI = false, FlipFinalOutput = false };
            movie.AudioInputSettings.PreserveAudio = false;
            movie.OutputFile = Path.Combine(OutDir, name).Replace('\\', '/');
            cs.AddRecorderSettings(movie);
            cs.SetRecordModeToManual();
            cs.FrameRatePlayback = FrameRatePlayback.Constant; cs.FrameRate = Fps; cs.CapFrameRate = true;
            controller = new RecorderController(cs);
            controller.PrepareRecording();
            bool ok = controller.StartRecording();
            Debug.Log($"[Render] recording {name}: {(ok ? "started" : "FAILED to start")}");
        }

        static void OnStarted(string name) { Cutscenes.Started -= OnStarted; StartRecorder(name); }

        static void OnFinished(string name)
        {
            Cutscenes.Finished -= OnFinished;
            if (controller != null && controller.IsRecording()) controller.StopRecording();
            controller = null;
            Debug.Log($"[Render] {name} finished");
            EditorApplication.delayCall += () => EditorApplication.ExitPlaymode();
        }
    }
}
