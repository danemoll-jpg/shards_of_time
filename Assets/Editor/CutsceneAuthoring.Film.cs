using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 9: the eight film scenes redone to follow Dan's clips (SourceArt/Reference/Clips/, notes in Docs/CUTSCENE-FILM-NOTES.md):
    // the clip's shot order, angles, framing and timing, in the game's style with the game's models (outfits from the concept sheets).
    // Side-by-side sheets of film and game: python Tools/film_sheet.py sheet <Name> -> Data/screenshots/round9/<Name>.jpg.
    // Conventions here: actors are made at their set's origin (yaw 0) and moved with Path (positions and headings in the set's own space);
    // a set (Kit.Set) stands at a world spot on the ground (CutscenePlace), so a scene can leave its anchor (Act I's fall over the Ice Age,
    // Egypt's dune, the Wise Men in the field).
    public static partial class CutsceneAuthoring
    {
        static readonly Color[] Rainbow = { new Color(1f, .2f, .2f), new Color(1f, .55f, .1f), new Color(1f, .92f, .2f), new Color(.3f, .9f, .3f), new Color(.2f, .85f, 1f), new Color(.25f, .4f, 1f), new Color(.7f, .3f, 1f) };
        static readonly Color[] BlueSwirl = { new Color(.35f, .75f, 1f), new Color(.85f, .97f, 1f), new Color(.2f, .5f, 1f), new Color(.6f, .9f, 1f) };
        static readonly Color[] RedSwirl = { new Color(.9f, .12f, .1f), new Color(1f, .55f, .2f), new Color(.75f, .05f, .08f), new Color(1f, .85f, .5f) };
        static readonly Color[] GoldSwirl = { new Color(1f, .75f, .25f), new Color(1f, .9f, .55f), new Color(1f, .62f, .15f), new Color(1f, .97f, .8f) };
        static readonly Color[] PurpleSwirl = { new Color(.55f, .3f, 1f), new Color(.85f, .7f, 1f), new Color(.4f, .15f, .8f), new Color(.7f, .45f, 1f) };

        // ---------------------------------------------------------------- helpers
        static readonly Dictionary<GameObject, Quaternion> lastQ = new Dictionary<GameObject, Quaternion>();
        static void KeyRot(Kit k, GameObject a, string path, float t, Quaternion q)
        {
            if (lastQ.TryGetValue(a, out var p) && Quaternion.Dot(p, q) < 0) q = new Quaternion(-q.x, -q.y, -q.z, -q.w);   // no flips between keys
            lastQ[a] = q; k.KeyQ(a, path, t, q);
        }

        // an actor's Body through waypoints (set space): (time, position, heading); big turns get in-between keys
        static void Path(Kit k, GameObject a, params (float t, Vector3 p, float yaw)[] w)
        {
            for (int i = 0; i < w.Length; i++)
            {
                if (i > 0 && Mathf.Abs(Mathf.DeltaAngle(w[i - 1].yaw, w[i].yaw)) > 60f && w[i].t - w[i - 1].t > .12f)
                    for (int s = 1; s < 3; s++)
                    {
                        float f = s / 3f;
                        KeyRot(k, a, "Body", Mathf.Lerp(w[i - 1].t, w[i].t, f), Quaternion.Euler(0, w[i - 1].yaw + Mathf.DeltaAngle(w[i - 1].yaw, w[i].yaw) * f, 0));
                    }
                k.Key(a, "Body", "m_LocalPosition", w[i].t, w[i].p);
                KeyRot(k, a, "Body", w[i].t, Quaternion.Euler(0, w[i].yaw, 0));
            }
        }

        // an actor made at its set's origin; Path moves it
        static GameObject Cast0(Kit k, string hero, string model) => k.Actor(hero, model, Vector3.zero, 0);

        // a figure tumbling from `from` to `to` (falling through a vortex), spinning
        static void Tumble(Kit k, GameObject a, Vector3 from, Vector3 to, float t0, float t1, float spins, float tilt = 70f, float phase = 0f, float len = 0f)
        {
            for (float t = t0; t <= t1 + .001f; t += .2f)
            {
                float f = Mathf.InverseLerp(t0, t1, t);
                k.Key(a, "Body", "m_LocalPosition", t, Vector3.Lerp(from, to, f) + new Vector3(Mathf.Sin(f * 9f + phase) * .3f, 0, 0));
                KeyRot(k, a, "Body", t, Quaternion.Euler(tilt * Mathf.Sin(f * 6f + phase), phase * 40f + f * spins * 360f, f * spins * 120f));
            }
            Appear(k, a, t0, t1, len > 0 ? len : k.duration);
        }

        // Round 9 sheets: a humanoid actor switched on mid-scene (an activation track) kept its model where it woke up, ignoring its keyed
        // path (the fall over the Ice Age happened on the ground). Actors that appear late stay active and are hidden by their scale instead.
        static void Appear(Kit k, GameObject a, float t0, float t1, float len)
        {
            k.Key(a, "Body", "m_LocalScale", 0f, Vector3.one * .001f); k.Key(a, "Body", "m_LocalScale", t0 - .02f, Vector3.one * .001f); k.Key(a, "Body", "m_LocalScale", t0, Vector3.one);
            k.Key(a, "Body", "m_LocalScale", t1, Vector3.one);
            if (t1 < len - .05f) { k.Key(a, "Body", "m_LocalScale", t1 + .02f, Vector3.one * .001f); k.Key(a, "Body", "m_LocalScale", len, Vector3.one * .001f); }
        }

        static void Pulse(Kit k, GameObject dan, params (float t, float s)[] keys)
        {
            if (!k.Necklaces.TryGetValue(dan, out var neck)) return;
            foreach (var (t, s) in keys) k.Key(neck.gameObject, "Shard", "m_LocalScale", t, Vector3.one * s);
        }
        static void PulseFrom(Kit k, GameObject dan, float t0, float t1, float len)
        {
            var keys = new List<(float, float)> { (0f, 1f), (t0, 1f) };
            for (float t = t0 + .4f; t < t1; t += .4f) keys.Add((t, keys.Count % 2 == 0 ? 2.3f : 1.5f));
            keys.Add((len, 1.8f)); Pulse(k, dan, keys.ToArray());
        }

        static void TintC(Kit k, Color c, params (float t, float amount)[] keys)
        {
            var h = k.Holder("Screen tint", Vector3.zero, 0);
            var a = new GameObject("Amount"); a.transform.SetParent(h.transform, false); a.AddComponent<ScreenTint>().color = c;
            foreach (var (t, v) in keys) k.Key(h, "Amount", "m_LocalScale", t, new Vector3(v, 1, 1));
        }

        // a spiral vortex: arms of glowing blobs on a disc that grows from t0 to t1 and turns until tEnd. The disc faces local -z (facing).
        static GameObject Spiral(Kit k, string name, Vector3 at, Quaternion facing, Color[] cols, float R, float t0, float t1, float tEnd, float spin = 120f,
            Color? back = null, int arms = 5, bool byRadius = true, float shrinkAt = -1f)
        {
            var h = k.Holder(name, at, 0); h.transform.localRotation = facing;
            var s = new GameObject("Spin"); s.transform.SetParent(h.transform, false);
            if (back.HasValue)
            {
                var d = Placeholders.Prim(PrimitiveType.Cylinder, s.transform, Vector3.zero, new Vector3(2f * R, .004f * R, 2f * R), Placeholders.Mat(back.Value, "Glow"), name: "Disc");
                d.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            const int n = 22;   // Round 9 sheets: arms of many blobs read as a swirl, few read as dots
            for (int a = 0; a < arms; a++)
                for (int i = 0; i < n; i++)
                {
                    float f = i / (n - 1f), r = R * (.05f + .95f * f), ang = (a * 360f / arms + f * 320f) * Mathf.Deg2Rad, sz = R * (.08f + .22f * f);   // Round 9 sheets: bigger blobs read as a solid swirl
                    var c = byRadius ? cols[Mathf.Min(cols.Length - 1, (int)(f * cols.Length))] : cols[(i + a) % cols.Length];
                    Placeholders.Prim(PrimitiveType.Sphere, s.transform, new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r, 0), new Vector3(sz, sz, .05f * R), Placeholders.Mat(c, "Glow"), name: $"Arm{a}_{i}");
                }
            Placeholders.Prim(PrimitiveType.Sphere, s.transform, Vector3.zero, new Vector3(.16f * R, .16f * R, .06f * R), Placeholders.Mat(Color.Lerp(cols[0], Color.white, .7f), "Glow"), name: "Eye");
            k.Key(h, "Spin", "m_LocalScale", t0, Vector3.one * .01f); k.Key(h, "Spin", "m_LocalScale", t1, Vector3.one);
            if (shrinkAt > 0) { k.Key(h, "Spin", "m_LocalScale", shrinkAt, Vector3.one); k.Key(h, "Spin", "m_LocalScale", tEnd, Vector3.one * .02f); }
            else k.Key(h, "Spin", "m_LocalScale", tEnd, Vector3.one * 1.05f);
            for (float t = t0; t <= tEnd + .001f; t += .25f) KeyRot(k, h, "Spin", t, Quaternion.Euler(0, 0, -spin * (t - t0)));
            k.Show(h, t0, tEnd);
            return h;
        }

        // a hollow ring of glowing blobs (the golden ring over the barn), turning; it faces local -z
        static GameObject Halo(Kit k, string name, Vector3 at, Quaternion facing, Color[] cols, float R, float t0, float t1, float tEnd, float spin = 90f)
        {
            var h = k.Holder(name, at, 0); h.transform.localRotation = facing;
            var s = new GameObject("Spin"); s.transform.SetParent(h.transform, false);
            for (int ring = 0; ring < 3; ring++)
            {
                float rr = R * (1f - ring * .13f); int n = 40 - ring * 6;
                for (int i = 0; i < n; i++)
                {
                    float a = (i + ring * .5f) / n * Mathf.PI * 2f, sz = R * (.16f - ring * .03f);
                    Placeholders.Prim(PrimitiveType.Sphere, s.transform, new Vector3(Mathf.Cos(a) * rr, Mathf.Sin(a) * rr, 0), new Vector3(sz, sz, sz * .5f), Placeholders.Mat(cols[(i + ring) % cols.Length], "Glow"), name: $"Ring{ring}_{i}");
                }
            }
            k.Key(h, "Spin", "m_LocalScale", t0, Vector3.one * .01f); k.Key(h, "Spin", "m_LocalScale", t1, Vector3.one); k.Key(h, "Spin", "m_LocalScale", tEnd, Vector3.one * 1.05f);
            for (float t = t0; t <= tEnd + .001f; t += .25f) KeyRot(k, h, "Spin", t, Quaternion.Euler(0, 0, -spin * (t - t0)));
            k.Show(h, t0, tEnd);
            return h;
        }

        // a funnel of bits (the hay tornado, the cobblestone whirlpool and its column): a helix cone from radius rb at the bottom to rt at
        // `height`, spinning; it grows up out of the ground from t0 to t1
        static GameObject Funnel(Kit k, string name, Vector3 at, float rb, float rt, float height, int count, float spin, float t0, float t1, float tEnd,
            Material[] mats, Vector3 bit, string sound = null)
        {
            var h = k.Holder(name, at, 0);
            var s = new GameObject("Spin"); s.transform.SetParent(h.transform, false);
            for (int i = 0; i < count; i++)
            {
                float f = (i + .5f) / count, r = Mathf.Lerp(rb, rt, f), ang = i * 137.5f * Mathf.Deg2Rad;
                var b = Placeholders.Prim(PrimitiveType.Cube, s.transform, new Vector3(Mathf.Cos(ang) * r, f * height, Mathf.Sin(ang) * r), bit * Mathf.Lerp(.8f, 1.3f, (i * 7 % 10) / 10f), mats[i % mats.Length], name: "Bit" + i);
                b.transform.localRotation = Quaternion.Euler(i * 31 % 50, -ang * Mathf.Rad2Deg, i * 17 % 40);
            }
            k.Key(h, "Spin", "m_LocalScale", t0, new Vector3(.3f, .01f, .3f)); k.Key(h, "Spin", "m_LocalScale", t1, Vector3.one); k.Key(h, "Spin", "m_LocalScale", tEnd, Vector3.one);
            for (float t = t0; t <= tEnd + .001f; t += .2f) KeyRot(k, h, "Spin", t, Quaternion.Euler(0, -spin * (t - t0), 0));
            k.Show(h, t0, tEnd);
            if (sound != null) k.Sound(sound, t0, .7f);
            return h;
        }

        // a burst of bits flying out of `at` (sand plume, hay, light)
        static void Burst(Kit k, string name, Vector3 at, Color c, float t0, float size, int count, float life, float rise = .6f, string kind = "Glow", bool cubes = false)
        {
            var h = k.Holder(name, at, 0); var m = Placeholders.Mat(c, kind);
            for (int i = 0; i < count; i++)
            {
                Placeholders.Prim(cubes ? PrimitiveType.Cube : PrimitiveType.Sphere, h.transform, Vector3.zero, Vector3.one * .05f, m, name: "Bit" + i);
                var dir = Quaternion.Euler(0, i * (360f / count) + (i % 3) * 13f, 0) * new Vector3(size * (.35f + .2f * (i % 3)), size * rise * (.6f + .4f * ((i * 5) % 3)), 0);
                k.Key(h, "Bit" + i, "m_LocalPosition", t0, Vector3.zero); k.Key(h, "Bit" + i, "m_LocalPosition", t0 + life * .45f, dir); k.Key(h, "Bit" + i, "m_LocalPosition", t0 + life, dir * 1.2f + Vector3.down * size * .2f);
                var big = cubes ? new Vector3(.45f, .08f, .15f) * size : Vector3.one * size * .45f;
                k.Key(h, "Bit" + i, "m_LocalScale", t0, Vector3.one * .03f); k.Key(h, "Bit" + i, "m_LocalScale", t0 + life * .3f, big); k.Key(h, "Bit" + i, "m_LocalScale", t0 + life, Vector3.one * .02f);
            }
            k.Show(h, t0, t0 + life);
        }

        static void Lamp(Kit k, string n, Vector3 p, Color c, float range, float intensity, float t0, float t1)
        {
            var l = k.Holder(n, p, 0); var lt = l.AddComponent<Light>(); lt.type = LightType.Point; lt.color = c; lt.range = range; lt.intensity = intensity; lt.shadows = LightShadows.None;
            k.Show(l, t0, t1);
        }

        static GameObject PropFrom(string file, string prop, Transform parent, Vector3 localPos, float yaw, float scale = 1f)
        {
            var f = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Resources/World/{file}.fbx");
            var src = f ? f.transform.Find(prop) : null;
            if (!src) { Debug.LogWarning($"[Cutscene] no {prop} in {file}"); return null; }
            var go = Object.Instantiate(src.gameObject); go.name = prop; WorldArt.Dress(go);
            go.transform.SetParent(parent, false); go.transform.localPosition = localPos; go.transform.localRotation = Quaternion.Euler(0, yaw, 0) * src.localRotation; go.transform.localScale = src.localScale * scale;
            return go;
        }

        static GameObject Ball(Kit k, string n, Vector3 at, Color c, float size, string kind = "Glow")
        {
            var h = k.Holder(n, at, 0); Placeholders.Prim(PrimitiveType.Sphere, h.transform, Vector3.zero, Vector3.one * size, Placeholders.Mat(c, kind), name: "Glow");
            return h;
        }

        // a held thing (Kira in Abi's arms): where in front of a standing / kneeling person's chest
        static Vector3 Arms(Vector3 at, float yaw, float h, float fwd = .32f) => at + Quaternion.Euler(0, yaw, 0) * new Vector3(0, h, fwd);

        // a camera looking along `yaw` from `at`
        static Vector3 Fwd(float yaw) => Quaternion.Euler(0, yaw, 0) * Vector3.forward;

        // ======================================================================================== 1. Act I (00-Home_ActOne.mp4, 64 s)
        // Decorating the tree together, Kira swats, Dan fumbles the bauble, it drops; the glow, the cracks and light beams, Kira sniffs,
        // Abi scoops her up; the crystal-shard burst and the rainbow spiral; sucked in holding hands; the rainbow tunnel; then the blue
        // vortex in the Ice Age sky (a mammoth below) and the fall over the mountains and volcanoes. 60 s, timed to Dan's act1.mp3 (59.9 s).
        // The root is Game.HomeAnchor (the room); the sky is a set at the Ice Age's landing spot (world (0, 12)).
        public const float ActOneLength = 60f;
        public static void ActOne()
        {
            DataAssets.Ensure(); lastQ.Clear();
            var k = new Kit("ActOne", ActOneLength);
            var mood = k.root.AddComponent<CutsceneMood>();
            var src = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/World/home_room.fbx");
            Transform orn = null;
            if (src)
            {
                var roomInst = (GameObject)PrefabUtility.InstantiatePrefab(src); PrefabUtility.UnpackPrefabInstance(roomInst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                roomInst.name = "Living room"; roomInst.transform.SetParent(k.root.transform, false);
                WorldArt.Dress(roomInst);
                orn = roomInst.transform.Find("Ornament");
                var heart = roomInst.transform.Find("HeartOrnament");
                if (heart)
                {
                    var tg = new GameObject("Initials"); tg.transform.SetParent(heart, false); tg.transform.localPosition = new Vector3(0, .01f, -.075f);
                    var tm = tg.AddComponent<TextMesh>(); tm.text = "D + A"; tm.font = UiKit.Font; tm.fontSize = 48; tm.characterSize = .006f; tm.anchor = TextAnchor.MiddleCenter; tm.color = new Color(1f, .9f, .75f);
                    tg.GetComponent<MeshRenderer>().sharedMaterial = UiKit.Font.material;
                }
            }
            void RoomLamp(string n, Vector3 p, Color c, float range, float intensity)
            {
                var l = new GameObject(n); l.transform.SetParent(k.root.transform, false); l.transform.localPosition = p;
                var lt = l.AddComponent<Light>(); lt.type = LightType.Point; lt.color = c; lt.range = range; lt.intensity = intensity; lt.shadows = LightShadows.Soft;
            }
            RoomLamp("Tree light", new Vector3(2.4f, 1.5f, 2.4f), new Color(1f, .72f, .4f), 6f, 2.4f);
            RoomLamp("Room light", new Vector3(1f, 2.8f, .8f), new Color(1f, .82f, .62f), 10f, 1.9f);
            RoomLamp("Wreath light", new Vector3(-1.9f, 2.1f, 4.3f), new Color(1f, .78f, .45f), 4f, 1.2f);
            RoomLamp("Window light", new Vector3(2f, 1.8f, 4.5f), new Color(.6f, .7f, 1f), 4f, .8f);

            // --- the room (0-41.5 s). The tree at (2.5, 3.3); Abi left of it, Dan right, Kira under it (the film's framing from the front)
            var dan = Cast0(k, "Dan", "dan_home"); var abi = Cast0(k, "Abi", "abi_home"); var kira = Cast0(k, "Kira", "kira");
            Vector3 danAt = new Vector3(3.4f, 0, 2.65f), abiAt = new Vector3(1.6f, 0, 2.7f), kiraAt = new Vector3(2.75f, 0, 2.2f);
            var floorAt = new Vector3(2.55f, 0, 1.75f);   // where the bauble ends up (its hook .27 over the floor)
            Vector3 danKneel = new Vector3(2.05f, 0, 2.3f), abiKneel = new Vector3(3.05f, 0, 2.3f);   // behind it, facing the camera (Dan left, as in the film)
            Path(k, dan, (0f, danAt, -80f), (12.2f, danAt, -80f), (12.6f, danAt + new Vector3(-.45f, 0, -.25f), -110f), (13.6f, danAt + new Vector3(-.45f, 0, -.25f), -110f),
                (17.5f, danAt + new Vector3(-.45f, 0, -.25f), -150f), (19.4f, danAt + new Vector3(-.45f, 0, -.25f), -150f), (19.45f, danKneel, 180f), (37.6f, danKneel, 180f),
                (38.4f, danKneel + new Vector3(.08f, 0, 0), 120f), (39.8f, danKneel + new Vector3(.12f, 0, 0), 120f));
            Path(k, abi, (0f, abiAt, 80f), (4f, abiAt, 80f), (5f, abiAt + new Vector3(-.2f, 0, -.3f), 150f), (7.5f, abiAt, 80f), (13f, abiAt, 80f), (14f, abiAt + new Vector3(.2f, 0, -.2f), 110f),
                (19.4f, abiAt + new Vector3(.2f, 0, -.2f), 110f), (19.45f, abiKneel, 180f), (37.6f, abiKneel, 180f), (38.4f, abiKneel - new Vector3(.08f, 0, 0), 240f), (39.8f, abiKneel - new Vector3(.12f, 0, 0), 240f));
            k.Clips(dan, (PeopleClip("offer"), 0f), (PeopleClip("look"), 6f), (PeopleClip("offer"), 7.5f), (PeopleClip("surprised"), 10.8f), (PeopleClip("jump"), 12.2f),
                (PeopleClip("surprised"), 13.4f), (PeopleClip("mm_kneel"), 19.45f), (PeopleClip("offer"), 38.2f), (PeopleClip("fall"), 40.2f));
            k.Clips(abi, (PeopleClip("offer"), 0f), (PeopleClip("walk"), 4f), (PeopleClip("offer"), 7.5f), (PeopleClip("look"), 10.4f), (PeopleClip("surprised"), 12.6f),
                (PeopleClip("mm_kneel"), 19.45f), (PeopleClip("offer"), 38.2f), (PeopleClip("fall"), 40.2f));
            // Kira plays under the tree, leaps at the bauble in Dan's hand (10.2), then creeps up and sniffs it on the floor; Abi scoops her up (25.5)
            var kiraHigh = new Vector3(3.0f, .85f, 2.35f);
            var sniff = floorAt + new Vector3(.45f, 0, .05f);
            var inArms = Arms(abiKneel, 180f, .42f, .36f);
            Path(k, kira, (0f, kiraAt, 160f), (3f, kiraAt + new Vector3(-.2f, 0, .1f), 120f), (6f, kiraAt, 80f), (9.9f, kiraAt, 60f), (10.25f, kiraHigh, 60f), (10.55f, kiraHigh + new Vector3(.05f, 0, 0), 60f),
                (10.95f, kiraAt + new Vector3(.1f, 0, -.1f), 60f), (13f, kiraAt + new Vector3(.1f, 0, -.1f), 200f), (19.4f, kiraAt + new Vector3(.3f, 0, -.2f), 200f), (19.45f, sniff + new Vector3(.4f, 0, .3f), 240f),
                (21.6f, sniff, 260f), (25.3f, sniff, 260f), (25.9f, inArms, 180f), (37.6f, inArms, 180f), (38.4f, Arms(abiKneel - new Vector3(.08f, 0, 0), 240f, .42f, .36f), 240f), (39.8f, Arms(abiKneel - new Vector3(.12f, 0, 0), 240f, .42f, .36f), 240f));
            k.Clips(kira, (KiraClip("idle"), 0f), (KiraClip("walk"), 2.6f), (KiraClip("idle"), 6.2f), (KiraClip("jump"), 9.85f), (KiraClip("attack"), 10.3f), (KiraClip("land"), 10.8f),
                (KiraClip("idle"), 11.3f), (KiraClip("walk"), 19.45f), (KiraClip("idle"), 21.6f), (KiraClip("fall"), 40.2f));
            // the bauble: in Dan's hand, Kira swats it, he fumbles it twice, it drops, bounces, rests; it glows gold (21.5), light beams (27), it
            // bursts into blue crystal shards (29.5) and the rainbow spiral opens where it was (33)
            var handAt = new Vector3(2.95f, 1.22f, 2.55f);
            var oh = k.Holder("Ornament holder", Vector3.zero, 0);
            var ornRot0 = Quaternion.identity;
            if (orn) { orn.SetParent(oh.transform, true); orn.name = "Ornament"; orn.localPosition = handAt; ornRot0 = orn.localRotation; }
            else Placeholders.Prim(PrimitiveType.Sphere, oh.transform, handAt, Vector3.one * .2f, Placeholders.Mat(new Color(.28f, .3f, .35f)), name: "Ornament");
            var rest = floorAt + new Vector3(0, .27f, 0);
            foreach (var (t, p) in new[] { (0f, handAt), (10.45f, handAt), (10.8f, handAt + new Vector3(-.15f, .35f, -.1f)), (11.2f, handAt + new Vector3(.12f, .05f, -.15f)), (11.6f, handAt + new Vector3(-.1f, .4f, -.25f)),
                (12.2f, handAt + new Vector3(-.15f, -.1f, -.35f)), (12.9f, new Vector3(2.62f, .3f, 1.85f)), (13.3f, new Vector3(2.58f, .55f, 1.8f)), (13.7f, rest), (14.0f, rest + Vector3.up * .12f), (14.3f, rest), (33.5f, rest) })
                k.Key(oh, "Ornament", "m_LocalPosition", t, p);
            float[] rt = { 0f, 10.45f, 11.2f, 12.0f, 12.9f, 13.7f, 14.3f, 33.5f }; float[] rx = { 0, 0, 160, 330, 520, 700, 720, 720 };
            for (int i = 0; i < rt.Length; i++) KeyRot(k, oh, "Ornament", rt[i], Quaternion.Euler(rx[i], 0, rx[i] * .5f) * ornRot0);   // ends upright (720, 0, 360): the glass hangs under its hook again
            k.Show(oh, 0f, 33.5f);
            var glow = Ball(k, "Ornament glow", rest + new Vector3(0, -.16f, 0), new Color(1f, .82f, .35f), .1f);
            foreach (var (t, s) in new[] { (21.4f, .05f), (22.4f, .3f), (23.4f, .26f), (24.4f, .32f), (26.5f, .3f), (28.5f, .36f), (31f, .34f), (33.6f, .42f) }) k.Key(glow, "Glow", "m_LocalScale", t, Vector3.one * s);
            k.Show(glow, 21.4f, 33.6f);
            Lamp(k, "Glow light", rest + new Vector3(0, .25f, -.25f), new Color(1f, .78f, .4f), 4f, 3f, 21.6f, 38f);
            // light beams out of the cracks, then crystal shards fly out past the camera
            var beams = k.Holder("Light beams", rest + new Vector3(0, -.16f, 0), 0);
            for (int i = 0; i < 9; i++)
            {
                var piv = new GameObject("Beam" + i); piv.transform.SetParent(beams.transform, false);
                piv.transform.localRotation = Quaternion.Euler(-15f - (i % 3) * 25f, -100f + i * 25f, 0);
                Placeholders.Prim(PrimitiveType.Cube, piv.transform, new Vector3(0, 0, .5f), new Vector3(.025f, .025f, 1f), Placeholders.Mat(new Color(.6f, .88f, 1f), "Glow"), name: "Ray");
                k.Key(beams, $"Beam{i}", "m_LocalScale", 26.8f + i * .05f, new Vector3(1, 1, .01f)); k.Key(beams, $"Beam{i}", "m_LocalScale", 28.2f, new Vector3(1, 1, 1.0f));
                k.Key(beams, $"Beam{i}", "m_LocalScale", 32.5f, new Vector3(1, 1, 1.2f)); k.Key(beams, $"Beam{i}", "m_LocalScale", 34f, new Vector3(1, 1, .01f));
            }
            k.Show(beams, 26.8f, 34f);
            var sh = k.Holder("Crystal shards", Vector3.zero, 0);
            for (int i = 0; i < 16; i++)
            {
                var c = Placeholders.CrystalObject("Shard" + i, i % 3 == 0 ? new Color(.85f, .97f, 1f) : new Color(.35f, .7f, 1f), .16f); c.transform.SetParent(sh.transform, false);
                float a = i * 22.5f * Mathf.Deg2Rad; var o = rest + new Vector3(0, -.1f, 0);
                var far = o + new Vector3(Mathf.Cos(a) * 3.2f, .4f + (i % 4) * .45f, -Mathf.Abs(Mathf.Sin(a)) * 2.6f - .5f);
                float t0 = 29.4f + (i % 5) * .7f;
                k.Key(sh, "Shard" + i, "m_LocalPosition", 29f, o); k.Key(sh, "Shard" + i, "m_LocalPosition", t0, o); k.Key(sh, "Shard" + i, "m_LocalPosition", t0 + 1.1f, far);
                k.Key(sh, "Shard" + i, "m_LocalPosition", 41.5f, far);
                KeyRot(k, sh, "Shard" + i, t0, Quaternion.Euler(0, i * 40, 90)); KeyRot(k, sh, "Shard" + i, t0 + 1.1f, Quaternion.Euler(i * 70, i * 40 + 160, 90));
            }
            k.Show(sh, 29.4f, 40.5f);
            // the rainbow spiral opens where the bauble was, facing the camera, and swallows them (holding hands, Abi with Kira)
            var spiralAt = rest + new Vector3(0, .45f, .7f);   // between and just behind them (they kneel in front of it)
            Spiral(k, "Rainbow spiral", spiralAt, Quaternion.identity, Rainbow, .95f, 33.2f, 36.5f, 41.5f, 160f, new Color(.6f, .25f, .9f), 6);
            k.Key(k.root.transform.Find("Rainbow spiral").gameObject, "Spin", "m_LocalScale", 39f, Vector3.one * 1.3f);
            foreach (var (a, off) in new[] { (dan, new Vector3(-.15f, 0, 0)), (abi, new Vector3(.15f, 0, 0)), (kira, new Vector3(.15f, .6f, 0)) })
            {
                var from = a == dan ? danKneel + new Vector3(.12f, 0, 0) : a == abi ? abiKneel - new Vector3(.12f, 0, 0) : Arms(abiKneel - new Vector3(.12f, 0, 0), 240f, .42f, .36f);
                float y0 = a == dan ? 120f : 240f;
                Path(k, a, (40.3f, from + new Vector3(0, .25f, 0), y0), (40.9f, spiralAt + off + new Vector3(0, -.3f, .05f), y0 + 200f), (41.4f, spiralAt + new Vector3(0, -.2f, .2f), y0 + 360f));
                k.Key(a, "Body", "m_LocalScale", 40.3f, Vector3.one); k.Key(a, "Body", "m_LocalScale", 41.4f, Vector3.one * .1f);
                k.Show(a, 0f, 41.45f);
            }
            k.Sound("crack", 27f, .7f); k.Sound("shard", 29.4f, .8f); k.Sound("necklace", 33.2f, .7f);

            // --- the rainbow tunnel (41.5-49 s): a set far under the room, the spiral filling the frame, the three tumbling into it
            var tun = k.Set("Rainbow tunnel", 0, -3000f, 0, 330f, false); k.under = tun;
            Spiral(k, "Tunnel spiral", new Vector3(0, 0, 30), Quaternion.identity, Rainbow, 24f, 41.4f, 41.6f, 49f, 90f, new Color(.55f, .25f, .9f), 7);
            var td = Cast0(k, "Dan tumbling", "dan_home"); var ta = Cast0(k, "Abi tumbling", "abi_home"); var tk = Cast0(k, "Kira tumbling", "kira");
            k.Clips(td, (PeopleClip("fall"), 41.5f)); k.Clips(ta, (PeopleClip("fall"), 41.5f)); k.Clips(tk, (KiraClip("fall"), 41.5f));   // (from when they show: the fall clip sinks as it loops)
            Tumble(k, td, new Vector3(-.6f, -.5f, 3.5f), new Vector3(0, 0, 27f), 41.5f, 46.6f, 1.4f, 60f, 0f);
            Tumble(k, ta, new Vector3(.6f, -.6f, 3.7f), new Vector3(.2f, 0, 27f), 41.5f, 46.6f, 1.4f, 60f, .5f);
            Tumble(k, tk, new Vector3(.1f, .2f, 3.2f), new Vector3(.1f, .1f, 27f), 41.5f, 46.6f, 2f, 80f, 1.2f);
            k.Shot("Into the rainbow", 41.5f, 46.6f, new Vector3(0, 0, -1.5f), new Vector3(0, 0, 30f), 62f, new Vector3(0, 0, 3f));
            k.Shot("The swirl", 46.6f, 49f, new Vector3(0, 0, 8f), new Vector3(0, 0, 30f), 62f, new Vector3(0, 0, 14f));
            k.under = null;

            // --- the Ice Age sky (49-60 s): the world's own light from here (CutsceneMoodSwitch)
            var sw = k.Holder("Mood switch", Vector3.zero, 0);
            var amt = new GameObject("Amount"); amt.transform.SetParent(sw.transform, false); amt.AddComponent<CutsceneMoodSwitch>().mood = mood;
            foreach (var (t, v) in new[] { (0f, 0f), (48.95f, 0f), (49f, 1f), (ActOneLength, 1f) }) k.Key(sw, "Amount", "m_LocalScale", t, new Vector3(v, 1, 1));
            var sky = k.Set("Ice Age sky", 0, 12f, 0, 0); k.under = sky;
            var mam = Cast0(k, "Mammoth", "mammoth");
            Path(k, mam, (0f, new Vector3(5.5f, 0, 22f), 200f), (53.5f, new Vector3(5f, 0, 20.5f), 200f));
            k.Clips(mam, (KiraClip("idle", "mammoth"), 0f), (KiraClip("walk", "mammoth"), 49f));
            var vAt = new Vector3(10f, 75f, 120f);
            var camM = new Vector3(3.6f, .7f, 12.5f);
            Spiral(k, "Blue vortex", vAt, Quaternion.LookRotation(vAt - new Vector3(0, 40f, 40f)), BlueSwirl, 22f, 49f, 50.5f, ActOneLength, 110f, new Color(.3f, .6f, 1f), 5, false);
            for (int i = 0; i < 9; i++)
            {
                float a = i * 40f * Mathf.Deg2Rad;
                var cl = k.Holder("Cloud" + i, vAt + new Vector3(Mathf.Cos(a) * 38f, Mathf.Sin(a) * 20f - 8f, 6f + (i % 3) * 5f), 0);
                for (int j = 0; j < 3; j++) Placeholders.Prim(PrimitiveType.Sphere, cl.transform, new Vector3((j - 1) * 7f, (j % 2) * 2f, 0), new Vector3(14f, 7f, 8f), Placeholders.Mat(new Color(.93f, .95f, 1f), "Lit"), name: "Puff" + j);
                k.Show(cl, 49f, ActOneLength);
            }
            var fd = Cast0(k, "Dan falling", "dan_home"); var fa = Cast0(k, "Abi falling", "abi_home"); var fk = Cast0(k, "Kira falling", "kira");
            foreach (var (a, x, ph) in new[] { (fd, -.8f, 0f), (fa, .8f, 1f), (fk, .05f, 2f) })
            {
                Path(k, a, (55.4f, vAt + new Vector3(x, -3f, -2f), 180f), (56.6f, new Vector3(x, 52f, 64f), 180f), (ActOneLength, new Vector3(x * 1.3f, 34f, 62f), 180f));
                Appear(k, a, 55.4f, ActOneLength, ActOneLength);
            }
            k.Clips(fd, (PeopleClip("fall"), 55.4f)); k.Clips(fa, (PeopleClip("fall"), 55.4f)); k.Clips(fk, (KiraClip("fall"), 55.4f));
            k.Shot("A mammoth, a vortex", 49f, 53.4f, camM, new Vector3(7.5f, 9f, 40f), 55f, camM + new Vector3(.2f, -.1f, -.6f));
            k.Shot("The vortex in the clouds", 53.4f, 56.4f, new Vector3(2f, 38f, 30f), vAt, 42f, new Vector3(3f, 42f, 38f));
            k.Shot("Over the Ice Age", 56.4f, ActOneLength, new Vector3(.3f, 51.5f, 58.5f), new Vector3(0, 50.5f, 66f), 52f, new Vector3(.3f, 34f, 57.5f), new Vector3(0, 33.5f, 64f));   // just under them, looking level: the mountains and volcanoes behind (the film)
            k.under = null;
            TintC(k, Color.white, (0f, 0f), (59.2f, 0f), (59.9f, 1f), (ActOneLength, 1f));

            // --- the room's shots, in the film's order
            k.Shot("Decorating the tree", 0f, 6f, new Vector3(2.5f, 1.45f, -1.0f), new Vector3(2.5f, 1.15f, 3.0f), 50f, new Vector3(2.5f, 1.4f, -.5f));
            k.Shot("Dan's bauble", 6f, 9.4f, new Vector3(1.95f, 1.15f, .9f), new Vector3(3.15f, 1.05f, 2.6f), 42f);
            k.Shot("Kira swats", 9.4f, 12.1f, new Vector3(2.45f, 1.0f, .2f), new Vector3(2.8f, .8f, 2.5f), 50f);
            k.Shot("It drops", 12.1f, 15.4f, new Vector3(2.5f, 1.25f, -.2f), new Vector3(2.6f, .8f, 2.4f), 54f);
            k.Shot("Dan lunges, Abi gasps", 15.4f, 18.6f, new Vector3(1.1f, 1.55f, .35f), new Vector3(2.85f, .7f, 2.3f), 50f);
            k.Shot("On the floor", 18.6f, 21.6f, new Vector3(2.55f, .2f, 1.05f), rest + new Vector3(0, -.12f, 0), 36f, new Vector3(2.55f, .2f, 1.2f));
            k.Shot("It glows", 21.6f, 24.6f, new Vector3(2.55f, .62f, .55f), new Vector3(2.55f, .55f, 2.1f), 50f);
            k.Shot("Abi scoops Kira", 24.6f, 28.2f, new Vector3(2.55f, .82f, .9f), new Vector3(2.55f, .7f, 2.2f), 46f);
            k.Shot("Light bursts out", 28.2f, 33.2f, new Vector3(2.55f, .95f, .2f), new Vector3(2.55f, .6f, 2.2f), 58f);
            k.Shot("The rainbow spiral", 33.2f, 38.2f, new Vector3(2.55f, .85f, .05f), new Vector3(2.55f, .8f, 2.0f), 58f, new Vector3(2.55f, .82f, .3f));
            k.Shot("Holding hands", 38.2f, 41.5f, new Vector3(2.55f, 1.0f, .35f), new Vector3(2.55f, .8f, 2.1f), 55f);
            k.Save();
        }

        // ======================================================================================== 2. The Ice Age arrival (01-IceAge_Arrival.mp4, 18 s)
        // They've landed in the snow; by a campfire Kira (still the gray house cat) glows and turns into the sabertooth on screen; high-angle
        // shots on Dan and Abi, ground-level shots on her: the snarl, the stalk, the leap; Dan flat on his back, Kira on his chest pawing the
        // necklace, which glows blue; Abi kneels beside. Round 5's relief beat stays at the end (Dan laughs, the shard in his hand).
        // The root is the landing spot, +z up the valley.
        public const float ArrivalLength = 22f;
        public static void IceAgeArrival()
        {
            DataAssets.Ensure(); lastQ.Clear();
            var k = new Kit("IceAgeArrival", ArrivalLength);
            var dan = Cast0(k, "Dan", "dan_iceage"); var abi = Cast0(k, "Abi", "abi_iceage");
            var cat = Cast0(k, "Kira", "kira"); var kira = Cast0(k, "Kira sabertooth", "kira_sabertooth");
            Vector3 danAt = new Vector3(-.4f, 0, 0), abiAt = new Vector3(.6f, 0, .15f), kiraAt = new Vector3(.1f, 0, 5.3f), fireAt = new Vector3(1.05f, 0, 5.75f);   // Abi on the left as the film sees them (from the front)
            // the campfire
            var fire = k.Holder("Campfire", fireAt, 0);
            if (!PropFrom("bethlehem_props", "Campfire", fire.transform, Vector3.zero, 0, .55f)) Placeholders.Prim(PrimitiveType.Cylinder, fire.transform, new Vector3(0, .1f, 0), new Vector3(.8f, .1f, .8f), WorldArt.Mat("Wood"), name: "Logs");
            Flames(k, fire, "", new Vector3(0, .12f, 0), 0f, .1f, .16f, ArrivalLength);
            var fl = new GameObject("Fire light"); fl.transform.SetParent(fire.transform, false); fl.transform.localPosition = new Vector3(0, .8f, 0);
            var lt = fl.AddComponent<Light>(); lt.type = LightType.Point; lt.color = new Color(1f, .6f, .25f); lt.range = 7f; lt.intensity = 3f;
            // they land (white fading), stand there smiling
            TintC(k, Color.white, (0f, 1f), (.3f, 1f), (.9f, 0f));
            foreach (var (a, at, yaw, t) in new[] { (dan, danAt, 5f, .55f), (abi, abiAt, -8f, .45f) })
            {
                Path(k, a, (0f, at + Vector3.up * 3.5f, yaw), (t, at, yaw));
                Puff(k, at + Vector3.up * .1f, t, .8f);
            }
            // the transformation: the cat by the fire, a white glow swells, she's the sabertooth when it fades
            Path(k, cat, (0f, kiraAt, 160f));
            Appear(k, cat, .03f, 1.75f, ArrivalLength);
            var tf = Ball(k, "Transformation", kiraAt + new Vector3(0, .35f, 0), new Color(1f, .98f, .9f), .1f);
            foreach (var (t, s) in new[] { (1.0f, .05f), (1.45f, .6f), (1.75f, .75f), (2.2f, .05f) }) k.Key(tf, "Glow", "m_LocalScale", t, Vector3.one * s);
            k.Show(tf, 1.0f, 2.2f);
            var chest = new Vector3(0, .3f, -.95f) + danAt;
            var fallBack = PeopleClip("fall_back");
            if (k.Models.TryGetValue(dan, out var danAnim) && fallBack)
            {
                var bone = danAnim.GetBoneTransform(HumanBodyBones.Chest) ?? danAnim.GetBoneTransform(HumanBodyBones.Spine);
                if (bone)
                {
                    AnimationMode.StartAnimationMode(); AnimationMode.BeginSampling(); AnimationMode.SampleAnimationClip(danAnim.gameObject, fallBack, fallBack.length); AnimationMode.EndSampling();
                    chest = k.root.transform.InverseTransformPoint(bone.position) + new Vector3(0, .14f, 0) + danAt;
                    AnimationMode.StopAnimationMode();
                    Debug.Log($"[Cutscene] IceAgeArrival: Dan's chest after the fall at {chest}");
                }
            }
            var stalkTo = new Vector3(0, 0, 2.6f);
            Path(k, kira, (0f, kiraAt, 160f), (5.2f, kiraAt, 185f), (7.8f, kiraAt, 185f), (10.2f, stalkTo, 182f), (12.25f, stalkTo, 182f), (12.8f, Vector3.Lerp(stalkTo, chest, .5f) + Vector3.up * 1.1f, 180f),
                (13.35f, chest, 180f), (18f, chest, 180f), (18.05f, danAt + new Vector3(-.9f, 0, .6f), 140f), (ArrivalLength, danAt + new Vector3(-.9f, 0, .6f), 140f));
            Appear(k, kira, 1.75f, ArrivalLength, ArrivalLength);
            k.Clips(kira, (KiraClip("idle", "kira_sabertooth"), 0f), (KiraClip("attack", "kira_sabertooth"), 5.4f), (KiraClip("idle", "kira_sabertooth"), 6.2f), (KiraClip("attack", "kira_sabertooth"), 6.7f),
                (KiraClip("walk", "kira_sabertooth"), 7.8f), (KiraClip("idle", "kira_sabertooth"), 10.2f), (KiraClip("jump", "kira_sabertooth"), 12.2f), (KiraClip("land", "kira_sabertooth"), 13.3f),
                (KiraClip("idle", "kira_sabertooth"), 13.3f), (KiraClip("attack", "kira_sabertooth"), 14.6f), (KiraClip("attack", "kira_sabertooth"), 15.4f), (KiraClip("attack", "kira_sabertooth"), 16.2f),
                (KiraClip("idle", "kira_sabertooth"), 17f));
            k.Clips(cat, (KiraClip("idle"), 0f));
            k.Sound("growl", 5.4f, 1f); k.Sound("growl", 8.4f, .8f); k.Sound("growl", 12.2f, .9f);
            // Dan and Abi: smiling, stunned, scared; he goes down on his back at the leap, she kneels by him
            Path(k, dan, (.55f, danAt, 5f), (ArrivalLength, danAt, 5f));
            var abiKneel = new Vector3(chest.x + .75f, 0, chest.z + .05f);
            Path(k, abi, (.45f, abiAt, -8f), (13.2f, abiAt, -8f), (13.25f, abiKneel, -90f), (18f, abiKneel, -90f), (18.05f, abiAt, -8f), (ArrivalLength, abiAt, -8f));
            k.Clips(dan, (PeopleClip("fall"), 0f), (PeopleClip("land"), .55f), (PeopleClip("idle"), 1.2f), (PeopleClip("surprised"), 2.4f), (PeopleClip("nervous"), 10.1f),
                (fallBack ? fallBack : PeopleClip("hurt"), 12.35f), (PeopleClip("laugh"), 18f), (PeopleClip("hold_up"), 20f));
            k.Clips(abi, (PeopleClip("fall"), 0f), (PeopleClip("land"), .45f), (PeopleClip("idle"), 1.1f), (PeopleClip("surprised"), 2.5f), (PeopleClip("nervous"), 10.1f),
                (PeopleClip("mm_kneel"), 13.25f), (PeopleClip("laugh"), 18f), (PeopleClip("idle"), 20f));
            // the necklace glows blue
            if (k.Necklaces.TryGetValue(dan, out var neck))
            {
                PulseFrom(k, dan, 14.4f, 18f, 20.1f);
                var aura = new GameObject("Aura"); aura.transform.SetParent(neck, false);
                var shard = neck.Find("Shard"); aura.transform.localPosition = shard ? shard.localPosition : Vector3.zero;
                Placeholders.Prim(PrimitiveType.Sphere, aura.transform, Vector3.zero, Vector3.one * .1f, Placeholders.Mat(new Color(.4f, .7f, 1f), "Glow"), name: "Glow");
                for (int i = 0; i < 9; i++) k.Key(neck.gameObject, "Aura/Glow", "m_LocalScale", 14.4f + i * .4f, Vector3.one * (i % 2 == 0 ? .1f : .26f));
                k.Show(aura, 14.4f, 18f);
                // the relief: the shard in his hand (the one on his chest hides)
                var hand = danAnim ? danAnim.GetBoneTransform(HumanBodyBones.RightHand) : null;
                if (hand)
                {
                    var hs = Placeholders.CrystalObject("Hand shard", NecklaceModel.Bright, 1f); hs.transform.SetParent(hand, false);
                    hs.transform.position = hand.position + hand.up * .09f; var ls = hand.lossyScale; hs.transform.localScale = new Vector3(.07f / ls.x, .07f / ls.y, .07f / ls.z);
                    k.Show(hs, 20.2f, ArrivalLength);
                    Pulse(k, dan, (20.15f, 1.6f), (20.2f, .01f), (ArrivalLength, .01f));
                }
            }
            // shots, the film's pattern: high on the people, ground level on Kira
            k.Shot("They've landed", 0f, 1.0f, new Vector3(.1f, 2.3f, 2.0f), new Vector3(.1f, 1.15f, 0), 44f);
            k.Shot("Kira transforms", 1.0f, 2.3f, kiraAt + new Vector3(-.55f, .3f, -1.35f), kiraAt + new Vector3(.25f, .35f, .2f), 44f);
            k.Shot("Stunned (high)", 2.3f, 5.2f, new Vector3(.1f, 2.75f, 1.6f), new Vector3(.1f, 1.2f, 0), 44f, new Vector3(.1f, 2.6f, 1.4f));
            k.Shot("The snarl (low)", 5.2f, 7.8f, kiraAt + new Vector3(.05f, .3f, -1.05f), kiraAt + new Vector3(0, .48f, 0), 36f, kiraAt + new Vector3(.05f, .32f, -.9f));
            k.Shot("She stalks (low)", 7.8f, 10.2f, kiraAt + new Vector3(0, .26f, -1.15f), kiraAt + new Vector3(0, .42f, 0), 40f, stalkTo + new Vector3(0, .26f, -1.15f), stalkTo + new Vector3(0, .42f, 0));
            k.Shot("Scared (high)", 10.2f, 12.2f, new Vector3(.1f, 2.6f, 1.45f), new Vector3(.1f, 1.2f, 0), 46f);
            k.Shot("The leap", 12.2f, 13.6f, new Vector3(2.6f, 1.6f, 1.6f), new Vector3(-.2f, .6f, .3f), 50f);
            k.Shot("Kira on his chest", 13.6f, 18f, chest + new Vector3(.2f, 2.2f, 1.45f), chest + new Vector3(.2f, .15f, -.25f), 44f, chest + new Vector3(.18f, 2.05f, 1.3f));
            k.Shot("Relief", 18f, 20f, danAt + new Vector3(.4f, 1.6f, 2.6f), danAt + new Vector3(0, 1.45f, 0), 40f);
            k.Shot("The shard in his hand", 20f, ArrivalLength, danAt + new Vector3(.9f, 1.75f, 1.2f), danAt + new Vector3(.2f, 1.6f, .25f), 38f, danAt + new Vector3(.7f, 1.7f, .95f));
            k.Save();
        }

        // ======================================================================================== 3. Egypt's arrival (02-Egypt_Arrival.mp4, 44 s)
        // The establishing shot at the building site (the scaffolded pyramid, workers with stone sledges, the judgmental camel crossing
        // frame); the red vortex over the dunes; the sand plume, then the empty dune (the comic pause); Dan and Abi pop out of the sand;
        // they dig frantically for Kira; mummy Kira pops up; Abi hugs her. 33 s. The root is the arrival spot on the plaza; the building
        // site and the dune are sets out in the world (the dune just north of the town at world (-12, 158), the pyramids behind it).
        public const float EgyptArrivalLength = 33f;
        public static void EgyptArrival()
        {
            DataAssets.Ensure(); lastQ.Clear();
            var c = EgyptCast; NecklaceModel.Tint = c.shard;
            var k = new Kit("EgyptArrival", EgyptArrivalLength);
            // --- the building site (0-6 s)
            var site = k.Set("Building site", 40f, 192f, 0); k.under = site;
            foreach (var (z, x0, x1, dt) in new[] { (1.5f, -3f, 8f, 0f), (7f, 9f, -1f, 0f), (12f, 2f, 12f, 0f) })
            {
                var w = k.Holder("Sledge " + z, new Vector3(x0, 0, z), x1 > x0 ? 90 : -90);
                var grp = new GameObject("Group"); grp.transform.SetParent(w.transform, false);
                Placeholders.Prim(PrimitiveType.Cube, grp.transform, new Vector3(0, .2f, -1.2f), new Vector3(1.6f, .25f, 2.4f), WorldArt.Mat("Wood"), name: "Sledge");
                Placeholders.Prim(PrimitiveType.Cube, grp.transform, new Vector3(0, .95f, -1.2f), new Vector3(1.4f, 1.2f, 1.9f), WorldArt.Mat("Sandstone"), name: "Stone");
                for (int i = 0; i < 3; i++)
                {
                    var man = new GameObject("Worker" + i); man.transform.SetParent(grp.transform, false); man.transform.localPosition = new Vector3(i * .7f - .7f, 0, 1.4f + (i % 2) * .5f);
                    Placeholders.Prim(PrimitiveType.Capsule, man.transform, new Vector3(0, .85f, 0), new Vector3(.42f, .85f, .38f), Placeholders.Mat(new Color(.68f, .46f, .32f)), name: "Body");
                    Placeholders.Prim(PrimitiveType.Cylinder, man.transform, new Vector3(0, .62f, 0), new Vector3(.48f, .2f, .4f), Placeholders.Mat(new Color(.96f, .93f, .85f)), name: "Kilt");
                    Placeholders.Prim(PrimitiveType.Sphere, man.transform, new Vector3(0, 1.78f, 0), new Vector3(.3f, .32f, .3f), Placeholders.Mat(new Color(.95f, .95f, .9f)), name: "Head");
                }
                k.Key(w, "Group", "m_LocalPosition", 0f, Vector3.zero); k.Key(w, "Group", "m_LocalPosition", 6f, new Vector3(0, 0, Mathf.Abs(x1 - x0) * .35f));
                k.Show(w, 0f, 6f);
            }
            var camel = Cast0(k, "Camel", Have("camel", ""));
            Path(k, camel, (0f, new Vector3(-9f, 0, -7.5f), 90f), (.8f, new Vector3(-9f, 0, -7.5f), 90f), (6f, new Vector3(1.5f, 0, -7.8f), 92f));
            k.Clips(camel, (KiraClip("idle", "camel"), 0f), (KiraClip("walk", "camel"), 1.2f));
            k.Show(camel, 0f, 6f);
            foreach (var t in new[] { .4f, 1.8f, 3.2f, 4.6f }) Burst(k, "Dust", new Vector3(6f + t * 2f, .3f, 16f), new Color(.93f, .82f, .6f), t, 1.6f, 6, 1.4f, .5f, "Lit");
            k.Shot("The building site", 0f, 6f, new Vector3(-6f, 2.2f, -14f), new Vector3(22f, 10f, 40f), 52f, new Vector3(-5.2f, 2.3f, -12.8f));
            k.under = null;
            // --- the dune (6-33 s): a hump of sand they land in (it covers the ground's dips), the pyramids far behind it
            var dune = k.Set("Dune", -12f, 158f, 0); k.under = dune;
            var hump = k.Holder("Sand hump", new Vector3(0, -.35f, 0), 0);
            Placeholders.Prim(PrimitiveType.Sphere, hump.transform, Vector3.zero, new Vector3(9f, 1.7f, 8f), WorldArt.Mat("Sand"), name: "Sand");
            var top = new Vector3(0, .48f, 0);
            var vAt = new Vector3(0, 19f, 22f); var camA = new Vector3(0, 1.6f, -14f);
            Spiral(k, "Red vortex", vAt, Quaternion.LookRotation(vAt - camA) * Quaternion.Euler(-35, 0, 0), RedSwirl, 11f, 6.2f, 8.2f, 16.5f, 140f, new Color(.7f, .08f, .06f), 5, false, 13.2f);
            var dan = Cast0(k, "Dan", c.dan); var abi = Cast0(k, "Abi", c.abi); var kira = Cast0(k, "Kira", c.kira);
            Vector3 danAt = top + new Vector3(.55f, 0, 0), abiAt = top + new Vector3(-.55f, 0, 0), kiraUp = top + new Vector3(0, 0, -.55f);
            const float under = -2.0f;
            foreach (var (a, at, d) in new[] { (dan, danAt, 0f), (abi, abiAt, .1f), (kira, kiraUp, .05f) })
                Path(k, a, (6f, vAt, 180f), (11.2f + d, vAt + Vector3.down * 1f, 180f), (11.9f + d, at + new Vector3(0, 2.5f, 0), 180f), (12.1f + d, at + new Vector3(0, under, 0), 180f));
            Burst(k, "Sand plume", top, new Color(.92f, .76f, .45f), 12.05f, 5f, 14, 2.6f, .9f, "Lit");
            Burst(k, "Sand plume 2", top + new Vector3(.5f, 0, .3f), new Color(.86f, .68f, .38f), 12.2f, 4f, 10, 2.4f, 1.1f, "Lit");
            // the comic pause, then they pop up out of the sand, look round, notice Kira's gone and dig for her
            Path(k, dan, (18.2f, danAt + new Vector3(0, under, 0), 180f), (18.7f, danAt + new Vector3(0, .15f, 0), 180f), (18.9f, danAt, 180f), (23.4f, danAt, 180f), (23.8f, danAt + new Vector3(-.05f, 0, -.1f), 215f), (EgyptArrivalLength, danAt + new Vector3(-.05f, 0, -.1f), 215f));
            Path(k, abi, (18.5f, abiAt + new Vector3(0, under, 0), 180f), (19.0f, abiAt + new Vector3(0, .15f, 0), 180f), (19.2f, abiAt, 180f), (23.4f, abiAt, 180f), (23.8f, abiAt + new Vector3(.05f, 0, -.1f), 145f), (EgyptArrivalLength, abiAt + new Vector3(.05f, 0, -.1f), 145f));
            Burst(k, "Dan pops up", danAt + Vector3.up * .1f, new Color(.92f, .78f, .5f), 18.3f, 1.2f, 7, 1f, .6f, "Lit");
            Burst(k, "Abi pops up", abiAt + Vector3.up * .1f, new Color(.92f, .78f, .5f), 18.6f, 1.2f, 7, 1f, .6f, "Lit");
            k.Clips(dan, (PeopleClip("fall"), 6f), (PeopleClip("idle"), 18.2f), (PeopleClip("look"), 19.2f), (PeopleClip("surprised"), 21.4f), (PeopleClip("mm_kneel"), 23.4f));
            k.Clips(abi, (PeopleClip("fall"), 6f), (PeopleClip("idle"), 18.5f), (PeopleClip("look"), 19.6f), (PeopleClip("surprised"), 21.6f), (PeopleClip("mm_kneel"), 23.4f));
            for (int i = 0; i < 9; i++) Burst(k, "Digging " + i, top + new Vector3((i % 2 == 0 ? .45f : -.45f), .05f, -.5f), new Color(.92f, .78f, .5f), 23.8f + i * .5f, .35f, 5, .7f, 1.2f, "Lit", true);
            // mummy Kira pops up between them, Abi hugs her
            var hug = Arms(abiAt + new Vector3(.05f, 0, -.1f), 145f, .28f, .4f);
            Path(k, kira, (28.4f, kiraUp + new Vector3(0, under, 0), 180f), (28.8f, kiraUp + new Vector3(0, .5f, 0), 180f), (29.1f, kiraUp, 180f), (30.2f, kiraUp, 160f), (30.7f, hug, 330f), (EgyptArrivalLength, hug, 330f));
            Appear(k, kira, 6f, EgyptArrivalLength, EgyptArrivalLength);
            Burst(k, "Kira pops up", kiraUp + Vector3.up * .1f, new Color(.92f, .78f, .5f), 28.45f, 1f, 7, 1f, .7f, "Lit");
            k.Clips(kira, (KiraClip("fall", c.kira), 6f), (KiraClip("jump", c.kira), 28.4f), (KiraClip("land", c.kira), 29f), (KiraClip("idle", c.kira), 29.4f));
            PulseFrom(k, dan, 31f, EgyptArrivalLength, EgyptArrivalLength);
            k.Shot("A red vortex", 6f, 12f, camA, vAt + new Vector3(0, -7f, 0), 58f);
            k.Shot("The sand plume", 12f, 14.6f, new Vector3(.4f, 1.6f, -11f), top + new Vector3(0, 2f, 0), 50f);
            k.Shot("The empty dune", 14.6f, 18.2f, new Vector3(.4f, 1.5f, -9f), top + new Vector3(0, 1.4f, 0), 50f);
            k.Shot("They pop up", 18.2f, 21.2f, new Vector3(0, 1.45f, -2.9f), top + new Vector3(0, 1.2f, 0), 44f);
            k.Shot("Where's Kira?", 21.2f, 23.6f, new Vector3(.1f, 1.5f, -2.4f), top + new Vector3(0, 1.35f, 0), 42f);
            k.Shot("Digging", 23.6f, 28.4f, new Vector3(-.15f, 1.1f, -2.2f), top + new Vector3(0, .55f, -.1f), 48f, new Vector3(-.1f, 1.05f, -1.95f));
            k.Shot("Mummy Kira!", 28.4f, 30.4f, new Vector3(.1f, .95f, -2.4f), kiraUp + new Vector3(0, .55f, 0), 50f);
            k.Shot("A hug", 30.4f, EgyptArrivalLength, new Vector3(-.2f, .95f, -2.0f), abiAt + new Vector3(.2f, .6f, -.25f), 44f);
            k.under = null;
            k.Sound("camel_snort", 3.0f, .5f); k.Sound("shard", 6.4f, .5f); k.Sound("sand_sink", 12.05f, .9f); k.Sound("sand_sink", 18.3f, .5f); k.Sound("sand_sink", 28.45f, .5f);
            k.Save();
            NecklaceModel.Tint = IceAgeCast.shard;
        }

        // ======================================================================================== 4. The Sphinx (02-Egypt_Sphinx.mp4, 17.6 s)
        // From behind Dan and Abi, the Sphinx far off; a slow push-in to its face (Kira's), held; they react; Dan looks down at mummy Kira;
        // her innocent close-ups; the long look between them, his necklace red. Comic timing over speed. The root is Egypt.GagSpot, +z toward
        // the Sphinx (its face ~21 m ahead, ~16.5 m up).
        public const float SphinxGagLength = 17.6f;
        public static void SphinxGag()
        {
            DataAssets.Ensure(); lastQ.Clear();
            var c = EgyptCast; NecklaceModel.Tint = c.shard;
            var k = new Kit("SphinxGag", SphinxGagLength);
            var dan = Cast0(k, "Dan", c.dan); var abi = Cast0(k, "Abi", c.abi); var kira = Cast0(k, "Kira", c.kira);
            Vector3 danAt = new Vector3(.42f, 0, 0), abiAt = new Vector3(-.42f, 0, 0), kiraAt = new Vector3(1.6f, 0, -1.0f);   // Abi on the left from behind and (they turn round) from the front, as in the film
            var face = new Vector3(-.5f, 11f, 12.5f);   // measured on the Round 9 sheet (the old 16.5 m / 21 m was inside its chin)
            Path(k, dan, (0f, danAt, 0f), (6.2f, danAt, 0f), (6.6f, danAt, 195f), (8.0f, danAt, 195f), (8.5f, danAt, 160f), (SphinxGagLength, danAt, 160f));
            Path(k, abi, (0f, abiAt, 0f), (6.2f, abiAt, 0f), (6.6f, abiAt, 170f), (15.0f, abiAt, 170f), (15.5f, abiAt, 115f), (SphinxGagLength, abiAt, 115f));
            Path(k, kira, (0f, kiraAt, 10f), (8.6f, kiraAt, 10f), (8.9f, kiraAt, 200f), (9.8f, kiraAt, 165f), (10.6f, kiraAt, 215f), (11.5f, kiraAt, 190f), (12.6f, kiraAt, 168f), (13.6f, kiraAt, 200f), (SphinxGagLength, kiraAt, 190f));
            k.Clips(dan, (PeopleClip("idle"), 0f), (PeopleClip("surprised"), 6.1f), (PeopleClip("look"), 7.4f), (PeopleClip("map"), 8.4f));
            k.Clips(abi, (PeopleClip("idle"), 0f), (PeopleClip("surprised"), 6.2f), (PeopleClip("idle"), 7.6f), (PeopleClip("look"), 15.3f));
            k.Clips(kira, (KiraClip("idle", c.kira), 0f));
            PulseFrom(k, dan, 14.8f, SphinxGagLength, SphinxGagLength);
            k.Shot("From behind them", 0f, 1.5f, new Vector3(.02f, 1.5f, -2.0f), new Vector3(0, 5.5f, 21f), 52f);
            k.Shot("The push-in", 1.5f, 4.0f, new Vector3(0, 1.9f, -2.4f), face, 34f, new Vector3(-.2f, 4.6f, -.6f));
            k.Shot("Kira's face", 4.0f, 5.8f, new Vector3(-.25f, 5.4f, .2f), face, 28f, new Vector3(-.28f, 5.7f, .6f));
            k.Shot("They react", 5.8f, 8.4f, new Vector3(-.05f, 1.55f, -2.1f), new Vector3(0, 1.4f, 0), 42f);
            k.Shot("He looks down", 8.4f, 9.4f, new Vector3(-.25f, 1.6f, -1.75f), new Vector3(.5f, 1.15f, 0), 42f);
            k.Shot("Mummy Kira", 9.4f, 12f, kiraAt + new Vector3(.08f, .38f, -.78f), kiraAt + new Vector3(0, .33f, 0), 38f);
            k.Shot("Innocent", 12f, 14.6f, kiraAt + new Vector3(.05f, .38f, -.66f), kiraAt + new Vector3(0, .36f, 0), 36f);
            k.Shot("The look", 14.6f, SphinxGagLength, new Vector3(-.05f, 1.5f, -1.55f), new Vector3(0, 1.38f, 0), 40f);
            k.Sound("camel_snort", 16.6f, .0f);
            k.Save();
            NecklaceModel.Tint = IceAgeCast.shard;
        }

        // ======================================================================================== 5. Bethlehem's arrival (03-Bethlehem_Arrival.mp4, 21 s)
        // The Wise Men walking to the glowing barn see a star grow into a golden spiral; it flattens into a ring hovering over the barn with
        // a beam down; a golden flash; top-down, the three crash through the thatched roof; the Wise Men's puzzled close-up; inside, Abi on
        // the hay with lamb Kira beside her, Dan flat on the floor with the gold glow. The root is the barn's middle (world (0, floor, 34)),
        // +z north: the door at -8, the hay under the roof hole at (-3, -1.5). The Wise Men are a set in the field south-east of the barn.
        public const float BethlehemArrivalLength = 21f;
        public static void BethlehemArrival()
        {
            DataAssets.Ensure(); lastQ.Clear();
            var c = BethlehemCast; NecklaceModel.Tint = c.shard;
            var k = new Kit("BethlehemArrival", BethlehemArrivalLength);
            // --- the Wise Men in the field east of the barn (world (18, 16)): the set's -x points at the barn's door (0, 26), 20.6 m away;
            // the camera south of their path sees them on the right walking left, the barn far left, the star over it (the film's framing)
            var field = k.Set("The field", 18f, 16f, 29f); k.under = field;
            string[] models = { "wiseman_santa", "wiseman_crown", "wiseman_old" }; string[] gifts = { "GiftRed", "GiftGreen", "GiftBlue" };
            Color[] robes = { new Color(.5f, .14f, .12f), new Color(.95f, .9f, .78f), new Color(.35f, .45f, .2f) };
            var props = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/World/bethlehem_props.fbx");
            float walkYaw = -90f; var step = new Vector3(-2.4f, 0, 0);
            for (int i = 0; i < 3; i++)
            {
                var m = WiseMan(k, "Wise Man " + i, models[i], Vector3.zero, 0, robes[i], i);
                if (props && props.transform.Find(gifts[i]) is Transform g) { var gi = Object.Instantiate(g.gameObject); gi.name = "Present"; WorldArt.Dress(gi); gi.transform.SetParent(m.transform.Find("Body") ?? m.transform, false); gi.transform.localPosition = new Vector3(0, .78f, .36f); }
                var at = new Vector3(i * .85f, 0, (2 - i) * .55f);
                Path(k, m, (0f, at, walkYaw), (3.0f, at + step, walkYaw), (BethlehemArrivalLength, at + step, walkYaw));
                k.Clips(m, (PeopleClip("walk"), 0f), (PeopleClip("look"), 3.0f), (PeopleClip("cheer"), 3.6f), (PeopleClip("offer"), 6.0f), (PeopleClip("surprised") ?? PeopleClip("look"), 16f));
            }
            var camW = new Vector3(2.4f, 1.0f, -4.5f);
            var starAt = new Vector3(-26f, 17f, 7f);
            var star = Ball(k, "Star", starAt, new Color(1f, .95f, .75f), .9f);
            foreach (var (t, s) in new[] { (0f, .9f), (1.5f, 2.2f), (2.0f, 1.2f) }) k.Key(star, "Glow", "m_LocalScale", t, Vector3.one * s);
            k.Show(star, 0f, 2.1f);
            Spiral(k, "Golden spiral", starAt, Quaternion.LookRotation(starAt - camW), GoldSwirl, 3.4f, 1.6f, 5.5f, 6.4f, 150f, null, 5, false);
            k.Shot("The star", 0f, 6.3f, camW, new Vector3(-5f, 2.6f, 4f), 54f, camW + new Vector3(-.5f, 0, .2f));
            k.Shot("Puzzled", 15.8f, 17.5f, new Vector3(-2.2f, 1.62f, .6f) + step, new Vector3(.8f, 1.55f, .6f) + step, 44f);
            k.under = null;
            // --- the ring over the barn, its beam; the golden flash
            var ringAt = new Vector3(0, 17f, -1f);
            Halo(k, "Golden ring", ringAt, Quaternion.Euler(-90, 0, 0), GoldSwirl, 4.2f, 6.3f, 8f, 15.6f, 60f);
            var beam = k.Holder("Beam", new Vector3(0, ringAt.y / 2f, -1f), 0);
            Placeholders.Prim(PrimitiveType.Cylinder, beam.transform, Vector3.zero, new Vector3(.7f, ringAt.y / 2f, .7f), Placeholders.Mat(new Color(.55f, .42f, .18f), "Glow"), name: "Glow");
            k.Show(beam, 8f, 14.2f);
            TintC(k, new Color(1f, .85f, .4f), (0f, 0f), (13.0f, 0f), (13.5f, .85f), (14.2f, 0f), (BethlehemArrivalLength, 0f));
            // --- the crash: from the ring down through the roof (the hole at (-2.6, 6.6, -.9)), then inside: Abi on the hay, Kira beside her, Dan on the floor
            var hole = new Vector3(-2.6f, 6.6f, -.9f); var hay = new Vector3(-3f, 0, -1.5f);
            var dan = Cast0(k, "Dan", c.dan); var abi = Cast0(k, "Abi", c.abi); var kira = Cast0(k, "Kira", c.kira);
            Vector3 danFloor = new Vector3(-.6f, 0, .5f), abiHay = hay + new Vector3(0, .8f, 0), kiraHay = hay + new Vector3(.75f, .6f, .35f);
            foreach (var (a, land, yaw, d) in new[] { (abi, abiHay, 150f, 0f), (kira, kiraHay, 170f, .08f), (dan, danFloor, 200f, .16f) })
                Path(k, a, (0f, ringAt + Vector3.up * 2f, yaw), (14.1f + d, ringAt, yaw), (15.2f + d, hole + Vector3.up * .6f, yaw), (15.4f + d, hole + Vector3.down * 1.5f, yaw),
                    (17.6f + d, hole + Vector3.down * 1.5f, yaw), (18.2f + d, land, yaw), (BethlehemArrivalLength, land, yaw));
            Burst(k, "Thatch", hole, new Color(.9f, .75f, .35f), 15.25f, 2f, 9, 1f, .5f, "Lit", true);
            Burst(k, "Hay", abiHay, new Color(.95f, .82f, .4f), 18.2f, 1.2f, 8, .9f, .6f, "Glow", true);
            Burst(k, "Dust", danFloor + Vector3.up * .1f, new Color(.6f, .48f, .32f), 18.4f, .8f, 6, .8f);
            k.Clips(dan, (PeopleClip("fall"), 14f), (PeopleClip("fall_back"), 18.3f));
            k.Clips(abi, (PeopleClip("fall"), 14f), (PeopleClip("sit") ?? PeopleClip("land"), 18.2f));
            foreach (var a in new[] { dan, abi, kira }) Appear(k, a, 14f, BethlehemArrivalLength, BethlehemArrivalLength);
            k.Clips(kira, (KiraClip("fall", c.kira), 14f), (KiraClip("land", c.kira), 18.25f), (KiraClip("idle", c.kira), 18.7f));
            PulseFrom(k, dan, 18.8f, BethlehemArrivalLength, BethlehemArrivalLength);
            var gold = Ball(k, "Gold glow", danFloor + new Vector3(0, .3f, -.4f), new Color(1f, .82f, .35f), .1f);
            foreach (var (t, s) in new[] { (18.8f, .05f), (19.4f, .4f), (20f, .25f), (20.6f, .42f), (BethlehemArrivalLength, .3f) }) k.Key(gold, "Glow", "m_LocalScale", t, Vector3.one * s);
            k.Show(gold, 18.8f, BethlehemArrivalLength);
            k.Shot("The ring over the barn", 6.3f, 14f, new Vector3(-6f, 2.2f, -42f), new Vector3(0, 8f, 0), 40f);
            k.Shot("Top-down: the crash", 14f, 15.8f, new Vector3(-9f, 30f, -15f), new Vector3(-1.5f, 4f, -.5f), 50f);
            Lamp(k, "Barn warm light", new Vector3(-.5f, 3f, -.5f), new Color(1f, .7f, .35f), 14f, 3.5f, 17.5f, BethlehemArrivalLength);
            Lamp(k, "Hay light", hay + new Vector3(1.2f, 2f, -.8f), new Color(1f, .78f, .4f), 6f, 2.5f, 17.5f, BethlehemArrivalLength);
            k.Shot("Inside", 17.5f, BethlehemArrivalLength, new Vector3(.6f, 1.35f, -3.0f), new Vector3(-1.5f, .65f, -.6f), 50f);
            k.Sound("necklace", 1.8f, .4f); k.Sound("roof_crash", 15.2f, .8f); k.Sound("hay_rustle", 18.2f, .6f); k.Sound("sheep_bleat", 19f, .5f);
            k.Save();
            NecklaceModel.Tint = IceAgeCast.shard;
        }

        // ======================================================================================== 6. The Wise Men arrive (03-Bethlehem_WiseMenArrive.mp4, 14.8 s)
        // From behind, the Wise Men walk up to the barn, silhouetted against its golden light; they push the doors open; inside, Dan, Abi and
        // lamb Kira lean over the manger where the shard glows; the Wise Men's close-up, staring, then disappointed; they turn and walk away,
        // and the Santa-hat one tosses his present over his shoulder; the hug; the hay erupts into a golden tornado that takes them. 17 s.
        // The root is in the barn 4 m south of the manger (world (0, floor, 33)), +z north: the manger at +4, the door at -7.
        public const float WiseMenLength = 17f;
        public static void WiseMenArrive()
        {
            DataAssets.Ensure(); lastQ.Clear();
            var c = BethlehemCast; NecklaceModel.Tint = c.shard;
            var k = new Kit("WiseMenArrive", WiseMenLength);
            var manger = new Vector3(0, 0, 4f);
            var dan = Cast0(k, "Dan", c.dan); var abi = Cast0(k, "Abi", c.abi); var kira = Cast0(k, "Kira", c.kira);
            Vector3 danAt = new Vector3(-.8f, 0, 3.55f), abiAt = new Vector3(.8f, 0, 3.6f), kiraAt = new Vector3(.05f, 0, 2.85f);   // either side of the manger, facing each other over it (the film)
            Path(k, dan, (0f, danAt, 75f), (13.3f, danAt, 75f), (13.35f, new Vector3(-.4f, 0, 2.6f), 90f), (WiseMenLength, new Vector3(-.4f, 0, 2.6f), 90f));
            Path(k, abi, (0f, abiAt, -75f), (13.3f, abiAt, -75f), (13.35f, new Vector3(.45f, 0, 2.6f), -90f), (WiseMenLength, new Vector3(.45f, 0, 2.6f), -90f));
            Path(k, kira, (0f, kiraAt, 0f), (13.3f, kiraAt, 0f), (13.35f, new Vector3(.02f, 0, 2.6f), 0f), (WiseMenLength, new Vector3(.02f, 0, 2.6f), 0f));
            k.Clips(dan, (PeopleClip("map"), 0f), (PeopleClip("mm_kneel"), 13.35f), (PeopleClip("fall"), 15.3f));
            k.Clips(abi, (PeopleClip("map"), 0f), (PeopleClip("mm_kneel"), 13.35f), (PeopleClip("fall"), 15.3f));
            k.Clips(kira, (KiraClip("idle", c.kira), 0f), (KiraClip("jump", c.kira), 14.4f), (KiraClip("fall", c.kira), 15.3f));
            var shard = k.Holder("Golden shard", manger + new Vector3(0, .85f, 0), 0);
            Placeholders.CrystalObject("Crystal", c.shard, .9f).transform.SetParent(shard.transform, false);
            k.Show(shard, 0f, 14.4f);
            Lamp(k, "Manger light", manger + new Vector3(0, 1.2f, -.6f), new Color(1f, .8f, .4f), 5f, 2.5f, 0f, 14.4f);
            Lamp(k, "Barn glow", new Vector3(0, 2.4f, -6.2f), new Color(1f, .72f, .35f), 12f, 4f, 0f, 9.2f);   // the golden light they are silhouetted against
            // the doors (hinged at the door's sides, swinging in)
            foreach (var (side, x) in new[] { ("Door L", -2.4f), ("Door R", 2.4f) })
            {
                var d = k.Holder(side, new Vector3(x, 0, -7.85f), 0);
                var leaf = new GameObject("Leaf"); leaf.transform.SetParent(d.transform, false);
                Placeholders.Prim(PrimitiveType.Cube, leaf.transform, new Vector3(-Mathf.Sign(x) * 1.2f, 1.7f, 0), new Vector3(2.35f, 3.4f, .1f), WorldArt.Mat("Plank"), name: "Boards");
                for (int b = 0; b < 2; b++) Placeholders.Prim(PrimitiveType.Cube, leaf.transform, new Vector3(-Mathf.Sign(x) * 1.2f, .9f + b * 1.6f, -.07f), new Vector3(2.3f, .16f, .05f), WorldArt.Mat("Wood"), name: "Brace" + b);
                float open = x < 0 ? -100f : 100f;
                KeyRot(k, d, "Leaf", 0f, Quaternion.identity); KeyRot(k, d, "Leaf", 3.7f, Quaternion.identity); KeyRot(k, d, "Leaf", 4.7f, Quaternion.Euler(0, open, 0)); KeyRot(k, d, "Leaf", WiseMenLength, Quaternion.Euler(0, open, 0));
            }
            // the Wise Men: walk up from outside, push the doors, step in, stare, sag, turn and leave; the Santa-hat one tosses his present
            string[] models = { "wiseman_santa", "wiseman_crown", "wiseman_old" }; string[] gifts = { "GiftRed", "GiftGreen", "GiftBlue" };
            Color[] robes = { new Color(.5f, .14f, .12f), new Color(.95f, .9f, .78f), new Color(.35f, .45f, .2f) };
            var props = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/World/bethlehem_props.fbx");
            for (int i = 0; i < 3; i++)
            {
                var m = WiseMan(k, "Wise Man " + i, models[i], Vector3.zero, 0, robes[i], i);
                if (props && props.transform.Find(gifts[i]) is Transform g) { var gi = Object.Instantiate(g.gameObject); gi.name = "Present"; WorldArt.Dress(gi); gi.transform.SetParent(m.transform.Find("Body") ?? m.transform, false); gi.transform.localPosition = new Vector3(0, .78f, .36f); }
                float x = (1 - i) * 1.05f;   // seen from the barn: the Santa hat on the left, as in the film
                Vector3 outside = new Vector3(x, 0, -14f), atDoor = new Vector3(x * .9f, 0, -8.5f), inside = new Vector3(x, 0, -5.2f + (i == 1 ? .3f : 0f)), gone = new Vector3(x, 0, -10.5f);
                Path(k, m, (0f, outside, 0f), (3.6f, atDoor, 0f), (4.7f, atDoor, 0f), (5.6f, inside, 0f), (11.4f, inside, 0f), (11.9f, inside, 180f), (13.4f, inside + (gone - inside) * .5f, 180f), (WiseMenLength, gone, 180f));
                k.Clips(m, (PeopleClip("walk"), 0f), (PeopleClip("push"), 3.5f), (PeopleClip("walk"), 4.7f), (PeopleClip("offer"), 5.6f), (PeopleClip("surprised"), 9.2f), (PeopleClip("sad"), 10.3f), (PeopleClip("walk"), 11.5f));
                if (i == 0)   // the toss: from his hands, over his shoulder, onto the floor behind him
                {
                    var p0 = new Vector3(0, .78f, .36f);
                    foreach (var (t, p) in new[] { (12.15f, p0), (12.45f, new Vector3(.2f, 2.1f, -.2f)), (12.9f, new Vector3(.35f, 1.6f, -1.6f)), (13.25f, new Vector3(.4f, .15f, -2.4f)) }) k.Key(m, "Body/Present", "m_LocalPosition", t, p);
                    KeyRot(k, m, "Body/Present", 12.15f, Quaternion.identity); KeyRot(k, m, "Body/Present", 13.25f, Quaternion.Euler(200, 40, 30));
                }
            }
            // the hay tornado at the manger takes them
            var hayMats = new[] { Placeholders.Mat(new Color(1f, .82f, .35f), "Glow"), Placeholders.Mat(new Color(.95f, .7f, .25f), "Glow"), Placeholders.Mat(new Color(1f, .93f, .6f), "Glow") };
            Funnel(k, "Hay tornado", manger + new Vector3(0, .5f, 0), .5f, 2.6f, 7.5f, 150, 300f, 14.3f, 15.3f, WiseMenLength, hayMats, new Vector3(.45f, .06f, .14f), "hay_rustle");
            foreach (var (a, h, d) in new[] { (dan, .9f, 0f), (abi, .9f, .1f), (kira, .3f, .05f) })
            {
                k.Key(a, "Body", "m_LocalPosition", 15.3f + d, a == dan ? new Vector3(-.4f, 0, 2.6f) : a == abi ? new Vector3(.45f, 0, 2.6f) : new Vector3(.02f, 0, 2.6f));
                k.Key(a, "Body", "m_LocalPosition", 16.4f + d, manger + new Vector3(0, 3.5f - h, 0));
                k.Key(a, "Body", "m_LocalScale", 15.3f + d, Vector3.one); k.Key(a, "Body", "m_LocalScale", 16.4f + d, Vector3.one * .1f);
            }
            TintC(k, Color.white, (0f, 0f), (16.1f, 0f), (16.8f, 1f), (WiseMenLength, 1f));
            k.Shot("Silhouettes at the barn", 0f, 4.9f, new Vector3(.4f, 1.45f, -19.5f), new Vector3(0, 1.8f, -7f), 46f, new Vector3(.35f, 1.5f, -17.5f));
            k.Shot("At the manger", 4.9f, 9.2f, new Vector3(-.45f, 1.4f, 1.35f), manger + new Vector3(0, .9f, 0), 46f, new Vector3(-.35f, 1.35f, 1.7f));
            k.Shot("Disappointed", 9.2f, 11.6f, new Vector3(0, 1.62f, -2.7f), new Vector3(0, 1.55f, -5.1f), 40f);
            k.Shot("They leave", 11.6f, 13.4f, new Vector3(.25f, 1.55f, -2.9f), new Vector3(0, 1.4f, -9f), 50f);
            k.Shot("A hug", 13.4f, 14.3f, new Vector3(.2f, 1.15f, .6f), new Vector3(0, .75f, 2.6f), 46f);
            k.Shot("The hay tornado", 14.3f, WiseMenLength, new Vector3(2.4f, 1.6f, -2.8f), manger + new Vector3(0, 2.6f, 0), 58f);
            k.Sound("choir_hallelujah", 4.6f, .8f); k.Sound("necklace", 14.3f, .6f);
            k.Save();
            NecklaceModel.Tint = IceAgeCast.shard;
        }

        // ======================================================================================== 7. The Medieval Kingdom's arrival (04-Medieval_Arrival.mp4, 38 s)
        // The purple vortex over the castle at night; the three fall out of it against the sky; top-down, the horse pulls the hay wagon
        // through the courtyard and they drop into the hay; the hay heaves; they pop up (Kira, Dan, Abi); Kira's sneeze sets the hay
        // alight; they jump out; from behind them the burning wagon careens off, into the west wall (the game's blast and the singed
        // driver stay); then they walk on, Kira flying beside them on her little wings. 41 s. The root is the courtyard at world
        // (0, floor, 14), +z north: the wagon's stop at (2, 0, 10) facing west, the west wall at x -30 (the wreck at world (-25.5, 0, 24)).
        public const float MedievalArrivalLength = 41f;
        public static void MedievalArrival()
        {
            DataAssets.Ensure(); lastQ.Clear();
            var c = MedievalCast; NecklaceModel.Tint = c.shard;
            var k = new Kit("MedievalArrival", MedievalArrivalLength);
            const float L = MedievalArrivalLength;
            var hay = new Vector3(2f, 2.15f, 10f);
            // the wagon, the pony and the sleepy driver roll in (the prop's front is its -z; the holder faces west)
            var wagon = k.Holder("Wagon", new Vector3(2, 0, 10), 90);
            var cart = new GameObject("Cart"); cart.transform.SetParent(wagon.transform, false);
            if (!MedProp("HayWagon", cart.transform, Vector3.zero, 0))
            {
                Placeholders.Prim(PrimitiveType.Cube, cart.transform, new Vector3(0, .8f, 0), new Vector3(2f, .6f, 4f), WorldArt.Mat("Wood"), name: "Bed");
                Placeholders.Prim(PrimitiveType.Sphere, cart.transform, new Vector3(0, 1.5f, 0), new Vector3(2f, 1f, 3.6f), WorldArt.Mat("Hay"), name: "Hay");
            }
            var heave = new GameObject("Heave"); heave.transform.SetParent(cart.transform, false); heave.transform.localPosition = new Vector3(0, 1.75f, .1f);
            Placeholders.Prim(PrimitiveType.Sphere, heave.transform, Vector3.zero, new Vector3(1.5f, .6f, 2.2f), WorldArt.Mat("Hay"), name: "Mound");
            foreach (var (t, s) in new[] { (12.6f, 1f), (13.0f, 1.25f), (13.4f, .95f), (13.9f, 1.3f), (14.4f, 1f), (14.8f, 1.2f), (15.3f, 1f) }) k.Key(wagon, "Cart/Heave/Mound", "m_LocalScale", t, new Vector3(1.5f, .6f * s, 2.2f));
            Flames(k, wagon, "Cart", new Vector3(0, 2.1f, -.6f), 21.2f, 24.5f, 1.1f, 31.1f);   // (the cart's -z is Kira's end)
            foreach (var (t, z) in new[] { (0f, 9f), (7.6f, 7f), (10.4f, 0f), (27.2f, 0f), (28.6f, -6f), (31.0f, -27.3f) }) k.Key(wagon, "Cart", "m_LocalPosition", t, new Vector3(0, 0, z));
            k.Show(wagon, 0f, 31.05f);
            var pony = k.Actor("Pony", Have("horse", ""), new Vector3(2 - 3.6f, 0, 10), -90);
            foreach (var (t, p) in new[] { (0f, new Vector3(0, 0, -9f)), (7.6f, new Vector3(0, 0, -7f)), (10.4f, Vector3.zero), (27.2f, Vector3.zero), (28.6f, new Vector3(0, 0, 6f)), (30.2f, new Vector3(-1.5f, 0, 18f)),
                (31.4f, new Vector3(-6.5f, 0, 21f)), (33.5f, new Vector3(-9f, 0, 19.5f)), (L, new Vector3(-9.5f, 0, 19f)) }) k.Key(pony, "Body", "m_LocalPosition", t, p);
            k.KeyYaw(pony, "Body", 0f, 0); k.KeyYaw(pony, "Body", 30.0f, 0); k.KeyYaw(pony, "Body", 31.2f, -70f); k.KeyYaw(pony, "Body", 33.5f, -120f); k.KeyYaw(pony, "Body", L, -120f);
            k.Clips(pony, (KiraClip("walk", "horse"), 0f), (KiraClip("idle", "horse"), 10.4f), (KiraClip("run", "horse"), 27f), (KiraClip("walk", "horse"), 31.8f), (KiraClip("idle", "horse"), 33.6f));
            var driver = k.Actor("Driver", Have("wagon_driver", ""), new Vector3(2 - 1.6f, 1.05f, 10), -90);
            foreach (var (t, z) in new[] { (0f, -9f), (7.6f, -7f), (10.4f, 0f), (27.2f, 0f), (28.6f, 6f), (31.0f, 27.3f) }) k.Key(driver, "Body", "m_LocalPosition", t, new Vector3(0, 0, z));
            k.Clips(driver, (PeopleClip("sit") ?? PeopleClip("idle"), 0f), (PeopleClip("surprised") ?? PeopleClip("idle"), 27f), (PeopleClip("sit") ?? PeopleClip("idle"), 28.2f));
            k.Show(driver, 0f, 31.05f);
            var singed = k.Actor("Driver singed", Have("wagon_driver", ""), new Vector3(-23f, 0, 9.5f), 120);
            k.Key(singed, "Body", "m_LocalPosition", 31.6f, Vector3.zero); k.Key(singed, "Body", "m_LocalPosition", 34.5f, new Vector3(0, 0, 1.6f)); k.Key(singed, "Body", "m_LocalPosition", L, new Vector3(0, 0, 1.6f));
            k.Clips(singed, (PeopleClip("limp") ?? PeopleClip("walk"), 31.6f), (PeopleClip("dazed") ?? PeopleClip("idle"), 34.5f));
            k.Show(singed, 31.6f, L);
            for (int i = 0; i < 4; i++)
            {
                var sm = k.Holder("Hat smoke " + i, new Vector3(-23f, 1.9f, 9.5f), 0);
                Placeholders.Prim(PrimitiveType.Sphere, sm.transform, Vector3.zero, Vector3.one * .1f, Placeholders.Mat(new Color(.35f, .33f, .32f)), name: "Puff");
                float t0 = 32f + i * .9f;
                k.Key(sm, "Puff", "m_LocalPosition", t0, Vector3.zero); k.Key(sm, "Puff", "m_LocalPosition", t0 + 1.4f, new Vector3(.2f, 1.2f, .5f));
                k.Key(sm, "Puff", "m_LocalScale", t0, Vector3.one * .08f); k.Key(sm, "Puff", "m_LocalScale", t0 + .6f, Vector3.one * .3f); k.Key(sm, "Puff", "m_LocalScale", t0 + 1.4f, Vector3.one * .02f);
                k.Show(sm, t0, Mathf.Min(L, t0 + 1.4f));
            }
            var wreck = k.Holder("Wreck", new Vector3(-25.5f, 0, 10f), 80);
            if (!MedProp("WagonWreck", wreck.transform, Vector3.zero, 0)) Placeholders.Prim(PrimitiveType.Cube, wreck.transform, new Vector3(0, .4f, 0), new Vector3(2.4f, .8f, 4f), WorldArt.Mat("Dark"), name: "Wreck");
            Flames(k, wreck, "", new Vector3(0, .5f, 0), 31.1f, 31.4f, .7f, 34f);
            k.Show(wreck, 31.05f, L);
            Blast(k, new Vector3(-26f, 1.6f, 10f), 31.0f, 4.5f);
            TintC(k, Color.white, (0f, 0f), (30.95f, 0f), (31.05f, .55f), (31.5f, 0f), (L, 0f));
            // the purple vortex over the castle (seen from outside, south), and a closer one over the courtyard they fall out of
            var camCastle = new Vector3(0, 6f, -50f); var bigV = new Vector3(0, 64f, 62f);
            Spiral(k, "Purple vortex", bigV, Quaternion.LookRotation(bigV - camCastle), PurpleSwirl, 40f, 0f, 1.6f, 4.7f, 70f, new Color(.25f, .1f, .5f), 6, false);
            var nearV = hay + new Vector3(0, 60f, 22f);
            Spiral(k, "Purple vortex (near)", nearV, Quaternion.LookRotation(nearV - (hay + new Vector3(1.5f, 48f, -6f))), PurpleSwirl, 15f, 4.4f, 4.7f, 7.7f, 110f, new Color(.25f, .1f, .5f), 6, false);
            // the party: out of the vortex, down into the hay (10.4 s), under it, popping up (15.6 s), out (25.8 s), watching, walking on
            var dan = Cast0(k, "Dan", c.dan); var abi = Cast0(k, "Abi", c.abi); var kira = Cast0(k, "Kira", c.kira);
            var row = new[] { (kira, new Vector3(-.75f, .65f, 0), 1.25f), (dan, new Vector3(0, -.35f, 0), 0f), (abi, new Vector3(.75f, -.3f, 0), 0f) };
            Vector3 danOut = new Vector3(3.3f, 0, 6.4f), abiOut = new Vector3(4.5f, 0, 6.6f), kiraOut = new Vector3(3.9f, 1.1f, 6.1f);
            for (int j = 0; j < 3; j++)
            {
                var (a, off, _) = row[j]; var inHay = hay + off; float d = j * .15f;
                var outAt = a == dan ? danOut : a == abi ? abiOut : kiraOut;
                Path(k, a, (4.6f, hay + new Vector3((j - 1) * .9f, 55f, 14f), 180f), (7.6f + d, hay + new Vector3((j - 1) * .8f, 40f, 4f), 180f), (10.2f + d, inHay + new Vector3(0, 3f, 0), 180f),
                    (10.45f + d, inHay + new Vector3(0, -1.8f, 0), 180f), (15.5f + d * 2, inHay + new Vector3(0, -1.8f, 0), 180f), (15.9f + d * 2, inHay + new Vector3(0, .15f, 0), 180f), (16.1f + d * 2, inHay, 180f),
                    (25.6f + d, inHay, 180f), (26.0f + d, Vector3.Lerp(inHay, outAt, .5f) + Vector3.up * 1.5f, 120f), (26.5f + d, outAt, 270f), (35.9f, outAt, 270f), (36.0f, outAt, 180f),
                    (40f, outAt + new Vector3(0, 0, -4.2f), 180f), (L, outAt + new Vector3(0, 0, -6.2f), 180f));
                Appear(k, a, 4.6f, L, L);
            }
            // Kira flies beside them (a gentle bob)
            for (float t = 36.2f; t < L; t += .45f) k.Key(kira, "Body", "m_LocalPosition", t, kiraOut + new Vector3(0, ((int)((t - 36.2f) / .45f) % 2 == 0 ? .12f : -.06f), -4.2f * Mathf.InverseLerp(36f, 40f, t) - 2f * Mathf.InverseLerp(40f, L, t)));
            Burst(k, "Hay", hay + new Vector3(0, .3f, 0), new Color(.95f, .8f, .38f), 10.45f, 2.2f, 10, 1f, .6f, "Glow", true);
            Burst(k, "Pop", hay + new Vector3(0, .4f, 0), new Color(.95f, .82f, .4f), 15.9f, 1.4f, 8, .9f, .6f, "Glow", true);
            var sneeze = k.Holder("Sneeze", hay + new Vector3(-.75f, .5f, -.25f), 0);
            for (int i = 0; i < 5; i++)
            {
                Placeholders.Prim(PrimitiveType.Sphere, sneeze.transform, Vector3.zero, Vector3.one * .05f, Placeholders.Mat(i % 2 == 0 ? new Color(1f, .55f, .15f) : new Color(1f, .9f, .4f), "Glow"), name: "Bit" + i);
                var dir = new Vector3((i - 2f) * .12f, -.2f - i * .04f, -.35f - i * .05f);
                k.Key(sneeze, "Bit" + i, "m_LocalPosition", 21.0f, Vector3.zero); k.Key(sneeze, "Bit" + i, "m_LocalPosition", 21.4f, dir);
                k.Key(sneeze, "Bit" + i, "m_LocalScale", 21.0f, Vector3.one * .05f); k.Key(sneeze, "Bit" + i, "m_LocalScale", 21.15f, Vector3.one * .22f); k.Key(sneeze, "Bit" + i, "m_LocalScale", 21.7f, Vector3.one * .03f);
            }
            k.Show(sneeze, 21.0f, 21.7f);
            AnimationClip Surprise() => PeopleClip("surprised") ?? PeopleClip("look");
            k.Clips(dan, (PeopleClip("fall"), 4.6f), (PeopleClip("idle"), 15.5f), (Surprise(), 16.6f), (PeopleClip("idle"), 18.4f), (Surprise(), 21.5f), (PeopleClip("jump"), 25.6f), (PeopleClip("land"), 26.4f), (Surprise(), 31f), (PeopleClip("idle"), 33.4f), (PeopleClip("walk"), 36.0f), (PeopleClip("run"), 40f));
            k.Clips(abi, (PeopleClip("fall"), 4.6f), (PeopleClip("idle"), 15.5f), (PeopleClip("look"), 17.2f), (Surprise(), 21.6f), (PeopleClip("jump"), 25.7f), (PeopleClip("land"), 26.5f), (Surprise(), 31f), (PeopleClip("idle"), 33.4f), (PeopleClip("walk"), 36.0f), (PeopleClip("run"), 40f));
            k.Clips(kira, (KiraClip("fall", c.kira), 4.6f), (KiraClip("idle", c.kira), 15.5f), (KiraClip("hurt", c.kira), 20.6f), (KiraClip("attack", c.kira), 20.9f), (KiraClip("idle", c.kira), 21.6f),
                (KiraClip("jump", c.kira), 25.6f), (KiraClip("fall", c.kira), 26.2f), (KiraClip("idle", c.kira), 27f), (KiraClip("fall", c.kira), 36f));
            PulseFrom(k, dan, 37f, L, L);
            // shots, the film's order
            k.Shot("The castle, a purple vortex", 0f, 4.6f, camCastle, new Vector3(0, 24f, 60f), 58f, camCastle + new Vector3(0, .5f, 3f));
            k.Shot("They fall", 4.6f, 7.6f, hay + new Vector3(2.5f, 41f, -3f), hay + new Vector3(0, 52f, 12f), 58f);
            Lamp(k, "Fall light", hay + new Vector3(1.5f, 53f, 10f), new Color(.85f, .75f, 1f), 14f, 3f, 4.6f, 6.2f);
            Lamp(k, "Fall light 2", hay + new Vector3(1.5f, 43f, 2f), new Color(.85f, .75f, 1f), 14f, 3f, 6.2f, 7.6f);
            k.Shot("Top-down: into the hay", 7.6f, 12.6f, hay + new Vector3(.3f, 13f, -.4f), hay + new Vector3(0, 0, .1f), 50f, hay + new Vector3(.2f, 11.5f, -.3f));
            k.Shot("The hay heaves", 12.6f, 15.4f, hay + new Vector3(3.6f, -.1f, -3.8f), hay + new Vector3(0, -.2f, 0), 40f);
            k.Shot("They pop up", 15.4f, 19.8f, hay + new Vector3(0, 1.25f, -2.7f), hay + new Vector3(0, .95f, 0), 44f, hay + new Vector3(0, 1.22f, -2.45f));
            k.Shot("Achoo!", 19.8f, 25.6f, hay + new Vector3(-.3f, 1.25f, -2.4f), hay + new Vector3(-.3f, .9f, 0), 46f);
            k.Shot("Out!", 25.6f, 27.2f, new Vector3(8f, 2.6f, 3.5f), hay + new Vector3(1f, -.8f, -1.6f), 52f);
            k.Shot("The runaway wagon", 27.2f, 31f, danOut + new Vector3(3.4f, 2.0f, -1.6f), new Vector3(-10f, 1.2f, 9.5f), 52f);
            k.Shot("Kaboom", 31f, 33.4f, new Vector3(-14f, 3.4f, 1.5f), new Vector3(-25f, 2f, 10f), 50f);
            k.Shot("The driver, singed", 33.4f, 36f, new Vector3(-19.6f, 1.7f, 5.8f), new Vector3(-23f, 1.3f, 10.6f), 42f);
            k.Shot("Kira flies with them", 36f, L, new Vector3(3.9f, 1.5f, 3.4f), new Vector3(3.9f, 1.25f, 6.6f), 50f, new Vector3(3.9f, 1.5f, -1.6f), new Vector3(3.9f, 1.25f, 1.2f));
            Lamp(k, "Torch light", hay + new Vector3(0, 1.5f, -3f), new Color(1f, .68f, .35f), 9f, 3f, 12.6f, 27.2f);
            Lamp(k, "Walk light", new Vector3(3.9f, 3f, 3f), new Color(1f, .7f, .4f), 10f, 2.5f, 36f, L);
            k.Sound("shard", 4.6f, .5f); k.Sound("hay_rustle", 10.45f, .7f); k.Sound("hay_rustle", 13f, .5f); k.Sound("hay_rustle", 15.9f, .5f); k.Sound("fire_puff", 21f, .7f); k.Sound("hay_fire", 21.4f, .6f);
            k.Sound("horse_neigh", 27f, .6f); k.Sound("wagon_blast", 31.0f, .85f); k.Sound("wings_flap", 36.3f, .5f); k.Sound("wings_flap", 38.5f, .5f);
            k.Save();
            NecklaceModel.Tint = IceAgeCast.shard;
        }

        // ======================================================================================== 8. The stone (04-Medieval_TheStone..., 0-62 s)
        // Muscle Man heaves at the shard in the pedestal, fails, falls back; embarrassed, then smug; points at the shard to challenge Dan;
        // Dan steps up, his necklace glows, he lifts it out with no effort and holds it glowing purple; Muscle Man laughs, then gapes:
        // "My hero!"; the cobblestones swirl up into a column and a whirlpool that swallows them all, Muscle Man too, into the blue-white
        // tunnel. 60 s. The root is the stone (world (0, floor, 34)), +z north (the keep): the party south of it.
        public const float TheStoneLength = 60f;
        public static void TheStone()
        {
            DataAssets.Ensure(); lastQ.Clear();
            var c = MedievalCast; NecklaceModel.Tint = c.shard;
            var k = new Kit("TheStone", TheStoneLength);
            const float L = TheStoneLength;
            var dan = Cast0(k, "Dan", c.dan); var abi = Cast0(k, "Abi", c.abi); var kira = Cast0(k, "Kira", c.kira);
            var mm = Cast0(k, "Muscle Man", Have("muscleman_medieval", ""));
            if (!k.Models.ContainsKey(mm)) Placeholders.Prim(PrimitiveType.Capsule, mm.transform, new Vector3(0, 1f, 0), new Vector3(1f, 1f, .7f), Placeholders.Mat(new Color(.2f, .25f, .4f)), name: "Body");
            // the shard: in the stone, then in Dan's hand
            var shard = k.Holder("Purple shard", Vector3.zero, 0);
            Placeholders.CrystalObject("Crystal", c.shard, .38f).transform.SetParent(shard.transform, false);
            Vector3 inStone = new Vector3(0, 1.15f, 0), danAt = new Vector3(.95f, 0, -.2f); float danFace = -120f;   // east of the stone, three-quarters to the camera (the film)
            var palm = Arms(danAt, danFace, 1.2f, .45f);
            foreach (var (t, p) in new[] { (0f, inStone), (26.3f, inStone), (27.6f, inStone + Vector3.up * .45f), (28.4f, palm), (45f, palm) }) k.Key(shard, "Crystal", "m_LocalPosition", t, p);
            k.Show(shard, 0f, 47f);
            var aura = Ball(k, "Shard aura", inStone + Vector3.up * .2f, Glow(c, .5f), .1f);
            foreach (var (t, s) in new[] { (17f, .05f), (18f, .5f), (19f, .35f), (20f, .55f), (22f, .4f), (24f, .6f), (26.3f, .7f), (27.6f, .05f) }) k.Key(aura, "Glow", "m_LocalScale", t, Vector3.one * s);
            k.Show(aura, 17f, 27.6f);
            // Muscle Man: heaves (north of the stone, facing the camera), falls back, gets up; then beside it, pointing; laughs; gapes; kneels
            Vector3 mmHeave = new Vector3(.05f, 0, .8f), mmSide = new Vector3(-1.45f, 0, .4f), mmRim = new Vector3(5.6f, 0, -4.4f);
            float mmYaw = 120f;
            Path(k, mm, (0f, mmHeave, 180f), (6.9f, mmHeave, 180f), (6.95f, mmSide, mmYaw), (46.5f, mmSide, mmYaw), (46.55f, mmRim, -60f), (52f, mmRim, -60f), (53.5f, mmRim + new Vector3(-.8f, 0, .6f), -55f),
                (55.6f, new Vector3(1.6f, -1.2f, -1.2f), 200f));
            k.Key(mm, "Body", "m_LocalScale", 54.5f, Vector3.one); k.Key(mm, "Body", "m_LocalScale", 55.6f, Vector3.one * .15f);
            AnimationClip MM(string n, string f) => PeopleClip(n) ?? PeopleClip(f);
            k.Clips(mm, (MM("mm_heave", "push"), 0f), (MM("mm_fall", "fall_back"), 5.3f), (MM("nervous", "idle"), 6.95f), (MM("laugh", "idle"), 8.6f), (MM("mm_taunt", "laugh"), 10f), (PeopleClip("idle"), 14.6f),
                (PeopleClip("laugh"), 29.8f), (PeopleClip("surprised"), 33.4f), (MM("mm_kneel", "idle"), 35.6f), (PeopleClip("idle"), 38f), (PeopleClip("surprised"), 46.55f), (PeopleClip("nervous"), 52.5f), (PeopleClip("fall"), 54.2f));
            // Dan steps up (15 -> 17), reaches (21), lifts it out (26.3); Abi comes up beside him with Kira in her arms (36.5)
            var danStart = new Vector3(2.3f, 0, -4.6f);
            Path(k, dan, (0f, danStart, -20f), (15f, danStart, -20f), (16.9f, danAt, danFace), (46.5f, danAt, danFace));
            Vector3 abiStart = new Vector3(3.3f, 0, -4.9f), abiBy = new Vector3(1.75f, 0, -.75f);
            Path(k, abi, (0f, abiStart, -20f), (36.4f, abiStart, -20f), (36.45f, abiBy, -150f), (46.5f, abiBy, -150f));
            var kiraArms = Arms(abiBy, -150f, .95f, .32f);
            Path(k, kira, (0f, new Vector3(2.8f, 0, -4.2f), -20f), (36.4f, new Vector3(2.8f, 0, -4.2f), -20f), (36.45f, kiraArms, -150f), (46.5f, kiraArms, -150f));
            k.Clips(dan, (PeopleClip("idle"), 0f), (PeopleClip("walk"), 15f), (PeopleClip("idle"), 16.9f), (PeopleClip("offer"), 20.8f), (PeopleClip("idle"), 26f), (PeopleClip("offer"), 27.6f), (PeopleClip("surprised"), 38.4f), (PeopleClip("fall"), 47.5f));
            k.Clips(abi, (PeopleClip("idle"), 0f), (PeopleClip("idle"), 36.45f), (PeopleClip("surprised"), 38.4f), (PeopleClip("fall"), 47.5f));
            k.Clips(kira, (KiraClip("idle", c.kira), 0f), (KiraClip("fall", c.kira), 47.5f));
            if (k.Necklaces.TryGetValue(dan, out _)) PulseFrom(k, dan, 17f, 31f, L);
            // the cobblestones: a column twists up (38.5 s), then the whirlpool opens round them (45.5 s) and swallows them
            var cobble = new[] { WorldArt.Mat("Sandstone"), WorldArt.Mat("Cobble"), WorldArt.Mat("Flagstone"), WorldArt.Mat("Sand") };
            Funnel(k, "Stone column", new Vector3(-.5f, 0, -1.9f), .55f, 1.8f, 13f, 200, 280f, 38.5f, 44.5f, 47f, cobble, new Vector3(.55f, .3f, .45f), "stone_whirl");
            var pool = new Vector3(1.3f, 0, -1.6f);
            var warm = new[] { Placeholders.Mat(new Color(.86f, .68f, .48f), "Glow"), Placeholders.Mat(new Color(.72f, .55f, .38f), "Glow"), WorldArt.Mat("Sandstone"), Placeholders.Mat(new Color(.95f, .8f, .58f), "Glow") };   // lit from the glowing heart, as in the film
            Funnel(k, "Whirlpool", pool, .9f, 7f, 1.1f, 360, 150f, 45.3f, 47.3f, 56.2f, warm, new Vector3(.6f, .3f, .5f), "stone_whirl");
            var hot = k.Holder("Whirlpool glow", pool + new Vector3(0, .04f, 0), 0);
            Placeholders.Prim(PrimitiveType.Cylinder, hot.transform, Vector3.zero, new Vector3(.1f, .01f, .1f), Placeholders.Mat(new Color(1f, .62f, .2f), "Glow"), name: "Glow");
            k.Key(hot, "Glow", "m_LocalScale", 45.6f, new Vector3(.1f, .01f, .1f)); k.Key(hot, "Glow", "m_LocalScale", 47.3f, new Vector3(4.2f, .01f, 4.2f)); k.Key(hot, "Glow", "m_LocalScale", 56.2f, new Vector3(4.6f, .01f, 4.6f));
            k.Show(hot, 45.6f, 56.2f);
            Lamp(k, "Whirlpool light", pool + new Vector3(0, 5f, 0), new Color(1f, .62f, .3f), 16f, 4f, 45.3f, 56.2f);
            foreach (var (a, from, ph) in new[] { (dan, danAt, 0f), (abi, abiBy, 1f), (kira, kiraArms, 1f) })
            {
                var vs = new List<(float, Vector3, float)>();
                for (int i = 0; i <= 8; i++)
                {
                    float f = i / 8f, t = Mathf.Lerp(47.5f, 51.6f, f), ang = Mathf.Atan2(from.z - pool.z, from.x - pool.x) + f * 4f + ph * .2f;
                    float r = Mathf.Lerp(new Vector2(from.x - pool.x, from.z - pool.z).magnitude, 0f, f);
                    vs.Add((t, pool + new Vector3(Mathf.Cos(ang) * r, from.y - f * f * 1.8f, Mathf.Sin(ang) * r), (a == dan ? danFace : -150f) + f * 500f));
                }
                Path(k, a, vs.ToArray());
                k.Key(a, "Body", "m_LocalScale", 50.6f, Vector3.one); k.Key(a, "Body", "m_LocalScale", 51.6f, Vector3.one * .1f);
                k.Show(a, 0f, 51.65f);
            }
            k.Show(mm, 0f, 55.65f);
            // the tunnel, high over the castle (a set: nothing in the way), blue and white, the four tumbling down it
            var tun = k.Set("Tunnel", 0, 34f, 0, 420f, false); k.under = tun;
            Spiral(k, "Tunnel spiral", new Vector3(0, -32f, 0), Quaternion.Euler(-90, 0, 0), BlueSwirl, 26f, 55.9f, 56.1f, L, 100f, new Color(.25f, .55f, 1f), 6, false);
            var td = Cast0(k, "Dan tumbling", c.dan); var ta = Cast0(k, "Abi tumbling", c.abi); var tk = Cast0(k, "Kira tumbling", c.kira); var tm = Cast0(k, "Muscle Man tumbling", Have("muscleman_medieval", ""));
            k.Clips(td, (PeopleClip("fall"), 56f)); k.Clips(ta, (PeopleClip("fall"), 56f)); k.Clips(tk, (KiraClip("fall", c.kira), 56f)); k.Clips(tm, (PeopleClip("fall"), 56f));
            Tumble(k, td, new Vector3(-.5f, -2.5f, .2f), new Vector3(0, -14f, 0), 56f, L, 1.2f, 60f, 0f);
            Tumble(k, ta, new Vector3(.6f, -2.8f, -.3f), new Vector3(.3f, -14f, .1f), 56f, L, 1.2f, 60f, .7f);
            Tumble(k, tk, new Vector3(.3f, -2.3f, .5f), new Vector3(.2f, -14f, .3f), 56f, L, 1.8f, 80f, 1.4f);
            Tumble(k, tm, new Vector3(-.9f, -4.5f, -.8f), new Vector3(-.2f, -15f, -.2f), 56.3f, L, 1f, 50f, 2.1f);
            k.Shot("Down the tunnel", 56f, L, new Vector3(0, .5f, .02f), new Vector3(0, -30f, 0), 62f, new Vector3(0, -1.5f, .02f));
            k.under = null;
            TintC(k, Color.white, (0f, 0f), (59.2f, 0f), (59.9f, 1f), (L, 1f));
            // shots, the film's order
            var mmFace = mmSide + new Vector3(0, 1.95f, 0); var mmLook = Fwd(mmYaw);
            k.Shot("He heaves", 0f, 5.3f, new Vector3(-.85f, 1.3f, -2.7f), new Vector3(.05f, 1.1f, .6f), 46f, new Vector3(-.8f, 1.25f, -2.45f));
            k.Shot("Flat", 5.3f, 6.95f, new Vector3(2.7f, 1.5f, 1.0f), new Vector3(-.1f, .5f, 1.7f), 50f);
            k.Shot("Embarrassed, then smug", 6.95f, 10f, mmFace + mmLook * 1.25f + new Vector3(0, -.05f, 0), mmFace + new Vector3(0, -.05f, 0), 40f);
            k.Shot("He points: your turn", 10f, 15f, mmSide + Fwd(mmYaw - 55f) * 2.4f + new Vector3(0, 1.5f, 0), mmSide + new Vector3(.6f, 1.3f, -.2f), 48f);
            k.Shot("Dan steps up", 15f, 20.6f, new Vector3(-.25f, 1.35f, -2.5f), new Vector3(.45f, 1.15f, -.1f), 44f);
            k.Shot("He reaches", 20.6f, 26.2f, new Vector3(-.15f, 1.35f, -1.8f), new Vector3(.45f, 1.2f, -.05f), 40f, new Vector3(-.1f, 1.33f, -1.6f));
            k.Shot("Out it comes", 26.2f, 29.8f, new Vector3(-.1f, 1.4f, -1.6f), new Vector3(.4f, 1.3f, -.1f), 40f);
            k.Shot("He laughs", 29.8f, 33.4f, mmFace + mmLook * 1.35f, mmFace, 40f);
            k.Shot("My hero!", 33.4f, 37f, mmFace + mmLook * 1.15f + new Vector3(0, -.15f, 0), mmFace + new Vector3(0, -.25f, 0), 42f);
            k.Shot("The stones stir", 37f, 40.6f, new Vector3(.2f, 1.5f, -4.4f), new Vector3(.9f, 1.3f, -.6f), 50f);
            k.Shot("They swirl up", 40.6f, 46.5f, new Vector3(1.9f, 1.4f, -3.9f), new Vector3(.3f, 2.2f, -1.2f), 56f, new Vector3(1.85f, 1.3f, -3.6f));
            k.Shot("The whirlpool", 46.5f, 52f, pool + new Vector3(0, 15f, -3f), pool, 58f, pool + new Vector3(0, 12f, -2.2f));
            k.Shot("Muscle Man too", 52f, 56f, mmRim + new Vector3(2.2f, 5.5f, -3.2f), pool + new Vector3(1f, 0, -.3f), 58f);
            Lamp(k, "Torch glow", new Vector3(.3f, 2.6f, -1.8f), new Color(1f, .66f, .35f), 10f, 2.6f, 0f, 47f);
            k.Sound("mm_grunt", .6f, .7f); k.Sound("mm_grunt", 2.2f, .8f); k.Sound("mm_grunt", 3.8f, .9f); k.Sound("mm_laugh", 10.4f, .7f);
            k.Sound("necklace", 17f, .6f); k.Sound("shard_slide", 26.3f, .8f); k.Sound("mm_laugh", 29.9f, .7f); k.Sound("mm_my_hero", 34.3f, .9f); k.Sound("shard", 56f, .6f);
            k.Save();
            NecklaceModel.Tint = IceAgeCast.shard;
        }
    }
}
