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
        public List<string> worldsReached = new List<string>();   // Round 4: worlds the vortex has taken the party to ("Egypt": to be continued)
        public int maxHearts = Health.StartHearts;
        public string currentWorld = WorldData.IceAge;
        public string checkpoint = "";
        public string savedAt = "";
        public List<string> seen = new List<string>();      // Fix 10.1: the cutscenes the player has seen (the Theater)
        public string returnWorld = "";                     // Fix 10.1: Pause > Return to the vortex left this world from returnSpot
        public Vector3 returnSpot; public float returnYaw; public string returnCp = "";

        public static SaveGame Current { get; private set; } = new SaveGame();
        public static string Slot = "";   // Round 4: "testarea" = the graybox playground (a debug option), in its own file
        public static string FilePath => Path.Combine(GameSettings.Folder, Slot == "" ? "save.json" : $"save_{Slot}.json");
        // story shards found in the real worlds (the vortex's way home needs all eight)
        public int StoryShards { get { int n = 0; foreach (var w in worlds) if (w.storyShard && w.world != WorldData.Playground) n++; return n; } }
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
            if (Exists) Current.FillSeen();
        }

        // Fix 10.1, item 5: a save from before the Theater gets the cutscenes its progress shows were seen
        public void FillSeen()
        {
            if (seen == null) seen = new List<string>();
            void S(bool b, string n) { if (b && !seen.Contains(n)) seen.Add(n); }
            WorldProgress P(string w) { foreach (var x in worlds) if (x.world == w) return x; return new WorldProgress { world = w }; }
            var ia = P(WorldData.IceAge); var eg = P(WorldData.Egypt); var be = P(WorldData.Bethlehem); var me = P(WorldData.Medieval); var ps = P(WorldData.Pirate);
            S(true, "ActOne"); S(true, "IceAgeArrival");
            S(Has(Abilities.GroundPound), "ViolinistTeaches"); S(ia.necklaceAwake || ia.storyShard, "NecklaceWakes"); S(ia.storyShard, "VortexOpens");
            S(eg.opened.Contains("EG-arrived"), "EgyptArrival"); S(eg.opened.Contains("EG-sphinx-gag"), "SphinxGag"); S(Has(Abilities.DoubleJump), "ViolinistTeachesEgypt");
            S(eg.necklaceAwake || eg.storyShard, "NecklaceWakesEgypt"); S(eg.storyShard, "VortexOpensEgypt");
            S(be.opened.Contains("BE-arrived"), "BethlehemArrival"); S(Has(Abilities.Pounce), "ViolinistTeachesBethlehem"); S(be.necklaceAwake || be.storyShard, "NecklaceWakesBethlehem"); S(be.storyShard, "WiseMenArrive");
            S(me.opened.Contains("ME-arrived"), "MedievalArrival"); S(Has(Abilities.ChargedPunch), "ViolinistTeachesMedieval"); S(me.necklaceAwake || me.storyShard, "NecklaceWakesMedieval"); S(me.storyShard, "TheStone");
            S(ps.opened.Contains("PS-arrived"), "PirateArrival"); S(Has(Abilities.DeepDive), "ViolinistTeachesPirate"); S(ps.necklaceAwake || ps.storyShard, "NecklaceWakesPirate");
            S(ps.opened.Contains("PS-overboard"), "ShardOverboard"); S(ps.storyShard, "SeaVortex");
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
