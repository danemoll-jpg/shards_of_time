using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 12: Tomorrowland, world 7, a city of tomorrow at pink-lavender dusk, built in code on flat ground at 0 (Dan's tomorrowland.mp3):
    //   the neon avenue (z 0..150; street x -8..8, sidewalks to +-14 with moving walkways (east +z; west: the malfunctioning one), brick
    //     shopfronts with teal glass x 14..24 / -24..-14, roofs at 7; the escalator to the east roofs at z 84..90 runs down hard until robot
    //     Kira's terminal turns it round; the wall-jump shaft between two west shops (z 62..64.4); Muscle Man's boost onto the west roofs;
    //     the parked car on the maintenance hatch (z 46..52); the vending machine at the west alley (z 116); the chrome bars at the east
    //     nook (z 104); the boost ramp to the billboard over the street (z 124)),
    //   the hover-car lane across at z 154 (a bump, never a lost heart),
    //   the square (z 160..230, x -45..45): the fountain, the garden dome (west), the Atomic Diner dome (east: robot Kira opens its door),
    //     the Violinist (south-east corner of the north side), the air-dash pads (north-west), the Skyline Tower (-70, 200: 46 m, its glass
    //     lift dark until the necklace wakes: the pink shard on its roof), the Ring Tower's sky deck (-40, 120: 16 m, a flying car always
    //     running from the square), the Orbit Tower (60, 200: 24 m, a flying car robot Kira calls),
    //   the Rocket Terminal (z 236..330): the golden domed launch hall (centre (0, 290), radius 30), the silver rocket (0, 296), two gantries,
    //     the empty fuel socket at the rocket's foot: the pink shard there plays RocketLaunch.
    public static class Tomorrowland
    {
        static Transform root;
        public const string Start = "TL-start", VortexCp = "TL-vortex", SquareCp = "TL-square", TerminalCp = "TL-terminal";
        public const string ArrivedFlag = "TL-arrived", LaunchFlag = "TL-launched";
        public static readonly Vector3 ArriveAt = new Vector3(0, 0, 10f);
        public static readonly Vector3 RocketAt = new Vector3(0, 0, 298f), HallAt = new Vector3(0, 0, 290f), SocketAt = new Vector3(0, 0, 286.5f);
        public static readonly Vector3 SkylineAt = new Vector3(-70f, 0, 200f), ViolinistAt = new Vector3(30f, 0, 224f);
        public const float SkylineTop = 46f, RoofY = 7f;
        public static readonly Color Pink = new Color(1f, .35f, .82f);
        public static Portal Gate { get; private set; }
        public static GameObject RocketModel { get; private set; }
        public static GameObject HallDome { get; private set; }   // hidden while RocketLaunch plays (its own dome halves slide apart)
        public static ShardLift Lift { get; private set; }
        public static Conveyor Escalator { get; private set; }
        public static readonly List<MovingPlatform> Cars = new List<MovingPlatform>();

        public static Transform Build(WorldData w)
        {
            root = new GameObject("World Tomorrowland").transform; Cars.Clear();
            var save = SaveGame.Current; var prog = save.World(w.worldId);
            Ground(); Avenue(prog); Lane(); Square(prog); Towers(prog); Terminal(prog);
            foreach (var (c, s) in new[] { (new Vector3(0, 40, -22), new Vector3(280, 100, 4)), (new Vector3(0, 40, 338), new Vector3(280, 100, 4)),
                                           (new Vector3(-132, 40, 158), new Vector3(4, 100, 364)), (new Vector3(132, 40, 158), new Vector3(4, 100, 364)) })
                Box("World boundary", c, s, Color.clear, "Water").GetComponent<Renderer>().enabled = false;
            Physics.SyncTransforms();
            foreach (var s in w.shards)
                if (!prog.shards.Contains(s.id)) Pickup.Make(Pickup.Kind.Shard, s.id, s.area, ShardPos(s.position), root, new Color(1f, .6f, .9f));
            PlayLog.Line("Tomorrowland shards placed: " + string.Join(" ", w.shards.Select(s => $"{s.id}{Fmt(ShardPos(s.position))}")));
            var a = w.Area("TL");
            // the story shard: the neon pink shard on the Skyline Tower's roof (shown when the necklace wakes)
            var story = Pickup.Make(Pickup.Kind.StoryShard, "story-" + w.worldId, a.id, a.storyShard, root, a.storyShardColor);
            Game.I.StoryShard = story;
            Game.I.StoryBeam = Box("Story beam", a.storyShard + new Vector3(0, 30f, 0), new Vector3(.4f, 60f, .4f), Pink, "Glow", false).gameObject;
            bool awake = prog.necklaceAwake && !prog.storyShard;
            story.gameObject.SetActive(awake); Game.I.StoryBeam.SetActive(awake);
            if (!save.heartUpgrades.Contains("TL-H1")) Pickup.Make(Pickup.Kind.HeartUpgrade, "TL-H1", "TL", new Vector3(40f, 1f, 186f), root, Playground.Accent);
            if (!save.heartUpgrades.Contains("TL-H2")) Pickup.Make(Pickup.Kind.HeartUpgrade, "TL-H2", "TL", new Vector3(62f, 25.2f, 204f), root, Playground.Accent);
            foreach (var h in new[] { new Vector3(-4, 0, 30), new Vector3(4, 0, 110), new Vector3(-10, 0, 176), new Vector3(10, 0, 214), new Vector3(-6, 0, 262) })
                Pickup.HealHeart(h + Vector3.up * 1f, root);
            Enemies();
            return root;
        }

        public static Vector3 ShardPos(Vector3 hint) => new Vector3(hint.x, Ground(hint.x, hint.z, hint.y + 1.6f) + 1f, hint.z);
        static string Fmt(Vector3 v) => $"({v.x:0},{v.y:0.0},{v.z:0})";
        const int GroundMask = ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << Hero.EnemyLayer) | (1 << 4) | (1 << 2));
        public static float Ground(float x, float z, float fromY = 60f) =>
            Physics.Raycast(new Vector3(x, fromY, z), Vector3.down, out var hit, 200f, GroundMask, QueryTriggerInteraction.Ignore) ? hit.point.y : 0f;
        public static Vector3 OnGround(float x, float z, float fromY = 60f) => new Vector3(x, Ground(x, z, fromY), z);
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
        static void Ring(string name, Vector3 at, float r, float tube, string mat) => ProcMesh.Object(name, ProcMesh.Torus(r, tube), WorldArt.Mat(mat), root, at);
        static void Hint(Vector3 at, float r, System.Func<string> text)
        {
            var go = new GameObject("Hint"); go.transform.SetParent(root, false); go.transform.position = at;
            var h = go.AddComponent<HintZone>(); h.radius = r; h.text = text;
        }
        static string B(Controls.Act a) => Controls.Btn(a);
        static Checkpoint Cp(string id, Vector3 at, float yaw, bool visible = true) => Playground.CheckpointHere(id, at, yaw, root, visible);
        static Light PointLight(string name, Vector3 at, float range, float intensity, Color c)
        {
            var l = new GameObject(name); l.transform.SetParent(root, false); l.transform.position = at;
            var lt = l.AddComponent<Light>(); lt.type = LightType.Point; lt.range = range; lt.intensity = intensity; lt.color = c; lt.shadows = LightShadows.None; return lt;
        }
        static void Sign(string text, Vector3 at, float yaw, float size, Color c)
        {
            var go = new GameObject("Sign " + text); go.transform.SetParent(root, false); go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            var tm = go.AddComponent<TextMesh>(); tm.text = text; tm.font = UiKit.Font; tm.fontSize = 64; tm.characterSize = size; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = c;
            go.GetComponent<MeshRenderer>().sharedMaterial = UiKit.Font.material;
        }
        // a robot Kira terminal: a glowing pad on the ground and a little screen post
        static GearSocket Terminal(Vector3 at, WindUpMachine m, string what)
        {
            var go = new GameObject("Terminal " + what); go.transform.SetParent(root, false); go.transform.position = at;
            var pad = Placeholders.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0, .03f, 0), new Vector3(1.5f, .03f, 1.5f), WorldArt.Mat("NeonTeal"), false, "Pad");
            Placeholders.Prim(PrimitiveType.Cube, go.transform, new Vector3(0, .7f, .95f), new Vector3(.5f, 1.4f, .2f), WorldArt.Mat("Chrome"), true, "Terminal post");
            Placeholders.Prim(PrimitiveType.Cube, go.transform, new Vector3(0, 1.15f, .84f), new Vector3(.42f, .3f, .02f), WorldArt.Mat("NeonPink"), false, "Screen");
            m.what = what;
            var s = go.AddComponent<GearSocket>(); s.machine = m; s.ability = Abilities.PlugIn; s.doing = "plugs in and takes over"; s.sound = "plug_in"; s.radius = .9f;
            s.hint = "A glowing data terminal with a cat-sized plug... (robot Kira could plug in here)";
            _ = pad; return s;
        }

        // ---- the ground: one floor, the maintenance pit's hole cut out of it (x -7..-1, z 44..52)
        static void Ground()
        {
            Slab("Floor", -130, 130, -20, 44, 0, -1, "TLStreet"); Slab("Floor", -130, 130, 52, 336, 0, -1, "TLStreet");
            Slab("Floor", -130, -7, 44, 52, 0, -1, "TLStreet"); Slab("Floor", -1, 130, 44, 52, 0, -1, "TLStreet");
            // the pit under the hatch: 2.6 m deep, steps up its south side
            Slab("Pit floor", -7, -1, 44, 52, -2.6f, -3.6f, "ChromeDark");
            // (a ramp up to the south rim: 24 deg)
            Slab("Pit wall W", -7.5f, -7, 44, 52, 0, -3.6f, "Chrome"); Slab("Pit wall E", -1, -.5f, 44, 52, 0, -3.6f, "Chrome"); Slab("Pit wall N", -7, -1, 52, 52.5f, 0, -3.6f, "Chrome");
            Slab("Pit wall S", -7, -1, 43.5f, 44, 0, -3.6f, "Chrome");
            PointLight("Pit light", new Vector3(-4, -1f, 48), 6f, 1.2f, new Color(.4f, 1f, .9f));
            // the plaza paving and the avenue's neon centre line
            Slab("Plaza", -46, 46, 158, 232, .02f, 0, "TLPlaza");
            for (int i = 0; i < 15; i++) Box("Neon line", new Vector3(0, .03f, 6 + i * 10), new Vector3(.25f, .02f, 5f), "NeonPink", false);
            Cp(Start, new Vector3(0, 0, 6), 0, false);
            Ramp("Pit ramp", new Vector3(-4, -2.6f, 50.2f), new Vector3(-4, 0, 44.1f), 4.6f);
        }

        // ---- the neon avenue
        static void Avenue(WorldProgress prog)
        {
            Slab("Sidewalk E", 8, 14, 0, 150, .15f, 0, "TLWalk"); Slab("Sidewalk W", -14, -8, 0, 150, .15f, 0, "TLWalk");
            // moving walkways: east carries you north; west is the malfunctioning one (it flips every few seconds)
            var east = Walkway("Walkway E", new Vector3(11.25f, .2f, 80), new Vector3(2.5f, .1f, 110f), Vector3.forward, 2.4f);
            var west = Walkway("Walkway W", new Vector3(-11.25f, .2f, 80), new Vector3(2.5f, .1f, 110f), Vector3.back, 2.4f);
            var mal = west.gameObject.AddComponent<Malfunction>(); mal.walkway = west;
            Hint(new Vector3(-11, 0, 30), 3f, () => "This walkway is on the blink: it keeps flipping direction!");
            _ = east;
            // shopfronts: brick with teal glass and a neon sign, roofs at 7 (east: z 8..84 and 90..150; west with the shaft gap at 62..64.4)
            string[] names = { "ATOMIC TOYS", "ROBO-WASH", "RAY GUN DINER", "STARLIGHT", "JETPACKS", "MOON PIES", "ORBIT RECORDS", "CHROME & CO", "SPACE SHOES", "GALAXY SODA" };
            int n = 0;
            foreach (var (z0, z1) in new[] { (8f, 20f), (22f, 34f), (36f, 48f), (50f, 62f), (64f, 76f), (78f, 84f), (90f, 102f), (107f, 118f), (120f, 134f), (136f, 150f) })
                Shop(14, 24, z0, z1, names[n++ % names.Length], true);
            n = 3;
            foreach (var (z0, z1) in new[] { (8f, 20f), (22f, 34f), (36f, 48f), (50f, 62f), (64.4f, 76f), (78f, 90f), (92f, 104f), (106f, 116f), (119.5f, 134f), (136f, 150f) })
                Shop(-24, -14, z0, z1, names[n++ % names.Length], false, z0 == 50f || z0 == 64.4f ? 12f : RoofY);   // the two tall ones either side of the wall-jump shaft
            // lamp posts with glowing globes
            for (int i = 0; i < 12; i++) foreach (float x in new[] { -8.6f, 8.6f }) { Cyl("Lamp post", new Vector3(x, .15f, 6 + i * 12), .09f, 4.2f, "Chrome", false); var g = Placeholders.Prim(PrimitiveType.Sphere, root, new Vector3(x, 4.5f, 6 + i * 12), Vector3.one * .55f, WorldArt.Mat(i % 2 == 0 ? "NeonTeal" : "NeonPink"), false, "Lamp globe"); _ = g; }
            for (int i = 0; i < 6; i++) PointLight("Avenue light", new Vector3(i % 2 == 0 ? -6 : 6, 5f, 14 + i * 24), 18f, 1.4f, i % 2 == 0 ? new Color(1f, .45f, .85f) : new Color(.35f, 1f, .95f));
            Cp("TL-avenue", new Vector3(0, 0, 90), 0, true);
            // the east escalator (z 84..90) up to the roofs: it runs down hard (you can't climb it) until robot Kira turns it round
            var ramp = new GameObject("Escalator"); ramp.transform.SetParent(root, false);
            float run = 9.6f, len = Mathf.Sqrt(run * run + RoofY * RoofY), ang = Mathf.Atan2(RoofY, run) * Mathf.Rad2Deg;
            ramp.transform.SetPositionAndRotation(new Vector3(14.2f + run / 2, RoofY / 2 - .15f, 87f), Quaternion.Euler(0, 0, ang));
            Placeholders.Prim(PrimitiveType.Cube, ramp.transform, Vector3.zero, new Vector3(len, .3f, 5.6f), WorldArt.Mat("Walkway"), true, "Escalator deck");
            Placeholders.Prim(PrimitiveType.Cube, ramp.transform, new Vector3(0, .17f, 0), new Vector3(len, .02f, 1.2f), WorldArt.Mat("NeonTeal"), false, "Escalator stripe");
            var esc = new GameObject("Escalator belt"); esc.transform.SetParent(root, false); esc.transform.position = new Vector3(19.0f, RoofY / 2 + .4f, 87f); esc.transform.localScale = new Vector3(9.8f, RoofY + 1.2f, 5.6f);
            Escalator = esc.AddComponent<Conveyor>(); Escalator.speed = 9f;
            var down = new Vector3(-Mathf.Cos(ang * Mathf.Deg2Rad), -Mathf.Sin(ang * Mathf.Deg2Rad), 0); Escalator.dir = down;
            var flip = new GameObject("Escalator control").AddComponent<FlipWalkway>(); flip.transform.SetParent(root, false); flip.id = "TL-escalator"; flip.walkway = Escalator; flip.flipped = -down;
            Slab("Roof landing", 23.6f, 26.5f, 82.8f, 91.2f, RoofY, RoofY - .4f, "Chrome");   // the escalator's top: a deck joining the roofs either side
            var t = Terminal(new Vector3(12.8f, .15f, 92.5f), flip, "the escalator");
            Hint(new Vector3(12, 0, 86), 3f, () => SaveGame.Current.World(Game.World.worldId).opened.Contains("TL-escalator") ? "The escalator runs up to the rooftops now." : "The escalator's running down full speed... the terminal beside it might turn it round (robot Kira).");
            _ = t;
            // (once flipped it runs up at a gentle 2.6 m/s)
            var fixSpeed = esc.AddComponent<EscalatorSpeed>(); fixSpeed.flip = flip;
            foreach (var (z0, z1) in new[] { (20f, 22f), (34f, 36f), (48f, 50f), (62f, 64f), (76f, 78f), (102f, 107f), (118f, 120f), (134f, 136f) }) Slab("Roof bridge", 14, 24, z0, z1, RoofY, RoofY - .3f, "Chrome");
            foreach (var (z0, z1) in new[] { (20f, 22f), (34f, 36f), (48f, 50f), (76f, 78f), (90f, 92f), (104f, 106f), (116f, 119.5f), (134f, 136f) }) Slab("Roof bridge", -24, -14, z0, z1, RoofY, RoofY - .3f, "Chrome");
            Slab("Courtyard roof", -36.6f, -24, 104, 132, RoofY, RoofY - .3f, "ShopCream"); Slab("Nook roof", 24, 30, 102, 107, 6.3f, 6f, "ShopBrick");
            // the wall-jump shaft between two west shops (z 62..64.4, 7 m): a shard on the roof above
            Hint(new Vector3(-12.5f, 0, 63.2f), 2.6f, () => SaveGame.Current.Has(Abilities.WallJump) ? "A narrow gap between the shops: Abi can wall-jump up it." : "A narrow gap between two shops, up to the roofs...");
            // Muscle Man's boost onto the west roofs (z 100)
            var boost = new GameObject("Boost spot (west roofs)"); boost.transform.SetParent(root, false); boost.transform.SetPositionAndRotation(new Vector3(-11.6f, .15f, 98f), Quaternion.Euler(0, -90, 0));
            var bs = boost.AddComponent<BoostSpot>(); bs.radius = 1.6f; bs.height = 9.5f; bs.landAt = new Vector3(-19f, RoofY + .05f, 98f);
            Box("Boost mark", new Vector3(-11.6f, .17f, 98f), new Vector3(1.4f, .02f, 1.4f), "NeonYellow", false);
            // the parked flying car on the maintenance hatch (Muscle Man lifts it off; the pit below has shards and steps out)
            Box("Hatch", new Vector3(-4, .02f, 48), new Vector3(6.2f, .04f, 8.2f), "NeonYellow", false);
            var carJob = new GameObject("Parked car (Muscle Man)"); carJob.transform.SetParent(root, false); carJob.transform.position = new Vector3(-4, 0, 48);
            var pc = CarModel(carJob.transform, new Vector3(-4, 0, 48), 0, new Color(.9f, .88f, 1f));
            var lid = new GameObject("Hatch lid"); lid.transform.SetParent(carJob.transform, false); lid.transform.position = new Vector3(-4, -.1f, 48); lid.AddComponent<BoxCollider>().size = new Vector3(6f, .2f, 8f);
            var blk = new GameObject("Car block"); blk.transform.SetParent(carJob.transform, false); blk.transform.position = new Vector3(-4, .7f, 48); blk.AddComponent<BoxCollider>().size = new Vector3(2.4f, 1.4f, 3.4f);
            var cb = carJob.AddComponent<Boulder>(); cb.id = "TL-car-hatch"; cb.lifts = true; cb.aside = new Vector3(4.5f, 0, 0); cb.verbText = "lifts the parked car off the hatch";
            _ = pc;
            // the vending machine at the west alley (z 116..119.5): Muscle Man pushes it aside; the alley leads to a little courtyard
            Slab("Courtyard wall", -36, -24, 104, 104.6f, 5, 0, "ShopCream"); Slab("Courtyard wall", -36, -24, 131.4f, 132, 5, 0, "ShopCream"); Slab("Courtyard wall", -36.6f, -36, 104, 132, 5, 0, "ShopCream");
            var vend = new GameObject("Vending machine (Muscle Man)"); vend.transform.SetParent(root, false); vend.transform.position = new Vector3(-15.2f, 0, 117.75f);
            Placeholders.Prim(PrimitiveType.Cube, vend.transform, new Vector3(0, 1.2f, 0), new Vector3(1.2f, 2.4f, 3.3f), WorldArt.Mat("NeonPink"), false, "Machine");
            Placeholders.Prim(PrimitiveType.Cube, vend.transform, new Vector3(.62f, 1.4f, 0), new Vector3(.05f, 1.4f, 2.4f), WorldArt.Mat("TealWindow"), false, "Window");
            var vb = new GameObject("Block"); vb.transform.SetParent(vend.transform, false); vb.transform.localPosition = new Vector3(0, 1.2f, 0); vb.AddComponent<BoxCollider>().size = new Vector3(1.2f, 2.4f, 3.3f);
            var vj = vend.AddComponent<Boulder>(); vj.id = "TL-vending"; vj.aside = new Vector3(3.2f, 0, -.2f); vj.verbText = "pushes the vending machine aside";
            // the chrome bars at the east nook (z 102..107): Muscle Man bends them
            Slab("Nook back", 24, 30, 102, 102.4f, 6, 0, "ShopBrick"); Slab("Nook back", 24, 30, 106.6f, 107, 6, 0, "ShopBrick"); Slab("Nook end", 29.6f, 30, 102, 107, 6, 0, "ShopBrick");
            var bars = new GameObject("Chrome bars (Muscle Man)"); bars.transform.SetParent(root, false); bars.transform.SetPositionAndRotation(new Vector3(14.6f, 0, 104.5f), Quaternion.Euler(0, 90, 0));
            var straight = new GameObject("Straight"); straight.transform.SetParent(bars.transform, false);
            var bent = new GameObject("Bent"); bent.transform.SetParent(bars.transform, false);
            for (int i = 0; i < 7; i++)
            {
                float x = -1.95f + i * .65f;
                var s = Placeholders.Prim(PrimitiveType.Cylinder, straight.transform, new Vector3(x, 1.6f, 0), new Vector3(.12f, 1.6f, .12f), WorldArt.Mat("Chrome"), false, "Bar");
                var b = Placeholders.Prim(PrimitiveType.Cylinder, bent.transform, new Vector3(x * (i == 3 ? 1f : 1.8f) + (i < 3 ? -.6f : i > 3 ? .6f : 0), 1.6f, 0), new Vector3(.12f, 1.6f, .12f), WorldArt.Mat("Chrome"), false, "Bar"); b.transform.localRotation = Quaternion.Euler(0, 0, (i - 3) * 9f);
                _ = s;
            }
            var bc = new GameObject("Block"); bc.transform.SetParent(straight.transform, false); bc.transform.localPosition = new Vector3(0, 1.6f, 0); bc.AddComponent<BoxCollider>().size = new Vector3(4.6f, 3.2f, .3f);
            Slab("Bars frame", 14.4f, 14.8f, 101.8f, 102.2f, 3.4f, 0, "Chrome"); Slab("Bars frame", 14.4f, 14.8f, 106.8f, 107.2f, 3.4f, 0, "Chrome");
            var bj = bars.AddComponent<BendJob>(); bj.id = "TL-bars-nook"; bj.straight = straight; bj.bent = bent; bj.verbText = "bends the chrome bars";
            // robot Kira's boost ramp: a wheel dash onto it launches her onto the billboard over the street (z 128, 8.5 m)
            var rampGo = new GameObject("Boost ramp"); rampGo.transform.SetParent(root, false); rampGo.transform.SetPositionAndRotation(new Vector3(0, .5f, 116f), Quaternion.Euler(-14, 0, 0));
            Placeholders.Prim(PrimitiveType.Cube, rampGo.transform, Vector3.zero, new Vector3(3f, .3f, 4.2f), WorldArt.Mat("Chrome"), true, "Ramp");
            for (int i = 0; i < 3; i++) Placeholders.Prim(PrimitiveType.Cube, rampGo.transform, new Vector3(0, .17f, -1.4f + i * 1.2f), new Vector3(1.6f, .02f, .25f), WorldArt.Mat("NeonYellow"), false, "Chevron");
            var vol = new GameObject("Boost ramp volume"); vol.transform.SetParent(root, false); vol.transform.position = new Vector3(0, 1.2f, 117.6f); vol.transform.localScale = new Vector3(3f, 1.6f, 1.2f);
            var br = vol.AddComponent<BoostRamp>(); br.height = 8.6f;
            Slab("Billboard deck", -3.5f, 3.5f, 126, 131, 8.5f, 8.1f, "Chrome");
            foreach (float x in new[] { -3.2f, 3.2f }) Cyl("Billboard post", new Vector3(x, 0, 130.5f), .2f, 8.1f, "ChromeDark");
            Box("Billboard", new Vector3(0, 11.5f, 130.8f), new Vector3(7f, 5f, .3f), "NeonLilac"); Sign("VISIT THE MOON!", new Vector3(0, 11.5f, 130.6f), 0, .12f, Color.white);
            Hint(new Vector3(0, 0, 110), 4f, () => $"A boost ramp! Robot Kira rolling fast on her wheels ({B(Controls.Act.Dive)} on the ground) would fly up to the billboard.");
            // the wall-jump shaft's walls are the shop sides; a shard sits on the roof above it
        }
        static Conveyor Walkway(string name, Vector3 centre, Vector3 size, Vector3 dir, float speed)
        {
            var vis = Placeholders.Prim(PrimitiveType.Cube, root, centre, size, WorldArt.Mat("Walkway"), true, name);
            var lk = vis.AddComponent<WalkwayLook>(); lk.dir = dir; lk.speed = speed;
            for (int i = 0; i < (int)(size.z / 4); i++) Placeholders.Prim(PrimitiveType.Cube, root, new Vector3(centre.x, centre.y + .06f, centre.z - size.z / 2 + 2 + i * 4), new Vector3(size.x * .8f, .02f, .2f), WorldArt.Mat("NeonTeal"), false, name + " stripe");
            var b = new GameObject(name + " belt"); b.transform.SetParent(root, false); b.transform.position = centre + Vector3.up * .5f; b.transform.localScale = new Vector3(size.x, 1.2f, size.z);
            var c = b.AddComponent<Conveyor>(); c.dir = dir; c.speed = speed; return c;
        }
        static void Shop(float x0, float x1, float z0, float z1, string name, bool east, float top = RoofY)
        {
            float front = east ? x0 : x1, back = east ? x1 : x0, dir = east ? -1 : 1;
            Slab("Shop " + name, Mathf.Min(x0, x1), Mathf.Max(x0, x1), z0, z1, top, 0, (int)z0 % 3 == 0 ? "ShopCream" : "ShopBrick");
            Slab("Roof trim", Mathf.Min(x0, x1) - .1f, Mathf.Max(x0, x1) + .1f, z0 - .1f, z1 + .1f, top + .25f, top, "Chrome");
            Box("Shop window", new Vector3(front + dir * .06f, 2.4f, (z0 + z1) / 2), new Vector3(.12f, 2.6f, (z1 - z0) * .7f), "TealWindow", false);
            Box("Shop sign", new Vector3(front + dir * .12f, 5.2f, (z0 + z1) / 2), new Vector3(.1f, 1.0f, (z1 - z0) * .75f), (int)z0 % 2 == 0 ? "NeonPink" : "NeonTeal", false).gameObject.AddComponent<NeonFlicker>();
            Sign(name, new Vector3(front + dir * .2f, 5.2f, (z0 + z1) / 2), east ? 90 : -90, .055f, Color.white);   // (a TextMesh reads from its -z side)
            _ = back;
        }

        // the flying car model (Dan's Tripo car, canopy cut off: a see-through glass bubble and a chrome rim on top), tinted
        static GameObject CarModel(Transform parent, Vector3 at, float yaw, Color tint)
        {
            var g = new GameObject("Flying car"); g.transform.SetParent(parent, false); g.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            var m = StaticModel.Spawn("flying_car", g.transform, at, yaw, 1.42f);
            if (m) { foreach (var r in m.GetComponentsInChildren<Renderer>()) { var mm = new Material(r.sharedMaterial); mm.color = tint; if (mm.HasProperty("_BaseColor")) mm.SetColor("_BaseColor", tint); r.sharedMaterial = mm; } }
            else Placeholders.Prim(PrimitiveType.Capsule, g.transform, new Vector3(0, .6f, 0), new Vector3(1.6f, 1.6f, .9f), Placeholders.Mat(tint), false, "Car body").transform.localRotation = Quaternion.Euler(90, 0, 0);
            var dome = ProcMesh.Object("Glass canopy", ProcMesh.Dome(.9f, .62f, 28, 8), Placeholders.Mat(new Color(.75f, .92f, 1f, .28f), "Water"), g.transform, Vector3.zero);
            dome.transform.localPosition = new Vector3(0, 1.08f, .1f); dome.transform.localRotation = Quaternion.identity;
            var rim = ProcMesh.Object("Canopy rim", ProcMesh.Torus(.9f, .045f, 32, 8), WorldArt.Mat("Chrome"), g.transform, Vector3.zero); rim.transform.localPosition = new Vector3(0, 1.08f, .1f); rim.transform.localRotation = Quaternion.identity;
            var arch = ProcMesh.Object("Canopy arch", ProcMesh.Torus(.9f, .04f, 32, 8), WorldArt.Mat("Chrome"), g.transform, Vector3.zero); arch.transform.localPosition = new Vector3(0, 1.08f, .1f); arch.transform.localRotation = Quaternion.Euler(0, 0, 90); arch.transform.localScale = new Vector3(.69f, 1f, 1f);
            return g;
        }

        // a flying car that runs a route (a moving platform you ride: its seat deck is solid; the bubble is not)
        static MovingPlatform RouteCar(string name, Vector3[] path, float speed, Color tint, float start = 0)
        {
            var go = new GameObject(name); go.transform.SetParent(root, false); go.transform.position = path[0];
            CarModel(go.transform, path[0], 0, tint);
            var deck = new GameObject("Seat deck"); deck.transform.SetParent(go.transform, false); deck.transform.localPosition = new Vector3(0, .55f, 0); deck.AddComponent<BoxCollider>().size = new Vector3(2.2f, .3f, 3.4f);
            foreach (float x in new[] { -1.15f, 1.15f }) { var s = new GameObject("Side"); s.transform.SetParent(go.transform, false); s.transform.localPosition = new Vector3(x, .95f, 0); s.AddComponent<BoxCollider>().size = new Vector3(.12f, .5f, 3.2f); }
            var mp = go.AddComponent<MovingPlatform>(); mp.path = path; mp.speed = speed; mp.turnDegPerSec = 60f; mp.startDistance = start;
            Cars.Add(mp); return mp;
        }

        // ---- the hover-car lane across the street at z 154
        static void Lane()
        {
            Box("Lane", new Vector3(0, .03f, 154), new Vector3(110f, .02f, 3.2f), "NeonYellow", false);
            foreach (float z in new[] { 152.3f, 155.7f }) for (int i = 0; i < 22; i++) Box("Lane dash", new Vector3(-52 + i * 5, .04f, z), new Vector3(2.2f, .02f, .15f), "NeonPink", false);
            var go = new GameObject("Hover-car lane"); go.transform.SetParent(root, false); go.transform.position = new Vector3(0, 0, 154);
            var lane = go.AddComponent<HoverLane>(); lane.from = new Vector3(-55, 0, 154); lane.to = new Vector3(55, 0, 154); lane.speed = 10f;
            var tints = new[] { new Color(1f, .92f, .96f), new Color(.82f, 1f, .98f), new Color(1f, .98f, .8f) };   // light tints over the car's own pink and cream
            for (int i = 0; i < 3; i++) lane.cars.Add(CarModel(go.transform, go.transform.position, 0, tints[i]).transform);
            Hint(new Vector3(0, 0, 148), 5f, () => "Hover-car lane! Cross between the cars (they only bump you aside).");
        }

        // ---- the square
        static void Square(WorldProgress prog)
        {
            Cp(SquareCp, new Vector3(0, 0, 166), 0, true);
            // the fountain: a chrome basin with a glowing jet ring (no water to fall in)
            Cyl("Fountain basin", new Vector3(0, 0, 195), 5f, .7f, "Chrome");
            Cyl("Fountain pool", new Vector3(0, .7f, 195), 4.6f, .02f, "NeonTeal", false);
            Cyl("Fountain column", new Vector3(0, .7f, 195), .6f, 3.5f, "Chrome");
            Ring("Fountain ring", new Vector3(0, 4.4f, 195), 1.6f, .15f, "NeonPink");
            PointLight("Fountain light", new Vector3(0, 3f, 195), 16f, 1.6f, new Color(.4f, 1f, .95f));
            // the garden dome (west): a see-through bubble over a little garden (walk in anywhere at its open base)
            var gd = ProcMesh.Object("Garden dome", ProcMesh.Dome(9f, 7f), Placeholders.Mat(new Color(.7f, .92f, 1f, .2f), "Water"), root, new Vector3(-32, 2.2f, 186));
            Ring("Garden dome rim", new Vector3(-32, 2.2f, 186), 9f, .12f, "Chrome");
            for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4; if (i == 1) continue; Cyl("Garden dome strut", new Vector3(-32 + Mathf.Cos(a) * 9f, 0, 186 + Mathf.Sin(a) * 9f), .12f, 2.2f, "Chrome"); }
            for (int i = 0; i < 6; i++) { float a = i * 1.1f; var p = new Vector3(-32 + Mathf.Cos(a) * 4.5f, 0, 186 + Mathf.Sin(a) * 4.5f); Cyl("Garden bush", p, .9f, .9f, "GardenGreen", false); Placeholders.Prim(PrimitiveType.Sphere, root, p + Vector3.up * 1.3f, Vector3.one * 1.2f, WorldArt.Mat(i % 2 == 0 ? "NeonPink" : "NeonYellow"), false, "Garden flower"); }
            _ = gd;
            // the Atomic Diner (east): a round wall with a sliding door (robot Kira's terminal), a glass dome over it
            Vector3 dc = new Vector3(36, 0, 186); float dr = 8f;
            for (int i = 0; i < 24; i++)
            {
                float a = i * Mathf.PI * 2 / 24; var p = dc + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * dr;
                if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, 180f)) < 12f) continue;   // the door, facing west
                var w = Box("Diner wall", p + Vector3.up * 1.6f, new Vector3(.4f, 3.2f, 2.3f), i % 2 == 0 ? "ShopCream" : "NeonTeal"); w.rotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0);
            }
            ProcMesh.Object("Diner dome", ProcMesh.Dome(dr + .2f, 5.5f), Placeholders.Mat(new Color(.8f, .9f, 1f, .2f), "Water"), root, dc + Vector3.up * 3.2f, true);   // solid: no way in over the wall
            Ring("Diner rim", dc + Vector3.up * 3.2f, dr + .2f, .15f, "NeonPink");
            Sign("ATOMIC DINER", dc + new Vector3(-dr - .4f, 4.4f, 0), 90, .1f, new Color(1f, .5f, .85f));
            var door = Box("Diner door", dc + new Vector3(-dr, 1.6f, 0), new Vector3(.3f, 3.2f, 2.8f), "Chrome").gameObject;
            var sd = door.AddComponent<SlideDoor>(); sd.id = "TL-door-diner"; sd.lift = 3.3f;
            Terminal(dc + new Vector3(-dr - 2.4f, 0, -2.2f), sd, "the diner's door");
            Cyl("Diner counter", dc + new Vector3(2.5f, 0, 0), 1.6f, 1.1f, "NeonPink");
            for (int i = 0; i < 4; i++) Cyl("Diner stool", dc + new Vector3(0f, 0, -3 + i * 2f), .35f, .7f, "Chrome");
            PointLight("Diner light", dc + Vector3.up * 3f, 12f, 1.6f, new Color(1f, .6f, .85f));
            // the friendly switch for the cleanbots (by the lane)
            var fs = new GameObject("Cleanbot network").AddComponent<FriendlySwitch>(); fs.transform.SetParent(root, false);
            var ft = Terminal(new Vector3(6, .02f, 166), fs, "the cleanbot network"); ft.cooldown = 4f;
            // the air-dash pads (north-west): a 4 m platform by the steps, the far pad 12.5 m off (too far for a double jump)
            Ramp("Pad ramp", new Vector3(-24f, 0, 212), new Vector3(-33.2f, 3.95f, 212), 3.6f);
            Slab("Dash pad A", -38, -33, 210, 214, 4f, 3.6f, "Chrome"); Cyl("Pad A glow", new Vector3(-35.5f, 4f, 212), 1.4f, .02f, "NeonTeal", false);
            Slab("Dash pad B", -53, -50.5f, 210.5f, 213.5f, 4f, 3.6f, "Chrome"); Cyl("Pad B glow", new Vector3(-51.75f, 4f, 212), 1.0f, .02f, "NeonPink", false);
            Cyl("Pad B post", new Vector3(-51.75f, 0, 212), .25f, 3.6f, "ChromeDark");
            Hint(new Vector3(-36, 4f, 212), 3f, () => SaveGame.Current.Has(Abilities.AirDash) ? $"The far pad: jump, double jump, then Abi's air dash ({B(Controls.Act.Dive)} in the air)." : "That far pad is out of reach... even with a double jump.");
            // the Violinist (teal jacket, the chrome electric violin): teaches Abi the air dash
            var v = Violinist.Make(root, ViolinistAt, -135f, "ChromeViolin", Game.World.violinistPlayClip);
            v.ability = Abilities.AirDash; v.learner = "Abi"; v.moveName = "Air Dash";
            Hint(ViolinistAt + new Vector3(-2.5f, 0, -2.5f), 4f, () => SaveGame.Current.Has(Abilities.AirDash) ? $"The air dash: in the air, {B(Controls.Act.Dive)} = one quick dash the way you point (Abi). Long gaps everywhere wait for it." : "A lounge musician with a gleaming chrome violin, playing to the evening crowd...");
            // the greeter robot at the start of the avenue: "Your rocket is ready for departure!" (points to the Terminal)
            Greeter(new Vector3(5f, 0, 24f), "Your rocket is ready for departure!", "greeter_ready", true);
            Sign("ROCKET TERMINAL  >>>", new Vector3(9.5f, 3.4f, 28f), 0, .07f, new Color(1f, .55f, .9f));
            Box("Arrow sign", new Vector3(9.5f, 3.4f, 28.12f), new Vector3(4.2f, .8f, .1f), "ChromeDark", false);
            foreach (var (x, z) in new[] { (-26f, 166f), (26f, 166f), (-40f, 226f), (40f, 226f) }) { Cyl("Plaza lamp", new Vector3(x, 0, z), .1f, 5f, "Chrome", false); Placeholders.Prim(PrimitiveType.Sphere, root, new Vector3(x, 5.3f, z), Vector3.one * .7f, WorldArt.Mat("NeonLilac"), false, "Plaza globe"); }
        }

        static Greeter Greeter(Vector3 at, string line, string sound, bool hug)
        {
            var go = new GameObject("Greeter robot"); go.transform.SetParent(root, false); go.transform.position = at;
            var m = StaticModel.Spawn("robot_greeter", go.transform, at, 180, 1.35f);
            if (m) m.AddComponent<RigidParts>();
            else
            {
                Placeholders.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0, .7f, 0), new Vector3(.6f, .4f, .6f), WorldArt.Mat("Chrome"), false, "Body");
                Placeholders.Prim(PrimitiveType.Sphere, go.transform, new Vector3(0, 1.2f, 0), Vector3.one * .45f, Placeholders.Mat(new Color(.7f, .9f, 1f, .4f), "Water"), false, "Dome");
            }
            var col = new GameObject("Body"); col.transform.SetParent(go.transform, false); col.transform.localPosition = new Vector3(0, .65f, 0); col.AddComponent<CapsuleCollider>().radius = .35f;
            var g = go.AddComponent<Greeter>(); g.line = line; g.sound = sound; g.hugGag = hug; g.pointAt = RocketAt;
            return g;
        }

        // ---- the towers: the Ring Tower's sky deck (a car always running), the Orbit Tower (robot Kira calls a car), the Skyline Tower (the lift)
        static void Towers(WorldProgress prog)
        {
            // background towers for the skyline
            foreach (var (x, z, h) in new[] { (-60f, 40f, 34f), (60f, 60f, 40f), (-80f, 100f, 30f), (85f, 130f, 38f), (-95f, 260f, 44f), (95f, 270f, 36f), (55f, 20f, 26f), (-45f, 80f, 28f) })
            {
                Cyl("Tower", new Vector3(x, 0, z), 5f, h, "Chrome"); Ring("Tower ring", new Vector3(x, h - 2f, z), 7.5f, .5f, (int)h % 2 == 0 ? "NeonTeal" : "NeonPink");
                Placeholders.Prim(PrimitiveType.Sphere, root, new Vector3(x, h, z), new Vector3(10f, 4f, 10f), WorldArt.Mat("ChromeDark"), false, "Tower cap");
            }
            // the Ring Tower (-40, 0, 120): the sky deck at 16; the always-running car from the square's west pad
            Cyl("Ring Tower", new Vector3(-40, 0, 120), 4f, 16f, "Chrome");
            Cyl("Sky deck", new Vector3(-40, 15.6f, 120), 8f, .4f, "ChromeDark");
            Ring("Sky deck ring", new Vector3(-40, 17.6f, 120), 8.2f, .3f, "NeonPink"); Ring("Ring Tower ring", new Vector3(-40, 22f, 120), 6f, .5f, "NeonTeal");
            Cyl("Sky deck rail post", new Vector3(-40, 16f, 120), .1f, .1f, "Chrome", false);
            Box("Car pad (square)", new Vector3(-24, .04f, 168), new Vector3(4f, .05f, 5f), "NeonYellow", false);
            RouteCar("Sky car", new[] { new Vector3(-24, .3f, 168), new Vector3(-24, .3f, 162), new Vector3(-30, 9f, 140), new Vector3(-31.4f, 16.05f, 124), new Vector3(-31.4f, 16.05f, 116), new Vector3(-30, 9f, 140) }, 3.6f, new Color(1f, .93f, .97f));
            Hint(new Vector3(-24, 0, 170), 3f, () => "A flying car on its route: hop in for a ride up to the Ring Tower's sky deck.");
            // the Orbit Tower (60, 0, 200): roof at 24; the car robot Kira calls from the pad at (40, 172)
            Cyl("Orbit Tower", new Vector3(60, 0, 200), 5f, 23.6f, "Chrome");
            Cyl("Orbit roof", new Vector3(60, 23.6f, 200), 8f, .4f, "ChromeDark"); Ring("Orbit ring", new Vector3(60, 26f, 200), 9f, .45f, "NeonLilac");
            Box("Car pad (Orbit)", new Vector3(44, .04f, 172), new Vector3(4f, .05f, 5f), "NeonYellow", false);
            var orbit = RouteCar("Orbit car", new[] { new Vector3(44, .3f, 172), new Vector3(44, .3f, 166), new Vector3(46, 12f, 180), new Vector3(51f, 24.05f, 196), new Vector3(51f, 24.05f, 204), new Vector3(46, 12f, 186) }, 0f, new Color(.86f, 1f, .97f));
            var call = new GameObject("Orbit car call").AddComponent<CallCar>(); call.transform.SetParent(root, false); call.id = "TL-call-orbit"; call.car = orbit; call.speed = 3.6f;
            Terminal(new Vector3(40, .02f, 170), call, "the Orbit car");
            // the Skyline Tower (-70, 0, 200): 46 m; its glass lift (east side) dark until the necklace wakes; the pink shard on its roof
            Cyl("Skyline Tower", SkylineAt, 6f, SkylineTop - .4f, "Chrome");
            Cyl("Skyline roof", SkylineAt + Vector3.up * (SkylineTop - .4f), 7.2f, .4f, "ChromeDark");
            Ring("Skyline ring", SkylineAt + Vector3.up * (SkylineTop + 9f), 10f, .6f, "NeonPink");   // (high over the roof: the camera up there stays clear of it) Ring("Skyline ring low", SkylineAt + Vector3.up * 30f, 8f, .4f, "NeonTeal");
            for (int i = 0; i < 6; i++) Ring("Skyline band", SkylineAt + Vector3.up * (6 + i * 6), 6.05f, .12f, i % 2 == 0 ? "NeonPink" : "NeonLilac");
            Placeholders.Prim(PrimitiveType.Cylinder, root, new Vector3(-61.6f, SkylineTop / 2 + 1f, 200), new Vector3(3.4f, SkylineTop / 2 + 1f, 3.4f), Placeholders.Mat(new Color(.75f, .92f, 1f, .13f), "Water"), false, "Lift tube");
            var lift = Box("Skyline lift", new Vector3(-61.6f, .15f, 200), new Vector3(2.6f, .3f, 2.6f), "Chrome").gameObject;
            var lightG = Box("Lift light", new Vector3(-61.6f, .32f, 200), new Vector3(2.2f, .04f, 2.2f), "NeonPink", false).gameObject; lightG.transform.SetParent(lift.transform, true);
            lift.AddComponent<MovingPlatform>();
            Lift = lift.AddComponent<ShardLift>(); Lift.bottom = new Vector3(-61.6f, .15f, 200); Lift.top = new Vector3(-61.6f, SkylineTop - .15f, 200); Lift.light = lightG;
            Hint(new Vector3(-60, 0, 200), 3f, () => SaveGame.Current.World(Game.World.worldId).necklaceAwake ? "The Skyline Tower's lift is lit: up to the roof!" : "The Skyline Tower's glass lift is dark... (it waits for something)");
            // the wall-jump test of the old: a hint at the shaft lives in Avenue()
        }

        // ---- the Rocket Terminal and its golden launch hall
        static void Terminal(WorldProgress prog)
        {
            Cp(TerminalCp, new Vector3(0, 0, 248), 0, true);
            // the terminal front: a cream building with a chrome arch and the sign
            Slab("Terminal front W", -30, -4, 236, 240, 9, 0, "ShopCream"); Slab("Terminal front E", 4, 30, 236, 240, 9, 0, "ShopCream");
            Slab("Terminal front top", -4, 4, 236, 240, 9, 5, "ShopCream");
            ProcMesh.Object("Terminal arch", ProcMesh.Torus(4f, .3f), WorldArt.Mat("Chrome"), root, new Vector3(0, 5f, 235.8f)).transform.rotation = Quaternion.Euler(90, 0, 0);
            Sign("ROCKET TERMINAL", new Vector3(0, 7.4f, 235.7f), 0, .14f, new Color(1f, .5f, .88f));
            Box("Terminal sign glow", new Vector3(0, 7.4f, 235.85f), new Vector3(16f, 1.8f, .15f), "ChromeDark", false);
            // the passage to the hall (z 240..260): walls with light strips
            Slab("Passage wall W", -6, -4, 240, 261, 6, 0, "ShopCream"); Slab("Passage wall E", 4, 6, 240, 261, 6, 0, "ShopCream"); Slab("Passage roof", -6, 6, 240, 261, 6.4f, 6, "HallGold");
            for (int i = 0; i < 5; i++) foreach (float x in new[] { -3.95f, 3.95f }) Box("Passage strip", new Vector3(x, 3f, 242 + i * 4), new Vector3(.05f, 3f, .25f), "NeonYellow", false);
            // the hall: a ring wall (radius 30, 14 m), the door south, a golden dome; light strips round it
            float R = 30f, H = 14f;
            for (int i = 0; i < 48; i++)
            {
                float a = i * Mathf.PI * 2 / 48; var p = HallAt + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * R;
                if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, 270f)) < 9f) continue;   // the doorway (south)
                var w = Box("Hall wall", p + Vector3.up * H / 2, new Vector3(.8f, H, 4.1f), "HallGold"); w.rotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0);
            }
            Slab("Hall lintel", -6, 6, 259, 261, H, 6, "HallGold");
            HallDome = ProcMesh.Object("Hall dome", ProcMesh.Dome(R + .4f, 20f, 48, 16), WorldArt.Mat("HallGold"), root, HallAt + Vector3.up * H);
            for (int k = 0; k < 4; k++) Ring("Hall light strip", HallAt + Vector3.up * (3f + k * 3.2f), R - .5f, .12f, k % 2 == 0 ? "NeonYellow" : "NeonPink");
            Ring("Dome light ring", HallAt + Vector3.up * (H + 12f), 14f, .25f, "NeonYellow");
            for (int i = 0; i < 4; i++) PointLight("Hall light", HallAt + new Vector3((i % 2 == 0 ? -1 : 1) * 14, 12f, (i < 2 ? -1 : 1) * 10), 30f, 1.6f, new Color(1f, .85f, .6f));
            Slab("Hall floor", -30, 30, 260, 320, .05f, 0, "ChromeDark");
            for (int i = 0; i < 6; i++) Box("Floor line", new Vector3(0, .07f, 264 + i * 4), new Vector3(.3f, .02f, 2f), "NeonPink", false);
            // the rocket (Dan's Tripo rocket: 24 m) on its launch pad; its own mesh is solid
            Cyl("Launch pad", RocketAt, 9f, .4f, "Chrome");
            Ring("Launch pad ring", RocketAt + Vector3.up * .45f, 8.6f, .15f, "NeonTeal");
            RocketModel = StaticModel.Spawn("rocket", root, RocketAt + Vector3.up * .4f, 0, 24f);
            if (RocketModel) foreach (var mf in RocketModel.GetComponentsInChildren<MeshFilter>()) { var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; }
            else { Cyl("Rocket", RocketAt + Vector3.up * .4f, 2.4f, 20f, "Chrome"); RocketModel = root.Find("Rocket")?.gameObject; }
            // the fuel socket at its foot (south): an empty pink ring; the greeter: "Fuel cell missing!"
            Cyl("Fuel socket", SocketAt, .9f, 1.1f, "Chrome");
            Ring("Fuel socket ring", SocketAt + Vector3.up * 1.15f, .55f, .1f, "NeonPink");
            var reach = new GameObject("Fuel socket reach"); reach.transform.SetParent(root, false); reach.transform.position = SocketAt + new Vector3(0, 0, -1.6f); reach.AddComponent<RocketReach>();
            Greeter(new Vector3(5f, .05f, 280f), "Fuel cell missing!", "greeter_fuel", false);
            // the gantries: west by stairs (platforms at 6 and 12), east by a service lift robot Kira switches on (platforms 6, 12, 18)
            Gantry(-13f, false); Gantry(13f, true);
            Gate = Playground.PortalHere("TL-vortex-gate", new Vector3(-16, 0, 250), 90, Vortex.Checkpoint, false, root, Pink, vortex: true);
            Gate.gameObject.SetActive(prog.storyShard && prog.opened.Contains(LaunchFlag));
            Cp(VortexCp, new Vector3(-12, 0, 250), 90, false);
        }
        static void Gantry(float x, bool lift)
        {
            float z0 = 292f, z1 = 300f;
            foreach (float zz in new[] { z0, z1 }) foreach (float xx in new[] { x - 2.5f, x + 2.5f }) Cyl("Gantry leg", new Vector3(xx, 0, zz), .2f, 19f, "ChromeDark");
            foreach (float h in lift ? new[] { 6f, 12f, 18f } : new[] { 6f }) Slab("Gantry deck", x - 2.8f, x + 2.8f, z0 - .3f, z1 + .3f, h, h - .3f, "Chrome");
            if (!lift)
            {
                // a ramp up the outer side (x - 2.8 .. - 6.4) to the 6 m deck
                Ramp("Gantry ramp", new Vector3(x - 4.6f, 0, 274f), new Vector3(x - 4.6f, 5.95f, 292.4f), 3.4f);
                Slab("Gantry landing", x - 6.4f, x - 2.8f, 292, 300, 6f, 5.7f, "Chrome");
            }
            else
            {
                var car = Box("Service lift", new Vector3(x + 4.2f, .15f, 296), new Vector3(2.4f, .3f, 2.4f), "Chrome").gameObject;
                var mp = car.AddComponent<MovingPlatform>(); mp.Hold(car.transform.position);
                var wl = car.AddComponent<WindLift>(); wl.id = "TL-gantry-lift"; wl.bottom = new Vector3(x + 4.2f, .15f, 296); wl.topAt = new Vector3(x + 4.2f, 17.85f, 296); wl.period = 16f; wl.what = "the gantry's service lift";
                Terminal(new Vector3(x + 4.2f, .05f, 290.5f), wl, "the gantry's service lift");
                Slab("Gantry ledge", x + 2.8f, x + 3.0f, 292, 300, 18f, 17.7f, "Chrome");
            }
        }

        // a walkable ramp (a turned box) from one point to another (both on the ramp's top surface)
        static void Ramp(string name, Vector3 from, Vector3 to, float width)
        {
            var d = to - from; float len = d.magnitude;
            var go = Placeholders.Prim(PrimitiveType.Cube, root, (from + to) / 2 - Vector3.up * .15f, new Vector3(width, .3f, len), WorldArt.Mat("Chrome"), true, name);
            go.transform.rotation = Quaternion.LookRotation(d, Vector3.up);
            Placeholders.Prim(PrimitiveType.Cube, go.transform, new Vector3(0, .51f, 0), new Vector3(.3f, .02f, 1f), WorldArt.Mat("NeonTeal"), false, name + " stripe");
        }

        static void Enemies()
        {
            Enemy.Cleanbot(root, new Vector3(-3, .1f, 36), new Vector3(3, .1f, 70));
            Enemy.Cleanbot(root, new Vector3(4, .1f, 80), new Vector3(-4, .1f, 112));
            Enemy.Cleanbot(root, new Vector3(-20, .1f, 176), new Vector3(-6, .1f, 208));
            Enemy.Cleanbot(root, new Vector3(26, .1f, 212), new Vector3(26, .1f, 172));   // (beats clear of the shard spots: a cleanbot vacuums what it rolls over)
            Enemy.Cleanbot(root, new Vector3(-14, .1f, 282), new Vector3(-6, .1f, 282));
        }
    }

    // the escalator's speed: 9 m/s down until robot Kira turns it round, then a gentle 2.6 m/s up
    public sealed class EscalatorSpeed : MonoBehaviour
    {
        public FlipWalkway flip; Conveyor c;
        void Start() { c = GetComponent<Conveyor>(); }
        void Update() { if (c && flip) c.speed = flip.Powered ? 2.6f : 9f; }
    }
}
