using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 1, item 9 (rules 2 and 3): a world's collectibles as data (Assets/Resources/Data/Worlds/<id>.asset, made by
    // Editor/DataAssets.cs from Defaults() if missing). Every shard lists what it needs (an ability, or a hero's size / strength: "Kira",
    // "Dan"), every area its small-shard requirement and what the party has on its first visit, so RequirementReport can prove the
    // requirement is met without anything the player can't have yet.
    [Serializable]
    public sealed class ShardSpot
    {
        public string id;
        public string area;
        public Vector3 position;
        public string[] requires = new string[0];
        public string note;
    }

    [Serializable]
    public sealed class AreaData
    {
        public string id;
        public string name;
        public int requirement;                    // small shards (in this area) that wake the necklace; 0 = no story shard here
        public string[] firstVisit = new string[0];// heroes and abilities the party has the first time it can get here
        public Vector3 storyShard;
        public Color storyShardColor = new Color(.3f, .6f, 1f);
        public Vector3 spawn; public float spawnYaw;
        [Tooltip("Kira's form here (Resources/Data/KiraForms/<id>); empty = Home")] public string kiraForm = "";
    }

    [CreateAssetMenu(menuName = "Shards of Time/World Data")]
    public sealed class WorldData : ScriptableObject
    {
        public string worldId = "Playground";
        public string displayName = "Graybox Playground";
        public List<AreaData> areas = new List<AreaData>();
        public List<ShardSpot> shards = new List<ShardSpot>();

        public AreaData Area(string id) => areas.FirstOrDefault(a => a.id == id);
        public IEnumerable<ShardSpot> InArea(string id) => shards.Where(s => s.area == id);

        // Rule 2's check: the shards of an area reachable with only what the party has there on its first visit.
        public (int reachable, int requirement, bool ok, string detail) RequirementReport(string areaId)
        {
            var a = Area(areaId); var have = new HashSet<string>(a.firstVisit);
            var inArea = InArea(areaId).ToList();
            var ok = inArea.Where(s => s.requires.All(have.Contains)).ToList();
            var locked = inArea.Except(ok).Select(s => $"{s.id} needs {string.Join("+", s.requires)}");
            return (ok.Count, a.requirement, ok.Count >= a.requirement,
                $"area {a.id} ({a.name}): {ok.Count} of {inArea.Count} shards reachable with [{string.Join(", ", a.firstVisit)}], requirement {a.requirement}" + (locked.Any() ? $"; locked: {string.Join(", ", locked)}" : ""));
        }

        public static WorldData Load(string id) => Resources.Load<WorldData>("Data/Worlds/" + id) ?? Defaults();

        // The graybox playground's layout (positions match World/Playground.cs; 1 unit = 1 m). Area A at the origin, Area B at x = 400.
        public static WorldData Defaults()
        {
            var w = CreateInstance<WorldData>(); w.name = "Playground";
            string[] heroes = { "Dan", "Abi", "Kira" };
            w.areas.Add(new AreaData { id = "A", name = "Area A", requirement = 10, firstVisit = heroes, storyShard = new Vector3(0, 1.4f, 52f), storyShardColor = new Color(.3f, .62f, 1f), spawn = new Vector3(0, .1f, -48f), spawnYaw = 0, kiraForm = KiraForm.Home });
            // Area B: Kira arrives as the sabertooth (Round 2), so her ClawIce is there on the first visit
            w.areas.Add(new AreaData { id = "B", name = "Area B", requirement = 0, firstVisit = new[] { "Dan", "Abi", "Kira", Abilities.ClawIce }, spawn = new Vector3(400, .1f, -12f), spawnYaw = 0, kiraForm = KiraForm.Sabertooth });
            void S(string id, string area, float x, float y, float z, string note, params string[] req) => w.shards.Add(new ShardSpot { id = id, area = area, position = new Vector3(x, y, z), requires = req, note = note });
            // Area A: the start path
            S("A01", "A", 0, 1f, -42f, "start path");
            S("A02", "A", 0, 1f, -36f, "start path");
            S("A03", "A", -6f, 1f, -30f, "by the slopes");
            // the steps
            S("A04", "A", -18f, 2.0f, -30f, "step 1");
            S("A05", "A", -22f, 3.0f, -30f, "step 2");
            S("A06", "A", -26f, 4.0f, -30f, "step 3");
            // the moving platform over the pit
            S("A07", "A", -42f, 1f, -6f, "far side of the pit (moving platform)");
            // the pool (dive)
            S("A08", "A", 30f, -2.8f, 2f, "pool bottom (dive)");
            S("A09", "A", 34f, -2.8f, -4f, "pool bottom (dive)");
            // low gravity tower
            S("A10", "A", 32f, 6.0f, 36f, "low-gravity tower top (Dan or Abi)");
            // Kira's crawl garden
            S("A11", "A", -2f, 1f, 27f, "crawl garden (Kira)", "Kira");
            S("A12", "A", 2f, 1f, 27f, "crawl garden (Kira)", "Kira");
            // Dan's push block ledge
            S("A13", "A", -14f, 3.9f, 44f, "ledge (Dan pushes the block)", "Dan");
            // the enemy yard and the spikes
            S("A14", "A", -30f, 1f, 22f, "enemy yard");
            S("A15", "A", -36f, 1f, 30f, "past the spikes");
            S("A16", "A", 12f, 1f, -10f, "behind the pillar");
            // the double-jump ledge (rule 2: never needed for the requirement)
            S("A17", "A", 16f, 4.2f, -18f, "high ledge", Abilities.DoubleJump);
            S("A18", "A", 19f, 4.2f, -18f, "high ledge", Abilities.DoubleJump);
            S("A19", "A", 22f, 4.2f, -18f, "high ledge", Abilities.DoubleJump);
            // Area B
            S("B01", "B", 400f, 1f, -4f, "path");
            S("B02", "B", 404f, 1f, 4f, "path");
            S("B03", "B", 396f, 2.4f, 10f, "platform");
            S("B04", "B", 400f, 3.4f, 18f, "platform");
            S("B05", "B", 408f, 4.1f, 14f, "high pillar", Abilities.DoubleJump);
            S("B06", "B", 412.5f, 1f, -3f, "behind the ice wall (sabertooth Kira claws it)", "Kira", Abilities.ClawIce);
            return w;
        }
    }
}
