using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 7, items 2-12: Bethlehem, world 3: a starry night. The ground and the props come from Tools/Blender/bethlehem_world.py (the same
    // layout numbers); the gameplay is placed on them here, about 10-15 minutes on a first visit:
    //   the barn and its farmyard (x -35..35, z -10..70): the barn the party crashed into (the hole stays in its roof), the manger (the
    //     golden story shard appears in it when the necklace wakes), the hay loft, hay stacks and a bale tower to climb, springy hay and
    //     wool piles only lamb Kira bounces on (up to the barn's ridge, the big stack, up through the roof hole), rail fences; the golden
    //     vortex hovers over the barn with its beam down ("the star")
    //   the hills (east, x 40..195): rolling dark grass, three shepherds' camps with campfires, flocks wandering, grumpy goats among the
    //     rocks, a rocky outcrop, the olive grove, the well (deep water, later), the shepherds' cave (Muscle Man, later), two rock
    //     pillars 10.5 m apart (Kira's pounce); the Violinist at the first camp with his shepherd's harp (he teaches Kira the pounce)
    //   the village (west, x -195..-55): flat-roofed houses, the main street, the market (stalls closed for the night), courtyards, the
    //     inn ("no room"), rooftops and outside stairs, night watchmen with lanterns (light stealth: seen = "Halt!" and back to the
    //     checkpoint), guard dogs (bark for a watchman), a flock driven through the streets (lamb Kira hides in it), the watchmen's yard,
    //     a far roof (Kira's pounce), a cracked courtyard wall (charged punch, later), a narrow gap between two houses (wall jump, later)
    //   the road (north): the Wise Men ride it toward the star the whole time (they stop, argue, check the map)
    public static class Bethlehem
    {
        static Transform root; static Collider terrainCol;
        public const string Start = "BE-start", VortexCp = "BE-vortex";
        public static readonly Vector3 BarnAt = new Vector3(0, 0, 34);
        public static readonly Vector3 MangerAt = new Vector3(0, 0, 37);
        public static readonly Vector3 HayPile = new Vector3(-3.0f, 0, 32.5f);   // under the roof hole (springy: lamb Kira pops out through it)
        public static readonly Vector3 ViolinistAt = new Vector3(73, 0, 44);
        public static readonly Vector3[] Road = { new Vector3(175, 0, 205), new Vector3(100, 0, 185), new Vector3(35, 0, 195), new Vector3(-40, 0, 182), new Vector3(-120, 0, 200), new Vector3(-180, 0, 190) };
        public static readonly Vector3[] Camps = { new Vector3(70, 0, 40), new Vector3(128, 0, 112), new Vector3(150, 0, 0) };
        public static readonly Color Gold = new Color(1f, .78f, .2f);
        public static Portal Gate { get; private set; }
        public static float PillarTop { get; private set; }
        public static Vector3 PillarA { get; private set; }
        public static Vector3 PillarB { get; private set; }
        public static Vector3 RoofNear => new Vector3(-162, 4.2f, 104);
        public static Vector3 RoofFar => new Vector3(-178.5f, 4.2f, 104);
        public static readonly Vector3 BarnBounce = new Vector3(8.4f, 0, 36), StackBounce = new Vector3(-18f, 0, 23.5f);

        public static Transform Build(WorldData w)
        {
            root = new GameObject("World Bethlehem").transform;
            var save = SaveGame.Current; var prog = save.World(w.worldId);
            var terrain = WorldArt.SpawnFile("bethlehem_terrain", root);
            if (terrain)
                foreach (var mf in terrain.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf.name.StartsWith("FarHills")) { mf.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; continue; }
                    var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; if (mf.name.StartsWith("Terrain")) terrainCol = mc;
                }
            else Box("Fallback ground", new Vector3(0, -.5f, 90), new Vector3(420, 1, 360), "Grass");
            Physics.SyncTransforms();

            Farm(prog); Hills(prog); Village(prog); RoadAndCaravan();
            foreach (var (c, s) in new[] { (new Vector3(0, 40, -86), new Vector3(430, 120, 4)), (new Vector3(0, 40, 256), new Vector3(430, 120, 4)),
                                           (new Vector3(-206, 40, 85), new Vector3(4, 120, 350)), (new Vector3(206, 40, 85), new Vector3(4, 120, 350)) })
                Box("Hills boundary", c, s, Color.clear, "Water").GetComponent<Renderer>().enabled = false;
            Physics.SyncTransforms();

            foreach (var s in w.shards)
                if (!prog.shards.Contains(s.id)) Pickup.Make(Pickup.Kind.Shard, s.id, s.area, ShardPos(s.position), root, new Color(1f, .82f, .4f));
            PlayLog.Line("Bethlehem shards placed: " + string.Join(" ", w.shards.Select(s => $"{s.id}{Fmt(ShardPos(s.position))}")));
            var a = w.Area("BE");
            var story = Pickup.Make(Pickup.Kind.StoryShard, "story-" + w.worldId, a.id, a.storyShard, root, a.storyShardColor);
            story.gameObject.SetActive(prog.necklaceAwake && !prog.storyShard);
            Game.I.StoryShard = story;
            var sp = story.transform.position;
            // the beam: up from the manger, out through the roof hole and on into the sky
            Game.I.StoryBeam = Box("Story beam", new Vector3(sp.x, sp.y + 14f, sp.z), new Vector3(.35f, 28f, .35f), new Color(1f, .85f, .35f), "Glow", false).gameObject;
            Game.I.StoryBeam.SetActive(prog.necklaceAwake && !prog.storyShard);
            if (!save.heartUpgrades.Contains("BE-H1")) Pickup.Make(Pickup.Kind.HeartUpgrade, "BE-H1", "BE", new Vector3(0, 9.1f, 30.5f), root, Playground.Accent);
            if (!save.heartUpgrades.Contains("BE-H2")) Pickup.Make(Pickup.Kind.HeartUpgrade, "BE-H2", "BE", PillarB + new Vector3(0, 1.2f, 3f), root, Playground.Accent);
            foreach (var h in new[] { new Vector3(10, 0, 2), new Vector3(64, 0, 36), new Vector3(-118, 0, 60), new Vector3(132, 0, 106), new Vector3(40, 0, 188), new Vector3(-150, 0, 120) })
                Pickup.HealHeart(OnGround(h.x, h.z) + Vector3.up * 1f, root);
            Enemies();
            StarDome.Make(root);
            return root;
        }

        // Shard y in the data = its height over the terrain there (the hills rise and fall): 1 m over whatever it stands on
        public static Vector3 ShardPos(Vector3 hint)
        {
            float g = Terrain(hint.x, hint.z);
            return new Vector3(hint.x, Ground(hint.x, hint.z, g + hint.y + 1.6f) + 1f, hint.z);
        }
        public static float Terrain(float x, float z) => terrainCol && terrainCol.Raycast(new Ray(new Vector3(x, 300f, z), Vector3.down), out var hit, 600f) ? hit.point.y : 0f;
        static string Fmt(Vector3 v) => $"({v.x:0},{v.y:0.0},{v.z:0})";

        // ---- helpers (as Egypt's)
        const int GroundMask = ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << Hero.EnemyLayer) | (1 << 4) | (1 << 2));
        public static float Ground(float x, float z, float fromY = 80f) =>
            Physics.Raycast(new Vector3(x, fromY, z), Vector3.down, out var hit, 300f, GroundMask, QueryTriggerInteraction.Ignore) ? hit.point.y : 0f;
        public static Vector3 OnGround(float x, float z, float fromY = 80f) => new Vector3(x, Ground(x, z, fromY), z);
        static Transform Box(string name, Vector3 centre, Vector3 size, string mat, bool collider = true) =>
            Placeholders.Prim(PrimitiveType.Cube, root, centre, size, WorldArt.Mat(mat), collider, name).transform;
        static Transform Box(string name, Vector3 centre, Vector3 size, Color c, string kind = "Lit", bool collider = true) =>
            Placeholders.Prim(PrimitiveType.Cube, root, centre, size, Placeholders.Mat(c, kind), collider, name).transform;
        static Transform Slab(string name, float x0, float x1, float z0, float z1, float top, float bottom, string mat = "Plank") =>
            Box(name, new Vector3((x0 + x1) / 2, (top + bottom) / 2, (z0 + z1) / 2), new Vector3(x1 - x0, top - bottom, z1 - z0), mat);
        static Transform Hidden(Transform t) { t.GetComponent<Renderer>().enabled = false; return t; }
        static void Hint(Vector3 at, float r, System.Func<string> text)
        {
            var go = new GameObject("Hint"); go.transform.SetParent(root, false); go.transform.position = at;
            var h = go.AddComponent<HintZone>(); h.radius = r; h.text = text;
        }
        static string B(Controls.Act a) => Controls.Btn(a);
        static Checkpoint Cp(string id, float x, float z, float yaw) => Playground.CheckpointHere(id, OnGround(x, z), yaw, root);
        static GameObject Prop(string prop, float x, float z, float yaw = 0, float scale = 1, bool collider = false, float y = float.NaN) =>
            WorldArt.Prop("bethlehem_props", prop, root, float.IsNaN(y) ? OnGround(x, z) : new Vector3(x, y, z), yaw, scale, collider);
        static void Solid(Vector3 at, Vector3 size, float yaw = 0)
        {
            var b = new GameObject("Solid"); b.transform.SetParent(root, false); b.transform.position = at + Vector3.up * size.y / 2;
            b.transform.rotation = Quaternion.Euler(0, yaw, 0); b.AddComponent<BoxCollider>().size = size;
        }
        static Light PointLight(string name, Vector3 at, float range, float intensity, Color c)
        {
            var l = new GameObject(name); l.transform.SetParent(root, false); l.transform.position = at;
            var lt = l.AddComponent<Light>(); lt.type = LightType.Point; lt.range = range; lt.intensity = intensity; lt.color = c; lt.shadows = LightShadows.None; return lt;
        }
        static readonly Color Warm = new Color(1f, .72f, .38f);
        static void Bouncy(string name, Vector3 at, float scale, float height, bool wool)
        {
            var go = new GameObject(name); go.transform.SetParent(root, false); go.transform.position = at;
            var pad = go.AddComponent<BouncePad>(); pad.height = height;
            var mat = wool ? Placeholders.Mat(new Color(.93f, .9f, .84f)) : WorldArt.Mat("Hay");
            for (int i = 0; i < 4; i++)
            {
                float a = i * 1.7f;
                Placeholders.Prim(PrimitiveType.Sphere, go.transform, new Vector3(Mathf.Cos(a) * .35f * scale, .35f * scale, Mathf.Sin(a) * .35f * scale), new Vector3(1.1f, .75f, 1.1f) * scale, mat, name: wool ? "Wool" : "Hay");
            }
            var col = go.AddComponent<BoxCollider>(); col.center = new Vector3(0, .45f * scale, 0); col.size = new Vector3(1.6f, .9f, 1.6f) * scale;
        }

        // ---- the barn and the farmyard
        static void Farm(WorldProgress prog)
        {
            Playground.CheckpointHere(Start, new Vector3(0, .05f, 29.5f), 180, root, false);   // inside the barn (no flag: the arrival lands here)
            Cp(VortexCp, 5, 16, 180);
            Hint(new Vector3(0, 0, 22), 6f, () => $"Bethlehem, a starry night. Every shard counts: at {Game.World.areas[0].requirement} in all, the golden shard on Dan's necklace wakes ({SaveGame.Current.Total} so far).");
            // the barn (12 x 16 m, the door south): its walls and the roof as colliders (the model's boards are only looks)
            var barn = Prop("Barn", BarnAt.x, BarnAt.z, 0, 1f, false, 0f);
            float W = 6f, L = 8f, H = 4.5f;
            Solid(new Vector3(-W, 0, 34), new Vector3(.3f, H, 2 * L + .3f)); Solid(new Vector3(W, 0, 34), new Vector3(.3f, H, 2 * L + .3f));
            Solid(new Vector3(0, 0, 34 + L), new Vector3(2 * W + .3f, H + 3.5f, .3f));
            Solid(new Vector3(-4.25f, 0, 34 - L), new Vector3(3.5f, H, .3f)); Solid(new Vector3(4.25f, 0, 34 - L), new Vector3(3.5f, H, .3f));
            Solid(new Vector3(0, H - .5f, 34 - L), new Vector3(5f, 4f, .3f));   // over the door, up into the gable
            // the roof: two 30 deg slopes; the west one has the crash hole (x -5.1..-1.7, z 31.85..36.15)
            float slope = Mathf.Atan2(3.5f, 6f), run = 7.87f, zs = 34 - L - .6f, ze = 34 + L + .6f;
            void Roof(int side, float d0, float d1, float z0, float z1)
            {
                float dm = (d0 + d1) / 2;
                var c = new Vector3(side * dm * Mathf.Cos(slope), 8.15f - dm * Mathf.Sin(slope), (z0 + z1) / 2);
                var b = new GameObject("Roof"); b.transform.SetParent(root, false); b.transform.SetPositionAndRotation(c, Quaternion.Euler(0, 0, -side * slope * Mathf.Rad2Deg));
                b.AddComponent<BoxCollider>().size = new Vector3(d1 - d0, .35f, z1 - z0);
            }
            Roof(1, 0, run, zs, ze);
            Roof(-1, 0, run, zs, 31.85f); Roof(-1, 0, run, 36.15f, ze); Roof(-1, 0, 1.97f, 31.85f, 36.15f); Roof(-1, 5.9f, run, 31.85f, 36.15f);
            Box("Ridge", new Vector3(0, 8.3f, 34), new Vector3(.7f, .3f, 17.4f), "Thatch", false);   // looks only: the slopes meet under it (a lip lamb Kira couldn't step onto)
            // inside: the manger (the golden shard appears in it), the hay loft up the bales, the springy hay pile under the roof hole
            Prop("Manger", MangerAt.x, MangerAt.z, 0, 1.2f, false, 0f); Solid(MangerAt, new Vector3(2.2f, .8f, 1.0f));
            Slab("Hay loft", -6, 6, 40, 42, 3.2f, 3.0f, "Plank");
            foreach (var (z0, z1, top) in new[] { (35.6f, 36.8f, .6f), (36.8f, 38f, 1.2f), (38f, 39.2f, 1.8f), (39.2f, 40f, 2.5f) }) Slab("Hay bale step", -5.8f, -4.2f, z0, z1, top, 0, "Hay");
            for (int i = 0; i < 4; i++) Prop("HayBale", -3 + i * 1.6f, 41, 0, 1f, false, 3.2f);
            Bouncy("Springy hay (under the hole)", HayPile, 1.05f, 6.5f, false);
            Hint(HayPile + Vector3.forward * 1.5f, 2.5f, () => "A heap of springy hay right under the hole in the roof...");
            PointLight("Barn light", new Vector3(0, 3.5f, 34), 15, 3.2f, Warm); PointLight("Barn door light", new Vector3(0, 2.2f, 22.5f), 16, 2.4f, Warm);
            // outside: the bounce up onto the roof (east wall), the big hay stack (and its wool pile), the bale tower, fences, barrels, the trough, the cart
            Bouncy("Wool pile (by the barn)", BarnBounce, 1f, 6f, true);
            Hint(BarnBounce + Vector3.left * -2f, 3f, () => SaveGame.Current.World(WorldData.Bethlehem).shards.Contains("F08") ? "Sheared wool, piled up soft." : "A pile of sheared wool against the barn. Bouncy enough for a lamb to reach the roof? (Kira)");
            Prop("HayStack", -22, 20, 0, 2f, true);
            Bouncy("Wool pile (by the stack)", StackBounce, 1f, 5.5f, true);
            foreach (var (z0, z1, top) in new[] { (24f, 25f, .6f), (25f, 26f, 1.2f), (26f, 27f, 1.8f), (27f, 28f, 2.4f), (28f, 29f, 3.0f), (29f, 31.4f, 3.6f) }) Slab("Bale tower", 18.8f, 21.2f, z0, z1, top, 0, "Hay");
            Bouncy("Springy hay (the yard)", new Vector3(10, 0, 22), .9f, 5f, false);
            foreach (var (x, z, yaw) in new[] { (-9f, 26f, 15f), (9.5f, 26.5f, -20f), (-12f, 40f, 80f), (14f, 44f, 10f), (-26f, 36f, 40f) }) Prop("HayBale", x, z, yaw, 1f, true);
            foreach (var (x, z) in new[] { (7.6f, 24.6f), (8.4f, 25.3f), (-7.5f, 44f), (26f, 12f) }) Prop("Barrel", x, z, x * 20, 1f, true);
            Prop("Trough", 24, 50, 90, 1f, true); Prop("Cart", 13, 63, 30, 1f, true);
            // the rail fence round the yard (gaps: south x -6..6, north x 8..20, west z 26..40, east z 22..36)
            void FenceLine(Vector3 from, Vector3 to, float gapFrom, float gapTo)
            {
                var d = to - from; int n = Mathf.RoundToInt(d.magnitude / 4f); float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg - 90;
                for (int i = 0; i < n; i++)
                {
                    var p = from + d * ((i + .5f) / n); float along = Vector3.Dot(p - from, d.normalized);
                    if (along > gapFrom && along < gapTo) continue;
                    Prop("Fence", p.x, p.z, yaw, 1f); Solid(OnGround(p.x, p.z), new Vector3(4.1f, 1.4f, .25f), yaw);
                }
            }
            FenceLine(new Vector3(-34, 0, -8), new Vector3(34, 0, -8), 28, 40); FenceLine(new Vector3(-34, 0, 68), new Vector3(34, 0, 68), 42, 54);
            FenceLine(new Vector3(-34, 0, -8), new Vector3(-34, 0, 68), 34, 48); FenceLine(new Vector3(34, 0, -8), new Vector3(34, 0, 68), 30, 44);
            // a small pen of sheep in the north yard (practice for hiding), and the star over the barn
            var pen = Flock.Make(root, "the yard", new Vector3(-12, 0, 56), 4); pen.wander = 4f;
            Hint(new Vector3(-12, 0, 52), 5f, () => Party.I && Party.I.Player.Stats.displayName == "Kira" ? "Lamb Kira fits right in. Stay in a flock and nobody gives her a second look." : "Sheep. Lamb Kira could blend right in with them...");
            StarRing.Make(root, new Vector3(0, 30f, 34f), 8.6f);
            // the vortex gate (after the finale the hay's vortex stays open in the barn: the way back to the vortex)
            Gate = Playground.PortalHere("BE-vortex-gate", new Vector3(3.6f, 0, 31.5f), -90, Vortex.Checkpoint, false, root, Gold, vortex: true);
            Gate.gameObject.SetActive(prog.storyShard);
            _ = barn; _ = prog;
        }

        // ---- the hills: camps, flocks, goats, the outcrop, the grove, the well, the cave, the pounce pillars
        static void Hills(WorldProgress prog)
        {
            Cp("BE-hills", 44, 28, 90);
            for (int i = 0; i < Camps.Length; i++)
            {
                var c = Camps[i]; var at = OnGround(c.x, c.z);
                Campfire.Make(root, at); PointLight("Campfire light", at + Vector3.up * 1.2f, 14, 2.6f, new Color(1f, .6f, .3f));
                foreach (float a in new[] { 40f, 160f, 280f }) { var p = c + Quaternion.Euler(0, a, 0) * new Vector3(0, 0, 2.6f); Prop("Rock", p.x, p.z, a, .25f); }
                if (i > 0) Shepherd(c + new Vector3(2.4f, 0, -1.2f), -60);
                Cp("BE-camp" + (i + 1), c.x + 4, c.z - 4, 0);
            }
            var v = Violinist.Make(root, OnGround(ViolinistAt.x, ViolinistAt.z), 245f, "Harp");
            v.ability = Abilities.Pounce; v.learner = "Kira"; v.moveName = "Pounce";
            Hint(ViolinistAt + new Vector3(-4, 0, -2), 5f, () => SaveGame.Current.Has(Abilities.Pounce) ? $"Kira's pounce: {B(Controls.Act.Dive)} on the ground to crouch and aim, then she leaps far." : "A musician with a little harp by the shepherds' fire. He keeps glancing at Kira...");
            Flock.Make(root, "camp 1", new Vector3(80, 0, 54), 7); Flock.Make(root, "camp 2", new Vector3(136, 0, 96), 8); Flock.Make(root, "camp 3", new Vector3(158, 0, 8), 6);
            // the olive grove
            var rnd = new System.Random(5);
            for (int i = 0; i < 22; i++) { float x = 96 + (float)rnd.NextDouble() * 48, z = 151 + (float)rnd.NextDouble() * 48; if (Mathf.Abs(x - 120) < 3 && Mathf.Abs(z - 182) < 3) continue; Prop("Olive", x, z, (float)rnd.NextDouble() * 360, .9f + (float)rnd.NextDouble() * .4f); }
            Cp("BE-grove", 112, 148, 0);
            // rocks about the hills
            foreach (var (x, z, s) in new[] { (55f, 88f, .8f), (126f, 58f, 1f), (134f, 64f, .7f), (168f, 24f, .9f), (102f, 128f, .6f), (180f, 108f, 1.1f), (48f, 160f, .8f), (150f, 60f, .7f) }) Prop("Rock", x, z, x * 7, s, true);
            // the rocky outcrop (top 3.8 m) and its high rock (6.5 m: the double jump, later)
            var oc = OnGround(90.5f, 110.5f); float g = oc.y;
            Prop("Rock", 90.5f, 110.5f, 20, 1.45f, false, g); Prop("Rock", 86f, 111f, 70, .5f, false, g); Prop("Rock", 87.6f, 108.2f, 140, .95f, false, g); Prop("Rock", 90.5f, 114f, 0, .7f, false, g + 3.5f);
            Hidden(Slab("Outcrop", 88.2f, 92.8f, 108.5f, 112.5f, g + 3.8f, g - 1)); Hidden(Slab("Outcrop step", 84.9f, 87.1f, 109.9f, 112.1f, g + 1.3f, g - 1)); Hidden(Slab("Outcrop step", 86.6f, 88.6f, 107.2f, 109.2f, g + 2.6f, g - 1));
            Hidden(Slab("High rock", 89.6f, 91.4f, 113.1f, 114.9f, g + 6.5f, g - 1));
            Hint(new Vector3(90, g + 3.8f, 112), 2.5f, () => SaveGame.Current.Has(Abilities.DoubleJump) ? "The high rock: Abi's double jump reaches it!" : "A high rock above the outcrop. Too high for one jump... (Abi's double jump)");
            // the well (deep: the deep dive, Pirate Seas)
            var well = OnGround(108, 72, 30f); float wy = Terrain(108, 82) - .1f; Prop("Well", 108, 72, 0, 2f, false, wy);   // Fix 7.2: wy from the flat ground past the terrain's hole (at z 76 it was down in it, ~5 m)
            for (int i = 0; i < 14; i++) { float a = i * Mathf.PI * 2 / 14; Solid(new Vector3(108 + Mathf.Cos(a) * 2.6f, -15f, 72 + Mathf.Sin(a) * 2.6f), new Vector3(1.2f, wy + 1.8f + 15f, 1.0f), -a * Mathf.Rad2Deg + 90); }   // Fix 7.2: down to the bottom (the terrain's hole is wider than the water: a dry ledge outside it trapped heroes)
            var water = Box("Well water", new Vector3(108, (wy - 1.2f - 15f) / 2f, 72), new Vector3(4.4f, wy - 1.2f + 15f, 4.4f), new Color(.15f, .3f, .45f, .7f), "Water", false); water.gameObject.layer = 4; water.gameObject.AddComponent<WaterVolume>();
            var deep = Box("Deep water (deep dive)", new Vector3(108, -10.5f, 72), new Vector3(4.4f, 9f, 4.4f), Color.clear, "Water", false); deep.GetComponent<Renderer>().enabled = false; deep.gameObject.layer = 2; deep.gameObject.AddComponent<DeepWater>();
            // Fix 7.2: a stone apron round the well over the terrain's wide hole (a dry moat outside the ring trapped heroes), solid to the bottom
            Slab("Well apron N", 98.5f, 117.5f, 74.2f, 81.5f, wy + .05f, -15f, "Stone"); Slab("Well apron S", 98.5f, 117.5f, 62.5f, 69.8f, wy + .05f, -15f, "Stone");
            Slab("Well apron E", 110.2f, 117.5f, 69.8f, 74.2f, wy + .05f, -15f, "Stone"); Slab("Well apron W", 98.5f, 105.8f, 69.8f, 74.2f, wy + .05f, -15f, "Stone");
            Hint(new Vector3(108, 0, 76), 4f, () => "An old well. It goes down a very long way... too deep to dive, for now.");
            // Fix 7.2, item 1: never stuck in the well: a rope from the beam down to a bucket on the water; Jump (or swim to the wall)
            // climbs it and over the rim (WellExit)
            Placeholders.Prim(PrimitiveType.Cylinder, root, new Vector3(108, wy + 1.55f, 72), new Vector3(.07f, 2.75f, .07f), WorldArt.Mat("Rope"), false, "Well rope");
            Placeholders.Prim(PrimitiveType.Cylinder, root, new Vector3(108, wy - 1.05f, 72), new Vector3(.55f, .25f, .55f), WorldArt.Mat("Plank"), false, "Well bucket");
            var exit = new GameObject("Well exit").AddComponent<WellExit>(); exit.transform.SetParent(root, false);
            exit.centre = new Vector3(108, 0, 72); exit.radius = 2.1f; exit.rimTop = wy + 2f; exit.outAt = 4.4f;
            _ = well;
            // the shepherds' cave behind a boulder (Muscle Man, Medieval)
            float cg = Terrain(190, 60);
            Slab("Cave wall N", 184, 198, 66, 68, cg + 5, cg - 2, "Stone"); Slab("Cave wall S", 184, 198, 52, 54, cg + 5, cg - 2, "Stone"); Slab("Cave wall E", 196, 198, 54, 66, cg + 5, cg - 2, "Stone");
            Slab("Cave wall W N", 184, 186, 62, 66, cg + 5, cg - 2, "Stone"); Slab("Cave wall W S", 184, 186, 54, 58, cg + 5, cg - 2, "Stone"); Slab("Cave roof", 184, 198, 52, 68, cg + 6, cg + 5, "Stone");
            Slab("Cave floor", 186, 196, 54, 66, cg + .05f, cg - 1, "Dirt");
            var boulder = Box("Boulder (needs Muscle Man)", new Vector3(184.5f, cg + 1.9f, 60), new Vector3(3.2f, 3.8f, 4.6f), "Stone"); boulder.gameObject.AddComponent<Boulder>();
            Hint(new Vector3(180, cg, 60), 4f, () => "A shepherds' cave, blocked by a huge boulder. Someone very strong could roll it away... (later)");
            // the two rock pillars, 10.5 m apart: steps up the near one; only Kira's pounce reaches the far one (its shard and a heart)
            float pg = Mathf.Max(Terrain(160, 140), Terrain(177.5f, 140)); PillarTop = pg + 5f;
            Slab("Pillar near", 156, 164, 136, 144, PillarTop, pg - 3, "Stone"); Slab("Pillar far", 174.5f, 180.5f, 136, 144, PillarTop, pg - 3, "Stone");
            PillarA = new Vector3(160, PillarTop, 140); PillarB = new Vector3(177.5f, PillarTop, 140);
            int n = 0; for (float top = PillarTop - 1.1f; top > Terrain(160, 135 - n * 1.6f) + .2f && n < 12; top -= 1.15f, n++)
                Slab("Pillar step", 157.5f, 162.5f, 136f - (n + 1) * 1.6f, 136f - n * 1.6f + .01f, top, pg - 3, "Stone");
            Hint(new Vector3(162, PillarTop, 140), 3.5f, () => SaveGame.Current.Has(Abilities.Pounce) ? $"The far pillar: a running start isn't enough... crouch with {B(Controls.Act.Dive)}, aim, and Kira pounces!" : "The far pillar is too far to jump. A cat could pounce it, one day...");
            _ = prog;
        }

        // a shepherd by the fire: sitting, a crook, a headcloth (placeholder figure)
        static void Shepherd(Vector3 at, float yaw)
        {
            var go = new GameObject("Shepherd"); go.transform.SetParent(root, false); go.transform.SetPositionAndRotation(OnGround(at.x, at.z), Quaternion.Euler(0, yaw, 0));
            var robe = Placeholders.Mat(new Color(.6f, .5f, .38f)); var skin = Placeholders.Mat(new Color(.78f, .58f, .44f));
            Placeholders.Prim(PrimitiveType.Capsule, go.transform, new Vector3(0, .55f, 0), new Vector3(.55f, .55f, .5f), robe, name: "Robe");
            Placeholders.Prim(PrimitiveType.Sphere, go.transform, new Vector3(0, 1.2f, 0), new Vector3(.3f, .32f, .3f), skin, name: "Head");
            Placeholders.Prim(PrimitiveType.Sphere, go.transform, new Vector3(0, 1.3f, -.04f), new Vector3(.34f, .25f, .34f), Placeholders.Mat(new Color(.9f, .85f, .72f)), name: "Headcloth");
            Placeholders.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(.4f, .9f, .1f), new Vector3(.04f, .9f, .04f), WorldArt.Mat("Wood"), name: "Crook");
        }

        // ---- the village: houses, the main street, the market, courtyards, the inn, rooftops, the watchmen and their dogs
        static GameObject House(float x, float z, int kind, float yaw, int storeys = 1)
        {
            var h = Prop("House" + kind, x, z, yaw, 1f, false, 0f);
            for (int s = 1; s < storeys; s++) Prop("House" + kind, x, z, yaw, 1f, false, 4.2f * s);
            Solid(new Vector3(x, 0, z), new Vector3(6f, 4.2f * storeys, 6f), yaw);
            return h;
        }
        static void Stair(float x0, float x1, float zTop, int steps, float rise, float dir = -1)
        {
            // steps of `rise` along z (dir -1: rising toward lower z), the top one ending at zTop
            for (int i = 0; i < steps; i++) { float z = zTop - dir * (steps - 1 - i); Slab("Outside stair", x0, x1, Mathf.Min(z, z - dir), Mathf.Max(z, z - dir), (i + 1) * rise, 0, "Mudbrick"); }
        }
        static void Village(WorldProgress prog)
        {
            Cp("BE-village", -60, 56, 270); Cp("BE-market", -118, 60, 270); Cp("BE-inn", -150, 82, 0); Cp("BE-lane", -150, 26, 270);
            Hint(new Vector3(-64, 0, 60), 6f, () => "The village is asleep... except the night watchmen. Keep out of their lanterns' light, or it's \"Halt!\" and back you go.");
            // the main street (z 60): a row of houses each side, the market between x -104 and -134
            foreach (var x in new[] { -70f, -80f, -90f, -100f, -140f, -150f, -160f, -170f, -180f }) House(x, 46, x % 20 == 0 ? 1 : 2, 180);
            foreach (var x in new[] { -70f, -80f, -90f, -100f, -140f, -170f, -180f }) House(x, 74, x % 20 == 0 ? 3 : 1, 0);
            Stair(-96.6f, -95.2f, 46.5f, 6, .7f, 1); Stair(-146.8f, -145.4f, 46.5f, 6, .7f, 1);
            foreach (var x in new[] { -155f, -165f, -175f }) Box("Roof plank", new Vector3(x, 4.15f, 46), new Vector3(4.2f, .2f, 2f), "Plank");
            Hint(new Vector3(-146, 4.2f, 46), 3f, () => "Flat roofs and planks across: the rooftops are the quiet way through.");
            // the market: stalls shut for the night, lanterns on poles
            foreach (var (x, z, yaw) in new[] { (-110f, 54f, 0f), (-120f, 54f, 0f), (-128f, 54f, 0f), (-110f, 66f, 180f), (-120f, 66f, 180f), (-128f, 66f, 180f) })
            { Prop("Stall", x, z, yaw, 1f); Solid(new Vector3(x, 0, z), new Vector3(2.9f, 1f, 1.2f), yaw); }
            foreach (var (x, z) in new[] { (-104f, 60f), (-134f, 60f), (-66f, 60f), (-150f, 88f), (-90f, 88f), (-175f, 60f) })
            {
                Box("Lantern pole", new Vector3(x, 1.4f, z), new Vector3(.12f, 2.8f, .12f), "Wood");
                Prop("Lantern", x, z, 0, 1.4f, false, 2.85f); PointLight("Street lantern", new Vector3(x, 2.6f, z), 9, 1.6f, Warm);
            }
            // the back streets: a courtyard with a dog, more houses, the inn
            foreach (var (x, z, k) in new[] { (-80f, 100f, 2), (-110f, 104f, 3), (-130f, 104f, 1), (-162f, 104f, 2), (-178.5f, 104f, 3), (-70f, 120f, 1), (-120f, 124f, 2), (-140f, 124f, 3) }) House(x, z, k, 0);
            Stair(-126.8f, -125.4f, 104.5f, 6, .7f, 1); Stair(-158.8f, -157.4f, 104.5f, 6, .7f, 1);
            Slab("Courtyard wall W", -102, -101.4f, 94, 106, 2f, 0, "Mudbrick"); Slab("Courtyard wall E", -88.6f, -88, 94, 106, 2f, 0, "Mudbrick"); Slab("Courtyard wall N", -102, -88, 105.4f, 106, 2f, 0, "Mudbrick");
            Slab("Courtyard wall S W", -102, -97, 94, 94.6f, 2f, 0, "Mudbrick"); Slab("Courtyard wall S E", -93, -88, 94, 94.6f, 2f, 0, "Mudbrick");
            Prop("Trough", -92, 102, 0, 1f, true);
            Hint(RoofNear + new Vector3(2, 0, 0), 3f, () => SaveGame.Current.Has(Abilities.Pounce) ? $"The far roof: crouch with {B(Controls.Act.Dive)} and pounce, Kira!" : "That far roof is much too far to jump. A cat could pounce it, one day...");
            // the inn (two storeys) and its "no room"
            Prop("Inn", -150, 95, 0, 1f, false, 0f); Solid(new Vector3(-150, 0, 95), new Vector3(10, 7.5f, 8));
            var sign = Prop("Signpost", -148.4f, 90.5f, 0, 1.2f, false, 3.0f);
            var label = new GameObject("No room"); label.transform.SetParent(root, false); label.transform.position = new Vector3(-148.4f, 2.45f, 90.4f);
            var tm = label.AddComponent<TextMesh>(); tm.text = "NO ROOM"; tm.font = UiKit.Font; tm.fontSize = 64; tm.characterSize = .045f; tm.anchor = TextAnchor.MiddleCenter; tm.color = new Color(.25f, .12f, .06f);
            label.GetComponent<MeshRenderer>().sharedMaterial = UiKit.Font.material;
            var door = new GameObject("Inn door"); door.transform.SetParent(root, false); door.transform.position = new Vector3(-150, 0, 90.5f); door.AddComponent<NoRoom>();
            PointLight("Inn light", new Vector3(-150, 2.2f, 89), 10, 2f, Warm);
            for (int i = 0; i < 10; i++) Slab("Inn stair", -144.6f, -143.2f, 89 + i, 90 + i, (i + 1) * .75f, 0, "Mudbrick");
            Slab("Inn roof terrace", -152, -148, 95, 99, 10.1f, 7.5f, "Mudbrick");
            Hint(new Vector3(-147, 7.5f, 96), 3f, () => SaveGame.Current.Has(Abilities.DoubleJump) ? "The roof terrace: Abi's double jump!" : "A little roof terrace, high up. Too high for one jump... (Abi's double jump)");
            // the watchmen's yard: walls, its gate east; a watchman at the gate looking out (only a flock gets past him)
            Slab("Yard wall W", -195, -194, 4, 20, 3.6f, 0, "Mudbrick"); Slab("Yard wall N", -195, -178, 19, 20, 3.6f, 0, "Mudbrick"); Slab("Yard wall S", -195, -178, 4, 5, 3.6f, 0, "Mudbrick");
            Slab("Yard wall E N", -179, -178, 14.5f, 19, 3.6f, 0, "Mudbrick"); Slab("Yard wall E S", -179, -178, 5, 9.5f, 3.6f, 0, "Mudbrick");
            var gatekeeper = Guard.Make(root, new Vector3(-180.5f, 0, 12f), new Vector3(-180.5f, 0, 12f), "Watchman (the yard gate)");
            gatekeeper.transform.rotation = Quaternion.Euler(0, 90, 0); gatekeeper.sight = 11f; gatekeeper.halfAngle = 55f;
            Hint(new Vector3(-170, 0, 12), 5f, () => "The watchmen's yard. Its guard never takes his eyes off the gate... though he doesn't count sheep.");
            Flock.Make(root, "the street flock", new Vector3(-120, 0, 12), 6, new[] { new Vector3(-120, 0, 12), new Vector3(-160, 0, 12), new Vector3(-188, 0, 12), new Vector3(-188, 0, 15), new Vector3(-160, 0, 17), new Vector3(-120, 0, 17) }).speed = 1.25f;
            // watchmen on their rounds (one dozes at the inn door now and then)
            Guard.Make(root, new Vector3(-78, 0, 62), new Vector3(-102, 0, 62), "Watchman (main street east)");   // the village gate (x -60..-70) stays quiet
            Guard.Make(root, new Vector3(-136, 0, 58), new Vector3(-184, 0, 58), "Watchman (main street west)");
            Guard.Make(root, new Vector3(-104, 0, 50), new Vector3(-134, 0, 50), "Watchman (the market)");
            Guard.Make(root, new Vector3(-80, 0, 88), new Vector3(-140, 0, 88), "Watchman (the back street)");
            var dozer = Guard.Make(root, new Vector3(-146, 0, 88.5f), new Vector3(-146, 0, 88.5f), "Watchman (the inn door)"); dozer.dozes = true; dozer.transform.rotation = Quaternion.Euler(0, 200, 0);
            // the charged-punch courtyard (Medieval) and the wall-jump gap (Steampunk)
            Slab("Walled yard W", -171, -170, 131, 143, 4.5f, 0, "Mudbrick"); Slab("Walled yard E", -152, -151, 131, 143, 4.5f, 0, "Mudbrick"); Slab("Walled yard N", -171, -151, 142, 143, 4.5f, 0, "Mudbrick");
            Slab("Walled yard S W", -171, -162, 131, 132, 4.5f, 0, "Mudbrick"); Slab("Walled yard S E", -158, -151, 131, 132, 4.5f, 0, "Mudbrick");
            if (!prog.opened.Contains("BE-crack-courtyard"))
            {
                var rock = Slab("Cracked wall", -162, -158, 131, 132, 4.5f, 0, "Mudbrick"); rock.gameObject.AddComponent<RockWall>().id = "BE-crack-courtyard";
                for (int i = 0; i < 3; i++) Placeholders.Prim(PrimitiveType.Cube, rock, new Vector3(.1f * i - .1f, .1f * i, -.55f), new Vector3(.5f, .05f, .1f), Placeholders.Mat(new Color(.3f, .22f, .15f)), name: "Crack").transform.localRotation = Quaternion.Euler(0, 0, i * 40 - 30);
            }
            Hint(new Vector3(-160, 0, 128), 3f, () => "This wall is cracked... a much stronger punch could break it. (later)");
            foreach (var (x, k) in new[] { (-94.25f, 1), (-85.75f, 3) }) { Prop("House" + k, x, 133, 0, 1f, false, 0f); Prop("House" + k, x, 133, 0, 1f, false, 4.2f); }
            Solid(new Vector3(-94.25f, 0, 133), new Vector3(5.5f, 8.4f, 6)); Solid(new Vector3(-85.75f, 0, 133), new Vector3(5.5f, 8.4f, 6));
            Box("Plank over the gap", new Vector3(-90, 8.3f, 133), new Vector3(3.2f, .2f, 1.2f), "Plank");
            Hint(new Vector3(-90, 0, 128), 3f, () => "A narrow gap between two tall houses. Someone who can kick off walls could climb it... (later)");
            _ = sign;
        }

        // ---- the road: the Wise Men's caravan, its checkpoint
        static void RoadAndCaravan()
        {
            Cp("BE-road", 40, 186, 270);
            Caravan.Make(root, Road.Select(p => OnGround(p.x, p.z)).ToArray());
            Hint(new Vector3(35, 0, 190), 8f, () => "Three Wise Men on camels, following the star. They don't seem to agree on the way.");
        }

        static void Enemies()
        {
            Vector3 G(float x, float z) => OnGround(x, z) + Vector3.up * .1f;
            Enemy.Goat(root, G(130, 60), G(136, 66)); Enemy.Goat(root, G(124, 54), G(118, 62)); Enemy.Goat(root, G(160, 30), G(165, 40)); Enemy.Goat(root, G(95, 140), G(100, 150));
            Enemy.Dog(root, G(-99, 97), G(-91, 103)); Enemy.Dog(root, G(-130, 57), G(-132, 63)); Enemy.Dog(root, G(-90, 12), G(-75, 8));
            Enemy.Scorpion(root, G(88, 104), G(92, 100)); Enemy.Scorpion(root, G(60, 100), G(64, 96)); Enemy.Scorpion(root, G(170, 128), G(166, 124)); Enemy.Scorpion(root, G(-58, 8), G(-62, 14));
            Enemy.Scorpion(root, G(30, 120), G(26, 126));
        }

        // the arrival spot: inside the barn, by the door (the roof crash lands them here)
        public static Vector3 ArriveAt => new Vector3(0, Ground(0, 29.5f, 2f), 29.5f);
    }
}
