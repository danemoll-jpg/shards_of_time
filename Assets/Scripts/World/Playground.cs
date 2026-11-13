using UnityEngine;

namespace ShardsOfTime
{
    // Round 1: the graybox playground, built in code from simple boxes (1 unit = 1 m). Area A at the origin: start path, slopes, steps,
    // a moving platform over a pit, the pool (dive), the low-gravity tower, Kira's crawl garden, Dan's push block ledge, the enemy yard
    // and spikes, the double-jump ledge, the "needs Muscle Man" boulder, the story shard and the portal to Area B (at x = 400): the
    // double jump pickup, a few shards, the ice cave (Round 2: sabertooth Kira claws its wall), the portal back. Shards, the story shard and the requirement come from WorldData; anything
    // already collected in the save is left out.
    public static class Playground
    {
        public static readonly Color Grass = new Color(.48f, .72f, .42f), Stone = new Color(.72f, .7f, .66f), Wood = new Color(.66f, .5f, .34f),
            Accent = new Color(.95f, .7f, .3f), WaterCol = new Color(.2f, .55f, .85f, .55f), Danger = new Color(.85f, .25f, .25f), LowG = new Color(.65f, .45f, 1f, .18f);

        static Transform root;

        public static Transform Build(WorldData w)
        {
            root = new GameObject("World").transform;
            var save = SaveGame.Current; var prog = save.World(w.worldId);
            BuildAreaA(); BuildAreaB();
            // shards from the data
            foreach (var s in w.shards)
                if (!prog.shards.Contains(s.id)) Pickup.Make(Pickup.Kind.Shard, s.id, s.area, s.position, root, new Color(.45f, .92f, 1f));
            var a = w.Area("A");
            var story = Pickup.Make(Pickup.Kind.StoryShard, "story-A", "A", a.storyShard, root, a.storyShardColor);
            story.gameObject.SetActive(prog.necklaceAwake && !prog.storyShard);
            Game.I.StoryShard = story;
            Game.I.StoryBeam = Box("Story beam", new Vector3(a.storyShard.x, 30f, a.storyShard.z), new Vector3(.5f, 60f, .5f), new Color(.5f, .8f, 1f), "Glow", false).gameObject;
            Game.I.StoryBeam.SetActive(prog.necklaceAwake && !prog.storyShard);
            if (!save.heartUpgrades.Contains("H1")) Pickup.Make(Pickup.Kind.HeartUpgrade, "H1", "A", new Vector3(37f, 7.6f, 41f), root, Accent);
            if (!save.Has(Abilities.DoubleJump)) Pickup.Make(Pickup.Kind.Ability, Abilities.DoubleJump, "B", new Vector3(400f, 1.2f, 26f), root, new Color(.93f, .36f, .62f));
            Pickup.HealHeart(new Vector3(-26f, 1f, 16f), root);
            Pickup.HealHeart(new Vector3(20f, 1f, 12f), root);
            return root;
        }

        // ---- builders
        public static Transform Box(string name, Vector3 centre, Vector3 size, Color c, string kind = "Lit", bool collider = true)
        {
            var go = Placeholders.Prim(PrimitiveType.Cube, root, centre, size, Placeholders.Mat(c, kind), collider, name);
            return go.transform;
        }
        // a slab whose top is at `top`: x / z ranges
        static Transform Slab(string name, float x0, float x1, float z0, float z1, float top, float thick, Color c) =>
            Box(name, new Vector3((x0 + x1) / 2, top - thick / 2, (z0 + z1) / 2), new Vector3(x1 - x0, thick, z1 - z0), c);

        static void Ramp(string name, Vector3 bottomCentre, float width, float length, float angle, Color c)
        {
            // a box tilted up along +z, its top surface starting at bottomCentre
            float rad = angle * Mathf.Deg2Rad, t = .6f;
            var dir = new Vector3(0, Mathf.Sin(rad), Mathf.Cos(rad));
            var centre = bottomCentre + dir * length / 2 - new Vector3(0, Mathf.Cos(rad), -Mathf.Sin(rad)) * t / 2;
            var b = Box(name, centre, new Vector3(width, t, length), c);
            b.rotation = Quaternion.Euler(-angle, 0, 0);
        }

