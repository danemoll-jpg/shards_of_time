using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 1, item 10: one save file, save.json in the save folder (beside settings.json). Collected shard ids per world, the total,
    // unlocked abilities, opened portals, max hearts, where the player is. Saved at checkpoints, portals and on quit; loaded on start.
    // "New game" deletes it.
    [Serializable]
    public sealed class WorldProgress
    {
        public string world;
        public List<string> shards = new List<string>();
        public bool storyShard;
        public bool necklaceAwake;
        public List<string> opened = new List<string>();   // ice walls and the like, opened for good (Round 2)
    }

    [Serializable]
    public sealed class SaveGame
    {
        public int version = 1;
        public List<WorldProgress> worlds = new List<WorldProgress>();
        public int totalShards;
        public List<string> abilities = new List<string>();
        public List<string> portals = new List<string>();
        public List<string> heartUpgrades = new List<string>();
        public int maxHearts = Health.StartHearts;
        public string currentWorld = "Playground";
        public string checkpoint = "";
        public string savedAt = "";

        public static SaveGame Current { get; private set; } = new SaveGame();
        public static string FilePath => Path.Combine(GameSettings.Folder, "save.json");
        public static bool Exists => File.Exists(FilePath);

        public WorldProgress World(string id)
        {
            foreach (var w in worlds) if (w.world == id) return w;
            var n = new WorldProgress { world = id }; worlds.Add(n); return n;
        }
        public bool Has(string ability) => abilities.Contains(ability);
        public int Total { get { int t = 0; foreach (var w in worlds) t += w.shards.Count; return t; } }

        public static void Load()
        {
            Current = new SaveGame();
            try { if (Exists) Current = JsonUtility.FromJson<SaveGame>(File.ReadAllText(FilePath)) ?? new SaveGame(); }
            catch (Exception e) { Debug.LogWarning("[Save] save.json unreadable, starting fresh: " + e.Message); Current = new SaveGame(); }
            Current.maxHearts = Mathf.Clamp(Current.maxHearts, Health.StartHearts, Health.MaxHeartsCap);
        }

        public static void NewGame()
        {
            try { if (Exists) File.Delete(FilePath); } catch (Exception e) { Debug.LogWarning("[Save] could not delete save.json: " + e.Message); }
            Current = new SaveGame();
            PlayLog.Line("new game: save cleared");
        }

        public static bool Disabled;   // the editor's film render plays without touching the save

        public static void Save(string why)
        {
            if (Disabled) return;
            var s = Current; s.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); s.totalShards = s.Total;
            try
            {
                Directory.CreateDirectory(GameSettings.Folder);
                File.WriteAllText(FilePath + ".tmp", JsonUtility.ToJson(s, true));
                File.Copy(FilePath + ".tmp", FilePath, true); File.Delete(FilePath + ".tmp");
                PlayLog.Line($"saved ({why}): {s.totalShards} shards, abilities [{string.Join(",", s.abilities)}], portals [{string.Join(",", s.portals)}], max hearts {s.maxHearts}, checkpoint {s.checkpoint}");
            }
            catch (Exception e) { Debug.LogWarning("[Save] not saved: " + e.Message); }
        }
    }
}
