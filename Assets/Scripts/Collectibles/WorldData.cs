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

    // Fix 7.2, item 2 (never stuck): a pit, pool, well, hole, crawl space or enclosed spot. The exe check `stuck` drops each hero at
    // `drop` and proves the bot gets out (toward `via`, then `exit`) within ~20 s with only the moves the party is sure to have there.
    // `needs` = what it takes to get in (empty: anyone can fall in); a later move = not enterable yet (reported, not tested).
    [Serializable]
    public sealed class LowSpot
    {
        public string id;
        public string kind;                         // "water", "pit", "crawl space", "enclosed"
        public Vector3 drop;                        // where a hero lands in it (world numbers)
        public Vector3 via;                         // a waypoint on the way out (zero = none)
        public Vector3 exit;                        // a spot outside (y: about the rim's height; the check finds the ground there)
        public float radius = 4f;                   // out = at least this far from `drop`, on the ground, at the rim's height
        public string needs = "";                   // to get in
        public string heroes = "";                  // only these can get in ("Kira": the crawl gap); empty = all three
        public string opens = "";                   // a cracked floor over it the check breaks first (getting in breaks it)
        public string checkpoint = "";              // the checkpoint a player has when they get here (falls send you back to it)
        public string note;
    }

    [Serializable]
    public sealed class AreaData
    {
        public string id;
        public string name;
        public int requirement;                    // Fix 6.1: the universal shard total (all worlds) that wakes the necklace here; 0 = no story shard here
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
        public List<LowSpot> lowSpots = new List<LowSpot>();  // Fix 7.2 (never stuck)
        [Tooltip("Round 6: the Violinist's instrument here (a part of Resources/World/instruments.fbx: Fiddle, Lyre)")]
        public string violinistInstrument = "Fiddle";
        [Tooltip("Fix 6.1: the clip his playing loop uses (Resources/Characters/Clips/<name>): violinist_play (the fiddle), violinist_lyre")]
        public string violinistPlayClip = "violinist_play";

        // Fix 6.1: the worlds in story order. Shards are one universal total (never spent); a world's requirement is a threshold on it.
        public static readonly string[] StoryOrder = { IceAge, Egypt, Bethlehem };

        public AreaData Area(string id) => areas.FirstOrDefault(a => a.id == id);
        public AreaData AreaAt(Vector3 p) => areas.FirstOrDefault(a => a.box.size != Vector3.zero && a.box.Contains(p));
        public IEnumerable<ShardSpot> InArea(string id) => shards.Where(s => s.area == id);

        // Rule 2's check (Fix 6.1: shards are universal). Available on arrival = every earlier world's shards reachable with that world's
        // own first-visit set plus what the party has on arriving here (moves learned on the way, such as the ground pound, are not
        // counted: a player may skip them), plus this area's first-visit shards. ok = available >= the threshold.
        public (int reachable, int requirement, bool ok, string detail) RequirementReport(string areaId)
        {
            var a = Area(areaId); var have = new HashSet<string>(a.firstVisit);
            var inArea = InArea(areaId).ToList();
            var ok = inArea.Where(s => s.requires.All(have.Contains)).ToList();
            var locked = inArea.Except(ok).Select(s => $"{s.id} needs {string.Join("+", s.requires)}");
            int earlier = 0; var parts = new List<string>();
            int idx = Array.IndexOf(StoryOrder, worldId);
            for (int i = 0; i < idx; i++)
            {
                var e = Load(StoryOrder[i]); if (!e || e.areas.Count == 0) continue;
                var eh = new HashSet<string>(e.areas[0].firstVisit.Concat(a.firstVisit));
                int n = e.shards.Count(s => s.requires.All(eh.Contains)); earlier += n;
                parts.Add($"{e.displayName} {n} of {e.shards.Count}");
            }
            int avail = earlier + ok.Count;
            parts.Add($"{a.name} {ok.Count} of {inArea.Count}");
            return (avail, a.requirement, avail >= a.requirement,
                $"area {a.id} ({a.name}): available on arrival {string.Join(" + ", parts)} = {avail} with [{string.Join(", ", a.firstVisit)}], threshold {a.requirement} (the universal total)" + (locked.Any() ? $"; locked here: {string.Join(", ", locked)}" : "") +
                (gates.Count > 0 ? $"; gates: {string.Join(", ", gates.Select(g => $"{g.id} ({g.kind}, needs {g.needs}: {string.Join(" ", g.opens)})"))}" : ""));
        }

        // Fix 7.2: every low or enclosed spot per world (new worlds: add theirs here; DataAssets adds missing ones to an existing asset)
        public static List<LowSpot> LowSpotDefaults(string world)
        {
            var l = new List<LowSpot>();
            void L(string id, string kind, Vector3 drop, Vector3 exit, float radius, string note, string needs = "", Vector3 via = default, string heroes = "", string opens = "", string cp = "") =>
                l.Add(new LowSpot { id = id, kind = kind, drop = drop, via = via, exit = exit, radius = radius, note = note, needs = needs, heroes = heroes, opens = opens, checkpoint = cp });
            if (world == IceAge)
            {
                L("IA-pond", "water", new Vector3(24, 1, 60), new Vector3(5, 0, 55), 12, "the half-frozen pond");
                L("IA-river", "water", new Vector3(0, -6.5f, 252), new Vector3(49, -1, 228), 14, "the river in the gorge (out up the ramp, x 44..54)", via: new Vector3(49, -8, 250.25f));
                L("IA-cave-pit", "pit", new Vector3(-29.5f, -3, 342.5f), new Vector3(-25.5f, 0, 342.5f), 3.3f, "the cave's pit under the cracked ice (steps up the east side)", Abilities.GroundPound, opens: "IA-crack-cave");
                L("IA-well", "pit", new Vector3(18, -2, 478), new Vector3(26, 0, 478), 4.6f, "the frozen well (steps up the east side)", Abilities.GroundPound, opens: "IA-crack-well");
                L("IA-lake-near", "water", new Vector3(-118, 1, 84), new Vector3(-110, 0, 84), 4.5f, "the frozen lake's near hole");
                L("IA-lake-mid", "water", new Vector3(-118, 1, 108), new Vector3(-110, 0, 108), 4.5f, "the frozen lake's exit hole");
                L("IA-lake-deep", "water", new Vector3(-118, 1, 133), new Vector3(-110, 0, 133), 4.5f, "the hole over the lake's deep part");
                L("IA-lake-far", "water", new Vector3(-101, 1, 160), new Vector3(-101, 0, 167), 4.5f, "the frozen lake's far hole");
                L("IA-crawl", "crawl space", new Vector3(-52, .3f, 320), new Vector3(-34, 0, 320), 8, "Kira's crawl chamber in the cave", heroes: "Kira");
                L("IA-grotto", "enclosed", new Vector3(71, .3f, 460), new Vector3(54, 0, 464), 8, "the hot-spring grotto (behind the cracked rock)", Abilities.ChargedPunch);
                L("IA-grotto-boulder", "enclosed", new Vector3(-49, .3f, 484), new Vector3(-40, 0, 484), 6, "the grotto behind the boulder", Abilities.MuscleMan);
                L("IA-lake-hollow", "enclosed", new Vector3(-154, .3f, 124), new Vector3(-144, 0, 124), 6, "the ice hollow behind the boulder", Abilities.MuscleMan);
            }
            else if (world == Egypt)
            {
                L("EG-nile", "water", new Vector3(-128, 1, 130), new Vector3(-98, 0, 130), 20, "the Nile (out onto the east bank)");
                L("EG-nile-deep", "water", new Vector3(-128, 1, 77), new Vector3(-98, 0, 77), 20, "the Nile's deep pool");
                L("EG-quicksand", "pit", new Vector3(50, 1, 150), new Vector3(60, 0, 172), 6, "the quicksand pit (sinking sand sends you back)", cp: "EG-quicksand");
                L("EG-tomb-sand", "pit", new Vector3(506, 61, 52), new Vector3(500, 60, 43), 3, "the tomb's sand-pit hall (sinking sand)", cp: "EG-tomb-sand");
                L("EG-tomb-pit", "pit", new Vector3(506, 52.5f, 96), new Vector3(500, 60, 87), 3, "the turning room's pit (falls send you back)", cp: "EG-tomb-turn");
                L("EG-quarry", "enclosed", new Vector3(92, .3f, 61), new Vector3(80, 0, 61), 6, "the quarry behind the huge block", Abilities.MuscleMan);
                L("EG-secret-room", "enclosed", new Vector3(516, 60.3f, 122), new Vector3(505, 60, 122), 6, "the secret room behind the cracked wall", Abilities.ChargedPunch);
            }
            else if (world == Bethlehem)
            {
                L("BE-well", "water", new Vector3(108, 2, 72), new Vector3(108, 0, 77), 3.5f, "the well (the rope climbs out)");
                L("BE-barn", "enclosed", new Vector3(0, .3f, 34), new Vector3(0, 0, 20), 8, "inside the barn (the door is south)");
                L("BE-cave", "enclosed", new Vector3(190, .3f, 60), new Vector3(178, 0, 60), 6, "the shepherds' cave behind the boulder", Abilities.MuscleMan);
            }
            return l;
        }

        public static WorldData Load(string id) => Resources.Load<WorldData>("Data/Worlds/" + id) ?? (id == IceAge ? IceAgeDefaults() : id == Egypt ? EgyptDefaults() : id == Bethlehem ? BethlehemDefaults() : Defaults());
        public const string IceAge = "IceAge", Playground = "Playground", Egypt = "Egypt", Bethlehem = "Bethlehem";

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
            w.lowSpots.AddRange(LowSpotDefaults(w.worldId));
            return w;
        }

        // Round 6: Ancient Egypt (World/Egypt.cs builds it; the ground and the big pieces in Tools/Blender/egypt_world.py, the same numbers).
        // One area, "E". 60 small shards, 35 wake the necklace (DEFAULT). Shard y = the height of the surface it sits on (a hint, as the
        // Ice Age). The pyramid's inside (the tomb) is a space of its own at Egypt.Tomb (x 500, y 60): its shards' x / y / z are world
        // numbers. Rule 2: the first visit has the three heroes and mummy Kira (mummies don't wake for her); not the ground pound (a
        // player may skip the Ice Age's Violinist), not the double jump (Abi learns it here, from the Violinist), not the later moves.
        public static WorldData EgyptDefaults()
        {
            var w = CreateInstance<WorldData>(); w.name = Egypt; w.worldId = Egypt; w.displayName = "Ancient Egypt"; w.heightsAreHints = true;
            w.violinistInstrument = "Lyre"; w.violinistPlayClip = "violinist_lyre";   // Fix 6.1: his own lyre clip
            // Fix 6.1: 75 = the universal total (Ice Age + Egypt; 120 exist, 93 available on arrival)
            w.areas.Add(new AreaData { id = "E", name = "Ancient Egypt", requirement = 75, firstVisit = new[] { "Dan", "Abi", "Kira", Abilities.MummyHush },
                storyShard = new Vector3(500, 61.6f, 127f), storyShardColor = new Color(1f, .18f, .14f), spawn = new Vector3(0, .3f, 8f), spawnYaw = 0,
                kiraForm = KiraForm.Mummy, outfit = Outfits.Egypt, box = new Bounds(new Vector3(160, 0, 160), new Vector3(800, 600, 700)) });
            void S(string id, float x, float y, float z, string note, params string[] req) => w.shards.Add(new ShardSpot { id = id, area = "E", position = new Vector3(x, y, z), requires = req, note = note });
            // the town (the arrival plaza, the main street, the north gate) and the market
            S("E01", 0, 0, 18, "the arrival plaza"); S("E02", 0, 0, 32, "main street"); S("E03", -4, 0, 44, "main street"); S("E04", 4, 0, 96, "main street");
            S("E05", 0, 0, 114, "the north gate"); S("E06", -20, 0, 58, "market"); S("E07", 20, 0, 80, "market"); S("E08", -26, 0, 82, "market, behind the stalls");
            S("E09", 26, 0, 54, "market, behind the stalls");
            // the rooftops (crates and stairs up), a far roof across a gap only Kira's pounce clears (Bethlehem), the obelisk's top
            S("E10", -27, 4.6f, 26, "west rooftop (the plank across)"); S("E11", -27, 4.6f, 40, "west rooftop (up the outside stair)"); S("E12", 17, 4.6f, 100, "east rooftop (up the outside stair)"); S("E13", -17, 4.6f, 104, "north-west rooftop (up the outside stair)");
            S("E14", -43.5f, 5, 38, "the far roof (Kira's pounce)", Abilities.Pounce); S("E15", -43.5f, 5, 42, "the far roof (Kira's pounce)", Abilities.Pounce);   // Round 7: the roof moved in to a 10.5 m gap (it was 19 m)
            S("E16", 0, 5.6f, 64, "the obelisk's golden ledge (double jump)", Abilities.DoubleJump);
            // the cat temple: its door opens for Kira (a cat plate)
            S("E17", -54, 0, 66, "the cat temple (Kira opens its door)", "Kira"); S("E18", -54, 0, 78, "the cat temple (Kira opens its door)", "Kira");
            // the quarry road: workers haul stones on rollers; a quicksand pit crossed on a stone block; a huge block (Muscle Man)
            S("E19", 60, 0, 60, "the quarry road"); S("E20", 64, 0, 90, "the quarry road"); S("E21", 58, 0, 120, "the quarry road");
            S("E22", 60, 0, 172, "past the quicksand (ride the stone)"); S("E23", 74, 0, 176, "past the quicksand");
            S("E24", 92, 0, 56, "the quarry, behind the huge block (Muscle Man)", Abilities.MuscleMan); S("E25", 92, 0, 66, "the quarry, behind the huge block (Muscle Man)", Abilities.MuscleMan);
            // the dunes, along the camel's route to the Sphinx
            S("E26", 20, 0, 146, "dunes (the camel's route)"); S("E27", 2, 0, 164, "dunes (the camel's route)"); S("E28", -14, 0, 178, "dunes (the camel's route)");
            S("E29", 40, 0, 192, "a dune top"); S("E30", -12, 0, 140, "a dune hollow");
            // the Great Sphinx (Kira's face): its plinth, paws, back and head
            S("E31", -35, 4, 208, "the Sphinx's right paw"); S("E32", -35, 4, 222, "the Sphinx's left paw"); S("E33", -62, 13, 215, "the Sphinx's back");
            S("E34", -72, 13, 215, "the Sphinx's back"); S("E35", -46, 22, 215, "the Sphinx's head"); S("E36", -29, 2, 201, "the Sphinx's plinth");
            // the pyramid under construction: the scaffolding up its south face, the top
            S("E37", 60, 8, 200, "scaffolding, level 2"); S("E38", 80, 16, 200, "scaffolding, level 4"); S("E39", 60, 24, 200, "scaffolding, level 6");
            S("E40", 80, 32, 200, "scaffolding, level 8"); S("E41", 74, 36, 236, "the pyramid's unfinished top");
            S("E42", 88, 39.3f, 199, "the high plank over the scaffolding (double jump)", Abilities.DoubleJump);
            // the Nile bank: reeds, the boats, the far bank, a swim, the deep pool (deep dive, later)
            S("E43", -90, 0, 30, "the reeds"); S("E44", -94, 0, 80, "the reeds"); S("E45", -162, 0, 40, "the far bank (ride a boat)");
            S("E46", -162, 0, 110, "the far bank (ride a boat)"); S("E47", -124, -4, 50, "the riverbed (dive)");
            S("E48", -128, -15, 75, "the deep pool (deep dive)", Abilities.DeepDive); S("E49", -131, -15, 79, "the deep pool (deep dive)", Abilities.DeepDive);
            // the pyramid's inside (the tomb, at x 500, floor y 60): traps, dark corridors, machinery, hidden chambers
            S("E50", 500, 60, 15, "tomb: the blade corridor"); S("E51", 500, 60, 39, "tomb: the blade corridor"); S("E52", 500, 60.3f, 53, "tomb: the sand-pit hall (stepping stones)");
            S("E53", 500, 60, 74, "tomb: the sliding-block corridor"); S("E54", 507, 60, 96, "tomb: across the turning bridge");
            S("E55", 522, 60, 96, "tomb: the mummies' gallery (mummy Kira walks past quietly)"); S("E56", 534, 60, 96, "tomb: the mummies' gallery");
            S("E57", 547, 60, 92, "tomb: the hidden cat chamber (Kira opens it)", "Kira");
            S("E58", 474, 66, 96, "tomb: the counterweight lift's ledge (Dan pushes the weight)", "Dan");
            S("E59", 515, 60, 122, "tomb: behind the cracked wall (charged punch)", Abilities.ChargedPunch);
            S("E60", 486, 71, 118, "tomb: the top of the narrow shaft (wall jump)", Abilities.WallJump);
            void G(string id, string kind, string needs, Vector3 at, string note, params string[] opens) => w.gates.Add(new GateData { id = id, kind = kind, needs = needs, position = at, note = note, opens = opens });
            G("EG-pounce-roof", "pounce gap", Abilities.Pounce, new Vector3(-36.5f, 5, 40), "the gap to the far roof (Kira's pounce, Bethlehem)", "E14", "E15");
            G("EG-ledge-obelisk", "double-jump ledge", Abilities.DoubleJump, new Vector3(0, 5.6f, 64), "the obelisk's top", "E16");
            G("EG-ledge-scaffold", "double-jump ledge", Abilities.DoubleJump, new Vector3(88, 39.3f, 199), "the high plank over the scaffolding", "E42");
            G("EG-block-quarry", "huge block", Abilities.MuscleMan, new Vector3(86, 0, 61), "the quarry's huge block (Muscle Man, Medieval)", "E24", "E25");
            G("EG-deep-nile", "deep water", Abilities.DeepDive, new Vector3(-128, -6, 77), "the Nile's deep pool (deep dive, Pirate Seas)", "E48", "E49");
            G("EG-crack-tomb", "cracked wall", Abilities.ChargedPunch, new Vector3(511, 60, 122), "the burial chamber's cracked wall (charged punch, Medieval)", "E59", "EG-H2");
            G("EG-shaft-tomb", "wall-jump shaft", Abilities.WallJump, new Vector3(486, 60, 118), "the narrow shaft by the burial chamber (wall jump, Steampunk)", "E60");
            w.lowSpots.AddRange(LowSpotDefaults(w.worldId));
            return w;
        }

        // Round 7: Bethlehem (World/Bethlehem.cs builds it; the ground and the props in Tools/Blender/bethlehem_world.py, the same numbers).
        // One area, "BE". 60 small shards. Fix 6.1's rule: the threshold is on the universal total, about 60-65 % of every shard up to
        // here (Ice Age 60 + Egypt 60 + Bethlehem 60 = 180): 112. Rule 2: the first visit has the three heroes and lamb Kira (flock hide,
        // the wool bounce); not the ground pound or the double jump (a player may skip both Violinists), not the pounce (Kira learns it
        // here), not the later moves.
        public static WorldData BethlehemDefaults()
        {
            var w = CreateInstance<WorldData>(); w.name = Bethlehem; w.worldId = Bethlehem; w.displayName = "Bethlehem"; w.heightsAreHints = true;
            w.violinistInstrument = "Harp"; w.violinistPlayClip = "violinist_lyre";   // item 2: a small shepherd's harp, held and played like the lyre
            w.areas.Add(new AreaData { id = "BE", name = "Bethlehem", requirement = 112, firstVisit = new[] { "Dan", "Abi", "Kira", Abilities.FlockHide, Abilities.WoolBounce },
                storyShard = new Vector3(0, 1.25f, 37f), storyShardColor = new Color(1f, .78f, .2f), spawn = new Vector3(0, .1f, 29.5f), spawnYaw = 180,
                kiraForm = KiraForm.Lamb, outfit = Outfits.Bethlehem, box = new Bounds(new Vector3(0, 0, 90), new Vector3(440, 600, 400)) });
            void S(string id, float x, float y, float z, string note, params string[] req) => w.shards.Add(new ShardSpot { id = id, area = "BE", position = new Vector3(x, y, z), requires = req, note = note });
            // the barn and its farmyard (x -35..35, z -10..70): hay stacks, the loft, bounces only lamb Kira gets
            S("F01", 0, 0, 18, "in front of the barn"); S("F02", -14, 0, 10, "the farmyard"); S("F03", 16, 0, 8, "the farmyard"); S("F04", -24, 0, 48, "behind the hay stacks");
            S("F05", 27, 0, 54, "by the trough"); S("F06", 3, 3.2f, 41, "the hay loft (up the bales)"); S("F07", 20, 3.6f, 30, "on the bale tower");
            S("F08", 0, 8.3f, 37, "on the barn's ridge (lamb Kira bounces up)", "Kira", Abilities.WoolBounce);
            S("F09", -22, 4.4f, 20, "on top of the big hay stack (lamb Kira bounces up)", "Kira", Abilities.WoolBounce);
            S("F10", 8, 0, 62, "by the cart, north yard");
            // the hills (east): shepherds' camps, flocks, rocky outcrops, the olive grove
            S("H01", 50, 0, 20, "the first hill"); S("H02", 62, 0, 60, "by the Violinist's camp"); S("H03", 80, 0, 30, "by the Violinist's camp"); S("H04", 95, 0, 50, "the hillside");
            S("H05", 120, 0, 100, "the middle camp"); S("H06", 138, 0, 120, "the middle camp"); S("H07", 150, 0, 10, "the south camp"); S("H08", 165, 0, -20, "the far hill");
            S("H09", 172, 0, 40, "the far hill"); S("H10", 100, 0, 160, "the olive grove"); S("H11", 120, 0, 182, "the olive grove"); S("H12", 140, 0, 165, "the olive grove");
            S("H13", 90, 3.8f, 110, "on the rocky outcrop"); S("H14", 130, 0, 60, "among the goats' rocks"); S("H15", 60, 0, 130, "the north slope"); S("H16", 170, 0, 90, "the far hill");
            // the village (west): streets, the market, courtyards, the inn, rooftops (watchmen with lanterns, guard dogs)
            S("V01", -64, 0, 58, "the village gate"); S("V02", -90, 0, 52, "the main street"); S("V03", -115, 0, 50, "the market"); S("V04", -125, 0, 70, "the market");
            S("V05", -108, 0, 72, "the market, behind a stall"); S("V06", -95, 0, 100, "a courtyard"); S("V07", -140, 0, 30, "a courtyard"); S("V08", -150, 0, 84, "in front of the inn");
            S("V09", -100, 4.2f, 46, "a flat roof (up the outside stair)"); S("V10", -130, 4.2f, 104, "a flat roof (up the outside stair)"); S("V11", -160, 4.2f, 46, "a flat roof (across the planks)");
            S("V12", -170, 0, 120, "the back alley"); S("V13", -80, 0, 10, "the south lane"); S("V14", -186, 0, 66, "the west street");
            S("V15", -188, 0, 8, "the watchmen's yard (slip in with the flock: lamb Kira)", "Kira", Abilities.FlockHide); S("V16", -60, 0, 130, "the north lane");
            // the Wise Men's road (north): shards along their route
            S("R01", 150, 0, 198, "the Wise Men's road"); S("R02", 100, 0, 185, "the Wise Men's road"); S("R03", 60, 0, 190, "the Wise Men's road");
            S("R04", 10, 0, 192, "the Wise Men's road"); S("R05", -40, 0, 182, "the Wise Men's road"); S("R06", -100, 0, 196, "the Wise Men's road");
            // Kira's pounce (learned here from the Violinist): the hill pillars, the village's far roof
            S("P01", 177.5f, 5f, 139, "the far rock pillar (Kira's pounce)", Abilities.Pounce);
            S("P03", -178, 4.2f, 102, "the far roof (Kira's pounce)", Abilities.Pounce); S("P04", -179, 4.2f, 106, "the far roof (Kira's pounce)", Abilities.Pounce);
            // the places later moves open (visible now)
            S("W01", 108, 0, 72, "the bottom of the well, 13 m down (deep dive)", Abilities.DeepDive); S("W02", 109, 0, 73.5f, "the bottom of the well, 13 m down (deep dive)", Abilities.DeepDive);
            S("M01", 188, 0, 62, "the shepherds' cave, behind the boulder (Muscle Man)", Abilities.MuscleMan); S("M02", 192, 0, 58, "the shepherds' cave, behind the boulder (Muscle Man)", Abilities.MuscleMan);
            S("C01", -160, 0, 136, "behind the cracked courtyard wall (charged punch)", Abilities.ChargedPunch); S("C02", -165, 0, 138, "behind the cracked courtyard wall (charged punch)", Abilities.ChargedPunch);
            S("J01", -90, 8.4f, 133, "the top of the narrow gap between two houses (wall jump)", Abilities.WallJump);
            S("D01", 90.5f, 6.5f, 114, "the high rock over the outcrop (double jump)", Abilities.DoubleJump);
            S("D02", -150, 10.1f, 97, "the inn's roof terrace (double jump)", Abilities.DoubleJump);
            void G(string id, string kind, string needs, Vector3 at, string note, params string[] opens) => w.gates.Add(new GateData { id = id, kind = kind, needs = needs, position = at, note = note, opens = opens });
            G("BE-pounce-pillar", "pounce gap", Abilities.Pounce, new Vector3(169, 5, 140), "the gap to the far rock pillar (Kira's pounce, learned here)", "P01", "BE-H2");
            G("BE-pounce-roof", "pounce gap", Abilities.Pounce, new Vector3(-170, 4.2f, 104), "the gap to the village's far roof (Kira's pounce, learned here)", "P03", "P04");
            G("BE-deep-well", "deep water", Abilities.DeepDive, new Vector3(108, -4, 72), "the well (deep dive, Pirate Seas)", "W01", "W02");
            G("BE-boulder-cave", "boulder", Abilities.MuscleMan, new Vector3(184, 0, 60), "the shepherds' cave (Muscle Man, Medieval)", "M01", "M02");
            G("BE-crack-courtyard", "cracked wall", Abilities.ChargedPunch, new Vector3(-160, 0, 131), "the courtyard's cracked wall (charged punch, Medieval)", "C01", "C02");
            G("BE-gap-houses", "wall-jump gap", Abilities.WallJump, new Vector3(-90, 0, 133), "the narrow gap between two houses (wall jump, Steampunk)", "J01");
            G("BE-ledge-rock", "double-jump ledge", Abilities.DoubleJump, new Vector3(90.5f, 6.5f, 114), "the high rock over the outcrop", "D01");
            G("BE-ledge-inn", "double-jump ledge", Abilities.DoubleJump, new Vector3(-150, 10.1f, 97), "the inn's roof terrace", "D02");
            w.lowSpots.AddRange(LowSpotDefaults(w.worldId));
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
