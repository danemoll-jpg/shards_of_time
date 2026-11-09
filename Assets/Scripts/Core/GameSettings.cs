using System;
using System.IO;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 1, item 13 (Hostile Horizon's GameSettings, trimmed): the player's settings, settings.json in the save folder
    // (persistentDataPath, or the launcher's isolated test folder: -racerTestSave <dir>). A check run plays on the defaults and never saves.
    [Serializable]
    public sealed class GameSettings
    {
        public int version = 1;
        public int volume = 80;            // master, percent
        public int effectsVolume = 100;    // percent of master
        public int musicVolume = 70;       // percent of master (Round 4)
        public bool rumble = true;         // the necklace's light pulse on the controller
        public bool invertX, invertY;      // camera
        public int cameraSpeed = 100;      // percent, 50-200
        public int steamDeck;              // 0 Auto, 1 On, 2 Off

        public static GameSettings S { get; private set; } = new GameSettings();

        public static string Folder
        {
            get
            {
                if (CheckRun.Active) return Path.Combine(CheckRun.Dir, "save");
                var test = LauncherBridge.Argument("-racerTestSave");
                return !string.IsNullOrEmpty(test) && Path.IsPathRooted(test) ? test : Application.persistentDataPath;
            }
        }
        static string FilePath => Path.Combine(Folder, "settings.json");

        public static void Load()
        {
            if (CheckRun.Active) { S = new GameSettings(); return; }
            try { if (File.Exists(FilePath)) S = JsonUtility.FromJson<GameSettings>(File.ReadAllText(FilePath)) ?? new GameSettings(); }
            catch (Exception e) { Debug.LogWarning("Settings unreadable, using defaults: " + e.Message); S = new GameSettings(); }
            S.cameraSpeed = Mathf.Clamp(S.cameraSpeed, 50, 200);
        }

        public static void Save()
        {
            if (CheckRun.Active) return;
            try { Directory.CreateDirectory(Folder); File.WriteAllText(FilePath + ".tmp", JsonUtility.ToJson(S, true)); File.Copy(FilePath + ".tmp", FilePath, true); File.Delete(FilePath + ".tmp"); }
            catch (Exception e) { Debug.LogWarning("Settings not saved: " + e.Message); }
        }

        public static void ResetToDefaults() { S = new GameSettings(); ScreenSetting.ResetToDefault(); }
    }
}
