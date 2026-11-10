using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 6, items 6-12: Ancient Egypt, world 2. The ground, the pyramids and the props come from Tools/Blender/egypt_world.py (the same
    // layout numbers); the gameplay is placed on them here, about 10-15 minutes on a first visit:
    //   the town (x -70..70, z -20..125): the arrival plaza, the main street, pastel houses with rooftops to climb, the market (striped
    //     stalls, merchants, the Violinist with his lyre), guards on patrol (they spot you and send you back: light stealth), the cat
    //     temple (its door opens for Kira), the obelisk (double jump, later)
    //   the quarry road (x 40..100, z 30..190): workers haul stones on rollers (ride them), a quicksand pit crossed on a stone block,
    //     the huge block (Muscle Man, later)
    //   the dunes: the judgmental camel at the north gate; the whole party rides it across the dunes to the Sphinx
    //   the Great Sphinx with Kira's face (x -95..-20, z 188..242): a landmark seen from everywhere; SphinxGag the first time you reach
    //     it; climbable (paws, back, head)
    //   the pyramid under construction (x 42..98, z 207..263): scaffolding up its south face, workers with stone carts, the top, the way in
    //   the tomb (the pyramid's inside, a space of its own at Tomb): dark corridors the necklace lights, swinging blades, a dart wall, a
    //     sand-pit hall, sliding blocks, a turning bridge, the mummies' gallery, a counterweight lift, a hidden cat chamber, the burial
    //     chamber with the red story shard and the vortex gate; a cracked wall (charged punch) and a narrow shaft (wall jump) for later
    //   the Nile bank (x -175..-75): reeds, boats as moving platforms to the far bank, a swim, the deep pool (deep dive, later)
    // Enemies: mummies (sleep until someone passes; never wake for mummy Kira), scarabs (small, fast, in threes), temple guardians
    // (statues that wake when you pass, then a short chase). Placeholders until the models are in.
    public static class Egypt
    {
        static Transform root;
        public const string Start = "EG-start", VortexCp = "EG-vortex", TombCp = "EG-tomb", TopCp = "EG-top";
        public static readonly Vector3 Tomb = new Vector3(500, 60, 0);   // the tomb's entry (its floor y)
        public static readonly Vector3 SphinxAt = new Vector3(-55, 0, 215);
        public static readonly Vector3 ViolinistAt = new Vector3(9, 0, 73);   // in the market's middle, out of the alley guards' sight
        public static readonly Vector3 CamelStop = new Vector3(24, 0, 128);
        public static readonly Vector3[] CamelRoute = { new Vector3(24, 0, 128), new Vector3(14, 0, 150), new Vector3(-6, 0, 172), new Vector3(-24, 0, 186) };
        public static Portal Gate { get; private set; }
        public static Transform Sphinx { get; private set; }
        public static Camel TheCamel { get; private set; }
        public static readonly Color Red = new Color(1f, .18f, .14f);

        public static Transform Build(WorldData w)
        {
            root = new GameObject("World Egypt").transform;
            var save = SaveGame.Current; var prog = save.World(w.worldId);
            var terrain = WorldArt.SpawnFile("egypt_terrain", root);
            if (terrain)
                foreach (var mf in terrain.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf.name.StartsWith("FarSand")) { mf.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; continue; }
                    var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; if (mf.name.StartsWith("Terrain")) terrainCol = mc;
                }
            else Box("Fallback ground", new Vector3(0, -.5f, 150f), new Vector3(400, 1, 500), "Sand");
            var pyr = WorldArt.SpawnFile("egypt_pyramids", root);
            if (pyr)
                foreach (var mf in pyr.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf.name.StartsWith("BuildPyramid")) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                    else mf.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            Physics.SyncTransforms();

            Town(prog); Market(); Quarry(); Dunes(); SphinxSite(); Scaffold(); Nile(); TombBuild(save, prog);
            // invisible walls round the playable desert
            foreach (var (c, s) in new[] { (new Vector3(-10, 40, -62), new Vector3(400, 120, 4)), (new Vector3(-10, 40, 330), new Vector3(400, 120, 4)),
                                           (new Vector3(-182, 40, 134), new Vector3(4, 120, 400)), (new Vector3(152, 40, 134), new Vector3(4, 120, 400)) })
                Box("Desert boundary", c, s, Color.clear, "Water").GetComponent<Renderer>().enabled = false;
            Physics.SyncTransforms();

            foreach (var s in w.shards)
                if (!prog.shards.Contains(s.id)) Pickup.Make(Pickup.Kind.Shard, s.id, s.area, ShardPos(s.position), root, new Color(1f, .78f, .35f));
            PlayLog.Line("Egypt shards placed: " + string.Join(" ", w.shards.Select(s => $"{s.id}{Fmt(ShardPos(s.position))}")));
            var a = w.Area("E");
            var story = Pickup.Make(Pickup.Kind.StoryShard, "story-" + w.worldId, a.id, a.storyShard, root, a.storyShardColor);
            story.gameObject.SetActive(prog.necklaceAwake && !prog.storyShard);
            Game.I.StoryShard = story;
            var sp = story.transform.position;
            Game.I.StoryBeam = Box("Story beam", new Vector3(sp.x, sp.y + 6f, sp.z), new Vector3(.4f, 12f, .4f), new Color(1f, .4f, .3f), "Glow", false).gameObject;
            Game.I.StoryBeam.SetActive(prog.necklaceAwake && !prog.storyShard);
            if (!save.heartUpgrades.Contains("EG-H1")) Pickup.Make(Pickup.Kind.HeartUpgrade, "EG-H1", "E", new Vector3(549, 61.2f, 100), root, Playground.Accent);
            if (!save.heartUpgrades.Contains("EG-H2")) Pickup.Make(Pickup.Kind.HeartUpgrade, "EG-H2", "E", new Vector3(517, 61.2f, 124.5f), root, Playground.Accent);
            foreach (var h in new[] { new Vector3(-10, 0, 60), new Vector3(50, 0, 130), new Vector3(-20, 0, 196), new Vector3(66, 0, 192), new Vector3(-86, 0, 60) })
                Pickup.HealHeart(OnGround(h.x, h.z) + Vector3.up * 1f, root);
            Pickup.HealHeart(new Vector3(500, 61f, 8), root); Pickup.HealHeart(new Vector3(500, 61f, 84), root);

            Enemies();
            return root;
        }

        public static Vector3 ShardPos(Vector3 hint)
        {
            float from = hint.y + 1.6f;
            if (terrainCol && terrainCol.Raycast(new Ray(new Vector3(hint.x, 200f, hint.z), Vector3.down), out var hit, 400f)) from = Mathf.Max(from, hit.point.y + 1.6f);   // a dune higher than the hint
            return new Vector3(hint.x, Ground(hint.x, hint.z, from) + 1f, hint.z);
        }
        static Collider terrainCol;
        static string Fmt(Vector3 v) => $"({v.x:0},{v.y:0.0},{v.z:0})";

        // ---- helpers (as IceAge's)
        const int GroundMask = ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << Hero.EnemyLayer) | (1 << 4) | (1 << 2));
        public static float Ground(float x, float z, float fromY = 80f) =>
            Physics.Raycast(new Vector3(x, fromY, z), Vector3.down, out var hit, 300f, GroundMask, QueryTriggerInteraction.Ignore) ? hit.point.y : 0f;
        public static Vector3 OnGround(float x, float z, float fromY = 80f) => new Vector3(x, Ground(x, z, fromY), z);
        static Transform Box(string name, Vector3 centre, Vector3 size, string mat, bool collider = true) =>
            Placeholders.Prim(PrimitiveType.Cube, root, centre, size, WorldArt.Mat(mat), collider, name).transform;
        static Transform Box(string name, Vector3 centre, Vector3 size, Color c, string kind = "Lit", bool collider = true) =>
            Placeholders.Prim(PrimitiveType.Cube, root, centre, size, Placeholders.Mat(c, kind), collider, name).transform;
        static Transform Slab(string name, float x0, float x1, float z0, float z1, float top, float bottom, string mat = "Sandstone") =>
            Box(name, new Vector3((x0 + x1) / 2, (top + bottom) / 2, (z0 + z1) / 2), new Vector3(x1 - x0, top - bottom, z1 - z0), mat);
        static void Hint(Vector3 at, float r, System.Func<string> text)
        {
            var go = new GameObject("Hint"); go.transform.SetParent(root, false); go.transform.position = at;
            var h = go.AddComponent<HintZone>(); h.radius = r; h.text = text;
        }
        static string B(Controls.Act a) => Controls.Btn(a);
        static Checkpoint Cp(string id, float x, float z, float yaw) => Playground.CheckpointHere(id, OnGround(x, z), yaw, root);
        static Checkpoint CpAt(string id, Vector3 at, float yaw) => Playground.CheckpointHere(id, at, yaw, root);
        static GameObject Prop(string prop, float x, float z, float yaw = 0, float scale = 1, bool collider = false, float y = float.NaN) =>
            WorldArt.Prop("egypt_props", prop, root, float.IsNaN(y) ? OnGround(x, z) : new Vector3(x, y, z), yaw, scale, collider);
        // a solid box collider round a prop's shape (houses, blocks, stalls: cheap and walkable on top)
        static void Solid(GameObject go, Vector3 size, Vector3 offset = default)
        {
            if (!go) return;
            var c = go.AddComponent<BoxCollider>(); var s = go.transform.lossyScale;
            c.size = new Vector3(size.x / s.x, size.y / s.y, size.z / s.z); c.center = new Vector3(offset.x / s.x, (offset.y + size.y / 2) / s.y, offset.z / s.z);
            // the prop's root carries Blender's 90 deg turn (Z-up): the box is set in world axes instead, on a child
            Object.Destroy(c);
            var b = new GameObject("Solid"); b.transform.SetParent(root, false); b.transform.position = go.transform.position + Quaternion.Euler(0, go.transform.eulerAngles.y, 0) * (offset + Vector3.up * size.y / 2);
            b.transform.rotation = Quaternion.Euler(0, go.transform.eulerAngles.y, 0); b.AddComponent<BoxCollider>().size = size;
        }

        // ---- the town: the plaza, the main street, the houses (rooftops), the north gate, the cat temple
        static void Town(WorldProgress prog)
        {
            Playground.CheckpointHere(Start, OnGround(0, 8), 0, root, false);   // no flag pole here: the arrival cutscene stands on this spot
            Cp(VortexCp, 6, 6, 0);
            Hint(new Vector3(0, 0, 14), 6f, () => $"Ancient Egypt! The red shard on Dan's necklace is calling. Every shard counts: at {Game.World.areas[0].requirement} in all it wakes ({SaveGame.Current.Total} so far).");
            // a little vortex arch on the plaza: where the vortex sets you down (the gate itself opens in the tomb)
            for (int i = 0; i < 9; i++) { float a = i * Mathf.PI / 8; Box("Arrival arch", new Vector3(6 + Mathf.Cos(a) * 2.2f, .2f + Mathf.Sin(a) * 2.2f, 1), new Vector3(.6f, .6f, .5f), "Sandstone"); }
            // houses: west and east rows along the main street, both sides of the market; 6 x 6 m, roofs at 4.6 m (5 with the parapet)
            var houses = new List<(float x, float z, int kind, bool dome)> {
                (-27, 26, 1, false), (-27, 40, 2, true), (-17, 24, 3, false), (-17, 40, 4, false),
                (27, 26, 2, false), (27, 40, 3, true), (17, 24, 4, false), (17, 40, 1, false),
                (-27, 104, 3, false), (-17, 104, 1, true), (-27, 116, 4, false), (27, 100, 2, true), (17, 100, 3, false), (27, 114, 1, false), (17, 116, 4, false),
                (-43.5f, 40, 2, false), (-40, 110, 1, false), (40, 104, 3, false), (40, 30, 4, true), (-60, 100, 2, false), (52, 20, 1, false) };
            foreach (var (x, z, k, dome) in houses)
            {
                float yaw = x < 0 ? 90 : -90; float g = OnGround(x, z).y;   // the ground before the house's own collider is there
                var h = Prop("House" + k, x, z, yaw, 1f, false, g); Solid(h, new Vector3(6, 4.6f, 6));
                if (dome) Prop("Dome", x, z, 0, .9f, false, g + 4.6f);
            }
            // the far roof (x -43.5): 10.5 m from the west roofs (Round 7: it was 19 m): only Kira's pounce clears it (Bethlehem); Abi's double jump reaches ~9.8 m
            Hint(new Vector3(-29, 5, 40), 4f, () => "That far roof is too far to jump... A cat could pounce it, one day. (later)");
            // ways up: outside stairs (0.78 m steps, a jump each) beside three houses; crates as decoration
            for (int i = 0; i < 6; i++) Slab("House stair", -23.8f, -22.4f, 49 - i * 1.0f, 50 - i * 1.0f, (i + 1) * .78f, 0, "Plaster");
            foreach (var (x, z) in new[] { (-21.2f, 30f), (-21.6f, 31.4f), (22f, 34f) }) Prop("Crate", x, z, x * 9, 1f, true);
            for (int i = 0; i < 6; i++) Slab("House stair", 20.2f + i * .01f, 21f, 104 - i * 1.0f, 105 - i * 1.0f, (i + 1) * .78f, 0, "Plaster");
            for (int i = 0; i < 6; i++) Slab("House stair", -21f, -20.2f, 98 + i * 1.0f, 99 + i * 1.0f, (i + 1) * .78f, 0, "Plaster");
            // planks between the west roofs (z 26 / 40) and north-west roofs (z 104 / 116)
            Box("Roof plank", OnGround(-27, 33) + new Vector3(0, 4.55f, 0), new Vector3(2f, .2f, 8.5f), "Wood");
            // the town wall's north gate (z 122), palms, pots, baskets
            // the wall (each segment 12 m): x -66..-6 and 6..30, the gate between; open east of x 30 (the quarry road) and west of -66
            foreach (var x in new[] { -60f, -48f, -36f, -24f, -12f, 12f, 24f }) Prop("WallSeg", x, 122, 0, 3f, true);
            foreach (var x in new[] { -7.5f, 7.5f }) Prop("Column", x, 122, 0, 1f, true);
            foreach (var (x, z, s) in new[] { (-10f, 12f, 1f), (10f, 12f, .9f), (-34f, 60f, 1.1f), (34f, 62f, 1f), (-34f, 84f, .95f), (34f, 82f, 1.05f), (-8f, 118f, 1f), (9f, 118f, .9f), (46f, 70f, 1f), (-46f, 50f, 1.1f) })
                Prop("Palm", x, z, x * 7, s);
            foreach (var (x, z) in new[] { (-9f, 22f), (9f, 26f), (-9f, 90f), (9f, 92f), (12f, 108f) }) Prop("Pot", x, z, x * 13, 1f, true);
            // the cat temple (west, x -62..-44, z 60..84): columns, two cat statues, a door that opens for Kira
            Slab("Cat temple floor", -66, -44, 58, 86, .1f, -1f, "Sandstone");   // a low lip: Kira steps over it
            Slab("Cat temple wall W", -66, -64, 58, 86, 7f, 0); Slab("Cat temple wall S", -66, -44, 58, 60, 7f, 0); Slab("Cat temple wall N", -66, -44, 84, 86, 7f, 0);
            Slab("Cat temple front S", -46, -44, 60, 70, 7f, 0); Slab("Cat temple front N", -46, -44, 74, 84, 7f, 0); Slab("Cat temple lintel", -46, -44, 70, 74, 7f, 4.5f);
            Slab("Cat temple roof", -66, -44, 58, 86, 7.6f, 7f, "Plaster");
            foreach (var z in new[] { 66f, 78f }) Prop("Column", -42, z, 0, 1f, true);
            foreach (var z in new[] { 68.5f, 75.5f }) Prop("CatStatue", -41.5f, z, 270, 1f, true);
            var door = Slab("Cat temple door", -45.6f, -44.4f, 70, 74, 4.5f, .1f, "Sandstone");
            CatPlate.Make(root, OnGround(-40, 72) + Vector3.up * .02f, "EG-cat-temple", door, prog);
            Hint(new Vector3(-40, 0, 72), 4.5f, () => "A cat-shaped plate in front of the temple door. Only a cat can wake it.");
            var light = new GameObject("Temple light"); light.transform.SetParent(root, false); light.transform.position = new Vector3(-55, 4, 72);
            var lt = light.AddComponent<Light>(); lt.type = LightType.Point; lt.range = 18; lt.intensity = 3; lt.color = new Color(1f, .85f, .55f);
        }

        // ---- the market: stalls, merchants, baskets, the obelisk (double jump, later), the Violinist with his lyre, the guards
        static void Market()
        {
            Cp("EG-market", 0, 52, 0);
            foreach (var (x, z, yaw, k) in new[] { (-22f, 56f, 90f, 1), (-22f, 66f, 90f, 2), (-22f, 76f, 90f, 1), (22f, 56f, -90f, 2), (22f, 76f, -90f, 1), (-12f, 84f, 180f, 2), (12f, 84f, 180f, 1) })
            {
                var st = Prop(k == 1 ? "Stall" : "Stall2", x, z, yaw, 1f); Solid(st, new Vector3(2.9f, 1f, 1f), Quaternion.Euler(0, yaw, 0) * new Vector3(0, 0, -.6f));
                for (int i = 0; i < 3; i++) Prop(i == 1 ? "Pot" : "Basket", x + Mathf.Sin(yaw * Mathf.Deg2Rad) * -1.6f + (i - 1) * .9f * Mathf.Cos(yaw * Mathf.Deg2Rad), z + Mathf.Cos(yaw * Mathf.Deg2Rad) * -1.6f, i * 40, .9f);
                Merchant.Make(root, OnGround(x, z) + Quaternion.Euler(0, yaw, 0) * new Vector3(0, 0, .6f), yaw + 180, k);
            }
            // the obelisk in the middle: a pedestal (2.4 m, crates up), its top ledge at 5.6 m (Abi's double jump)
            Slab("Obelisk pedestal", -2, 2, 65, 69, 2.4f, 0, "Sandstone");
            Prop("Obelisk", 0, 67, 0, 1.25f, false, 2.4f);
            Box("Obelisk shaft", new Vector3(0, 2.4f + 5f, 67), new Vector3(1.6f, 10f, 1.6f), Color.clear, "Water").GetComponent<Renderer>().enabled = false;
            Slab("Obelisk ledge", -1.4f, 1.4f, 63.4f, 64.6f, 5.6f, 5.3f, "Gold");
            Prop("Crate", 3.1f, 67, 15, 1.2f, true); Prop("Crate", 3.3f, 68.3f, 0, 1.2f, true, 1.2f);
            Hint(new Vector3(0, 2.4f, 66), 2.5f, () => SaveGame.Current.Has(Abilities.DoubleJump) ? "The ledge up there: Abi's double jump reaches it!" : "A golden ledge high up the obelisk. Too high for one jump... (Abi's double jump)");
            // the Violinist with his lyre (he teaches Abi the double jump)
            var v = Violinist.Make(root, OnGround(ViolinistAt.x, ViolinistAt.z), 225f, "Lyre");
            v.ability = Abilities.DoubleJump; v.learner = "Abi"; v.moveName = "Double Jump";
            // guards: they walk the side streets and the market's edge; if one sees you (10 m, in front, nothing in between) he sends
            // you back to the last checkpoint. The main street's stalls are cover.
            // Round 6 tuning: they walk the market's back alleys (behind the stalls, where two shards are), not the main street
            Guard.Make(root, new Vector3(-31, 0, 50), new Vector3(-31, 0, 88), "Guard west");
            Guard.Make(root, new Vector3(31, 0, 88), new Vector3(31, 0, 50), "Guard east");
            Hint(new Vector3(-24, 0, 66), 5f, () => "A guard walks the alley behind the stalls. Slip past when his back is turned, or he'll send you back!");
            Hint(new Vector3(24, 0, 66), 5f, () => "A guard walks the alley behind the stalls. Slip past when his back is turned, or he'll send you back!");
        }

        // ---- the quarry road: stones on rollers (moving platforms with workers), the quicksand pit crossed on a block, the huge block
        static void Quarry()
        {
            Cp("EG-quarry", 52, 44, 0);
            // two stones hauled up and down the road (ride them), a worker pulling each
            foreach (var (x, z0, z1, ph) in new[] { (50f, 50f, 112f, 0f), (70f, 112f, 50f, 9f) })
            {
                var block = Box("Hauled stone", new Vector3(x, .9f, z0), new Vector3(2.6f, 1.6f, 2.2f), "Sandstone");
                var mp = block.gameObject.AddComponent<MovingPlatform>(); mp.a = new Vector3(x, .9f, z0); mp.b = new Vector3(x, .9f, z1); mp.period = 26f;
                for (int i = 0; i < 3; i++) Placeholders.Prim(PrimitiveType.Cylinder, block, new Vector3((i - 1) * .3f, -.6f, 0), new Vector3(.12f, .55f, .12f), WorldArt.Mat("Wood"), name: "Roller").transform.localRotation = Quaternion.Euler(0, 0, 90);
                Worker.Make(root, block, new Vector3(0, -.9f, 2.4f));
            }
            // the quicksand pit (x 45..75, z 138..162): sinking sand sends you back; a stone block shuttles across it (ride it)
            var qs = Box("Quicksand", new Vector3(60, -.3f, 150), new Vector3(30, 1.2f, 24), new Color(.86f, .7f, .45f), "Lit", false);
            qs.gameObject.AddComponent<Quicksand>();
            var shuttle = Box("Shuttle stone", new Vector3(60, .55f, 136), new Vector3(3f, 1.4f, 3f), "Sandstone");
            var sm = shuttle.gameObject.AddComponent<MovingPlatform>(); sm.a = new Vector3(60, .55f, 134.5f); sm.b = new Vector3(60, .55f, 165.5f); sm.period = 14f;
            Worker.Make(root, shuttle, new Vector3(0, -.55f, -2f));
            Hint(new Vector3(60, 0, 132), 6f, () => "Quicksand! Ride the stone across.");
            Cp("EG-quicksand", 60, 172, 0);
            // the huge block in the quarry (Muscle Man) and the quarry's stones
            var huge = Box("Huge block (needs Muscle Man)", new Vector3(86, 2.5f, 61), new Vector3(3f, 5f, 12f), "Sandstone"); huge.gameObject.AddComponent<Boulder>();
            Slab("Quarry wall N", 84, 100, 72, 74, 6, 0); Slab("Quarry wall S", 84, 100, 48, 50, 6, 0); Slab("Quarry wall E", 98, 100, 50, 72, 6, 0);
            foreach (var (x, z, yaw) in new[] { (70f, 40f, 10f), (74f, 44f, 80f), (80f, 90f, 30f), (44f, 128f, 0f), (76f, 128f, 60f) }) Prop("Block", x, z, yaw, 1f, true);
            Prop("Cart", 66, 100, 20, 1f, true);
        }

        // ---- the dunes: the camel at the north gate, its route to the Sphinx
        static void Dunes()
        {
            Cp("EG-gate", 10, 126, 0);
            TheCamel = Camel.Make(root, CamelRoute.Select(p => OnGround(p.x, p.z)).ToArray());
            for (int e = 0; e < 2; e++)   // three steps up to the saddle (~2.3 m), right beside the camel
                foreach (var (p, top) in MountSteps(e))
                {
                    float g = OnGround(p.x, p.z).y;
                    Box("Mounting step", new Vector3(p.x, (g + top - 1f) / 2f, p.z), new Vector3(1.3f, g + top + 1f, 1.3f), "Sandstone");
                }
            Hint(CamelStop + new Vector3(-4, 0, 0), 5f, () => $"The camel takes the whole party across the dunes to the Sphinx. Jump up on its saddle ({B(Controls.Act.Jump)})!");
            foreach (var (x, z, s) in new[] { (30f, 160f, 1.1f), (-30f, 150f, 1f), (44f, 176f, .9f), (-40f, 176f, 1.2f) }) Prop("Palm", x, z, x * 3, s);
        }

        // where the camel stands at an end of its route (0 home, 1 the Sphinx), and its mounting block and step on its left side
        public static (Vector3 at, float top)[] MountSteps(int end)
        {
            var (block, step) = Mount(end); var side = (step - block).normalized; var at = block - side * 2.4f;
            return new[] { (at + side * 1.75f, 1.9f), (at + side * 3.05f, 1.25f), (at + side * 4.35f, .6f) };
        }
        public static (Vector3 block, Vector3 step) Mount(int end)
        {
            var at = end == 0 ? CamelRoute[0] : CamelRoute[CamelRoute.Length - 1];
            var from = end == 0 ? CamelRoute[1] : CamelRoute[CamelRoute.Length - 2];
            var fwd = end == 0 ? (from - at) : (at - from); fwd.y = 0; fwd.Normalize();
            var side = new Vector3(fwd.z, 0, -fwd.x);   // its right side (the open side at both ends; the left faces the town wall)
            return (at + side * 2.4f, at + side * 4.1f);
        }

        // ---- the Great Sphinx with Kira's face: the Meshy model (22 m), a landmark; its plinth, paws, back and head are walkable
        static void SphinxSite()
        {
            Cp("EG-sphinx", -22, 196, 270);
            Slab("Sphinx plinth", -84, -28, 200, 230, 2f, -1f);
            // a ramp up its front (2 m over 7 m)
            var pr = Box("Plinth ramp", new Vector3(-24.6f, 1f, 215f), new Vector3(7.4f, .3f, 20f), "Sandstone"); pr.rotation = Quaternion.Euler(0, 0, -Mathf.Atan2(2f, 7f) * Mathf.Rad2Deg);   // high end (west) at the plinth
            var sph = StaticModel.Spawn("sphinx_kira", root, new Vector3(SphinxAt.x, 2f, SphinxAt.z), -90f, 22f);   // its face (Blender -Y) toward the east, where the party comes from
            Sphinx = sph ? sph.transform : null;
            // the walkable shape (whatever the model is: placed from these numbers): paws, body, back, head
            Slab("Sphinx paw R", -48, -32, 205, 211, 4f, 2f); Slab("Sphinx paw L", -48, -32, 219, 225, 4f, 2f);
            foreach (var z in new[] { 208f, 222f }) Slab("Paw step", -31.5f, -30f, z - 1.2f, z + 1.2f, 3f, 2f, "Sandstone");   // 1 m up, then 1 m onto the paw
            Slab("Sphinx chest", -52, -44, 207, 223, 11f, 2f); Slab("Sphinx body", -80, -52, 207, 223, 13f, 2f);
            Slab("Sphinx neck", -54, -42, 209, 221, 16f, 11f); Slab("Sphinx head", -52, -40, 210, 220, 22f, 16f);
            foreach (var t in root.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Sphinx ") && t.name != "Sphinx plinth"))
                if (Sphinx && t.GetComponent<Renderer>()) t.GetComponent<Renderer>().enabled = false;   // the model is what you see
            // the way up: restoration blocks along its south flank (each 1.3 m higher), a ladder of scaffold up the neck to the head
            for (int i = 0; i < 9; i++) Slab("Restoration block", -78 + i * 3f, -75.6f + i * 3f, 203.2f, 207f, 2f + 1.25f * (i + 1), -1f);
            for (int i = 0; i < 4; i++) Slab("Head scaffold", -57f + i * 1.6f, -55.4f + i * 1.6f, 223f, 225f, 13f + 1.25f * (i + 1), 12f, "Wood");
            Box("Head plank", new Vector3(-50.5f, 17.9f, 222.5f), new Vector3(5f, .2f, 2.2f), "Wood");
            Box("Head step", new Vector3(-47.5f, 19.3f, 221.5f), new Vector3(2f, .2f, 2f), "Wood");
            Box("Head step 2", new Vector3(-46f, 20.7f, 220.6f), new Vector3(2f, .2f, 1.4f), "Wood");
            Hint(new Vector3(-30, 2, 215), 8f, () => "The Great Sphinx... with a very familiar face.");
            var gag = new GameObject("Sphinx gag"); gag.transform.SetParent(root, false); gag.transform.position = new Vector3(-26, 0, 215);
            gag.AddComponent<SphinxGagZone>();
            // temple guardians by the plinth: stone cats that wake when you pass, chase a little, then go back
            Enemy.Guardian(root, OnGround(-26, 202) + Vector3.up * .1f, 270); Enemy.Guardian(root, OnGround(-26, 228) + Vector3.up * .1f, 270);
            foreach (var z in new[] { 200f, 230f }) Prop("Column", -24, z, 0, 1f, true);
        }

        // ---- the pyramid under construction: a scaffold tower in front of its south face (z 196..206), levels every 4 m, ramps between,
        // workers pushing stone carts; a bridge from the top level onto the unfinished top (y 36); the way in
        static void Scaffold()
        {
            Cp("EG-pyramid", 70, 190, 0);
            const float x0 = 56, x1 = 84, z0 = 196, z1 = 206;
            for (int lv = 1; lv <= 9; lv++)
            {
                float y = lv * 4f;
                bool west = lv % 2 == 0;   // levels alternate their open end; a ramp climbs from one level to the next
                Box($"Scaffold level {lv}", new Vector3((x0 + x1) / 2, y - .1f, (z0 + z1) / 2), new Vector3(x1 - x0, .2f, z1 - z0), "Wood");
                // the ramp up to this level from the one below: 4 m up over 11 m, along the level's south edge
                float rx = west ? x0 + 7 : x1 - 7; float dir = west ? -1 : 1;
                var ramp = Box($"Scaffold ramp {lv}", new Vector3(rx, y - 2f, z0 - 1.6f), new Vector3(11.8f, .25f, 3f), "Wood");
                ramp.rotation = Quaternion.Euler(0, 0, -dir * Mathf.Atan2(4f, 11f) * Mathf.Rad2Deg);
                // poles at the corners
                foreach (var px in new[] { x0, (x0 + x1) / 2, x1 })
                    foreach (var pz in new[] { z0, z1 })
                    { var p = WorldArt.Prop("egypt_props", "Pole", root, new Vector3(px, y - 4f, pz), 0, 1f); if (p) p.transform.localScale = Vector3.Scale(p.transform.localScale, new Vector3(1, 1, 4f)); }
                // rails on the open side
                if (lv < 9) Box("Scaffold rail", new Vector3((x0 + x1) / 2, y + .5f, z1 + .1f), new Vector3(x1 - x0, .15f, .15f), "Wood");   // level 9: open to the bridge
            }
            // (level 1's ramp starts on the ground at the east end); the top bridge onto the pyramid's unfinished top
            Box("Top bridge", new Vector3(70, 35.9f, 217.5f), new Vector3(3f, .2f, 23f), "Wood");
            Box("Top bridge rail W", new Vector3(68.4f, 36.5f, 217.5f), new Vector3(.15f, .15f, 23f), "Wood"); Box("Top bridge rail E", new Vector3(71.6f, 36.5f, 217.5f), new Vector3(.15f, .15f, 23f), "Wood");
            // the double-jump plank (over the top level, east)
            Box("High plank", new Vector3(88, 39.2f, 199), new Vector3(3f, .2f, 2.4f), "Wood");
            // workers' carts on levels 2 and 6
            foreach (var lv in new[] { 2, 6 })
            {
                var cart = Box("Stone cart", new Vector3(60, lv * 4f + .6f, 202), new Vector3(2.2f, 1f, 1.6f), "Wood");
                Placeholders.Prim(PrimitiveType.Cube, cart, new Vector3(0, .9f, 0), new Vector3(.8f, .9f, .8f), WorldArt.Mat("Sandstone"), name: "Load");
                var mp = cart.gameObject.AddComponent<MovingPlatform>(); mp.a = new Vector3(60, lv * 4f + .6f, 202); mp.b = new Vector3(80, lv * 4f + .6f, 202); mp.period = 16f + lv;
                Worker.Make(root, cart, new Vector3(-1.8f, -.6f, 0));
            }
            // the top: blocks waiting to be set, the way in (a doorway down into the tomb)
            CpAt(TopCp, new Vector3(70, 36f, 232), 180);
            foreach (var (x, z) in new[] { (66f, 232f), (74f, 239f) }) Prop("Block", x, z, x * 5, 1f, true, 36f);
            Slab("Tomb doorway L", 67, 68, 236, 240, 39.5f, 36f); Slab("Tomb doorway R", 72, 73, 236, 240, 39.5f, 36f); Slab("Tomb doorway top", 67, 73, 236, 240, 40.5f, 39.5f);
            Box("Tomb doorway dark", new Vector3(70, 37.75f, 239.6f), new Vector3(4f, 3.5f, .2f), new Color(.05f, .04f, .03f), "Glow", false);
            var into = Playground.PortalHere("EG-tomb-in", new Vector3(70, 36f, 238.5f), 0, TombCp, true, root, new Color(.9f, .7f, .3f));
            into.transform.Find("Disc")?.gameObject.SetActive(false);
            Hint(new Vector3(70, 36, 232), 5f, () => "The way into the pyramid! It's dark in there: Dan's necklace will light the way.");
        }

        // ---- the Nile bank: reeds, two boats to the far bank, the swim, the deep pool
        static void Nile()
        {
            Cp("EG-nile", -84, 60, 270);
            var water = Box("Nile", new Vector3(-127.5f, -6f, 134f), new Vector3(45, 10.8f, 420), new Color(.2f, .55f, .62f, .6f), "Water", false);
            water.gameObject.layer = 4; water.gameObject.AddComponent<WaterVolume>();
            var deepW = Box("Nile deep pool", new Vector3(-128, -14f, 77), new Vector3(16, 6, 16), new Color(.15f, .4f, .5f, .6f), "Water", false);
            deepW.gameObject.layer = 4; deepW.gameObject.AddComponent<WaterVolume>(); deepW.GetComponent<Renderer>().enabled = false;
            var deep = Box("Deep water (deep dive)", new Vector3(-128, -12.5f, 77), new Vector3(16, 9f, 16), Color.clear, "Water", false);
            deep.GetComponent<Renderer>().enabled = false; deep.gameObject.layer = 2; deep.gameObject.AddComponent<DeepWater>();
            Hint(new Vector3(-108, 0, 77), 7f, () => "The river runs very deep here... too deep to dive, for now.");
            foreach (var (z, ph) in new[] { (40f, 0f), (110f, 5f) })
            {
                var boat = WorldArt.Prop("egypt_props", "Boat", root, new Vector3(-110, -.6f, z), 90, 1f);
                var deck = Box("Boat deck", new Vector3(-110, -.05f, z), new Vector3(8f, .3f, 2f), Color.clear, "Water");
                deck.GetComponent<Renderer>().enabled = false;
                if (boat) boat.transform.SetParent(deck, true);
                var mp = deck.gameObject.AddComponent<MovingPlatform>(); mp.a = new Vector3(-108, -.05f, z); mp.b = new Vector3(-147, -.05f, z); mp.period = 22f + ph;
            }
            foreach (var (x, z, s) in new[] { (-98f, 22f, 1f), (-100f, 36f, 1.2f), (-97f, 70f, .9f), (-101f, 88f, 1.1f), (-99f, 120f, 1f), (-104f, 150f, 1.2f), (-153f, 30f, 1f), (-155f, 120f, 1.1f) })
                Prop("Reeds", x, z, x * 11, s);
            foreach (var (x, z) in new[] { (-86f, 46f), (-88f, 104f), (-165f, 70f), (-168f, 140f) }) Prop("Palm", x, z, x * 5, 1.1f);
            Cp("EG-farbank", -162, 75, 90);
        }

        // ---- the tomb (inside the pyramid): its own space at Tomb (x 500, floor y 60), dark (the necklace lights it)
        static void TombBuild(SaveGame save, WorldProgress prog)
        {
            float X = Tomb.x, Y = Tomb.y;
            Transform W(string n, float x0, float x1, float z0, float z1, float y0, float y1, string mat = "Sandstone") => Slab(n, X + x0, X + x1, z0, z1, Y + y1, Y + y0, mat);
            var dark = new GameObject("Tomb darkness"); dark.transform.SetParent(root, false); dark.transform.position = new Vector3(X + 10, Y + 5, 70); dark.transform.localScale = new Vector3(110, 30, 150);
            dark.AddComponent<DarkVolume>();
            CpAt(TombCp, new Vector3(X, Y, 4), 0);
            var outP = Playground.PortalHere("EG-tomb-out", new Vector3(X, Y, .8f), 180, TopCp, true, root, new Color(.9f, .7f, .3f));
            // the entry room (x -5..5, z 0..10)
            W("Entry floor", -5, 5, -1, 10, -1, 0); W("Entry wall W", -6, -5, -1, 10, 0, 5); W("Entry wall E", 5, 6, -1, 10, 0, 5); W("Entry wall S", -5, 5, -2, -1, 0, 5); W("Entry ceiling", -6, 6, -2, 10, 5, 6);
            W("Entry wall N W", -5, -2, 9, 10, 0, 5); W("Entry wall N E", 2, 5, 9, 10, 0, 5);
            Hint(new Vector3(X, Y, 6), 4f, () => "The tomb. Watch for traps: blades, darts, sinking sand...");
            // corridor A (x -2..2, z 10..42): three swinging blades, a dart wall
            W("Corridor A floor", -2, 2, 10, 42, -1, 0); W("Corridor A wall W", -3, -2, 10, 42, 0, 4); W("Corridor A wall E", 2, 3, 10, 42, 0, 4); W("Corridor A ceiling", -3, 3, 10, 42, 4, 5);
            foreach (var (z, ph) in new[] { (18f, 0f), (26f, 1.1f), (34f, 2.2f) }) SwingBlade.Make(root, new Vector3(X, Y + 3.9f, z), ph);
            DartWall.Make(root, new Vector3(X - 1.9f, Y + 1f, 30f), new Vector3(1, 0, 0), 3.8f, 2.4f);
            // the sand-pit hall (x -10..10, z 42..62): sinking sand, stepping stones
            W("Sand hall wall W", -11, -10, 42, 62, -2, 6); W("Sand hall wall E", 10, 11, 42, 62, -2, 6); W("Sand hall ceiling", -11, 11, 42, 62, 6, 7);
            W("Sand hall wall S W", -10, -2, 41, 42, 0, 6); W("Sand hall wall S E", 2, 10, 41, 42, 0, 6); W("Sand hall wall N W", -10, -2, 62, 63, 0, 6); W("Sand hall wall N E", 2, 10, 62, 63, 0, 6);
            W("Sand hall floor", -10, 10, 42, 62, -2, -1.2f, "Sand");
            var sand = Box("Tomb quicksand", new Vector3(X, Y - .9f, 52), new Vector3(20, .6f, 20), new Color(.86f, .7f, .45f), "Lit", false); sand.gameObject.AddComponent<Quicksand>();
            W("Sand hall ledge S", -2, 2, 42, 44, -1, 0); W("Sand hall ledge N", -2, 2, 60, 62, -1, 0);
            foreach (var z in SandStones) W("Stepping stone", -1.3f, 1.3f, z - 1.3f, z + 1.3f, -1.2f, .3f);   // a line of stones, 0.7 m gaps
            CpAt("EG-tomb-sand", new Vector3(X, Y, 43), 0);
            Enemy.ScarabSwarm(root, new Vector3(X - 7, Y + .6f, 59), new Vector3(X + 7, Y + .6f, 59));
            W("Scarab ledge", -10, 10, 59.4f, 62, -1.2f, 0f);
            // the sliding-block corridor (x -2..2, z 62..86): blocks slide across from the walls' niches
            W("Corridor B floor", -2, 2, 62, 86, -1, 0); W("Corridor B wall W", -6, -2, 63, 86, 0, 4); W("Corridor B wall E", 2, 6, 63, 86, 0, 4); W("Corridor B ceiling", -6, 6, 63, 86, 4, 5);
            foreach (var (z, ph, from) in new[] { (68f, 0f, -1f), (74f, 1.5f, 1f), (80f, .7f, -1f) })
            {
                var blk = Box("Sliding block", new Vector3(X + from * 4f, Y + 1.5f, z), new Vector3(3.6f, 3f, 2.2f), "Sandstone");
                var mp = blk.gameObject.AddComponent<MovingPlatform>(); mp.a = new Vector3(X + from * 4f, Y + 1.5f, z); mp.b = new Vector3(X + from * .1f, Y + 1.5f, z); mp.period = 3.6f + ph;
                var hurt = new GameObject("Crush"); hurt.transform.SetParent(blk, false); hurt.transform.localScale = new Vector3(1.1f, .9f, 1.05f); var sp = hurt.AddComponent<Spikes>(); sp.cause = "a sliding block";
            }
            CpAt("EG-tomb-blocks", new Vector3(X, Y, 64), 0);
            // the turning room (x -12..12, z 86..106): a pit with a turning bridge; branches east (the mummies' gallery) and west (the lift)
            W("Turning room wall S W", -12, -2, 85, 86, -10, 7); W("Turning room wall S E", 2, 12, 85, 86, -10, 7); W("Turning room ceiling", -12, 12, 86, 106, 7, 8);
            W("Turning room wall N W", -12, -4, 106, 107, -10, 7); W("Turning room wall N E", 4, 12, 106, 107, -10, 7);
            W("Turning room ledge S", -2, 2, 86, 88, -1, 0); W("Turning room ledge N", -4, 4, 104, 106, -1, 0);
            W("Turning room ledge E", 8, 12, 92, 100, -1, 0); W("Turning room ledge W", -12, -8, 92, 100, -1, 0);
            W("Turning room wall E S", 12, 13, 86, 92, -10, 7); W("Turning room wall E N", 12, 13, 100, 106, -10, 7);
            W("Turning room wall W S", -13, -12, 86, 92, -10, 7); W("Turning room wall W N", -13, -12, 100, 106, -10, 7);
            var pit = new GameObject("Pit"); pit.transform.SetParent(root, false); pit.transform.position = new Vector3(X, Y - 9f, 96); pit.transform.localScale = new Vector3(24, 2, 20); pit.AddComponent<FallVolume>();
            W("Pit floor", -12, 12, 86, 106, -11, -10, "Sand");
            var bridge = Box("Turning bridge", new Vector3(X, Y - .1f, 96), new Vector3(18f, .4f, 2.4f), "Wood");
            bridge.gameObject.AddComponent<MovingPlatform>().Hold(bridge.position);   // carries riders; StepTurner turns it
            bridge.gameObject.AddComponent<StepTurner>();
            Box("Turning hub", new Vector3(X, Y - 5f, 96), new Vector3(1.4f, 10f, 1.4f), "Sandstone");
            CpAt("EG-tomb-turn", new Vector3(X, Y, 87), 0);
            // the mummies' gallery (east, x 12..44, z 92..100) and, behind a cat door, the hidden chamber (x 44..52)
            W("Gallery floor", 12, 44, 92, 100, -1, 0); W("Gallery wall S", 12, 44, 91, 92, 0, 4); W("Gallery wall N", 12, 44, 100, 101, 0, 4); W("Gallery ceiling", 12, 44, 91, 101, 4, 5);
            foreach (var x in new[] { 18f, 26f, 34f, 40f }) Prop("Sarcophagus", X + x, 99.2f, 180, .9f, true, Y);
            Enemy.Mummy(root, new Vector3(X + 20, Y, 93.5f), new Vector3(X + 30, Y, 93.5f)); Enemy.Mummy(root, new Vector3(X + 32, Y, 98f), new Vector3(X + 24, Y, 98f));
            Enemy.Mummy(root, new Vector3(X + 39, Y, 95f), new Vector3(X + 39, Y, 95f));
            Hint(new Vector3(X + 14, Y, 96), 3.5f, () => "Sleeping mummies... they wake for anyone but a mummy. Mummy Kira can tiptoe past.");
            W("Hidden chamber floor", 44, 52, 88, 104, -1, 0); W("Hidden chamber wall E", 52, 53, 88, 104, 0, 4); W("Hidden chamber wall S", 44, 52, 87, 88, 0, 4); W("Hidden chamber wall N", 44, 52, 104, 105, 0, 4);
            W("Hidden chamber ceiling", 44, 53, 87, 105, 4, 5); W("Gallery end S", 44, 45, 88, 94, 0, 4); W("Gallery end N", 44, 45, 98, 104, 0, 4);
            var cdoor = W("Cat door", 44, 45, 94, 98, 0, 4);
            CatPlate.Make(root, new Vector3(X + 42.2f, Y + .02f, 96), "EG-cat-tomb", cdoor, prog);
            Prop("CatStatue", X + 43, 92.8f, 270, .7f, true, Y);
            // the counterweight lift (west, x -44..-12): Dan pushes the weight onto the plate, the lift rises to the high ledge
            W("Lift hall floor", -44, -12, 90, 102, -1, 0); W("Lift hall wall S", -44, -12, 89, 90, 0, 9); W("Lift hall wall N", -44, -12, 102, 103, 0, 9); W("Lift hall wall W", -45, -44, 89, 103, 0, 9);
            W("Lift hall ceiling", -45, -12, 89, 103, 9, 10);
            W("High ledge", -30, -24, 93, 99, 5.8f, 6f);
            var lift = Box("Lift", new Vector3(X - 20, Y + .1f, 96), new Vector3(3f, .3f, 3f), "Wood");
            var plate = Box("Weight plate", new Vector3(X - 37.5f, Y + .03f, 96), new Vector3(3f, .06f, 3f), "Gold", false);
            var weight = Box("Counterweight", new Vector3(X - 34, Y + 1f, 96), new Vector3(1.9f, 2f, 1.9f), "Stone");
            var pb = weight.gameObject.AddComponent<PushBlock>(); pb.speed = 2.4f; pb.limits = new Bounds(new Vector3(X - 30, Y + 1f, 96), new Vector3(24f, 4f, .5f));   // it slides east-west only
            var cw = lift.gameObject.AddComponent<Counterweight>(); cw.weight = weight; cw.plate = plate; cw.low = lift.position; cw.high = lift.position + Vector3.up * 5.9f;
            lift.gameObject.AddComponent<MovingPlatform>().Hold(lift.position);
            Hint(new Vector3(X - 16, Y, 96), 4f, () => "A lift and a stone weight. Push the weight onto the golden plate (Dan can).");
            // the burial chamber (x -12..12, z 106..136): the sarcophagus, the story shard, the vortex gate; braziers light it
            W("Chamber floor", -12, 12, 106, 136, -1, 0); W("Chamber wall W S", -13, -12, 107, 116, 0, 12); W("Chamber wall W N", -13, -12, 120, 136, 0, 12); W("Chamber wall E S", 12, 13, 107, 120, 0, 12); W("Chamber wall E N", 12, 13, 124, 136, 0, 12);
            W("Chamber wall N", -13, 13, 136, 137, 0, 12); W("Chamber ceiling", -13, 13, 106, 137, 12, 13);
            Prop("Sarcophagus", X, 124, 180, 1.2f, true, Y);
            foreach (var (x, z) in new[] { (-9f, 110f), (9f, 110f), (-9f, 133f), (9f, 133f) })
            {
                Prop("Brazier", X + x, z, 0, 1f, true, Y);
                var l = new GameObject("Brazier light"); l.transform.SetParent(root, false); l.transform.position = new Vector3(X + x, Y + 2f, z);
                var lt = l.AddComponent<Light>(); lt.type = LightType.Point; lt.range = 14; lt.intensity = 3.5f; lt.color = new Color(1f, .62f, .3f); lt.shadows = LightShadows.None;
                Placeholders.Prim(PrimitiveType.Sphere, root, new Vector3(X + x, Y + 1.55f, z), new Vector3(.5f, .4f, .5f), Placeholders.Mat(new Color(1f, .55f, .2f), "Glow"), name: "Flame");
            }
            Gate = Playground.PortalHere("EG-vortex-gate", new Vector3(X, Y, 133.5f), 180, Vortex.Checkpoint, false, root, Red, vortex: true);
            // the cracked wall (charged punch) east, a secret room behind it; the narrow shaft (wall jump) west
            if (!prog.opened.Contains("EG-crack-tomb"))
            {
                var rock = W("Cracked wall", 12, 13, 120, 124, 0, 12); rock.gameObject.AddComponent<RockWall>().id = "EG-crack-tomb";
                for (int i = 0; i < 3; i++) Placeholders.Prim(PrimitiveType.Cube, rock, new Vector3(-.55f, .1f * i - .1f, 0), new Vector3(.05f, .5f, .1f), Placeholders.Mat(new Color(.3f, .22f, .15f)), name: "Crack").transform.localRotation = Quaternion.Euler(i * 40 - 30, 0, 0);
            }
            W("Secret room floor", 13, 20, 118, 126, -1, 0); W("Secret room walls N", 13, 20, 126, 127, 0, 5); W("Secret room walls S", 13, 20, 117, 118, 0, 5); W("Secret room wall E", 20, 21, 117, 127, 0, 5); W("Secret room ceiling", 13, 21, 117, 127, 5, 6);
            Hint(new Vector3(X + 10.5f, Y, 122), 2.5f, () => "This wall is cracked... a much stronger punch could break it. (later)");
            W("Shaft floor", -16, -12, 116, 120, -1, 0); W("Shaft wall W", -17, -16, 115, 121, 0, 13); W("Shaft wall N", -16, -12, 120, 121, 0, 13); W("Shaft wall S", -16, -12, 115, 116, 0, 13);
            W("Shaft top", -16, -12, 116, 120, 10.6f, 11f);
            Hint(new Vector3(X - 13, Y, 118), 2.5f, () => "A narrow shaft going up, its walls close together. Someone who can kick off walls could climb it... (later)");
            CpAt("EG-chamber", new Vector3(X, Y, 109), 0);
            Hint(new Vector3(X, Y, 116), 6f, () => SaveGame.Current.World(WorldData.Egypt).necklaceAwake ? "The pharaoh's burial chamber. The red shard is here!" : "The pharaoh's burial chamber. Dan's necklace stirs... find more shards to wake it.");
            _ = outP; _ = save;
        }

        static void Enemies()
        {
            // scarabs in the dunes and by the quarry, guardians by the pyramid, a mummy at the scaffold's foot
            Enemy.ScarabSwarm(root, OnGround(30, 160) + Vector3.up * .3f, OnGround(40, 180) + Vector3.up * .3f);
            Enemy.ScarabSwarm(root, OnGround(-8, 150) + Vector3.up * .3f, OnGround(-18, 166) + Vector3.up * .3f);
            Enemy.ScarabSwarm(root, OnGround(66, 70) + Vector3.up * .3f, OnGround(74, 96) + Vector3.up * .3f);
            Enemy.Guardian(root, OnGround(48, 196) + Vector3.up * .1f, 180); Enemy.Guardian(root, OnGround(92, 196) + Vector3.up * .1f, 180);
            Enemy.Mummy(root, OnGround(46, 214) + Vector3.up * .1f, OnGround(46, 230) + Vector3.up * .1f);
        }

        // ---- the gag: the camera's places for SphinxGag (also used by the check's close-ups)
        public static Vector3 GagSpot => OnGround(-24, 215);
        public static readonly float[] SandStones = { 46.4f, 49.7f, 53f, 56.3f };
    }
}
