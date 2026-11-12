using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 11: Victorian Steampunk's scenes. SteampunkArrival and MuscleManHero follow Dan's clips (06-Steampunk_Arrival.mp4,
    // 06-Steampunk_MuscleManHero.mp4: Docs/CUTSCENE-FILM-NOTES.md) shot by shot; side-by-side sheets: python Tools/film_sheet.py sheet <Name>.
    // SteampunkArrival's root is the belt's start (World/Steampunk.cs ArriveAt: world (0, 3.05, 10)): the factory floor is local y -3.05, the
    //   hall's south wall local z -10, the roof local y 15.
    // MuscleManHero's root is the belt's top under the press (world (0, 3, 84)): the north wall (the hole) is local z 12, the floor local y -3.
    // ClockShard's root is the great clock's foot (world (0, 0, 200)): the teal gear sits in the face at local (0, 6.4, -1.25).
    public static partial class CutsceneAuthoring
    {
        static Cast SteamCast => new Cast { dan = Have("dan_steampunk", Have("dan_pirate", "dan_home")), abi = Have("abi_steampunk", Have("abi_pirate", "abi_home")), kira = Have("kira_mechanical", "kira"), shard = new Color(.15f, .9f, .85f) };
        static string MMSteam => Have("muscleman_steampunk", MMModel);
        static readonly Color[] TealSwirl = { new Color(.15f, .95f, .85f), new Color(.6f, 1f, .95f), new Color(.05f, .6f, .6f), new Color(.35f, 1f, .9f) };
        public static void NecklaceWakesSteampunk() => NecklaceWakesIn("NecklaceWakesSteampunk", SteamCast);
        public static void ViolinistTeachesSteampunk() => ViolinistTeachesIn("ViolinistTeachesSteampunk", "Abi", SteamCast.abi, SteamCast);   // Abi's wall jump

        static GameObject SteamMM(Kit k, string n)
        {
            var mm = Cast0(k, n, MMSteam);
            if (!k.Models.ContainsKey(mm)) Placeholders.Prim(PrimitiveType.Capsule, mm.transform, new Vector3(0, 1f, 0), new Vector3(1f, 1f, .7f), Placeholders.Mat(new Color(.35f, .25f, .2f)), name: "Body");
            return mm;
        }
        static AnimationClip MMHold => PeopleClip("mm_hold") ?? PeopleClip("mm_lift") ?? PeopleClip("cheer");
        static AnimationClip MMThrow => PeopleClip("mm_throw") ?? PeopleClip("mm_boost") ?? PeopleClip("punch");

        public const float SteampunkArrivalLength = 23f;
        public static void SteampunkArrival()
        {
            DataAssets.Ensure(); lastQ.Clear();
            var c = SteamCast; NecklaceModel.Tint = c.shard;
            const float L = SteampunkArrivalLength;
            var k = new Kit("SteampunkArrival", L);
            // the city at night (0-12.5): the giant brass gear in front puffing steam, domes and chimneys, the factory behind; a teal streak
            // across the sky that winds into a big flat teal spiral over the factory (the clip, 0-14 s)
            var city = k.Holder("City", new Vector3(0, -3.05f, -10f), 0);
            var gear = PropFrom("steampunk_props", "BigGear", city.transform, new Vector3(-27f, 11f, -42f), 25f, 8f);
            if (gear) gear.name = "Giant gear";
            // the ground out there (the factory city's yards at night), blocks of houses with lit windows behind the domes
            Placeholders.Prim(PrimitiveType.Cube, city.transform, new Vector3(0, -.5f, -40f), new Vector3(260f, 1f, 160f), Placeholders.Mat(new Color(.1f, .1f, .14f)), name: "Ground");
            for (int i = 0; i < 9; i++) Placeholders.Prim(PrimitiveType.Cube, city.transform, new Vector3(-60f + i * 15f, 6f + (i % 3) * 3f, -2f - (i % 2) * 10f), new Vector3(11f, 12f + (i % 3) * 6f, 9f), Placeholders.Mat(new Color(.16f, .13f, .2f)), name: "Block");
            for (int i = 0; i < 6; i++)
            {
                var d = Placeholders.Prim(PrimitiveType.Sphere, city.transform, new Vector3(-40f + i * 16f, 1.5f, -18f + (i % 2) * 8f), new Vector3(10f, 9f, 10f), Placeholders.Mat(new Color(.12f, .3f, .3f)), name: "Dome");
                Placeholders.Prim(PrimitiveType.Cylinder, city.transform, new Vector3(-34f + i * 15f, 10f, -12f), new Vector3(1.6f, 10f, 1.6f), Placeholders.Mat(new Color(.18f, .15f, .14f)), name: "Chimney");
                _ = d;
            }
            for (int i = 0; i < 8; i++) Lamp(k, "Window " + i, new Vector3(-36f + i * 10f, 6f, -12f), new Color(1f, .7f, .35f), 9f, 1.4f, 0f, 12.5f);
            for (float t = .3f; t < 12.5f; t += .7f) Burst(k, "Steam " + t.ToString("0.0"), new Vector3(-17f, 5f + (t % 1.4f), -40f), new Color(.85f, .88f, .95f), t, 2.6f, 6, 2.2f, 2.4f, "Lit");
            var streak = k.Holder("Teal streak", new Vector3(-80f, 40f, 30f), 0);
            Placeholders.Prim(PrimitiveType.Sphere, streak.transform, Vector3.zero, new Vector3(18f, 2.2f, 2.2f), Placeholders.Mat(new Color(.3f, 1f, .9f), "Glow"), name: "Glow");
            k.Key(streak, "Glow", "m_LocalPosition", 0f, Vector3.zero); k.Key(streak, "Glow", "m_LocalPosition", 3.4f, new Vector3(80f, -6f, 0f));
            k.Show(streak, 0f, 3.6f);
            var over = new Vector3(0, 30f, 38f);
            Spiral(k, "Teal vortex", over, Quaternion.Euler(-90, 0, 0), TealSwirl, 26f, 3.4f, 9f, 12.6f, 70f, new Color(.05f, .45f, .45f), 6, true);
            Lamp(k, "Vortex light", over + Vector3.down * 6f, new Color(.3f, 1f, .9f), 70f, 2.4f, 4f, 12.6f);
            k.Shot("The city at night", 0f, 9f, new Vector3(-26f, 6f, -82f), new Vector3(-6f, 16f, 10f), 56f, new Vector3(-24f, 6.5f, -78f));
            k.Shot("Over the domes", 9f, 12.5f, new Vector3(-8f, 22f, -40f), new Vector3(0, 14f, 30f), 50f, new Vector3(-2f, 20f, -10f), new Vector3(0, 10f, 40f));
            // inside (12.5-23): the gear wall, the chain line with the copper bucket of bolts; the four drop out of a small teal swirl under the
            // roof onto the chain (arms up), into the bucket; it is carried along to the belt and tips them out onto the blue rollers
            var wall = k.Holder("Gear wall", new Vector3(-8f, 8f, 2.6f), 0);
            Placeholders.Prim(PrimitiveType.Cube, wall.transform, Vector3.zero, new Vector3(30f, 9f, .4f), Placeholders.Mat(new Color(.3f, .2f, .14f)), name: "Panel");
            for (int i = 0; i < 9; i++)
            {
                var gr = Placeholders.Prim(PrimitiveType.Cylinder, wall.transform, new Vector3(-13f + i * 3.3f, (i % 2 == 0 ? -1.6f : 1.4f), -.35f), new Vector3(2.6f + (i % 3) * .5f, .15f, 2.6f + (i % 3) * .5f), WorldArt.Mat("Brass"), name: "Gear " + i);
                gr.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            k.Show(wall, 12.4f, L);
            var line = k.Holder("Chain line", new Vector3(-6f, 9.6f, -.2f), 0);
            PropFrom("steampunk_props", "ChainLine", line.transform, Vector3.zero, 0);
            k.Show(line, 12.4f, L);
            var bucket = k.Holder("Bolt bucket", Vector3.zero, 0);
            var bk = PropFrom("steampunk_props", "BoltBucket", bucket.transform, new Vector3(-14f, 5.6f, 0), 0);
            if (!bk) Placeholders.Prim(PrimitiveType.Cylinder, bucket.transform, new Vector3(-14f, 6.4f, 0), new Vector3(2.6f, .8f, 2.6f), WorldArt.Mat("Copper"), name: "Bucket");
            string bp = bk ? bk.name : "Bucket";
            Vector3 bStart = new Vector3(-14f, 5.6f, 0), bEnd = new Vector3(-.4f, 5.6f, -.6f);
            k.Key(bucket, bp, "m_LocalPosition", 0f, bStart); k.Key(bucket, bp, "m_LocalPosition", 15.6f, bStart); k.Key(bucket, bp, "m_LocalPosition", 19.4f, bEnd);
            var q0 = bk ? bk.transform.localRotation : Quaternion.identity;
            k.KeyQ(bucket, bp, 0f, q0); k.KeyQ(bucket, bp, 19.4f, q0); k.KeyQ(bucket, bp, 20.4f, Quaternion.Euler(0, 0, -105f) * q0); k.KeyQ(bucket, bp, L, Quaternion.Euler(0, 0, -105f) * q0);
            k.Show(bucket, 12.4f, L);
            Spiral(k, "Teal swirl (inside)", new Vector3(-14f, 13.5f, 0), Quaternion.Euler(90, 0, 0), TealSwirl, 3.2f, 12.6f, 13.2f, 15.2f, 160f, null, 5, true);
            var dan = Cast0(k, "Dan", c.dan); var abi = Cast0(k, "Abi", c.abi); var kira = Cast0(k, "Kira", c.kira); var mm = SteamMM(k, "Muscle Man");
            var who = new[] { (dan, -.6f, .3f, 0f), (abi, .5f, .2f, .25f), (kira, 0f, -.5f, .45f), (mm, .1f, -.1f, .7f) };
            foreach (var (a, ox, oz, d) in who)
            {
                var off = new Vector3(ox, 0, oz); float inY = a == kira ? 7.2f : 6.4f;
                var w = new List<(float, Vector3, float)>
                {
                    (0f, new Vector3(-14f, 13f, 0) + off, 0f), (13.0f + d, new Vector3(-14f, 13f, 0) + off, 0f),
                    (13.4f + d, new Vector3(-14f, 10.1f, 0) + off, 30f),          // on the chain, arms up
                    (14.4f + d, new Vector3(-14f, 10.1f, 0) + off, 30f),
                    (14.9f + d, new Vector3(-14f, inY, 0) + off, 0f),              // into the bolts
                    (15.6f, new Vector3(-14f, inY, 0) + off, 0f), (19.4f, bEnd + new Vector3(0, inY - 5.6f, 0) + off, 0f),
                    (20.3f, new Vector3(0, 1.4f, 1.2f) + off, 0f), (20.9f, new Vector3(0, .02f, 2.4f + oz * 2f) + off, 0f), (L, new Vector3(0, .02f, 3.2f + oz * 2f) + off, 0f)
                };
                Path(k, a, w.ToArray());
                Appear(k, a, 12.9f + d, L, L);
                if (a == kira) k.Clips(a, (KiraClip("fall", c.kira), 0f), (KiraClip("idle", c.kira), 14.9f), (KiraClip("fall", c.kira), 20f), (KiraClip("land", c.kira), 20.9f), (KiraClip("idle", c.kira), 21.5f));
                else k.Clips(a, (PeopleClip("fall"), 0f), (PeopleClip("cheer"), 13.4f + d), (PeopleClip("idle"), 14.9f + d), (PeopleClip("surprised"), 16f), (PeopleClip("fall"), 20f), (PC("mm_fall", "fall_back"), 20.85f));
            }
            for (int i = 0; i < 3; i++) Burst(k, "Steam vent " + i, new Vector3(-10f + i * 5f, 7.5f, 1.2f), new Color(.9f, .92f, .98f), 13f + i * 1.7f, 1.4f, 6, 1.4f, 1.2f, "Lit");
            Burst(k, "Bolts", new Vector3(0, .6f, 2f), new Color(.45f, .47f, .52f), 20.4f, 1.6f, 14, .8f, .3f, "Lit", true);
            if (k.Necklaces.ContainsKey(dan)) PulseFrom(k, dan, 21.2f, L, L);
            k.Shot("The gear wall", 12.5f, 15.6f, new Vector3(-14f, 7.6f, -6.5f), new Vector3(-14f, 8.4f, 0), 52f, new Vector3(-14f, 7.4f, -5.8f));
            k.Shot("Carried along", 15.6f, 19.4f, new Vector3(-12f, 6.8f, -6f), new Vector3(-13f, 6.6f, 0), 52f, new Vector3(-1.5f, 6.6f, -6f), new Vector3(-1f, 6.4f, 0));
            k.Shot("Tipped onto the belt", 19.4f, L, new Vector3(1.5f, 2.6f, 6.8f), new Vector3(0, .8f, 2.2f), 56f, new Vector3(1.4f, 2.2f, 6.2f));
            TintC(k, new Color(.3f, 1f, .9f), (0f, 0f), (5f, .06f), (12.4f, .12f), (12.6f, .05f), (L, 0f));
            k.Sound("portal", 3.5f, .4f); k.Sound("chain_line", 15.6f, .6f); k.Sound("bucket_rattle", 15.0f, .7f); k.Sound("bucket_tip", 20.2f, .8f); k.Sound("belt_loop", 21f, .4f); k.Sound("necklace", 21.4f, .5f);
            Look(k, dan, abi, 21.6f, L); Look(k, abi, dan, 21.7f, L);
            k.Save();
            NecklaceModel.Tint = IceAgeCast.shard;
        }

        public const float MuscleManHeroLength = 14f;
        public static void MuscleManHero()
        {
            DataAssets.Ensure(); lastQ.Clear();
            var c = SteamCast; NecklaceModel.Tint = c.shard;
            const float L = MuscleManHeroLength;
            var k = new Kit("MuscleManHero", L);
            var dan = Cast0(k, "Dan", c.dan); var abi = Cast0(k, "Abi", c.abi); var kira = Cast0(k, "Kira", c.kira); var mm = SteamMM(k, "Muscle Man");
            // the wall the press smashes through (its own copy: hidden at the smash, the broken edges after)
            var patch = k.Holder("Wall patch", new Vector3(0, -3f, 12f), 0); PropFrom("steampunk_props", "WallPatch", patch.transform, Vector3.zero, 0); k.Show(patch, 0f, 10.6f);
            var broke = k.Holder("Wall break", new Vector3(0, -3f, 12f), 0); PropFrom("steampunk_props", "WallBreak", broke.transform, Vector3.zero, 0); k.Show(broke, 10.6f, L);
            // the dome press: up over the belt, comes down (4-6.4), held up (6.4-9.6), heaved off: out through the wall and window (9.6-11)
            var press = k.Holder("Press dome", Vector3.zero, 0);
            var pd = PropFrom("steampunk_props", "PressDome", press.transform, new Vector3(0, 6.2f, 0), 0);
            if (!pd) Placeholders.Prim(PrimitiveType.Cube, press.transform, new Vector3(0, 8.7f, 0), new Vector3(7f, 5f, 6f), WorldArt.Mat("Brass"), name: "Dome");
            string pp = pd ? pd.name : "Dome"; float baseY = pd ? 0f : 2.5f;
            var pq = pd ? pd.transform.localRotation : Quaternion.identity;
            k.Key(press, pp, "m_LocalPosition", 0f, new Vector3(0, 6.2f + baseY, 0)); k.Key(press, pp, "m_LocalPosition", 4.2f, new Vector3(0, 6.2f + baseY, 0));
            k.Key(press, pp, "m_LocalPosition", 6.4f, new Vector3(0, 2.75f + baseY, 0)); k.Key(press, pp, "m_LocalPosition", 9.6f, new Vector3(0, 2.6f + baseY, 0));
            k.Key(press, pp, "m_LocalPosition", 10.6f, new Vector3(0, 4.8f + baseY, 11.5f)); k.Key(press, pp, "m_LocalPosition", 11.4f, new Vector3(1.5f, 3.2f + baseY, 20f)); k.Key(press, pp, "m_LocalPosition", L, new Vector3(2f, 1.0f + baseY, 23f));
            k.KeyQ(press, pp, 0f, pq); k.KeyQ(press, pp, 9.6f, pq); k.KeyQ(press, pp, 10.6f, Quaternion.Euler(-35, 10, 15) * pq); k.KeyQ(press, pp, L, Quaternion.Euler(-80, 25, 30) * pq);
            // Dan, Abi and Kira carried along on the belt, flat, covering their heads (0-7.4); Muscle Man tumbles in behind, gets up, steps under it
            Path(k, dan, (0f, new Vector3(-.9f, 0, -9f), 0f), (5.5f, new Vector3(-.9f, 0, -1.4f), 0f), (L, new Vector3(-.9f, 0, -1.2f), 0f));
            Path(k, abi, (0f, new Vector3(.9f, 0, -9.6f), 0f), (5.7f, new Vector3(.9f, 0, -1.8f), 0f), (L, new Vector3(.9f, 0, -1.6f), 0f));
            Path(k, kira, (0f, new Vector3(0, 0, -8.4f), 0f), (5.6f, new Vector3(0, 0, -.9f), 0f), (L, new Vector3(0, 0, -.8f), 0f));
            k.Clips(dan, (PeopleClip("walk"), 0f), (PC("mm_fall", "fall_back"), 1.2f), (PeopleClip("surprised"), 11f), (PeopleClip("cheer"), 12.3f));
            k.Clips(abi, (PeopleClip("walk"), 0f), (PC("mm_fall", "fall_back"), 1.6f), (PeopleClip("surprised"), 11f), (PeopleClip("laugh") ?? PeopleClip("cheer"), 12.4f));
            k.Clips(kira, (KiraClip("walk", c.kira), 0f), (KiraClip("land", c.kira), 1.4f), (KiraClip("idle", c.kira), 2.4f), (KiraClip("jump", c.kira), 11.4f), (KiraClip("idle", c.kira), 12.2f));
            Path(k, mm, (0f, new Vector3(0, 0, -14f), 0f), (4.4f, new Vector3(0, 0, -4.4f), 0f), (6.0f, new Vector3(0, 0, -.2f), 0f), (L, new Vector3(0, 0, -.2f), 0f));
            k.Clips(mm, (PeopleClip("fall"), 0f), (PC("mm_getup", "land_hard"), 3.2f), (PeopleClip("run"), 4.4f), (MMHold, 6.0f), (MMThrow, 9.4f), (PC("mm_flex", "cheer"), 11.2f));
            // red alarms flashing, cables snapping, sparks
            for (int i = 0; i < 6; i++) { float t = 6.4f + i * .55f; Lamp(k, "Alarm " + i, new Vector3(i % 2 == 0 ? -5f : 5f, 3f, 1.5f), new Color(1f, .12f, .08f), 16f, 3.2f, t, t + .3f); }
            Burst(k, "Cable snaps", new Vector3(-2.5f, 7.5f, .5f), new Color(1f, .8f, .3f), 7.0f, 1.2f, 10, .6f, .3f);
            Burst(k, "Sparks", new Vector3(2.4f, 5.5f, -.5f), new Color(1f, .7f, .25f), 8.1f, 1.4f, 12, .7f, .4f);
            Burst(k, "Glass", new Vector3(0, 5f, 12f), new Color(.6f, .8f, 1f), 10.5f, 3.2f, 16, 1.2f, .6f, "Glow", true);
            Burst(k, "Bricks", new Vector3(0, 3f, 12f), new Color(.62f, .25f, .18f), 10.55f, 3.5f, 16, 1.4f, .4f, "Lit", true);
            for (int i = 0; i < 2; i++) Burst(k, "Dust " + i, new Vector3(-1f + i * 2f, 1.5f, 8f + i * 2f), new Color(.9f, .88f, .86f), 10.7f + i * .3f, 3f, 5, 2.2f, .8f, "Lit");
            TintC(k, new Color(1f, .15f, .1f), (0f, 0f), (6.3f, 0f), (6.6f, .16f), (7.2f, .06f), (7.8f, .16f), (8.4f, .06f), (9f, .16f), (10.5f, .1f), (11.5f, .05f), (L, 0f));
            if (k.Necklaces.ContainsKey(dan)) PulseFrom(k, dan, 12f, L, L);
            // shots, the clip's order
            k.Shot("Tumbling along the belt", 0f, 3.8f, new Vector3(-.3f, 1.1f, -2.6f), new Vector3(0, .35f, -7.5f), 52f, new Vector3(-.2f, .9f, -1.2f), new Vector3(0, .3f, -2.5f));
            k.Shot("Here it comes", 3.8f, 6.4f, new Vector3(0, 1.4f, -12f), new Vector3(0, 4.5f, 0), 50f);
            k.Shot("Heads down", 6.4f, 7.4f, new Vector3(1.7f, .5f, -.5f), new Vector3(-.3f, .3f, -1.6f), 52f);
            k.Shot("He holds it up", 7.4f, 9.6f, new Vector3(0, 1.0f, -6f), new Vector3(0, 2.9f, 0), 56f, new Vector3(0, 1.0f, -5.2f));
            k.Shot("He heaves it off", 9.6f, 11f, new Vector3(-3.5f, 1.8f, -6f), new Vector3(0, 4f, 6f), 54f);
            k.Shot("Out through the wall", 11f, L, new Vector3(4f, 3.5f, -3f), new Vector3(0, 3f, 12f), 55f, new Vector3(3.5f, 3.2f, -1.5f));
            k.Sound("belt_loop", 0f, .5f); k.Sound("press_thump", 4.4f, .6f); k.Sound("alarm", 6.4f, .7f); k.Sound("press_groan", 6.6f, .8f); k.Sound("cable_snap", 7.0f, .8f);
            k.Sound("cable_snap", 8.1f, .6f); k.Sound("mm_grunt", 9.4f, .9f); k.Sound("wall_smash", 10.5f, 1f); k.Sound("mm_laugh", 12.0f, .7f);
            Look(k, dan, mm, 11.2f, L); Look(k, abi, mm, 11.3f, L); Look(k, kira, mm, 11.4f, L); Look(k, mm, dan, 12.4f, L);
            k.Save();
            NecklaceModel.Tint = IceAgeCast.shard;
        }

        public const float ClockShardLength = 13f;
        public static void ClockShard()
        {
            DataAssets.Ensure(); lastQ.Clear();
            var c = SteamCast; NecklaceModel.Tint = c.shard;
            const float L = ClockShardLength;
            var k = new Kit("ClockShard", L);
            var dan = Cast0(k, "Dan", c.dan); var abi = Cast0(k, "Abi", c.abi); var kira = Cast0(k, "Kira", c.kira); var mm = SteamMM(k, "Muscle Man");
            var face = new Vector3(0, 6.4f, -1.25f);
            var gearH = k.Holder("Teal gear", Vector3.zero, 0);
            var g = PropFrom("steampunk_props", "TealGearShard", gearH.transform, face, 0);
            if (!g) Placeholders.CrystalObject("Crystal", c.shard, .4f).transform.SetParent(gearH.transform, false);
            string gp = g ? g.name : "Crystal";
            if (!g) k.Key(gearH, gp, "m_LocalPosition", 0f, face);
            Lamp(k, "Gear glint", face + new Vector3(0, 0, -.6f), c.shard, 5f, 2f, 0f, 7f);
            // Kira spots it (0-2.6); Muscle Man steps up and lifts Dan over his head (2.6-5.6); Dan takes it (6); the teal vortex (7.6-)
            Path(k, kira, (0f, new Vector3(-1.2f, 0, -3.6f), 0f), (L, new Vector3(-1.2f, 0, -3.6f), 0f));
            k.Clips(kira, (KiraClip("idle", c.kira), 0f), (KiraClip("jump", c.kira), 1.0f), (KiraClip("idle", c.kira), 1.8f));
            Path(k, abi, (0f, new Vector3(1.4f, 0, -4.2f), -15f), (L, new Vector3(1.4f, 0, -4.2f), -15f));
            k.Clips(abi, (PeopleClip("idle"), 0f), (PeopleClip("look"), 1.4f), (PeopleClip("cheer"), 6.4f), (PeopleClip("idle"), 7.6f));
            Path(k, mm, (0f, new Vector3(1.6f, 0, -6f), 0f), (2.6f, new Vector3(1.6f, 0, -6f), 0f), (3.8f, new Vector3(0, 0, -2.1f), 0f), (L, new Vector3(0, 0, -2.1f), 0f));
            k.Clips(mm, (PeopleClip("idle"), 0f), (PeopleClip("walk"), 2.6f), (MMHold, 3.8f), (PC("mm_cheer", "cheer"), 7.0f));
            Path(k, dan, (0f, new Vector3(-.4f, 0, -4.6f), 0f), (3.6f, new Vector3(-.4f, 0, -4.6f), 0f), (4.0f, new Vector3(0, 0, -2.3f), 0f), (5.2f, new Vector3(0, 3.0f, -2.0f), 0f), (6.8f, new Vector3(0, 3.0f, -2.0f), 0f), (7.4f, new Vector3(0, 0, -2.6f), 0f), (L, new Vector3(0, 0, -2.6f), 0f));
            k.Clips(dan, (PeopleClip("look"), 0f), (PeopleClip("idle"), 2.6f), (PeopleClip("offer") ?? PeopleClip("cheer"), 5.2f), (PeopleClip("cheer"), 6.4f), (PeopleClip("idle"), 7.4f));
            k.Key(gearH, gp, "m_LocalPosition", 5.9f, face); k.Key(gearH, gp, "m_LocalPosition", 6.3f, new Vector3(.3f, 4.9f, -2.0f)); k.Key(gearH, gp, "m_LocalPosition", 7.2f, new Vector3(.3f, 4.9f, -2.0f));
            k.Show(gearH, 0f, 7.3f);
            Burst(k, "Teal flash", face, c.shard, 6.0f, 1.6f, 12, .9f, .4f);
            if (k.Necklaces.ContainsKey(dan)) PulseFrom(k, dan, 6.2f, L, L);
            // the teal vortex opens over the clock (the game's change) and takes all four up into it
            var eye = new Vector3(0, 15f, 0);
            Spiral(k, "Teal vortex", eye, Quaternion.Euler(-90, 0, 0), TealSwirl, 9f, 7.4f, 9.2f, L, 120f, new Color(.05f, .45f, .45f), 6, true);
            Lamp(k, "Vortex light", eye + Vector3.down * 3f, c.shard, 30f, 3f, 7.6f, L);
            foreach (var (a, ph, t0) in new[] { (dan, 0f, 9.2f), (abi, 1.6f, 9.4f), (kira, 3.1f, 9.6f), (mm, 4.4f, 9.9f) })
            {
                var from = a == dan ? new Vector3(0, 0, -2.6f) : a == abi ? new Vector3(1.4f, 0, -4.2f) : a == kira ? new Vector3(-1.2f, 0, -3.6f) : new Vector3(0, 0, -2.1f);
                var w = new List<(float, Vector3, float)> { (t0, from, 0f) };
                for (int i = 1; i <= 8; i++) { float f = i / 8f, ang = ph + f * 6f, r = Mathf.Lerp(2f, .2f, f); w.Add((Mathf.Lerp(t0, t0 + 2.4f, f), Vector3.Lerp(from, eye, f) + new Vector3(Mathf.Cos(ang) * r, 0, Mathf.Sin(ang) * r), f * 540f)); }
                Path(k, a, w.ToArray());
                k.Key(a, "Body", "m_LocalScale", t0 + 1.4f, Vector3.one); k.Key(a, "Body", "m_LocalScale", t0 + 2.4f, Vector3.one * .05f); k.Key(a, "Body", "m_LocalScale", L, Vector3.one * .05f);
                if (a == kira) k.Clips(a, (KiraClip("idle", c.kira), 0f), (KiraClip("fall", c.kira), t0));
            }
            k.Clips(dan, (PeopleClip("look"), 0f), (PeopleClip("idle"), 2.6f), (PeopleClip("offer") ?? PeopleClip("cheer"), 5.2f), (PeopleClip("cheer"), 6.4f), (PeopleClip("idle"), 7.4f), (PeopleClip("fall"), 9.2f));
            k.Shot("Kira spots it", 0f, 2.6f, new Vector3(-1.6f, .5f, -6.4f), new Vector3(0, 6.4f, -1.2f), 46f, new Vector3(-1.4f, .6f, -5.8f));
            k.Shot("Muscle Man lifts Dan", 2.6f, 5.6f, new Vector3(4.5f, 2.4f, -8f), new Vector3(0, 3.6f, -1.8f), 50f);
            k.Shot("Dan takes it", 5.6f, 7.6f, new Vector3(1.4f, 5.6f, -4.6f), new Vector3(0, 5.6f, -1.6f), 44f);
            k.Shot("The teal vortex", 7.6f, L, new Vector3(0, 2f, -16f), new Vector3(0, 9f, 0), 56f, new Vector3(0, 2.4f, -13f), new Vector3(0, 13f, 0));
            TintC(k, Color.white, (0f, 0f), (11.6f, 0f), (12.6f, 1f), (L, 1f));
            k.Sound("clock_chime", .4f, .7f); k.Sound("mm_grunt", 3.9f, .8f); k.Sound("story", 6.0f, .7f); k.Sound("teal_vortex", 7.5f, .9f);
            Look(k, kira, gearH, 0f, 2.6f); Look(k, abi, gearH, 1.2f, 6f); Look(k, mm, dan, 3.8f, 6.8f);
            k.Save();
            NecklaceModel.Tint = IceAgeCast.shard;
        }
    }
}
