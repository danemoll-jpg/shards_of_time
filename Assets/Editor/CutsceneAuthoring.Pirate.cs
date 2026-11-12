using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 10: the Pirate Seas' scenes. PirateArrival and ShardOverboard follow Dan's clips (04-Medieval_TheStone_05-Pirate_Arrival.mp4 from
    // ~70 s; Worlds/05-PirateSeas/shard-rolls-overboard.mp4) shot by shot; side-by-side sheets: python Tools/film_sheet.py sheet <Name>.
    // PirateArrival's root is the main deck (World/Pirate.cs ArriveAt: world (0, 3.5, -4); the ship's origin is local (0, -3.5, 4)).
    // ShardOverboard's root is the treasure chest by the starboard rail (world (4.9, 3.5, -7.5)): the port rail is local x -10.8, the sea -3.5.
    // SeaVortex's root is the green shard on the sea floor.
    public static partial class CutsceneAuthoring
    {
        static Cast PirateCast => new Cast { dan = Have("dan_pirate", Have("dan_medieval", "dan_home")), abi = Have("abi_pirate", Have("abi_medieval", "abi_home")), kira = Have("kira_otter", "kira"), shard = new Color(.2f, .95f, .45f) };
        static string MMModel => Have("muscleman_pirate", Have("muscleman_medieval", ""));
        static readonly Color[] GreenSwirl = { new Color(.2f, .95f, .4f), new Color(.75f, 1f, .7f), new Color(.1f, .65f, .3f), new Color(.45f, 1f, .6f) };
        public static void NecklaceWakesPirate() => NecklaceWakesIn("NecklaceWakesPirate", PirateCast);
        public static void ViolinistTeachesPirate() => ViolinistTeachesIn("ViolinistTeachesPirate", "Dan", PirateCast.dan, PirateCast);   // the deep dive (everyone; Dan in the scene)

        static GameObject MuscleMan(Kit k, string n)
        {
            var mm = Cast0(k, n, MMModel);
            if (!k.Models.ContainsKey(mm)) Placeholders.Prim(PrimitiveType.Capsule, mm.transform, new Vector3(0, 1f, 0), new Vector3(1f, 1f, .7f), Placeholders.Mat(new Color(.35f, .25f, .2f)), name: "Body");
            return mm;
        }
        static AnimationClip PC(string n, string fallback) => PeopleClip(n) ?? PeopleClip(fallback);

        public const float PirateArrivalLength = 34f;
        public static void PirateArrival()
        {
            DataAssets.Ensure(); lastQ.Clear();
            var c = PirateCast; NecklaceModel.Tint = c.shard;
            const float L = PirateArrivalLength;
            var k = new Kit("PirateArrival", L);
            // the tunnel (from TheStone): the four tumble down the blue-white spiral, high over the sea
            var tun = k.Set("Tunnel", 0, -4f, 0, 420f, false); k.under = tun;
            Spiral(k, "Tunnel spiral", new Vector3(0, -32f, 0), Quaternion.Euler(-90, 0, 0), BlueSwirl, 26f, 0f, .1f, 3.2f, 100f, new Color(.4f, .75f, 1f), 6, false);
            var td = Cast0(k, "Dan tumbling", c.dan); var tm = MuscleMan(k, "Muscle Man tumbling");
            k.Clips(td, (PeopleClip("fall"), 0f)); k.Clips(tm, (PeopleClip("fall"), 0f));
            Tumble(k, tm, new Vector3(-.3f, -3f, .2f), new Vector3(0, -16f, 0), 0f, 3.1f, 1.2f, 60f, 0f, L);
            Tumble(k, td, new Vector3(.6f, -6f, -.3f), new Vector3(.2f, -18f, .1f), 0f, 3.1f, 1.4f, 60f, .9f, L);
            k.Shot("Down the tunnel", 0f, 3.2f, new Vector3(0, .5f, .02f), new Vector3(0, -30f, 0), 62f, new Vector3(0, -1.5f, .02f));
            k.under = null;
            // the green vortex over the ship (8 s on), its light
            var vortexAt = new Vector3(0, 30f, 4f);
            Spiral(k, "Green vortex", vortexAt, Quaternion.Euler(-90, 0, 0), GreenSwirl, 18f, 8f, 11f, 23f, 90f, new Color(.15f, .6f, .3f), 5, true, 21.5f);
            Lamp(k, "Vortex light", new Vector3(0, 16f, 4f), new Color(.35f, 1f, .5f), 60f, 3.2f, 8.5f, L);
            // Fix 10.1, item 3 (the clip, 87-89 s): the three fall out of the vortex past the sails and each drops into an upright barrel (a shot
            // of each landing: thud, splash, the barrel wobbles); from the cut at 13.6 the barrels lie on their sides, mouths to the camera (-z),
            // and they crawl out: Dan's, Kira's, Abi's
            var barrels = k.Holder("Barrels", Vector3.zero, 0);
            float[] bx = { -1.0f, .2f, 1.4f }; float[] landT = { 12.5f, 12.95f, 13.35f }; const float upZ = 3.1f, cut = 13.6f;
            for (int i = 0; i < 3; i++)
            {
                var b = PropFrom("pirate_props", "Barrel", barrels.transform, new Vector3(bx[i], .45f, 2.0f), 0);
                if (b)
                {
                    b.name = "Barrel" + i; var up = b.transform.localRotation; var lying = Quaternion.Euler(90, 0, 0) * up;
                    if (Quaternion.Dot(up, lying) < 0) lying = new Quaternion(-lying.x, -lying.y, -lying.z, -lying.w);
                    b.transform.localRotation = lying; b.transform.localPosition = new Vector3(bx[i], .45f, 2.55f);
                    string bp = "Barrel" + i; var upPos = new Vector3(bx[i], 0, upZ); float lt = landT[i];
                    k.Key(barrels, bp, "m_LocalPosition", 0f, upPos); k.Key(barrels, bp, "m_LocalPosition", cut - .02f, upPos);
                    k.KeyQ(barrels, bp, 0f, up);
                    foreach (var (dt, tilt) in new[] { (.06f, 7f), (.16f, -5f), (.26f, 3f), (.36f, 0f) }) k.KeyQ(barrels, bp, lt + dt, Quaternion.Euler(0, 0, tilt) * up);
                    k.KeyQ(barrels, bp, cut - .02f, up); k.KeyQ(barrels, bp, cut, lying);
                    Burst(k, "Thud " + i, new Vector3(bx[i], 1.15f, upZ), new Color(.62f, .44f, .26f), lt, .55f, 7, .7f, .5f, "Lit", true);
                    Burst(k, "Splash " + i, new Vector3(bx[i], 1.2f, upZ), new Color(.85f, .95f, 1f), lt + .02f, .7f, 8, .6f, .9f);
                }
                else Placeholders.Prim(PrimitiveType.Cylinder, barrels.transform, new Vector3(bx[i], .45f, 2.0f), new Vector3(.9f, .55f, .9f), Placeholders.Mat(new Color(.5f, .32f, .18f)), name: "Barrel" + i).transform.localRotation = Quaternion.Euler(90, 0, 0);
                // a wobble as someone lands inside
                string p = "Barrel" + i; var home = new Vector3(bx[i], .45f, b ? 2.55f : 2.0f);
                foreach (var (t, dx) in new[] { (13.6f + i * .3f, 0f), (13.75f + i * .3f, .08f), (13.9f + i * .3f, -.06f), (14.1f + i * .3f, 0f) }) k.Key(barrels, p, "m_LocalPosition", t, home + new Vector3(dx, 0, 0));
            }
            // the three falling out of the vortex (10.6), tumbling past the sails, straightening and dropping feet first into their barrels
            var fd = Cast0(k, "Dan falling", c.dan); var fk = Cast0(k, "Kira falling", c.kira); var fa = Cast0(k, "Abi falling", c.abi);
            var fallers = new[] { fd, fk, fa };
            for (int i = 0; i < 3; i++)
            {
                var a = fallers[i]; float lt = landT[i], ph = i * 1.7f;
                var top = new Vector3(bx[i] + (i - 1) * .9f, 27f, 4.4f); var mid = new Vector3(bx[i], 8.5f, upZ + .3f);
                for (float t = 10.6f; t <= 11.8f + .001f; t += .2f)
                {
                    float f = Mathf.InverseLerp(10.6f, 11.8f, t);
                    k.Key(a, "Body", "m_LocalPosition", t, Vector3.Lerp(top, mid, f * f * .4f + f * .6f) + new Vector3(Mathf.Sin(f * 7f + ph) * .35f, 0, 0));
                    KeyRot(k, a, "Body", t, Quaternion.Euler(50f * Mathf.Sin(f * 6f + ph), ph * 40f + f * 300f, 40f * Mathf.Cos(f * 5f + ph)));
                }
                k.Key(a, "Body", "m_LocalPosition", lt - .55f, new Vector3(bx[i], 4.2f, upZ)); KeyRot(k, a, "Body", lt - .55f, Quaternion.Euler(10f, 180f, 0));
                k.Key(a, "Body", "m_LocalPosition", lt, new Vector3(bx[i], i == 1 ? 1.0f : .85f, upZ)); KeyRot(k, a, "Body", lt, Quaternion.Euler(0, 180f, 0));
                k.Key(a, "Body", "m_LocalPosition", lt + .13f, new Vector3(bx[i], .15f, upZ));   // down into it (hidden by its staves)
                Appear(k, a, 10.6f, lt + .14f, L);
                if (i == 1) k.Clips(a, (KiraClip("fall", c.kira), 0f));
                else k.Clips(a, (PeopleClip("fall"), 0f), (PeopleClip("jump") ?? PeopleClip("idle"), lt - .6f));   // feet first for the drop (the skydive clip hit the barrel flat)
            }
            // a broken barrel where Muscle Man crashes (staves scattered), shown from the crash
            var wreck = k.Holder("Smashed barrel", new Vector3(1.6f, 0, 4.4f), 0);
            for (int i = 0; i < 8; i++) Placeholders.Prim(PrimitiveType.Cube, wreck.transform, new Vector3(Mathf.Cos(i * .8f) * (.6f + .1f * (i % 3)), .03f, Mathf.Sin(i * .8f) * (.7f + .1f * (i % 2))), new Vector3(.12f, .04f, .65f), WorldArt.Mat("Wood"), name: "Stave").transform.localRotation = Quaternion.Euler(0, i * 47f, 0);
            k.Show(wreck, 21.8f, L);
            // Dan and otter Kira pop out of their barrels (15 s), Abi out of hers off to the right and walks in (16.5)
            var dan = Cast0(k, "Dan", c.dan); var abi = Cast0(k, "Abi", c.abi); var kira = Cast0(k, "Kira", c.kira);
            Path(k, dan, (0f, new Vector3(-1f, .05f, 2.6f), 180f), (15f, new Vector3(-1f, .05f, 2.6f), 180f), (15.9f, new Vector3(-1f, 0, 1.2f), 180f), (16.6f, new Vector3(-.55f, 0, .8f), 180f), (21.7f, new Vector3(-.55f, 0, .8f), 180f), (22.2f, new Vector3(-.55f, 0, .8f), 28f), (30.0f, new Vector3(-.55f, 0, .8f), 28f), (30.45f, new Vector3(-.55f, 0, .8f), 180f), (L, new Vector3(-.55f, 0, .8f), 180f));
            Appear(k, dan, 14.95f, L, L);
            k.Clips(dan, (PeopleClip("idle"), 0f), (PeopleClip("walk"), 15f), (PeopleClip("look"), 16.6f), (PeopleClip("surprised"), 21.6f), (PeopleClip("idle"), 23.5f), (PeopleClip("hold_up"), 30.6f));
            Path(k, kira, (0f, new Vector3(.2f, 0, 2.6f), 180f), (15.3f, new Vector3(.2f, 0, 2.6f), 180f), (16.2f, new Vector3(.25f, 0, .5f), 180f), (21.9f, new Vector3(.25f, 0, .5f), 180f), (22.4f, new Vector3(.25f, 0, .5f), 18f), (29.9f, new Vector3(.25f, 0, .5f), 18f), (30.4f, new Vector3(.25f, 0, .5f), 180f), (L, new Vector3(.25f, 0, .5f), 180f));
            Appear(k, kira, 15.25f, L, L);
            k.Clips(kira, (KiraClip("idle", c.kira), 0f), (KiraClip("walk", c.kira), 15.3f), (KiraClip("idle", c.kira), 16.2f), (KiraClip("jump", c.kira), 21.7f), (KiraClip("idle", c.kira), 22.3f));
            Path(k, abi, (0f, new Vector3(2.8f, 0, 1.6f), 250f), (16.5f, new Vector3(2.8f, 0, 1.6f), 250f), (17.8f, new Vector3(.95f, 0, .85f), 180f), (21.8f, new Vector3(.95f, 0, .85f), 180f), (22.3f, new Vector3(.95f, 0, .85f), 10f), (30.0f, new Vector3(.95f, 0, .85f), 10f), (30.45f, new Vector3(.95f, 0, .85f), 180f), (L, new Vector3(.95f, 0, .85f), 180f));
            Appear(k, abi, 16.45f, L, L);
            k.Clips(abi, (PeopleClip("idle"), 0f), (PeopleClip("walk"), 16.5f), (PeopleClip("idle"), 17.8f), (PeopleClip("surprised"), 21.6f), (PeopleClip("idle"), 23.5f), (PeopleClip("laugh"), 30.5f));
            if (k.Necklaces.ContainsKey(dan)) PulseFrom(k, dan, 30.6f, L, L);
            // Muscle Man drops out of the vortex, through the mainsail, and crashes flat on the deck (21.6); lies there; sits up grinning (27.5)
            var mm = MuscleMan(k, "Muscle Man");
            var land = new Vector3(1.6f, 0, 4.4f);
            Path(k, mm, (0f, new Vector3(1.4f, 26f, 4.4f), 0f), (19.5f, new Vector3(1.4f, 26f, 4.4f), 0f), (20.6f, new Vector3(1.5f, 8f, 4.4f), 0f), (21.6f, land, 0f), (L, land, 0f));
            Appear(k, mm, 19.45f, L, L);
            for (float t = 19.5f; t < 21.6f; t += .25f) KeyRot(k, mm, "Body", t, Quaternion.Euler(Mathf.Sin(t * 5f) * 40f, (t - 19.5f) * 160f, Mathf.Cos(t * 4f) * 30f));
            KeyRot(k, mm, "Body", 21.6f, Quaternion.identity);
            k.Clips(mm, (PeopleClip("fall"), 0f), (PC("mm_fall", "fall_back"), 21.55f), (PC("mm_getup", "land_hard"), 27.5f), (PC("laugh", "idle"), 30.2f));
            Burst(k, "Sail rips", new Vector3(1.5f, 5.0f, 4.7f), new Color(.98f, .72f, .8f), 20.7f, 1.6f, 9, 1.4f, .2f, "Lit", true);
            Burst(k, "Crash", land + Vector3.up * .3f, new Color(.6f, .42f, .25f), 21.6f, 2.2f, 12, 1.2f, .5f, "Lit", true);
            // shots, the film's order
            var shipSide = new Vector3(0, 4f, 4f);
            k.Shot("The ship at night", 3.2f, 8f, new Vector3(-58f, 3f, 12f), shipSide, 40f, new Vector3(-54f, 3.5f, 8f));
            k.Shot("The green vortex", 8f, 10.6f, new Vector3(-52f, 6f, 2f), new Vector3(0, 13f, 4f), 52f, new Vector3(-49f, 6.8f, 3f), new Vector3(0, 15f, 4f));
            k.Shot("Falling past the sails", 10.6f, 11.8f, new Vector3(-4.5f, 1.2f, -3.5f), new Vector3(.2f, 15f, 4.2f), 62f, null, new Vector3(.2f, 10f, 4f));
            k.Shot("Down onto the deck", 11.8f, 12.3f, new Vector3(-3.0f, 5.2f, upZ - .6f), new Vector3(.4f, .9f, upZ), 60f);   // from the side, inboard of the shrouds: between the main and fore sails
            for (int i = 0; i < 3; i++)   // a shot of each landing
                k.Shot("Into barrel " + i, i == 0 ? 12.3f : landT[i - 1] + .23f, i == 2 ? cut : landT[i] + .23f, new Vector3(bx[i] + .5f, 2.3f, upZ - 2.3f), new Vector3(bx[i], 1.0f, upZ), 50f);
            k.Shot("The barrels", 13.6f, 17f, new Vector3(.1f, .55f, -1.4f), new Vector3(.1f, .45f, 2.4f), 50f, new Vector3(.1f, .6f, -1.1f));
            k.Shot("Feet and the otter", 17f, 18.2f, new Vector3(.3f, .35f, -.6f), new Vector3(.3f, .3f, 1.2f), 55f);
            k.Shot("Standing", 18.2f, 19.5f, new Vector3(.2f, 1.6f, -3.4f), new Vector3(.2f, 1f, 1.2f), 48f);
            k.Shot("Through the sails", 19.5f, 21.5f, new Vector3(-9f, 1.8f, -3f), new Vector3(1.4f, 12f, 4.4f), 58f, null, new Vector3(1.4f, 2f, 4.4f));
            k.Shot("The crash", 21.5f, 26f, new Vector3(.3f, 2.3f, -2.4f), new Vector3(1.1f, .4f, 3.4f), 54f);
            k.Shot("Flat on the deck", 26f, 27.5f, new Vector3(2.8f, 1.3f, 1.2f), new Vector3(1.6f, .3f, 3.4f), 46f);
            k.Shot("He sits up grinning", 27.5f, 30.5f, new Vector3(1.6f, 1.4f, 7.4f), new Vector3(1.6f, 1.0f, 4.0f), 44f, new Vector3(1.6f, 1.5f, 7.0f));
            k.Shot("Abi laughs", 30.5f, 32.4f, new Vector3(.25f, 1.55f, -1.5f), new Vector3(.25f, 1.45f, .8f), 46f);
            k.Shot("The green shard", 32.4f, L, new Vector3(-.2f, 1.45f, -1.0f), new Vector3(-.55f, 1.3f, .8f), 40f);
            TintC(k, new Color(.35f, 1f, .5f), (0f, 0f), (9f, 0f), (12f, .18f), (21.5f, .2f), (23f, .08f), (L, .08f));
            k.Sound("portal", 3.0f, .5f); for (int i = 0; i < 3; i++) { k.Sound("barrel_thud", landT[i], .8f); k.Sound("splash", landT[i] + .03f, .35f); }
            k.Sound("sail_rip", 20.6f, .8f); k.Sound("wood_crash", 21.6f, .9f); k.Sound("mm_grunt", 27.6f, .7f); k.Sound("mm_laugh", 29.4f, .7f); k.Sound("necklace", 30.8f, .6f);
            // Fix 10.1, item 3: who looks at whom (CutsceneLookAt): all three at Muscle Man after his crash; he looks at Dan as Abi laughs;
            // Dan glances at Abi (the clip's last shot)
            Look(k, dan, mm, 22.2f, 29.9f); Look(k, abi, mm, 22.3f, 29.9f); Look(k, kira, mm, 22.4f, 29.8f);
            Look(k, mm, dan, 30.6f, L); Look(k, dan, abi, 30.9f, 32.3f);
            k.Save();
            NecklaceModel.Tint = IceAgeCast.shard;
        }

        public const float ShardOverboardLength = 17.5f;
        public static void ShardOverboard()
        {
            DataAssets.Ensure(); lastQ.Clear();
            var c = PirateCast; NecklaceModel.Tint = c.shard;
            const float L = ShardOverboardLength;
            var k = new Kit("ShardOverboard", L);
            var dan = Cast0(k, "Dan", c.dan); var abi = Cast0(k, "Abi", c.abi); var kira = Cast0(k, "Kira", c.kira); var mm = MuscleMan(k, "Muscle Man");
            // the shard: in the chest (0-1.4), in his raised right fist (1.4-5.3), then loose: it
            // slips, bounces, rolls across the deck, under the rail and down into the sea
            float seaY = -3.45f, railX = -10.8f;
            var inChest = k.Holder("Green shard (chest)", new Vector3(0, .75f, 0), 0);
            Placeholders.CrystalObject("Crystal", c.shard, .3f).transform.SetParent(inChest.transform, false);
            k.Show(inChest, 0f, 1.4f);
            var shard = k.Holder("Green shard", Vector3.zero, 0);
            Placeholders.CrystalObject("Crystal", c.shard, .3f).transform.SetParent(shard.transform, false);
            var keys = new (float t, Vector3 p)[] { (1.4f, new Vector3(0, .75f, 0)), (1.9f, new Vector3(-1.0f, 1.25f, .3f)), (2.9f, new Vector3(-1.95f, 2.3f, .55f)), (4.1f, new Vector3(-1.95f, 2.4f, .55f)), (5.1f, new Vector3(-1.95f, 2.3f, .55f)), (5.3f, new Vector3(-2.1f, 2.5f, .4f)), (5.5f, new Vector3(-2.5f, 2.9f, .3f)), (5.9f, new Vector3(-3.1f, .12f, .35f)),
                (6.15f, new Vector3(-3.4f, .55f, .4f)), (6.45f, new Vector3(-3.7f, .12f, .45f)), (10.4f, new Vector3(railX + .25f, .12f, .6f)), (10.9f, new Vector3(railX - 1.0f, -.5f, .6f)),
                (11.6f, new Vector3(railX - 1.7f, seaY + .1f, .6f)), (12.3f, new Vector3(railX - 1.8f, seaY - 1.2f, .6f)), (L, new Vector3(railX - 1.8f, seaY - 2f, .6f)) };
            foreach (var (t, p) in keys) k.Key(shard, "Crystal", "m_LocalPosition", t, p);
            for (float t = 6.45f; t <= 10.4f; t += .2f) KeyRot(k, shard, "Crystal", t, Quaternion.Euler(0, 0, (t - 6.45f) * 420f));
            k.Show(shard, 1.4f, L);   // (his right fist, raised: Victory; yaw -90 puts his right hand toward +z)
            Lamp(k, "Shard light", new Vector3(-2.2f, 3.1f, .2f), new Color(.4f, 1f, .55f), 6f, 2.2f, 1.9f, 5.6f);
            // Muscle Man: grabs it out of the chest (west of it, facing it), turns to the mainmast holding it up high and laughing (the film: from
            // behind him, the shard against the sky), it slips, he gapes as it rolls away, then charges to the rail and leaps over after it
            Path(k, mm, (0f, new Vector3(-1.1f, 0, 0), 90f), (1.9f, new Vector3(-1.1f, 0, 0), 90f), (2.6f, new Vector3(-1.9f, 0, .1f), -90f), (9.7f, new Vector3(-2.0f, 0, .1f), -90f),
                (11.3f, new Vector3(-9.6f, 0, .3f), -90f), (11.8f, new Vector3(-10.4f, 1.2f, .3f), -90f), (12.3f, new Vector3(-12.2f, .6f, .3f), -90f), (12.9f, new Vector3(-13.4f, seaY - .8f, .3f), -90f), (L, new Vector3(-13.6f, seaY - 1.6f, .3f), -90f));
            k.Clips(mm, (PeopleClip("idle"), 0f), (PC("mm_lift", "offer"), .3f), (PC("mm_cheer", "cheer"), 2.6f), (PeopleClip("surprised"), 5.3f), (PeopleClip("run"), 9.7f),
                (PC("mm_leap", "jump"), 11.3f), (PeopleClip("fall"), 12.1f), (PC("mm_struggle", "swim"), 12.9f));
            k.Key(mm, "Body", "m_LocalScale", 12.8f, Vector3.one); k.Key(mm, "Body", "m_LocalScale", 13.9f, Vector3.one * .001f); k.Key(mm, "Body", "m_LocalScale", L, Vector3.one * .001f);
            // Dan and Abi watch from by the chest, gasp, run after him and jump; otter Kira dives in last
            Vector3 dStart = new Vector3(-.6f, 0, -1.7f), aStart = new Vector3(.3f, 0, -1.9f), kStart = new Vector3(-.5f, 0, 1.4f);
            Path(k, dan, (0f, dStart, -60f), (12.9f, dStart, -90f), (14.7f, new Vector3(-10f, 0, -1.2f), -90f), (15.2f, new Vector3(-11.2f, 1.2f, -1.2f), -90f), (15.9f, new Vector3(-13f, seaY - .8f, -1.2f), -90f), (L, new Vector3(-13.2f, seaY - 1.4f, -1.2f), -90f));
            Path(k, abi, (0f, aStart, -60f), (13.2f, aStart, -90f), (15.1f, new Vector3(-10f, 0, -2.0f), -90f), (15.6f, new Vector3(-11.2f, 1.4f, -2.0f), -90f), (16.3f, new Vector3(-13f, seaY - .8f, -2.0f), -90f), (L, new Vector3(-13.2f, seaY - 1.4f, -2.0f), -90f));
            Path(k, kira, (0f, kStart, -90f), (13.8f, kStart, -90f), (15.6f, new Vector3(-10.2f, 0, .9f), -90f), (16.1f, new Vector3(-11.4f, .9f, .9f), -90f), (16.8f, new Vector3(-12.8f, seaY - .7f, .9f), -90f), (L, new Vector3(-13f, seaY - 1.2f, .9f), -90f));
            k.Clips(dan, (PeopleClip("idle"), 0f), (PeopleClip("surprised"), 5.4f), (PeopleClip("run"), 12.9f), (PeopleClip("jump"), 14.7f), (PeopleClip("swim"), 15.9f));
            k.Clips(abi, (PeopleClip("idle"), 0f), (PeopleClip("surprised"), 5.5f), (PeopleClip("run"), 13.2f), (PeopleClip("jump"), 15.1f), (PeopleClip("swim"), 16.3f));
            k.Clips(kira, (KiraClip("idle", c.kira), 0f), (KiraClip("run", c.kira), 13.8f), (KiraClip("jump", c.kira), 15.6f), (KiraClip("swim", c.kira), 16.8f));
            if (k.Necklaces.ContainsKey(dan)) PulseFrom(k, dan, 0f, 6f, L);
            Burst(k, "Splash (shard)", new Vector3(railX - 1.7f, seaY, .6f), new Color(.85f, .95f, 1f), 11.6f, .9f, 8, .9f, .9f);
            Burst(k, "Splash (Muscle Man)", new Vector3(-13.4f, seaY, .3f), new Color(.85f, .95f, 1f), 12.8f, 2.4f, 12, 1.3f, 1.1f);
            Burst(k, "Splash (Dan)", new Vector3(-13f, seaY, -1.2f), new Color(.85f, .95f, 1f), 15.8f, 1.6f, 10, 1.1f, 1f);
            Burst(k, "Splash (Abi)", new Vector3(-13f, seaY, -2.0f), new Color(.85f, .95f, 1f), 16.2f, 1.6f, 10, 1.1f, 1f);
            Burst(k, "Splash (Kira)", new Vector3(-12.8f, seaY, .9f), new Color(.85f, .95f, 1f), 16.7f, .9f, 8, .9f, .9f);
            // shots: the film's (from behind him, the shard against the sky; the low shot rolling with it along the deck; over the side), then the leaps
            k.Shot("He grabs it", 0f, 2.6f, new Vector3(-1.3f, 1.9f, 3.0f), new Vector3(-.6f, 1.2f, 0f), 46f);
            k.Shot("Held up, laughing", 2.6f, 5.3f, new Vector3(.3f, 1.5f, .9f), new Vector3(-2.6f, 2.3f, -.1f), 50f, new Vector3(.15f, 1.45f, .8f));
            k.Shot("It slips", 5.3f, 6.5f, new Vector3(-.5f, 1.4f, 2.2f), new Vector3(-2.8f, 1.0f, .2f), 55f);
            k.Shot("It rolls", 6.5f, 10.4f, new Vector3(-2.9f, .32f, .9f), new Vector3(-6f, .2f, .6f), 50f, new Vector3(-8.8f, .3f, .9f), new Vector3(-11.6f, .1f, .6f));
            k.Shot("Over the side", 10.4f, 13.2f, new Vector3(-14.5f, -.6f, -3.5f), new Vector3(-11.0f, -.8f, .5f), 56f);
            k.Shot("They leap in after it", 13.2f, L, new Vector3(-24f, -1.2f, -7f), new Vector3(-10.5f, -1.2f, -.4f), 50f, new Vector3(-23f, -1.6f, -6f));
            k.Sound("chest_open", .2f, .6f); k.Sound("mm_laugh", 3.4f, .8f); k.Sound("shard_bounce", 5.9f, .7f); k.Sound("shard_bounce", 6.45f, .6f); k.Sound("shard_roll", 6.6f, .6f);
            k.Sound("splash", 11.6f, .6f); k.Sound("mm_grunt", 11.3f, .8f); k.Sound("splash", 12.8f, .9f); k.Sound("splash", 15.8f, .8f); k.Sound("splash", 16.2f, .8f); k.Sound("splash", 16.7f, .6f);
            k.Save();
            NecklaceModel.Tint = IceAgeCast.shard;
        }

        // the green shard on the sea floor: a swirling green vortex opens beyond it and pulls everyone in, Muscle Man too
        public static void SeaVortex()
        {
            DataAssets.Ensure(); lastQ.Clear();
            var c = PirateCast; NecklaceModel.Tint = c.shard;
            const float L = 9f;
            var k = new Kit("SeaVortex", L);
            var dan = Cast0(k, "Dan", c.dan); var abi = Cast0(k, "Abi", c.abi); var kira = Cast0(k, "Kira", c.kira); var mm = MuscleMan(k, "Muscle Man");
            var eye = new Vector3(0, .8f, 4f);
            Spiral(k, "Sea vortex", eye, Quaternion.identity, GreenSwirl, 5f, .3f, 2.8f, L, 160f, new Color(.1f, .45f, .35f), 6, true);
            Lamp(k, "Vortex light", eye + new Vector3(0, 0, -1.5f), new Color(.4f, 1f, .6f), 18f, 3.5f, .3f, L);
            foreach (var (a, from, ph, clip, t0) in new[] { (dan, new Vector3(-1.4f, .2f, -1f), 0f, "swim", 3.4f), (abi, new Vector3(1.4f, .3f, -1.1f), 1.3f, "swim", 3.7f), (kira, new Vector3(0, -.2f, -1.4f), 2.4f, "swim", 3.9f), (mm, new Vector3(.3f, .9f, -3.2f), 3.6f, "mm_struggle", 4.3f) })
            {
                var w = new List<(float, Vector3, float)> { (0f, from, 0f), (t0, from + new Vector3(0, .2f, .4f), 0f) };
                for (int i = 1; i <= 8; i++)
                {
                    float f = i / 8f, ang = ph + f * 7f, r = Mathf.Lerp(1.6f, .1f, f);
                    w.Add((Mathf.Lerp(t0, t0 + 2.6f, f), Vector3.Lerp(from, eye, f) + new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r, 0), f * 540f));
                }
                Path(k, a, w.ToArray());
                k.Key(a, "Body", "m_LocalScale", t0 + 1.6f, Vector3.one); k.Key(a, "Body", "m_LocalScale", t0 + 2.6f, Vector3.one * .05f); k.Key(a, "Body", "m_LocalScale", L, Vector3.one * .05f);
                if (a == kira) k.Clips(a, (KiraClip("swim", c.kira), 0f));
                else k.Clips(a, (a == mm ? PC(clip, "swim") : PeopleClip("swim"), 0f), (PeopleClip("fall"), t0 + .6f));
            }
            if (k.Necklaces.ContainsKey(dan)) PulseFrom(k, dan, 0f, 3f, L);
            k.Shot("The vortex opens", 0f, 3.4f, new Vector3(-1.5f, 1.4f, -6.5f), new Vector3(0, .8f, 2.5f), 52f, new Vector3(-1.2f, 1.3f, -5.8f));
            k.Shot("Pulled in", 3.4f, 6.6f, new Vector3(5.5f, 1.6f, .2f), new Vector3(0, .6f, 2.2f), 54f);
            k.Shot("Into the green", 6.6f, L, new Vector3(0, .9f, -1.8f), eye, 60f, eye + new Vector3(0, 0, -.6f));
            TintC(k, Color.white, (0f, 0f), (7.6f, 0f), (8.6f, 1f), (L, 1f));
            k.Sound("sea_vortex", .3f, .9f); k.Sound("bubbles", 3.4f, .6f);
            k.Save();
            NecklaceModel.Tint = IceAgeCast.shard;
        }
    }
}