        public static void Sign(string text, Vector3 at, float yaw, Color? c = null)
        {
            var go = new GameObject("Sign " + text); go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            Placeholders.Prim(PrimitiveType.Cube, go.transform, new Vector3(0, -.9f, .05f), new Vector3(.12f, 1.8f, .12f), Placeholders.Mat(Wood), true);
            Placeholders.Prim(PrimitiveType.Cube, go.transform, new Vector3(0, 0, .05f), new Vector3(Mathf.Max(1.6f, text.Length * .16f), .7f, .08f), Placeholders.Mat(c ?? new Color(.96f, .92f, .8f)), true);
            var tgo = new GameObject("Text"); tgo.transform.SetParent(go.transform, false); tgo.transform.localPosition = new Vector3(0, 0, -.02f);
            var tm = tgo.AddComponent<TextMesh>(); tm.text = text; tm.font = UiKit.Font; tm.fontSize = 48; tm.characterSize = .05f;
            tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = new Color(.2f, .15f, .1f);
            UiKit.WorldText(tm);   // TextMesh reads correctly from -z: the sign's front
        }

        static void CheckpointAt(string id, Vector3 at, float yaw) => CheckpointHere(id, at, yaw, root);

        // a checkpoint flag (Round 4: any world, or an invisible one: the vortex space)
        public static Checkpoint CheckpointHere(string id, Vector3 at, float yaw, Transform parent, bool visible = true)
        {
            var go = new GameObject("Checkpoint " + id); go.transform.SetParent(parent, false); go.transform.position = at;
            if (visible)
            {
                Placeholders.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0, 1.1f, 0), new Vector3(.12f, 1.1f, .12f), Placeholders.Mat(Stone));
                var flag = Placeholders.Prim(PrimitiveType.Cube, go.transform, new Vector3(.4f, 1.9f, 0), new Vector3(.7f, .45f, .05f), Placeholders.Mat(new Color(.9f, .85f, .3f)), name: "Flag");
                flag.GetComponent<Renderer>().material = new Material(flag.GetComponent<Renderer>().sharedMaterial);
                Placeholders.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0, .03f, 0), new Vector3(1.6f, .03f, 1.6f), Placeholders.Mat(new Color(.9f, .85f, .55f)));
            }
            var c = go.AddComponent<Checkpoint>(); c.id = id; c.yaw = yaw; c.silent = !visible;
            Checkpoint.All[id] = c;
            return c;
        }

        static Portal PortalAt(string id, Vector3 at, float yaw, string to, bool open) => PortalHere(id, at, yaw, to, open, root, new Color(.55f, .4f, 1f));

        // a portal ring (Round 4: any world; vortex = the Ice Age's vortex gate: ice ring, the vortex's swirl in it)
        public static Portal PortalHere(string id, Vector3 at, float yaw, string to, bool open, Transform parent, Color color, bool vortex = false)
        {
            var go = new GameObject("Portal " + id); go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            var stone = vortex ? WorldArt.Mat("Ice") : Placeholders.Mat(new Color(.55f, .52f, .6f));
            for (int i = 0; i < 16; i++)   // a ring of stones, 2 m radius, centre 2.2 m up
            {
                float a = i * Mathf.PI * 2 / 16;
                if (Mathf.Sin(a) < -.8f) continue;   // an arch: no stones across the bottom, walk straight through
                var s = Placeholders.Prim(PrimitiveType.Cube, go.transform, new Vector3(Mathf.Cos(a) * 2f, 2.2f + Mathf.Sin(a) * 2f, 0), new Vector3(.8f, .8f, .6f), stone, true);
                s.transform.localRotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg);
            }
            var disc = Placeholders.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0, 2.2f, 0), new Vector3(3.6f, .02f, 3.6f), vortex ? Vortex.SwirlMaterial() : Placeholders.Mat(color, "Glow"), name: "Disc");
            disc.transform.localRotation = Quaternion.Euler(90, 0, 0);
            if (vortex)
            {
                var l = new GameObject("Vortex light"); l.transform.SetParent(disc.transform.parent, false); l.transform.localPosition = new Vector3(0, 2.2f, -1.5f);
                var lt = l.AddComponent<Light>(); lt.type = LightType.Point; lt.color = color; lt.range = 14f; lt.intensity = 2.5f; l.SetActive(false);
                go.AddComponent<PortalLight>().lightObject = l;
            }
            Placeholders.Prim(PrimitiveType.Cube, go.transform, new Vector3(0, -.1f, 0), new Vector3(5.5f, .3f, 2f), stone, true);
            var p = go.AddComponent<Portal>(); p.id = id; p.toCheckpoint = to; p.startsOpen = open;
            return p;
        }

        static void BuildAreaA()
        {
            // ground: the main slab (x -30..60) with the pool cut out; the west pieces around the pit
            const float T = 5f;
            Slab("Ground main W", -30, 22, -60, 60, 0, T, Grass);
            Slab("Ground main S", 22, 40, -60, -10, 0, T, Grass);
            Slab("Ground main N", 22, 40, 8, 60, 0, T, Grass);
            Slab("Ground main E", 40, 60, -60, 60, 0, T, Grass);
            Slab("Ground NW (enemy yard)", -60, -30, 12, 60, 0, T, new Color(.55f, .68f, .4f));
            Slab("Ground SW (boulder)", -60, -30, -60, -22, 0, T, new Color(.55f, .68f, .4f));
            Slab("Island over the pit", -56, -42, -14, 4, 0, T, new Color(.6f, .75f, .45f));
            Box("Pit floor marker", new Vector3(-42f, -24f, -5f), new Vector3(40f, .5f, 60f), new Color(.25f, .2f, .3f), "Lit", false);
            Sign("Start", new Vector3(3f, 2f, -50f), 0);

            // start + checkpoints
            CheckpointAt("A-start", new Vector3(0, 0, -50f), 0);
            CheckpointAt("A-mid", new Vector3(-3f, 0, -2f), 0);
            CheckpointAt("A-west", new Vector3(-26f, 0, -2f), 270);
            CheckpointAt("A-portal", new Vector3(5f, 0, 50f), 180);

            // slopes: a walkable 20 degree ramp and a 55 degree slope (too steep: you slide) up to a plateau
            Ramp("Ramp 20", new Vector3(10f, 0, -46f), 3f, 8.5f, 20f, Stone);
            Ramp("Slope 55", new Vector3(17f, 0, -40.0f), 3f, 3.6f, 55f, new Color(.62f, .6f, .58f));
            Slab("Plateau", 7, 20, -38.1f, -33f, 2.9f, 2.9f, Stone);
            Sign("Slopes", new Vector3(13.5f, 2f, -48f), 0);

            // steps (shards A04-A06)
            for (int i = 0; i < 3; i++) Slab("Step " + (i + 1), -19.5f - 4 * i, -16.5f - 4 * i, -31.5f, -28.5f, 1f + i, 1f + i, Wood);
            Sign("Steps", new Vector3(-14f, 2f, -34f), 0);

            // pillar (A16 behind it)
            Box("Pillar", new Vector3(12f, 3f, -7f), new Vector3(2f, 6f, 2f), Stone);

            // the double-jump ledge (A17-A19): 3.2 m, out of a single jump's reach
            Slab("High ledge", 14, 24, -20, -16, 3.2f, 3.2f, new Color(.8f, .55f, .55f));
            Sign("Double jump ledge", new Vector3(19f, 2f, -22f), 0, new Color(1f, .85f, .85f));

            // moving platform over the pit to the island (A07)
            var mp = Box("Moving platform", new Vector3(-31.6f, -.25f, -5f), new Vector3(3f, .5f, 3f), Accent);
            var m = mp.gameObject.AddComponent<MovingPlatform>(); m.a = new Vector3(-31.6f, -.25f, -5f); m.b = new Vector3(-40.4f, -.25f, -5f); m.period = 7f;
            Sign("Moving platform", new Vector3(-27.5f, 2f, -9f), 270);

            // the pool: basin 18 x 18, 4 m deep, water to 0.3 m below the edge; a shelf to climb out (A08, A09 on the bottom)
            Slab("Pool floor", 22, 40, -10, 8, -4f, 1f, new Color(.4f, .55f, .62f));
            Slab("Pool shelf", 37.5f, 40, -10, 8, -1.2f, 2.8f, new Color(.45f, .6f, .66f));
            var water = Box("Water", new Vector3(31f, -2.15f, -1f), new Vector3(18f, 3.7f, 18f), WaterCol, "Water", false);
            water.gameObject.layer = 4; water.gameObject.AddComponent<WaterVolume>();
            Sign("Pool: dive!", new Vector3(20f, 2f, -10f), 0);

            // low gravity: a violet zone with the tower (A10) and a taller pillar (heart upgrade H1, Abi only)
            var lg = Box("Low gravity zone", new Vector3(32f, 7f, 36f), new Vector3(16f, 14f, 16f), LowG, "Water", false);
            lg.gameObject.layer = 2; lg.gameObject.AddComponent<LowGravityVolume>();
            Box("Low-g tower", new Vector3(32f, 2.5f, 36f), new Vector3(3f, 5f, 3f), new Color(.6f, .5f, .85f));
            Box("Low-g pillar", new Vector3(37f, 3.3f, 41f), new Vector3(2f, 6.6f, 2f), new Color(.55f, .45f, .8f));
            Slab("Low-g step", 26, 29, 30, 33, 3f, 3f, new Color(.6f, .5f, .85f));
            Sign("Low gravity", new Vector3(27f, 2f, 26f), 0, new Color(.9f, .85f, 1f));

            // Kira's crawl garden: walls 3 m high, one low gap (0.7 m) only Kira fits through (A11, A12)
            var wall = new Color(.7f, .62f, .5f);
            Box("Garden wall S left", new Vector3(-3.3f, 1.5f, 24.5f), new Vector3(5.4f, 3f, 1f), wall);
            Box("Garden wall S right", new Vector3(3.3f, 1.5f, 24.5f), new Vector3(5.4f, 3f, 1f), wall);
            Box("Garden wall S over gap", new Vector3(0, 1.85f, 24.5f), new Vector3(1.2f, 2.3f, 1f), wall);
            Box("Garden wall N", new Vector3(0, 1.5f, 32.5f), new Vector3(13f, 3f, 1f), wall);
            Box("Garden wall W", new Vector3(-6f, 1.5f, 28.5f), new Vector3(1f, 3f, 7f), wall);
            Box("Garden wall E", new Vector3(6f, 1.5f, 28.5f), new Vector3(1f, 3f, 7f), wall);
            Sign("Kira only", new Vector3(2.2f, 2f, 23.4f), 0, new Color(.85f, .85f, .9f));

            // Dan's push block: push it against the ledge, climb up (A13)
            Slab("Push ledge", -18, -10, 42, 46, 2.9f, 2.9f, Stone);
            var blk = Box("Push block", new Vector3(-14f, .8f, 30f), new Vector3(1.6f, 1.6f, 1.6f), new Color(.6f, .45f, .3f));
            var pb = blk.gameObject.AddComponent<PushBlock>(); pb.limits = new Bounds(new Vector3(-14f, 0, 34.6f), new Vector3(6.6f, 10f, 13.4f));
            Sign("Dan: push the block", new Vector3(-9.5f, 2f, 28f), 0);

            // enemy yard and spikes (A14, A15)
            var enemy = MakeEnemy(new Vector3(-36f, 0, 20f), new Vector3(-44f, 0, 20f));
            enemy.name = "Enemy A";
            var sp = Box("Spikes", new Vector3(-36f, .25f, 27f), new Vector3(16f, .5f, 2f), Danger, "Lit", false);
            sp.gameObject.AddComponent<Spikes>();
            for (int i = 0; i < 16; i++) for (int j = 0; j < 2; j++)
                    Placeholders.Prim(PrimitiveType.Cylinder, sp, new Vector3(-.47f + i / 16f, .5f, -.25f + j * .5f), new Vector3(.02f, .5f, .15f), Placeholders.Mat(new Color(.75f, .75f, .8f)));
            Sign("Spikes!", new Vector3(-36f, 2f, 24.5f), 0, new Color(1f, .85f, .8f));

            // the boulder that needs Muscle Man, in front of a cave
            Box("Cave", new Vector3(-46f, 3f, -52f), new Vector3(14f, 6f, 12f), new Color(.5f, .48f, .45f));
            Box("Cave mouth", new Vector3(-46f, 1.6f, -45.95f), new Vector3(4f, 3.2f, .1f), new Color(.08f, .07f, .07f), "Lit", false);
            var boulder = Placeholders.Prim(PrimitiveType.Sphere, root, new Vector3(-46f, 2f, -44f), Vector3.one * 4f, Placeholders.Mat(new Color(.45f, .42f, .4f)), true, "Boulder (needs Muscle Man)");
            boulder.AddComponent<Boulder>();
            Sign("Needs Muscle Man", new Vector3(-41f, 2f, -40f), 0, new Color(1f, .9f, .7f));

            // the story shard's pedestal and the portal to Area B
            Box("Pedestal", new Vector3(0, .25f, 52f), new Vector3(1.6f, .5f, 1.6f), Stone);
            Game.I.PortalA = PortalAt("A-B", new Vector3(0, 0, 57f), 180, "B-portal", false);
        }

        static void BuildAreaB()
        {
            Slab("B ground", 385, 415, -22, 32, 0, 5, new Color(.62f, .55f, .78f));
            CheckpointAt("B-portal", new Vector3(400f, 0, -12f), 0);
            PortalAt("B-A", new Vector3(400f, 0, -18f), 0, "A-portal", true);
            Slab("B step 1", 394.5f, 397.5f, 8.5f, 11.5f, 1.4f, 1.4f, Wood);
            Slab("B step 2", 398.5f, 401.5f, 12.5f, 15.5f, 2.4f, 2.4f, Wood);
            Slab("B step 3", 398.5f, 401.5f, 16.5f, 19.5f, 2.4f, 2.4f, Wood);
            Slab("B high pillar", 406.5f, 409.5f, 12.5f, 15.5f, 3.1f, 3.1f, new Color(.8f, .55f, .55f));
            Box("B arch L", new Vector3(397f, 2f, 26f), new Vector3(1f, 4f, 1f), Stone);
            Box("B arch R", new Vector3(403f, 2f, 26f), new Vector3(1f, 4f, 1f), Stone);
            Box("B arch top", new Vector3(400f, 4.25f, 26f), new Vector3(7f, .5f, 1f), Stone);
            Sign("Area B", new Vector3(403f, 2f, -10f), 0);
            Sign("Abi: double jump", new Vector3(404.5f, 2f, 23f), 0, new Color(1f, .85f, .9f));

            // Round 2, item 7: an ice cave on the east side; only sabertooth Kira's claws break its wall (B06 inside). Walls 5 m and a
            // roof, so no jump gets round it.
            var rock = new Color(.62f, .6f, .66f);
            Box("Ice cave N", new Vector3(412.25f, 2.5f, 1.5f), new Vector3(5.5f, 5f, 1f), rock);
            Box("Ice cave S", new Vector3(412.25f, 2.5f, -7.5f), new Vector3(5.5f, 5f, 1f), rock);
            Box("Ice cave E", new Vector3(414.5f, 2.5f, -3f), new Vector3(1f, 5f, 10f), rock);
            Box("Ice cave roof", new Vector3(412.25f, 5.25f, -3f), new Vector3(5.5f, .5f, 10f), rock);
            if (!SaveGame.Current.World(Game.World.worldId).opened.Contains("B-ice"))
            {
                var ice = Box("Ice wall", new Vector3(409.75f, 2.5f, -3f), new Vector3(.8f, 5f, 8f), new Color(.7f, .9f, 1f, .75f), "Water");
                ice.gameObject.AddComponent<IceWall>().id = "B-ice";
            }
            Sign("Sabertooth Kira: claw the ice", new Vector3(406.5f, 2f, -8.5f), 0, new Color(.85f, .95f, 1f));
        }

        public static Enemy MakeEnemy(Vector3 a, Vector3 b)
        {
            var go = new GameObject("Enemy"); go.transform.SetParent(root, false); go.transform.position = a + Vector3.up * .05f;
            go.layer = Hero.EnemyLayer;
            var cc = go.AddComponent<CharacterController>(); cc.height = 1.1f; cc.radius = .45f; cc.center = new Vector3(0, .55f, 0); cc.stepOffset = .3f;
            var red = Placeholders.Mat(new Color(.85f, .2f, .25f));
            var bodyMat = new Material(red);
            var body = Placeholders.Prim(PrimitiveType.Sphere, go.transform, new Vector3(0, .55f, 0), new Vector3(1f, 1.05f, 1f), bodyMat);
            Placeholders.Prim(PrimitiveType.Sphere, go.transform, new Vector3(-.2f, .75f, .4f), new Vector3(.22f, .28f, .12f), Placeholders.Mat(Color.white));
            Placeholders.Prim(PrimitiveType.Sphere, go.transform, new Vector3(.2f, .75f, .4f), new Vector3(.22f, .28f, .12f), Placeholders.Mat(Color.white));
            Placeholders.Prim(PrimitiveType.Sphere, go.transform, new Vector3(-.2f, .72f, .45f), new Vector3(.1f, .14f, .06f), Placeholders.Mat(Color.black));
            Placeholders.Prim(PrimitiveType.Sphere, go.transform, new Vector3(.2f, .72f, .45f), new Vector3(.1f, .14f, .06f), Placeholders.Mat(Color.black));
            Placeholders.Prim(PrimitiveType.Cube, go.transform, new Vector3(-.28f, .98f, .25f), new Vector3(.3f, .06f, .08f), Placeholders.Mat(Color.black)).transform.localRotation = Quaternion.Euler(0, 0, -20);
            Placeholders.Prim(PrimitiveType.Cube, go.transform, new Vector3(.28f, .98f, .25f), new Vector3(.3f, .06f, .08f), Placeholders.Mat(Color.black)).transform.localRotation = Quaternion.Euler(0, 0, 20);
            _ = body;
            var e = go.AddComponent<Enemy>(); e.a = a; e.b = b;
            return e;
        }
    }
}
