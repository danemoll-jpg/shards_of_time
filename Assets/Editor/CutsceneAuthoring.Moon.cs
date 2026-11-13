using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 13: the Moon's scenes (new: the film's Moon has the floating gag and the light puzzle; the game adds the landing and the finale).
    // MoonLanding: root = the rocket's spot on the dust (world (0, ~0, 24)); the party ends at local (0, 0, -16) (the arrival spot, world z 8).
    //   Earth is up to the north-east (world (260, 620, 1700)); the cabin set is far above (local y 300).
    // KiraFloats: root = the ridge's foot (Moon.FloatsAt), facing the ridge (225 deg).
    // WhiteShard: root = the rover's control panel on the plateau, facing the rover (180 deg: the rover at local z +6.4, its top ~3 m).
    // Every Moon actor wears its bubble helmet (Helmet.Attach at play time, Cutscenes.Play).
    public static partial class CutsceneAuthoring
    {
        static Cast MoonCast => new Cast { dan = Have("dan_moon", Have("dan_tomorrowland", Have("dan_steampunk", "dan_home"))), abi = Have("abi_moon", Have("abi_tomorrowland", Have("abi_steampunk", "abi_home"))), kira = Have("kira_moon", "kira"), shard = new Color(.95f, .97f, 1f) };
        static string MMMoon => Have("muscleman_moon", MMTL);
        public static void NecklaceWakesMoon() => NecklaceWakesIn("NecklaceWakesMoon", MoonCast);
        public static void ViolinistPlaysMoon() => ViolinistTeachesIn("ViolinistPlaysMoon", "Abi", MoonCast.abi, MoonCast);   // no move: his floaty tune (Abi listens)

        public const float MoonLandingLength = 17f;
        public static void MoonLanding()
        {
            DataAssets.Ensure(); lastQ.Clear();
            var c = MoonCast; NecklaceModel.Tint = c.shard;
            const float L = MoonLandingLength;
            var k = new Kit("MoonLanding", L);
            // the rocket comes down on its flame (0-4.6) and settles in a puff of dust
            var rk = k.Holder("Rocket", Vector3.zero, 0);
            var rm = StaticIn("rocket", rk.transform, new Vector3(0, 70f, 0), 0, 24f); string rp = rm ? rm.name : "Rocket";
            if (!rm) Placeholders.Prim(PrimitiveType.Cylinder, rk.transform, new Vector3(0, 82f, 0), new Vector3(4.8f, 12f, 4.8f), Placeholders.Mat(new Color(.85f, .87f, .92f)), name: "Rocket");
            k.Key(rk, rp, "m_LocalPosition", 0f, new Vector3(0, 70f, 0)); k.Key(rk, rp, "m_LocalPosition", 3.2f, new Vector3(0, 9f, 0)); k.Key(rk, rp, "m_LocalPosition", 4.6f, Vector3.zero); k.Key(rk, rp, "m_LocalPosition", L, Vector3.zero);
            var fl = k.Holder("Landing flame", Vector3.zero, 0);
            Placeholders.Prim(PrimitiveType.Sphere, fl.transform, Vector3.zero, new Vector3(2.4f, 7f, 2.4f), Placeholders.Mat(new Color(1f, .95f, .85f), "Glow"), name: "Flame");
            k.Key(fl, "Flame", "m_LocalPosition", 0f, new Vector3(0, 68f, 0)); k.Key(fl, "Flame", "m_LocalPosition", 3.2f, new Vector3(0, 7f, 0)); k.Key(fl, "Flame", "m_LocalPosition", 4.6f, new Vector3(0, -1.5f, 0));
            k.Key(fl, "Flame", "m_LocalScale", 0f, new Vector3(2.4f, 7f, 2.4f)); k.Key(fl, "Flame", "m_LocalScale", 4.2f, new Vector3(2.4f, 4f, 2.4f)); k.Key(fl, "Flame", "m_LocalScale", 4.7f, Vector3.one * .01f);
            k.Show(fl, 0f, 4.8f);
            Burst(k, "Dust ring", new Vector3(0, .5f, 0), new Color(.72f, .72f, .74f), 4.4f, 9f, 22, 2.6f, .25f, "Lit");
            Burst(k, "Dust puff", new Vector3(0, 1.2f, 0), new Color(.8f, .8f, .82f), 4.5f, 4f, 10, 2f, .6f, "Lit");
            // the cabin (5-8): the four in the rocket, helmets on (they click), a porthole of stars, Earth through it
            var cab = k.Holder("Cabin", new Vector3(0, 300f, 0), 0);
            Placeholders.Prim(PrimitiveType.Cube, cab.transform, new Vector3(0, -.1f, 0), new Vector3(6f, .2f, 6f), Placeholders.Mat(new Color(.82f, .84f, .9f)), name: "Floor");
            Placeholders.Prim(PrimitiveType.Cube, cab.transform, new Vector3(0, 1.6f, 2.2f), new Vector3(6f, 3.4f, .2f), Placeholders.Mat(new Color(.95f, .92f, .86f)), name: "Wall");
            Placeholders.Prim(PrimitiveType.Cylinder, cab.transform, new Vector3(0, 1.8f, 2.08f), new Vector3(1.2f, .02f, 1.2f), Placeholders.Mat(new Color(.08f, .12f, .3f), "Glow"), name: "Porthole").transform.localRotation = Quaternion.Euler(90, 0, 0);
            Placeholders.Prim(PrimitiveType.Sphere, cab.transform, new Vector3(.25f, 2.0f, 2.05f), Vector3.one * .3f, Placeholders.Mat(new Color(.35f, .6f, 1f), "Glow"), name: "Earth in the porthole");
            for (int i = 0; i < 6; i++) Placeholders.Prim(PrimitiveType.Cube, cab.transform, new Vector3(-2.4f + i * .9f, 2.8f, 2.08f), new Vector3(.18f, .1f, .05f), Placeholders.Mat(i % 2 == 0 ? new Color(1f, .4f, .8f) : new Color(.3f, 1f, .9f), "Glow"), name: "Panel light");
            k.Show(cab, 4.8f, 8.2f);
            var dan = Cast0(k, "Dan", c.dan); var abi = Cast0(k, "Abi", c.abi); var kira = Cast0(k, "Kira", c.kira); var mm = TLMM(k, "Muscle Man", MMMoon);
            var inCab = new[] { new Vector3(-.9f, 300f, .4f), new Vector3(.8f, 300f, .5f), new Vector3(0, 300f, -.3f), new Vector3(1.9f, 300f, -.2f) };
            // the hatch (8-9): on the rocket's south side at 7 m; the ladder; then down in slow floaty bounds (9-12.5) to the dust
            var hatch = k.Holder("Hatch", new Vector3(0, 7.2f, -2.4f), 0);
            Placeholders.Prim(PrimitiveType.Cube, hatch.transform, Vector3.zero, new Vector3(1.4f, 2.0f, .1f), Placeholders.Mat(new Color(.7f, .72f, .78f)), name: "Door");
            k.Key(hatch, "Door", "m_LocalPosition", 0f, Vector3.zero); k.Key(hatch, "Door", "m_LocalPosition", 8.2f, Vector3.zero); k.Key(hatch, "Door", "m_LocalPosition", 9.0f, new Vector3(1.5f, 0, -.2f));
            Lamp(k, "Hatch light", new Vector3(0, 7.2f, -3f), new Color(1f, .9f, .75f), 8f, 2f, 8.3f, 12.5f);
            var ends = new[] { new Vector3(-.9f, 0, -15.4f), new Vector3(.9f, 0, -15.6f), new Vector3(0, 0, -16.4f), new Vector3(2.2f, 0, -14.6f) };
            var actors = new[] { dan, abi, kira, mm };
            for (int i = 0; i < 4; i++)
            {
                var a = actors[i]; float d0 = 9.0f + i * .45f; var top = new Vector3(0, 6.3f, -2.9f);
                var w = new List<(float, Vector3, float)> { (0f, inCab[i], 0f), (8.2f, inCab[i], 0f), (8.25f, top, 180f), (d0, top, 180f) };
                // three slow bounds down and away from the rocket
                var p0 = top; var p1 = new Vector3(ends[i].x * .4f, 0, -6f); var p2 = new Vector3(ends[i].x * .8f, 0, -11f); var p3 = ends[i];
                foreach (var (from, to, t0, t1) in new[] { (p0, p1, d0, d0 + 1.3f), (p1, p2, d0 + 1.3f, d0 + 2.3f), (p2, p3, d0 + 2.3f, d0 + 3.1f) })
                    for (int s = 1; s <= 4; s++) { float f = s / 4f; w.Add((Mathf.Lerp(t0, t1, f), Vector3.Lerp(from, to, f) + Vector3.up * Mathf.Sin(f * Mathf.PI) * (from.y > 1f ? 1.4f : 1.6f), 180f)); }
                w.Add((12.6f + i * .1f, p3, 0f));
                if (a == mm) { w.Add((13.4f, p3, 0f)); w.Add((14.5f, p3 + new Vector3(0, 6.5f, 1.2f), 0f)); w.Add((15.6f, p3 + new Vector3(0, 0, 2.4f), 0f)); w.Add((L, p3 + new Vector3(0, 0, 2.4f), 0f)); }
                else w.Add((L, p3, 0f));
                Path(k, a, w.ToArray());
                Appear(k, a, 4.8f, L, L);
            }
            k.Clips(dan, (PeopleClip("idle"), 0f), (PeopleClip("look"), 5.4f), (PeopleClip("jump"), 9.0f), (PeopleClip("fall"), 9.6f), (PC("land", "idle"), 12.0f), (PeopleClip("look"), 12.6f), (PeopleClip("surprised"), 13.6f));
            k.Clips(abi, (PeopleClip("idle"), 0f), (PeopleClip("cheer"), 5.6f), (PeopleClip("jump"), 9.45f), (PeopleClip("fall"), 10.0f), (PC("land", "idle"), 12.4f), (PeopleClip("look"), 12.8f), (PeopleClip("laugh") ?? PeopleClip("cheer"), 15.2f));
            k.Clips(kira, (KiraClip("idle", c.kira), 0f), (KiraClip("jump", c.kira), 9.9f), (KiraClip("fall", c.kira), 10.4f), (KiraClip("land", c.kira), 12.8f), (KiraClip("idle", c.kira), 13.4f));
            k.Clips(mm, (PeopleClip("idle"), 0f), (PC("mm_flex", "cheer"), 5.8f), (PeopleClip("jump"), 10.35f), (PeopleClip("fall"), 10.8f), (PC("land", "idle"), 13.3f), (PC("mm_leap", "jump"), 13.4f), (PeopleClip("fall"), 14.4f), (PeopleClip("laugh") ?? PC("mm_cheer", "cheer"), 15.6f));
            for (int i = 0; i < 4; i++) Burst(k, "Footfall " + i, ends[i] + Vector3.up * .1f, new Color(.78f, .78f, .8f), 12.1f + i * .4f, 1.2f, 6, 1.2f, .3f, "Lit");
            Burst(k, "Big landing", ends[3] + new Vector3(0, .1f, 2.4f), new Color(.8f, .8f, .82f), 15.5f, 2.6f, 10, 1.6f, .3f, "Lit");
            if (k.Necklaces.ContainsKey(dan)) PulseFrom(k, dan, 13f, L, L);
            var earthDir = new Vector3(260f, 620f, 1676f).normalized;
            k.Shot("The rocket comes down", 0f, 4.8f, new Vector3(-26f, 2.5f, -30f), new Vector3(0, 14f, 0), 52f, new Vector3(-24f, 2f, -28f), new Vector3(0, 3f, 0));
            k.Shot("Helmets on", 4.8f, 8.2f, new Vector3(.4f, 301.7f, -3.6f), new Vector3(.3f, 301.3f, 0), 50f, new Vector3(.4f, 301.6f, -3.2f));
            k.Shot("The hatch opens", 8.2f, 9.4f, new Vector3(4f, 3f, -12f), new Vector3(0, 6.6f, -2.6f), 40f);
            k.Shot("Bounding down", 9.4f, 12.8f, new Vector3(9f, 1.6f, -14f), new Vector3(0, 3f, -8f), 56f, new Vector3(8f, 1.4f, -15f), new Vector3(.5f, 1.4f, -13f));
            k.Shot("Earth rises behind them", 12.8f, L, new Vector3(-1.5f, .5f, -25f), new Vector3(0, 1.5f, -16f) + earthDir * 40f, 62f, new Vector3(-1.2f, .6f, -23.5f));
            k.Sound("rocket_landing", 0.4f, 1f); k.Sound("radio_beep", 5.0f, .6f);
            for (int i = 0; i < 4; i++) k.Sound("helmet_click", 5.4f + i * .35f, .7f);
            k.Sound("airlock_hiss", 8.2f, .7f); k.Sound("footstep_dust", 12.1f, .6f); k.Sound("footstep_dust", 12.6f, .6f); k.Sound("mm_laugh", 15.5f, .8f);
            Look(k, abi, dan, 5.6f, 7.6f); Look(k, dan, mm, 14.6f, L); Look(k, abi, mm, 14.7f, L); Look(k, kira, mm, 14.8f, L);
            k.Save();
            NecklaceModel.Tint = IceAgeCast.shard;
        }

        public const float KiraFloatsLength = 9f;
        public static void KiraFloats()
        {
            DataAssets.Ensure(); lastQ.Clear();
            var c = MoonCast; NecklaceModel.Tint = c.shard;
            const float L = KiraFloatsLength;
            var k = new Kit("KiraFloats", L);
            var dan = Cast0(k, "Dan", c.dan); var abi = Cast0(k, "Abi", c.abi); var kira = Cast0(k, "Kira", c.kira); var mm = TLMM(k, "Muscle Man", MMMoon);
            // Kira takes a little leap (0.6) and keeps going up, paddling (1-5.4); Abi bounds after her and catches her ankle (5.4); pulls her down (7)
            Path(k, kira, (0f, new Vector3(0, 0, 1.2f), 0f), (.6f, new Vector3(0, 0, 1.2f), 0f), (1.4f, new Vector3(0, 1.1f, 1.9f), 0f), (3.0f, new Vector3(.1f, 2.4f, 2.3f), 15f), (5.4f, new Vector3(.2f, 3.6f, 2.5f), -10f),
                (5.6f, new Vector3(.2f, 3.5f, 2.5f), -10f), (7.0f, new Vector3(.35f, 1.15f, 2.2f), 180f), (L, new Vector3(.35f, 1.15f, 2.2f), 180f));
            k.Clips(kira, (KiraClip("idle", c.kira), 0f), (KiraClip("jump", c.kira), .6f), (KiraClip("swim", c.kira) ?? KiraClip("run", c.kira), 1.3f), (KiraClip("hurt", c.kira), 5.5f), (KiraClip("idle", c.kira), 7.0f));
            Path(k, abi, (0f, new Vector3(1.5f, 0, -.8f), 0f), (3.4f, new Vector3(1.5f, 0, -.8f), 0f), (4.2f, new Vector3(1f, 0, 1.0f), -20f), (5.3f, new Vector3(.4f, 2.0f, 2.1f), 0f), (6.4f, new Vector3(.35f, 0, 1.7f), 0f), (L, new Vector3(.35f, 0, 1.7f), 0f));
            k.Clips(abi, (PeopleClip("look"), 0f), (PeopleClip("surprised"), 2.4f), (PeopleClip("run"), 3.4f), (PeopleClip("jump"), 4.4f), (PeopleClip("offer") ?? PeopleClip("cheer"), 5.4f), (PeopleClip("laugh") ?? PeopleClip("cheer"), 7.2f));
            Path(k, dan, (0f, new Vector3(-1.4f, 0, -.6f), 10f), (L, new Vector3(-1.4f, 0, -.6f), 10f));
            k.Clips(dan, (PeopleClip("idle"), 0f), (PeopleClip("look"), 1.4f), (PeopleClip("surprised"), 3.0f), (PeopleClip("laugh") ?? PeopleClip("cheer"), 7.2f));
            Path(k, mm, (0f, new Vector3(-.4f, 0, -2.6f), 0f), (L, new Vector3(-.4f, 0, -2.6f), 0f));
            k.Clips(mm, (PeopleClip("idle"), 0f), (PeopleClip("surprised"), 2.0f), (PeopleClip("laugh") ?? PC("mm_cheer", "cheer"), 6.8f));
            Burst(k, "Little leap", new Vector3(0, .1f, 1.2f), new Color(.78f, .78f, .8f), .6f, .8f, 6, 1f, .3f, "Lit");
            k.Shot("A little leap", 0f, 1.6f, new Vector3(2.6f, .7f, 4.4f), new Vector3(0, .5f, 1.4f), 44f);
            k.Shot("Up and up", 1.6f, 4.0f, new Vector3(-1.4f, 1.0f, 6f), new Vector3(.1f, 2.6f, 2.3f), 46f, new Vector3(-1.2f, 1.6f, 5.6f), new Vector3(.2f, 3.4f, 2.4f));
            k.Shot("Abi catches her", 4.0f, 6.6f, new Vector3(3.6f, 2.2f, 5.6f), new Vector3(.3f, 2.4f, 2.1f), 48f);
            k.Shot("Back down", 6.6f, L, new Vector3(-3.4f, 1.6f, 5.6f), new Vector3(0, 1.1f, .6f), 50f);
            k.Sound("kira_float", 1.2f, .7f); k.Sound("critter_boing", .6f, .4f); k.Sound("jump", 4.3f, .5f); k.Sound("mm_laugh", 7.0f, .6f);
            Look(k, abi, kira, 1.6f, 6.4f); Look(k, dan, kira, 1.8f, 7f); Look(k, mm, kira, 2f, 7f); Look(k, kira, abi, 7.2f, L);
            k.Save();
            NecklaceModel.Tint = IceAgeCast.shard;
        }

        public const float WhiteShardLength = 15f;
        public static void WhiteShard()
        {
            DataAssets.Ensure(); lastQ.Clear();
            var c = MoonCast; NecklaceModel.Tint = c.shard;
            const float L = WhiteShardLength;
            var k = new Kit("WhiteShard", L);
            var dan = Cast0(k, "Dan", c.dan); var abi = Cast0(k, "Abi", c.abi); var kira = Cast0(k, "Kira", c.kira); var mm = TLMM(k, "Muscle Man", MMMoon);
            var top = new Vector3(0, 3.4f, 6.4f);
            // the shard answers (0-3): it pulses on the rover; it rises and floats to Dan (3-5.4)
            var sh = Ball(k, "White shard", top, Color.white, .32f);
            k.Key(sh, "Glow", "m_LocalPosition", 0f, Vector3.zero); k.Key(sh, "Glow", "m_LocalPosition", 3.0f, Vector3.zero); k.Key(sh, "Glow", "m_LocalPosition", 4.0f, new Vector3(0, 1.4f, -2.6f)); k.Key(sh, "Glow", "m_LocalPosition", 5.4f, new Vector3(.3f, -2.1f, -7.2f));
            foreach (var (t, s) in new[] { (0f, .32f), (.6f, .5f), (1.2f, .32f), (1.8f, .55f), (2.4f, .32f), (3f, .6f), (5.4f, .2f) }) k.Key(sh, "Glow", "m_LocalScale", t, Vector3.one * s);
            k.Show(sh, 0f, 5.5f);
            Lamp(k, "Shard light", top + Vector3.up * .5f, Color.white, 10f, 3f, 0f, 5.4f);
            Path(k, abi, (0f, new Vector3(0, 0, -.6f), 0f), (L, new Vector3(0, 0, -.6f), 0f));
            k.Clips(abi, (PeopleClip("press") ?? PeopleClip("offer") ?? PeopleClip("idle"), 0f), (PeopleClip("surprised"), 2.4f), (PeopleClip("look"), 3.4f), (PeopleClip("cheer"), 7.6f));
            Path(k, dan, (0f, new Vector3(.3f, 0, -2.6f), 0f), (L, new Vector3(.3f, 0, -2.6f), 0f));
            k.Clips(dan, (PeopleClip("look"), 0f), (PeopleClip("offer") ?? PeopleClip("idle"), 4.6f), (PeopleClip("cheer"), 7.4f));
            Path(k, kira, (0f, new Vector3(-1.2f, 0, -1.8f), 20f), (L, new Vector3(-1.2f, 0, -1.8f), 20f));
            k.Clips(kira, (KiraClip("idle", c.kira), 0f), (KiraClip("jump", c.kira), 7.6f), (KiraClip("idle", c.kira), 8.4f));
            Path(k, mm, (0f, new Vector3(1.8f, 0, -3.4f), -20f), (L, new Vector3(1.8f, 0, -3.4f), -20f));
            k.Clips(mm, (PeopleClip("idle"), 0f), (PeopleClip("surprised"), 3.2f), (PC("mm_cheer", "cheer"), 7.8f));
            // every shard in Dan's necklace lights up together (5.4-8): eight colours round his chest, then the white light fills the Moon (8-12.5)
            var cols = new[] { new Color(.3f, .6f, 1f), new Color(1f, .3f, .25f), new Color(1f, .85f, .35f), new Color(.65f, .4f, .95f), new Color(.35f, 1f, .5f), new Color(.2f, .95f, .85f), new Color(1f, .35f, .82f), Color.white };
            var neck = new Vector3(.3f, 1.35f, -2.35f);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2 / 8; var b = Ball(k, "Necklace shard " + i, neck, cols[i], .01f);
                var ring = new Vector3(Mathf.Cos(a) * .28f, Mathf.Sin(a) * .28f, -.05f);
                k.Key(b, "Glow", "m_LocalPosition", 5.4f, Vector3.zero); k.Key(b, "Glow", "m_LocalPosition", 6.0f + i * .12f, ring); k.Key(b, "Glow", "m_LocalPosition", 9f, ring * 1.4f);
                k.Key(b, "Glow", "m_LocalScale", 5.4f, Vector3.one * .01f); k.Key(b, "Glow", "m_LocalScale", 6.0f + i * .12f, Vector3.one * .12f); k.Key(b, "Glow", "m_LocalScale", 9f, Vector3.one * .22f);
                k.Show(b, 5.4f, 12f);
            }
            if (k.Necklaces.ContainsKey(dan)) PulseFrom(k, dan, 5.4f, L, L);
            var glow = k.Holder("Moon light", neck, 0);
            Placeholders.Prim(PrimitiveType.Sphere, glow.transform, Vector3.zero, Vector3.one * .1f, Placeholders.Mat(new Color(1f, 1f, 1f), "Glow"), name: "Glow");
            k.Key(glow, "Glow", "m_LocalScale", 8f, Vector3.one * .1f); k.Key(glow, "Glow", "m_LocalScale", 10.5f, Vector3.one * 60f); k.Key(glow, "Glow", "m_LocalScale", 12.5f, Vector3.one * 600f);
            k.Show(glow, 8f, L);
            Lamp(k, "Brilliant light", neck + Vector3.up * 2f, Color.white, 200f, 8f, 8f, L);
            k.Shot("Abi at the panel", 0f, 3.0f, new Vector3(1.6f, 1.8f, -2.8f), new Vector3(0, 1.4f, 1.8f), 46f);
            k.Shot("The shard answers", 3.0f, 5.4f, new Vector3(-3.6f, 2.4f, -3f), new Vector3(0, 2.6f, 3f), 50f, new Vector3(-3f, 2.2f, -3.4f), new Vector3(.2f, 1.6f, -2f));
            k.Shot("Every shard lights up", 5.4f, 8.2f, new Vector3(1.9f, 1.55f, -1.3f), neck, 34f, new Vector3(1.6f, 1.5f, -1.5f));   // (in front of Dan, from the rover's side)
            k.Shot("Light fills the Moon", 8.2f, L, new Vector3(-26f, 8f, -38f), new Vector3(0, 2f, 0), 60f, new Vector3(-34f, 14f, -50f), new Vector3(0, 6f, 0));
            TintC(k, Color.white, (0f, 0f), (9.5f, 0f), (11.5f, .6f), (13.5f, 1f), (L, 1f));
            k.Sound("rover_tone_1", .3f, .6f); k.Sound("rover_tone_3", .7f, .6f); k.Sound("rover_tone_4", 1.1f, .6f); k.Sound("white_chime", 2.4f, .9f); k.Sound("story", 5.3f, .7f); k.Sound("white_swell", 7.6f, 1f);
            Look(k, abi, sh, 2.4f, 5.4f); Look(k, dan, sh, 3f, 5.6f); Look(k, mm, dan, 6f, 9f); Look(k, kira, dan, 6.2f, 9f); Look(k, abi, dan, 6f, 9f);
            k.Save();
            NecklaceModel.Tint = IceAgeCast.shard;
        }
    }
}
