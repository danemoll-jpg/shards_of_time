using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 12: Tomorrowland's scenes.
    // TomorrowlandArrival (new: the film has none). Root = the arrival spot on the avenue (world (0, 0, 10)): the avenue runs to +z, the
    //   shops start at local x +-14, the greeter rolls in from local (6, 0, 14).
    // RocketLaunch (Dan's clip 07-Tomorrowland_RocketLaunch.mp4, 6 s, extended): root = the rocket's pad in the launch hall (world (0, 0, 298));
    //   the fuel socket at local (0, 0, -11.5), the hall's door at local z -38, its wall radius 30 round local (0, 0, -8).
    public static partial class CutsceneAuthoring
    {
        static Cast TLCast => new Cast { dan = Have("dan_tomorrowland", Have("dan_steampunk", "dan_home")), abi = Have("abi_tomorrowland", Have("abi_steampunk", "abi_home")), kira = Have("kira_robot", "kira"), shard = new Color(1f, .35f, .82f) };
        static string MMTL => Have("muscleman_tomorrowland", Have("muscleman_steampunk", MMModel));
        static readonly Color[] PinkSwirl = { new Color(1f, .4f, .85f), new Color(1f, .75f, .95f), new Color(.55f, .25f, .7f), new Color(.4f, 1f, .95f) };
        public static void NecklaceWakesTomorrowland() => NecklaceWakesIn("NecklaceWakesTomorrowland", TLCast);
        public static void ViolinistTeachesTomorrowland() => ViolinistTeachesIn("ViolinistTeachesTomorrowland", "Abi", TLCast.abi, TLCast);   // Abi's air dash

        static GameObject TLMM(Kit k, string n, string model)
        {
            var mm = Cast0(k, n, model);
            if (!k.Models.ContainsKey(mm)) Placeholders.Prim(PrimitiveType.Capsule, mm.transform, new Vector3(0, 1f, 0), new Vector3(1f, 1f, .7f), Placeholders.Mat(new Color(.75f, .78f, .85f)), name: "Body");
            return mm;
        }

        // a static world model (Resources/World/<name>.fbx + _albedo) in a set, with a saved material (prefab-safe), scaled to `height`
        static GameObject StaticIn(string name, Transform parent, Vector3 localPos, float yaw, float height)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Resources/World/{name}.fbx");
            if (!src) { Debug.LogWarning($"[Cutscene] no World/{name}.fbx"); return null; }
            string mp = $"Assets/Resources/World/{name}_cutscene.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(mp);
            if (!mat)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Resources/World/{name}_albedo.png");
                if (tex) { mat.SetTexture("_BaseMap", tex); mat.mainTexture = tex; }
                mat.SetFloat("_Smoothness", .35f); AssetDatabase.CreateAsset(mat, mp);
            }
            var go = (GameObject)Object.Instantiate(src); go.name = name;
            foreach (var r in go.GetComponentsInChildren<Renderer>()) { var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = mat; r.sharedMaterials = ms; }
            go.transform.SetParent(parent, false); go.transform.localPosition = localPos; go.transform.localRotation = Quaternion.Euler(0, yaw, 0) * src.transform.localRotation;
            var b = new Bounds(go.transform.position, Vector3.zero); bool any = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>()) { if (!any) b = r.bounds; else b.Encapsulate(r.bounds); any = true; }
            if (any && b.size.y > .001f) go.transform.localScale *= height / b.size.y;
            return go;
        }

        public const float TomorrowlandArrivalLength = 16f;
        public static void TomorrowlandArrival()
        {
            DataAssets.Ensure(); lastQ.Clear();
            var c = TLCast; NecklaceModel.Tint = c.shard;
            const float L = TomorrowlandArrivalLength;
            var k = new Kit("TomorrowlandArrival", L);
            // the teal vortex over the avenue at dusk (0-3.4); the four tumble out of it and land (land_hard)
            var eye = new Vector3(0, 22f, 12f);
            Spiral(k, "Teal vortex", eye, Quaternion.Euler(90, 0, 0), TealSwirl, 9f, 0f, 1.2f, 3.6f, 160f, new Color(.05f, .45f, .45f), 6, true, 3.0f);
            Lamp(k, "Vortex light", eye + Vector3.down * 4f, new Color(.3f, 1f, .9f), 40f, 2.6f, 0f, 3.6f);
            var spots = new[] { new Vector3(-.9f, 0, .4f), new Vector3(.9f, 0, .2f), new Vector3(0, 0, -.6f), new Vector3(.3f, 0, -2f) };
            // each hero twice: the steampunk outfit until the shimmer (4.6 s), then the retro-future one (the same path)
            string Old(string hero) => hero == "Dan" ? Have("dan_steampunk", c.dan) : hero == "Abi" ? Have("abi_steampunk", c.abi) : hero == "Kira" ? Have("kira_mechanical", c.kira) : Have("muscleman_steampunk", MMTL);
            var heroes = new[] { ("Dan", c.dan), ("Abi", c.abi), ("Kira", c.kira), ("Muscle Man", MMTL) };
            var now = new Dictionary<string, GameObject>(); var before = new Dictionary<string, GameObject>();
            for (int i = 0; i < heroes.Length; i++)
            {
                var (hero, model) = heroes[i]; bool mm = hero == "Muscle Man";
                var a0 = mm ? TLMM(k, hero + " (before)", Old(hero)) : Cast0(k, hero + " (before)", Old(hero));
                var a1 = mm ? TLMM(k, hero, model) : Cast0(k, hero, model);
                before[hero] = a0; now[hero] = a1;
                foreach (var a in new[] { a0, a1 })
                {
                    float d = i * .25f; var s = spots[i]; Vector3 from = eye + s * .3f + Vector3.down * 2f, to = s + Vector3.up * .3f;
                    for (float t = .9f + d; t <= 2.7f + d + .001f; t += .2f)
                    {
                        float f = Mathf.InverseLerp(.9f + d, 2.7f + d, t);
                        k.Key(a, "Body", "m_LocalPosition", t, Vector3.Lerp(from, to, f) + new Vector3(Mathf.Sin(f * 9f + i) * .3f, 0, 0));
                        KeyRot(k, a, "Body", t, Quaternion.Euler(60f * Mathf.Sin(f * 6f + i) * (1 - f), i * 40f + f * 430f, f * 140f * (1 - f)));
                    }
                    Path(k, a, (2.75f + d, s, 0f), (L, s, 0f));
                }
                Appear(k, a0, .9f + i * .25f, 4.6f, L); Appear(k, a1, 4.6f, L, L);
                if (hero == "Kira")
                {
                    k.Clips(a0, (KiraClip("fall", Old("Kira")), 0f), (KiraClip("land", Old("Kira")), 2.8f), (KiraClip("idle", Old("Kira")), 3.4f));
                    k.Clips(a1, (KiraClip("idle", model), 0f), (KiraClip("jump", model), 5.0f), (KiraClip("idle", model), 5.8f), (KiraClip("run", model), 12.6f), (KiraClip("idle", model), 13.4f));
                }
                else if (mm)
                {
                    k.Clips(a0, (PeopleClip("fall"), 0f), (PC("mm_fall", "fall_back"), 2.8f), (PC("mm_getup", "land_hard"), 3.6f));
                    k.Clips(a1, (PeopleClip("idle"), 0f), (PeopleClip("surprised"), 6.4f), (PC("mm_cheer", "cheer"), 8.2f), (PeopleClip("idle"), 9.4f), (PeopleClip("laugh") ?? PeopleClip("cheer"), 11.6f));
                }
                else
                {
                    k.Clips(a0, (PeopleClip("fall"), 0f), (PC("land_hard", "land"), 2.8f), (PeopleClip("idle"), 3.6f));
                    k.Clips(a1, (PeopleClip("idle"), 0f), (PeopleClip("look"), 5.0f), (PeopleClip("idle"), 6.2f), (PeopleClip("surprised"), 10.4f), (PeopleClip("idle"), 11.2f));
                }
            }
            // the shimmer (4.4-5.0): sparkles round each, a pink-white flash
            foreach (var (hero, _) in heroes) { var p = spots[System.Array.FindIndex(heroes, x => x.Item1 == hero)]; Burst(k, "Shimmer " + hero, p + Vector3.up * (hero == "Kira" ? .3f : 1f), new Color(1f, .75f, .95f), 4.35f, hero == "Kira" ? .8f : 1.6f, 12, .9f, .5f); }
            TintC(k, new Color(1f, .8f, .95f), (0f, 0f), (4.3f, 0f), (4.6f, .55f), (5.0f, 0f), (L, 0f));
            // a flying car swoops over (6-8.6); Muscle Man gapes, then is delighted
            var car = k.Holder("Flying car", Vector3.zero, 0);
            var cm = StaticIn("flying_car", car.transform, new Vector3(-30f, 9f, 18f), 90f, 1.42f);
            string cp = cm ? cm.name : "Car";
            if (!cm) Placeholders.Prim(PrimitiveType.Capsule, car.transform, new Vector3(-30f, 9f, 18f), new Vector3(1.6f, 1.6f, .9f), Placeholders.Mat(new Color(1f, .7f, .8f)), name: "Car").transform.localRotation = Quaternion.Euler(0, 0, 90);
            k.Key(car, cp, "m_LocalPosition", 0f, new Vector3(-30f, 9f, 18f)); k.Key(car, cp, "m_LocalPosition", 6.0f, new Vector3(-30f, 9f, 18f));
            k.Key(car, cp, "m_LocalPosition", 7.3f, new Vector3(0f, 7.2f, 8f)); k.Key(car, cp, "m_LocalPosition", 8.6f, new Vector3(32f, 10f, 2f));
            k.Show(car, 5.9f, 8.7f);
            Burst(k, "Car jets", new Vector3(0f, 6.6f, 8f), new Color(.6f, 1f, .95f), 7.2f, .8f, 8, .6f, -.2f);
            // the greeter robot rolls up (9-12.6): "Your rocket is ready for departure!" and points at the Rocket Terminal sign
            var gr = k.Holder("Greeter robot", Vector3.zero, 0);
            var gm = StaticIn("robot_greeter", gr.transform, new Vector3(6f, 0, 14f), 200f, 1.35f);
            string gp = gm ? gm.name : "Greeter";
            if (!gm) Placeholders.Prim(PrimitiveType.Cylinder, gr.transform, new Vector3(6f, .7f, 14f), new Vector3(.6f, .7f, .6f), Placeholders.Mat(new Color(.8f, .82f, .88f)), name: "Greeter");
            else gm.AddComponent<RigidParts>().wave = 1f;
            k.Key(gr, gp, "m_LocalPosition", 0f, new Vector3(6f, 0, 14f)); k.Key(gr, gp, "m_LocalPosition", 9.0f, new Vector3(6f, 0, 14f)); k.Key(gr, gp, "m_LocalPosition", 10.6f, new Vector3(2.4f, 0, 3.4f));
            var gq = gm ? gm.transform.localRotation : Quaternion.identity;
            k.KeyQ(gr, gp, 0f, gq); k.KeyQ(gr, gp, 10.6f, gq); k.KeyQ(gr, gp, 11.8f, Quaternion.Euler(0, 140f, 0) * gq); k.KeyQ(gr, gp, L, Quaternion.Euler(0, 140f, 0) * gq);
            // the Rocket Terminal's sign far up the avenue (the greeter points at it)
            var sign = k.Holder("Terminal sign", new Vector3(0, 10f, 226f), 0);
            Placeholders.Prim(PrimitiveType.Cube, sign.transform, Vector3.zero, new Vector3(18f, 3f, .3f), Placeholders.Mat(new Color(1f, .4f, .85f), "Glow"), name: "Glow");
            // the necklace glows pink (13-16)
            if (k.Necklaces.ContainsKey(now["Dan"])) PulseFrom(k, now["Dan"], 13.2f, L, L);
            Lamp(k, "Necklace glow", spots[0] + new Vector3(0, 1.05f, .35f), c.shard, 2.2f, .7f, 13.2f, L);   // (soft: it over-lit his face)
            // shots
            k.Shot("The vortex at dusk", 0f, 2.9f, new Vector3(4f, 1.6f, -14f), new Vector3(0, 14f, 10f), 58f, new Vector3(3f, 1.8f, -11f), new Vector3(0, 6f, 4f));
            k.Shot("Landed", 2.9f, 4.3f, new Vector3(2.8f, 1.5f, -4.2f), new Vector3(0, .6f, 0), 50f);   // (from the south: the avenue's shards are north)
            k.Shot("The shimmer", 4.3f, 6.0f, new Vector3(0, 1.6f, -5.6f), new Vector3(0, 1f, 0), 46f, new Vector3(0, 1.5f, -4.8f));
            k.Shot("A flying car!", 6.0f, 9.0f, new Vector3(-2.6f, 1.2f, -3.2f), new Vector3(-8f, 8f, 13f), 62f, new Vector3(-2.4f, 1.2f, -3f), new Vector3(10f, 8f, 6f));   // the camera follows the car over
            k.Shot("The greeter rolls up", 9.0f, 13.0f, new Vector3(-3f, 1.5f, -3.4f), new Vector3(2.4f, .9f, 4f), 50f, new Vector3(-2.6f, 1.4f, -2.6f), new Vector3(2.4f, .9f, 3.4f));
            k.Shot("The necklace glows pink", 13.0f, L, new Vector3(-1.4f, 1.5f, 2.6f), spots[0] + new Vector3(0, 1.25f, 0), 36f, new Vector3(-1.2f, 1.45f, 2.2f));
            k.Sound("portal", .2f, .5f); k.Sound("land_hard", 2.8f, .6f); k.Sound("shimmer", 4.35f, .7f); k.Sound("hovercar_whoosh", 6.6f, .8f);
            k.Sound("mm_laugh", 8.3f, .6f); k.Sound("robot_beep", 9.2f, .5f); k.Sound("greeter_ready", 10.8f, .9f); k.Sound("necklace", 13.2f, .5f);
            Look(k, now["Muscle Man"], car, 6.2f, 8.6f); Look(k, now["Dan"], gr, 10.4f, 12.8f); Look(k, now["Abi"], gr, 10.5f, 12.8f); Look(k, now["Kira"], gr, 10.6f, 12.6f);
            Look(k, now["Abi"], now["Dan"], 13.4f, L);
            k.Save();
            NecklaceModel.Tint = IceAgeCast.shard;
        }

        public const float RocketLaunchLength = 15f;
        public static void RocketLaunch()
        {
            DataAssets.Ensure(); lastQ.Clear();
            var c = TLCast; NecklaceModel.Tint = c.shard;
            const float L = RocketLaunchLength;
            var k = new Kit("RocketLaunch", L);
            var sock = new Vector3(0, 0, -11.5f);
            var dan = Cast0(k, "Dan", c.dan); var abi = Cast0(k, "Abi", c.abi); var kira = Cast0(k, "Kira", c.kira); var mm = TLMM(k, "Muscle Man", MMTL);
            // the rocket (Dan's model, 24 m) and its flame; the hall's dome halves (they slide apart for the launch)
            var rk = k.Holder("Rocket", Vector3.zero, 0);
            var rm = StaticIn("rocket", rk.transform, new Vector3(0, .4f, 0), 0, 24f);
            string rp = rm ? rm.name : "Rocket";
            if (!rm) Placeholders.Prim(PrimitiveType.Cylinder, rk.transform, new Vector3(0, 12f, 0), new Vector3(4.8f, 12f, 4.8f), Placeholders.Mat(new Color(.85f, .87f, .92f)), name: "Rocket");
            k.Key(rk, rp, "m_LocalPosition", 0f, new Vector3(0, .4f, 0)); k.Key(rk, rp, "m_LocalPosition", 9.6f, new Vector3(0, .4f, 0));
            k.Key(rk, rp, "m_LocalPosition", 11.5f, new Vector3(0, 9f, 0)); k.Key(rk, rp, "m_LocalPosition", L, new Vector3(0, 95f, 0));
            var fl = k.Holder("Engine flame", new Vector3(0, -.4f, 0), 0);
            Placeholders.Prim(PrimitiveType.Sphere, fl.transform, Vector3.zero, new Vector3(2.6f, 6f, 2.6f), Placeholders.Mat(new Color(1f, .96f, .85f), "Glow"), name: "Core");
            Placeholders.Prim(PrimitiveType.Sphere, fl.transform, new Vector3(0, -1.5f, 0), new Vector3(4f, 9f, 4f), Placeholders.Mat(new Color(1f, .62f, .25f), "Glow"), name: "Outer");
            foreach (var p in new[] { "Core", "Outer" })
            {
                k.Key(fl, p, "m_LocalScale", 0f, Vector3.one * .01f); k.Key(fl, p, "m_LocalScale", 8.4f, Vector3.one * .01f);
                k.Key(fl, p, "m_LocalScale", 9.4f, p == "Core" ? new Vector3(2.6f, 6f, 2.6f) : new Vector3(4f, 9f, 4f)); k.Key(fl, p, "m_LocalScale", L, p == "Core" ? new Vector3(2.8f, 9f, 2.8f) : new Vector3(4.4f, 14f, 4.4f));
            }
            k.Key(fl, "Core", "m_LocalPosition", 9.6f, Vector3.zero); k.Key(fl, "Core", "m_LocalPosition", 11.5f, new Vector3(0, 8.6f, 0)); k.Key(fl, "Core", "m_LocalPosition", L, new Vector3(0, 94.6f, 0));
            k.Key(fl, "Outer", "m_LocalPosition", 9.6f, new Vector3(0, -1.5f, 0)); k.Key(fl, "Outer", "m_LocalPosition", 11.5f, new Vector3(0, 7.1f, 0)); k.Key(fl, "Outer", "m_LocalPosition", L, new Vector3(0, 93f, 0));
            k.Show(fl, 8.3f, L);
            Lamp(k, "Ignition light", new Vector3(0, 2f, 0), new Color(1f, .75f, .45f), 60f, 6f, 8.6f, 11.6f);
            // the dome's halves (the world's dome is hidden for the scene): they slide apart (5.6-8)
            var hallC = new Vector3(0, 14f, -8f);
            foreach (float s in new[] { -1f, 1f })
            {
                var half = k.Holder(s < 0 ? "Dome west" : "Dome east", Vector3.zero, 0);
                var d = new GameObject("Half"); d.transform.SetParent(half.transform, false); d.transform.localPosition = hallC;
                d.AddComponent<MeshFilter>().sharedMesh = HalfDome(30.4f, 20f, s);
                d.AddComponent<MeshRenderer>().sharedMaterial = Placeholders.Mat(WorldArtColor("HallGold"));
                k.Key(half, "Half", "m_LocalPosition", 0f, hallC); k.Key(half, "Half", "m_LocalPosition", 9.4f, hallC); k.Key(half, "Half", "m_LocalPosition", 11.4f, hallC + new Vector3(s * 34f, -2f, 0));   // (Dan's clip: the hall stays closed until the ignition)
            }
            // the stars above the opened hall
            var stars = k.Holder("Stars", new Vector3(0, 0, 0), 0);
            for (int i = 0; i < 70; i++)
            {
                float a = i * 2.39996f, r = 40f + (i * 37 % 160);
                Placeholders.Prim(PrimitiveType.Sphere, stars.transform, new Vector3(Mathf.Cos(a) * r, 180f + (i * 53 % 90), Mathf.Sin(a) * r), Vector3.one * (.8f + (i % 3) * .5f), Placeholders.Mat(new Color(1f, .95f, .9f), "Glow"), name: "Star");
            }
            k.Show(stars, 9.5f, L);
            // the pink shard: from Dan's hands into the socket (0-3); the socket and the rocket's lights glow
            var sh = Ball(k, "Pink shard", sock + new Vector3(0, 1.3f, -.7f), c.shard, .25f);
            k.Key(sh, "Glow", "m_LocalPosition", 0f, Vector3.zero); k.Key(sh, "Glow", "m_LocalPosition", 1.6f, Vector3.zero); k.Key(sh, "Glow", "m_LocalPosition", 2.4f, new Vector3(0, -.1f, .7f));
            k.Show(sh, 0f, 3.2f);
            Burst(k, "Socket flash", sock + new Vector3(0, 1.2f, 0), c.shard, 2.4f, 1.6f, 14, .9f, .5f);
            Lamp(k, "Socket glow", sock + new Vector3(0, 1.5f, -.4f), c.shard, 8f, 3f, 2.4f, 6.5f);
            // the doors behind the rocket glow warmer and warmer (6.5-9.6)
            for (int i = 0; i < 4; i++) Lamp(k, "Doors glow " + i, new Vector3(-6f + i * 4f, 4f, 18f), new Color(1f, .6f - i * .05f, .3f), 22f, .8f + i * .6f, 6.5f + i * .7f, 11f);
            // the four: Dan at the socket, the others behind; small before the rocket (3-5.6); they walk to the hatch and board (5-6.6)
            Path(k, dan, (0f, sock + new Vector3(0, 0, -1.3f), 0f), (3.2f, sock + new Vector3(0, 0, -1.3f), 0f), (3.3f, sock + new Vector3(-.8f, 0, -6f), 0f), (5.0f, sock + new Vector3(-.8f, 0, -6f), 0f), (6.4f, new Vector3(-.6f, 0, -3.4f), 0f), (L, new Vector3(-.6f, 0, -3.4f), 0f));
            Path(k, abi, (0f, sock + new Vector3(1.4f, 0, -2.6f), -10f), (3.2f, sock + new Vector3(1.4f, 0, -2.6f), -10f), (3.3f, sock + new Vector3(.8f, 0, -6.2f), 0f), (5.0f, sock + new Vector3(.8f, 0, -6.2f), 0f), (6.4f, new Vector3(.6f, 0, -3.4f), 0f), (L, new Vector3(.6f, 0, -3.4f), 0f));
            Path(k, kira, (0f, sock + new Vector3(-1.3f, 0, -2.2f), 0f), (3.2f, sock + new Vector3(-1.3f, 0, -2.2f), 0f), (3.3f, sock + new Vector3(0, 0, -5.4f), 0f), (5.0f, sock + new Vector3(0, 0, -5.4f), 0f), (6.3f, new Vector3(0, 0, -3.4f), 0f), (L, new Vector3(0, 0, -3.4f), 0f));
            Path(k, mm, (0f, sock + new Vector3(2.6f, 0, -3.6f), -20f), (3.2f, sock + new Vector3(2.6f, 0, -3.6f), -20f), (3.3f, sock + new Vector3(2.2f, 0, -6.6f), 0f), (5.0f, sock + new Vector3(2.2f, 0, -6.6f), 0f), (6.6f, new Vector3(1.4f, 0, -3.6f), 0f), (L, new Vector3(1.4f, 0, -3.6f), 0f));
            foreach (var (a, t1) in new[] { (dan, 6.4f), (abi, 6.4f), (kira, 6.3f), (mm, 6.6f) })
            { k.Key(a, "Body", "m_LocalScale", 0f, Vector3.one); k.Key(a, "Body", "m_LocalScale", t1, Vector3.one); k.Key(a, "Body", "m_LocalScale", t1 + .02f, Vector3.one * .001f); k.Key(a, "Body", "m_LocalScale", L, Vector3.one * .001f); }
            k.Clips(dan, (PeopleClip("offer") ?? PeopleClip("idle"), 0f), (PeopleClip("cheer"), 2.4f), (PeopleClip("look"), 3.3f), (PeopleClip("walk"), 5.0f));
            k.Clips(abi, (PeopleClip("idle"), 0f), (PeopleClip("cheer"), 2.5f), (PeopleClip("look"), 3.3f), (PeopleClip("walk"), 5.0f));
            k.Clips(kira, (KiraClip("idle", c.kira), 0f), (KiraClip("jump", c.kira), 2.5f), (KiraClip("idle", c.kira), 3.3f), (KiraClip("run", c.kira), 5.0f));
            k.Clips(mm, (PeopleClip("idle"), 0f), (PC("mm_cheer", "cheer"), 2.6f), (PeopleClip("idle"), 3.3f), (PeopleClip("walk"), 5.0f));
            if (k.Necklaces.ContainsKey(dan)) PulseFrom(k, dan, 2.4f, 5f, L);
            // shots: Dan places the shard; the four small before the huge rocket (the clip's first shot); boarding; the rocket alone, the doors
            // glowing; ignition; from below it rises into the starry dusk sky
            k.Shot("Dan places the shard", 0f, 3.2f, new Vector3(-1.6f, 1.6f, -16f), sock + new Vector3(0, 1.1f, 0), 40f, new Vector3(-1.2f, 1.5f, -15f));
            k.Shot("Small before the rocket", 3.2f, 5.6f, new Vector3(0, 1.2f, -24f), new Vector3(0, 6.5f, 0), 60f, new Vector3(0, 1.3f, -22.5f));   // Dan's clip: low, straight at the fins
            k.Shot("They board", 5.6f, 6.8f, new Vector3(5f, 2f, -10f), new Vector3(0, 1.5f, -3f), 50f);
            k.Shot("The rocket alone", 6.8f, 9.6f, new Vector3(0, 1.4f, -22f), new Vector3(0, 6f, 6f), 58f, new Vector3(0, 1.4f, -17.5f));   // the clip's slow push-in, the doors glowing behind
            k.Shot("Ignition", 9.6f, 11.6f, new Vector3(9f, 1.2f, -16f), new Vector3(0, 2f, 0), 58f);
            k.Shot("Into the starry sky", 11.6f, L, new Vector3(-6f, 1f, -24f), new Vector3(0, 40f, 0), 62f, new Vector3(-6f, 1f, -24f), new Vector3(0, 90f, 0));
            TintC(k, new Color(1f, .9f, .75f), (0f, 0f), (9.4f, 0f), (9.7f, .35f), (10.4f, .1f), (L - 1.2f, 0f), (L, 1f));
            k.Sound("story", 2.3f, .7f); k.Sound("pink_shard_click", 2.4f, .8f); k.Sound("dome_open", 9.3f, .7f); k.Sound("rocket_countdown", 6.2f, .8f);
            k.Sound("rocket_ignite", 9.3f, 1f); k.Sound("rocket_roar", 10.2f, 1f);
            Look(k, abi, rk, 3.4f, 5.4f); Look(k, mm, rk, 3.4f, 5.4f); Look(k, kira, rk, 3.5f, 5.2f);
            k.Save();
            NecklaceModel.Tint = IceAgeCast.shard;
        }

        static Color WorldArtColor(string mat) { var m = WorldArt.Mat(mat); return m ? m.color : new Color(.95f, .76f, .38f); }

        // one half of a dome (x >= 0 for side 1, x <= 0 for side -1), both faces
        static Mesh HalfDome(float r, float h, float side)
        {
            var full = ProcMesh.Dome(r, h, 48, 16);
            var v = full.vertices; var tris = full.triangles; var keep = new List<int>();
            for (int i = 0; i < tris.Length; i += 3)
            {
                float cx = (v[tris[i]].x + v[tris[i + 1]].x + v[tris[i + 2]].x) / 3f;
                if (cx * side >= 0) keep.AddRange(new[] { tris[i], tris[i + 1], tris[i + 2] });
            }
            var m = new Mesh { name = "Half dome" }; m.vertices = v; m.normals = full.normals; m.triangles = keep.ToArray(); m.RecalculateBounds();
            string p = $"Assets/Resources/Cutscenes/HalfDome{(side < 0 ? "W" : "E")}.asset"; AssetDatabase.DeleteAsset(p); AssetDatabase.CreateAsset(m, p);
            return m;
        }
    }
}
