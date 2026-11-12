using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 11: Victorian Steampunk, world 6, two areas on flat ground (both at 0), the same song (Dan's steampunk.mp3):
    //   the factory at night (the hall x -36..36, z 0..96, roof 18; blue-violet haze through the skylights, warm brass lamps):
    //     the arrival (SteampunkArrival: the bolt bucket tips them onto the belt), the belt run on the conveyor (x -2..2, top 3.0, z 8..88,
    //     carried toward the press at 2.4 m/s; stampers and steam jets; screens on both sides; its gate at z 80 opens after 30 s), the great
    //     press over the belt's end (z 84): MuscleManHero, then the press is gone through the north wall (the hole x -5..5 at z 96) and the
    //     factory is free: the catwalk ring at 7 (stairs east and west), the boiler room (gear door: wind-up), the lift to the crane deck
    //     (wind-up), the gantry (its bridge: wind-up), two riveted pillars (magnet paws), the store cage (bars: Muscle Man), the tool nook
    //     (machine: Muscle Man), the stamper lane (the big stamper: Muscle Man), the brick stack's shaft (wall jump), workers, oil, steam
    //   the streets by day (warm golden morning): the yard (the press's wreck), the avenue (z 112..176; the narrow alley west: wall jump; the
    //     pipe alley east: Muscle Man), the square (x -40..40, z 178..222) with the great clock at (0, 200) (its face holds the teal gear
    //     shard; ClockReach at its foot plays ClockShard once the necklace is awake), brass carriages on the rail loop (x +-22), clockwork
    //     horses, the Violinist (a street musician: Abi's wall jump), the west street (the carriage turntable: wind-up; the signal tower:
    //     magnet paws), the east street and the great gear gate to the clockmaker's yard (wind-up), the north street; the passage behind
    //     the crates (Muscle Man), a cream house's roof (Muscle Man's boost)
    public static class Steampunk
    {
        static Transform root;
        public const string Start = "SP-start", FactoryCp = "SP-factory", VortexCp = "SP-vortex", StreetCp = "SP-street";
        public const string ArrivedFlag = "SP-arrived", PressFlag = "SP-press";
        public static readonly Vector3 ArriveAt = new Vector3(0, 3.05f, 10f);         // on the belt, by the chute
        public static readonly Vector3 PressAt = new Vector3(0, 3f, 84f);             // the press over the belt's end (the belt's top)
        public static readonly Vector3 AfterPress = new Vector3(5f, 0, 86f);           // on the floor beside the belt's end
        public static readonly Vector3 ClockAt = new Vector3(0, 0, 200f);
        public static readonly Vector3 ViolinistAt = new Vector3(32f, 0, 181f);
        public static readonly Color Teal = new Color(.15f, .9f, .85f);
        public const float BeltTop = 3.0f, BeltSpeed = 2.4f, BeltGateAfter = 30f;
        public static Portal Gate { get; private set; }
        public static bool PressGone { get; private set; }
        static GameObject pressFrame, pressDome, pressStub, pressWreck, wallPatch, wallBreak, beltGuard, beltGate;
        public static Transform PressDome => pressDome ? pressDome.transform : null;
        static Conveyor belt; static readonly List<GameObject> beltRunOnly = new List<GameObject>();
        public static GameObject ShardVisual { get; private set; }

        // ---- the belt run (item 5): from the arrival to the press. Not escapable (screens), never a lost heart; the gate opens after 30 s.
        public static bool BeltRunning { get; private set; }
        public static float BeltTime { get; private set; }
        public static bool BeltGateOpen => beltGate && !beltGate.activeSelf;
        public static int BeltRuns { get; private set; }
        public static void StartBeltRun()
        {
            if (PressGone) return;
            BeltRunning = true; BeltTime = 0; BeltRuns++;
            if (beltGate) beltGate.SetActive(true);
            if (belt) belt.speed = BeltSpeed;
            Party.I.PlaceAll(ArriveAt + Vector3.up * .05f, Quaternion.identity); CameraRig.I.SnapBehind();
            SaveGame.Current.checkpoint = Start;
            Hud.Message($"The conveyor carries everyone toward the great press! Hop the stampers and the steam jets ({Controls.Btn(Controls.Act.Jump)}).", 6f);
            PlayLog.Line("the belt run starts");
        }
        static void BeltTick()
        {
            if (!BeltRunning || !Party.I || !Game.Playing) return;
            BeltTime += Time.deltaTime;
            if (beltGate && beltGate.activeSelf && BeltTime >= BeltGateAfter) { beltGate.SetActive(false); Sfx.Play("alarm", .5f); Hud.Message("The gate ahead swings open... and there's the great press!", 3f); PlayLog.Line($"the belt's gate opens after {BeltTime:0.0} s"); }
            var p = Party.I.Player;
            // falling off can't happen (screens); if anything goes wrong: back on the belt where they were
            if (p.transform.position.y < BeltTop - 1.2f || Mathf.Abs(p.transform.position.x) > 3f)
            {
                var back = new Vector3(0, BeltTop + .1f, Mathf.Clamp(p.transform.position.z - 2f, 10f, 78f));
                Party.I.PlaceAll(back, Quaternion.identity); PlayLog.Line($"back on the belt at {back}");
            }
            if (BeltGateOpen && p.transform.position.z > 81.2f && p.Motor.Grounded && !Game.I.Busy)
            {
                BeltRunning = false;
                PlayLog.Line($"the belt run ends at the press after {BeltTime:0.0} s");
                Game.I.StartCoroutine(Game.I.PressScene());
            }
        }

        // after MuscleManHero (or on load): the press is gone through the wall, the belt stopped, the screens down, the factory free
        public static void SetPressGone(bool gone)
        {
            PressGone = gone;
            if (pressFrame) pressFrame.SetActive(!gone); if (pressDome) pressDome.SetActive(!gone); if (pressStub) pressStub.SetActive(gone);
            if (pressWreck) pressWreck.SetActive(gone); if (wallPatch) wallPatch.SetActive(!gone); if (wallBreak) wallBreak.SetActive(gone);
            if (beltGuard) beltGuard.SetActive(!gone); if (beltGate) beltGate.SetActive(!gone);
            if (belt) belt.speed = gone ? 0f : BeltSpeed;
            foreach (var g in beltRunOnly) if (g) g.SetActive(!gone);
            if (gone) BeltRunning = false;
            Physics.SyncTransforms();
        }

        // MuscleManHero brings its own dome, wall patch and broken wall: the world's press dome, patch, screens and gate step aside for it
        public static void PrepareScene()
        {
            if (pressDome) pressDome.SetActive(false); if (wallPatch) wallPatch.SetActive(false); if (wallBreak) wallBreak.SetActive(false);
            if (beltGuard) beltGuard.SetActive(false); if (beltGate) beltGate.SetActive(false);
            foreach (var g in beltRunOnly) if (g) g.SetActive(false);
        }

        // look and sound: the factory at night, the streets by day (by where the player is)
        static bool lastNight = true, lookSet;
        public static void Watch()
        {
            if (!Game.InSteampunk || !Party.I) return;
            BeltTick();
            var p = Party.I.Player.transform.position;
            bool night = p.z < 96.6f && !Vortex.Inside(p);
            if (!lookSet || night != lastNight) { lookSet = true; lastNight = night; Look.SetNight(night); Music.SetAmbience(Music.Track == "steampunk" ? (night ? "factory_loop" : "city_loop") : null); }
        }

        public static Transform Build(WorldData w)
        {
            root = new GameObject("World Victorian Steampunk").transform;
            beltRunOnly.Clear(); lookSet = false; BeltRunning = false;
            var save = SaveGame.Current; var prog = save.World(w.worldId);
            var terrain = WorldArt.SpawnFile("steampunk_terrain", root);
            if (terrain) foreach (var mf in terrain.GetComponentsInChildren<MeshFilter>()) { string n = mf.name; if (n.StartsWith("Rail") || n.StartsWith("Sleeper")) continue; var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; }
            else { Box("Fallback floor", new Vector3(0, -.5f, 48), new Vector3(72, 1, 96), "FactoryFloor"); Box("Fallback streets", new Vector3(0, -.5f, 206), new Vector3(200, 1, 190), "Cobble"); }
            Physics.SyncTransforms();
            Factory(prog); Yard(); Avenue(); Square(prog); WestStreet(prog); EastStreet(prog); NorthStreet();
            foreach (var (c, s) in new[] { (new Vector3(0, 30, -2), new Vector3(220, 80, 4)), (new Vector3(0, 30, 302), new Vector3(220, 80, 4)),
                                           (new Vector3(-102, 30, 150), new Vector3(4, 80, 310)), (new Vector3(102, 30, 150), new Vector3(4, 80, 310)) })
                Box("World boundary", c, s, Color.clear, "Lit").GetComponent<Renderer>().enabled = false;
            Physics.SyncTransforms();
            foreach (var s in w.shards)
                if (!prog.shards.Contains(s.id)) Pickup.Make(Pickup.Kind.Shard, s.id, s.area, ShardPos(s.position), root, new Color(.55f, 1f, .95f));
            PlayLog.Line("Steampunk shards placed: " + string.Join(" ", w.shards.Select(s => $"{s.id}{Fmt(ShardPos(s.position))}")));
            var a = w.Area("SP");
            // the story shard: in the great clock's face (too high to reach: ClockReach at its foot plays the finale)
            var story = Pickup.Make(Pickup.Kind.StoryShard, "story-" + w.worldId, a.id, a.storyShard, root, a.storyShardColor);
            foreach (var c in story.GetComponentsInChildren<Collider>()) c.enabled = false;
            story.radius = .01f;   // (the necklace points at it; nobody can touch it up there: ClockReach plays the finale)
            Game.I.StoryShard = story;
            Game.I.StoryBeam = Box("Story beam", ClockAt + new Vector3(0, 22f, -1.3f), new Vector3(.35f, 28f, .35f), new Color(.5f, 1f, .95f), "Glow", false).gameObject;
            ShardVisual = Prop("TealGearShard", a.storyShard.x, a.storyShard.z, 0, 1f, a.storyShard.y);
            bool awake = prog.necklaceAwake && !prog.storyShard;
            story.gameObject.SetActive(awake); Game.I.StoryBeam.SetActive(awake);
            if (ShardVisual) ShardVisual.SetActive(!prog.storyShard);
            if (!save.heartUpgrades.Contains("SP-H1")) Pickup.Make(Pickup.Kind.HeartUpgrade, "SP-H1", "SP", new Vector3(-33f, 1f, 6.5f), root, Playground.Accent);
            if (!save.heartUpgrades.Contains("SP-H2")) Pickup.Make(Pickup.Kind.HeartUpgrade, "SP-H2", "SP", new Vector3(94f, 1f, 208f), root, Playground.Accent);
            foreach (var h in new[] { new Vector3(-6, 0, 30), new Vector3(10, 0, 70), new Vector3(0, 0, 106), new Vector3(-6, 0, 186), new Vector3(-60, 0, 206), new Vector3(56, 0, 194) })
                Pickup.HealHeart(OnGround(h.x, h.z) + Vector3.up * 1f, root);
            TripoDecor();
            Enemies();
            new GameObject("Steampunk watch").AddComponent<SteampunkWatch>().transform.SetParent(root, false);
            SetPressGone(prog.opened.Contains(PressFlag));
            return root;
        }

        // shard y in the data = the height of what it sits on (a hint): 1 m over whatever is found there
        public static Vector3 ShardPos(Vector3 hint) => new Vector3(hint.x, Ground(hint.x, hint.z, hint.y + 1.6f) + 1f, hint.z);
        static string Fmt(Vector3 v) => $"({v.x:0},{v.y:0.0},{v.z:0})";
        const int GroundMask = ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << Hero.EnemyLayer) | (1 << 4) | (1 << 2));
        public static float Ground(float x, float z, float fromY = 40f) =>
            Physics.Raycast(new Vector3(x, fromY, z), Vector3.down, out var hit, 200f, GroundMask, QueryTriggerInteraction.Ignore) ? hit.point.y : 0f;
        public static Vector3 OnGround(float x, float z, float fromY = 40f) => new Vector3(x, Ground(x, z, fromY), z);
        static Transform Box(string name, Vector3 centre, Vector3 size, string mat, bool collider = true) =>
            Placeholders.Prim(PrimitiveType.Cube, root, centre, size, WorldArt.Mat(mat), collider, name).transform;
        static Transform Box(string name, Vector3 centre, Vector3 size, Color c, string kind = "Lit", bool collider = true) =>
            Placeholders.Prim(PrimitiveType.Cube, root, centre, size, Placeholders.Mat(c, kind), collider, name).transform;
        // a wall box from x0..x1, z0..z1, y0..y1
        static Transform Wall(string name, float x0, float x1, float z0, float z1, float y1, string mat = "Brick", float y0 = 0) =>
            Box(name, new Vector3((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2), new Vector3(x1 - x0, y1 - y0, z1 - z0), mat);
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
        static GameObject Prop(string prop, float x, float z, float yaw = 0, float scale = 1, float y = float.NaN, Transform parent = null) =>
            WorldArt.Prop("steampunk_props", prop, parent ? parent : root, float.IsNaN(y) ? OnGround(x, z) : new Vector3(x, y, z), yaw, scale, false);
        static Light PointLight(string name, Vector3 at, float range, float intensity, Color c, Transform parent = null)
        {
            var l = new GameObject(name); l.transform.SetParent(parent ? parent : root, false); l.transform.position = at;
            var lt = l.AddComponent<Light>(); lt.type = LightType.Point; lt.range = range; lt.intensity = intensity; lt.color = c; lt.shadows = LightShadows.None; return lt;
        }
        static readonly Color Warm = new Color(1f, .74f, .42f);
        // the drawn parts that are solid (the rest: glass, glows, rail strips, chains, wall gears, decals, oil)
        public static readonly string[] SolidParts = { "Wall", "Pillar", "Catwalk", "Step", "CatRail", "Patch", "Rubble", "BeltTop", "BeltFrame", "BeltLeg", "BeltRail", "Chute",
            "BeltStair", "Guard", "GuardGate", "StamperFrame", "Boiler", "Bucket", "PressPillar", "PressBeam", "PressPlate", "PressRing", "Dome", "MagPillar", "Girder",
            "LiftDeck", "Shaft", "Bridge", "GearDoor", "GearGate", "Frame", "Machine", "Bars", "HouseWall", "Roof", "Parapet", "Chimney", "Post", "Bench", "Base",
            "Column", "ClockHead", "Carriage", "CarriageRoof", "Turntable", "YardWall", "Coping", "PipeFence", "Crate", "Barrel", "Panel", "Console",
            "Vent", "Cornice", "Trim", "Plinth", "Lintel", "Pilaster", "PillarBase", "GuardPane", "Cradle", "PressFoot", "BeamCap", "PressPiston", "BridgeBeam",
            "Teeth", "Voussoir", "Keystone", "BarFrame", "Pier", "BaseCap", "ColumnRing", "Chassis", "Hoop", "ChuteLip", "Rim" };
        static void MeshSolid(GameObject go)
        {
            if (!go) return;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                string n = mf.name; int k = n.IndexOf("__"); string part = k > 0 ? n.Substring(0, k) : n;
                if (System.Array.IndexOf(SolidParts, part) < 0) continue;
                var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh;
            }
        }
        static GameObject SolidProp(string prop, float x, float z, float yaw = 0, float y = float.NaN, Transform parent = null) { var g = Prop(prop, x, z, yaw, 1f, y, parent); MeshSolid(g); return g; }

        // ---- the factory
        static void Factory(WorldProgress prog)
        {
            var hall = SolidProp("FactoryHall", 0, 0, 0, 0f);
            if (!hall)
            {
                Wall("Fallback west wall", -36.8f, -36, 0, 96, 18); Wall("Fallback east wall", 36, 36.8f, 0, 96, 18); Wall("Fallback south wall", -36.8f, 36.8f, -.8f, 0, 18);
                Wall("Fallback north wall", -36.8f, -5, 96, 96.8f, 18); Wall("Fallback north wall", 5, 36.8f, 96, 96.8f, 18); Wall("Fallback north lintel", -5, 5, 96, 96.8f, 18, "Brick", 9);
                Wall("Fallback catwalk", 33.5f, 36, 0, 96, 7, "IronGrate", 6.8f); Wall("Fallback catwalk", -36, -33.5f, 0, 96, 7, "IronGrate", 6.8f); Wall("Fallback catwalk", -36, 36, 0, 2.5f, 7, "IronGrate", 6.8f);
            }
            wallPatch = SolidProp("WallPatch", 0, 96, 0, 0f) ?? Wall("Fallback wall patch", -5, 5, 96, 96.8f, 9).gameObject;
            wallBreak = Prop("WallBreak", 0, 96, 0, 1f, 0f); MeshSolid(wallBreak);
            Playground.CheckpointHere(Start, ArriveAt + Vector3.up * .05f, 0, root, false);
            Cp(FactoryCp, AfterPress, 0, false);
            Hint(AfterPress, 4f, () => PressGone ? $"The factory is ours! Shards everywhere: the catwalks, the machines... and through the hole, the city. At {Game.RuleText(Game.World.areas[0])}, the teal shard on Dan's necklace wakes ({Game.I.Progress(Game.World.areas[0])})." : "");
            // the conveyor belt (carries what stands on it toward the press until the press is gone)
            var conv = SolidProp("ConveyorBelt", 0, 8, 0, 0f);
            if (!conv) { Box("Fallback belt", new Vector3(0, BeltTop / 2, 48), new Vector3(4, BeltTop, 80), "BeltRubber"); }
            var bv = new GameObject("Conveyor"); bv.transform.SetParent(root, false); bv.transform.position = new Vector3(0, BeltTop + .45f, 48); bv.transform.localScale = new Vector3(4.2f, 1.1f, 80f);
            belt = bv.AddComponent<Conveyor>(); belt.dir = Vector3.forward; belt.speed = BeltSpeed;
            beltGuard = SolidProp("BeltGuard", 0, 8, 0, 0f);
            if (!beltGuard) { var g = new GameObject("Fallback belt guard"); g.transform.SetParent(root, false); foreach (var sx in new[] { -2.3f, 2.3f }) Box("Belt guard", new Vector3(sx, 4.7f, 48), new Vector3(.12f, 3.4f, 80), new Color(.3f, .32f, .36f)).SetParent(g.transform, true); beltGuard = g; }
            beltGate = SolidProp("BeltGate", 0, 80, 0, BeltTop) ?? Box("Fallback belt gate", new Vector3(0, BeltTop + 1.7f, 80), new Vector3(4.2f, 3.4f, .2f), new Color(.25f, .25f, .28f)).gameObject;
            // stampers across the belt (z 22, 36, 46, 60) and steam jets in it (z 29, 41, 55, 67): the belt run's hazards
            float ph = 0;
            foreach (var z in new[] { 22f, 36f, 46f, 60f }) { beltRunOnly.Add(MakeStamper(new Vector3(0, BeltTop, z), 0, Vector3.forward, ph, 2.6f).gameObject); ph += .9f; }   // (a bonk never sends you back: it squashes you on through)
            foreach (var z in new[] { 29f, 41f, 55f, 67f }) { var j = MakeJet(new Vector3(Random.Range(-1f, 1f), BeltTop, z), ph); beltRunOnly.Add(j.gameObject); ph += 1.1f; }
            Hint(new Vector3(0, BeltTop, 14), 3f, () => Steampunk.BeltRunning ? $"Run! Jump the stampers ({B(Controls.Act.Jump)}); the steam gives you a hop." : "");
            // the great press over the belt's end; after: its stubs, and its wreck out in the yard
            pressFrame = SolidProp("PressFrame", 0, 84, 0, 0f);
            pressDome = SolidProp("PressDome", 0, 84, 0, BeltTop + 6.2f);
            pressStub = SolidProp("PressStub", 0, 84, 0, 0f);
            pressWreck = SolidProp("PressWreck", 9, 104, 25, 0f);
            if (!pressFrame) { var f = new GameObject("Fallback press"); f.transform.SetParent(root, false); foreach (var sx in new[] { -4.6f, 4.6f }) Wall("Press pillar", sx - .6f, sx + .6f, 83.4f, 84.6f, 15, "Iron").SetParent(f.transform, true); pressFrame = f; }
            if (!pressDome) pressDome = Box("Fallback press dome", new Vector3(0, BeltTop + 8.7f, 84), new Vector3(7, 5, 6), "Brass").gameObject;
            // the belt's stairs down (both sides, z 72..76) are part of the belt; the guards cover them during the run
            // lights: warm brass lamps along the walls, a soft violet fill
            var lamps = new GameObject("Factory lamps"); lamps.transform.SetParent(root, false);
            for (int i = 0; i < 6; i++) foreach (var sx in new[] { -34.5f, 34.5f }) PointLight("Brass lamp", new Vector3(sx, 9.5f, 8 + i * 16), 22, 1.6f, Warm, lamps.transform);
            foreach (var z in new[] { 20f, 50f, 80f }) PointLight("Belt lamp", new Vector3(0, 9f, z), 18, 1.3f, Warm, lamps.transform);
            PointLight("Violet fill", new Vector3(0, 16f, 48), 70, .6f, new Color(.55f, .45f, 1f), lamps.transform);
            // the factory floor: the boiler, crates, barrels, control panels, steam vents, oil
            SolidProp("Boiler", -20, 20, 0);
            foreach (var (x, z, s) in new[] { (16f, 28f, 0), (17f, 29.2f, 1), (16f, 30.4f, 0), (-14f, 47f, 2), (-13f, 48.2f, 2), (18f, 88f, 0), (-16f, 86f, 1), (10f, 8f, 0), (25f, 6f, 1), (-8f, 92f, 2) })
            {
                string p = s == 0 ? "Crate" : s == 1 ? "Barrel" : "Barrel";
                var g = Prop(p, x, z, x * 31, 1f, 0f); MeshSolid(g); if (!g) Box("Fallback " + p, new Vector3(x, .5f, z), Vector3.one, "Wood");
            }
            foreach (var (x, z, yaw) in new[] { (12f, 93.2f, 0f), (16f, 93.2f, 0f), (-20f, 93.2f, 0f), (30f, 60f, -90f) }) SolidProp("ControlPanel", x, z, yaw, 0f);
            foreach (var (x, z) in new[] { (-6f, 40f), (8f, 70f), (-8f, 20f), (24f, 80f), (-26f, 70f) }) { SolidProp("SteamVent", x, z, 0, 0f); MakeJet(new Vector3(x, 0, z), x * .37f, 1.6f); }
            foreach (var (x, z) in new[] { (10f, 40f), (-10f, 66f), (12f, 80f), (-6f, 58f) })
            {
                Prop("OilPatch", x, z, x * 13, 1f, .005f);
                var o = new GameObject("Oil patch"); o.transform.SetParent(root, false); o.transform.position = new Vector3(x, .4f, z); o.transform.localScale = new Vector3(2.4f, .8f, 2.4f); o.AddComponent<OilPatch>();
            }
            Hint(new Vector3(10, 0, 37), 3f, () => "Oil on the floor. The workers never look where they're going...");
            // the boiler room (x -36..-24, z 0..12, roof at 5): its gear door (mechanical Kira's socket beside it)
            Wall("Boiler room wall", -24.4f, -24, 0, 3.9f, 5); Wall("Boiler room wall", -24.4f, -24, 8.1f, 12, 5); Wall("Boiler room lintel", -24.4f, -24, 3.9f, 8.1f, 5, "Brick", 4.3f);
            Wall("Boiler room wall", -36, -24, 12, 12.4f, 5); Wall("Boiler room roof", -36, -24, 0, 12.4f, 5.3f, "Slate", 5);
            PointLight("Boiler room glow", new Vector3(-30, 3.5f, 6), 10, 1.8f, new Color(1f, .5f, .25f));
            var door = WindMachine<WindGearDoor>("GearDoor", new Vector3(-24.2f, 0, 6), -90, "SP-geardoor-boiler", "the gear door");
            door.roll = 4.4f; door.radius = 2.1f; if (door.transform.childCount == 0) Box("Fallback gear door", door.transform.position + Vector3.up * 2.1f, new Vector3(.4f, 4.2f, 4.2f), "Brass").SetParent(door.transform, true);
            Socket(new Vector3(-21f, 0, 9.5f), door);
            Hint(new Vector3(-21, 0, 6), 3.5f, () => door.Powered ? "" : "A round gear door, locked tight. The brass socket beside it wants winding... (mechanical Kira)");
            // the lift to the crane deck (west wall, z 48..55): wound up, it rides up and down for good
            SolidProp("LiftShaft", -29, 50, 0, 0f);
            var liftGo = new GameObject("Lift"); liftGo.transform.SetParent(root, false); liftGo.transform.position = new Vector3(-29, .25f, 50);
            var mp = liftGo.AddComponent<MovingPlatform>(); mp.Hold(liftGo.transform.position);
            var deck = Prop("Lift", -29, 50, 0, 1f, .25f, liftGo.transform); MeshSolid(deck);
            if (!deck) Box("Fallback lift deck", new Vector3(-29, .1f, 50), new Vector3(3, .3f, 3), "IronGrate").SetParent(liftGo.transform, true);
            var lift = liftGo.AddComponent<WindLift>(); lift.id = "SP-lift"; lift.what = "the lift"; lift.bottom = new Vector3(-29, .25f, 50); lift.topAt = new Vector3(-29, 12.05f, 50); lift.period = 12f;
            Socket(new Vector3(-28f, 0, 44f), lift);
            Box("Crane deck", new Vector3(-29, 11.85f, 53.5f), new Vector3(6, .3f, 3.8f), "IronGrate");
            Box("Crane deck rail", new Vector3(-26.05f, 12.55f, 53.5f), new Vector3(.1f, 1.1f, 3.8f), "Iron"); Box("Crane deck rail", new Vector3(-29, 12.55f, 55.35f), new Vector3(6, 1.1f, .1f), "Iron");
            foreach (var x in new[] { -31.6f, -26.4f }) Box("Crane deck strut", new Vector3(x, 10.9f, 55f), new Vector3(.25f, 1.6f, .25f), "Iron", false);
            Hint(new Vector3(-28, 0, 46), 3.5f, () => lift.Powered ? "" : "A lift up to the crane deck, but no power. The socket wants winding... (mechanical Kira)");
            // the gantry in the hall (x 17..23, z 25..31, top 7) and its catwalk bridge, stowed upright at the east catwalk's edge
            Box("Gantry", new Vector3(20, 6.85f, 28), new Vector3(6, .3f, 6), "IronGrate");
            foreach (var (x, z) in new[] { (17.3f, 25.3f), (22.7f, 25.3f), (17.3f, 30.7f), (22.7f, 30.7f) }) Box("Gantry leg", new Vector3(x, 3.35f, z), new Vector3(.4f, 6.7f, .4f), "Iron");
            Box("Gantry rail", new Vector3(17.05f, 7.55f, 28), new Vector3(.1f, 1.1f, 6), "Iron"); Box("Gantry rail", new Vector3(20, 7.55f, 25.05f), new Vector3(6, 1.1f, .1f), "Iron"); Box("Gantry rail", new Vector3(20, 7.55f, 30.95f), new Vector3(6, 1.1f, .1f), "Iron");
            var hinge = new GameObject("Gantry bridge"); hinge.transform.SetParent(root, false); hinge.transform.position = new Vector3(33.5f, 7f, 28f);
            var br = Prop("CatwalkBridge", 28.25f, 28f, 90, 1f, 7f, hinge.transform);
            if (br) MeshSolid(br);   // (10 m: from the catwalk's edge (33.5) to the gantry (23))
            else Box("Fallback bridge", new Vector3(28.25f, 6.9f, 28), new Vector3(10.6f, .25f, 3), "IronGrate").SetParent(hinge.transform, true);
            var bridge = hinge.AddComponent<WindBridge>(); bridge.id = "SP-bridge"; bridge.what = "the catwalk bridge"; bridge.downRot = Quaternion.identity; bridge.upRot = Quaternion.Euler(0, 0, -88f);
            hinge.transform.rotation = bridge.upRot;
            Socket(new Vector3(34.75f, 7f, 32f), bridge);
            Hint(new Vector3(34.7f, 7, 30), 3f, () => bridge.Powered ? "" : "A folded catwalk bridge over to the gantry. The socket here would lower it... (mechanical Kira)");
            // the riveted pillars (magnet paws) with their girders at 12
            Pillar(new Vector3(20, 0, 72)); Pillar(new Vector3(-18, 0, 64));
            // the store cage (east wall, x 28..36, z 40..48): bars Muscle Man bends
            Wall("Cage wall", 28, 36, 39.6f, 40, 4.2f); Wall("Cage wall", 28, 36, 48, 48.4f, 4.2f); Wall("Cage front", 27.8f, 28.2f, 40, 42.5f, 4.2f); Wall("Cage front", 27.8f, 28.2f, 45.5f, 48, 4.2f);
            Wall("Cage front lintel", 27.8f, 28.2f, 42.5f, 45.5f, 4.2f, "Brick", 3f); Wall("Cage roof", 27.8f, 36, 39.6f, 48.4f, 4.5f, "Slate", 4.2f);
            Bend("BarGrate", "BentBars", new Vector3(28, 0, 44), 90, "SP-bars-cage", "bends the bars aside");
            // the tool nook (west wall, x -36..-30, z 30..36): the machine Muscle Man lifts aside
            Wall("Nook wall", -36, -30, 29.6f, 30, 4.2f); Wall("Nook wall", -36, -30, 36, 36.4f, 4.2f); Wall("Nook front", -30.2f, -29.8f, 29.6f, 31.8f, 4.2f); Wall("Nook front", -30.2f, -29.8f, 34.2f, 36.4f, 4.2f);
            Wall("Nook front lintel", -30.2f, -29.8f, 31.8f, 34.2f, 4.2f, "Brick", 2.6f); Wall("Nook roof", -36, -29.8f, 29.6f, 36.4f, 4.5f, "Slate", 4.2f);
            Job("CrankBox", new Vector3(-29.1f, 0, 33f), 90, "SP-crank-nook", new Vector3(2.2f, 0, 3.6f), new Vector3(1.6f, 1.8f, 2.4f), "lifts the machine aside");
            // the stamper lane (x 18..22, z 50..66): three little stampers, the big one at its end (Muscle Man holds it up)
            Wall("Lane side", 16.5f, 18, 50, 66, 2.6f, "Iron"); Wall("Lane side", 22, 23.5f, 50, 66, 2.6f, "Iron"); Wall("Lane end", 16.5f, 23.5f, 66, 66.6f, 2.6f, "Iron");
            foreach (var (z, p2) in new[] { (52f, 0f), (55.5f, .8f), (59f, 1.6f) }) MakeStamper(new Vector3(20, 0, z), 0, Vector3.back, p2, 2.4f, 3.0f);
            var big = new GameObject("Big stamper (Muscle Man)"); big.transform.SetParent(root, false); big.transform.position = new Vector3(20, 0, 62.6f);
            Placeholders.Prim(PrimitiveType.Cube, big.transform, new Vector3(20, 1.35f, 62.6f), new Vector3(3.9f, 2.7f, 1.6f), WorldArt.Mat("Iron"), false, "Big stamper").transform.position = new Vector3(20, 1.35f, 62.6f);
            JobOn(big, "SP-stamper-lane", new Vector3(0, 2.9f, 0), new Vector3(3.9f, 2.7f, 1.6f), "holds the big stamper up");
            Hint(new Vector3(20, 0, 49), 2.5f, () => "The stamper lane. Time your run... a shard waits at the end.");
            // the brick stack's shaft (north-east: x 26..28.4, z 90..96): wall-jump up to the ledge at 10.5
            Wall("Stack wall", 25.4f, 26f, 90, 96, 11.2f); Wall("Stack wall", 28.4f, 29f, 90, 96, 11.2f); Wall("Stack ledge", 26f, 28.4f, 95.4f, 96f, 10.5f, "Brick", 10.2f);
            Hint(new Vector3(27.2f, 0, 91.5f), 2f, () => SaveGame.Current.Has(Abilities.WallJump) ? $"Abi: jump at a wall, then {B(Controls.Act.Jump)} as she touches it. Bounce up!" : "A tall narrow brick shaft. Something glints at the top... (a wall jump would do it)");
            // big gears turning on the walls, a chain line overhead
            foreach (var (x, y, z, yaw) in new[] { (-35.9f, 11f, 30f, 90f), (35.9f, 11f, 64f, -90f), (-18f, 11f, 95.8f, 180f) })
            { var g = Prop("BigGear", x, z, yaw, 1.6f, y); if (g) g.AddComponent<Spin>().speed = 14f; }
            var cl = Prop("ChainLine", -6, 12, 0, 1f, 14.5f); _ = cl;
            // workers' wake-up text
            Hint(new Vector3(0, 0, 60), 0f, () => "");
        }

        static Stamper MakeStamper(Vector3 at, float yaw, Vector3 bonk, float phase, float period, float lift = 2.2f)
        {
            var go = new GameObject("Stamper"); go.transform.SetParent(root, false); go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            var fr = Prop("StamperFrame", at.x, at.z, yaw, 1f, at.y, go.transform); MeshSolid(fr);
            var headHolder = new GameObject("Head"); headHolder.transform.SetParent(go.transform, false);
            var hd = Prop("StamperHead", at.x, at.z, yaw, 1f, at.y, headHolder.transform);
            if (!hd) Placeholders.Prim(PrimitiveType.Cube, headHolder.transform, at + new Vector3(0, .3f, 0), new Vector3(1.8f, .6f, 1.4f), WorldArt.Mat("Iron"), false, "Stamper head").transform.position = at + Vector3.up * .3f;
            var s = go.AddComponent<Stamper>(); s.head = headHolder.transform; s.top = lift; s.period = period; s.phase = phase; s.bonkDir = bonk;
            return s;
        }
        static SteamJet MakeJet(Vector3 at, float phase, float hop = 1.4f)
        {
            var go = new GameObject("Steam jet"); go.transform.SetParent(root, false); go.transform.position = at;
            var j = go.AddComponent<SteamJet>(); j.phase = phase; j.hop = hop; j.period = 3.4f;
            return j;
        }
        static T WindMachine<T>(string prop, Vector3 at, float yaw, string id, string what) where T : WindUpMachine
        {
            var go = new GameObject(prop + " (wind-up)"); go.transform.SetParent(root, false); go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            var p = Prop(prop, at.x, at.z, yaw, 1f, at.y, go.transform); MeshSolid(p);
            var m = go.AddComponent<T>(); m.id = id; m.what = what;
            return m;
        }
        static void Socket(Vector3 at, WindUpMachine m)
        {
            var go = new GameObject("Gear socket"); go.transform.SetParent(root, false); go.transform.position = at;
            var plate = Prop("GearSocket", at.x, at.z, 0, 1f, at.y + .005f, go.transform);
            if (!plate) Placeholders.Prim(PrimitiveType.Cylinder, go.transform, at + Vector3.up * .04f, new Vector3(1.3f, .04f, 1.3f), WorldArt.Mat("Brass"), false, "Socket plate");
            var s = go.AddComponent<GearSocket>(); s.machine = m;
            PointLight("Socket glint", at + Vector3.up * 1.2f, 4, .8f, new Color(1f, .8f, .4f), go.transform);
        }
        static void Pillar(Vector3 at)
        {
            var p = SolidProp("MagnetPillar", at.x, at.z, 0, at.y);
            if (!p) { Box("Fallback pillar", at + Vector3.up * 6, new Vector3(.7f, 12, .7f), "Iron"); Box("Fallback girder", at + Vector3.up * 11.85f, new Vector3(3, .3f, 1.4f), "Iron"); }
            var go = new GameObject("Riveted pillar"); go.transform.SetParent(root, false); go.transform.position = at + new Vector3(0, 0, -.75f);
            var m = go.AddComponent<MagnetPillar>(); m.radius = 1.3f; m.to = at + new Vector3(0, 12.05f, 0);
            Hint(at + new Vector3(0, 0, -1.5f), 2.2f, () => "An iron pillar with a strip of big rivets, up to a girder. (mechanical Kira: Jump at its foot)");
        }
        static void Bend(string straightProp, string bentProp, Vector3 at, float yaw, string id, string verb)
        {
            var go = new GameObject(straightProp + " (Muscle Man)"); go.transform.SetParent(root, false); go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            var s = Prop(straightProp, at.x, at.z, yaw, 1f, at.y, go.transform); MeshSolid(s);
            var b = Prop(bentProp, at.x, at.z, yaw, 1f, at.y, go.transform);
            if (!s) { s = Box("Fallback bars", at + Vector3.up * 1.5f, new Vector3(3f, 3f, .3f), "Iron").gameObject; s.transform.rotation = Quaternion.Euler(0, yaw, 0); s.transform.SetParent(go.transform, true); }
            var j = go.AddComponent<BendJob>(); j.id = id; j.straight = s; j.bent = b; j.verbText = verb;
        }
        static void Job(string prop, Vector3 at, float yaw, string id, Vector3 aside, Vector3 size, string verb)
        {
            var g = new GameObject(prop + " (Muscle Man)"); g.transform.SetParent(root, false); g.transform.position = at;
            var p = WorldArt.Prop("steampunk_props", prop, g.transform, at, yaw);
            if (!p) Placeholders.Prim(PrimitiveType.Cube, g.transform, at + Vector3.up * size.y / 2, size, WorldArt.Mat("Iron"), false, prop);
            JobOn(g, id, aside, size, verb, yaw);
        }
        static void JobOn(GameObject g, string id, Vector3 aside, Vector3 size, string verb, float yaw = 0)
        {
            var col = new GameObject("Block"); col.transform.SetParent(g.transform, false); col.transform.position = g.transform.position + Vector3.up * size.y / 2; col.transform.rotation = Quaternion.Euler(0, yaw, 0);
            col.AddComponent<BoxCollider>().size = size;
            var b = g.AddComponent<Boulder>(); b.id = id; b.aside = aside; b.verbText = verb; b.lifts = true;
        }

        // ---- the yard (z 96..112): brick walls, the press's wreck (after the scene)
        static void Yard()
        {
            foreach (var z in new[] { 101f, 107f }) { SolidProp("YardWall", -30.3f, z, 90, 0f); SolidProp("YardWall", 30.3f, z, 90, 0f); }
            foreach (var x in new[] { -25f, -15f, 15f, 25f }) SolidProp("YardWall", x, 112.3f, 0, 0f);
            Wall("Yard wall", -30.6f, -30f, 96.8f, 112.6f, 3); Wall("Yard wall", 30f, 30.6f, 96.8f, 112.6f, 3); Wall("Yard wall", -30.6f, -10f, 112f, 112.6f, 3); Wall("Yard wall", 10f, 30.6f, 112f, 112.6f, 3);
            Cp(StreetCp, new Vector3(0, 0, 110), 0, false);
            Hint(new Vector3(0, 0, 100), 4f, () => "Morning in the city! The great press lies in the yard, smashed. The streets are that way.");
            foreach (var (x, z) in new[] { (-20f, 100f), (22f, 108f), (-14f, 109f) }) SolidProp("Rubble", x, z, x * 7, 0f);
        }

        // ---- houses: Victorian brick and cream terraces (front to the street)
        static GameObject House(string kind, float x, float z, float yaw)
        {
            var h = SolidProp(kind, x, z, yaw, 0f);
            if (!h)
            {
                float w = kind == "HouseNarrow" ? 6 : 10, ht = kind == "HouseCream" ? 9 : kind == "HouseNarrow" ? 14 : 12;
                var b = Box("Fallback " + kind, new Vector3(x, ht / 2, z), new Vector3(w, ht, 8), kind == "HouseCream" ? "CreamStone" : "Brick"); b.rotation = Quaternion.Euler(0, yaw, 0); h = b.gameObject;
            }
            return h;
        }
        static void Lamp(float x, float z) { SolidProp("LampPost", x, z, 0, 0f); }

        // ---- the avenue (x -10..10, z 112..176): the narrow alley west (wall jump), the pipe alley east (Muscle Man)
        static void Avenue()
        {
            House("HouseBrick", -14, 117, -90); House("HouseCream", -14, 127, -90); House("HouseNarrow", -14, 135, -90);
            House("HouseNarrow", -14, 143.4f, -90); House("HouseBrick", -14, 151.4f, -90); House("HouseCream", -14, 161.4f, -90); House("HouseBrick", -14, 171.4f, -90);
            Wall("Alley back", -18.6f, -18f, 138, 140.4f, 14);
            Hint(new Vector3(-11, 0, 139.2f), 2f, () => SaveGame.Current.Has(Abilities.WallJump) ? $"The narrow alley: Abi bounces wall to wall ({B(Controls.Act.Jump)} as she touches each wall) up to the roofs." : "A very narrow alley between two tall houses. Something shines on the roof...");
            House("HouseCream", 14, 117, 90); House("HouseBrick", 14, 127, 90); House("HouseCream", 14, 137, 90);
            House("HouseBrick", 14, 150, 90); House("HouseCream", 14, 160, 90); House("HouseBrick", 14, 170, 90);
            Wall("Pipe alley back", 18, 18.6f, 142, 145, 9);
            Bend("PipeFence", "PipeFenceBent", new Vector3(10.2f, 0, 143.5f), 90, "SP-pipes-alley", "bends the pipes apart");
            for (int i = 0; i < 6; i++) { Lamp(-9.2f, 118 + i * 11); Lamp(9.2f, 123 + i * 11); }
            foreach (var (x, z) in new[] { (-8.5f, 130f), (8.6f, 156f) }) SolidProp("Bench", x, z, x < 0 ? -90 : 90, 0f);
            foreach (var (x, z) in new[] { (-8.4f, 147f), (8.4f, 132f), (8.2f, 133f) }) SolidProp("Barrel", x, z, x * 9, 0f);
        }

        // ---- the square (x -40..40, z 178..222): the great clock, the carriage loop, the Violinist, horses
        static void Square(WorldProgress prog)
        {
            Cp(VortexCp, new Vector3(-10, 0, 184), 0, false);
            // Fix 11.1: Dan's Tripo clock (static, its face toward the avenue: -z); the Blender one if it's missing
            var clock = StaticModel.Spawn("street_clock", root, ClockAt, 180f, 7.5f) ?? Prop("StreetClock", ClockAt.x, ClockAt.z, 0, 1f, 0f);
            Solid(ClockAt, new Vector3(3f, 1f, 3f)); Solid(ClockAt, new Vector3(1.3f, 8f, 1.3f));
            if (!clock) { Box("Fallback clock column", ClockAt + Vector3.up * 3.5f, new Vector3(1.2f, 7, 1.2f), new Color(.12f, .32f, .25f)); Box("Fallback clock head", ClockAt + Vector3.up * 7f, new Vector3(2.6f, 2.6f, 2.6f), "Brass"); }
            PointLight("Clock glow", ClockAt + new Vector3(0, 6.4f, -2.2f), 6, 1.2f, Teal);
            var reach = new GameObject("Clock's foot"); reach.transform.SetParent(root, false); reach.transform.position = ClockAt + new Vector3(0, 0, -2.6f); reach.AddComponent<ClockReach>();
            // the rail loop's carriages (their roofs are moving platforms)
            var loop = new[] { new Vector3(-22, 0, 178), new Vector3(22, 0, 178), new Vector3(22, 0, 222), new Vector3(-22, 0, 222) };
            for (int i = 0; i < 2; i++) MakeCarriage(loop, i * 88f);
            // houses round the square (south corners, east and west sides with their streets, north row with the crate passage)
            House("HouseBrick", -35, 172, 180); House("HouseCream", -25, 172, 180); House("HouseCream", 25, 172, 180); House("HouseBrick", 35, 172, 180);
            House("HouseBrick", -44, 183, -90); House("HouseCream", -44, 215, -90);
            House("HouseCream", 44, 183, 90); House("HouseBrick", 44, 215, 90);
            House("HouseBrick", -35, 230, 0); House("HouseNarrow", -24, 230, 0); House("HouseCream", -16, 230, 0);
            House("HouseCream", 15, 230, 0); House("HouseBrick", 25, 230, 0); House("HouseBrick", 35, 230, 0);
            // the crate passage (x -30..-27, behind it z 234..241)
            Wall("Passage wall", -30.6f, -30, 234, 241.6f, 6); Wall("Passage wall", -27f, -26.4f, 234, 241.6f, 6); Wall("Passage end", -30.6f, -26.4f, 241, 241.6f, 6);
            var crates = new GameObject("Crate stack"); crates.transform.SetParent(root, false); crates.transform.position = new Vector3(-28.5f, 0, 227.2f);
            for (int i = 0; i < 6; i++) { var c = WorldArt.Prop("steampunk_props", "Crate", crates.transform, new Vector3(-28.5f + (i % 3 - 1) * 1.0f, (i / 3) * 1.0f, 227.2f)); if (!c) Placeholders.Prim(PrimitiveType.Cube, crates.transform, new Vector3(-28.5f + (i % 3 - 1), .5f + i / 3, 227.2f), Vector3.one, WorldArt.Mat("Wood"), false, "Crate"); }
            JobOn(crates, "SP-crates-passage", new Vector3(3.6f, 0, -2.4f), new Vector3(3f, 2f, 1f), "lifts the crates aside");
            // Muscle Man's boost onto the cream house's roof (44, 183: roof at 9)
            var boost = new GameObject("Boost spot (roof)"); boost.transform.SetParent(root, false); boost.transform.SetPositionAndRotation(new Vector3(36.5f, 0, 183), Quaternion.Euler(0, 90, 0));
            var bs = boost.AddComponent<BoostSpot>(); bs.radius = 1.6f; bs.height = 11.2f; bs.landAt = new Vector3(44f, 9.05f, 183f);
            Hint(new Vector3(36, 0, 186), 2.5f, () => "That roof is high... (Muscle Man could throw you up there)");
            // lamps, benches round the clock, the Violinist on the south-east corner (a street musician)
            foreach (var (x, z) in new[] { (-30f, 180f), (30f, 220f), (-30f, 220f), (12f, 196f), (-12f, 204f), (-6f, 188f) }) Lamp(x, z);
            foreach (var (x, z, yaw) in new[] { (-7f, 196f, 90f), (7f, 204f, -90f), (0f, 207f, 180f) }) SolidProp("Bench", x, z, yaw, 0f);
            var v = Violinist.Make(root, ViolinistAt, -59f, "SteamViolin", Game.World.violinistPlayClip);
            v.ability = Abilities.WallJump; v.learner = "Abi"; v.moveName = "Wall Jump";
            Hint(ViolinistAt + new Vector3(-2.5f, 0, 1.5f), 4f, () => SaveGame.Current.Has(Abilities.WallJump) ? "The wall jump: Abi bounces off walls, up narrow alleys and chimneys (the Medieval village's chimney, the Ice Age's glacier chimneys too)." : "A street musician with a gleaming brass violin, playing to the morning crowd...");
            // the clockwork horses: one walking its round, one by the west side (Muscle Man's pet)
            Horse(new[] { new Vector3(-28, 0, 182), new Vector3(28, 0, 182), new Vector3(28, 0, 218), new Vector3(-28, 0, 218) }, false, Vector3.zero, 0);
            var gag = Horse(null, true, new Vector3(-34f, 0, 190f), 90);
            var hg = new GameObject("Horse gag"); hg.transform.SetParent(root, false); hg.transform.SetPositionAndRotation(new Vector3(-34f, 0, 190f), Quaternion.Euler(0, 90, 0));
            var g2 = hg.AddComponent<HorseGag>(); g2.horse = gag;
            // the vortex gate (after the teal shard: the way back)
            Gate = Playground.PortalHere("SP-vortex-gate", new Vector3(-10, 0, 188), 0, Vortex.Checkpoint, false, root, Teal, vortex: true);
            Gate.gameObject.SetActive(prog.storyShard);
            Hint(new Vector3(0, 0, 186), 6f, () => SaveGame.Current.World(Game.World.worldId).necklaceAwake ? "" : "The great clock of the square. Its face... is that a teal gear, right in the middle?");
        }

        static void MakeCarriage(Vector3[] loop, float startAt)
        {
            var go = new GameObject("Steam carriage"); go.transform.SetParent(root, false);
            var mp = go.AddComponent<MovingPlatform>(); mp.path = loop; mp.speed = 3.2f; mp.startDistance = startAt; mp.turnDegPerSec = 70f;
            var c = Prop("SteamCarriage", 0, 0, 0, 1f, 0f, go.transform);
            if (c) { c.transform.localPosition = Vector3.zero; c.transform.localRotation = Quaternion.Euler(0, 180, 0) * c.transform.localRotation; foreach (var mf in c.GetComponentsInChildren<MeshFilter>()) if (mf.name.StartsWith("CarriageRoof")) { var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; } }
            else Placeholders.Prim(PrimitiveType.Cube, go.transform, new Vector3(0, 2.7f, 0), new Vector3(2f, .2f, 3.6f), WorldArt.Mat("Copper"), true, "Fallback carriage roof").transform.localPosition = new Vector3(0, 2.7f, 0);
            var body = new GameObject("Carriage body"); body.transform.SetParent(go.transform, false); body.transform.localPosition = new Vector3(0, 1.25f, 0);
            body.AddComponent<BoxCollider>().size = new Vector3(2.1f, 2.5f, 3.9f);
            go.AddComponent<Carriage>();
        }

        static Transform Horse(Vector3[] path, bool still, Vector3 at, float yaw)
        {
            var go = new GameObject(still ? "Clockwork horse (still)" : "Clockwork horse"); go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(path != null ? path[0] : at, Quaternion.Euler(0, yaw, 0));
            string model = CharacterModel.Exists("clockwork_horse") ? "clockwork_horse" : "horse";
            var anim = CharacterModel.Creature(go.transform, model, 1.4f, 3f);
            if (anim && model == "horse") foreach (var r in go.GetComponentsInChildren<Renderer>()) { var m = new Material(r.sharedMaterial); m.color = new Color(.95f, .62f, .4f); if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", .6f); if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", .6f); r.sharedMaterial = m; }   // copper (the pony's rig until its own model is in)
            if (!anim) Placeholders.Prim(PrimitiveType.Capsule, go.transform, new Vector3(0, 1f, 0), new Vector3(.8f, 1f, 1.6f), WorldArt.Mat("Copper"), name: "Fallback horse");
            var w = go.AddComponent<HorseWalk>(); w.path = path; w.still = still; w.anim = anim; w.speed = 1.4f;
            var smoke = new GameObject("Chimney puffs"); smoke.transform.SetParent(go.transform, false); smoke.transform.localPosition = new Vector3(0, 2.0f, -.3f);
            return go.transform;
        }

        // ---- the west street (x -100..-40, z 190..210): the carriage turntable (wind-up), the signal tower (magnet paws)
        static void WestStreet(WorldProgress prog)
        {
            foreach (var x in new[] { -55f, -65f, -75f, -85f, -95f }) { House(x == -65f || x == -85f ? "HouseCream" : "HouseBrick", x, 214, 0); House(x == -55f || x == -75f ? "HouseCream" : "HouseBrick", x, 186, 180); }
            Wall("West street end", -100.6f, -100, 186, 214, 4, "Brick");
            var tt = new GameObject("Turntable"); tt.transform.SetParent(root, false); tt.transform.position = new Vector3(-60, 0, 200);
            var mp = tt.AddComponent<MovingPlatform>(); mp.Hold(tt.transform.position);
            var tp = Prop("Turntable", -60, 200, 0, 1f, 0f, tt.transform); MeshSolid(tp);
            if (!tp) Placeholders.Prim(PrimitiveType.Cylinder, tt.transform, new Vector3(-60, .15f, 200), new Vector3(8f, .15f, 8f), WorldArt.Mat("IronGrate"), false, "Fallback turntable").AddComponent<MeshCollider>();
            var parked = StaticModel.Spawn("steam_carriage", tt.transform, new Vector3(-60, .3f, 200), 90f, 3.4f) ?? Prop("SteamCarriage", -60, 200, 90, 1f, .3f, tt.transform);
            _ = parked;
            var turn = tt.AddComponent<WindTurntable>(); turn.id = "SP-turntable"; turn.what = "the carriage turntable"; turn.spin = 14f;
            Socket(new Vector3(-52f, 0, 195f), turn);
            Hint(new Vector3(-54, 0, 198), 3f, () => turn.Powered ? "" : "A carriage turntable, stuck. Its gear socket wants winding... (mechanical Kira)");
            Pillar(new Vector3(-80, 0, 204.75f));
            foreach (var x in new[] { -50f, -70f, -90f }) { Lamp(x, 191f); Lamp(x + 5, 209f); }
        }

        // ---- the east street (x 40..70, z 190..210) and the great gear gate to the clockmaker's yard (x 72..98, z 186..214)
        static void EastStreet(WorldProgress prog)
        {
            foreach (var x in new[] { 53f, 63f }) { House(x == 53f ? "HouseBrick" : "HouseCream", x, 214, 0); House(x == 53f ? "HouseCream" : "HouseBrick", x, 186, 180); }
            var frame = SolidProp("GearGateFrame", 70.5f, 200, 90, 0f);
            if (!frame) { Wall("Fallback gate frame", 70, 71, 186, 195.3f, 12, "CreamStone"); Wall("Fallback gate frame", 70, 71, 204.7f, 214, 12, "CreamStone"); Wall("Fallback gate lintel", 70, 71, 195.3f, 204.7f, 12, "CreamStone", 9.4f); }
            Wall("Gate side wall", 69.6f, 71.4f, 186, 192.3f, 12, "CreamStone"); Wall("Gate side wall", 69.6f, 71.4f, 207.7f, 214, 12, "CreamStone");
            var gate = WindMachine<WindGearDoor>("GearGate", new Vector3(70.5f, 0, 200), 90, "SP-geargate", "the great gear gate");
            gate.roll = 9.8f; gate.radius = 4.5f;
            if (gate.transform.childCount == 0) Box("Fallback gear gate", gate.transform.position + Vector3.up * 4.5f, new Vector3(.8f, 9f, 9f), "Brass").SetParent(gate.transform, true);
            Socket(new Vector3(64f, 0, 195f), gate);
            Hint(new Vector3(63, 0, 200), 4f, () => gate.Powered ? "" : "The great gear gate of the clockmaker's yard. The brass socket at its foot wants winding... (mechanical Kira)");
            // the clockmaker's yard: walls round it, gears and crates
            Wall("Clockmaker's wall", 71.4f, 98.6f, 185.4f, 186, 5, "Brick"); Wall("Clockmaker's wall", 71.4f, 98.6f, 214, 214.6f, 5, "Brick"); Wall("Clockmaker's wall", 98, 98.6f, 186, 214, 5, "Brick");
            foreach (var (x, z, yaw) in new[] { (95f, 192f, 0f), (92f, 210f, 40f), (86f, 212f, 0f) }) { var g = Prop("BigGear", x, z, yaw, 1f, 1.5f); if (g) g.AddComponent<Spin>().speed = 25f; }
            foreach (var (x, z) in new[] { (96f, 200f), (96f, 201.2f) }) SolidProp("Crate", x, z, 0, 0f);
            Hint(new Vector3(84, 0, 200), 5f, () => "The clockmaker's yard: gears everywhere, ticking.");
            Lamp(50, 191); Lamp(60, 209);
            // the lamp tower in the square's north-east (magnet paws)
            Pillar(new Vector3(34, 0, 216.75f));
        }

        // ---- the north street (x -10..10, z 222..300)
        static void NorthStreet()
        {
            foreach (var z in new[] { 245f, 255f, 265f, 275f, 285f, 295f }) { House(z % 20 == 5 ? "HouseCream" : "HouseBrick", -14, z, -90); House(z % 20 == 5 ? "HouseBrick" : "HouseCream", 14, z, 90); }
            Wall("North street end", -10, 10, 300, 300.6f, 4, "Brick");
            for (int i = 0; i < 4; i++) { Lamp(-9.2f, 248 + i * 14); Lamp(9.2f, 255 + i * 14); }
        }

        // Fix 11.1: Dan's Tripo models where they stand still: parked carriages, smaller presses in the factory (each with a box collider)
        static void TripoDecor()
        {
            foreach (var (x, z, yaw) in new[] { (22f, 104f, 90f), (-56f, 207f, 0f), (54f, 193f, 180f) })
                MeshSolidAll(StaticModel.Spawn("steam_carriage", root, new Vector3(x, 0, z), yaw, 3.4f));
            foreach (var (x, z, yaw) in new[] { (-26f, 90.5f, 180f), (8f, 14f, 90f) })
                MeshSolidAll(StaticModel.Spawn("steam_press", root, new Vector3(x, 0, z), yaw, 4f));
        }
        // a static model solid as drawn (its own mesh: the press is open between its pillars)
        static void MeshSolidAll(GameObject go)
        {
            if (!go) return;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>()) { var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; }
        }

        static void Enemies()
        {
            Enemy.Worker(root, new Vector3(6, .1f, 36), new Vector3(14, .1f, 48));
            Enemy.Worker(root, new Vector3(-14, .1f, 58), new Vector3(-6, .1f, 72));
            Enemy.Worker(root, new Vector3(8, .1f, 78), new Vector3(16, .1f, 88));
            Enemy.Worker(root, new Vector3(-20, .1f, 100), new Vector3(-8, .1f, 108));   // the yard
        }
    }

    public sealed class SteampunkWatch : MonoBehaviour
    {
        bool hidden;
        void Update()
        {
            Steampunk.Watch();
            bool hide = Game.InCutscene;   // the workers stay out of the cutscenes' shots
            if (hide != hidden) { hidden = hide; foreach (var e in Enemy.All) if (e && e.kind == Enemy.Kind.Worker) foreach (var r in e.GetComponentsInChildren<Renderer>(true)) r.enabled = !hide; }
        }
    }
}
