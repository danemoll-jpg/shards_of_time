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
        [Tooltip("Fix 8.1: and at least this many shards from this world (both must be true)")] public int fromHere = 20;
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

        // Fix 10.1: stepped thresholds: each world's = the previous world's + 25 (Ice Age 25, Egypt 50, Bethlehem 75, Medieval 100,
        // Pirate Seas 125, ...), and at least 15 from the world itself (AreaData.fromHere). Replaces Fix 8.1's 75 % rule.
        // Fix 6.1: the worlds in story order. Shards are one universal total (never spent); a world's requirement is a threshold on it.
        public static readonly string[] StoryOrder = { IceAge, Egypt, Bethlehem, Medieval, Pirate, Steampunk, Tomorrowland, Moon };   // Round 11: Victorian Steampunk; Rounds 12-13: Tomorrowland, the Moon

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
            // Fix 10.1: two players. The minimum player arrives with exactly the previous world's threshold (they took only what it
            // needed) and may only use shards reachable here before this necklace wakes (`ok`: the first-visit moves; the under-sea
            // half needs Abilities.Overboard, never on arrival). The completionist arrives with every shard reachable so far.
            int prev = 0;
            if (idx > 0) { var pw = Load(StoryOrder[idx - 1]); if (pw && pw.areas.Count > 0) prev = pw.areas[0].requirement; }
            int minNeed = Mathf.Max(a.requirement - prev, a.fromHere);
            bool minOk = ok.Count >= minNeed, fullOk = avail >= a.requirement && ok.Count >= a.fromHere;
            return (avail, a.requirement, minOk && fullOk,
                $"area {a.id} ({a.name}): threshold {a.requirement} and {a.fromHere} from here; minimum player: arrives with {prev}, needs {minNeed} here, {ok.Count} reachable before the wake: {(minOk ? "ok" : "FAILS")}; completionist: available on arrival {string.Join(" + ", parts)} = {avail} with [{string.Join(", ", a.firstVisit)}]: {(fullOk ? "ok" : "FAILS")}" + (locked.Any() ? $"; locked here: {string.Join(", ", locked)}" : "") +
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
                L("IA-grotto", "enclosed", new Vector3(71, .3f, 460), new Vector3(54, 0, 464), 8, "the hot-spring grotto (behind the cracked rock)", Abilities.ChargedPunch, opens: "IA-rock-hotspring");
                L("IA-grotto-boulder", "enclosed", new Vector3(-49, .3f, 484), new Vector3(-40, 0, 484), 6, "the grotto behind the boulder", Abilities.MuscleMan, opens: "IA-boulder-grotto");
                L("IA-lake-hollow", "enclosed", new Vector3(-154, .3f, 124), new Vector3(-144, 0, 124), 6, "the ice hollow behind the boulder", Abilities.MuscleMan, opens: "IA-boulder-lake");
            }
            else if (world == Egypt)
            {
                L("EG-nile", "water", new Vector3(-128, 1, 130), new Vector3(-98, 0, 130), 20, "the Nile (out onto the east bank)");
                L("EG-nile-deep", "water", new Vector3(-128, 1, 77), new Vector3(-98, 0, 77), 20, "the Nile's deep pool");
                L("EG-quicksand", "pit", new Vector3(50, 1, 150), new Vector3(60, 0, 172), 6, "the quicksand pit (sinking sand sends you back)", cp: "EG-quicksand");
                L("EG-tomb-sand", "pit", new Vector3(506, 61, 52), new Vector3(500, 60, 43), 3, "the tomb's sand-pit hall (sinking sand)", cp: "EG-tomb-sand");
                L("EG-tomb-pit", "pit", new Vector3(506, 52.5f, 96), new Vector3(500, 60, 87), 3, "the turning room's pit (falls send you back)", cp: "EG-tomb-turn");
                L("EG-quarry", "enclosed", new Vector3(92, .3f, 61), new Vector3(80, 0, 61), 6, "the quarry behind the huge block", Abilities.MuscleMan, opens: "EG-block-quarry");
                L("EG-secret-room", "enclosed", new Vector3(516, 60.3f, 122), new Vector3(505, 60, 122), 6, "the secret room behind the cracked wall", Abilities.ChargedPunch, opens: "EG-crack-tomb");
            }
            else if (world == Bethlehem)
            {
                L("BE-well", "water", new Vector3(108, 2, 72), new Vector3(108, 0, 77), 3.5f, "the well (the rope climbs out)");
                L("BE-barn", "enclosed", new Vector3(0, .3f, 34), new Vector3(0, 0, 20), 8, "inside the barn (the door is south)");
                L("BE-cave", "enclosed", new Vector3(190, .3f, 60), new Vector3(178, 0, 60), 6, "the shepherds' cave behind the boulder", Abilities.MuscleMan, opens: "BE-boulder-cave");
            }
            else if (world == Medieval)   // Round 8
            {
                L("ME-moat-west", "water", new Vector3(-42, 0, 40), new Vector3(-55, 0, 40), 8, "the moat, west side (out onto the outer bank)");
                L("ME-moat-north", "water", new Vector3(0, 0, 96), new Vector3(0, 0, 109), 8, "the moat, north side");
                L("ME-moat-east", "water", new Vector3(42, 0, 30), new Vector3(55, 0, 30), 8, "the moat, east side");
                L("ME-moat-south", "water", new Vector3(24, 0, -12), new Vector3(24, 0, -25), 8, "the moat, south side (by the drawbridge)");
                L("ME-moat-deep", "water", new Vector3(-42, 0, 96), new Vector3(-55, 0, 96), 8, "over the moat's deep pit (NW corner)");
                L("ME-dungeon", "enclosed", new Vector3(400, .3f, 30), new Vector3(-27, 0, 44), 30, "the dungeon (its stair door back to the courtyard)", via: new Vector3(400, 0, -1.4f));   // (past the door line: the bot counts a waypoint reached 1.5 m early)
                L("ME-hall", "enclosed", new Vector3(8, .3f, 70), new Vector3(0, 0, 56), 10, "the great hall (the door is south)", via: new Vector3(0, 0, 64.5f));
                L("ME-yard", "enclosed", new Vector3(-25, .3f, 7), new Vector3(-12, 0, 8), 6, "the walled yard behind the boulder", Abilities.MuscleMan, opens: "ME-boulder-yard");
                L("ME-armory", "enclosed", new Vector3(22, .3f, 4), new Vector3(22, 0, 14), 6, "the armory behind the portcullis", Abilities.MuscleMan, opens: "ME-portcullis-armory");
                L("ME-store", "enclosed", new Vector3(-26, .3f, 56), new Vector3(-16, 0, 56), 6, "the store room behind the cracked wall", Abilities.ChargedPunch, opens: "ME-crack-store");
            }
            else if (world == Pirate)   // Round 10 (the deep-sea spots: the check opens the deep sea first; "underwater" = out of the place, any mode)
            {
                L("PS-sea-port", "water", new Vector3(-10, 0, -6), new Vector3(-4.6f, 3.5f, -6), 4, "the sea by the ship's port side (the rope ladder up the hull)", via: new Vector3(-7.2f, 0, -6));
                L("PS-sea-east", "water", new Vector3(14, 0, 20), new Vector3(33, 0, 20), 14, "the sea between the ship and the island (swim to the beach)");
                L("PS-sea-south", "water", new Vector3(0, 0, -34), new Vector3(32, 0, -22), 22, "the sea south of the ship (the beach)");
                L("PS-sea-north", "water", new Vector3(0, 0, 45), new Vector3(37, 0, 40), 24, "the sea north of the ship (the beach)");
                L("PS-sea-west", "water", new Vector3(-36, 0, 6), new Vector3(-4.6f, 3.5f, -6), 8, "the open sea west of the ship (back to its rope ladder)", via: new Vector3(-7.2f, 0, -6));
                L("PS-cove", "water", new Vector3(82, 0, 66), new Vector3(82, 0, 42), 16, "the island's cove (out onto its bank)");
                L("PS-hold", "enclosed", new Vector3(.5f, .9f, .3f), new Vector3(0, 3.5f, 8.6f), 7, "the ship's hold (up the ramp to the deck)");
                L("PS-hold-back", "enclosed", new Vector3(0, .9f, -9.5f), new Vector3(0, 3.5f, 8.6f), 12, "the hold's back room behind the crates", Abilities.MuscleMan, via: new Vector3(0, .6f, 1.0f), opens: "PS-crates-hold");
                L("PS-cabin", "enclosed", new Vector3(0, 3.8f, -17.5f), new Vector3(0, 3.5f, -9), 6, "the captain's cabin behind the cannon", Abilities.MuscleMan, opens: "PS-cannon-cabin");
                L("PS-locker", "enclosed", new Vector3(0, 3.8f, 19.5f), new Vector3(0, 3.5f, 10), 6, "the bosun's locker behind the anchor", Abilities.MuscleMan, opens: "PS-anchor-locker");
                L("PS-cave", "underwater", new Vector3(-135, -16, 0), new Vector3(-110, -14, 0), 17, "the sea cave (out of its mouth)", Abilities.DeepDive, via: new Vector3(-121, -15.5f, 0));
                L("PS-grotto", "underwater", new Vector3(-134, -17, 25), new Vector3(-134, -16, 6), 12, "the cave's little grotto (back through the narrow gap)", heroes: "Kira", via: new Vector3(-136.65f, -17.7f, 19.8f));
                L("PS-wreck", "underwater", new Vector3(-70, -16.5f, 118), new Vector3(-70, -10, 92), 15, "inside the shipwreck (out through its broken side or deck)", Abilities.DeepDive);
            }
            else if (world == Steampunk)   // Round 11 (the factory spots: after the press; the check smashes it first)
            {
                L("SP-belt", "enclosed", new Vector3(0, 3.1f, 40), new Vector3(9, 0, 74), 7, "on the conveyor belt (after the press: down its side stairs)", via: new Vector3(0, 3.1f, 74));
                L("SP-boiler-room", "enclosed", new Vector3(-30, .3f, 6), new Vector3(-18, 0, 6), 6, "the boiler room behind the gear door (mechanical Kira winds it open)", Abilities.WindUp, opens: "SP-geardoor-boiler");
                L("SP-cage", "enclosed", new Vector3(32, .3f, 44), new Vector3(22, 0, 44), 6, "the store cage behind the bars (Muscle Man bends them)", Abilities.MuscleMan, opens: "SP-bars-cage");
                L("SP-nook", "enclosed", new Vector3(-33, .3f, 33), new Vector3(-24, 0, 33), 5, "the tool nook behind the machine (Muscle Man lifts it)", Abilities.MuscleMan, opens: "SP-crank-nook");
                L("SP-lane", "enclosed", new Vector3(20, .3f, 64.5f), new Vector3(20, 0, 46), 10, "the end of the stamper lane (Muscle Man holds the big stamper up)", Abilities.MuscleMan, opens: "SP-stamper-lane");
                L("SP-chimney", "pit", new Vector3(27.2f, .3f, 93), new Vector3(27.2f, 0, 85), 6, "the brick stack's shaft by the north wall (walk out the front)");
                L("SP-alley-west", "pit", new Vector3(-14, .3f, 139.2f), new Vector3(-4, 0, 139.2f), 7, "the narrow alley between the tall houses (walk out to the avenue)");
                L("SP-alley-pipes", "enclosed", new Vector3(14, .3f, 143.5f), new Vector3(4, 0, 143.5f), 7, "the alley behind the pipes (Muscle Man bends them)", Abilities.MuscleMan, opens: "SP-pipes-alley");
                L("SP-passage", "enclosed", new Vector3(-28.5f, .3f, 238), new Vector3(-28.5f, 0, 226), 7, "the passage behind the crates (Muscle Man lifts them)", Abilities.MuscleMan, opens: "SP-crates-passage");
                L("SP-gear-yard", "enclosed", new Vector3(86, .3f, 200), new Vector3(62, 0, 200), 14, "the clockmaker's yard behind the great gear gate (mechanical Kira winds it open)", Abilities.WindUp, opens: "SP-geargate");
            }
            else if (world == Tomorrowland)   // Round 12
            {
                L("TL-pit", "pit", new Vector3(-4, -2.3f, 50.5f), new Vector3(-4, 0, 40), 5, "the maintenance pit under the parked car (a ramp up its south side)", Abilities.MuscleMan, opens: "TL-car-hatch");
                L("TL-courtyard", "enclosed", new Vector3(-30, .3f, 118), new Vector3(-9, 0, 117.75f), 8, "the courtyard behind the vending machine (out through the alley)", Abilities.MuscleMan, via: new Vector3(-19, 0, 117.75f), opens: "TL-vending");
                L("TL-nook", "enclosed", new Vector3(27, .3f, 104.5f), new Vector3(9, 0, 104.5f), 6, "the nook behind the chrome bars", Abilities.MuscleMan, opens: "TL-bars-nook");
                L("TL-diner", "enclosed", new Vector3(36, .3f, 186), new Vector3(23, 0, 186), 8, "inside the Atomic Diner (out of its door, west)", Abilities.PlugIn, opens: "TL-door-diner");
                L("TL-shaft", "pit", new Vector3(-19, .3f, 63.2f), new Vector3(-8, 0, 63.2f), 6, "the narrow gap between the two tall shops (walk out the front)");
                L("TL-hall", "enclosed", new Vector3(-10, .3f, 300), new Vector3(0, 0, 250), 30, "the launch hall (out through its south door)", via: new Vector3(0, 0, 263));
            }
            else if (world == Moon)   // Round 13
            {
                foreach (var s in MoonLowSpots()) l.Add(s);
            }
            return l;
        }

        public static WorldData Load(string id) => Resources.Load<WorldData>("Data/Worlds/" + id) ?? (id == IceAge ? IceAgeDefaults() : id == Egypt ? EgyptDefaults() : id == Bethlehem ? BethlehemDefaults() : id == Medieval ? MedievalDefaults() : id == Pirate ? PirateDefaults() : id == Steampunk ? SteampunkDefaults() : id == Tomorrowland ? TomorrowlandDefaults() : id == Moon ? MoonDefaults() : Defaults());
        public const string IceAge = "IceAge", Playground = "Playground", Egypt = "Egypt", Bethlehem = "Bethlehem", Medieval = "Medieval", Pirate = "PirateSeas", Steampunk = "Steampunk", Tomorrowland = "Tomorrowland", Moon = "Moon";

        // Round 5: 60 shards, 35 wake the necklace; the lake, the glacier and the hot-spring grotto; the gates later moves open (gates).
        // Round 4: the Ice Age, the first real world (World/IceAge.cs builds it; layout numbers in Tools/Blender/iceage_world.py). One
        // area, the valley "V". 40 small shards, 25 wake the necklace (DEFAULT). Shard y = the height of the surface it sits on (a hint:
        // the game raycasts down from 4 m above it and puts the shard 1 m over what it finds). Rule 2: the first visit has the three
        // heroes and sabertooth Kira's claws; no double jump (Abi learns it later), no Muscle Man, and not the ground pound (Dan learns it
        // here from the Violinist, so it isn't counted): 33 of 40 are reachable without them.
        public static WorldData IceAgeDefaults()
        {
            var w = CreateInstance<WorldData>(); w.name = IceAge; w.worldId = IceAge; w.displayName = "The Ice Age"; w.heightsAreHints = true;
            w.areas.Add(new AreaData { id = "V", name = "The Ice Age", requirement = 25, fromHere = 15, firstVisit = new[] { "Dan", "Abi", "Kira", Abilities.ClawIce },
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
            w.areas.Add(new AreaData { id = "E", name = "Ancient Egypt", requirement = 50, fromHere = 15, firstVisit = new[] { "Dan", "Abi", "Kira", Abilities.MummyHush },
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
            w.areas.Add(new AreaData { id = "BE", name = "Bethlehem", requirement = 75, fromHere = 15, firstVisit = new[] { "Dan", "Abi", "Kira", Abilities.FlockHide, Abilities.WoolBounce },
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

        // Round 8: the Medieval Kingdom (World/Medieval.cs builds it; the ground and the castle in Tools/Blender/medieval_world.py, the same
        // numbers). One area, "ME". 60 small shards. Fix 6.1's rule: the threshold is on the universal total, about 60-65 % of every shard up
        // to here (4 x 60 = 240): 150. Rule 2: the first visit has the three heroes and dragon Kira (glide, fire puff); not the ground pound,
        // the double jump or the pounce (a player may skip those Violinists), not the charged punch (Dan learns it here), not Muscle Man (he
        // joins only at the stone, the end), not the later moves. The dungeon is a space of its own at x 400 (its numbers are world numbers).
        public static WorldData MedievalDefaults()
        {
            var w = CreateInstance<WorldData>(); w.name = Medieval; w.worldId = Medieval; w.displayName = "The Medieval Kingdom"; w.heightsAreHints = true;
            w.violinistInstrument = "Lute"; w.violinistPlayClip = "violinist_lyre";   // item 7: a bard with his lute (held across his body by IK, played with the lyre's clip, as the harp)
            w.areas.Add(new AreaData { id = "ME", name = "The Medieval Kingdom", requirement = 100, fromHere = 15, firstVisit = new[] { "Dan", "Abi", "Kira", Abilities.Glide, Abilities.FirePuff },
                storyShard = new Vector3(0, 1.55f, 34f), storyShardColor = new Color(.62f, .32f, 1f), spawn = new Vector3(0, .1f, 8f), spawnYaw = 0,
                kiraForm = KiraForm.Dragon, outfit = Outfits.Medieval, box = new Bounds(new Vector3(130, 0, -10), new Vector3(720, 600, 420)) });
            void S(string id, float x, float y, float z, string note, params string[] req) => w.shards.Add(new ShardSpot { id = id, area = "ME", position = new Vector3(x, y, z), requires = req, note = note });
            // the courtyard (x -30..30, z 0..63): the arrival, benches, flower pots, the arcade along the east wall (its roof deck at 6.6)
            S("C01", 0, 0, 14, "the courtyard: the arrival"); S("C02", -12, 0, 22, "the courtyard: by the benches"); S("C03", 12, 0, 44, "the courtyard: by the flower pots");
            S("C04", -20, 0, 50, "the courtyard: the north-west corner"); S("C05", 18, 0, 14, "the courtyard: the south-east corner"); S("C06", 27, 0, 30, "under the arcade");
            S("C07", 27, 6.6f, 20, "the arcade's roof (up its stair)"); S("C08", 27, 6.6f, 44, "the arcade's roof (up its stair)");
            // the walls and towers: the west wall walk (stair from the courtyard), the north-west tower top; the east walk (from the arcade's roof)
            S("W01", -31.5f, 10, 20, "the west wall walk"); S("W02", -31.5f, 10, 36, "the west wall walk"); S("W03", -31.5f, 10, 42, "the west wall walk");
            S("W04", -31.5f, 16, 61.5f, "the north-west tower's top"); S("W05", 31.5f, 10, 30, "the east wall walk"); S("W06", 31.5f, 10, 42, "the east wall walk");
            S("W07", 31.5f, 16, 61.5f, "the north-east tower's top");
            // the great hall (the keep: floor 0, the gallery at 6, chandeliers to swing across to the east ledge)
            S("G01", 0, .95f, 73, "the great hall: on the feast table"); S("G02", -7, 0, 77, "the great hall"); S("G03", 8, 0, 69, "the great hall: by the fireplace");
            S("G04", -13, 6, 73, "the great hall: the west gallery (up the stair)"); S("G05", 0, 6, 82, "the great hall: the north gallery");
            S("G06", 13, 6, 72, "the great hall: the east ledge (ride the chandeliers)"); S("G07", 13, 6, 78, "the great hall: the east ledge (ride the chandeliers)");
            // the dungeon (its own space at x 400: corridor x 396..404, z 0..56; three locked cells west, their keys about; the brazier room at the end)
            S("D01", 400, 0, 6, "the dungeon: the stair's foot"); S("D02", 400, 0, 31, "the dungeon: the corridor");
            S("D03", 392, 0, 12, "the dungeon: cell 1 (its key)"); S("D04", 392, 0, 14.5f, "the dungeon: cell 1 (its key)");
            S("D05", 392, 0, 26, "the dungeon: cell 2 (its key)"); S("D06", 392, 0, 28.5f, "the dungeon: cell 2 (its key)");
            S("D07", 392, 0, 40, "the dungeon: cell 3 (its key)"); S("D08", 392, 0, 42.5f, "the dungeon: cell 3 (its key)");
            S("D09", 398, 0, 54, "the dungeon: the brazier room (dragon Kira lights the three braziers)", "Kira", Abilities.FirePuff);
            S("D10", 402, 0, 54, "the dungeon: the brazier room (dragon Kira lights the three braziers)", "Kira", Abilities.FirePuff);
            // the moat (water at -1.2, its floor at -5) and its banks
            S("M01", -42, -5, 40, "the moat's floor, west (dive)"); S("M02", 42, -5, 20, "the moat's floor, east (dive)"); S("M03", 20, 0, -24, "the moat's outer bank, south"); S("M04", -20, 0, 108, "the moat's outer bank, north");
            // the tournament field (south-east): the lists, the stands, tents, the test of strength
            S("T01", 60, 0, -50, "the tournament field: the way in"); S("T02", 100, 0, -72, "the tournament field: the lists"); S("T03", 112, 0, -95, "the tournament field: by the tilt");
            S("T04", 80, 1, -63.2f, "the stands, the first tier"); S("T05", 90, 3, -60.3f, "the stands, the top tier");
            S("T06", 122, 0, -110, "behind the hay bales (dragon Kira burns them)", "Kira", Abilities.FirePuff);
            S("T07", 95, -.9f, -116, "the prize chest (light the three braziers on the stands: dragon Kira)", "Kira", Abilities.FirePuff);
            // the village and market (south-west)
            S("V01", -40, 0, -70, "the village gate"); S("V02", -70, 0, -60, "the village"); S("V03", -85, 0, -85, "the market square");
            S("V04", -95, 2.6f, -90, "on a market stall's awning (up the crates)"); S("V05", -115, 0, -100, "a back street"); S("V06", -60, 0, -124, "behind the brambles (dragon Kira burns them)", "Kira", Abilities.FirePuff);
            S("V07", -128, 0, -60, "the village's far end");
            // the places later moves open (visible now)
            S("B01", -25, 0, 8, "the walled yard behind the boulder (Muscle Man)", Abilities.MuscleMan); S("B02", -27, 0, 5, "the walled yard behind the boulder (Muscle Man)", Abilities.MuscleMan);
            S("A01", 20, 0, 4, "the armory behind the portcullis (Muscle Man)", Abilities.MuscleMan); S("A02", 24, 0, 4, "the armory behind the portcullis (Muscle Man)", Abilities.MuscleMan);
            S("K01", -26, 0, 55, "the store room behind the cracked wall (charged punch, learned here)", Abilities.ChargedPunch); S("K02", -26, 0, 58, "the store room behind the cracked wall (charged punch, learned here)", Abilities.ChargedPunch);
            S("J01", -99, 9, -125.7f, "the ledge at the top of the chimney between two village houses (wall jump)", Abilities.WallJump);
            S("X01", 86.3f, 5.6f, -58.5f, "the stands' flag platform (double jump)", Abilities.DoubleJump);
            S("DD1", -42, -16, 96, "the moat's deep pit (deep dive)", Abilities.DeepDive); S("DD2", -40, -16, 98, "the moat's deep pit (deep dive)", Abilities.DeepDive);
            void G(string id, string kind, string needs, Vector3 at, string note, params string[] opens) => w.gates.Add(new GateData { id = id, kind = kind, needs = needs, position = at, note = note, opens = opens });
            G("ME-boulder-yard", "boulder", Abilities.MuscleMan, new Vector3(-18.5f, 0, 7.5f), "the walled yard in the courtyard (Muscle Man: he joins at the stone)", "B01", "B02");
            G("ME-portcullis-armory", "portcullis", Abilities.MuscleMan, new Vector3(22, 0, 8), "the armory's portcullis (Muscle Man)", "A01", "A02");
            G("ME-crack-store", "cracked wall", Abilities.ChargedPunch, new Vector3(-22, 0, 56), "the store room's cracked wall (charged punch, learned here)", "K01", "K02");
            G("ME-chimney-village", "wall-jump chimney", Abilities.WallJump, new Vector3(-99, 0, -124), "a chimney between two village houses (wall jump, Steampunk)", "J01");
            G("ME-ledge-stands", "double-jump ledge", Abilities.DoubleJump, new Vector3(85, 5.6f, -58.5f), "the stands' flag platform (double jump; or a boost from Muscle Man)", "X01");
            G("ME-deep-moat", "deep water", Abilities.DeepDive, new Vector3(-42, -10, 96), "the moat's deep pit (deep dive, Pirate Seas)", "DD1", "DD2");
            w.lowSpots.AddRange(LowSpotDefaults(w.worldId));
            return w;
        }

        // Round 10: the Pirate Seas (World/Pirate.cs builds it; the ground, the ship and the props in Tools/Blender/pirate_world.py, the same
        // numbers). One area, "PS", two parts: the ship and the island (S/Q/F/P/T/H/C/L/J/I/V/K: 30) and under the sea (U: 30, only after the
        // green shard goes overboard: they need Abilities.Overboard, which rule 2 never counts on arrival). Fix 8.1's rule: about 75 % of every
        // shard up to here (5 x 60 = 300): 225, and 20 from here (the ship and the island hold 30). Rule 2: the first visit has the three
        // heroes, otter Kira (swim, belly-slide) and Muscle Man (he joined at the stone); not the deep dive (the ship's fiddler teaches it).
        public static WorldData PirateDefaults()
        {
            var w = CreateInstance<WorldData>(); w.name = Pirate; w.worldId = Pirate; w.displayName = "The Pirate Seas"; w.heightsAreHints = true;
            w.violinistInstrument = "Fiddle"; w.violinistPlayClip = "violinist_play";   // item 8: the ship's fiddler, his fiddle (the Ice Age's own clip)
            w.areas.Add(new AreaData { id = "PS", name = "The Pirate Seas", requirement = 125, fromHere = 15, firstVisit = new[] { "Dan", "Abi", "Kira", Abilities.OtterSwim, Abilities.BellySlide, Abilities.MuscleMan },
                storyShard = new Vector3(-45, -11f, 10f), storyShardColor = new Color(.2f, .95f, .45f), spawn = new Vector3(0, 3.6f, -4f), spawnYaw = 0,
                kiraForm = KiraForm.Otter, outfit = Outfits.Pirate, box = new Bounds(new Vector3(0, -20, 25), new Vector3(420, 140, 380)) });
            void S(string id, float x, float y, float z, string note, params string[] req) => w.shards.Add(new ShardSpot { id = id, area = "PS", position = new Vector3(x, y, z), requires = req, note = note });
            string O = Abilities.Overboard;
            // the ship: the main deck (3.5), the quarterdeck (6), the forecastle (6), the plank, the tops (rope nets; the fore top: Muscle Man's boost)
            S("S01", 0, 3.5f, -9, "the main deck, aft of the mainmast"); S("S02", -3.5f, 3.5f, 9, "the main deck, by the foremast"); S("S03", 1.6f, 3.5f, 13.6f, "the main deck, by the foremast's foot");
            S("Q01", -2.5f, 6, -19, "the quarterdeck, by the wheel"); S("Q02", 2.5f, 6, -16, "the quarterdeck");
            S("F01", 0, 6, 17.5f, "the forecastle"); S("F02", -1.6f, 6, 21.5f, "the forecastle's bow");
            S("P01", -9.2f, 3.6f, 4, "the end of the plank (walk it!)");
            S("T01", 1.2f, 13, .8f, "the main top (climb the rope net)"); S("T02", 0, 21, 0, "the crow's nest (the net up the mainmast)");
            S("T03", .5f, 11.8f, 12.9f, "the fore top (Muscle Man boosts Dan or Abi)", Abilities.MuscleMan);
            // the hold (floor 0.6): open; its back room behind the crates (Muscle Man); under the low cargo rack (otter Kira slides under)
            S("H01", 2, .6f, 4, "the hold"); S("H02", -3, .6f, 10, "the hold, by the barrels"); S("H03", 3, .6f, -2.5f, "the hold, by the mainmast's foot");
            S("H04", -2, .6f, -9, "the hold's back room (Muscle Man lifts the crates)", Abilities.MuscleMan); S("H05", 2, .6f, -10, "the hold's back room (Muscle Man lifts the crates)", Abilities.MuscleMan);
            S("K01", -4.2f, .6f, 1, "the cargo nook behind the crates (only otter Kira fits through the gap at the bottom)", "Kira");
            // the captain's cabin (behind the cannon: Muscle Man), the bosun's locker (behind the anchor: Muscle Man)
            S("C01", -2, 3.5f, -18, "the captain's cabin (Muscle Man moves the cannon)", Abilities.MuscleMan); S("C02", 2, 3.5f, -16, "the captain's cabin (Muscle Man moves the cannon)", Abilities.MuscleMan);
            S("L01", 1.2f, 3.5f, 19, "the bosun's locker (Muscle Man lifts the anchor)", Abilities.MuscleMan);
            // the jetty and the island (beaches, the jungle path, the hilltop), the cove (shallow water)
            S("J01", 22, 1.2f, -2, "the jetty");
            S("I01", 36, 12, 10, "the island's west beach"); S("I02", 50, 12, -38, "the south beach"); S("I03", 62, 12, 32, "the jungle path");
            S("I04", 80, 12, 12, "the island's hilltop"); S("I05", 102, 12, -12, "the east side"); S("I06", 112, 12, 34, "the east beach"); S("I07", 70, 12, -46, "the south point");
            S("V01", 82, -2.4f, 64, "the cove's floor (shallow)"); S("V02", 82, -2.4f, 84, "the cove's channel");
            // under the sea (after the shard goes overboard): round the ship, the reef, the green shard's clearing, the trench and the wreck, the
            // cave and its grotto, the island's slopes, under the jetty, the kelp. Deeper than 16 m (the trench, the cave): otter Kira or the deep dive
            S("U01", -10, -14, -5, "the sea floor under the ship", O); S("U02", 8, -14, 20, "the sea floor by the bow", O); S("U03", -5, -14, 30, "the sea floor ahead of the ship", O);
            S("U04", -35, -10, -20, "the reef", O); S("U05", -50, -10, -35, "the reef", O); S("U06", -60, -10, 0, "the reef", O); S("U07", -75, -10, 30, "the reef", O);
            S("U08", -90, -10, -10, "the reef's far side", O); S("U09", -100, -10, 45, "the reef's far side", O); S("U10", -40, -10, 40, "the reef", O); S("U11", -65, -10, 55, "the reef", O);
            S("U12", -85, -10, -50, "the reef's south end", O); S("U13", -41, -11, 14, "the green shard's clearing", O); S("U14", -49, -11, 6, "the green shard's clearing", O);
            S("U15", -60, -21, 108, "the wreck's trench (deep)", O, "Kira"); S("U16", -80, -21, 128, "the wreck's trench (deep)", O, "Kira");
            S("U17", -70, -20, 118, "inside the shipwreck (deep)", O, "Kira"); S("U18", -68.6f, -18, 120.1f, "inside the shipwreck (deep)", O, "Kira");
            S("U19", -135, -18, -10, "the sea cave (deep)", O, "Kira"); S("U20", -130, -18, 10, "the sea cave (deep)", O, "Kira"); S("U21", -140, -18, 0, "the sea cave (deep)", O, "Kira");
            S("U22", -136, -18, 24, "the cave's grotto (otter Kira squeezes through)", O, "Kira"); S("U23", -131, -18, 25.5f, "the cave's grotto (otter Kira squeezes through)", O, "Kira");
            S("U24", 30, -6, 30, "the island's underwater slope", O); S("U25", 40, -8, -40, "the island's underwater slope", O); S("U26", 15, -10, -30, "the sea floor south of the ship", O);
            S("U27", -115, -18, -30, "the cave's basin (deep)", O, "Kira"); S("U28", -118, -18, 35, "the cave's basin (deep)", O, "Kira");
            S("U29", 18, -6, -2, "under the jetty", O); S("U30", 0, -14, 60, "the kelp forest north of the ship", O);
            void G(string id, string kind, string needs, Vector3 at, string note, params string[] opens) => w.gates.Add(new GateData { id = id, kind = kind, needs = needs, position = at, note = note, opens = opens });
            G("PS-crates-hold", "crate stack", Abilities.MuscleMan, new Vector3(0, .6f, -6), "the hold's back room (Muscle Man lifts the crates)", "H04", "H05");
            G("PS-cannon-cabin", "cannon", Abilities.MuscleMan, new Vector3(0, 3.5f, -12.2f), "the captain's cabin door (Muscle Man moves the cannon)", "C01", "C02", "PS-H1");
            G("PS-anchor-locker", "anchor", Abilities.MuscleMan, new Vector3(0, 3.5f, 14.2f), "the bosun's locker door (Muscle Man lifts the anchor)", "L01");
            G("PS-boost-foretop", "boost", Abilities.MuscleMan, new Vector3(0, 3.5f, 7.6f), "the fore top (Muscle Man's boost)", "T03");
            G("PS-deep", "deep water", Abilities.DeepDive, new Vector3(-70, -18, 110), "the trench, the wreck and the cave (deeper than 16 m: otter Kira any time, everyone with the deep dive)", "U15", "U16", "U17", "U18", "U19", "U20", "U21", "U27", "U28", "PS-H2");
            w.lowSpots.AddRange(LowSpotDefaults(w.worldId));
            return w;
        }

        // Round 11: Victorian Steampunk (World/Steampunk.cs builds it; the props in Tools/Blender/steampunk_world.py). One area, "SP", two
        // parts on flat ground at 0: the factory at night (the hall x -36..36, z 0..96: the belt run on the conveyor x -2..2 (top 3.0), the
        // press at z 84, the catwalk ring at 7, F: 30) and the streets of steampunk London by day (the yard z 96..112 through the hole the
        // press smashes, the avenue z 112..176, the square with the great clock at (0, 200), the east and west streets, S: 30). Fix 10.1's
        // rule: the Pirate Seas' 125 + 25 = 150, and 15 from here. Rule 2: the first visit has the three heroes, mechanical Kira (wind-up,
        // magnet paws) and Muscle Man; the press scene comes before any play (PressDone, counted); not the wall jump (Abi learns it here).
        public static WorldData SteampunkDefaults()
        {
            var w = CreateInstance<WorldData>(); w.name = Steampunk; w.worldId = Steampunk; w.displayName = "Victorian Steampunk"; w.heightsAreHints = true;
            w.violinistInstrument = "SteamViolin"; w.violinistPlayClip = "violinist_play";   // item 7: a street musician with his steampunk violin (the fiddle's clip)
            w.areas.Add(new AreaData { id = "SP", name = "Victorian Steampunk", requirement = 150, fromHere = 15, firstVisit = new[] { "Dan", "Abi", "Kira", Abilities.WindUp, Abilities.MagnetPaws, Abilities.MuscleMan, Abilities.PressDone },
                storyShard = new Vector3(0, 5.85f, 199.0f), storyShardColor = new Color(.15f, .9f, .85f), spawn = new Vector3(0, 3.1f, 10f), spawnYaw = 0,
                kiraForm = KiraForm.Mechanical, outfit = Outfits.Steampunk, box = new Bounds(new Vector3(0, 30, 150), new Vector3(240, 140, 340)) });
            void S(string id, float x, float y, float z, string note, params string[] req) => w.shards.Add(new ShardSpot { id = id, area = "SP", position = new Vector3(x, y, z), requires = req, note = note });
            string K = "Kira", WU = Abilities.WindUp, MP = Abilities.MagnetPaws, MM = Abilities.MuscleMan, WJ = Abilities.WallJump;
            // the factory (night): its floor, the stopped belt, the catwalk ring (7), the machines mechanical Kira winds up, Muscle Man's jobs
            S("F01", -14, 0, 14, "the factory floor, by the boiler"); S("F02", -14, 0, 28, "the factory floor, past the boiler"); S("F03", 14, 0, 30, "the factory floor, by the crates");
            S("F04", 12, 0, 44, "the factory floor"); S("F05", -12, 0, 50, "the factory floor, by the barrels"); S("F06", 12, 0, 62, "the factory floor");
            S("F07", -12, 0, 82, "the factory floor, north end"); S("F08", 14, 0, 88, "the factory floor, by the control panels"); S("F09", 0, 0, 92, "by the hole the press made");
            S("F10", -10, 0, 6, "the factory floor, south end");
            S("F11", 0, 3, 30, "on the conveyor belt (it stopped after the press)"); S("F12", 0, 3, 52, "on the conveyor belt");
            S("F13", 34.7f, 7, 40, "the east catwalk"); S("F14", 34.7f, 7, 70, "the east catwalk"); S("F15", -34.7f, 7, 40, "the west catwalk");
            S("F16", -34.7f, 7, 20, "the west catwalk"); S("F17", 0, 7, 1.2f, "the south catwalk");
            S("F18", 27.2f, 10.5f, 95.7f, "the top of the brick stack's shaft (wall jump)", WJ);
            S("F19", 20, 0, 64.5f, "the end of the stamper lane (Muscle Man holds the big stamper up)", MM);
            S("F20", -31, 0, 4, "the boiler room (mechanical Kira winds the gear door open)", K, WU); S("F21", -31, 0, 9, "the boiler room (mechanical Kira winds the gear door open)", K, WU);
            S("F22", -31, 12, 53.5f, "the crane deck (mechanical Kira winds up the lift)", K, WU); S("F23", -27.5f, 12, 53.5f, "the crane deck (mechanical Kira winds up the lift)", K, WU);
            S("F24", 18.5f, 7, 28, "the gantry (mechanical Kira lowers its catwalk bridge)", K, WU); S("F25", 21.5f, 7, 30, "the gantry (mechanical Kira lowers its catwalk bridge)", K, WU);
            S("F26", 20, 12, 72, "on the girder (mechanical Kira's magnet paws up the riveted pillar)", K, MP); S("F27", -18, 12, 64, "on the girder (mechanical Kira's magnet paws up the riveted pillar)", K, MP);
            S("F28", 31, 0, 42, "the store cage (Muscle Man bends the bars)", MM); S("F29", 33, 0, 46, "the store cage (Muscle Man bends the bars)", MM);
            S("F30", -33.5f, 0, 33, "the tool nook (Muscle Man lifts the machine aside)", MM);
            // the streets (day): the yard, the avenue, the square and its great clock, the west and east streets, the clockmaker's yard
            S("S01", 0, 0, 120, "the avenue"); S("S02", -5, 0, 135, "the avenue"); S("S03", 5, 0, 150, "the avenue"); S("S04", -4, 0, 165, "the avenue");
            S("S05", -6, 0, 104, "the factory yard");
            S("S06", -15, 0, 190, "the square"); S("S07", 15, 0, 190, "the square"); S("S08", -15, 0, 212, "the square"); S("S09", 15, 0, 212, "the square");
            S("S10", -14, 14, 143.4f, "a tall house's roof (wall jump up the narrow alley)", WJ);
            S("S11", 0, 0, 250, "the north street"); S("S12", -70, 0, 196, "the west street"); S("S13", -92, 0, 196, "the west street's end");
            S("S14", 50, 0, 200, "the east street"); S("S15", 58, 0, 206, "the east street, by the gear gate");
            S("S16", 82, 0, 192, "the clockmaker's yard (mechanical Kira winds the gear gate open)", K, WU); S("S17", 90, 0, 200, "the clockmaker's yard (mechanical Kira winds the gear gate open)", K, WU);
            S("S18", 82, 0, 208, "the clockmaker's yard (mechanical Kira winds the gear gate open)", K, WU);
            S("S19", -80, 12, 204.75f, "the signal tower's girder (magnet paws)", K, MP); S("S20", 34, 12, 216.75f, "the lamp tower's girder (magnet paws)", K, MP);
            S("S21", 13, 0, 143.5f, "the alley behind the pipes (Muscle Man bends them)", MM); S("S22", 16.5f, 0, 143.5f, "the alley behind the pipes (Muscle Man bends them)", MM);
            S("S23", -28.5f, 0, 236, "the passage behind the crates (Muscle Man lifts them)", MM); S("S24", -28.5f, 0, 239, "the passage behind the crates (Muscle Man lifts them)", MM);
            S("S25", 44, 9, 183, "a cream house's roof (Muscle Man boosts Dan or Abi)", MM);
            S("S26", -30, 0, 194, "the square, by the clockwork horse"); S("S27", 30, 0, 219, "the square's north-east corner"); S("S28", -30, 0, 219, "the square's north-west corner");
            S("S29", 0, 0, 280, "the north street's end"); S("S30", -60, .3f, 200, "on the carriage turntable");
            S("S31", 0, 3.6f, 277.5f, "the far lamp platform over the north street (Abi's air dash, Tomorrowland)", Abilities.AirDash);   // Round 12, item 7: an earlier world's air-dash gap
            void G(string id, string kind, string needs, Vector3 at, string note, params string[] opens) => w.gates.Add(new GateData { id = id, kind = kind, needs = needs, position = at, note = note, opens = opens });
            G("SP-geardoor-boiler", "gear door", Abilities.WindUp, new Vector3(-24, 0, 6), "the boiler room's gear door (mechanical Kira winds it)", "F20", "F21", "SP-H1");
            G("SP-lift", "wind-up lift", Abilities.WindUp, new Vector3(-28, 0, 44), "the lift to the crane deck (mechanical Kira winds it)", "F22", "F23");
            G("SP-bridge", "wind-up bridge", Abilities.WindUp, new Vector3(34.7f, 7, 30), "the gantry's catwalk bridge (mechanical Kira winds it down)", "F24", "F25");
            G("SP-geargate", "gear gate", Abilities.WindUp, new Vector3(64, 0, 195), "the great gear gate (mechanical Kira winds it)", "S16", "S17", "S18", "SP-H2");
            G("SP-magnet", "magnet pillars", Abilities.MagnetPaws, new Vector3(20, 0, 71.25f), "riveted iron pillars (mechanical Kira's magnet paws)", "F26", "F27", "S19", "S20");
            G("SP-bars-cage", "iron bars", Abilities.MuscleMan, new Vector3(28, 0, 44), "the store cage's bars (Muscle Man bends them)", "F28", "F29");
            G("SP-crank-nook", "machine", Abilities.MuscleMan, new Vector3(-30, 0, 33), "the tool nook's machine (Muscle Man lifts it)", "F30");
            G("SP-stamper-lane", "big stamper", Abilities.MuscleMan, new Vector3(20, 0, 62), "the big stamper at the lane's end (Muscle Man holds it up)", "F19");
            G("SP-pipes-alley", "pipe fence", Abilities.MuscleMan, new Vector3(10, 0, 143.5f), "the alley's pipes (Muscle Man bends them)", "S21", "S22");
            G("SP-crates-passage", "crate stack", Abilities.MuscleMan, new Vector3(-28.5f, 0, 232), "the passage's crates (Muscle Man lifts them)", "S23", "S24");
            G("SP-boost-roof", "boost", Abilities.MuscleMan, new Vector3(36.5f, 0, 183), "a cream house's roof (Muscle Man's boost)", "S25");
            G("SP-chimney-stack", "wall-jump chimney", Abilities.WallJump, new Vector3(27.2f, 0, 93), "the brick stack's shaft (wall jump, learned here)", "F18");
            G("SP-alley-west", "wall-jump alley", Abilities.WallJump, new Vector3(-14, 0, 139.2f), "the narrow alley between the tall houses (wall jump, learned here)", "S10");
            G("SP-dash-gap", "air-dash gap", Abilities.AirDash, new Vector3(0, 3.6f, 262), "two lamp platforms over the north street, 12.5 m apart (Abi's air dash, Tomorrowland)", "S31");
            w.lowSpots.AddRange(LowSpotDefaults(w.worldId));
            return w;
        }

        // Round 12: Tomorrowland (World/Tomorrowland.cs builds it in code on flat ground at 0): the neon avenue (z 0..150, shop roofs at 7), the
        // hover-car lane (z 154), the square (z 160..230: the garden dome, the Atomic Diner, the air-dash pads, the Violinist), the towers (the
        // Ring Tower's sky deck at 16 by the always-running car; the Orbit Tower's roof at 24 by the car robot Kira calls; the Skyline Tower's
        // roof at 46: the pink story shard, its lift lit by the necklace) and the Rocket Terminal (the golden launch hall, z 260..320).
        // Fix 10.1's rule: Steampunk's 150 + 25 = 175, and 15 from here. Rule 2: the first visit has the three heroes, robot Kira (plug-in,
        // wheel dash) and Muscle Man; not the air dash (Abi learns it here) nor earlier learned moves.
        public static WorldData TomorrowlandDefaults()
        {
            var w = CreateInstance<WorldData>(); w.name = Tomorrowland; w.worldId = Tomorrowland; w.displayName = "Tomorrowland"; w.heightsAreHints = true;
            w.violinistInstrument = "ChromeViolin"; w.violinistPlayClip = "violinist_play";   // item 7: a lounge musician with a chrome electric violin
            w.areas.Add(new AreaData { id = "TL", name = "Tomorrowland", requirement = 175, fromHere = 15, firstVisit = new[] { "Dan", "Abi", "Kira", Abilities.PlugIn, Abilities.WheelDash, Abilities.MuscleMan },
                storyShard = new Vector3(-70, 47f, 200), storyShardColor = new Color(1f, .35f, .82f), spawn = new Vector3(0, .1f, 10f), spawnYaw = 0,
                kiraForm = KiraForm.Robot, outfit = Outfits.Tomorrowland, box = new Bounds(new Vector3(0, 40, 158), new Vector3(264, 160, 360)) });
            void S(string id, float x, float y, float z, string note, params string[] req) => w.shards.Add(new ShardSpot { id = id, area = "TL", position = new Vector3(x, y, z), requires = req, note = note });
            string K = "Kira", PI = Abilities.PlugIn, WD = Abilities.WheelDash, MM = Abilities.MuscleMan;
            // the neon avenue (street and walkways)
            S("A01", 0, 0, 14, "the avenue"); S("A02", -4, 0, 26, "the avenue"); S("A03", 4, 0, 40, "the avenue"); S("A04", -2, 0, 60, "the avenue");
            S("A05", 3, 0, 74, "the avenue"); S("A06", -4, 0, 100, "the avenue"); S("A07", 4, 0, 108, "the avenue"); S("A08", 0, 0, 140, "the avenue's north end");
            S("A09", -11.25f, .2f, 40, "the malfunctioning walkway"); S("A10", 11.25f, .2f, 60, "the east walkway"); S("A11", 11.25f, .2f, 130, "the east walkway");
            S("A12", -11.25f, .2f, 145, "the malfunctioning walkway's end");
            // the rooftops
            S("R01", 19, 7, 80, "the east roofs (robot Kira turns the escalator round)", K, PI); S("R02", 19, 7, 95, "the east roofs (robot Kira turns the escalator round)", K, PI);
            S("R03", 19, 7, 112, "the east roofs (robot Kira turns the escalator round)", K, PI); S("R04", 19, 7, 128, "the east roofs (robot Kira turns the escalator round)", K, PI);
            S("R05", 23.5f, 7, 87, "the escalator's top (robot Kira turns it round)", K, PI);
            S("R06", -19, 7, 98, "the west roofs (Muscle Man's boost)", MM); S("R07", -19, 7, 111, "the west roofs (Muscle Man's boost)", MM); S("R08", -19, 7, 127, "the west roofs (Muscle Man's boost)", MM);
            S("R09", -19, 12, 56, "a tall shop's roof (wall jump up the narrow gap)", Abilities.WallJump);
            // Muscle Man's jobs
            S("P01", -5.5f, -2.6f, 50, "the maintenance pit (Muscle Man lifts the parked car off its hatch)", MM); S("P02", -2.5f, -2.6f, 50, "the maintenance pit (Muscle Man lifts the parked car off its hatch)", MM);
            S("V01", -30, 0, 112, "the courtyard behind the vending machine (Muscle Man pushes it aside)", MM); S("V02", -30, 0, 124, "the courtyard behind the vending machine (Muscle Man pushes it aside)", MM);
            S("B01", 27, 0, 103.6f, "the nook behind the chrome bars (Muscle Man bends them)", MM); S("B02", 27, 0, 105.4f, "the nook behind the chrome bars (Muscle Man bends them)", MM);
            // the billboard over the street (robot Kira's boost ramp)
            S("K01", -2, 8.5f, 128.5f, "the billboard over the street (robot Kira's wheel dash up the boost ramp)", K, WD); S("K02", 0, 8.5f, 128.5f, "the billboard (boost ramp)", K, WD);
            S("K03", 2, 8.5f, 128.5f, "the billboard (boost ramp)", K, WD);
            // the square, the garden dome, the Atomic Diner, the air-dash pads
            S("Q01", -12, 0, 172, "the square"); S("Q02", 12, 0, 180, "the square"); S("Q03", -14, 0, 210, "the square"); S("Q04", 14, 0, 218, "the square");
            S("Q05", 0, 0, 226, "the square's north side"); S("Q06", 20, 0, 198, "the square, east");
            S("G01", -32, 0, 186, "the garden dome"); S("G02", -29, 0, 191, "the garden dome"); S("G03", -35, 0, 182, "the garden dome");
            S("D01", 32, 0, 182, "the Atomic Diner (robot Kira opens its door)", K, PI); S("D02", 32, 0, 190, "the Atomic Diner (robot Kira opens its door)", K, PI); S("D03", 39, 0, 191, "the Atomic Diner (robot Kira opens its door)", K, PI);
            S("X01", -51.2f, 4, 211.4f, "the far air-dash pad (Abi's air dash, learned here)", Abilities.AirDash); S("X02", -52.3f, 4, 212.6f, "the far air-dash pad (Abi's air dash)", Abilities.AirDash);
            // the towers
            S("Y01", -40, 16, 114, "the Ring Tower's sky deck (the flying car from the square)"); S("Y02", -40, 16, 126, "the Ring Tower's sky deck"); S("Y03", -46, 16, 120, "the Ring Tower's sky deck"); S("Y04", -36, 16, 120, "the Ring Tower's sky deck");
            S("O01", 60, 24, 196, "the Orbit Tower's roof (robot Kira calls its car)", K, PI); S("O02", 60, 24, 204, "the Orbit Tower's roof (robot Kira calls its car)", K, PI); S("O03", 64, 24, 200, "the Orbit Tower's roof (robot Kira calls its car)", K, PI);
            // the Rocket Terminal
            S("T01", 0, 0, 252, "the terminal's passage"); S("T02", -10, 0, 270, "the launch hall"); S("T03", 10, 0, 270, "the launch hall"); S("T04", 0, 0, 316, "the launch hall, behind the rocket");
            S("T05", -13, 6, 294, "the west gantry (up its ramp)"); S("T06", -13, 6, 298, "the west gantry (up its ramp)");
            S("T07", 13, 18, 294, "the east gantry's top (robot Kira starts its service lift)", K, PI); S("T08", 13, 18, 298, "the east gantry's top (robot Kira starts its service lift)", K, PI);
            S("T09", 13, 12, 296, "the east gantry's middle deck (its service lift)", K, PI);
            void G(string id, string kind, string needs, Vector3 at, string note, params string[] opens) => w.gates.Add(new GateData { id = id, kind = kind, needs = needs, position = at, note = note, opens = opens });
            G("TL-escalator", "escalator", PI, new Vector3(12.8f, 0, 92.5f), "the escalator to the east roofs (robot Kira turns it round)", "R01", "R02", "R03", "R04", "R05");
            G("TL-door-diner", "terminal door", PI, new Vector3(28, 0, 186), "the Atomic Diner's door (robot Kira)", "D01", "D02", "D03", "TL-H1");
            G("TL-call-orbit", "flying car", PI, new Vector3(40, 0, 170), "the Orbit car (robot Kira calls it)", "O01", "O02", "O03", "TL-H2");
            G("TL-gantry-lift", "service lift", PI, new Vector3(17.2f, 0, 290.5f), "the east gantry's service lift (robot Kira)", "T07", "T08", "T09");
            G("TL-ramp", "boost ramp", WD, new Vector3(0, 0, 116), "the boost ramp to the billboard (robot Kira's wheel dash)", "K01", "K02", "K03");
            G("TL-car-hatch", "parked car", MM, new Vector3(-4, 0, 48), "the parked car on the hatch (Muscle Man lifts it)", "P01", "P02");
            G("TL-vending", "vending machine", MM, new Vector3(-15.2f, 0, 117.75f), "the vending machine in the alley (Muscle Man)", "V01", "V02");
            G("TL-bars-nook", "chrome bars", MM, new Vector3(14.6f, 0, 104.5f), "the chrome bars (Muscle Man bends them)", "B01", "B02");
            G("TL-boost-roofs", "boost", MM, new Vector3(-11.6f, 0, 98), "the west roofs (Muscle Man's boost)", "R06", "R07", "R08");
            G("TL-shaft", "wall-jump gap", Abilities.WallJump, new Vector3(-19, 0, 63.2f), "the narrow gap between the tall shops (wall jump, Steampunk)", "R09");
            G("TL-dash", "air-dash gap", Abilities.AirDash, new Vector3(-44, 4, 212), "the far pad 12.5 m off (Abi's air dash, learned here)", "X01", "X02");
            w.lowSpots.AddRange(LowSpotDefaults(w.worldId));
            return w;
        }

        // Round 13: the Moon (World/Moon.cs builds it in code: a cratered heightfield under low gravity). The landing site and the landed
        // rocket (0, 24), the crater field east (bouncy rims, the deep crater (100, 20), cave A behind a moon boulder, the float pillars), the
        // moon base north-west (the airlock Muscle Man opens; hub, garden, lab), cave B (-20, 100), the mesa (30, -90: Muscle Man's boost), the
        // ridge up to the rover's plateau (-60, -90: 16 m): the white story shard on the rover (the light puzzle: only Abi). Fix 10.1's rule:
        // Tomorrowland's 175 + 25 = 200, and 15 from here. Rule 2: the first visit has the three heroes, space Kira (float), Muscle Man and the
        // rocket (it is how you get here).
        public static WorldData MoonDefaults()
        {
            var w = CreateInstance<WorldData>(); w.name = Moon; w.worldId = Moon; w.displayName = "The Moon"; w.heightsAreHints = true;
            w.violinistInstrument = "ChromeViolin"; w.violinistPlayClip = "violinist_play";
            w.areas.Add(new AreaData { id = "MO", name = "The Moon", requirement = 200, fromHere = 15, firstVisit = new[] { "Dan", "Abi", "Kira", Abilities.Float, Abilities.MuscleMan, Abilities.Rocket },
                storyShard = new Vector3(-60, 19.4f, -90), storyShardColor = new Color(.95f, .97f, 1f), spawn = new Vector3(0, .2f, 8f), spawnYaw = 0,
                kiraForm = KiraForm.Space, outfit = Outfits.Moon, box = new Bounds(new Vector3(0, 60, 0), new Vector3(620, 260, 620)) });
            void S(string id, float x, float y, float z, string note, params string[] req) => w.shards.Add(new ShardSpot { id = id, area = "MO", position = new Vector3(x, y, z), requires = req, note = note });
            string K = "Kira", FL = Abilities.Float, MM = Abilities.MuscleMan;
            // the landing site
            S("L01", 6, 0, 0, "the landing site"); S("L02", -6, 0, 4, "the landing site"); S("L03", 12, 0, 16, "by the rocket"); S("L04", -12, 0, 20, "by the rocket");
            S("L05", 8, 0, 38, "behind the rocket"); S("L06", -8, 0, 38, "behind the rocket"); S("L07", 20, 0, -6, "the landing site"); S("L08", -20, 0, 0, "the landing site");
            S("L09", 0, 0, -14, "the landing site, south"); S("L10", 16, 0, 46, "the landing site, north");
            // the crater field
            S("C01", 40, 0, 0, "the crater field"); S("C02", 50, 0, 15, "the crater field"); S("C03", 60, 0, -30, "in a small crater"); S("C04", 80, 0, 60, "in a crater");
            S("C05", 70, 0, 40, "the crater field"); S("C06", 95, 0, 48, "the crater field"); S("C07", 118, 0, 42, "the crater field"); S("C08", 130, 0, -20, "in a small crater");
            S("C09", 140, 0, 50, "in a small crater"); S("C10", 120, 0, -50, "the crater field, south"); S("C11", 55, 0, 30, "in a little crater"); S("C12", 160, 0, 20, "the crater field's far side");
            // the deep crater
            S("D01", 100, 0, 20, "the deep crater's floor"); S("D02", 94, 0, 14, "the deep crater"); S("D03", 106, 0, 26, "the deep crater"); S("D04", 100, 0, 10, "the deep crater");
            S("D05", 92, 0, 25, "the deep crater"); S("D06", 108, 0, 15, "the deep crater");
            // the float pillars (space Kira)
            S("F01", 108.85f, 7.6f, 70, "a float pillar's top (space Kira floats up)", K, FL); S("F02", 111.15f, 7.6f, 70, "a float pillar's top (space Kira)", K, FL);
            S("F03", 148.85f, 7.6f, 10, "a float pillar's top (space Kira)", K, FL); S("F04", 151.15f, 7.6f, 10, "a float pillar's top (space Kira)", K, FL);
            S("F05", 68.85f, 7.6f, -70, "a float pillar's top (space Kira)", K, FL); S("F06", 71.15f, 7.6f, -70, "a float pillar's top (space Kira)", K, FL);
            // the caves behind the moon boulders, the mesa
            S("B01", 150.5f, 0, -58, "cave A behind its moon boulder (Muscle Man)", MM); S("B02", 151, 0, -62, "cave A (Muscle Man)", MM);
            S("B03", -20, 0, 98.5f, "cave B behind its moon boulder (Muscle Man)", MM); S("B04", -18.5f, 0, 100.5f, "cave B (Muscle Man)", MM);
            S("M01", 28, 10, -92, "the mesa's top (Muscle Man's boost)", MM); S("M02", 31, 10, -89, "the mesa's top (Muscle Man's boost)", MM); S("M03", 30, 10, -95, "the mesa's top (Muscle Man's boost)", MM);
            // the moon base (Muscle Man opens its airlock)
            S("K01", -94, .3f, 76, "the base's hub (Muscle Man opens the airlock)", MM); S("K02", -86, .3f, 84, "the base's hub", MM); S("K03", -90, .3f, 72, "the base's hub", MM); S("K04", -96, .3f, 86, "the base's hub", MM);
            S("K05", -90, .3f, 110, "the garden dome", MM); S("K06", -86, .3f, 119, "the garden dome", MM); S("K07", -94, .3f, 119, "the garden dome", MM); S("K08", -90, .3f, 122, "the garden dome", MM);
            S("K09", -60, .3f, 76, "the lab dome", MM); S("K10", -56, .3f, 80, "the lab dome", MM); S("K11", -64, .3f, 78, "the lab dome", MM); S("K12", -73, .3f, 80, "the corridor to the lab", MM);
            // the ridge up to the rover
            S("R01", -30, 0, -58, "the ridge's foot"); S("R02", -36, 0, -64, "up the ridge"); S("R03", -42, 0, -70, "up the ridge"); S("R04", -48, 0, -76, "up the ridge");
            S("R05", -56, 16, -96, "the rover's plateau"); S("R06", -67, 16, -90, "the rover's plateau"); S("R07", -60, 16, -100, "the rover's plateau");
            void G(string id, string kind, string needs, Vector3 at, string note, params string[] opens) => w.gates.Add(new GateData { id = id, kind = kind, needs = needs, position = at, note = note, opens = opens });
            G("MO-airlock", "airlock door", MM, new Vector3(-90, 0, 57), "the moon base's jammed airlock door (Muscle Man)", "K01", "K02", "K03", "K04", "K05", "K06", "K07", "K08", "K09", "K10", "K11", "K12", "MO-H1");
            G("MO-boulder-a", "moon boulder", MM, new Vector3(144.4f, 0, -60), "cave A's moon boulder (Muscle Man)", "B01", "B02");
            G("MO-boulder-b", "moon boulder", MM, new Vector3(-20, 0, 105.6f), "cave B's moon boulder (Muscle Man)", "B03", "B04");
            G("MO-boost-mesa", "boost", MM, new Vector3(30, 0, -78), "the mesa (Muscle Man's boost)", "M01", "M02", "M03", "MO-H2");
            G("MO-pillars", "float pillars", FL, new Vector3(110, 0, 70), "the float pillars (space Kira floats up)", "F01", "F02", "F03", "F04", "F05", "F06");
            w.lowSpots.AddRange(LowSpotDefaults(w.worldId));
            return w;
        }
        static List<LowSpot> MoonLowSpots()
        {
            var l = new List<LowSpot>();
            void L(string id, string kind, Vector3 drop, Vector3 exit, float radius, string note, string needs = "", Vector3 via = default, string opens = "") =>
                l.Add(new LowSpot { id = id, kind = kind, drop = drop, via = via, exit = exit, radius = radius, note = note, needs = needs, opens = opens });
            L("MO-crater-deep", "pit", new Vector3(100, -6.5f, 20), new Vector3(100, 0, -8), 20, "the deep crater (walk up its sides)");
            L("MO-crater-a", "pit", new Vector3(60, -1.5f, -30), new Vector3(60, 0, -46), 10, "a small crater by the landing site (walk out)");
            L("MO-crater-b", "pit", new Vector3(80, -2.5f, 60), new Vector3(80, 0, 78), 12, "a crater in the field (walk out)");
            L("MO-base", "enclosed", new Vector3(-90, .6f, 80), new Vector3(-90, 0, 48), 20, "the moon base (out through the airlock)", Abilities.MuscleMan, new Vector3(-90, .3f, 63), "MO-airlock");
            L("MO-cave-a", "enclosed", new Vector3(150, .3f, -60), new Vector3(137, 0, -60), 9, "cave A (out of its mouth, west)", Abilities.MuscleMan, opens: "MO-boulder-a");
            L("MO-cave-b", "enclosed", new Vector3(-20, .3f, 100), new Vector3(-20, 0, 113), 9, "cave B (out of its mouth, north)", Abilities.MuscleMan, opens: "MO-boulder-b");
            return l;
        }

        // The graybox playground's layout (positions match World/Playground.cs; 1 unit = 1 m). Area A at the origin, Area B at x = 400.
        public static WorldData Defaults()
        {
            var w = CreateInstance<WorldData>(); w.name = Playground;
            string[] heroes = { "Dan", "Abi", "Kira" };
            w.areas.Add(new AreaData { id = "A", name = "Area A", requirement = 10, fromHere = 0, firstVisit = heroes, storyShard = new Vector3(0, 1.4f, 52f), storyShardColor = new Color(.3f, .62f, 1f), spawn = new Vector3(0, .1f, -48f), spawnYaw = 0, kiraForm = KiraForm.Home, outfit = Outfits.Home, box = new Bounds(Vector3.zero, new Vector3(400, 400, 400)) });
            // Area B: Kira arrives as the sabertooth (Round 2), so her ClawIce is there on the first visit
            w.areas.Add(new AreaData { id = "B", name = "Area B", requirement = 0, fromHere = 0, firstVisit = new[] { "Dan", "Abi", "Kira", Abilities.ClawIce }, spawn = new Vector3(400, .1f, -12f), spawnYaw = 0, kiraForm = KiraForm.Sabertooth, outfit = Outfits.IceAge, box = new Bounds(new Vector3(400, 0, 0), new Vector3(400, 400, 400)) });
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
