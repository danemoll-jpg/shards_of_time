using System.Linq;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 10, items 2-12: the Pirate Seas, world 5, two parts (almost two levels): the ship and the island at sunset (Dan's pirate.mp3)
    // and under the sea (underwater.mp3, crossfading at the surface). The ground, the ship and the props come from Tools/Blender/pirate_world.py
    // (the same numbers); colliders are the drawn meshes themselves (MeshColliders on the solid parts), the gameplay is placed here.
    //   the ship (hull z -22..24, keel on x 0, the sea at 0): the main deck 3.5 (the arrival; the treasure chest by the starboard rail; the
    //     hatch and ramp down to the hold), the quarterdeck 6 (the wheel; the ship's fiddler, who teaches everyone the deep dive), the
    //     forecastle 6; the hold (floor 0.6: barrels, a low cargo rack only otter Kira fits under, the back room behind a crate stack: Muscle
    //     Man), the captain's cabin behind a cannon (Muscle Man), the bosun's locker behind the anchor (Muscle Man); rope nets up to the main
    //     top (13.35) and on to the crow's nest (21); the fore top (11.8: Muscle Man's boost); the plank; the gangplank down to the jetty;
    //     rope ladders up the hull and the jetty out of the water
    //   the island (east, x 25..135): beaches, palms, a jungle path to the hilltop, the deckhands' camp, the shallow cove on the north side
    //   under the sea (after the green shard goes overboard; before that the sea pushes everyone back up below 3.5 m: SeaLock): the reef
    //     (west, corals, fish schools, pufferfish), the green shard's clearing (-45, 10), the trench and the sunken wreck (north-west), the
    //     sea cave (far west) and its grotto behind a gap only otter Kira fits; below 16 m only otter Kira or the deep dive (DeepWater);
    //     air bubbles from vents on the sea floor give Dan and Abi their breath back (AirVolume)
    public static class Pirate
    {
        static Transform root;
        public const string Start = "PS-start", VortexCp = "PS-vortex", SeaCp = "PS-sea", IslandCp = "PS-island";
        public const string ChestShardId = "story-PirateSeas-chest", OverboardFlag = "PS-overboard";
        public static readonly Vector3 ChestAt = new Vector3(4.9f, 3.5f, -7.5f);   // the treasure chest by the starboard rail
        public static readonly Vector3 SplashAt = new Vector3(-9f, -1.2f, -6f);       // in the water off the port side (after the overboard)
        public static readonly Vector3 ArriveAt = new Vector3(0, 3.5f, -4f);
        public static readonly Vector3 ViolinistAt = new Vector3(2.6f, 6f, -19.5f);
        public static readonly Color Green = new Color(.2f, .95f, .45f);
        public static Portal Gate { get; private set; }
        public static Pickup ChestShard { get; private set; }
        public static Transform ChestLid { get; private set; }
        public static Vector3 StoryAt { get; private set; }
        public static bool DeepOpen { get; private set; }
        public static GameObject NightLights { get; private set; }   // the lanterns' lights (on at night: the arrival; dimmer at sunset)

        // music and look: under water = the player's head has been under the surface (a short hold so a bob at the surface doesn't flip it)
        static float underFor, overFor;
        public static bool Underwater { get; private set; }
        public static void Watch()
        {
            if (!Game.InPirate || !Party.I) { Underwater = false; return; }
            bool u = Party.I.Player.Motor.HeadUnderwater;
            if (u) { underFor += Time.deltaTime; overFor = 0; } else { overFor += Time.deltaTime; underFor = 0; }
            if (!Underwater && underFor > .6f) { Underwater = true; PlayLog.Line("under the sea (music: underwater)"); }
            else if (Underwater && overFor > 1.2f) { Underwater = false; PlayLog.Line("above the water (music: pirate)"); }
        }

        public static Vector3 ArriveSpot => ArriveAt;

        public static Transform Build(WorldData w)
        {
            root = new GameObject("World Pirate Seas").transform;
            var save = SaveGame.Current; var prog = save.World(w.worldId);
            var terrain = WorldArt.SpawnFile("pirate_terrain", root);
            if (terrain) foreach (var mf in terrain.GetComponentsInChildren<MeshFilter>()) { var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; }
            else Box("Fallback sea floor", new Vector3(0, -14.5f, 25), new Vector3(400, 1, 350), "SeaSand");
            Physics.SyncTransforms();
            Sea(); Ship(prog); Island(); UnderTheSea(prog);
            foreach (var (c, s) in new[] { (new Vector3(0, 0, -152), new Vector3(420, 140, 4)), (new Vector3(0, 0, 202), new Vector3(420, 140, 4)),
                                           (new Vector3(-202, 0, 25), new Vector3(4, 140, 360)), (new Vector3(202, 0, 25), new Vector3(4, 140, 360)) })
                Box("World boundary", c, s, Color.clear, "Water").GetComponent<Renderer>().enabled = false;
            Physics.SyncTransforms();
            foreach (var s in w.shards)
                if (!prog.shards.Contains(s.id)) Pickup.Make(Pickup.Kind.Shard, s.id, s.area, ShardPos(s.position), root, new Color(.6f, 1f, .75f));
            PlayLog.Line("Pirate Seas shards placed: " + string.Join(" ", w.shards.Select(s => $"{s.id}{Fmt(ShardPos(s.position))}")));
            var a = w.Area("PS");
            // the story shard: first in the chest (the necklace leads there; collecting it plays ShardOverboard), then on the sea floor
            ChestShard = Pickup.Make(Pickup.Kind.StoryShard, ChestShardId, a.id, ChestAt + new Vector3(0, 1.0f, 0), root, a.storyShardColor);
            StoryAt = ShardPos(a.storyShard) + Vector3.up * .3f;
            var story = Pickup.Make(Pickup.Kind.StoryShard, "story-" + w.worldId, a.id, StoryAt, root, a.storyShardColor);
            Game.I.StoryShard = story;
            Game.I.StoryBeam = Box("Story beam", StoryAt + Vector3.up * 16f, new Vector3(.35f, 32f, .35f), new Color(.5f, 1f, .65f), "Glow", false).gameObject;
            OpenDeepSea(prog.opened.Contains(OverboardFlag));
            ShowStoryShard();
            if (!save.heartUpgrades.Contains("PS-H1")) Pickup.Make(Pickup.Kind.HeartUpgrade, "PS-H1", "PS", new Vector3(2.2f, 4.7f, -20.5f), root, Playground.Accent);
            if (!save.heartUpgrades.Contains("PS-H2")) Pickup.Make(Pickup.Kind.HeartUpgrade, "PS-H2", "PS", new Vector3(-71f, -19.2f, 116f), root, Playground.Accent);
            foreach (var h in new[] { new Vector3(-3.4f, 3.6f, 11.5f), new Vector3(40, 0, -10), new Vector3(90, 0, 30), new Vector3(-60, -9, 10), new Vector3(-125, -16, 0) })
                Pickup.HealHeart((h.y < 0 ? h : OnGround(h.x, h.z)) + Vector3.up * 1f, root);
            Enemies();
            new GameObject("Pirate watch").AddComponent<PirateWatch>().transform.SetParent(root, false);
            return root;
        }

        // the chest's shard (necklace awake, not overboard yet) or the sea floor's (after); the beam over whichever is live
        public static void ShowStoryShard()
        {
            var prog = SaveGame.Current.World(Game.World.worldId);
            bool awake = prog.necklaceAwake && !prog.storyShard, over = prog.opened.Contains(OverboardFlag);
            if (ChestShard) ChestShard.gameObject.SetActive(awake && !over);
            if (Game.I.StoryShard) Game.I.StoryShard.gameObject.SetActive(awake && over);
            if (Game.I.StoryBeam)
            {
                Game.I.StoryBeam.SetActive(awake);
                var at = over ? StoryAt : ChestAt + Vector3.up;
                Game.I.StoryBeam.transform.position = at + Vector3.up * 16f;
            }
            SetChestOpen(awake && !over || over);
        }

        public static void SetChestOpen(bool open)
        {
            if (!ChestLid) return;
            ChestLid.localRotation = Quaternion.Euler(open ? -70 : 0, 0, 0);
        }

        // the deep sea (after the shard goes overboard): no more SeaLock
        public static void OpenDeepSea(bool open)
        {
            DeepOpen = open;
            foreach (var l in SeaLock.All.ToArray()) l.gameObject.SetActive(!open);
            foreach (var l in Object.FindObjectsByType<SeaLock>(FindObjectsInactive.Include, FindObjectsSortMode.None)) l.gameObject.SetActive(!open);
        }

        public static void SetNight(bool night)
        {
            Look.SetNight(night);
            if (NightLights) foreach (var l in NightLights.GetComponentsInChildren<Light>(true)) l.intensity = night ? 2.6f : 1.2f;
        }

        // shard y in the data = the height of what it sits on (a hint): 1 m over whatever is found there
        public static Vector3 ShardPos(Vector3 hint) => new Vector3(hint.x, Ground(hint.x, hint.z, hint.y + 1.6f) + 1f, hint.z);
        static string Fmt(Vector3 v) => $"({v.x:0},{v.y:0.0},{v.z:0})";

        const int GroundMask = ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << Hero.EnemyLayer) | (1 << 4) | (1 << 2));
        public static float Ground(float x, float z, float fromY = 60f) =>
            Physics.Raycast(new Vector3(x, fromY, z), Vector3.down, out var hit, 300f, GroundMask, QueryTriggerInteraction.Ignore) ? hit.point.y : 0f;
        public static Vector3 OnGround(float x, float z, float fromY = 60f) => new Vector3(x, Ground(x, z, fromY), z);
        static Transform Box(string name, Vector3 centre, Vector3 size, string mat, bool collider = true) =>
            Placeholders.Prim(PrimitiveType.Cube, root, centre, size, WorldArt.Mat(mat), collider, name).transform;
        static Transform Box(string name, Vector3 centre, Vector3 size, Color c, string kind = "Lit", bool collider = true) =>
            Placeholders.Prim(PrimitiveType.Cube, root, centre, size, Placeholders.Mat(c, kind), collider, name).transform;
        static void Solid(Vector3 at, Vector3 size, float yaw = 0)
        {
            var b = new GameObject("Solid"); b.transform.SetParent(root, false); b.transform.position = at + Vector3.up * size.y / 2;
            b.transform.rotation = Quaternion.Euler(0, yaw, 0); b.AddComponent<BoxCollider>().size = size;
        }
        static void Hint(Vector3 at, float r, System.Func<string> text)
        {
            var go = new GameObject("Hint"); go.transform.SetParent(root, false); go.transform.position = at;
            var h = go.AddComponent<HintZone>(); h.radius = r; h.text = text;
        }
        static string B(Controls.Act a) => Controls.Btn(a);
        static Checkpoint Cp(string id, Vector3 at, float yaw, bool visible = true) => Playground.CheckpointHere(id, at, yaw, root, visible);
        static GameObject Prop(string prop, float x, float z, float yaw = 0, float scale = 1, float y = float.NaN) =>
            WorldArt.Prop("pirate_props", prop, root, float.IsNaN(y) ? OnGround(x, z) : new Vector3(x, y, z), yaw, scale, false);
        static Light PointLight(string name, Vector3 at, float range, float intensity, Color c, Transform parent = null)
        {
            var l = new GameObject(name); l.transform.SetParent(parent ? parent : root, false); l.transform.position = at;
            var lt = l.AddComponent<Light>(); lt.type = LightType.Point; lt.range = range; lt.intensity = intensity; lt.color = c; lt.shadows = LightShadows.None; return lt;
        }
        static readonly Color Warm = new Color(1f, .72f, .4f);
        // the drawn parts that are solid (the rest: sails, ropes, nets, flags, glows, windows, lines)
        static readonly string[] SolidParts = { "Hull", "Deck", "HoldFloor", "Bulkhead", "HoldEnd", "Partition", "Ramp", "Baluster", "RailCap", "RailFoot", "Mast", "Top",
                                                 "TopRim", "Nest", "NestWall", "WheelPost", "Beam", "JettyDeck", "Pile", "Bollard", "Gangplank", "Plank", "Cave", "Lump",
                                                 "Floor", "Rib", "RampSide", "BrokenMast", "Barnacle", "Chest", "Carriage", "Stock", "Shank", "Vent" };
        static void MeshSolid(GameObject go)
        {
            if (!go) return;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                string n = mf.name; int k = n.IndexOf("__"); string part = k > 0 ? n.Substring(0, k) : n;
                if (System.Array.IndexOf(SolidParts, part) < 0) continue;
                var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh;
            }
        }

        // ---- the sea: the surface (seen from above and below), the water, the deep-sea lock, the deep water
        static void Sea()
        {
            var water = new GameObject("Sea"); water.transform.SetParent(root, false); water.transform.position = new Vector3(0, -30, 25); water.transform.localScale = new Vector3(400, 60, 350);
            water.layer = 4; water.AddComponent<WaterVolume>();
            var mat = Placeholders.Mat(new Color(.12f, .36f, .62f, .72f), "Water");
            foreach (var flip in new[] { false, true })
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Plane); s.name = flip ? "Sea surface (below)" : "Sea surface"; Object.Destroy(s.GetComponent<Collider>());
                s.transform.SetParent(root, false); s.transform.position = new Vector3(0, -.02f, 25); s.transform.localScale = new Vector3(40, 1, 35);
                if (flip) s.transform.rotation = Quaternion.Euler(180, 0, 0);
                var r = s.GetComponent<Renderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                s.AddComponent<SeaWaves>();
            }
            // before the overboard: below 3.5 m the sea pushes everyone back up (the cove is shallow anyway)
            var lockGo = new GameObject("Sea lock"); lockGo.transform.SetParent(root, false); lockGo.transform.position = new Vector3(0, -32, 25); lockGo.transform.localScale = new Vector3(400, 57, 350);
            lockGo.AddComponent<SeaLock>();
            // deeper than 16 m: otter Kira, or everyone with the deep dive
            var deep = new GameObject("Deep water (deep dive)"); deep.transform.SetParent(root, false); deep.transform.position = new Vector3(0, -38, 25); deep.transform.localScale = new Vector3(400, 44, 350);
            deep.layer = 2; deep.AddComponent<DeepWater>();
        }

        // ---- the ship
        public static Transform ShipRoot { get; private set; }
        static void Ship(WorldProgress prog)
        {
            var ship = Prop("Ship", 0, 0, 0, 1f, 0f);
            if (ship) { MeshSolid(ship); ShipRoot = ship.transform; }
            else FallbackShip();
            Playground.CheckpointHere(Start, ArriveAt + Vector3.up * .05f, 0, root, false);
            Cp(VortexCp, new Vector3(-2.5f, 3.55f, -9), 0); Cp(SeaCp, new Vector3(-3.5f, 3.55f, -5.5f), 270, false);
            Hint(ArriveAt, 5f, () => $"The Pirate Seas, at sunset. Every shard counts: at {Game.RuleText(Game.World.areas[0])}, the green shard on Dan's necklace wakes ({Game.I.Progress(Game.World.areas[0])}).");
            // the treasure chest by the starboard rail (the necklace leads here)
            var chest = Prop("TreasureChest", ChestAt.x, ChestAt.z, 90, 1f, ChestAt.y);
            if (chest)
            {
                MeshSolid(chest);
                var lid = chest.transform.Find("Lid__ChestWood");
                if (lid)
                {
                    // a hinge at the lid's back edge (local -y of the prop = the rail side after the 90 deg turn)
                    var hinge = new GameObject("Lid hinge").transform; hinge.SetParent(chest.transform, false);
                    var lb = lid.GetComponent<Renderer>().bounds; hinge.position = new Vector3(lb.max.x, lb.center.y, lb.center.z);
                    hinge.rotation = Quaternion.Euler(0, -90, 0);
                    lid.SetParent(hinge, true); ChestLid = hinge;
                }
            }
            PointLight("Chest glow", ChestAt + new Vector3(-.5f, 1.4f, 0), 5, 1.2f, new Color(.5f, 1f, .6f));
            Hint(ChestAt + new Vector3(-1.5f, 0, 0), 2.5f, () => SaveGame.Current.World(Game.World.worldId).necklaceAwake ? "" : "A treasure chest, locked tight. Dan's necklace tingles near it...");
            // the plank (port side, over the water) and the gangplank (starboard) to the jetty
            var plank = Prop("Plank", -5.7f, 4f, 180, 1f, 3.58f); MeshSolid(plank);   // through a gap in the rail, at deck level
            Hint(new Vector3(-5f, 3.5f, 4f), 2f, () => "The plank! Walk it... carefully.");
            var gang = Prop("Gangplank", 5.9f, -2f, 0, 1f, 3.5f);
            if (gang) { gang.transform.rotation = Quaternion.AngleAxis(-16f, Vector3.forward) * gang.transform.rotation; MeshSolid(gang); }
            var jetty = Prop("Jetty", 13.6f, -2f, 0, 1f, 0f); MeshSolid(jetty);
            Hint(new Vector3(5f, 3.5f, -2f), 2f, () => "The gangplank, down to the jetty and the island.");
            // rope ladders up the hull (port) and the jetty's end: out of the water (WellExit: Jump climbs)
            Ladder(new Vector3(-6.15f, 4.6f, -6f), 90, new Vector3(-6.9f, 0, -6f), new Vector3(-4.6f, 3.6f, -6f), 4.7f);
            Ladder(new Vector3(15.2f, 1.2f, -4.1f), 0, new Vector3(15.2f, 0, -4.9f), new Vector3(15.2f, 1.3f, -2f), 2f);
            Hint(new Vector3(-7.5f, 0, -6f), 2.5f, () => $"A rope ladder up the hull: {B(Controls.Act.Jump)} to climb.");
            // the rigging: rope nets up to the main top (both sides), on up to the crow's nest; the fore top (Muscle Man's boost)
            Climb("Net to the main top (port)", new Vector3(-5.2f, 3.5f, -1f), 1.1f, new Vector3(-1.3f, 13.4f, -1f));
            Climb("Net to the main top (starboard)", new Vector3(5.2f, 3.5f, -1f), 1.1f, new Vector3(1.3f, 13.4f, -1f));
            Climb("Net to the crow's nest", new Vector3(-1.1f, 13.35f, 1.2f), .8f, new Vector3(-.3f, 21.05f, .35f));
            Hint(new Vector3(-4.4f, 3.5f, -1f), 1.8f, () => $"The rigging: {B(Controls.Act.Jump)} by the net to climb up to the main top.");
            Hint(new Vector3(-1.1f, 13.35f, 1.2f), 1.2f, () => $"Up the net to the crow's nest: {B(Controls.Act.Jump)}.");
            // (4.4 m out from the foremast: the throw comes down onto the fore top from above, not up into its underside)
            var boost = new GameObject("Boost spot (fore top)"); boost.transform.SetParent(root, false); boost.transform.SetPositionAndRotation(new Vector3(0, 3.5f, 7.6f), Quaternion.identity);
            var bs = boost.AddComponent<BoostSpot>(); bs.radius = 1.6f; bs.height = 9.8f; bs.landAt = new Vector3(.5f, 11.8f, 12.9f);
            Hint(new Vector3(0, 3.5f, 7.6f), 2.5f, () => "The fore top is way up there... (Muscle Man could throw you)");
            // cannons along the rails (looks; solid), cannon balls, barrels, crates, lanterns (their lights)
            foreach (var (x, z, yaw) in new[] { (-4.9f, 9f, 90f), (4.9f, 9f, -90f), (-4.6f, -10f, 90f), (4.6f, 2f, -90f) })
            { Prop("Cannon", x, z, yaw, 1f, 3.5f); Solid(new Vector3(x, 3.5f, z), new Vector3(1f, 1f, 2.1f), yaw); }
            Prop("CannonBalls", -4.2f, 6.6f, 0, 1f, 3.5f); Solid(new Vector3(-4.2f, 3.5f, 6.6f), new Vector3(.7f, .55f, .7f));
            foreach (var (x, y, z) in new[] { (-4.4f, 3.5f, -2.5f), (-3.6f, 3.5f, -2.9f), (3.4f, 6f, -21f), (-4.6f, .6f, 9.5f), (-4.6f, .6f, 11f), (-3.6f, .6f, 10.4f), (-4.4f, .6f, -3.6f), (-4.4f, .6f, -2.4f) })
            { Prop("Barrel", x, z, x * 37, 1f, y); Solid(new Vector3(x, y, z), new Vector3(.85f, 1.1f, .85f)); }
            foreach (var (x, y, z) in new[] { (3.6f, 3.5f, 6.5f), (3.6f, 4.5f, 6.5f), (4.4f, .6f, 8f), (4.4f, 1.6f, 8f), (4.4f, .6f, 11f) })
            { Prop("Crate", x, z, 0, 1f, y); Solid(new Vector3(x, y, z), new Vector3(1f, 1f, 1f)); }
            // the cargo nook in the hold (K01): crates and planks walled up to the deck beams, a 0.6 m gap at the bottom of its front: only
            // otter Kira fits through
            Box("Cargo nook side", new Vector3(-4.3f, 1.9f, -.4f), new Vector3(2.6f, 2.6f, .25f), "Plank"); Box("Cargo nook side", new Vector3(-4.3f, 1.9f, 2.4f), new Vector3(2.6f, 2.6f, .25f), "Plank");
            Box("Cargo nook front", new Vector3(-3.0f, 2.2f, 1f), new Vector3(.25f, 2.0f, 3.05f), "Plank");   // from 1.2 up to the beams (the gap: 0.6 .. 1.2)
            Box("Cargo nook front post", new Vector3(-3.0f, .9f, -.15f), new Vector3(.25f, .6f, .5f), "Plank"); Box("Cargo nook front post", new Vector3(-3.0f, .9f, 2.15f), new Vector3(.25f, .6f, .5f), "Plank");
            Hint(new Vector3(-2.3f, .6f, 1f), 1.6f, () => "Something glints behind the crates... only a little otter would fit through the gap at the bottom. (Kira)");
            // the wet deck (forecastle ramps' foot, z 7..14): otter Kira's belly-slide keeps its speed here
            var wet = new GameObject("Wet deck"); wet.transform.SetParent(root, false); wet.transform.position = new Vector3(0, 3.7f, 10.5f); wet.transform.localScale = new Vector3(5f, .8f, 7f);
            wet.AddComponent<WetDeck>();
            Box("Wet deck sheen", new Vector3(0, 3.505f, 10.5f), new Vector3(5f, .01f, 7f), new Color(.45f, .62f, .75f, .35f), "Water", false);
            Hint(new Vector3(0, 3.5f, 8f), 2.5f, () => $"A wet patch of deck. Otter Kira slides on it: run, then {B(Controls.Act.Dive)}.");
            // Muscle Man's jobs: the cannon at the cabin door, the crate stack in the hold, the anchor at the bosun's locker
            Job("Cannon", new Vector3(0, 3.5f, -12.2f), 180, "PS-cannon-cabin", new Vector3(2.4f, 0, 1.2f), new Vector3(1.1f, 1.1f, 2.2f), "rolls the cannon aside");
            var crates = new GameObject("Crate stack"); crates.transform.SetParent(root, false); crates.transform.position = new Vector3(0, .6f, -6.3f);
            for (int i = 0; i < 6; i++) { var c = WorldArt.Prop("pirate_props", "Crate", crates.transform, new Vector3((i % 3 - 1) * 1.0f, .6f + (i / 3) * 1.0f, -6.3f)); if (!c) Placeholders.Prim(PrimitiveType.Cube, crates.transform, new Vector3((i % 3 - 1), .5f + i / 3, 0), Vector3.one, WorldArt.Mat("Plank"), false, "Crate"); }
            JobOn(crates, "PS-crates-hold", new Vector3(3.3f, 0, 1.4f), new Vector3(3f, 2f, 1f), "lifts the crates aside");
            Job("Anchor", new Vector3(0, 3.5f, 14.3f), 90, "PS-anchor-locker", new Vector3(-2.2f, 0, -1.6f), new Vector3(2.8f, 1.1f, 1.4f), "lifts the anchor away");
            Hint(new Vector3(0, 3.5f, -10.5f), 2.5f, () => SaveGame.Current.Has(Abilities.MuscleMan) ? "" : "A cannon in front of the captain's cabin door. Far too heavy to move.");
            // the captain's cabin and the bosun's locker: lanterns; the hold: lanterns
            var lights = new GameObject("Lantern lights"); lights.transform.SetParent(root, false); NightLights = lights;
            foreach (var c in new[] { new Vector3(-4.2f, 7.5f, -21.7f), new Vector3(4.2f, 7.5f, -21.7f), new Vector3(0, 6.9f, -.6f), new Vector3(0, 6.9f, 11.5f), new Vector3(-3.6f, 7.5f, 22f), new Vector3(3.6f, 7.5f, 22f) })
                PointLight("Lantern light", c, 12, 1.2f, Warm, lights.transform);
            PointLight("Cabin lantern", new Vector3(0, 5.3f, -17.5f), 9, 1.8f, Warm); PointLight("Locker lantern", new Vector3(0, 5.3f, 19f), 7, 1.6f, Warm);
            PointLight("Hold lantern", new Vector3(0, 2.7f, 6f), 12, 1.6f, Warm); PointLight("Hold lantern (aft)", new Vector3(0, 2.7f, -3f), 12, 1.6f, Warm);
            PointLight("Back room lantern", new Vector3(0, 2.7f, -9.5f), 8, 1.4f, Warm);
            // the ship's fiddler (he teaches everyone the deep dive)
            var v = Violinist.Make(root, ViolinistAt, -90f, "Fiddle");
            v.ability = Abilities.DeepDive; v.learner = "*"; v.moveName = "Deep Dive";
            Hint(ViolinistAt + new Vector3(-2.5f, 0, 0), 4f, () => SaveGame.Current.Has(Abilities.DeepDive) ? "The deep dive: everyone holds their breath three times as long, and swims as deep as they like (the trench, the cave; and the deep water in every world)." : "A fiddler on the quarterdeck, playing a sea shanty to the sunset...");
            // the vortex gate (open after the green shard: the way back to the vortex)
            Gate = Playground.PortalHere("PS-vortex-gate", new Vector3(-3.4f, 3.5f, -11), 90, Vortex.Checkpoint, false, root, Green, vortex: true);
            Gate.gameObject.SetActive(prog.storyShard);
        }

        static void Ladder(Vector3 top, float yaw, Vector3 waterAt, Vector3 exitTo, float len)
        {
            var l = Prop("RopeLadder", top.x, top.z, yaw, 1f, top.y);
            if (l) l.transform.localScale = new Vector3(l.transform.localScale.x, l.transform.localScale.y, l.transform.localScale.z * len / 5f);
            var go = new GameObject("Ladder (out of the water)"); go.transform.SetParent(root, false); go.transform.position = waterAt;
            var we = go.AddComponent<WellExit>(); we.centre = waterAt; we.radius = 1.7f; we.rimTop = exitTo.y; we.fixedExit = exitTo; we.hintText = "A rope ladder! {0} climbs up.";
        }

        static void Climb(string name, Vector3 at, float r, Vector3 to)
        {
            var go = new GameObject(name); go.transform.SetParent(root, false); go.transform.position = at;
            var c = go.AddComponent<RopeClimb>(); c.radius = r; c.to = to;
        }

        static void Job(string prop, Vector3 at, float yaw, string id, Vector3 aside, Vector3 size, string verb)
        {
            var g = new GameObject(prop + " (Muscle Man)"); g.transform.SetParent(root, false); g.transform.position = at;
            var p = WorldArt.Prop("pirate_props", prop, g.transform, at, yaw);
            if (!p) Placeholders.Prim(PrimitiveType.Cube, g.transform, at + Vector3.up * size.y / 2, size, WorldArt.Mat("Iron"), false, prop);
            JobOn(g, id, aside, size, verb, yaw);
        }
        static void JobOn(GameObject g, string id, Vector3 aside, Vector3 size, string verb, float yaw = 0)
        {
            var col = new GameObject("Block"); col.transform.SetParent(g.transform, false); col.transform.position = g.transform.position + Vector3.up * size.y / 2; col.transform.rotation = Quaternion.Euler(0, yaw, 0);
            col.AddComponent<BoxCollider>().size = size;
            var b = g.AddComponent<Boulder>(); b.id = id; b.aside = aside; b.verbText = verb; b.lifts = true;
        }

        static void FallbackShip()
        {
            Box("Fallback deck", new Vector3(0, 3.35f, 0), new Vector3(11, .3f, 40), "DeckWood");
            Box("Fallback hull", new Vector3(0, .5f, 0), new Vector3(12, 5, 44), "ShipRed");
        }

        // ---- the island: beaches, palms, rocks, the jungle path to the hilltop, the deckhands' camp, the cove
        static void Island()
        {
            Cp(IslandCp, OnGround(32, 4), 90);
            Hint(OnGround(32, 2), 5f, () => "A tropical island! Crabs on the beach, a camp of snoozy deckhands, a cove on the north side.");
            var rnd = new System.Random(10);
            foreach (var (x, z) in new[] { (34f, 22f), (38f, -14f), (46f, 36f), (52f, -30f), (58f, 18f), (64f, 44f), (70f, -20f), (74f, 30f), (86f, -4f), (92f, 42f), (98f, 20f), (104f, -24f), (110f, 6f), (116f, 40f), (60f, 0f), (82f, 28f), (96f, -40f), (44f, 50f) })
            {
                var g = OnGround(x, z); if (g.y < .3f) continue;
                Prop(rnd.Next(3) == 0 ? "PalmSmall" : "PalmTree", x, z, rnd.Next(360));
                Solid(g, new Vector3(.6f, 4f, .6f));
            }
            foreach (var (x, z, k) in new[] { (48f, 6f, 0), (66f, -8f, 1), (88f, 46f, 1), (108f, -6f, 0), (76f, -36f, 1), (100f, 30f, 0) })
            {
                var g = OnGround(x, z);
                var rk = Prop(k == 0 ? "RockA" : "RockB", x, z, x * 11, 1f, g.y - .3f);
                if (rk) foreach (var mf in rk.GetComponentsInChildren<MeshFilter>()) { var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; mc.convex = true; }
            }
            foreach (var (x, z) in new[] { (30f, 0f), (33f, 16f), (40f, -22f), (118f, 30f), (52f, -40f) }) Prop("Starfish", x, z, x * 17, 2f, OnGround(x, z).y + .02f);
            // the deckhands' camp (the fire, a lean-to)
            var camp = OnGround(56, -6);
            Prop("Campfire", camp.x, camp.z, 0, 1f, camp.y); PointLight("Campfire light", camp + Vector3.up * 1f, 10, 2f, new Color(1f, .55f, .2f));
            Prop("LeanTo", 59, -9, 30); Solid(OnGround(59, -9), new Vector3(3.4f, 1.2f, 2.4f), 30);
            Hint(camp + new Vector3(-3, 0, 0), 4f, () => "The deckhands' camp. They snore... mostly.");
            Hint(OnGround(80, 12), 4f, () => "The top of the island: the whole sea around you. The reef glows under the water to the west.");
            Hint(OnGround(82, 50), 4f, () => "A shallow cove, warm and clear.");
        }

        // ---- under the sea: corals, kelp, the clearing, the wreck, the cave, air bubbles, fish
        static void UnderTheSea(WorldProgress prog)
        {
            var rnd = new System.Random(7);
            string[] corals = { "CoralBrain", "CoralFan", "CoralTube", "CoralBranch" };
            int n = 0;
            for (int i = 0; i < 150; i++)
            {
                float x = -110 + (float)rnd.NextDouble() * 90, z = -65 + (float)rnd.NextDouble() * 130;
                if (Mathf.Abs(x + 45) < 3.5f && Mathf.Abs(z - 10) < 3.5f) continue;   // the shard's own spot stays clear
                var g = OnGround(x, z, 0); if (g.y > -6) continue;
                var c = Prop(corals[rnd.Next(4)], x, z, rnd.Next(360), .8f + (float)rnd.NextDouble() * 1.2f, g.y - .05f);
                if (c) foreach (var r in c.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                n++;
            }
            // a ring of corals round the green shard's clearing (the film's shot)
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f * Mathf.Deg2Rad; var g = OnGround(-45 + Mathf.Cos(a) * 5.5f, 10 + Mathf.Sin(a) * 5.5f, 0);
                Prop(corals[i % 4], g.x, g.z, i * 47, 1.3f, g.y - .05f);
            }
            for (int i = 0; i < 30; i++)
            {
                float x = -20 + (float)rnd.NextDouble() * 40, z = 45 + (float)rnd.NextDouble() * 35;
                var g = OnGround(x, z, 0); if (g.y > -6) continue;
                var k = Prop("Kelp", x, z, rnd.Next(360), 1.2f + (float)rnd.NextDouble(), g.y - .05f);
                if (k) { foreach (var r in k.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; k.AddComponent<Sway>(); }
            }
            PlayLog.Line($"the reef: {n} corals");
            // the wreck in its trench, the cave in its basin (their meshes are solid)
            var wreck = Prop("Shipwreck", -70, 118, 35, 1f, Ground(-70, 118, 0) - .3f); MeshSolid(wreck);
            var cave = Prop("SeaCave", -135, 0, 0, 1f, -18.2f); MeshSolid(cave);
            PointLight("Cave glow", new Vector3(-133, -12, 4), 18, 1.4f, new Color(.4f, .9f, 1f)); PointLight("Grotto glow", new Vector3(-134, -15, 25), 8, 1.6f, new Color(.5f, 1f, .8f));
            PointLight("Wreck glow", new Vector3(-70, -14, 116), 16, 1.2f, new Color(.5f, .9f, 1f));
            Hint(new Vector3(-118, -14, 0), 6f, () => SaveGame.Current.Has(Abilities.DeepDive) ? "A sea cave. It goes deep." : "A sea cave, deeper than anyone can hold their breath... except a little otter. (Kira, or the deep dive)");
            Hint(new Vector3(-136.65f, -17.7f, 18f), 2.5f, () => "A narrow gap in the rock. Only a little otter could squeeze through. (Kira)");
            Hint(new Vector3(-62, -14, 100), 7f, () => "A sunken ship in a deep trench. (Kira, or the deep dive)");
            // air bubbles from vents on the sea floor (Dan and Abi get their breath back in them)
            foreach (var (x, z) in new[] { (-20f, -8f), (-45f, 18f), (-70f, 25f), (-95f, 0f), (-60f, 100f), (-120f, 0f), (-132f, 8f), (5f, 35f), (20f, -25f), (-50f, -40f), (-85f, 45f), (-10f, 10f) })
            {
                var g = OnGround(x, z, 0); if (g.y > -2) continue;
                Prop("BubbleVent", x, z, x * 7, 1f, g.y);
                var a = new GameObject("Air bubbles"); a.transform.SetParent(root, false); a.transform.position = new Vector3(x, g.y / 2f, z); a.transform.localScale = new Vector3(2.2f, -g.y + 2f, 2.2f);
                a.AddComponent<AirVolume>().MakeBubbles();
            }
            // the green shard's clearing: a soft light
            PointLight("Clearing glow", StoryAt + Vector3.up * 2f, 10, 1.6f, new Color(.5f, 1f, .65f));
            // fish schools (friendly)
            foreach (var (x, y, z, r) in new[] { (-40f, -6f, 0f, 9f), (-70f, -6f, 30f, 8f), (-90f, -5f, -30f, 10f), (-30f, -7f, 45f, 7f), (-60f, -14f, 105f, 9f), (-125f, -10f, 0f, 8f), (10f, -6f, 30f, 7f), (25f, -4f, -20f, 6f) })
                FishSchool.Make(root, new Vector3(x, y, z), r, 10);
            Hint(new Vector3(-9, -1, -6), 3f, () => DeepOpen ? "" : "The water's lovely... but it gets dark and deep fast. Better stay near the top.");
            _ = prog;
        }

        static void Enemies()
        {
            Enemy.Deckhand(root, new Vector3(-3f, 3.6f, -8f), new Vector3(3f, 3.6f, -10.5f));   // the main deck aft (clear of the arrival spot and the fiddler)
            Enemy.Deckhand(root, new Vector3(-1.5f, .7f, 11f), new Vector3(1.5f, .7f, 3f));
            Enemy.Deckhand(root, OnGround(52, -2) + Vector3.up * .1f, OnGround(62, -12) + Vector3.up * .1f);
            Enemy.Deckhand(root, OnGround(70, 20) + Vector3.up * .1f, OnGround(88, 34) + Vector3.up * .1f);
            Enemy.Crab(root, OnGround(34, 4) + Vector3.up * .1f, OnGround(36, 18) + Vector3.up * .1f);
            Enemy.Crab(root, OnGround(44, -32) + Vector3.up * .1f, OnGround(56, -40) + Vector3.up * .1f);
            Enemy.Crab(root, OnGround(112, 22) + Vector3.up * .1f, OnGround(116, 40) + Vector3.up * .1f);
            Enemy.Crab(root, new Vector3(1.5f, 6.1f, 22f), new Vector3(-1.2f, 6.1f, 21.2f));   // the forecastle's bow
            foreach (var (x, y, z) in new[] { (-35f, -7f, -10f), (-60f, -6f, 15f), (-80f, -6f, -25f), (-55f, -7f, 45f), (-95f, -6f, 30f), (-65f, -15f, 95f), (-110f, -12f, -10f) })
                Enemy.Pufferfish(root, new Vector3(x, y, z));
        }
    }
}
