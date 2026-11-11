using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 4, items 4-8: the Ice Age, the first real world. One frozen valley (x across, z from the south wall to the volcanoes in the
    // north), the Blender terrain (Tools/Blender/iceage_world.py: the same layout numbers) with the gameplay placed on it here:
    //   landing snowfield (z 0..110): start, tutorial hints, the half-frozen pond (dive, ice floes)
    //   mammoth ridge (z 110..220): climb the loading rock, ride a mammoth along three shelves; a high ledge for Abi's double jump
    //   ice bridges and the frozen river (gorge z 240..264): a cracked bridge that crumbles, ice pillars, the river and its ramp out
    //   ice caves (x -64..-20, z 308..372): dark (the necklace lights it), Kira's crawl tunnel, an ice wall for sabertooth Kira,
    //     cracked ice over a pit (Dan's ground pound); the Violinist by a campfire at the mouth
    //   volcano vents (x 30..64, z 392..452): warm steam lifts the party up three terraces
    //   standing stones (z 460..510): the altar for the blue story shard, the vortex gate, the frozen well (ground pound), a grotto
    //     behind a boulder (Muscle Man), a stone lintel (double jump)
    // Wolves and snow critters (placeholders), mammoths (placeholders), distant volcanoes with ash plumes, blowing snow.
    public static class IceAge
    {
        static Transform root;
        public const string Start = "IA-start";
        public static readonly Vector3 VortexGate = new Vector3(0, 0, 503f);
        public static readonly Vector3 ViolinistAt = new Vector3(-12f, 0, 338f);
        public static Portal Gate { get; private set; }

        public static Transform Build(WorldData w)
        {
            root = new GameObject("World Ice Age").transform;
            var save = SaveGame.Current; var prog = save.World(w.worldId);
            // the terrain, with mesh colliders
            var terrain = WorldArt.SpawnFile("iceage_terrain", root);
            if (terrain) foreach (var mf in terrain.GetComponentsInChildren<MeshFilter>()) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            else Box("Fallback ground", new Vector3(0, -.5f, 250f), new Vector3(140, 1, 560), "Snow");
            var volc = WorldArt.SpawnFile("iceage_volcanoes", root);
            if (volc) foreach (var r in volc.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            AshPlume.Make(root, new Vector3(-130, 228, 830), 26f); AshPlume.Make(root, new Vector3(120, 268, 890), 30f);
            Physics.SyncTransforms();

            Snowfield(); Ridge(); River(); Cave(prog); Vents(); Stones(prog);
            Lake(prog); Glacier(); Grotto(save, prog);   // Round 5
            // invisible walls round the valley (the foothills to the north and the cliff tops are not part of it)
            // (Round 5: openings into the lake's basin (west, z 98..132) and the glacier (east, z 280..310), walled round too)
            foreach (var (c, s) in new[] { (new Vector3(0, 40, 560), new Vector3(400, 120, 4)), (new Vector3(0, 40, -45), new Vector3(400, 120, 4)),
                                           (new Vector3(-92, 40, 26.5f), new Vector3(4, 120, 143)), (new Vector3(-92, 40, 346), new Vector3(4, 120, 428)),
                                           (new Vector3(92, 40, 117.5f), new Vector3(4, 120, 325)), (new Vector3(92, 40, 435), new Vector3(4, 120, 250)),
                                           (new Vector3(-165, 40, 125), new Vector3(4, 120, 186)), (new Vector3(-128.5f, 40, 32), new Vector3(77, 120, 4)), (new Vector3(-128.5f, 40, 218), new Vector3(77, 120, 4)),
                                           (new Vector3(165, 40, 320), new Vector3(4, 120, 116)), (new Vector3(128.5f, 40, 262), new Vector3(77, 120, 4)), (new Vector3(128.5f, 40, 378), new Vector3(77, 120, 4)) })
                Box("Valley boundary", c, s, Color.clear, "Water").GetComponent<Renderer>().enabled = false;
            Physics.SyncTransforms();

            // shards: each at 1 m over the ground found under its hint height
            foreach (var s in w.shards)
                if (!prog.shards.Contains(s.id)) Pickup.Make(Pickup.Kind.Shard, s.id, s.area, ShardPos(w, s.position), root, new Color(.45f, .92f, 1f));
            var a = w.Area("V");
            var story = Pickup.Make(Pickup.Kind.StoryShard, "story-" + w.worldId, a.id, ShardPos(w, a.storyShard), root, a.storyShardColor);
            story.gameObject.SetActive(prog.necklaceAwake && !prog.storyShard);
            Game.I.StoryShard = story;
            var sp = story.transform.position;
            Game.I.StoryBeam = Box("Story beam", new Vector3(sp.x, sp.y + 40f, sp.z), new Vector3(.5f, 80f, .5f), new Color(.45f, .7f, 1f), "Glow", false).gameObject;
            Game.I.StoryBeam.SetActive(prog.necklaceAwake && !prog.storyShard);
            if (!save.heartUpgrades.Contains("IA-H1")) Pickup.Make(Pickup.Kind.HeartUpgrade, "IA-H1", "V", new Vector3(-52f, 1.2f, 322f), root, Playground.Accent);
            foreach (var h in new[] { new Vector3(-8, 0, 104), new Vector3(36, 0, 214), new Vector3(10, 0, 232), new Vector3(-18, 0, 320), new Vector3(30, 0, 396), new Vector3(-4, 0, 458) })
                Pickup.HealHeart(OnGround(h.x, h.z) + Vector3.up * 1f, root);

            Enemies();
            Decor();
            SnowFall.Make(root);
            return root;
        }

        public static Vector3 ShardPos(WorldData w, Vector3 hint) =>
            w.heightsAreHints ? new Vector3(hint.x, Ground(hint.x, hint.z, hint.y + 4f) + 1f, hint.z) : hint;

        // ---- helpers
        const int GroundMask = ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << Hero.EnemyLayer) | (1 << 4) | (1 << 2));
        public static float Ground(float x, float z, float fromY = 80f)
        {
            return Physics.Raycast(new Vector3(x, fromY, z), Vector3.down, out var hit, 300f, GroundMask, QueryTriggerInteraction.Ignore) ? hit.point.y : 0f;
        }
        public static Vector3 OnGround(float x, float z, float fromY = 80f) => new Vector3(x, Ground(x, z, fromY), z);

        static Transform Box(string name, Vector3 centre, Vector3 size, string mat, bool collider = true) =>
            Placeholders.Prim(PrimitiveType.Cube, root, centre, size, WorldArt.Mat(mat), collider, name).transform;
        static Transform Box(string name, Vector3 centre, Vector3 size, Color c, string kind = "Lit", bool collider = true) =>
            Placeholders.Prim(PrimitiveType.Cube, root, centre, size, Placeholders.Mat(c, kind), collider, name).transform;
        // a box from x / z ranges and a top height (and a bottom)
        static Transform Slab(string name, float x0, float x1, float z0, float z1, float top, float bottom, string mat = "Rock")
        {
            var t = Box(name, new Vector3((x0 + x1) / 2, (top + bottom) / 2, (z0 + z1) / 2), new Vector3(x1 - x0, top - bottom, z1 - z0), mat);
            // Round 5 (clean-up): rock gets a snow cap with a soft rim over its edges, like the terrain's cliff tops (no collider:
            // feet sink 6 cm into it). Thin slabs (< 0.4 m) and ice stay bare.
            if (mat == "Rock" && top - bottom > .4f)
                Placeholders.Prim(PrimitiveType.Cube, root, new Vector3((x0 + x1) / 2, top - .12f, (z0 + z1) / 2), new Vector3(x1 - x0 + .2f, .36f, z1 - z0 + .2f), WorldArt.Mat("Snow"), false, name + " snow");
            return t;
        }
        static void Hint(Vector3 at, float r, System.Func<string> text)
        {
            var go = new GameObject("Hint"); go.transform.SetParent(root, false); go.transform.position = at;
            var h = go.AddComponent<HintZone>(); h.radius = r; h.text = text;
        }
        static string B(Controls.Act a) => Controls.Btn(a);
        static void Rock(float x, float z, float scale, float yaw = 0, int kind = 1, bool collider = true) =>
            WorldArt.Prop("iceage_props", "Rock" + kind, root, OnGround(x, z) + Vector3.down * .15f * scale, yaw, scale, collider);
        static Checkpoint Cp(string id, float x, float z, float yaw) => Playground.CheckpointHere(id, OnGround(x, z), yaw, root);

        // ---- the landing snowfield and the pond
        static void Snowfield()
        {
            Cp(Start, -5, 6, 0);   // beside the landing spot (out of the arrival's shots)
            Hint(new Vector3(0, 0, 12), 8f, () => $"Move with {B(Controls.Act.Move)}, jump with {B(Controls.Act.Jump)}. Find the shards!");
            Hint(new Vector3(0, 0, 32), 5f, () => $"Hold {B(Controls.Act.Jump)} longer to jump higher.");
            Hint(new Vector3(-6, 0, 70), 6f, () => $"{B(Controls.Act.SwitchPrev)} / {B(Controls.Act.SwitchNext)}: switch hero. Kira fits through small gaps, Abi jumps highest, Dan is the strongest.");
            Hint(new Vector3(11, 0, 48), 6f, () => $"The pond: {B(Controls.Act.Dive)} dives, {B(Controls.Act.Jump)} swims up and leaps out. Kira swims best: she never runs out of air.");
            // the pond: water 3 m deep in the bowl (its top at -0.5), two ice floes
            var water = Box("Pond water", new Vector3(22, -2.25f, 55), new Vector3(26, 3.5f, 26), new Color(.35f, .62f, .9f, .55f), "Water", false);
            water.gameObject.layer = 4; water.gameObject.AddComponent<WaterVolume>();
            Slab("Ice floe 1", 11, 14, 51.5f, 54.5f, -.25f, -.65f, "Ice");
            Slab("Ice floe 2", 15.5f, 18.5f, 50.5f, 53.5f, -.25f, -.65f, "Ice");
            Slab("Ice floe 3", 27, 30, 47, 50, -.25f, -.65f, "Ice");
        }

        // ---- the mammoth ridge
        static void Ridge()
        {
            Cp("IA-ridge", 22, 108, 0);
            Hint(new Vector3(40, 0, 106), 8f, () => "Mammoths! Climb the rocks and ride one: its back takes you past the high shelves.");
            // steps up to the loading rock (1.3 m each: every hero can climb them), the rock where you board a passing mammoth
            Slab("Ridge step 1", 48.5f, 53, 109, 112, 1.3f, -1); Slab("Ridge step 2", 48.5f, 53, 112, 115.5f, 2.6f, -1);
            Slab("Loading rock", 48.5f, 53, 115.5f, 124, 3.9f, -1);
            // the high shelf along the east cliff (z 150..212), 1 m over a mammoth's back: only a ride gets you up
            Slab("High shelf", 48f, 66, 150, 212, 5f, -1);
            var path = new[] { new Vector3(46, 0, 112), new Vector3(46, 0, 212), new Vector3(34, 0, 212), new Vector3(34, 0, 112) };
            for (int i = 0; i < path.Length; i++) path[i].y = .05f;
            Mammoth.Make(root, path, 2f, 0f);
            Mammoth.Make(root, path, 2f, 112f);
            // a high ledge for Abi's double jump (later): out of a single jump's reach
            Slab("High ledge (double jump)", 4, 12, 203, 209, 3.2f, -1, "Ice");
        }

        // ---- the gorge: the cracked ice bridge, the pillars, the river and its ramp
        static void River()
        {
            Cp("IA-river", 0, 228, 0);
            Hint(new Vector3(-10, 0, 232), 6f, () => "The ice bridge is cracked. Keep moving!");
            var water = Box("River", new Vector3(0, -8.75f, 252), new Vector3(150, 2.5f, 18), new Color(.3f, .55f, .85f, .6f), "Water", false);
            water.gameObject.layer = 4; water.gameObject.AddComponent<WaterVolume>();
            // half-frozen: ice sheets floating on the river
            foreach (var x in new[] { -48f, -30f, 6f, 32f }) Slab("River ice", x, x + 5, 246.2f, 249.5f, -7.3f, -7.7f, "Ice");   // by the south wall, off the swim lane
            // the bridge at x = -10: solid ends, four cracked planks between (they crumble under the player)
            Slab("Bridge south", -11.5f, -8.5f, 234, 244, .3f, -.2f, "Ice"); Slab("Bridge north", -11.5f, -8.5f, 260, 270, .3f, -.2f, "Ice");
            Slab("Bridge pier S", -11, -9, 242, 244, -.2f, -10.5f, "Ice"); Slab("Bridge pier N", -11, -9, 260, 262, -.2f, -10.5f, "Ice");
            for (int i = 0; i < 4; i++)
            {
                var plank = Slab("Cracked plank " + i, -11.5f, -8.5f, 244 + i * 4, 248 + i * 4, .3f, -.2f, "Ice");
                plank.gameObject.layer = 2;   // raycasts (shard placement) see the river bottom under it
                plank.gameObject.AddComponent<CrumblePlatform>();
                Placeholders.Prim(PrimitiveType.Cube, plank, new Vector3(0, .52f, 0), new Vector3(.6f, .05f, .05f), Placeholders.Mat(new Color(.3f, .45f, .6f)), name: "Crack").transform.localRotation = Quaternion.Euler(0, 35 + i * 20, 0);
            }
            // ice pillars at x = 20: 2.1 m gaps (an easy running jump), level with the banks
            foreach (var z in new[] { 243.5f, 248f, 252.5f, 257f, 261.5f }) Slab("Ice pillar", 18.8f, 21.2f, z - 1.2f, z + 1.2f, 0f, -10.5f, "Ice");
        }

        // ---- the ice caves
        static void Cave(WorldProgress prog)
        {
            Cp("IA-cave", -10, 326, 270);
            Hint(new Vector3(-16, 0, 330), 6f, () => "It's dark inside. Dan's necklace glows: keep him close.");
            const float top = 8f, bottom = -5f;
            // outer walls (the terrain under the cave is sunk to -4.5: the floor is built at 0 over a pit) and the roof
            Slab("Cave wall W", -66, -62, 308, 372, top, bottom); Slab("Cave wall S", -66, -20, 308, 312, top, bottom); Slab("Cave wall N", -66, -20, 368, 372, top, bottom);
            Slab("Cave wall E south", -24, -20, 312, 330, top, bottom); Slab("Cave wall E north", -24, -20, 336, 368, top, bottom);
            Slab("Cave mouth lintel", -24, -20, 330, 336, top, 4.2f);
            Slab("Cave mouth sill", -24, -20, 330, 336, 0, bottom);
            Slab("Cave roof", -66, -20, 308, 372, top + .5f, 7f);
            // the floor (top 0) round the hole of the cracked ice (x -32..-27, z 340..345)
            // Fix 7.2: solid down to the pit's floor (a 0.6 m slab left a hollow under the cave that Kira walked into from the pit)
            Slab("Cave floor W", -62, -32, 312, 368, 0, -4.6f); Slab("Cave floor E", -27, -24, 312, 368, 0, -4.6f);
            Slab("Cave floor S", -32, -27, 312, 340, 0, -4.6f); Slab("Cave floor N", -32, -27, 345, 368, 0, -4.6f);
            if (!prog.opened.Contains("IA-crack-cave"))
            {
                var ice = Slab("Cracked ice (cave)", -32, -27, 340, 345, 0, -.4f, "Ice");
                ice.gameObject.layer = 2; ice.gameObject.AddComponent<CrackedFloor>().id = "IA-crack-cave";
                for (int i = 0; i < 3; i++) Placeholders.Prim(PrimitiveType.Cube, ice, new Vector3(0, .52f, 0), new Vector3(.9f, .05f, .03f), Placeholders.Mat(new Color(.3f, .45f, .6f)), name: "Crack").transform.localRotation = Quaternion.Euler(0, i * 60, 0);
            }
            // steps out of the pit (pit floor -4.5): up the east side of the hole
            Slab("Pit step 1", -30, -29, 340, 345, -3.5f, -4.6f, "Stone"); Slab("Pit step 2", -29, -27.9f, 340, 345, -2.4f, -4.6f, "Stone");
            Slab("Pit step 3", -27.9f, -27, 340, 345, -1.2f, -4.6f, "Stone");
            // the partition between the hall (x -40..-24) and Kira's side: a crawl gap 1.2 m wide, 0.7 m high at z 319.4..320.6
            Slab("Partition S", -42, -40, 312, 319.4f, 7, bottom); Slab("Partition N", -42, -40, 320.6f, 368, 7, bottom);
            Slab("Partition over the gap", -42, -40, 319.4f, 320.6f, 7, .7f);
            Slab("Crawl chamber wall", -62, -42, 330, 332, 7, bottom); Slab("Rock fill", -62, -42, 332, 368, 7, bottom);
            Hint(new Vector3(-39, 0, 320), 3f, () => "A tiny gap: only Kira fits through.");
            // the ice wall (sabertooth Kira's claws) closing the north chamber
            Slab("North chamber wall W", -40, -34, 356, 358, 7, bottom); Slab("North chamber wall E", -28, -24, 356, 358, 7, bottom);
            Slab("North chamber over the ice", -34, -28, 356, 358, 7, 5f);
            if (!prog.opened.Contains("IA-ice-cave"))
            {
                var wall = Box("Ice wall (cave)", new Vector3(-31, 2.5f, 357), new Vector3(6, 5f, .8f), new Color(.7f, .9f, 1f, .75f), "Water");
                wall.gameObject.AddComponent<IceWall>().id = "IA-ice-cave";
            }
            var dark = Box("Cave darkness", new Vector3(-43, 1, 340), new Vector3(38, 14, 56), Color.clear, "Water", false);
            dark.GetComponent<Renderer>().enabled = false; dark.gameObject.layer = 2; dark.gameObject.AddComponent<DarkVolume>();
            // glowing ice crystals in the hall; the campfire and the Violinist at the mouth
            foreach (var (x, z, s) in new[] { (-26.5f, 314f, .8f), (-37f, 330f, 1f), (-25.5f, 352f, .7f), (-38f, 366f, .9f), (-60f, 314f, .6f) })
                WorldArt.Prop("iceage_props", "IceCrystal", root, new Vector3(x, 0, z), x * 13, s);
            Campfire.Make(root, OnGround(-15, 333));
            var v = Violinist.Make(root, OnGround(ViolinistAt.x, ViolinistAt.z), 135f, Game.World ? Game.World.violinistInstrument : "Fiddle");
            _ = v;
        }

        // ---- the volcano vents: steam lifts the party up three terraces
        static void Vents()
        {
            Cp("IA-vents", 34, 398, 0);
            Hint(new Vector3(36, 0, 406), 7f, () => "Warm steam! Stand in it to rise, then steer onto the ledge.");
            Slab("Terrace 1", 44, 64, 398, 446, 8, -1); Slab("Terrace 2", 52, 64, 402, 442, 16, 7.9f); Slab("Terrace 3", 56, 64, 408, 432, 24, 15.9f);
            Slab("Ice shelf (double jump)", 58, 62, 418, 422, 27.3f, 24, "Ice");
            Vent(40, 420, 0f, 14f); Vent(48, 422, 8f, 14f); Vent(53.6f, 420, 16f, 13f);
            // lava pools by the first vent: they hurt
            foreach (var (x, z) in new[] { (34f, 412f), (37f, 432f) })
            {
                var lava = Placeholders.Prim(PrimitiveType.Cylinder, root, new Vector3(x, Ground(x, z) + .05f, z), new Vector3(5, .08f, 4), WorldArt.Mat("Lava"), false, "Lava").transform;
                Placeholders.Prim(PrimitiveType.Cylinder, root, new Vector3(x, Ground(x, z) + .02f, z), new Vector3(5.8f, .06f, 4.8f), WorldArt.Mat("Basalt"), false, "Lava rim");
                var sp = lava.gameObject.AddComponent<Spikes>(); sp.cause = "lava";
                var l = new GameObject("Lava light"); l.transform.SetParent(lava, false); l.transform.localPosition = new Vector3(0, 3, 0);
                var lt = l.AddComponent<Light>(); lt.type = LightType.Point; lt.color = new Color(1f, .5f, .15f); lt.range = 10; lt.intensity = 2f;
            }
        }

        static void Vent(float x, float z, float baseY, float height)
        {
            var up = Box("Steam vent", new Vector3(x, baseY + height / 2 - .5f, z), new Vector3(2.6f, height, 2.6f), Color.clear, "Water", false);
            up.GetComponent<Renderer>().enabled = false; up.gameObject.layer = 2;
            up.gameObject.AddComponent<UpdraftVolume>();
            Box("Vent crack", new Vector3(x, baseY + .03f, z), new Vector3(1.8f, .06f, 1.8f), "Lava", false);
            for (int i = 0; i < 6; i++)
            {
                float rx = x + Mathf.Cos(i) * 1.7f, rz = z + Mathf.Sin(i) * 1.7f;
                WorldArt.Prop("iceage_props", "Rock" + (i % 3 + 1), root, new Vector3(rx, Ground(rx, rz, baseY + 2f) - .1f, rz), i * 60, .45f);
            }
            Steam.Make(root, new Vector3(x, baseY, z), height);
        }

        // ---- the standing stones: the altar, the vortex gate, the frozen well, the grotto, the lintel
        static void Stones(WorldProgress prog)
        {
            Cp("IA-stones", 0, 456, 0);
            for (int i = 0; i < 9; i++)
            {
                float a = (i + .5f) / 9f * Mathf.PI * 2f; if (Mathf.Abs(Mathf.Cos(a) - 1) < .05f) continue;
                var p = new Vector3(Mathf.Sin(a) * 12f, 0, 490 + Mathf.Cos(a) * 12f);
                if (p.z > 499 && Mathf.Abs(p.x) < 5) continue;   // leave the north side open for the vortex
                var st = WorldArt.Prop("iceage_props", "Stone", root, OnGround(p.x, p.z) + Vector3.down * .2f, a * Mathf.Rad2Deg, 1.3f, true);
                _ = st;
            }
            Slab("Altar", -1.2f, 1.2f, 489.2f, 490.8f, 1f, -.5f, "Stone");
            // the vortex gate (opened by the story shard) and where the party comes back out of the vortex
            Gate = Playground.PortalHere("IA-vortex", OnGround(VortexGate.x, VortexGate.z), 180, Vortex.Checkpoint, false, root, new Color(.3f, .62f, 1f), vortex: true);
            Cp("IA-vortex", 5, 500, 180);
            // the frozen well: cracked ice over a 3.5 m pit (Dan's ground pound), steps inside to climb out
            for (int i = 0; i < 10; i++) { float a = i / 10f * Mathf.PI * 2; Rock(18 + Mathf.Cos(a) * 5.2f, 478 + Mathf.Sin(a) * 5.2f, .5f, i * 36, i % 3 + 1, collider: false); }
            // four 0.8 m steps up the east side; the top one flush with the rim (you walk out)
            Slab("Well step 1", 19.3f, 20.1f, 476.8f, 479.2f, -2.7f, -4f, "Stone"); Slab("Well step 2", 20.1f, 20.9f, 476.8f, 479.2f, -1.9f, -4f, "Stone");
            Slab("Well step 3", 20.9f, 21.7f, 476.8f, 479.2f, -1.1f, -4f, "Stone"); Slab("Well step 4", 21.7f, 23.6f, 476.8f, 479.2f, -.3f, -4f, "Stone");
            if (!prog.opened.Contains("IA-crack-well"))
            {
                var cap = Placeholders.Prim(PrimitiveType.Cylinder, root, new Vector3(18, -.12f, 478), new Vector3(9.6f, .2f, 9.6f), WorldArt.Mat("Ice"), false, "Cracked ice (well)");
                var bc = cap.AddComponent<BoxCollider>(); bc.size = new Vector3(1f / 1.05f, 2f, 1f / 1.05f);
                cap.layer = 2; cap.AddComponent<CrackedFloor>().id = "IA-crack-well";
                for (int i = 0; i < 3; i++) Placeholders.Prim(PrimitiveType.Cube, cap.transform, new Vector3(0, 1.05f, 0), new Vector3(.7f, .2f, .02f), Placeholders.Mat(new Color(.3f, .45f, .6f)), name: "Crack").transform.localRotation = Quaternion.Euler(0, i * 60 + 10, 0);
            }
            // a grotto behind a boulder (Muscle Man, later)
            Slab("Grotto wall W", -54, -52, 478, 490, 4.5f, -1); Slab("Grotto wall S", -54, -44, 478, 480, 4.5f, -1); Slab("Grotto wall N", -54, -44, 488, 490, 4.5f, -1);
            Slab("Grotto front S", -45, -44, 480, 482.2f, 4.5f, -1); Slab("Grotto front N", -45, -44, 485.8f, 488, 4.5f, -1);   // a 3.6 m mouth the boulder fills
            Slab("Grotto roof", -54, -44, 478, 490, 5.5f, 4.5f);
            var boulder = WorldArt.Prop("iceage_props", "Rock2", root, OnGround(-43.5f, 484) + Vector3.down * .2f, 90, 1f, true);
            if (boulder)
            {
                boulder.name = "Boulder (needs Muscle Man)";
                boulder.transform.localScale = Vector3.Scale(boulder.transform.localScale, new Vector3(2.3f, 1.6f, 2.7f));   // Blender axes: width, depth, height
            }
            else boulder = Placeholders.Prim(PrimitiveType.Sphere, root, OnGround(-43.5f, 484) + Vector3.up * 2f, Vector3.one * 4.6f, WorldArt.Mat("Rock"), true, "Boulder (needs Muscle Man)");
            boulder.AddComponent<Boulder>();
            // the stone lintel (double jump)
            Slab("Lintel post W", -9.5f, -8.5f, 496.5f, 497.5f, 2.8f, -.5f, "Stone"); Slab("Lintel post E", -5.5f, -4.5f, 496.5f, 497.5f, 2.8f, -.5f, "Stone");
            Slab("Lintel", -10, -4, 496, 498, 3.3f, 2.8f, "Stone");
        }

        // ---- Round 5, item 5: the frozen lake (the west basin, through the pass at z 98..132)
        static void Lake(WorldProgress prog)
        {
            Cp("IA-lake", -74, 115, 270);
            Hint(new Vector3(-84, 0, 115), 7f, () => "A frozen lake! The ice is slippery: run, and ride the skating lane.");
            Hint(new Vector3(-118, 0, 133), 6f, () => "The water under this hole goes down and down... too deep to dive, for now.");
            // the water: 8 m under the ice, and the deep part (30 m) that pushes divers back up without the deep dive
            var water = Box("Lake water", new Vector3(-118, -4.25f, 125), new Vector3(48, 7.5f, 134), new Color(.3f, .55f, .85f, .55f), "Water", false);
            water.gameObject.layer = 4; water.gameObject.AddComponent<WaterVolume>();
            var deepW = Box("Lake deep water", new Vector3(-118, -19.25f, 133), new Vector3(32, 22.5f, 32), new Color(.2f, .4f, .75f, .55f), "Water", false);
            deepW.gameObject.layer = 4; deepW.gameObject.AddComponent<WaterVolume>();
            var deep = Box("Deep water (deep dive)", new Vector3(-118, -19f, 133), new Vector3(32, 22f, 32), Color.clear, "Water", false);
            deep.GetComponent<Renderer>().enabled = false; deep.gameObject.layer = 2; deep.gameObject.AddComponent<DeepWater>();
            // the ice sheet (top 0) with four 8 m holes: the near hole and the exit hole joined by one short channel under the ice (16 m, lit
            // at both ends: Round 6, item 2), the far hole, and the hole over the deep part. Everywhere else the ice is solid to the bottom,
            // so nobody can wander off under it.
            var holes = new[] { new Rect(-122, 80, 8, 8), new Rect(-122, 104, 8, 8), new Rect(-105, 156, 8, 8), new Rect(-122, 129, 8, 8) };
            IceSheet(-142, -94, 58, 192, holes, new[] { new Rect(-121, 88, 6, 16) });
            IceHole.Region = new Bounds(new Vector3(-118, -4, 125), new Vector3(48, 10, 134));
            foreach (var h in holes)
            {
                var go = new GameObject("Ice hole"); go.transform.SetParent(root, false);
                var ih = go.AddComponent<IceHole>(); ih.area = h; ih.iceTop = .05f;
                // a shaft of daylight down through the hole, and a light under it: the way up is always the bright spot
                Box("Light shaft", new Vector3(h.center.x, -4.2f, h.center.y), new Vector3(h.width - .6f, 7.6f, h.height - .6f), new Color(1f, .97f, .8f, .16f), "Water", false)
                    .GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var lg = new GameObject("Hole light"); lg.transform.SetParent(root, false); lg.transform.position = new Vector3(h.center.x, -3f, h.center.y);
                var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 14f; l.intensity = 6f; l.color = new Color(1f, .95f, .8f); l.shadows = LightShadows.None;
            }
            Hint(new Vector3(-118, 0, 84), 5f, () => "Dive under the ice! The tunnel's short, and the next hole shines ahead.");
            // slick ice over the whole lake; the skating lane drives you north to the ramp and the island
            var slick = Box("Slick ice", new Vector3(-118, .5f, 125), new Vector3(48, 2f, 134), Color.clear, "Water", false);
            slick.GetComponent<Renderer>().enabled = false; slick.gameObject.layer = 2; slick.gameObject.AddComponent<SlickIce>().grip = .3f;
            var lane = Box("Skating lane", new Vector3(-133, .5f, 120), new Vector3(6, 2f, 112), Color.clear, "Water", false);
            lane.GetComponent<Renderer>().enabled = false; lane.gameObject.layer = 2; var ls = lane.gameObject.AddComponent<SlickIce>(); ls.push = new Vector3(0, 0, 6f); ls.grip = .35f;   // Round 5 tuning: at 10% grip nobody could steer
            for (int i = 0; i < 14; i++) foreach (var x in new[] { -136.3f, -129.7f })
                Box("Lane mark", new Vector3(x, .03f, 68 + i * 8), new Vector3(.4f, .06f, 4f), new Color(.45f, .7f, 1f), "Lit", false);
            var ramp = Placeholders.Prim(PrimitiveType.Cube, root, new Vector3(-133, .7f, 175), new Vector3(6, .4f, 6.4f), WorldArt.Mat("Ice"), true, "Ice ramp");
            ramp.transform.rotation = Quaternion.Euler(-15, 0, 0);
            Slab("Ice island", -136, -130, 178, 190, 1.6f, -1);
            // the ice hollow on the west shore, behind a boulder (Muscle Man, later)
            Slab("Hollow wall W", -162, -160, 116, 132, 4f, -1); Slab("Hollow wall S", -162, -150, 116, 118, 4f, -1); Slab("Hollow wall N", -162, -150, 130, 132, 4f, -1);
            Slab("Hollow front S", -151, -150, 118, 121.5f, 4f, -1); Slab("Hollow front N", -151, -150, 126.5f, 130, 4f, -1); Slab("Hollow roof", -162, -150, 116, 132, 5f, 4f);
            var boulder = WorldArt.Prop("iceage_props", "Rock2", root, OnGround(-148, 124) + Vector3.down * .2f, 90, 1f, true);
            if (boulder) { boulder.name = "Boulder (needs Muscle Man)"; boulder.transform.localScale = Vector3.Scale(boulder.transform.localScale, new Vector3(2.4f, 1.6f, 2.6f)); boulder.AddComponent<Boulder>(); }
            foreach (var (x, z, s) in new[] { (-150f, 60f, 1.1f), (-90f, 200f, .9f), (-156f, 180f, 1.3f), (-96f, 50f, .8f) }) WorldArt.Prop("iceage_props", "IceCrystal", root, OnGround(x, z) + Vector3.down * .2f, x * 5, s);
            foreach (var (x, z, s) in new[] { (-152f, 44f, 1.2f), (-158f, 96f, 1.4f), (-150f, 206f, 1.1f), (-92f, 208f, 1.3f), (-90f, 44f, 1.0f) }) WorldArt.Prop("iceage_props", "Pine", root, OnGround(x, z) + Vector3.down * .2f, x * 3, s);
            foreach (var (x, z, s) in new[] { (-146f, 200f, 1.4f), (-158f, 70f, 1.1f), (-92f, 90f, .9f) }) Rock(x, z, s, x * 7, 2);
        }

        // the lake's ice over x0..x1, z0..z1 (top 0.05): open over the holes, a 0.55 m sheet over the swim channels, solid down to the
        // lake bed (-8.5) everywhere else (Round 6, item 2). Cut into a grid on every rectangle's edges, neighbouring cells of a row merged.
        static void IceSheet(float x0, float x1, float z0, float z1, Rect[] holes, Rect[] channels)
        {
            var all = holes.Concat(channels).ToList();
            var xs = new List<float> { x0, x1 }; var zs = new List<float> { z0, z1 };
            foreach (var h in all) { xs.Add(h.xMin); xs.Add(h.xMax); zs.Add(h.yMin); zs.Add(h.yMax); }
            xs = xs.Distinct().OrderBy(v => v).ToList(); zs = zs.Distinct().OrderBy(v => v).ToList();
            for (int i = 0; i < zs.Count - 1; i++)
            {
                float a = zs[i], b = zs[i + 1];
                float runX = xs[0]; float runBottom = float.NaN;
                for (int j = 0; j <= xs.Count - 1; j++)
                {
                    float bottom = float.NaN;   // NaN = open (a hole) or past the end
                    if (j < xs.Count - 1)
                    {
                        var c = new Vector2((xs[j] + xs[j + 1]) / 2, (a + b) / 2);
                        bottom = holes.Any(h => h.Contains(c)) ? float.NaN : channels.Any(h => h.Contains(c)) ? -.5f : -8.5f;
                    }
                    bool same = (float.IsNaN(bottom) && float.IsNaN(runBottom)) || bottom == runBottom;
                    if (j == xs.Count - 1 || !same)
                    {
                        if (!float.IsNaN(runBottom) && xs[j] > runX) Slab("Lake ice", runX, xs[j], a, b, .05f, runBottom, "Ice");
                        runX = xs[j]; runBottom = bottom;
                    }
                }
            }
        }

        // ---- the glacier climb (east, through the gap at z 280..310): 18 ice ledges (three rows side by side) zigzag up to the summit (24.7 m), a view over
        // the valley; two wall-jump chimneys (later) and a high ledge over the summit (double jump, later)
        public const float LedgeStep = 1.3f;
        static void Glacier()
        {
            Cp("IA-glacier", 86, 295, 90);
            Hint(new Vector3(98, 0, 295), 7f, () => $"The glacier! Climb the ice ledges ({B(Controls.Act.Jump)}) all the way to the top.");
            Hint(new Vector3(108, 0, 316), 5f, () => "A narrow chimney of ice, far too high to climb... someone who can jump off walls could do it.");
            for (int i = 0; i < 18; i++)
            {
                int row = i / 6, j = i % 6; float x = 118 + row * 8, z = row % 2 == 0 ? 272 + j * 4 : 292 - j * 4;
                Slab("Glacier ledge " + (i + 1), x, x + 8, z, z + 4, LedgeStep * (i + 1), -1, "Ice");
            }
            Slab("Glacier summit", 138, 158, 296, 316, LedgeStep * 19, -1);
            Slab("Glacier wall", 158, 163, 264, 376, 32f, -1, "Ice");
            Slab("High ledge (double jump)", 152, 158, 316, 320, LedgeStep * 19 + 3.2f, LedgeStep * 19 + 2.4f, "Ice");
            foreach (var cz in new[] { 300f, 330f })   // the chimneys: open to the west, 2 m inside, a ledge at the top with the shard
            {
                Slab("Chimney wall S", 111.2f, 115, cz - .8f, cz, 14.8f, -1, "Ice"); Slab("Chimney wall N", 111.2f, 115, cz + 2, cz + 2.8f, 14.8f, -1, "Ice");
                Slab("Chimney wall E", 114, 115, cz, cz + 2, 14.8f, -1, "Ice");
                Slab("Chimney top ledge", 113.4f, 114, cz, cz + 2, 13f, 12.6f, "Ice");
            }
            foreach (var (x, z, s) in new[] { (96f, 270f, 1.2f), (104f, 368f, 1.4f), (150f, 360f, 1.1f), (100f, 340f, .9f), (130f, 368f, 1.3f) }) WorldArt.Prop("iceage_props", "IceCrystal", root, OnGround(x, z) + Vector3.down * .2f, x * 9, s);
            foreach (var (x, z, s) in new[] { (94f, 360f, 1.4f), (140f, 330f, 1.1f) }) Rock(x, z, s, x * 3, 1);
        }

        // ---- the hot-spring grotto (east of the stones), behind a cracked rock wall (Dan's charged punch, later). Warm light and
        // steam leak out of the cracks and a slit window, so the player knows something is in there.
        static void Grotto(SaveGame save, WorldProgress prog)
        {
            Hint(new Vector3(54, 0, 464), 6f, () => "Warm air seeps through the cracks in this rock. A very strong punch could break it... (later)");
            Slab("Grotto wall W south", 58, 60, 450, 461, 6, -1); Slab("Grotto wall W north", 58, 60, 467, 470, 6, -1); Slab("Grotto wall W far", 58, 60, 470.8f, 478, 6, -1);
            Slab("Grotto slit sill", 58, 60, 470, 470.8f, 1, -1); Slab("Grotto slit head", 58, 60, 470, 470.8f, 6, 2.2f);
            Slab("Grotto lintel", 58, 60, 461, 467, 6, 3.6f);
            Slab("Grotto wall S", 58, 86, 448, 450, 6, -1); Slab("Grotto wall N", 58, 86, 478, 480, 6, -1); Slab("Grotto wall E", 84, 86, 450, 478, 6, -1);
            Slab("Grotto roof", 58, 86, 448, 480, 7, 6);
            if (!prog.opened.Contains("IA-rock-hotspring"))
            {
                var wall = Slab("Cracked rock wall (charged punch)", 58.2f, 59.8f, 461, 467, 3.6f, 0);
                wall.gameObject.AddComponent<RockWall>().id = "IA-rock-hotspring";
                for (int i = 0; i < 4; i++)
                {
                    var c = Box("Glowing crack", new Vector3(57.85f, .6f + i * .75f, 464 + Mathf.Sin(i * 2.1f) * 1.6f), new Vector3(.06f, .08f, 1.6f + i * .3f), "Lava", false);
                    c.localRotation = Quaternion.Euler(i * 23 - 30, 0, 0);
                }
            }
            // inside: a hot spring, warm light, steam out through a gap in the roof
            Box("Hot spring", new Vector3(71, .1f, 466), new Vector3(10, .25f, 8), new Color(.35f, .8f, .8f, .7f), "Water", false);
            foreach (var (x, z) in new[] { (65.5f, 466f), (76.5f, 466f), (71f, 461.5f), (71f, 470.5f) }) Rock(x, z, .55f, x * 11, 3, collider: false);
            var l = new GameObject("Grotto light"); l.transform.SetParent(root, false); l.transform.position = new Vector3(72, 3.5f, 464);
            var lt = l.AddComponent<Light>(); lt.type = LightType.Point; lt.color = new Color(1f, .6f, .3f); lt.range = 22; lt.intensity = 2.4f;
            Steam.Make(root, new Vector3(71, 7f, 466), 8f);
            if (!save.heartUpgrades.Contains("IA-H2")) Pickup.Make(Pickup.Kind.HeartUpgrade, "IA-H2", "V", new Vector3(82, 1.2f, 466), root, Playground.Accent);
        }

        // ---- wolves and snow critters
        static void Enemies()
        {
            Vector3 G(float x, float z) => OnGround(x, z) + Vector3.up * .1f;
            Enemy.Critter(root, G(-14, 44), G(-14, 74));
            Enemy.Critter(root, G(8, 86), G(22, 86));
            Enemy.Wolf(root, G(14, 176), G(14, 190));
            Enemy.Wolf(root, G(24, 186), G(24, 172));
            Enemy.Critter(root, G(-34, 232), G(-20, 232));
            Enemy.Critter(root, G(28, 274), G(42, 274));
            Enemy.Critter(root, new Vector3(50, 8.1f, 404), new Vector3(50, 8.1f, 440));
            Enemy.Wolf(root, G(-10, 462), G(-2, 470));
            Enemy.Wolf(root, G(10, 466), G(4, 474));
            Enemy.Wolf(root, G(-22, 450), G(-12, 446));
            // Round 5: the lake's shore and the glacier's floor
            Enemy.Critter(root, G(-140, 204), G(-104, 204)); Enemy.Wolf(root, G(-150, 50), G(-150, 80));
            Enemy.Critter(root, G(100, 276), G(100, 300)); Enemy.Wolf(root, G(110, 352), G(132, 356));
        }

        // ---- trees, rocks, bones, ruins
        static void Decor()
        {
            var rnd = new System.Random(11);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            // pines along both sides of the valley, thicker toward the cliffs; none on the paths
            for (int i = 0; i < 70; i++)
            {
                float side = i % 2 == 0 ? -1 : 1; float x = side * R(44, 60), z = R(-10, 470);
                if (x > 30 && z > 100 && z < 225) continue;   // the mammoth ridge
                if (x > 28 && z > 385 && z < 455) continue;   // the vents
                if (x < -18 && z > 300 && z < 380) continue;  // the cave
                if (Mathf.Abs(z - 252) < 16) continue;        // the gorge
                if (x > 40 && z > 444 && z < 484) continue;   // Round 5: the hot-spring grotto's cracked wall
                if (x > 40 && z > 274 && z < 316) continue;   // Round 5: the way into the glacier
                if (x < -40 && z > 92 && z < 138) continue;   // Round 5: the pass to the frozen lake
                var p = OnGround(x, z);
                if (p.y > 3f || p.y < -.8f) continue;
                WorldArt.Prop("iceage_props", "Pine", root, p + Vector3.down * .2f, R(0, 360), R(.8f, 1.5f), false);
            }
            for (int i = 0; i < 46; i++)
            {
                float x = R(-58, 58), z = R(-15, 520);
                if (Mathf.Abs(x) < 14 && z < 470) continue;   // keep the middle path clear
                if (Mathf.Abs(z - 252) < 14 || (x > 30 && z > 100 && z < 225) || (x < -18 && z > 300 && z < 380) || (x > 28 && z > 385 && z < 455)) continue;
                if (Mathf.Abs(x) < 16 && z > 470) continue;
                if (x > 10 && x < 56 && z > 214 && z < 240) continue;   // Round 5: the way to the pillars and the ramp (upright rocks blocked it)
                Rock(x, z, R(.6f, 2.2f), R(0, 360), i % 3 + 1);
            }
            foreach (var (x, z, y) in new[] { (-22f, 30f, 40f), (30f, 96f, 200f), (-40f, 200f, 110f), (36f, 300f, 15f), (-30f, 430f, 70f) })
                WorldArt.Prop("iceage_props", "Bones", root, OnGround(x, z) + Vector3.down * .3f, y, 1f);
            foreach (var (x, z) in new[] { (-30f, 235f), (34f, 238f), (-48f, 266f), (8f, 268f), (-20f, 380f), (44f, 392f) })
                WorldArt.Prop("iceage_props", "IceCrystal", root, OnGround(x, z) + Vector3.down * .2f, x * 7, 1.2f);
            // the stone ruins from Dan's reference (by the cave and on the way to the stones)
            foreach (var (x, z, yaw) in new[] { (-26f, 400f, 10f), (-23f, 404f, 80f), (-29f, 407f, 40f), (24f, 300f, 0f), (-6f, 140f, 20f) })
                WorldArt.Prop("iceage_props", "Stone", root, OnGround(x, z) + Vector3.down * .3f, yaw, .9f, true);
        }
    }
}
