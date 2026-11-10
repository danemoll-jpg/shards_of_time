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

    // Round 5, item 6: a place that opens with a later move. Visible on the first visit; `opens` = the shards / heart upgrade behind it.
    [Serializable]
    public sealed class GateData
    {
        public string id;
        public string kind;                         // "double-jump ledge", "boulder", "cracked rock wall", "deep water", "wall-jump chimney"
        public string needs;                        // the move (Abilities.*)
        public Vector3 position;
        public string[] opens = new string[0];
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
        [Tooltip("Dan's and Abi's outfit here (models <name>_<outfit>: home, iceage); empty = home")] public string outfit = "";
        [Tooltip("Round 4: the space this area covers (the player inside it is in this area)")] public Bounds box;
    }

    [CreateAssetMenu(menuName = "Shards of Time/World Data")]
    public sealed class WorldData : ScriptableObject
    {
        public string worldId = "Playground";
        public string displayName = "Graybox Playground";
        [Tooltip("Round 4: shard y values are the height of the surface the shard stands on (the game puts it 1 m above the ground found there)")]
        public bool heightsAreHints;
        public List<AreaData> areas = new List<AreaData>();
        public List<ShardSpot> shards = new List<ShardSpot>();
        public List<GateData> gates = new List<GateData>();   // Round 5

        public AreaData Area(string id) => areas.FirstOrDefault(a => a.id == id);
        public AreaData AreaAt(Vector3 p) => areas.FirstOrDefault(a => a.box.size != Vector3.zero && a.box.Contains(p));
        public IEnumerable<ShardSpot> InArea(string id) => shards.Where(s => s.area == id);

        // Rule 2's check: the shards of an area reachable with only what the party has there on its first visit.
        public (int reachable, int requirement, bool ok, string detail) RequirementReport(string areaId)
        {
            var a = Area(areaId); var have = new HashSet<string>(a.firstVisit);
            var inArea = InArea(areaId).ToList();
            var ok = inArea.Where(s => s.requires.All(have.Contains)).ToList();
            var locked = inArea.Except(ok).Select(s => $"{s.id} needs {string.Join("+", s.requires)}");
            return (ok.Count, a.requirement, ok.Count >= a.requirement,
                $"area {a.id} ({a.name}): {ok.Count} of {inArea.Count} shards reachable with [{string.Join(", ", a.firstVisit)}], requirement {a.requirement}" + (locked.Any() ? $"; locked: {string.Join(", ", locked)}" : "") +
                (gates.Count > 0 ? $"; gates: {string.Join(", ", gates.Select(g => $"{g.id} ({g.kind}, needs {g.needs}: {string.Join(" ", g.opens)})"))}" : ""));
        }

        public static WorldData Load(string id) => Resources.Load<WorldData>("Data/Worlds/" + id) ?? (id == IceAge ? IceAgeDefaults() : Defaults());
        public const string IceAge = "IceAge", Playground = "Playground";

        // Round 5: 60 shards, 35 wake the necklace; the lake, the glacier and the hot-spring grotto; the gates later moves open (gates).
        // Round 4: the Ice Age, the first real world (World/IceAge.cs builds it; layout numbers in Tools/Blender/iceage_world.py). One
        // area, the valley "V". 40 small shards, 25 wake the necklace (DEFAULT). Shard y = the height of the surface it sits on (a hint:
        // the game raycasts down from 4 m above it and puts the shard 1 m over what it finds). Rule 2: the first visit has the three
        // heroes and sabertooth Kira's claws; no double jump (Abi learns it later), no Muscle Man, and not the ground pound (Dan learns it
        // here from the Violinist, so it isn't counted): 33 of 40 are reachable without them.
        public static WorldData IceAgeDefaults()
        {
            var w = CreateInstance<WorldData>(); w.name = IceAge; w.worldId = IceAge; w.displayName = "The Ice Age"; w.heightsAreHints = true;
            w.areas.Add(new AreaData { id = "V", name = "The Ice Age", requirement = 35, firstVisit = new[] { "Dan", "Abi", "Kira", Abilities.ClawIce },
                storyShard = new Vector3(0, 1.0f, 490f), storyShardColor = new Color(.25f, .55f, 1f), spawn = new Vector3(0, .3f, 12f), spawnYaw = 0,
                kiraForm = KiraForm.Sabertooth, outfit = Outfits.IceAge, box = new Bounds(new Vector3(0, 0, 265), new Vector3(400, 300, 720)) });   // Round 5: requirement 25 -> 35 (60 shards)
            void S(string id, float x, float y, float z, string note, params string[] req) => w.shards.Add(new ShardSpot { id = id, area = "V", position = new Vector3(x, y, z), requires = req, note = note });
            // the landing snowfield (z 0..110): the path, the half-frozen pond
            S("S01", 0, 0, 24, "snowfield path"); S("S02", -6, 0, 36, "snowfield path"); S("S03", 6, 0, 46, "snowfield path");
            S("S04", -14, 0, 62, "by the rocks"); S("S05", -4, 0, 80, "snowfield path"); S("S06", 10, 0, 96, "snowfield path");
            S("S07", 22, -3.2f, 55, "pond bottom (dive)"); S("S08", 27, -2.4f, 60, "pond bottom (dive)"); S("S09", 17, -.25f, 52, "on an ice floe in the pond");
            // the mammoth ridge (z 110..220): ride a mammoth along the shelves
            S("R01", 30, 0, 120, "ridge"); S("R02", 20, 0, 150, "ridge"); S("R03", 28, 0, 190, "ridge");
            S("R04", 51, 5f, 158, "the high shelf (ride a mammoth)"); S("R05", 55, 5f, 180, "the high shelf (ride a mammoth)"); S("R06", 51, 5f, 202, "the high shelf (ride a mammoth)");
            S("R07", 51, 4f, 120, "the loading rock"); S("R08", 8, 3.2f, 206, "high ledge", Abilities.DoubleJump);
            // ice bridges and the frozen river (gorge z 240..264)
            S("V01", 20, 0, 243.5f, "ice pillar 1"); S("V02", 20, 0, 248, "ice pillar 2"); S("V03", 20, 0, 252.5f, "ice pillar 3"); S("V04", 20, 0, 257, "ice pillar 4");
            S("V05", -10, -10, 250, "river bottom under the bridge (dive)"); S("V06", 49, -5, 236, "the ramp out of the gorge");
            // the ice caves (x -62..-22, z 312..368): dark hall, Kira's crawl tunnel, the ice wall, the pit under the cracked ice
            S("C01", -38, 0, 316, "dark hall (the necklace lights it)"); S("C02", -38, 0, 352, "dark hall (the necklace lights it)");
            S("C03", -58, 0, 316, "crawl chamber (Kira)", "Kira"); S("C04", -58, 0, 324, "crawl chamber (Kira)", "Kira");
            S("C05", -31, 0, 362, "behind the ice wall (sabertooth Kira)", "Kira", Abilities.ClawIce);
            S("C06", -33, -4.5f, 338, "the pit under the cracked ice (ground pound)", Abilities.GroundPound);
            S("C07", -26, -4.5f, 347, "the pit under the cracked ice (ground pound)", Abilities.GroundPound);
            // the volcano vents (x 30..64, z 392..452): steam lifts you up three terraces
            S("H01", 47, 8, 436, "terrace 1 (steam)"); S("H02", 58, 16, 437, "terrace 2 (steam)"); S("H03", 60, 24, 414, "terrace 3 (steam)"); S("H04", 60, 24, 426, "terrace 3 (steam)");
            S("H05", 60, 27.3f, 420, "ice shelf over terrace 3", Abilities.DoubleJump);
            // the standing stones (z 460..510)
            S("T01", -12, 0, 476, "standing stones"); S("T02", 12, 0, 500, "standing stones");
            S("T03", -49, 0, 484, "grotto behind the boulder", Abilities.MuscleMan);
            S("T04", 18, -3.5f, 478, "the frozen well (ground pound)", Abilities.GroundPound);
            S("T05", -7, 3.3f, 497, "on the stone lintel", Abilities.DoubleJump);
            // Round 5, item 5: the frozen lake (the west basin, through the pass at z 100..130): skating lanes, holes in the ice, the deep
            // part under the ice (deep dive, later), an ice hollow behind a boulder (Muscle Man, later)
            S("L01", -133, 0, 118, "frozen lake: the skating lane"); S("L02", -133, 0, 150, "frozen lake: the skating lane");
            S("L03", -133, 1.6f, 186, "frozen lake: the ice island at the lane's end"); S("L04", -101, 0, 172, "frozen lake: by the far hole");
            S("L05", -118, -6, 96, "frozen lake: under the ice (dive in at the near hole)");
            S("L06", -118, -28, 128, "frozen lake: the deep part (deep dive)", Abilities.DeepDive);
            S("L07", -112, -28, 138, "frozen lake: the deep part (deep dive)", Abilities.DeepDive);
            S("L08", -154, 0, 124, "frozen lake: the ice hollow behind the boulder", Abilities.MuscleMan);
            // the glacier climb (east, through the gap at z 280..310): ice ledges up to the summit; two wall-jump chimneys; a high ledge
            S("G01", 122, 5.2f, 286, "glacier: ledge 4"); S("G02", 130, 11.7f, 286, "glacier: ledge 9");
            S("G03", 138, 18.2f, 278, "glacier: ledge 14"); S("G04", 148, 24.7f, 306, "glacier: the summit");
            S("G05", 155, 24.7f, 312, "glacier: the summit, the view");
            S("G06", 113.7f, 13, 301, "glacier: top of the near chimney (wall jump)", Abilities.WallJump);
            S("G07", 113.7f, 13, 331, "glacier: top of the far chimney (wall jump)", Abilities.WallJump);
            S("G08", 154, 27.9f, 318, "glacier: the high ledge over the summit (double jump)", Abilities.DoubleJump);
            // the hot-spring grotto (east of the stones, behind a cracked rock wall: Dan's charged punch, later)
            S("P01", 70, 0, 458, "hot-spring grotto", Abilities.ChargedPunch); S("P02", 78, 0, 458, "hot-spring grotto", Abilities.ChargedPunch);
            S("P03", 70, 0, 470, "hot-spring grotto", Abilities.ChargedPunch); S("P04", 80, 0, 472, "hot-spring grotto", Abilities.ChargedPunch);
            // item 6: the places later moves open (visible on the first visit)
            void G(string id, string kind, string needs, Vector3 at, string note, params string[] opens) => w.gates.Add(new GateData { id = id, kind = kind, needs = needs, position = at, note = note, opens = opens });
            G("IA-ledge-ridge", "double-jump ledge", Abilities.DoubleJump, new Vector3(8, 3.2f, 206), "the ridge's high ledge", "R08");
            G("IA-ledge-vents", "double-jump ledge", Abilities.DoubleJump, new Vector3(60, 27.3f, 420), "the ice shelf over the top terrace", "H05");
            G("IA-ledge-lintel", "double-jump ledge", Abilities.DoubleJump, new Vector3(-7, 3.3f, 497), "the stone lintel", "T05");
            G("IA-ledge-glacier", "double-jump ledge", Abilities.DoubleJump, new Vector3(154, 27.9f, 318), "over the glacier's summit (Round 5)", "G08");
            G("IA-boulder-grotto", "boulder", Abilities.MuscleMan, new Vector3(-43.5f, 0, 484), "the grotto by the stones", "T03");
            G("IA-boulder-lake", "boulder", Abilities.MuscleMan, new Vector3(-148, 0, 124), "the ice hollow on the lake's west shore (Round 5)", "L08");
            G("IA-rock-hotspring", "cracked rock wall", Abilities.ChargedPunch, new Vector3(62, 0, 464), "the hot-spring grotto (Round 5)", "P01", "P02", "P03", "P04", "IA-H2");
            G("IA-deep-lake", "deep water", Abilities.DeepDive, new Vector3(-118, -8, 130), "the deep part under the lake (Round 5)", "L06", "L07");
            G("IA-chimney-near", "wall-jump chimney", Abilities.WallJump, new Vector3(113, 0, 301), "the glacier's near chimney (Round 5)", "G06");
            G("IA-chimney-far", "wall-jump chimney", Abilities.WallJump, new Vector3(113, 0, 331), "the glacier's far chimney (Round 5)", "G07");
            return w;
        }

        // The graybox playground's layout (positions match World/Playground.cs; 1 unit = 1 m). Area A at the origin, Area B at x = 400.
        public static WorldData Defaults()
        {
            var w = CreateInstance<WorldData>(); w.name = Playground;
            string[] heroes = { "Dan", "Abi", "Kira" };
            w.areas.Add(new AreaData { id = "A", name = "Area A", requirement = 10, firstVisit = heroes, storyShard = new Vector3(0, 1.4f, 52f), storyShardColor = new Color(.3f, .62f, 1f), spawn = new Vector3(0, .1f, -48f), spawnYaw = 0, kiraForm = KiraForm.Home, outfit = Outfits.Home, box = new Bounds(Vector3.zero, new Vector3(400, 400, 400)) });
            // Area B: Kira arrives as the sabertooth (Round 2), so her ClawIce is there on the first visit
            w.areas.Add(new AreaData { id = "B", name = "Area B", requirement = 0, firstVisit = new[] { "Dan", "Abi", "Kira", Abilities.ClawIce }, spawn = new Vector3(400, .1f, -12f), spawnYaw = 0, kiraForm = KiraForm.Sabertooth, outfit = Outfits.IceAge, box = new Bounds(new Vector3(400, 0, 0), new Vector3(400, 400, 400)) });
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
