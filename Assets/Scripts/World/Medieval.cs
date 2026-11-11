using System.Linq;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 8, items 2-12: the Medieval Kingdom, world 4: a torch-lit night. The ground and the castle come from Tools/Blender/medieval_world.py
    // (the same layout numbers); the colliders and the gameplay are placed here, about 10-15 minutes on a first visit:
    //   the courtyard (x -30..30, z 0..63, cobbles at 0): the round stone with the purple shard stuck in it (0, 34) from the start (Dan's
    //     necklace hums near it), benches and flower pots, wall torches, the arcade along the east wall (its roof deck at 6.6), the
    //     Violinist as a bard with his lute (he teaches Dan the charged punch), the wrecked hay wagon by the west wall (the arrival), a
    //     walled yard behind a boulder (Muscle Man, later), the armory behind a portcullis (Muscle Man), a store room behind a cracked
    //     wall (the charged punch)
    //   the walls and towers: curtain walls 10 m (walks on top), round corner towers (the north ones' tops at 16 are open), the gatehouse;
    //     a stair up the west wall, the arcade's stair and steps on to the east walk; nervous archers on the south and east walks
    //   the great hall (the keep, x -15..15, z 66..84): the feast table, a gallery at 6 m (west and north), two chandeliers that swing
    //     across to the east ledge (a heart), banners, the fireplace; the rival Muscle Man at the feast
    //   the dungeon (its own dark space at x 400, down the stair door in the west wall): rats, three locked cells (their keys about), three
    //     braziers dragon Kira lights to open the last room (a heart)
    //   the moat (water at -1.2 round the castle island, its deep pit in the north-west corner: the deep dive, later); the drawbridge south
    //   the tournament field (south-east): the lists, the stands (three braziers to light: the prize chest), tents, hay bales, the test of
    //     strength where the rival Muscle Man rings the bell and Dan doesn't
    //   the village and market (south-west): timber houses, stalls (up the crates onto an awning), brambles to burn, a chimney (later)
    public static class Medieval
    {
        static Transform root; static Collider terrainCol;
        public const string Start = "ME-start", VortexCp = "ME-vortex", DungeonCp = "ME-dungeon";
        public static readonly Vector3 StoneAt = new Vector3(0, 0, 34);
        public static readonly Vector3 ViolinistAt = new Vector3(-14, 0, 38);
        public static readonly Vector3 Dungeon = new Vector3(400, 0, 0);
        public static readonly Vector3 WreckAt = new Vector3(-25.5f, 0, 24);
        public static readonly Color Purple = new Color(.62f, .32f, 1f);
        public static Portal Gate { get; private set; }
        public static GameObject StuckShard { get; private set; }   // the purple shard in the stone (looks only; the pickup appears when the necklace wakes)
        public static HighStriker Striker { get; private set; }
        public static GameObject WreckSite { get; private set; }   // the wagon's wreck, its driver and pony: hidden until the arrival's blast
        public static RivalMuscleMan CourtRival { get; private set; }
        public static Transform Drawbridge { get; private set; }   // Fix 8.1: the drawbridge (its deck collider is a child)
        public static Vector3 ArriveAt => new Vector3(0, Ground(0, 8, 3f), 8);

        public static Transform Build(WorldData w)
        {
            root = new GameObject("World Medieval").transform;
            var save = SaveGame.Current; var prog = save.World(w.worldId);
            var terrain = WorldArt.SpawnFile("medieval_terrain", root);
            if (terrain)
                foreach (var mf in terrain.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf.name.StartsWith("FarHills")) { mf.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; continue; }
                    var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; if (mf.name.StartsWith("Terrain")) terrainCol = mc;
                }
            else FallbackGround();
            Physics.SyncTransforms();

            Castle(prog); Courtyard(prog); Hall(prog); Moat(); DungeonSpace(prog); Field(prog); Village(prog);
            foreach (var (c, s) in new[] { (new Vector3(0, 40, -172), new Vector3(430, 120, 4)), (new Vector3(0, 40, 172), new Vector3(430, 120, 4)),
                                           (new Vector3(-207, 40, 0), new Vector3(4, 120, 350)), (new Vector3(207, 40, 0), new Vector3(4, 120, 350)) })
                Box("World boundary", c, s, Color.clear, "Water").GetComponent<Renderer>().enabled = false;
            Physics.SyncTransforms();

            foreach (var s in w.shards)
                if (!prog.shards.Contains(s.id)) Pickup.Make(Pickup.Kind.Shard, s.id, s.area, ShardPos(s.position), root, new Color(.85f, .7f, 1f));
            PlayLog.Line("Medieval shards placed: " + string.Join(" ", w.shards.Select(s => $"{s.id}{Fmt(ShardPos(s.position))}")));
            var a = w.Area("ME");
            var story = Pickup.Make(Pickup.Kind.StoryShard, "story-" + w.worldId, a.id, StoneAt + new Vector3(0, a.storyShard.y, -.35f), root, a.storyShardColor);
            story.gameObject.SetActive(prog.necklaceAwake && !prog.storyShard);
            Game.I.StoryShard = story;
            Game.I.StoryBeam = Box("Story beam", StoneAt + Vector3.up * 16f, new Vector3(.35f, 30f, .35f), new Color(.75f, .55f, 1f), "Glow", false).gameObject;
            Game.I.StoryBeam.SetActive(prog.necklaceAwake && !prog.storyShard);
            if (!save.heartUpgrades.Contains("ME-H1")) Pickup.Make(Pickup.Kind.HeartUpgrade, "ME-H1", "ME", Dungeon + new Vector3(0, 1.2f, 55.2f), root, Playground.Accent);
            if (!save.heartUpgrades.Contains("ME-H2")) Pickup.Make(Pickup.Kind.HeartUpgrade, "ME-H2", "ME", new Vector3(13, 7.2f, 75), root, Playground.Accent);
            foreach (var h in new[] { new Vector3(-6, 0, 18), new Vector3(10, 0, 60), new Vector3(70, 0, -55), new Vector3(-70, 0, -75), new Vector3(0, 0, -40), Dungeon + new Vector3(0, 0, 20) })
                Pickup.HealHeart(OnGround(h.x, h.z, h.x > 300 ? 3f : 30f) + Vector3.up * 1f, root);
            Enemies();
            StarDome.Make(root);
            return root;
        }

        // shard y in the data = the height of what it sits on (a hint): 1 m over whatever is found there
        public static Vector3 ShardPos(Vector3 hint) => new Vector3(hint.x, Ground(hint.x, hint.z, hint.y + 1.6f) + 1f, hint.z);
        static string Fmt(Vector3 v) => $"({v.x:0},{v.y:0.0},{v.z:0})";

        // ---- helpers (as Bethlehem's)
        const int GroundMask = ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << Hero.EnemyLayer) | (1 << 4) | (1 << 2));
        public static float Ground(float x, float z, float fromY = 60f) =>
            Physics.Raycast(new Vector3(x, fromY, z), Vector3.down, out var hit, 300f, GroundMask, QueryTriggerInteraction.Ignore) ? hit.point.y : 0f;
        public static Vector3 OnGround(float x, float z, float fromY = 60f) => new Vector3(x, Ground(x, z, fromY), z);
        static Transform Box(string name, Vector3 centre, Vector3 size, string mat, bool collider = true) =>
            Placeholders.Prim(PrimitiveType.Cube, root, centre, size, WorldArt.Mat(mat), collider, name).transform;
        static Transform Box(string name, Vector3 centre, Vector3 size, Color c, string kind = "Lit", bool collider = true) =>
            Placeholders.Prim(PrimitiveType.Cube, root, centre, size, Placeholders.Mat(c, kind), collider, name).transform;
        static Transform Slab(string name, float x0, float x1, float z0, float z1, float top, float bottom, string mat = "CastleStone") =>
            Box(name, new Vector3((x0 + x1) / 2, (top + bottom) / 2, (z0 + z1) / 2), new Vector3(x1 - x0, top - bottom, z1 - z0), mat);
        static Transform Hidden(Transform t) { t.GetComponent<Renderer>().enabled = false; return t; }
        // an invisible solid (the Blender castle is the look)
        static Transform Wall(string name, float x0, float x1, float z0, float z1, float top, float bottom) => Hidden(Slab(name, x0, x1, z0, z1, top, bottom));
        static void Solid(Vector3 at, Vector3 size, float yaw = 0)
        {
            var b = new GameObject("Solid"); b.transform.SetParent(root, false); b.transform.position = at + Vector3.up * size.y / 2;
            b.transform.rotation = Quaternion.Euler(0, yaw, 0); b.AddComponent<BoxCollider>().size = size;
        }
        // a round solid (a convex cylinder: Unity's cylinder mesh, 20 sides)
        static Mesh cylinder;
        static void Round(Vector3 at, float r, float h)
        {
            if (!cylinder) { var tmp = GameObject.CreatePrimitive(PrimitiveType.Cylinder); cylinder = tmp.GetComponent<MeshFilter>().sharedMesh; Object.DestroyImmediate(tmp); }
            var go = new GameObject("Tower round"); go.transform.SetParent(root, false); go.transform.position = at + Vector3.up * h / 2; go.transform.localScale = new Vector3(2 * r, h / 2, 2 * r);
            var mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = cylinder; mc.convex = true;
        }
        static void Hint(Vector3 at, float r, System.Func<string> text)
        {
            var go = new GameObject("Hint"); go.transform.SetParent(root, false); go.transform.position = at;
            var h = go.AddComponent<HintZone>(); h.radius = r; h.text = text;
        }
        static string B(Controls.Act a) => Controls.Btn(a);
        static Checkpoint Cp(string id, float x, float z, float yaw, bool visible = true) => Playground.CheckpointHere(id, OnGround(x, z, x > 300 ? 3f : 30f), yaw, root, visible);
        static GameObject Prop(string prop, float x, float z, float yaw = 0, float scale = 1, float y = float.NaN) =>
            WorldArt.Prop("medieval_props", prop, root, float.IsNaN(y) ? OnGround(x, z) : new Vector3(x, y, z), yaw, scale, false);
        static Light PointLight(string name, Vector3 at, float range, float intensity, Color c)
        {
            var l = new GameObject(name); l.transform.SetParent(root, false); l.transform.position = at;
            var lt = l.AddComponent<Light>(); lt.type = LightType.Point; lt.range = range; lt.intensity = intensity; lt.color = c; lt.shadows = LightShadows.None; return lt;
        }
        static readonly Color Warm = new Color(1f, .68f, .34f);
        // a straight stair of steps (rise 0.33, run 0.6: walkable) from `from` (feet) climbing along `dir` up to `top`
        static void Stair(string name, Vector3 from, Vector3 dir, float width, float top, float rise = .33f, float run = .6f)
        {
            int n = Mathf.CeilToInt((top - from.y) / rise); var side = Vector3.Cross(Vector3.up, dir).normalized;
            for (int i = 0; i < n; i++)
            {
                float t = Mathf.Min(top, from.y + (i + 1) * rise);
                var c = from + dir * (run * (i + .5f)); c.y = (t + from.y - .3f) / 2;
                var s = Box(name, c, new Vector3(Mathf.Abs(side.x) * width + Mathf.Abs(dir.x) * run, t - from.y + .3f, Mathf.Abs(side.z) * width + Mathf.Abs(dir.z) * run), "CastleStone");
                _ = s;
            }
        }
        // a torch on a wall (lit: a flame and a warm light; `light` false = no point light, to keep their number down)
        static void Torch(Vector3 at, float yaw, bool light = true)
        {
            var t = Prop("Torch", at.x, at.z, yaw, 1f, at.y);
            if (!t) Placeholders.Prim(PrimitiveType.Sphere, root, at + Vector3.up * .4f, new Vector3(.25f, .4f, .25f), Placeholders.Mat(new Color(1f, .6f, .2f), "Glow"), name: "Torch flame");
            if (light) PointLight("Torch light", at + Vector3.up * .6f + Quaternion.Euler(0, yaw, 0) * Vector3.back * .5f, 13, 2.4f, Warm);   // the prop leans out toward its -z
        }
        // a brazier dragon Kira lights (FireTarget); unlit until then
        static FireTarget Brazier(string id, Vector3 at, FireGroup group, float range = 10)
        {
            var go = Prop("Brazier", at.x, at.z, 0, 1f, at.y) ?? Placeholders.Prim(PrimitiveType.Cylinder, root, at + Vector3.up * .55f, new Vector3(.8f, .55f, .8f), WorldArt.Mat("Iron"), false, "Brazier");
            var col = go.AddComponent<BoxCollider>(); col.center = go.transform.InverseTransformPoint(at + Vector3.up * .6f) ; col.size = new Vector3(.9f, 1.2f, .9f);
            var f = go.AddComponent<FireTarget>(); f.kind = FireTarget.Kind.Brazier; f.id = id; f.group = group;
            var coals = go.transform.Find("Coals__Ember");
            var flame = new GameObject("Flame"); flame.transform.SetParent(go.transform, false); flame.transform.position = at + Vector3.up * 1.15f;
            for (int i = 0; i < 3; i++) Placeholders.Prim(PrimitiveType.Sphere, flame.transform, new Vector3((i - 1) * .14f, i * .08f, 0), new Vector3(.3f, .55f - i * .1f, .3f), Placeholders.Mat(i == 1 ? new Color(1f, .8f, .3f) : new Color(1f, .45f, .12f), "Glow"), name: "Tongue");
            if (coals) coals.SetParent(flame.transform, true);
            f.flame = flame; f.lamp = PointLight("Brazier light", at + Vector3.up * 1.6f, range, 2.6f, Warm);
            if (group) group.targets.Add(f);
            return f;
        }

        static void FallbackGround()
        {
            Box("Fallback ground W", new Vector3(-125, -.5f, 0), new Vector3(154, 1, 340), "Grass"); Box("Fallback ground E", new Vector3(125, -.5f, 0), new Vector3(154, 1, 340), "Grass");
            Box("Fallback ground S", new Vector3(0, -.5f, -95), new Vector3(96, 1, 154), "Grass"); Box("Fallback ground N", new Vector3(0, -.5f, 136), new Vector3(96, 1, 68), "Grass");
            Box("Fallback island", new Vector3(0, -.5f, 42), new Vector3(72, 1, 96), "Cobble");
            Box("Fallback moat floor", new Vector3(0, -5.5f, 42), new Vector3(96, 1, 120), "Dirt");
        }

        // ---- the castle's colliders (the Blender "Castle" is the look; these numbers match it)
        static void Castle(WorldProgress prog)
        {
            Prop("Castle", 0, 0, 0, 1f, 0f);
            const float wb = -5f;   // walls go down into the moat
            Wall("West wall", -33, -30, -3, 63, 10, wb); Wall("East wall", 30, 33, -3, 63, 10, wb);
            Wall("South wall W", -33, -8, -3, 0, 10, wb); Wall("South wall E", 8, 33, -3, 0, 10, wb);
            Wall("Gatehouse W", -8, -4, -6, 2, 14, wb); Wall("Gatehouse E", 4, 8, -6, 2, 14, wb); Wall("Gate arch", -4, 4, -6, 2, 14, 7);
            Wall("North wall W", -33, -18, 60, 63, 10, wb); Wall("North wall E", 18, 33, 60, 63, 10, wb);
            // crenellations along the walks' outer edges (a low lip so nobody walks off the outside by accident)
            Wall("West parapet", -33, -32.4f, 3.5f, 56.5f, 11.2f, 10); Wall("North parapet W", -27, -18, 62.4f, 63, 11.2f, 10); Wall("North parapet E", 18, 27, 62.4f, 63, 11.2f, 10);   // (Fix 8.1: the north walks had none: feet went into the merlons) Wall("East parapet", 32.4f, 33, 3.5f, 56.5f, 11.2f, 10); Wall("South parapet", -26.5f, 26.5f, -3, -2.4f, 11.2f, 10);
            // the round towers: four turned boxes each (radius 5); the north ones open on top at 16, the south ones roofed
            foreach (var (x, z, open) in new[] { (-31.5f, -1.5f, false), (31.5f, -1.5f, false), (-31.5f, 61.5f, true), (31.5f, 61.5f, true) })
            {
                Round(new Vector3(x, 0, z), 5f, 16f);   // (Fix 8.1: a cylinder as drawn (r 5), from the ground up; the old two-box star left its flats 1.4 m inside the tower)
                if (open) for (int i = 0; i < 8; i++) { if (i == 6) continue; /* the south side: the steps come up here */ float a = i * 45f * Mathf.Deg2Rad; Solid(new Vector3(x + Mathf.Cos(a) * 4.4f, 16, z + Mathf.Sin(a) * 4.4f), new Vector3(1.2f, 1.1f, 2.6f), -i * 45f); }
                else Solid(new Vector3(x, 16, z), new Vector3(7.5f, 9f, 7.5f));   // the roof (no landing on its cone)
            }
            // the keep: 3 m walls, the hall inside, the upper floors solid above the hall's ceiling, the roof walk at 22; the spired tower
            Wall("Keep south W", -18, -3, 63, 66, 22, 0); Wall("Keep south E", 3, 18, 63, 66, 22, 0); Wall("Keep door top", -3, 3, 63, 66, 22, 7);
            Wall("Keep north", -18, 18, 84, 87, 22, 0); Wall("Keep west", -18, -15, 66, 84, 22, 0); Wall("Keep east", 15, 18, 66, 84, 22, 0);
            Wall("Keep upper", -15, 15, 66, 84, 22, 14);
            // Fix 8.1, item 1: the three steps in front of the keep door (the Blender ones were looks only: feet sank into them)
            Wall("Keep step 1", -5, 5, 59, 63, .15f, 0); Wall("Keep step 2", -5, 5, 60.33f, 63, .3f, 0); Wall("Keep step 3", -5, 5, 61.67f, 63, .45f, 0);
            Solid(new Vector3(14, 21, 83), new Vector3(5.2f, 13f, 5.2f));   // the spired tower stands on the keep's roof
            // the arcade along the east wall: piers every 4 m, its roof deck at 6.6, a stair up from the courtyard's north end, steps on to
            // the east wall walk at 10
            for (float z = 8; z <= 52.1f; z += 4) Wall("Arcade pier", 24, 25, z - .5f, z + .5f, 6, 0);
            Wall("Arcade roof", 24, 30, 7.6f, 52.4f, 6.6f, 6);   // (Fix 8.1: to the deck's edge, not 0.5 m past it)
            Wall("Arcade lip", 23.85f, 24.15f, 7.6f, 52.4f, 6.75f, 6.6f);   // (Fix 8.1: the deck's sandstone edge)
            Stair("Arcade stair", new Vector3(26.5f, 0, 61.8f), Vector3.back, 2.6f, 6.6f, .33f, .5f);
            Stair("East walk steps", new Vector3(28.9f, 6.6f, 9f), Vector3.forward, 2.2f, 10f, .3f, .7f);   // x 27.8..30, z 9..17
            Stair("West wall stair", new Vector3(-28.8f, 0, 13f), Vector3.forward, 2.4f, 10f, .33f, .6f);   // z 13..31 (the walled yard ends at 12)
            // up the two north towers from the walks (steps on the walk's inner half)
            Stair("NW tower steps", new Vector3(-30.8f, 10f, 46.2f), Vector3.forward, 1.4f, 16f, .33f, .6f);   // z 46.2..57: onto the tower's top
            Stair("NE tower steps", new Vector3(30.8f, 10f, 46.2f), Vector3.forward, 1.4f, 16f, .33f, .6f);
            Hint(new Vector3(-27, 0, 14), 3f, () => "A stair up to the west wall walk. Watch out for archers on the far walls...");
            // wall torches round the courtyard (eight with a light)
            // (the Torch prop leans out toward its -z: yaw -90 = out toward +x)
            foreach (var (x, z, yaw, light) in new[] { (-29.6f, 36f, -90f, true), (-29.6f, 42f, -90f, false), (-29.6f, 52f, -90f, true), (23.6f, 18f, 90f, true), (23.6f, 42f, 90f, true),
                                                       (-12f, .4f, 180f, true), (12f, .4f, 180f, false), (-6f, 62.6f, 0f, true), (6f, 62.6f, 0f, true) })
                Torch(new Vector3(x, 3.2f, z), yaw, light);
            _ = prog;
        }

        // ---- the courtyard: the stone, the wagon wreck, benches and pots, the walled yard, the armory, the store room, the bard
        static void Courtyard(WorldProgress prog)
        {
            Playground.CheckpointHere(Start, new Vector3(0, .05f, 8), 0, root, false);
            Cp(VortexCp, 6, 12, 0); Cp("ME-court", -10, 30, 90);
            Hint(new Vector3(0, 0, 12), 7f, () => $"The Medieval Kingdom, by torchlight. Every shard counts: at {Game.RuleText(Game.World.areas[0])}, the purple shard on Dan's necklace wakes ({Game.I.Progress(Game.World.areas[0])}).");
            // the stone: the round pedestal, the purple shard stuck in it (looks), the necklace's hum nearby
            Prop("Pedestal", StoneAt.x, StoneAt.z, 0, 1f, 0f); Solid(StoneAt, new Vector3(1.25f, 1.1f, 1.25f)); Solid(StoneAt, new Vector3(1.25f, 1.1f, 1.25f), 45);
            StuckShard = Prop("PurpleShard", StoneAt.x, StoneAt.z, 15, 1f, 1.05f);
            if (!StuckShard) StuckShard = Placeholders.CrystalObject("Purple shard (stuck)", Purple, .9f);
            StuckShard.transform.SetParent(root, true); StuckShard.transform.position = StoneAt + Vector3.up * 1.05f;
            StuckShard.SetActive(!prog.storyShard && !prog.necklaceAwake);
            PointLight("The stone's glow", StoneAt + Vector3.up * 1.8f, 6, 1.6f, new Color(.7f, .45f, 1f));
            new GameObject("Stone hum").AddComponent<StoneHum>().transform.position = StoneAt;
            // benches, flower pots, barrels, crates
            foreach (var (x, z, yaw) in new[] { (-14f, 20f, 90f), (-14f, 26f, 90f), (14f, 46f, -90f), (-8f, 56f, 180f), (8f, 56f, 180f) }) { Prop("Bench", x, z, yaw); Solid(new Vector3(x, 0, z), new Vector3(2f, .5f, .6f), yaw); }
            foreach (var (x, z) in new[] { (-16f, 17f), (-16f, 29f), (16f, 41f), (16f, 51f), (-5f, 60f), (5f, 60f), (-25f, 40f), (-4.5f, 30f), (4.5f, 38f) }) { Prop("FlowerPot", x, z, x * 13); Solid(new Vector3(x, 0, z), new Vector3(.8f, .51f, .8f)); }   // (Fix 8.1: the pot's rim, r 0.4, top 0.51)
            foreach (var (x, z) in new[] { (-22f, 30f), (-21.2f, 31f), (20f, 58f) }) { Prop("Barrel", x, z, x * 31); Solid(new Vector3(x, 0, z), new Vector3(.8f, 1.1f, .8f)); }
            foreach (var (x, z, y) in new[] { (21f, 56.6f, 0f), (21f, 56.6f, 1f) }) { Prop("Crate", x, z, 0, 1f, y); Solid(new Vector3(x, y, z), new Vector3(1f, 1f, 1f)); }
            // the hay wagon's wreck against the west wall (the arrival), its singed driver and his pony (fine)
            var site = new GameObject("Wreck site").transform; site.SetParent(root, false); WreckSite = site.gameObject;
            Prop("WagonWreck", WreckAt.x, WreckAt.z, 80)?.transform.SetParent(site, true); Solid(WreckAt, new Vector3(2.4f, 1.2f, 4.2f), 80);
            for (int i = 0; i < 3; i++) Placeholders.Prim(PrimitiveType.Cylinder, site, new Vector3(-29.4f, 1.5f + i * .9f, 24 + (i - 1) * .9f), new Vector3(1.4f, .02f, 1.4f), Placeholders.Mat(new Color(.12f, .1f, .09f)), name: "Scorch").transform.localRotation = Quaternion.Euler(0, 0, 90);
            Npc("Wagon driver", "wagon_driver", new Vector3(-22.5f, 0, 27f), 210, "dazed").SetParent(site, true);
            Npc("Pony", "horse", new Vector3(-21f, 0, 18.5f), 150, null).SetParent(site, true);
            Hint(new Vector3(-21, 0, 24), 4f, () => "What's left of the hay wagon. The driver is still smoking a little. The pony is fine.");
            // the walled yard behind a boulder (Muscle Man), its rival show-off spot
            Slab("Yard wall N", -30, -20, 11.5f, 12, 3.5f, 0); Slab("Yard wall E S", -20.5f, -20, 1, 5.5f, 3.5f, 0); Slab("Yard wall E N", -20.5f, -20, 9.5f, 12, 3.5f, 0);
            var bould = Prop("Boulder", -19f, 7.5f, 30, 1.25f) ?? Placeholders.Prim(PrimitiveType.Sphere, root, OnGround(-19, 7.5f) + Vector3.up * 1.8f, Vector3.one * 3.6f, WorldArt.Mat("Stone"), false, "Boulder");
            if (!bould.GetComponentInChildren<Collider>()) { var bAt = OnGround(-19, 7.5f) + Vector3.up * 1.6f; var bc = bould.AddComponent<SphereCollider>(); bc.radius = 1.85f / Mathf.Max(.01f, bould.transform.lossyScale.x); bc.center = bould.transform.InverseTransformPoint(bAt); }   // (Fix 8.1: the ground found before the sphere exists: it used to land on its own new collider, 1.9 m up, with a gap under it)
            var boulder = bould.AddComponent<Boulder>(); boulder.id = "ME-boulder-yard"; boulder.aside = new Vector3(3.2f, 0, 1.5f);
            Hint(new Vector3(-16, 0, 7.5f), 3.5f, () => SaveGame.Current.Has(Abilities.MuscleMan) ? "" : "A walled yard, blocked by a boulder. Way too heavy for anyone here...");
            var pebble = Prop("Boulder", -11f, 13f, 0, .42f); if (!pebble) pebble = Placeholders.Prim(PrimitiveType.Sphere, root, OnGround(-11, 13) + Vector3.up * .7f, Vector3.one * 1.3f, WorldArt.Mat("Stone"), false, "Small boulder");
            var r1 = RivalMuscleMan.Make(root, "courtyard", OnGround(-12.5f, 11.5f), 120, "lifts a boulder"); r1.lifted = pebble.transform; r1.liftedHome = pebble.transform.position; CourtRival = r1;
            // the armory behind a portcullis (Muscle Man)
            Slab("Armory wall W", 18, 18.5f, 0, 8, 4.4f, 0); Slab("Armory wall E", 25.5f, 26, 0, 8, 4.4f, 0); Slab("Armory front W", 18, 20, 7.5f, 8, 4.4f, 0); Slab("Armory front E", 24, 26, 7.5f, 8, 4.4f, 0);
            Slab("Armory front top", 20, 24, 7.5f, 8, 4.4f, 3.6f); Slab("Armory roof", 18, 26, 0, 8, 4.8f, 4.4f);
            var pg = Prop("SmallPortcullis", 22, 7.75f, 0, 1f, 0f) ?? Placeholders.Prim(PrimitiveType.Cube, root, new Vector3(22, 1.8f, 7.75f), new Vector3(4, 3.6f, .2f), WorldArt.Mat("Iron"), false, "Portcullis");
            var pc = pg.AddComponent<BoxCollider>(); pc.center = pg.transform.InverseTransformPoint(new Vector3(22, 1.8f, 7.75f)); pc.size = new Vector3(4f, 3.6f, .3f) / Mathf.Max(.01f, pg.transform.lossyScale.x);
            var port = pg.AddComponent<Portcullis>(); port.id = "ME-portcullis-armory"; port.lift = 3.3f;
            foreach (var (x, z) in new[] { (19.2f, 1f), (24.8f, 1f) }) { Prop("Shield", x, z, 0, 1f, 1.3f); }
            // the store room behind a cracked wall (the charged punch)
            Slab("Store wall S", -30, -22, 52, 52.5f, 4.4f, 0); Slab("Store wall E S", -22.5f, -22, 52, 54, 4.4f, 0); Slab("Store wall E N", -22.5f, -22, 58, 60, 4.4f, 0);
            Slab("Store roof", -30, -22, 52, 60, 4.8f, 4.4f);
            if (!prog.opened.Contains("ME-crack-store"))
            {
                var rock = Slab("Cracked wall", -22.5f, -22, 54, 58, 4.4f, 0); rock.gameObject.AddComponent<RockWall>().id = "ME-crack-store";
                for (int i = 0; i < 3; i++) Placeholders.Prim(PrimitiveType.Cube, rock, new Vector3(.55f, .1f * i - .1f, .1f * i), new Vector3(.1f, .05f, .5f), Placeholders.Mat(new Color(.3f, .22f, .15f)), name: "Crack").transform.localRotation = Quaternion.Euler(i * 40 - 30, 0, 0);
            }
            Hint(new Vector3(-19, 0, 56), 3f, () => SaveGame.Current.Has(Abilities.ChargedPunch) ? $"A cracked wall: hold {B(Controls.Act.Attack)} to charge Dan's punch, then let go." : "This wall is cracked... a much stronger punch could break it.");
            // the dungeon's stair door (west wall)
            Box("Dungeon door", new Vector3(-29.85f, 1.6f, 46), new Vector3(.3f, 3.2f, 2.4f), new Color(.05f, .04f, .04f), "Glow", false);
            Playground.PortalHere("ME-dungeon-in", new Vector3(-29.2f, 0, 46), 90, DungeonCp, true, root, new Color(.25f, .2f, .18f));
            Hint(new Vector3(-26, 0, 46), 3.5f, () => "Steps down into the dark: the dungeon.");
            // the bard: the Violinist with his lute (he teaches Dan the charged punch)
            var v = Violinist.Make(root, OnGround(ViolinistAt.x, ViolinistAt.z), 100f, "Lute");
            v.ability = Abilities.ChargedPunch; v.learner = "Dan"; v.moveName = "Charged Punch";
            Hint(ViolinistAt + new Vector3(3, 0, 0), 5f, () => SaveGame.Current.Has(Abilities.ChargedPunch) ? $"Dan's charged punch: hold {B(Controls.Act.Attack)}, let go when his fist glows. It breaks cracked walls (in every world) and knights' shields." : "A bard with a lute, playing softly by the benches. He keeps glancing at Dan...");
            // the vortex gate (open after the stone: the way back to the vortex)
            Gate = Playground.PortalHere("ME-vortex-gate", new Vector3(7, 0, 30), -90, Vortex.Checkpoint, false, root, Purple, vortex: true);
            Gate.gameObject.SetActive(prog.storyShard);
            PointLight("Courtyard glow", new Vector3(0, 9, 30), 40, 1.1f, new Color(1f, .75f, .45f));
        }

        // a person or animal standing about (no collider; its own loop)
        static Transform Npc(string name, string model, Vector3 at, float yaw, string loop)
        {
            var go = new GameObject(name); go.transform.SetParent(root, false); go.transform.SetPositionAndRotation(OnGround(at.x, at.z), Quaternion.Euler(0, yaw, 0));
            var anim = CharacterModel.Creature(go.transform, model, 1.2f, 4f);
            if (anim) anim.Drive = () => (loop, 0f);
            else Placeholders.Prim(PrimitiveType.Capsule, go.transform, new Vector3(0, .85f, 0), new Vector3(.5f, .85f, .5f), Placeholders.Mat(new Color(.55f, .4f, .28f)), name: "Body");
            return go.transform;
        }

        // ---- the great hall: the feast, the gallery, the chandeliers, the east ledge
        static void Hall(WorldProgress prog)
        {
            Cp("ME-hall", 0, 68, 0);
            Hint(new Vector3(0, 0, 68), 5f, () => "The great hall. Up the stair to the gallery... and those chandeliers look sturdy enough to ride.");
            Prop("FeastTable", 0, 73, 0, 1f, 0f); Solid(new Vector3(0, 0, 73), new Vector3(12f, .84f, 1.3f));   // 12 m along x, top at 0.84 (Fix 8.1: its collider had slipped into a comment)
            foreach (var z in new[] { 71.85f, 74.15f }) Solid(new Vector3(0, 0, z), new Vector3(11.6f, .47f, .4f));   // the benches (Fix 8.1: on the Blender ones, z 73 +- 1.15)
            foreach (var x in new[] { -10f, 0f, 10f }) Prop("Banner", x, 83.4f, 0, 1f, 12.5f);   // origin at its rod, the cloth hangs 4 m, facing -z (into the hall)
            // the gallery (west and north, at 6), its stair along the south wall
            Slab("West gallery", -15, -11, 66, 84, 6, 5.6f, "Plank"); Slab("North gallery", -11, 15, 80, 84, 6, 5.6f, "Plank");
            Slab("West gallery rail", -11.15f, -11, 69, 80, 7.1f, 6, "Wood");
            Wall("North gallery rail", -11.1f, 15, 79.85f, 80.1f, 7.1f, 6);   // (Fix 8.1: the Blender rail had no collider: you walked through it, and onto the east ledge without the chandeliers)
            Stair("Gallery stair", new Vector3(-.8f, 0, 67f), Vector3.left, 2f, 6f, .33f, .56f);
            // the east ledge (a heart, two shards) across the hall: two chandeliers swing to it
            Slab("East ledge", 11, 15, 70, 80, 6, 5.6f, "Plank");
            for (int i = 0; i < 2; i++)
            {
                var ch = new GameObject("Chandelier " + (i + 1)); ch.transform.SetParent(root, false); ch.transform.position = new Vector3(i == 0 ? -9 : 9, 5.8f, 75);
                var look = WorldArt.Prop("medieval_props", "Chandelier", ch.transform, ch.transform.position);
                if (look) { look.transform.SetParent(ch.transform, true); look.transform.localPosition = new Vector3(0, 3f, 0); }   // its origin is the hook, the ring 3 m below
                else Placeholders.Prim(PrimitiveType.Cylinder, ch.transform, Vector3.zero, new Vector3(2.4f, .08f, 2.4f), WorldArt.Mat("Iron"), false, "Ring");
                var deck = new GameObject("Deck"); deck.transform.SetParent(ch.transform, false); deck.transform.localPosition = new Vector3(0, .05f, 0);
                deck.AddComponent<BoxCollider>().size = new Vector3(2.3f, .25f, 2.3f);
                Placeholders.Prim(PrimitiveType.Cylinder, ch.transform, new Vector3(0, look ? 5.6f : 4f, 0), new Vector3(.06f, look ? 2.6f : 4f, .06f), WorldArt.Mat("Iron"), false, "Chain");
                var mp = ch.AddComponent<MovingPlatform>(); mp.period = 5.5f;
                if (i == 0) { mp.a = new Vector3(-9, 5.8f, 75); mp.b = new Vector3(-1.2f, 5.8f, 75); } else { mp.a = new Vector3(9, 5.8f, 75); mp.b = new Vector3(1.2f, 5.8f, 75); }
                PointLight("Chandelier light", ch.transform.position + Vector3.down * .3f, 12, 1.8f, Warm).transform.SetParent(ch.transform, true);
            }
            Hint(new Vector3(-12, 6, 75), 2.5f, () => "Ride the chandeliers across: jump on as one swings close, hop to the next in the middle.");
            PointLight("Fireplace", new Vector3(13.5f, 1.2f, 75), 14, 3f, new Color(1f, .5f, .2f));
            Placeholders.Prim(PrimitiveType.Sphere, root, new Vector3(14.2f, .6f, 75), new Vector3(1.4f, .8f, .9f), Placeholders.Mat(new Color(1f, .5f, .15f), "Glow"), name: "Fire");
            Solid(new Vector3(14.2f, 0, 75), new Vector3(1.6f, 1f, 2.6f)); Wall("Hearth", 13.2f, 15, 72, 78, .25f, 0);   // (Fix 8.1: the hearth stone is a 25 cm step)
            // the rival at the feast: he lifts a barrel over his head with one hand
            var keg = Prop("Barrel", 9.5f, 70.5f, 0, 1.2f, 0f) ?? Placeholders.Prim(PrimitiveType.Cylinder, root, new Vector3(9.5f, .6f, 70.5f), new Vector3(.9f, .6f, .9f), WorldArt.Mat("Wood"), false, "Barrel");
            var r3 = RivalMuscleMan.Make(root, "hall", new Vector3(8.2f, 0, 69.5f), -60, "lifts a barrel"); r3.lifted = keg.transform; r3.liftedHome = keg.transform.position; r3.radius = 9f;
            PointLight("Hall light", new Vector3(0, 9, 75), 26, 1.4f, Warm);
            _ = prog;
        }

        // ---- the moat: water round the island, the deep pit, the drawbridge
        static void Moat()
        {
            Cp("ME-moat", 0, -24, 0);
            void Water(string n, float x0, float x1, float z0, float z1, float top, float bottom)
            {
                var w = Box(n, new Vector3((x0 + x1) / 2, (top + bottom) / 2, (z0 + z1) / 2), new Vector3(x1 - x0, top - bottom, z1 - z0), new Color(.12f, .22f, .32f, .75f), "Water", false);
                w.gameObject.layer = 4; w.gameObject.AddComponent<WaterVolume>();
            }
            Water("Moat W", -48, -36, -18, 102, -1.2f, -5.2f); Water("Moat E", 36, 48, -18, 102, -1.2f, -5.2f);
            Water("Moat S", -36, 36, -18, -6, -1.2f, -5.2f); Water("Moat N", -36, 36, 90, 102, -1.2f, -5.2f);
            Water("Moat deep pit", -48, -36, 90, 102, -5.2f, -16.2f);
            var deep = Box("Deep water (deep dive)", new Vector3(-42, -11, 96), new Vector3(12, 10, 12), Color.clear, "Water", false); deep.GetComponent<Renderer>().enabled = false; deep.gameObject.layer = 2; deep.gameObject.AddComponent<DeepWater>();
            Hint(new Vector3(-44, 0, 108), 5f, () => "The moat goes much deeper in this corner... too deep to dive, for now.");
            // stone steps out of the water onto the outer bank at each side's middle: two 0.3 m steps (walkable), the water at -1.2
            foreach (var (x0, x1, z0, z1, sx, sz) in new[] { (-48f, -46.6f, 38f, 42f, 1, 0), (46.6f, 48f, 28f, 32f, -1, 0), (22f, 26f, -18f, -16.6f, 0, 1), (-2f, 2f, 100.6f, 102f, 0, -1) })
            {
                Slab("Moat step", x0, x1, z0, z1, -.6f, -5.2f, "CastleStone");
                // the upper step: the half of it next to the bank
                float hx0 = sx > 0 ? x0 : sx < 0 ? (x0 + x1) / 2 : x0, hx1 = sx > 0 ? (x0 + x1) / 2 : x1, hz0 = sz > 0 ? z0 : sz < 0 ? (z0 + z1) / 2 : z0, hz1 = sz > 0 ? (z0 + z1) / 2 : z1;
                Slab("Moat step (upper)", hx0, hx1, hz0, hz1, -.3f, -5.2f, "CastleStone");
            }
            // the drawbridge (down) over the south moat, under the gatehouse
            // Fix 8.1, item 2: the deck's collider is a child of the drawbridge (hinged at the gate, the deck out to z -18), so it moves with
            // it when it's raised or lowered (the line had slipped into a comment in Round 8: Dan fell through)
            var bridge = Prop("Drawbridge", 0, -3, 0, 1f, 0f);
            float dx0 = -4, dx1 = 4, dz0 = -18.2f, dz1 = -5.8f, dTop = 0f;
            if (bridge)
            {
                // the collider goes exactly where the planks are seen (and the deck must point out over the moat, not into the gate)
                Bounds DeckSeen() { Bounds b = default; bool any = false; foreach (var r in bridge.GetComponentsInChildren<Renderer>()) if (r.name.StartsWith("Deck")) { if (!any) b = r.bounds; else b.Encapsulate(r.bounds); any = true; } return any ? b : new Bounds(new Vector3(0, -.2f, -10.5f), new Vector3(8, .4f, 15)); }
                var seen = DeckSeen();
                if (seen.center.z > -3f) { bridge.transform.rotation = Quaternion.Euler(0, 180, 0) * bridge.transform.rotation; seen = DeckSeen(); }
                dx0 = seen.min.x; dx1 = seen.max.x; dz0 = Mathf.Min(seen.min.z, -18.2f); dz1 = Mathf.Max(seen.max.z, -5.8f); dTop = seen.max.y;
                PlayLog.Line($"drawbridge: planks seen x {seen.min.x:0.0}..{seen.max.x:0.0}, z {seen.min.z:0.0}..{seen.max.z:0.0}, top {seen.max.y:0.00}");
            }
            var deck = Slab("Drawbridge deck", dx0, dx1, dz0, dz1, dTop, dTop - .4f, "Plank");
            if (bridge) { deck.GetComponent<Renderer>().enabled = false; deck.SetParent(bridge.transform, true); }
            Drawbridge = bridge ? bridge.transform : deck;
            Hint(new Vector3(0, 0, -12), 4f, () => "The drawbridge. Outside: the tournament field (east) and the village (west).");
        }

        public static Vector3 KeyAt(int cell) => new Vector3(Dungeon.x + 8.5f, 1f, cell == 0 ? 19f : cell == 1 ? 33f : 6f);
        // ---- the dungeon (x 400): a corridor, three cells with keys, braziers, rats
        static void DungeonSpace(WorldProgress prog)
        {
            float X = Dungeon.x;
            Cp(DungeonCp, X, 6f, 0, false);   // (well clear of the stair door at z 0; no flag: it stood in the corridor's way)
            Slab("Dungeon floor", X - 14, X + 14, -2, 60, 0, -1, "Stone");
            Slab("Dungeon ceiling", X - 14, X + 14, -2, 60, 5.6f, 4.6f, "Stone");
            Slab("Dungeon end S", X - 14, X + 14, -2, -1, 4.6f, 0, "Stone"); Slab("Dungeon end N", X - 14, X + 14, 59, 60, 4.6f, 0, "Stone");
            Slab("Dungeon outer W", X - 14, X - 13, -1, 59, 4.6f, 0, "Stone"); Slab("Dungeon outer E", X + 13, X + 14, -1, 59, 4.6f, 0, "Stone");
            // the corridor x 396..404; cells west (x 388..396) z 9..17, 23..31, 37..45 behind bars; east alcoves (open) with the keys
            float[] cz = { 9, 23, 37 };
            Slab("Corridor wall W", X - 4.5f, X - 4, -1, cz[0], 4.6f, 0, "Stone");
            for (int i = 0; i < 3; i++)
            {
                float z0 = cz[i], z1 = z0 + 8;
                Slab("Cell wall", X - 13, X - 4, z1, z1 + .5f, 4.6f, 0, "Stone");
                if (i < 2) Slab("Corridor wall W", X - 4.5f, X - 4, z1, cz[i + 1], 4.6f, 0, "Stone");
                Slab("Cell front S", X - 4.5f, X - 4, z0, z0 + 2.5f, 4.6f, 0, "Stone"); Slab("Cell front N", X - 4.5f, X - 4, z0 + 5.5f, z1, 4.6f, 0, "Stone");
                Slab("Cell door top", X - 4.5f, X - 4, z0 + 2.5f, z0 + 5.5f, 4.6f, 3.2f, "Stone");
                if (i == 0) Slab("Cell wall S", X - 13, X - 4, z0 - .5f, z0, 4.6f, 0, "Stone");
                var bars = Prop("CellBars", X - 4.25f, z0 + 4f, 90, 1f, 0f) ?? Placeholders.Prim(PrimitiveType.Cube, root, new Vector3(X - 4.25f, 1.6f, z0 + 4f), new Vector3(.15f, 3.2f, 3f), WorldArt.Mat("Iron"), false, "Cell bars");
                var bc = bars.AddComponent<BoxCollider>(); bc.center = bars.transform.InverseTransformPoint(new Vector3(X - 4.25f, 1.6f, z0 + 4f)); bc.size = bars.transform.InverseTransformVector(new Vector3(.3f, 3.2f, 3f)); bc.size = new Vector3(Mathf.Abs(bc.size.x), Mathf.Abs(bc.size.y), Mathf.Abs(bc.size.z));
                var door = bars.AddComponent<CellDoor>(); door.id = "ME-cell-" + (i + 1);
                // its key: in the middle of an east alcove (cell 1's: z 12.5..26; cell 2's: z 26.5..40; cell 3's: by the stair, z -1..12)
                var keyAt = KeyAt(i);
                var key = Prop("Key", keyAt.x, keyAt.z, 0, 1.6f, keyAt.y) ?? Placeholders.Prim(PrimitiveType.Cube, root, keyAt, new Vector3(.12f, .45f, .08f), WorldArt.Mat("Iron"), false, "Key");
                key.AddComponent<CellKey>().door = door;
                PointLight("Key glint", keyAt + Vector3.up * .5f, 4, 1.2f, new Color(1f, .85f, .5f));
            }
            Slab("Corridor wall W N", X - 4.5f, X - 4, 45.5f, 50, 4.6f, 0, "Stone");
            // east alcoves (open): walls between them
            foreach (var z in new[] { 12f, 26f, 40f }) Slab("Alcove wall", X + 4, X + 13, z, z + .5f, 4.6f, 0, "Stone");
            Slab("Corridor wall E", X + 4, X + 4.5f, 46, 50, 4.6f, 0, "Stone");
            // the brazier room (z 50..59) behind bars that open when the three braziers are lit
            Slab("Brazier room wall W", X - 13, X - 4, 50, 50.5f, 4.6f, 0, "Stone"); Slab("Brazier room wall E", X + 4, X + 13, 50, 50.5f, 4.6f, 0, "Stone");
            var gate = Prop("CellBars", X, 50.25f, 0, 1f, 0f);
            if (gate) gate.transform.localScale = Vector3.Scale(gate.transform.localScale, new Vector3(4f, 1f, 1f));
            else gate = Placeholders.Prim(PrimitiveType.Cube, root, new Vector3(X, 1.6f, 50.25f), new Vector3(8f, 3.2f, .15f), WorldArt.Mat("Iron"), false, "Brazier room bars");
            var gb = Wall("Brazier room bars (collider)", X - 4, X + 4, 50, 50.5f, 4.6f, 0);
            var group = new GameObject("Dungeon braziers").AddComponent<FireGroup>(); group.transform.SetParent(root, false); group.hint = "braziers lit...";
            var gateObj = gate; var gbObj = gb.gameObject;
            group.onAllLit = loading => { gateObj.SetActive(false); gbObj.SetActive(false); if (!loading) { Sfx.Play("portcullis", .6f); Hud.Message("All three braziers blaze: the bars at the end of the corridor swing open!", 4f); } };
            foreach (var z in new[] { 8f, 22f, 36f }) Brazier("ME-brazier-d" + z, new Vector3(X + 3f, 0, z), group, 9);
            Hint(Dungeon + new Vector3(0, 0, 8), 3f, () => "Cold braziers. A little dragon's fire would light them... (Kira)");
            // dark: the necklace's light (and the braziers, once lit)
            var dark = new GameObject("Dungeon darkness"); dark.transform.SetParent(root, false); dark.transform.position = Dungeon + new Vector3(0, 2.5f, 29); dark.transform.localScale = new Vector3(28, 6, 62); dark.AddComponent<DarkVolume>();
            // the way out: the stair door at the south end
            Box("Dungeon stair door", Dungeon + new Vector3(0, 1.6f, -.8f), new Vector3(2.6f, 3.2f, .2f), new Color(.6f, .45f, .3f), "Glow", false);
            Playground.PortalHere("ME-dungeon-out", Dungeon + new Vector3(0, 0, .2f), 180, "ME-court-west", true, root, new Color(.6f, .45f, .3f));
            Cp("ME-court-west", -26, 44, 90, false);
            _ = prog;
        }

        // ---- the tournament field (south-east)
        static void Field(WorldProgress prog)
        {
            Cp("ME-field", 62, -56, 120);
            Hint(new Vector3(62, 0, -54), 6f, () => "The tournament field! Knights clank about, the crowd's gone home... except for one very big show-off.");
            // the lists' tilt barrier
            for (int i = 0; i < 5; i++) { Prop("ListBarrier", 65 + i * 10, -82, 90); Solid(new Vector3(65 + i * 10, 0, -82), new Vector3(10f, 1.3f, .4f)); }
            // the stands: three tiers (1, 2, 3 m) facing south over the lists; the flag platform behind (double jump, or Muscle Man's boost)
            Prop("Stands", 85, -64, 0, 1f, 0f);
            Wall("Stands tier 1", 75, 95, -64, -62.5f, 1, 0); Wall("Stands tier 2", 75, 95, -62.5f, -61, 2, 0); Wall("Stands tier 3", 75, 95, -61, -59.5f, 3, 0);
            Slab("Flag platform", 83, 87, -59.5f, -57.5f, 5.6f, 5.3f, "Plank"); Slab("Flag post", 84.8f, 85.2f, -58.7f, -58.3f, 9f, 5.6f, "Wood");
            Prop("BannerPole", 85, -58.5f, 0, 1f, 5.6f);
            var boost = new GameObject("Boost spot (stands)"); boost.transform.SetParent(root, false); boost.transform.SetPositionAndRotation(new Vector3(83.6f, 0, -55.6f), Quaternion.Euler(0, 180, 0));
            var bs = boost.AddComponent<BoostSpot>(); bs.height = 7.5f; bs.landAt = new Vector3(83.6f, 5.6f, -58.5f);   // beside the flag post
            Hint(new Vector3(85, 3, -60), 3f, () => SaveGame.Current.Has(Abilities.DoubleJump) ? "The flag platform: Abi's double jump reaches it." : "The flag platform's just out of reach... (Abi's double jump; or a boost from someone strong)");
            // three braziers on the top tier: light them all and the prize chest opens
            var group = new GameObject("Stands braziers").AddComponent<FireGroup>(); group.transform.SetParent(root, false); group.hint = "braziers on the stands lit...";
            var chest = Slab("Prize chest", 94.2f, 95.8f, -116.7f, -115.3f, 1.3f, 0, "Wood");
            var lid = Slab("Prize chest lid", 94.1f, 95.9f, -116.8f, -115.2f, 1.45f, 1.3f, "Gold");
            var chestObj = chest.gameObject; var lidObj = lid.gameObject;
            group.onAllLit = loading => { chestObj.SetActive(false); lidObj.transform.position += new Vector3(0, .2f, -1.1f); lidObj.transform.rotation = Quaternion.Euler(-70, 0, 0); if (!loading) { Sfx.Play("crowd_cheer", .5f); Hud.Message("All three braziers blaze! Down on the field, the prize chest pops open.", 4f); } };
            foreach (var x in new[] { 77f, 85f, 93f }) Brazier("ME-brazier-s" + x, new Vector3(x, 3f, -60.2f), group, 8);
            Hint(new Vector3(95, 0, -112), 3f, () => "The prize chest, locked tight. A sign: \"Light the three braziers of the stands.\"");
            // tents, hay bales (one heap hides a shard: burn it), the test of strength
            foreach (var (x, z, yaw) in new[] { (50f, -110f, 30f), (118f, -50f, -20f), (125f, -80f, -80f) }) { Prop("Tent", x, z, yaw); Solid(new Vector3(x, 0, z), new Vector3(4.6f, 3.2f, 4.6f), yaw); }
            foreach (var (x, z) in new[] { (110f, -104f), (112f, -107f), (106f, -118f) }) { Prop("HayBale", x, z, x * 7); Solid(new Vector3(x, 0, z), new Vector3(1.4f, .9f, 1f), x * 7); }
            var hay = Slab("Hay heap", 119.5f, 124.5f, -112.5f, -107.5f, 2.6f, 0, "Hay"); var hf = hay.gameObject.AddComponent<FireTarget>(); hf.kind = FireTarget.Kind.Hay; hf.id = "ME-hay-field";
            Hint(new Vector3(118, 0, -106), 3f, () => "A big heap of dry hay. Something glints inside it... (a little fire would clear it: Kira)");
            var strikerAt = new Vector3(70, 0, -100);
            var hs = Prop("HighStriker", strikerAt.x, strikerAt.z, 0, 1f, 0f);
            Solid(strikerAt + new Vector3(0, 0, .6f), new Vector3(.6f, 6f, .4f));
            var striker = new GameObject("High striker pad"); striker.transform.SetParent(root, false); striker.transform.position = strikerAt + new Vector3(0, .3f, -.5f);
            var sc = striker.AddComponent<BoxCollider>(); sc.isTrigger = true; sc.size = new Vector3(1.4f, 1.2f, 1.4f);
            Striker = striker.AddComponent<HighStriker>();
            var puck = new GameObject("Puck").transform; puck.SetParent(striker.transform, false); puck.localPosition = new Vector3(0, .5f, .9f);
            Placeholders.Prim(PrimitiveType.Cube, puck, Vector3.zero, new Vector3(.3f, .25f, .2f), WorldArt.Mat("Red"), false, "Puck block");
            Striker.puck = puck; Striker.bottom = .5f; Striker.top = 5.4f;
            var r2 = RivalMuscleMan.Make(root, "field", strikerAt + new Vector3(1.6f, 0, -1.6f), -30, "rings the bell"); r2.striker = Striker; Striker.rival = r2; r2.radius = 12f;
            Hint(strikerAt + new Vector3(0, 0, -4), 4f, () => $"The test of strength: hit the pad ({B(Controls.Act.Attack)}) and ring the bell!");
            PointLight("Field light", new Vector3(85, 8, -85), 45, 1.2f, Warm); PointLight("Striker light", strikerAt + new Vector3(0, 4, -3), 12, 2f, Warm);
            _ = hs; _ = prog;
        }

        // ---- the village and market (south-west)
        static void Village(WorldProgress prog)
        {
            Cp("ME-village", -44, -70, 270);
            Hint(new Vector3(-46, 0, -70), 6f, () => "The village below the castle: timber houses, the market, everyone asleep but the knights on watch.");
            foreach (var (x, z, k, yaw) in new[] { (-55f, -52f, 1, 180f), (-67f, -52f, 2, 180f), (-79f, -52f, 3, 180f), (-103f, -52f, 1, 180f), (-115f, -52f, 2, 180f),
                                                   (-55f, -112f, 3, 0f), (-75f, -112f, 1, 0f), (-87f, -112f, 2, 0f), (-110f, -112f, 3, 0f), (-122f, -112f, 1, 0f),
                                                   (-136f, -75f, 2, 90f), (-136f, -95f, 3, 90f), (-50f, -92f, 1, -90f) })
            { Prop("House" + k, x, z, yaw); Solid(new Vector3(x, 0, z), new Vector3(6f, 7f, 6f), yaw); }
            // the market square: stalls (their awnings at 2.4-2.6 m, up a stack of crates)
            foreach (var (x, z, yaw) in new[] { (-78f, -80f, 0f), (-92f, -80f, 0f), (-78f, -90f, 180f), (-95f, -90f, 180f), (-100f, -84f, 90f) })
            { Prop("MarketStall", x, z, yaw); Solid(new Vector3(x, 0, z), new Vector3(3f, 1f, 1.5f), yaw); Wall("Stall awning", x - 1.6f, x + 1.6f, z - .9f, z + .9f, 2.55f, 2.35f); }
            foreach (var (x, z, y) in new[] { (-96.5f, -92.4f, 0f), (-96.5f, -92.4f, 1f), (-95.4f, -92.4f, 0f) }) { Prop("Crate", x, z, 0, 1f, y); Solid(new Vector3(x, y, z), new Vector3(1f, 1f, 1f)); }
            Prop("Smithy", -120, -82, 90); Solid(new Vector3(-120, 0, -82), new Vector3(4f, 3f, 5f), 90);
            foreach (var (x, z) in new[] { (-70f, -70f), (-100f, -70f), (-85f, -100f) }) { Torch(new Vector3(x, 0f, z), 0, true); Box("Lamp post", new Vector3(x, 1.4f, z), new Vector3(.15f, 2.8f, .15f), "Wood"); }
            // brambles across an alley (V06 behind them): burn them, Kira
            foreach (var x in new[] { -64.5f, -60f, -55.5f })
            {
                // Fix 8.1, item 3: the bushes were 2 m across with 4.6 m colliders turned with each bush (invisible walls in the gaps): now
                // big enough to fill the alley, the collider an unturned child (the fire puff finds it in its parent)
                var br = Prop("Bramble", x, -118.5f, x * 9, 2.2f) ?? Placeholders.Prim(PrimitiveType.Sphere, root, OnGround(x, -118.5f) + Vector3.up * 1.4f, new Vector3(4.6f, 2.8f, 2f), WorldArt.Mat("Bramble"), false, "Bramble");
                var blk = new GameObject("Bramble block"); blk.transform.SetParent(br.transform, false);
                blk.transform.SetPositionAndRotation(OnGround(x, -118.5f) + Vector3.up * 1.4f, Quaternion.identity);
                blk.AddComponent<BoxCollider>().size = new Vector3(4.6f, 2.8f, 1.8f) / Mathf.Max(.01f, br.transform.lossyScale.x);
                var f = br.AddComponent<FireTarget>(); f.kind = FireTarget.Kind.Bramble; f.id = "ME-bramble-" + (int)x;
            }
            // Fix 8.1, item 3: these were invisible (nothing in the Blender village stands here): now seen stone walls
            Slab("Alley wall W", -69, -67, -130, -118, 7, 0); Slab("Alley wall E", -53, -51, -130, -118, 7, 0); Slab("Alley back", -69, -51, -131, -129, 7, 0);
            Hint(new Vector3(-60, 0, -114), 3f, () => "Thorny brambles choke the alley. Dragon Kira's fire would clear them.");
            // a narrow chimney between two houses (the wall jump, later)
            Slab("Chimney W", -101, -100, -126, -122, 9, 0); Slab("Chimney E", -98, -97, -126, -122, 9, 0);   // (Fix 8.1: seen, not invisible) Slab("Chimney cap", -101, -97, -126, -122, 9.3f, 9, "CastleStone");
            Hint(new Vector3(-99, 0, -119), 3f, () => "A narrow gap between two tall chimney walls. Someone who can kick off walls could climb it... (later)");
            _ = prog;
        }

        static void Enemies()
        {
            Vector3 G(float x, float z, float from = 30f) => OnGround(x, z, from) + Vector3.up * .1f;
            Enemy.Knight(root, G(0, 50), G(12, 54)); Enemy.Knight(root, G(16, 38), G(16, 50));   // (away from the checkpoints and the dungeon door)
            Enemy.Knight(root, G(70, -70), G(100, -70)); Enemy.Knight(root, G(105, -95), G(80, -100)); Enemy.Knight(root, G(-80, -70), G(-100, -100));
            Enemy.Archer(root, new Vector3(-15, 10.05f, -1.5f), 0); Enemy.Archer(root, new Vector3(15, 10.05f, -1.5f), 0); Enemy.Archer(root, new Vector3(31.5f, 10.05f, 40), -90);
            foreach (var (a, b) in new[] { (new Vector3(408, 0, 15), new Vector3(408, 0, 22)), (new Vector3(408, 0, 33), new Vector3(408, 0, 38)), (new Vector3(399, 0, 42), new Vector3(402, 0, 46)) })
                Enemy.Rat(root, G(a.x, a.z, 3f), G(b.x, b.z, 3f));
        }
    }
}
