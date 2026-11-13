using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 13: the Moon, world 8, reached by the rocket from Tomorrowland (Dan's moon.mp3). Low gravity everywhere (one LowGravityVolume:
    // gravity x .35, take-off x .8: jumps ~1.8x higher and floatier). Built in code: a cratered heightfield (600 x 600 m) of gray dust, Earth
    // huge in a deep blue sky, stars.
    //   the landing site (round (0, 0)): the landed rocket (Tomorrowland's model) at (0, 24), the flat pad round it,
    //   the crater field east (x 40..160): bouncy crater rims (MoonBounce), the deep crater (100, 20: r 22, 8 deep, its sides walkable),
    //     cave A behind a moon boulder (150, -60), the float pillars (space Kira floats up: 7.6 m),
    //   the moon base north-west (hub dome (-90, 80) r 12, garden dome (-90, 114) r 10, lab dome (-60, 80) r 9, corridors; the airlock south of
    //     the hub (z 58..68): Muscle Man carries its jammed outer door aside), cave B (-20, 100), the Violinist on a rock by the base,
    //   the mesa (30, -90: 10 m, Muscle Man's boost) and the ridge south-west up to the rover's plateau (-60, -90: 16 m): the lunar rover, its
    //     control panel (the light puzzle: only Abi solves it), the white story shard on the rover. KiraFloats plays once at the ridge's foot.
    public static class Moon
    {
        public static bool Real => true;
        static Transform root;
        public const string Start = "MO-start", VortexCp = "MO-vortex", ArrivedFlag = "MO-arrived", FloatsFlag = "MO-floats", PuzzleFlag = "MO-puzzle";
        public static readonly Vector3 RocketAt = new Vector3(0, 0, 24f), ArriveAt = new Vector3(0, 0, 8f);
        public static readonly Vector3 RoverAt = new Vector3(-60f, 0, -90f), PanelAt = new Vector3(-60f, 0, -83.6f), ViolinistAt = new Vector3(-58f, 0, 46f);
        public static readonly Vector3 FloatsAt = new Vector3(-27f, 0, -55f);   // the ridge's foot (its ramp comes down to the north-east)
        public const float PlateauY = 16f, MesaY = 10f, PillarY = 7.6f;
        public static readonly Color White = new Color(.95f, .97f, 1f);
        public static Portal Gate { get; private set; }
        public static Vector3 RocketGround => new Vector3(RocketAt.x, Height(RocketAt.x, RocketAt.z), RocketAt.z);   // (not a raycast: it would land on the rocket's top)
        public static GameObject RoverModel { get; private set; }
        public static RoverPanel Panel { get; private set; }
        public static GameObject LandedRocket { get; private set; }

        // the ground: craters (centre x, z, radius, depth, rim height), the plateau and the mesa
        static readonly (float x, float z, float r, float d, float rim)[] Craters =
        {
            (100, 20, 22, 8, 2.2f), (60, -30, 10, 3, 1.6f), (80, 60, 12, 4, 2f), (130, -20, 9, 3, 1.5f), (140, 50, 8, 2.5f, 1.3f), (55, 30, 7, 2, 1.2f),
            (-20, -40, 9, 2.5f, 1.3f), (30, 70, 8, 2, 1.1f), (-130, -10, 14, 3, 1.8f), (20, -140, 12, 3, 1.6f), (-150, 150, 16, 4, 2f), (160, -130, 18, 5, 2.4f),
        };
        public static float Height(float x, float z)
        {
            float h = .22f * Mathf.Sin(x * .11f) * Mathf.Cos(z * .13f) + .15f * Mathf.Sin(x * .31f + z * .17f);
            // the landing site and the base are flat
            float flat = Mathf.Min(Dist(x, z, 0, 14) / 30f, Dist(x, z, -80, 92) / 46f, Dist(x, z, -60, -86) / 18f);
            h *= Mathf.Clamp01(flat - .6f);
            foreach (var c in Craters)
            {
                float d = Dist(x, z, c.x, c.z) / c.r;
                if (d < 1f) h += -c.d * (1f - d * d);
                h += c.rim * Mathf.Exp(-Mathf.Pow((d - 1f) / .16f, 2));
            }
            // the rover's plateau (16 m) with a walkable ramp north-east (the ridge) and the mesa (10 m, cliffs: Muscle Man's boost)
            float pd = Dist(x, z, RoverAt.x, RoverAt.z);
            if (pd < 14f) h = Mathf.Max(h, PlateauY);
            else
            {
                // the ridge: a walkable ramp (23 deg) down toward the north-east; cliffs everywhere else
                float ramp = Mathf.Clamp01(1f - (pd - 14f) / 38f); ramp = ramp * ramp * (3 - 2 * ramp);
                float cliff = Mathf.Clamp01(1f - (pd - 14f) / 4f);
                float ang = Mathf.Atan2(z - RoverAt.z, x - RoverAt.x) * Mathf.Rad2Deg;
                float onRidge = Mathf.Clamp01(1f - Mathf.Abs(Mathf.DeltaAngle(ang, 45f)) / 30f);
                h = Mathf.Max(h, PlateauY * Mathf.Lerp(cliff, ramp, onRidge));
            }
            float md = Dist(x, z, 30, -90);
            if (md < 8f) h = Mathf.Max(h, MesaY); else if (md < 9.5f) h = Mathf.Max(h, MesaY * (1f - (md - 8f) / 1.5f));
            return h;
        }
        static float Dist(float x, float z, float cx, float cz) => Mathf.Sqrt((x - cx) * (x - cx) + (z - cz) * (z - cz));

        public static Transform Build(WorldData w)
        {
            root = new GameObject("World The Moon").transform;
            var save = SaveGame.Current; var prog = save.World(w.worldId);
            var groundMesh = ProcMesh.Heightfield(600f, 240, Height, 6f);
            var ground = ProcMesh.Object("Moon ground", groundMesh, WorldArt.Mat("MoonDust"), root, Vector3.zero, true);
            ground.GetComponent<MeshRenderer>().receiveShadows = true;
            Physics.SyncTransforms();
            // low gravity everywhere
            var lg = new GameObject("Low gravity"); lg.transform.SetParent(root, false); lg.transform.position = new Vector3(0, 60, 0); lg.transform.localScale = new Vector3(640, 260, 640);
            var lgv = lg.AddComponent<LowGravityVolume>(); lgv.gravityScale = .35f; lgv.jumpScale = .8f;
            Sky(); Landing(prog); CraterField(); Base(prog); Ridge(prog); Rocks();
            foreach (var (c, s) in new[] { (new Vector3(0, 60, -302), new Vector3(620, 200, 4)), (new Vector3(0, 60, 302), new Vector3(620, 200, 4)), (new Vector3(-302, 60, 0), new Vector3(4, 200, 620)), (new Vector3(302, 60, 0), new Vector3(4, 200, 620)) })
                Box("World boundary", c, s, Color.clear, "Water").GetComponent<Renderer>().enabled = false;
            Physics.SyncTransforms();
            foreach (var s in w.shards)
                if (!prog.shards.Contains(s.id)) Pickup.Make(Pickup.Kind.Shard, s.id, s.area, ShardPos(s.position), root, new Color(.85f, .9f, 1f));
            PlayLog.Line("Moon shards placed: " + string.Join(" ", w.shards.Select(s => $"{s.id}{Fmt(ShardPos(s.position))}")));
            var a = w.Area("MO");
            // the white story shard: on the rover (the necklace points at it; the panel's light puzzle frees it: RoverPanel)
            var story = Pickup.Make(Pickup.Kind.StoryShard, "story-" + w.worldId, a.id, a.storyShard, root, a.storyShardColor);
            foreach (var c in story.GetComponentsInChildren<Collider>()) c.enabled = false;
            story.radius = .01f;
            Game.I.StoryShard = story;
            Game.I.StoryBeam = Box("Story beam", a.storyShard + new Vector3(0, 30f, 0), new Vector3(.4f, 60f, .4f), White, "Glow", false).gameObject;
            bool awake = prog.necklaceAwake && !prog.storyShard;
            story.gameObject.SetActive(awake); Game.I.StoryBeam.SetActive(awake);
            if (!save.heartUpgrades.Contains("MO-H1")) Pickup.Make(Pickup.Kind.HeartUpgrade, "MO-H1", "MO", new Vector3(-86f, 1.2f, 118f), root, Playground.Accent);
            if (!save.heartUpgrades.Contains("MO-H2")) Pickup.Make(Pickup.Kind.HeartUpgrade, "MO-H2", "MO", new Vector3(32f, MesaY + 1.2f, -92f), root, Playground.Accent);
            foreach (var h in new[] { new Vector3(6, 0, 0), new Vector3(70, 0, 0), new Vector3(-60, 0, 40), new Vector3(-30, 0, -40), new Vector3(110, 0, -40) })
                Pickup.HealHeart(OnGround(h.x, h.z) + Vector3.up * 1f, root);
            Enemies();
            return root;
        }

        public static Vector3 ShardPos(Vector3 hint) => new Vector3(hint.x, Ground(hint.x, hint.z, hint.y + 1.6f) + 1f, hint.z);
        static string Fmt(Vector3 v) => $"({v.x:0},{v.y:0.0},{v.z:0})";
        const int GroundMask = ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << Hero.EnemyLayer) | (1 << 4) | (1 << 2));
        public static float Ground(float x, float z, float fromY = 80f) =>
            Physics.Raycast(new Vector3(x, fromY, z), Vector3.down, out var hit, 300f, GroundMask, QueryTriggerInteraction.Ignore) ? hit.point.y : Height(x, z);
        public static Vector3 OnGround(float x, float z, float fromY = 80f) => new Vector3(x, Ground(x, z, fromY), z);
        static Transform Box(string name, Vector3 centre, Vector3 size, string mat, bool collider = true) =>
            Placeholders.Prim(PrimitiveType.Cube, root, centre, size, WorldArt.Mat(mat), collider, name).transform;
        static Transform Box(string name, Vector3 centre, Vector3 size, Color c, string kind = "Lit", bool collider = true) =>
            Placeholders.Prim(PrimitiveType.Cube, root, centre, size, Placeholders.Mat(c, kind), collider, name).transform;
        static Transform Slab(string name, float x0, float x1, float z0, float z1, float top, float bottom, string mat) =>
            Box(name, new Vector3((x0 + x1) / 2, (top + bottom) / 2, (z0 + z1) / 2), new Vector3(x1 - x0, top - bottom, z1 - z0), mat);
        static GameObject Cyl(string name, Vector3 baseAt, float r, float h, string mat, bool collider = true)
        {
            var g = Placeholders.Prim(PrimitiveType.Cylinder, root, baseAt + Vector3.up * h / 2, new Vector3(r * 2, h / 2, r * 2), WorldArt.Mat(mat), false, name);
            if (collider) { var mc = g.AddComponent<MeshCollider>(); mc.sharedMesh = g.GetComponent<MeshFilter>().sharedMesh; mc.convex = true; }
            return g;
        }
        static void Hint(Vector3 at, float r, System.Func<string> text)
        {
            var go = new GameObject("Hint"); go.transform.SetParent(root, false); go.transform.position = at;
            var h = go.AddComponent<HintZone>(); h.radius = r; h.text = text;
        }
        static string B(Controls.Act a) => Controls.Btn(a);
        static Checkpoint Cp(string id, Vector3 at, float yaw, bool visible = true) => Playground.CheckpointHere(id, OnGround(at.x, at.z), yaw, root, visible);
        static Light PointLight(string name, Vector3 at, float range, float intensity, Color c)
        {
            var l = new GameObject(name); l.transform.SetParent(root, false); l.transform.position = at;
            var lt = l.AddComponent<Light>(); lt.type = LightType.Point; lt.range = range; lt.intensity = intensity; lt.color = c; lt.shadows = LightShadows.None; return lt;
        }
        static GameObject Rock(Vector3 at, Vector3 size, float yaw, bool solid = true)
        {
            var g = Placeholders.Prim(PrimitiveType.Sphere, root, OnGround(at.x, at.z) + Vector3.up * (size.y * .35f + at.y), size, WorldArt.Mat("MoonRock"), solid, "Moon rock");
            g.transform.rotation = Quaternion.Euler(Random.Range(-8f, 8f), yaw, Random.Range(-8f, 8f)); return g;
        }

        // ---- Earth huge in the deep blue sky, the stars
        static void Sky()
        {
            StarDome.Make(root, 1400, 2600f);
            var earth = new GameObject("Earth"); earth.transform.SetParent(root, false); earth.transform.position = new Vector3(260f, 620f, 1700f);
            Placeholders.Prim(PrimitiveType.Sphere, earth.transform, Vector3.zero, Vector3.one * 520f, WorldArt.Mat("EarthBlue"), false, "Earth sea");
            var land = Placeholders.Mat(new Color(.35f, .75f, .4f), "Glow"); var cloud = Placeholders.Mat(new Color(.97f, .98f, 1f), "Glow");
            var rnd = new System.Random(8);
            for (int i = 0; i < 14; i++)
            {
                var d = new Vector3((float)rnd.NextDouble() - .5f, (float)rnd.NextDouble() - .5f, -(float)rnd.NextDouble() * .6f - .4f).normalized;
                var p = Placeholders.Prim(PrimitiveType.Sphere, earth.transform, d * 236f, new Vector3(90f + (float)rnd.NextDouble() * 70f, 28f, 70f + (float)rnd.NextDouble() * 60f), i % 3 == 0 ? land : cloud, false, i % 3 == 0 ? "Earth land" : "Earth cloud");
                p.transform.rotation = Quaternion.LookRotation(d) * Quaternion.Euler(90, (float)rnd.NextDouble() * 180f, 0);
            }
            Placeholders.Prim(PrimitiveType.Sphere, earth.transform, Vector3.zero, Vector3.one * 560f, Placeholders.Mat(new Color(.45f, .7f, 1f, .18f), "Water"), false, "Earth glow");
            var sp = earth.AddComponent<Spin>(); sp.axis = Vector3.up; sp.speed = 1.2f;
        }

        // ---- the landing site: the landed rocket (Tomorrowland's model), its ladder, footprints
        static void Landing(WorldProgress prog)
        {
            Cp(Start, new Vector3(0, 0, 6), 0, false);
            LandedRocket = StaticModel.Spawn("rocket", root, RocketGround, 0, 24f);
            if (LandedRocket) foreach (var mf in LandedRocket.GetComponentsInChildren<MeshFilter>()) { var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; }
            else Cyl("Rocket", RocketAt, 2.4f, 20f, "Chrome");
            // the ladder down from the hatch (cosmetic: the scene's), scorch marks round the fins
            var ladder = Box("Ladder", RocketGround + new Vector3(0, 3.4f, -2.6f), new Vector3(.8f, 6.8f, .1f), "ChromeDark", false);
            var scorch = Cyl("Scorch", RocketGround + Vector3.up * .02f, 10f, .02f, "MoonRock", false);
            if (LandedRocket) { ladder.SetParent(LandedRocket.transform, true); scorch.transform.SetParent(LandedRocket.transform, true); }   // (hidden with it while MoonLanding brings its own)
            for (int i = 0; i < 4; i++) Box("Footprint", OnGround(-.6f + (i % 2) * 1.2f, 4 + i * 1.6f) + Vector3.up * .03f, new Vector3(.3f, .02f, .5f), "MoonRock", false);
            Hint(new Vector3(0, 0, 4), 6f, () => $"The Moon! Everything is light here: jump and drift. Kira floats (hold {B(Controls.Act.Jump)} in the air).");
            Gate = Playground.PortalHere("MO-vortex-gate", OnGround(-14, 12), 90, Vortex.Checkpoint, false, root, White, vortex: true);
            Gate.gameObject.SetActive(prog.storyShard);
            Cp(VortexCp, new Vector3(-10, 0, 12), 90, false);
            // a flag (Dan's idea of a souvenir): the pink and teal pennant of Tomorrowland
            Cyl("Flag pole", OnGround(8, 10), .05f, 3f, "Chrome", false);
            Box("Flag", OnGround(8, 10) + new Vector3(.6f, 2.6f, 0), new Vector3(1.2f, .7f, .02f), "NeonPink", false);
        }

        // ---- the crater field: bouncy rims, the deep crater, cave A behind a boulder, the float pillars
        static void CraterField()
        {
            Cp("MO-crater", new Vector3(60, 0, 0), 90, true);
            // bouncy crater rims: pads on the rims of three craters (land on one: a big floaty bounce)
            foreach (var (cx, cz, r) in new[] { (60f, -30f, 10f), (80f, 60f, 12f), (130f, -20f, 9f) })
                for (int i = 0; i < 4; i++)
                {
                    float a = i * Mathf.PI / 2 + .4f; var p = new Vector3(cx + Mathf.Cos(a) * r, 0, cz + Mathf.Sin(a) * r);
                    var pad = Placeholders.Prim(PrimitiveType.Cylinder, root, OnGround(p.x, p.z) + Vector3.up * .1f, new Vector3(2.2f, .1f, 2.2f), Placeholders.Mat(new Color(.75f, .6f, 1f), "Glow"), true, "Bouncy rim");
                    pad.AddComponent<MoonBounce>();
                }
            Hint(new Vector3(60, 0, -18), 5f, () => "The glowing crater rims are springy: land on one!");
            // cave A (150, -60): a ring of rocks, its mouth west behind a moon boulder (Muscle Man)
            Cave(new Vector3(150, 0, -60), 180f, "MO-boulder-a");
            Cave(new Vector3(-20, 0, 100), 90f, "MO-boulder-b");
            // the float pillars: three rock columns 7.6 m tall (space Kira floats up; nobody else reaches)
            foreach (var p in new[] { new Vector3(110, 0, 70), new Vector3(150, 0, 10), new Vector3(70, 0, -70) })
            {
                var g = OnGround(p.x, p.z);
                Cyl("Float pillar", g + Vector3.down * 1f, 1.6f, PillarY + 1f - .0f, "MoonRock");
                Cyl("Float pillar cap", g + Vector3.up * (PillarY - .2f), 1.6f, .2f, "MoonRock");   // (no lip: Kira floats up along the column)
                Box("Float pillar glow", g + Vector3.up * (PillarY + .02f), new Vector3(1.2f, .02f, 1.2f), "WhiteShard", false);
            }
            Hint(new Vector3(110, 0, 64), 4f, () => SaveGame.Current.World(Game.World.worldId).opened.Contains(FloatsFlag) ? $"A tall rock column... Kira can float up it (hold {B(Controls.Act.Jump)} in the air)." : "A tall rock column, too high to jump...");
        }
        static void Cave(Vector3 at, float mouthYaw, string id)
        {
            var g = OnGround(at.x, at.z);
            for (int i = 0; i < 14; i++)
            {
                float a = i * Mathf.PI * 2 / 14; float deg = a * Mathf.Rad2Deg;
                if (Mathf.Abs(Mathf.DeltaAngle(deg, mouthYaw)) < 14f) continue;   // the mouth
                var p = g + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 5.5f;
                var w = Box("Cave wall", p + Vector3.up * 3f, new Vector3(2.8f, 6f, 1.4f), "MoonRock"); w.rotation = Quaternion.Euler(0, -deg + 90, 0);
            }
            Cyl("Cave roof", g + Vector3.up * 6f, 6.6f, .8f, "MoonRock");
            PointLight("Cave glow", g + Vector3.up * 2f, 7f, 1.2f, new Color(.7f, .6f, 1f));
            var mouth = g + Quaternion.Euler(0, -mouthYaw, 0) * new Vector3(5.6f, 0, 0);
            var bg = new GameObject("Moon boulder (Muscle Man)"); bg.transform.SetParent(root, false); bg.transform.position = mouth;
            var vis = Placeholders.Prim(PrimitiveType.Sphere, bg.transform, new Vector3(0, 1.5f, 0), new Vector3(3.4f, 3.1f, 3.2f), WorldArt.Mat("MoonRock"), false, "Boulder");
            var col = new GameObject("Block"); col.transform.SetParent(bg.transform, false); col.transform.localPosition = new Vector3(0, 1.5f, 0); col.AddComponent<SphereCollider>().radius = 1.6f;
            var b = bg.AddComponent<Boulder>(); b.id = id; b.lifts = true; b.verbText = "lifts the moon boulder aside";
            var outDir = (mouth - g); outDir.y = 0; b.aside = Quaternion.Euler(0, 90, 0) * outDir.normalized * 4.2f;
            _ = vis;
        }

        // ---- the moon base: the airlock (Muscle Man carries its jammed door aside), the hub, the garden and the lab, corridors
        static readonly Vector3 HubAt = new Vector3(-90, 0, 80), GardenAt = new Vector3(-90, 0, 114), LabAt = new Vector3(-60, 0, 80);
        static void Base(WorldProgress prog)
        {
            Cp("MO-base", new Vector3(-90, 0, 50), 0, true);
            float fy = .15f;   // (a low floor: the motor steps 0.35 m at most, and the dust is uneven)
            Slab("Base floor", -104, -76, 66, 94, fy, -.6f, "BaseFloor");                // the hub
            Slab("Garden floor", -101, -79, 103, 125, fy, -.6f, "GardenGreen");
            Slab("Lab floor", -70, -50, 70, 90, fy, -.6f, "BaseFloor");
            Slab("Corridor floor N", -92, -88, 91, 105, fy, -.6f, "BaseFloor");
            Slab("Corridor floor E", -79, -68, 78, 82, fy, -.6f, "BaseFloor");
            Slab("Airlock floor", -93.5f, -86.5f, 57, 69, fy, -.6f, "BaseFloor");
            var rmp = Placeholders.Prim(PrimitiveType.Cube, root, new Vector3(-90, fy / 2 - .1f, 55.5f), new Vector3(7f, .3f, 3.2f), WorldArt.Mat("BaseFloor"), true, "Airlock ramp");
            rmp.transform.rotation = Quaternion.Euler(-Mathf.Atan2(fy + .1f, 3.2f) * Mathf.Rad2Deg, 0, 0);
            Ring("Hub", HubAt, 12f, new[] { 270f, 90f, 0f }, 18f);     // openings: south (airlock), north (garden), east (lab)
            Ring("Garden", GardenAt, 10f, new[] { 270f }, 22f);
            Ring("Lab", LabAt, 9f, new[] { 180f }, 24f);
            // corridors: walls and a glass roof
            Slab("Corridor N wall W", -92.4f, -92, 91, 105, fy + 3.2f, fy, "BaseWhite"); Slab("Corridor N wall E", -88, -87.6f, 91, 105, fy + 3.2f, fy, "BaseWhite");
            Slab("Corridor E wall S", -79, -68, 77.6f, 78, fy + 3.2f, fy, "BaseWhite"); Slab("Corridor E wall N", -79, -68, 82, 82.4f, fy + 3.2f, fy, "BaseWhite");
            Box("Corridor roof", new Vector3(-90, fy + 3.3f, 98), new Vector3(4.8f, .1f, 14f), new Color(.8f, .92f, 1f, .25f), "Water");
            Box("Corridor roof", new Vector3(-73.5f, fy + 3.3f, 80), new Vector3(11f, .1f, 4.8f), new Color(.8f, .92f, 1f, .25f), "Water");
            // the domes over the rings (no collider: nobody jumps that high indoors)
            ProcMesh.Object("Hub dome", ProcMesh.Dome(12.2f, 8f), Placeholders.Mat(new Color(.85f, .93f, 1f, .22f), "Water"), root, HubAt + Vector3.up * (fy + 3.2f), true);   // solid: no way in over the walls (rule 12)
            ProcMesh.Object("Garden dome", ProcMesh.Dome(10.2f, 7f), Placeholders.Mat(new Color(.8f, 1f, .9f, .2f), "Water"), root, GardenAt + Vector3.up * (fy + 3.2f), true);
            ProcMesh.Object("Lab dome", ProcMesh.Dome(9.2f, 6f), WorldArt.Mat("BaseWhite"), root, LabAt + Vector3.up * (fy + 3.2f), true);
            // inside: the hub's consoles, the garden's plants and pond-less beds, the lab's desks
            for (int i = 0; i < 4; i++) { float a = i * Mathf.PI / 2 + Mathf.PI / 4; Box("Console", HubAt + new Vector3(Mathf.Cos(a) * 7f, fy + .55f, Mathf.Sin(a) * 7f), new Vector3(1.6f, 1.1f, .8f), "ChromeDark").rotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg + 90, 0); }
            for (int i = 0; i < 7; i++) { float a = i * .9f; var p = GardenAt + new Vector3(Mathf.Cos(a) * 5.5f, fy, Mathf.Sin(a) * 5.5f); Cyl("Garden bed", p, 1.1f, .5f, "BaseWhite"); Placeholders.Prim(PrimitiveType.Sphere, root, p + Vector3.up * 1.1f, new Vector3(1.4f, 1.1f, 1.4f), WorldArt.Mat("GardenGreen"), false, "Moon plant"); }
            for (int i = 0; i < 3; i++) Box("Lab desk", LabAt + new Vector3(-3 + i * 3, fy + .45f, 4.5f), new Vector3(2.2f, .9f, 1f), "BaseWhite");
            PointLight("Hub light", HubAt + Vector3.up * 6f, 18f, 1.4f, new Color(.9f, .95f, 1f)); PointLight("Garden light", GardenAt + Vector3.up * 5f, 16f, 1.4f, new Color(.85f, 1f, .85f)); PointLight("Lab light", LabAt + Vector3.up * 4f, 14f, 1.2f, new Color(.85f, .9f, 1f));
            // the airlock (z 57..69): walls, the inner way open, the jammed outer door at z 57 (Muscle Man carries it aside)
            Slab("Airlock wall W", -94, -93.5f, 57, 69, fy + 3.2f, fy, "BaseWhite"); Slab("Airlock wall E", -86.5f, -86, 57, 69, fy + 3.2f, fy, "BaseWhite");
            Box("Airlock roof", new Vector3(-90, fy + 3.3f, 62.3f), new Vector3(8f, .2f, 10.6f), "BaseWhite");   // (it ends at the hub's wall: no ledge inside the hub)
            Box("Airlock sign light", new Vector3(-90, fy + 3.6f, 56.8f), new Vector3(2f, .3f, .1f), "NeonYellow", false);
            var door = new GameObject("Airlock door (Muscle Man)"); door.transform.SetParent(root, false); door.transform.position = new Vector3(-90, fy, 57f);
            Placeholders.Prim(PrimitiveType.Cube, door.transform, new Vector3(0, 1.6f, 0), new Vector3(7f, 3.2f, .4f), WorldArt.Mat("ChromeDark"), false, "Door");
            Placeholders.Prim(PrimitiveType.Cube, door.transform, new Vector3(0, 1.6f, -.22f), new Vector3(1.2f, 1.2f, .05f), WorldArt.Mat("NeonYellow"), false, "Wheel");
            var dc = new GameObject("Block"); dc.transform.SetParent(door.transform, false); dc.transform.localPosition = new Vector3(0, 1.6f, 0); dc.AddComponent<BoxCollider>().size = new Vector3(7f, 3.2f, .4f);
            var dj = door.AddComponent<Boulder>(); dj.id = "MO-airlock"; dj.lifts = true; dj.aside = new Vector3(-6f, 0, -3f); dj.verbText = "carries the jammed airlock door aside";
            Hint(new Vector3(-90, 0, 52), 4f, () => SaveGame.Current.World(Game.World.worldId).opened.Contains("MO-airlock") ? "The moon base! Its airlock is open." : "The moon base's airlock door is jammed... someone very strong could carry it.");
            // the Violinist on a rock by the base (a slow floaty tune: no move left to learn)
            var seat = Rock(ViolinistAt + new Vector3(0, 0, .2f), new Vector3(2.6f, 1.2f, 2f), 20f);
            var v = Violinist.Make(root, OnGround(ViolinistAt.x, ViolinistAt.z) + Vector3.up * .55f, 200f, "ChromeViolin", Game.World.violinistPlayClip);
            v.ability = Abilities.MoonTune; v.learner = "*"; v.moveName = "floaty tune";
            Hint(ViolinistAt + new Vector3(2.5f, 0, -3f), 4f, () => SaveGame.Current.Has(Abilities.MoonTune) ? "The Violinist's tune: four notes, then six, then eight... like blinking lights." : "The Violinist in an old cream spacesuit, sitting on a rock, tuning his chrome violin...");
            _ = seat;
        }
        static void Ring(string name, Vector3 c, float r, float[] openings, float openDeg)
        {
            int n = Mathf.RoundToInt(r * 2.4f);
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2 / n; float deg = a * Mathf.Rad2Deg;
                if (openings.Any(o => Mathf.Abs(Mathf.DeltaAngle(deg, o)) < openDeg / 2f)) continue;
                var p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r;
                var w = Box(name + " wall", p + Vector3.up * (.3f + 1.6f), new Vector3(.4f, 3.2f, 2 * Mathf.PI * r / n + .1f), i % 4 == 0 ? "ChromeDark" : "BaseWhite");
                w.rotation = Quaternion.Euler(0, -deg, 0);
            }
            ProcMesh.Object(name + " rim", ProcMesh.Torus(r, .15f), WorldArt.Mat("NeonTeal"), root, c + Vector3.up * 3.5f);
        }

        // ---- the ridge, the plateau and the rover; the mesa (Muscle Man's boost); KiraFloats at the ridge's foot
        static void Ridge(WorldProgress prog)
        {
            Cp("MO-ridge", new Vector3(-36, 0, -46), 225, true);
            var pg = new Vector3(RoverAt.x, PlateauY, RoverAt.z);
            RoverModel = StaticModel.Spawn("lunar_rover", root, pg, 90f, 2.6f);
            if (RoverModel) { foreach (var mf in RoverModel.GetComponentsInChildren<MeshFilter>()) { var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; } }
            else Box("Rover", pg + Vector3.up * 1.2f, new Vector3(2.8f, 2.4f, 4.6f), "BaseWhite");
            // the control panel: a slanted desk on the rover's north side with four big round lights (the puzzle)
            var panel = new GameObject("Rover panel"); panel.transform.SetParent(root, false); panel.transform.position = new Vector3(PanelAt.x, PlateauY, PanelAt.z);
            Placeholders.Prim(PrimitiveType.Cube, panel.transform, new Vector3(0, .55f, 0), new Vector3(2.4f, 1.1f, .6f), WorldArt.Mat("ChromeDark"), true, "Panel base");
            var top = Placeholders.Prim(PrimitiveType.Cube, panel.transform, new Vector3(0, 1.15f, .05f), new Vector3(2.4f, .08f, .9f), WorldArt.Mat("BaseWhite"), true, "Panel top");
            top.transform.localRotation = Quaternion.Euler(-28, 0, 0);
            Panel = panel.AddComponent<RoverPanel>();
            var cols = new[] { new Color(1f, .3f, .3f), new Color(.3f, 1f, .45f), new Color(.35f, .55f, 1f), new Color(1f, .9f, .3f) };
            for (int i = 0; i < 4; i++)
            {
                var l = Placeholders.Prim(PrimitiveType.Sphere, top.transform, new Vector3(-.75f + i * .5f, .9f, 0), new Vector3(.16f, 3f, .3f), Placeholders.Mat(cols[i] * .35f), false, "Light " + i);
                Panel.lights[i] = l.GetComponent<Renderer>(); Panel.on[i] = Placeholders.Mat(cols[i], "Glow"); Panel.off[i] = Placeholders.Mat(cols[i] * .35f);
            }
            // little blinking lights all over the panel's edge (animated, cosmetic)
            for (int i = 0; i < 8; i++) Placeholders.Prim(PrimitiveType.Cube, top.transform, new Vector3(-1.05f + i * .3f, .9f, -.38f), new Vector3(.08f, 1.6f, .06f), WorldArt.Mat(i % 2 == 0 ? "NeonTeal" : "NeonPink"), false, "Blink").AddComponent<NeonFlicker>();
            Hint(new Vector3(-56, PlateauY, -80), 4f, () => "The lunar rover! Its control panel has four big round lights...");
            // the mesa (30, -90): Muscle Man boosts Dan or Abi up
            var boost = new GameObject("Boost spot (mesa)"); boost.transform.SetParent(root, false); boost.transform.SetPositionAndRotation(OnGround(30, -78f), Quaternion.Euler(0, 180, 0));
            var bs = boost.AddComponent<BoostSpot>(); bs.radius = 1.6f; bs.height = MesaY + 2.4f; bs.landAt = new Vector3(30, MesaY + .05f, -86.5f);
            Box("Boost mark", OnGround(30, -78f) + Vector3.up * .03f, new Vector3(1.4f, .02f, 1.4f), "NeonYellow", false);
            Hint(OnGround(30, -76f), 3f, () => "The mesa's cliffs are too sheer to jump... (Muscle Man could throw you up there)");
            // KiraFloats: once, on the way to the rover (the ridge's foot)
            var fz = new GameObject("KiraFloats zone"); fz.transform.SetParent(root, false); fz.transform.position = OnGround(FloatsAt.x, FloatsAt.z); fz.AddComponent<FloatsZone>();
        }

        static void Rocks()
        {
            var rnd = new System.Random(13);
            for (int i = 0; i < 70; i++)
            {
                float x = (float)rnd.NextDouble() * 520 - 260, z = (float)rnd.NextDouble() * 520 - 260;
                if (Dist(x, z, 0, 14) < 34 || Dist(x, z, -80, 92) < 50 || Dist(x, z, -60, -86) < 22 || Dist(x, z, 30, -90) < 14 || Dist(x, z, 100, 20) < 26) continue;
                float s = .6f + (float)rnd.NextDouble() * 2.6f;
                Rock(new Vector3(x, 0, z), new Vector3(s * 1.3f, s, s * 1.1f), (float)rnd.NextDouble() * 360f, s > 1.4f);
            }
        }

        static void Enemies()
        {
            Enemy.MoonCritter(root, OnGround(30, 10) + Vector3.up * .1f, OnGround(44, -8) + Vector3.up * .1f);
            Enemy.MoonCritter(root, OnGround(70, 20) + Vector3.up * .1f, OnGround(90, -10) + Vector3.up * .1f);
            Enemy.MoonCritter(root, OnGround(120, 30) + Vector3.up * .1f, OnGround(140, 0) + Vector3.up * .1f);
            Enemy.MoonCritter(root, OnGround(-30, 40) + Vector3.up * .1f, OnGround(-50, 30) + Vector3.up * .1f);
            Enemy.MoonCritter(root, OnGround(-20, -20) + Vector3.up * .1f, OnGround(-30, -60) + Vector3.up * .1f);
            Enemy.MoonCritter(root, OnGround(10, -60) + Vector3.up * .1f, OnGround(20, -40) + Vector3.up * .1f);
            Enemy.MoonCritter(root, OnGround(-110, 30) + Vector3.up * .1f, OnGround(-120, 0) + Vector3.up * .1f);
        }

        // the spot each Moon cutscene films at (Game.Film, the Theater)
        public static void FilmSpot(string name, out Vector3 at, out Quaternion facing)
        {
            facing = Quaternion.identity;
            if (name == "MoonLanding") at = RocketGround;
            else if (name == "KiraFloats") { at = OnGround(FloatsAt.x, FloatsAt.z); facing = Quaternion.Euler(0, 225, 0); }
            else if (name == "WhiteShard") { at = new Vector3(PanelAt.x, PlateauY, PanelAt.z); facing = Quaternion.Euler(0, 180, 0); }
            else if (name == "NecklaceWakesMoon") { at = OnGround(-10, -10); facing = Quaternion.Euler(0, 225, 0); }
            else if (name == "ViolinistPlaysMoon") { var v = Violinist.I; at = v.transform.position; facing = v.transform.rotation; }
            else at = OnGround(ArriveAt.x, ArriveAt.z);
        }
    }
}
