using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 1, item 11 (rule 8): Shards of Time > Render Cutscene to Video. Pick a cutscene (any prefab in Resources/Cutscenes).
    // Round 5: the video is made from the built game, never by the editor's Play mode (that run played sound on Dan's speakers and
    // took his focus): the menu runs Tools/render_cutscene.py, which starts the exe in batch mode, muted, with no window, plays the
    // cutscene at a fixed 30 fps, saves each frame off-screen and encodes Recordings/<Name>.mp4 (1920 x 1080, 30 fps, H.264, no
    // audio) with ffmpeg, all hidden (Tools/quiet.py). It uses the last build: build first if the cutscene changed.
    // Without the editor:  python Tools/render_cutscene.py <Name> [<Name> ...]. See Docs/CUTSCENES.md.
    public sealed class CutsceneRender : EditorWindow
    {
        public const int Width = 1920, Height = 1080, Fps = 30;
        Vector2 scroll;

        [MenuItem("Shards of Time/Render Cutscene to Video")]
        static void Open() => GetWindow<CutsceneRender>(true, "Render Cutscene to Video").minSize = new Vector2(380, 240);

        void OnGUI()
        {
            EditorGUILayout.HelpBox($"Renders the cutscene from the built game (no window, muted) to Recordings/<Name>.mp4, {Width} x {Height}, {Fps} fps. " +
                                    "Uses the last build: build first if the cutscene changed.", MessageType.Info);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var name in Names())
                if (GUILayout.Button("Render " + name, GUILayout.Height(28))) { Close(); Render(name); GUIUtility.ExitGUI(); }
            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("Open the Recordings folder")) { Directory.CreateDirectory(OutDir); EditorUtility.RevealInFinder(OutDir); }
        }

        public static string[] Names() => Directory.Exists(CutsceneAuthoring.PrefabDir)
            ? Directory.GetFiles(CutsceneAuthoring.PrefabDir, "*.prefab").Select(Path.GetFileNameWithoutExtension).OrderBy(n => n).ToArray() : new string[0];
        public static string OutDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Recordings"));
        static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        // runs Tools/render_cutscene.py hidden; reports when it is done
        public static void Render(string name)
        {
            var psi = new ProcessStartInfo("python", $"Tools/render_cutscene.py {name}")
            { WorkingDirectory = Root, CreateNoWindow = true, UseShellExecute = false, WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardOutput = true, RedirectStandardError = true };
            var p = Process.Start(psi);
            try { p.PriorityClass = ProcessPriorityClass.BelowNormal; } catch { }
            UnityEngine.Debug.Log($"[Render] {name}: rendering from the built game (no window, muted)...");
            p.EnableRaisingEvents = true;
            p.Exited += (_, __) =>
            {
                string text = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                EditorApplication.delayCall += () => UnityEngine.Debug.Log($"[Render] {name}: {(p.ExitCode == 0 ? "done" : "FAILED")}\n{text}");
            };
        }

        // batch use from the editor command line: the same script
        public static void RenderFromCommandLine()
        {
            var args = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(args, "-cutscene");
            string name = i >= 0 && i + 1 < args.Length ? args[i + 1] : "NecklaceWakes";
            var psi = new ProcessStartInfo("python", $"Tools/render_cutscene.py {name}") { WorkingDirectory = Root, CreateNoWindow = true, UseShellExecute = false, WindowStyle = ProcessWindowStyle.Hidden };
            var p = Process.Start(psi); p.WaitForExit();
            EditorApplication.Exit(p.ExitCode);
        }
    }
}
