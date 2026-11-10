using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 1, item 2: Hostile Horizon's one-step Windows player build (from Chastain Rush), ported. It makes the game's one scene
    // (Assets/Scenes/Main.unity: a Game object, which builds the rest at start), the missing data / material assets and the graybox
    // cutscenes, and writes Builds/ShardsOfTime/ShardsOfTime.exe (git-ignored) with BUILD_INFO.txt beside it.
    //   Menu:  Shards of Time > Build Executable
    //   Batch: Unity.exe -batchmode -nographics -quit -projectPath . -executeMethod ShardsOfTime.PlayerBuild.BuildFromCommandLine [-buildVersion 0.1.0] -logFile <file>
    //   (or python Tools/unity_batch.py build -executeMethod ShardsOfTime.PlayerBuild.BuildFromCommandLine)
    public static class PlayerBuild
    {
        public const string OutputDir = "Builds/ShardsOfTime";
        public const string ExeName = "ShardsOfTime.exe";
        public const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Shards of Time/Build Executable")]
        public static void BuildMenu() => Build();

        public static void BuildFromCommandLine()
        {
            try { var r = Build(); EditorApplication.Exit(r.summary.result == BuildResult.Succeeded ? 0 : 1); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static void MakeScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Game").AddComponent<Game>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        public static BuildReport Build()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Leave Play mode first.");
            DataAssets.Ensure();
            CharacterImport.MakeMaterials();
            var kiraLock = CharacterImport.KiraLock();
            kiraLock.AddRange(CharacterImport.PeopleReport());
            WorldReport.Write();
            CutsceneAuthoring.EnsureAll();
            MakeScene();
            var args = Environment.GetCommandLineArgs(); int vi = Array.IndexOf(args, "-buildVersion");
            if (vi >= 0 && vi + 1 < args.Length) PlayerSettings.bundleVersion = args[vi + 1];
            PlayerSettings.companyName = "Shards of Time";
            PlayerSettings.productName = "Shards of Time";
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;   // borderless, at the desktop resolution
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.usePlayerLog = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.show = false;
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/HandMade/Title/GameIcon.png");
            if (icon) PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.connectProfiler = false;

            string root = Path.GetDirectoryName(Application.dataPath);
            string dir = Path.Combine(root, OutputDir);
            Directory.CreateDirectory(dir);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(dir, ExeName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            var s = report.summary;
            string persistent = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", PlayerSettings.companyName, PlayerSettings.productName);
            var lines = new[]
            {
                $"Shards of Time player build {PlayerSettings.bundleVersion}, {DateTime.Now:yyyy-MM-dd HH:mm}",
                $"Result {s.result}; errors {s.totalErrors}; warnings {s.totalWarnings}; output {s.totalSize / 1048576.0:0.0} MB; took {s.totalTime.TotalSeconds:0} s",
                $"Exe: {Path.Combine(dir, ExeName)}",
                $"Unity {Application.unityVersion}; Windows x64; development off",
                $"Settings, save.json and Player.log: {persistent}",
                "Self-check: python Tools/run_check.py local/<dir> [--only all|mute]",
            }.Concat(kiraLock).ToArray();
            File.WriteAllLines(Path.Combine(dir, "BUILD_INFO.txt"), lines.Concat(report.steps.Select(st => $"  step {st.name}: {st.duration.TotalSeconds:0.0} s")));
            Debug.Log(string.Join("\n", lines));
            if (s.result != BuildResult.Succeeded) throw new Exception("Build failed: " + s.result);
            return report;
        }
    }
}
