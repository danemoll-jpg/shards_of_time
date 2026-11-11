using System.Collections.Generic;
using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace ShardsOfTime
{
    // Round 1, item 11: builds the two cutscenes as real Timeline + Cinemachine assets (Docs/CUTSCENES.md):
    //   Assets/Cutscenes/<Name>.playable              the Timeline (actor animation tracks, clip tracks, a Cinemachine shot track, activation tracks)
    //   Assets/Resources/Cutscenes/<Name>.prefab      the scene: PlayableDirector + actors + shot cameras + effects, bound to the Timeline
    // Made if missing (PlayerBuild); after that they are hand-edited in Unity's Timeline window. "Rebuild Cutscenes" overwrites them
    // (asks first). Everything is placed relative to the prefab's root, which the game puts at the cutscene's anchor.
    // Round 3: the actors are the real models (home outfits: both scenes are in Area A) playing their own clips on Timeline tracks
    // (idle, look, cheer; Kira idle / jump), Dan with his necklace; the graybox actors stay as the fallback when a model is missing.
    // Cutscenes built before the models existed are rebuilt once by EnsureAll.
    public static class CutsceneAuthoring
    {
        public const string TimelineDir = "Assets/Cutscenes", PrefabDir = "Assets/Resources/Cutscenes";

        // Round 4: a cutscene made by an older version of this code is rebuilt (CutsceneVersion on its root). Bump Version when a
        // cutscene's code changes. Hand-edited cutscenes: set their CutsceneVersion to 999 to keep them.
        public const int Version = 17;   // Fix 7.1: the Wise Men, Dan and Abi in their Bethlehem models   // Round 7: Bethlehem's cutscenes (11: Round 6's Egypt)
        static readonly (string name, System.Action build)[] All =
        {
            ("NecklaceWakes", NecklaceWakes), ("PortalOpens", PortalOpens), ("ActOne", ActOne), ("IceAgeArrival", IceAgeArrival),
            ("VortexOpens", VortexOpens), ("ViolinistTeaches", ViolinistTeaches),
            ("EgyptArrival", EgyptArrival), ("SphinxGag", SphinxGag), ("NecklaceWakesEgypt", NecklaceWakesEgypt), ("VortexOpensEgypt", VortexOpensEgypt),
            ("ViolinistTeachesEgypt", ViolinistTeachesEgypt),
            ("BethlehemArrival", BethlehemArrival), ("WiseMenArrive", WiseMenArrive), ("NecklaceWakesBethlehem", NecklaceWakesBethlehem),
            ("ViolinistTeachesBethlehem", ViolinistTeachesBethlehem),
            ("MedievalArrival", MedievalArrival), ("TheStone", TheStone), ("NecklaceWakesMedieval", NecklaceWakesMedieval), ("ViolinistTeachesMedieval", ViolinistTeachesMedieval),   // Round 8
        };

        // Round 6: who plays in a world's story cutscenes (a model not in yet falls back: Dan's / Abi's home models, home Kira) and the
        // colour of its story shard (the necklace's glow follows it: NecklaceModel.Tint)
        sealed class Cast { public string dan, abi, kira; public Color shard; }
        static string Have(string model, string fallback) => CharacterModel.Exists(model) ? model : fallback;
        static Cast IceAgeCast => new Cast { dan = "dan_iceage", abi = "abi_iceage", kira = "kira_sabertooth", shard = new Color(.25f, .55f, 1f) };
        static Cast EgyptCast => new Cast { dan = Have("dan_egypt", "dan_home"), abi = Have("abi_egypt", "abi_home"), kira = Have("kira_mummy", "kira"), shard = new Color(1f, .18f, .14f) };
        static Cast BethlehemCast => new Cast { dan = Have("dan_bethlehem", Have("dan_egypt", "dan_home")), abi = Have("abi_bethlehem", Have("abi_egypt", "abi_home")), kira = Have("kira_lamb", "kira"), shard = new Color(1f, .78f, .2f) };   // Round 7
        static Color Glow(Cast c, float whiten) => Color.Lerp(c.shard, Color.white, whiten);

        public static void EnsureAll()
        {
            foreach (var (name, build) in All) if (Stale(name)) build();
        }

        static bool Stale(string name)
        {
            string path = $"{PrefabDir}/{name}.prefab";
            if (!File.Exists(path)) return true;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var v = prefab ? prefab.GetComponent<CutsceneVersion>() : null;
            return !v || v.version < Version;
        }

        [MenuItem("Shards of Time/Cutscenes/Rebuild Cutscenes")]
        static void RebuildMenu()
        {
            if (!EditorUtility.DisplayDialog("Rebuild cutscenes", "This overwrites every cutscene (Timeline + prefab) with the code-made version. Hand edits are lost.", "Rebuild", "Cancel")) return;
            foreach (var (_, build) in All) build();
        }

        public static void RebuildFromCommandLine() { foreach (var (_, build) in All) build(); }

        // a clip from the people's shared set (Resources/Characters/Clips/<name>.fbx) or from a Kira model's own takes
        static AnimationClip ClipAsset(string file, string clip)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(file))
                if (o is AnimationClip c && !c.name.StartsWith("__preview__") && (c.name == clip || c.name.EndsWith("|" + clip))) return c;
            Debug.LogWarning($"[Cutscene] no clip {clip} in {file}");
            return null;
        }
        public static AnimationClip PeopleClip(string clip) => ClipAsset($"Assets/Resources/Characters/Clips/{clip}.fbx", clip);
        public static AnimationClip KiraClip(string clip, string model = "kira") => ClipAsset($"Assets/Resources/Characters/{model}.fbx", clip);

        // ---- a small authoring kit: an actor / camera / prop is a holder (Animator) with an animated child, so the clips animate
        // child paths only (no root-motion offsets) and the holder stays where it was placed.
        sealed class Kit
        {
            public GameObject root; public PlayableDirector dir; public TimelineAsset tl; public CinemachineTrack shots; public string name;
            readonly Dictionary<GameObject, AnimationClip> clips = new Dictionary<GameObject, AnimationClip>();
            public float duration;
            public readonly Dictionary<GameObject, Animator> Models = new Dictionary<GameObject, Animator>();
            public readonly Dictionary<GameObject, Transform> Necklaces = new Dictionary<GameObject, Transform>();

            public Kit(string name, float duration)
            {
                this.name = name; this.duration = duration;
                Directory.CreateDirectory(TimelineDir); Directory.CreateDirectory(PrefabDir);
                string tlPath = $"{TimelineDir}/{name}.playable";
                AssetDatabase.DeleteAsset(tlPath);
                tl = ScriptableObject.CreateInstance<TimelineAsset>(); tl.name = name;
                AssetDatabase.CreateAsset(tl, tlPath);
                tl.editorSettings.frameRate = 30;
                tl.durationMode = TimelineAsset.DurationMode.FixedLength; tl.fixedDuration = duration;
                root = new GameObject(name);
                dir = root.AddComponent<PlayableDirector>(); dir.playableAsset = tl; dir.playOnAwake = false; dir.extrapolationMode = DirectorWrapMode.None;
                shots = tl.CreateTrack<CinemachineTrack>(null, "Shots");
            }

            public GameObject Holder(string n, Vector3 pos, float yaw)
            {
                var h = new GameObject(n); h.transform.SetParent(root.transform, false); h.transform.localPosition = pos; h.transform.localRotation = Quaternion.Euler(0, yaw, 0);
                h.AddComponent<Animator>();
                return h;
            }

            // an actor: the hero's real model under "Body" (graybox shapes if the model is missing); Models[actor] = its Animator
            public GameObject Actor(string hero, string model, Vector3 pos, float yaw)
            {
                var h = Holder(hero, pos, yaw);
                if (!CharacterModel.Exists(model)) { Placeholders.HeroBody(CharacterStats.Defaults(hero), h.transform); return h; }
                var body = new GameObject("Body").transform; body.SetParent(h.transform, false);
                var m = CharacterModel.Spawn(model, body);
                var anim = m.GetComponentInChildren<Animator>(); anim.applyRootMotion = false; anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                Models[h] = anim;
                if (hero == "Dan")
                {
                    var shard = NecklaceModel.Attach(body, CharacterStats.Defaults(hero), m);
                    if (shard)
                    {
                        var glow = Placeholders.Mat(NecklaceModel.Bright, "Glow");   // a saved tint (prefab-safe)
                        shard.GetComponent<Renderer>().sharedMaterial = glow;
                        var neck = shard.parent.gameObject; neck.AddComponent<Animator>();   // its own Animator: the glow keys stay out of the rig's
                        Necklaces[h] = neck.transform;
                    }
                }
                return h;
            }

            // clips on the actor's model, one after another (each runs to the next start + a short overlap that blends them)
            public void Clips(GameObject actor, params (AnimationClip clip, float start)[] list)
            {
                if (!Models.TryGetValue(actor, out var anim)) return;
                var tr = tl.CreateTrack<AnimationTrack>(null, actor.name + " clips");
                tr.trackOffset = TrackOffset.ApplySceneOffsets;
                for (int i = 0; i < list.Length; i++)
                {
                    if (!list[i].clip) continue;
                    float end = i + 1 < list.Length ? list[i + 1].start + .25f : duration;
                    var c = tr.CreateClip(list[i].clip); c.start = list[i].start; c.duration = Mathf.Max(.1f, end - list[i].start);
                    ((AnimationPlayableAsset)c.asset).removeStartOffset = true;
                }
                dir.SetGenericBinding(tr, anim);
            }

            // a shot: a Cinemachine camera at pos looking at `look` (both in the root's space), used from t0 to t1; `dollyTo` moves it
            public CinemachineCamera Shot(string n, float t0, float t1, Vector3 pos, Vector3 look, float fov = 45f, Vector3? dollyTo = null)
            {
                var h = Holder("Shot " + n, Vector3.zero, 0);
                var camGo = new GameObject("Camera"); camGo.transform.SetParent(h.transform, false);
                camGo.transform.localPosition = pos; camGo.transform.localRotation = Quaternion.LookRotation(look - pos);
                var cam = camGo.AddComponent<CinemachineCamera>(); cam.Priority = -1; cam.Lens.FieldOfView = fov; cam.Lens.NearClipPlane = .05f; cam.Lens.FarClipPlane = 2500f;
                if (dollyTo.HasValue)
                {
                    Key(h, "Camera", "m_LocalPosition", t0, pos); Key(h, "Camera", "m_LocalPosition", t1, dollyTo.Value);
                    var r0 = camGo.transform.localRotation; var r = Quaternion.LookRotation(look - dollyTo.Value);
                    if (Quaternion.Dot(r0, r) < 0) r = new Quaternion(-r.x, -r.y, -r.z, -r.w);   // same hemisphere: no swing (or roll) on the way
                    KeyQ(h, "Camera", t0, r0); KeyQ(h, "Camera", t1, r);
                }
                var clip = shots.CreateClip<CinemachineShot>(); clip.start = t0; clip.duration = t1 - t0; clip.displayName = n;
                var shot = (CinemachineShot)clip.asset; shot.DisplayName = n;
                shot.VirtualCamera.exposedName = System.Guid.NewGuid().ToString("N");
                dir.SetReferenceValue(shot.VirtualCamera.exposedName, cam);
                return cam;
            }

            AnimationClip ClipFor(GameObject holder)
            {
                if (clips.TryGetValue(holder, out var c)) return c;
                c = new AnimationClip { name = holder.name, frameRate = 30 };
                clips[holder] = c; return c;
            }

            // keys on a child path of a holder: vector properties ("m_LocalPosition", "m_LocalScale")
            public void Key(GameObject holder, string path, string prop, float t, Vector3 v)
            {
                var c = ClipFor(holder);
                foreach (var (axis, val) in new[] { ("x", v.x), ("y", v.y), ("z", v.z) })
                {
                    var b = EditorCurveBinding.FloatCurve(path, typeof(Transform), $"{prop}.{axis}");
                    var curve = AnimationUtility.GetEditorCurve(c, b) ?? new AnimationCurve();
                    curve.AddKey(new Keyframe(t, val)); for (int i = 0; i < curve.length; i++) AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                    for (int i = 0; i < curve.length; i++) AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                    AnimationUtility.SetEditorCurve(c, b, curve);
                }
            }
            public void KeyQ(GameObject holder, string path, float t, Quaternion q)
            {
                var c = ClipFor(holder);
                foreach (var (axis, val) in new[] { ("x", q.x), ("y", q.y), ("z", q.z), ("w", q.w) })
                {
                    var b = EditorCurveBinding.FloatCurve(path, typeof(Transform), $"m_LocalRotation.{axis}");
                    var curve = AnimationUtility.GetEditorCurve(c, b) ?? new AnimationCurve();
                    curve.AddKey(new Keyframe(t, val)); AnimationUtility.SetEditorCurve(c, b, curve);
                }
            }
            public void KeyYaw(GameObject holder, string path, float t, float yaw) => KeyQ(holder, path, t, Quaternion.Euler(0, yaw, 0));

            // Round 5: a sound effect (Resources/Sounds/<name>) at t on the cutscene's audio track (plays in game; the MP4 has no audio)
            AudioTrack audio;
            public void Sound(string clipName, float t, float vol)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Resources/Sounds/{clipName}.mp3");
                if (!clip) { Debug.LogWarning("[Cutscene] no sound " + clipName); return; }
                if (audio == null)
                {
                    audio = tl.CreateTrack<AudioTrack>(null, "Sounds");
                    var src = root.AddComponent<AudioSource>(); src.playOnAwake = false; src.spatialBlend = 0;
                    root.AddComponent<CutsceneAudio>();
                    dir.SetGenericBinding(audio, src);
                }
                var c = audio.CreateClip(clip); c.start = t; c.duration = clip.length;
            }

            // a prop shown only from t0 to t1 (activation track)
            public void Show(GameObject go, float t0, float t1)
            {
                var tr = tl.CreateTrack<ActivationTrack>(null, "Show " + go.name);
                var clip = tr.CreateDefaultClip(); clip.start = t0; clip.duration = t1 - t0;
                tr.postPlaybackState = ActivationTrack.PostPlaybackState.Inactive;
                dir.SetGenericBinding(tr, go);
                go.SetActive(false);
            }

            public void Save()
            {
                foreach (var kv in clips)
                {
                    var holder = kv.Key; var clip = kv.Value;
                    AssetDatabase.AddObjectToAsset(clip, tl);
                    var tr = tl.CreateTrack<AnimationTrack>(null, holder.name);
                    var tc = tr.CreateClip(clip); tc.start = 0; tc.duration = duration; tc.displayName = holder.name;
                    dir.SetGenericBinding(tr, holder.GetComponent<Animator>());
                }
                EditorUtility.SetDirty(tl); AssetDatabase.SaveAssets();
                root.AddComponent<CutsceneVersion>().version = Version;
                string path = $"{PrefabDir}/{name}.prefab";
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Cutscene] built {name}: {path} + {TimelineDir}/{name}.playable ({duration} s, {Models.Count} model actors)");
            }
        }

        // The 10th shard of Area A: Dan's necklace wakes. The root is where the player stands, facing the story shard.
        public static void NecklaceWakes() => NecklaceWakesIn("NecklaceWakes", IceAgeCast);
        public static void NecklaceWakesEgypt() => NecklaceWakesIn("NecklaceWakesEgypt", EgyptCast);
        static void NecklaceWakesIn(string name, Cast c)
        {
            DataAssets.Ensure();
            NecklaceModel.Tint = c.shard;
            var k = new Kit(name, 6.5f);
            var dan = k.Actor("Dan", c.dan, Vector3.zero, 0);   // Round 4: it happens in the Ice Age (Round 6: and in Egypt)
            var abi = k.Actor("Abi", c.abi, new Vector3(-1.3f, 0, -1.1f), 30);
            var kira = k.Actor("Kira", c.kira, new Vector3(1.2f, 0, -.8f), -30);
            // the gem grows and glows; an aura blooms; a beam reaches out toward the story shard
            k.Necklaces.TryGetValue(dan, out var neck);
            if (neck)
            {
                var holder = neck.gameObject;
                k.Key(holder, "Shard", "m_LocalScale", 0f, Vector3.one); k.Key(holder, "Shard", "m_LocalScale", 1.2f, Vector3.one * 2.4f);
                k.Key(holder, "Shard", "m_LocalScale", 1.5f, Vector3.one * 1.8f); k.Key(holder, "Shard", "m_LocalScale", 6.5f, Vector3.one * 1.8f);
            }
            else
            {
                k.Key(dan, "Body/Necklace", "m_LocalScale", 0f, new Vector3(.09f, .13f, .05f));
                k.Key(dan, "Body/Necklace", "m_LocalScale", 1.2f, new Vector3(.2f, .3f, .1f));
                k.Key(dan, "Body/Necklace", "m_LocalScale", 1.5f, new Vector3(.16f, .24f, .08f));
                k.Key(dan, "Body/Necklace", "m_LocalScale", 6.5f, new Vector3(.16f, .24f, .08f));
            }
            // the aura blooms round the shard on his chest (a small glow on the real model; the capsule's big one otherwise)
            var gemAt = neck ? k.root.transform.InverseTransformPoint(neck.Find("Shard").position) + new Vector3(0, 0, .03f) : new Vector3(0, 1.15f, .35f);
            float big = neck ? .3f : .7f;
            var aura = k.Holder("Aura", gemAt, 0);
            Placeholders.Prim(PrimitiveType.Sphere, aura.transform, Vector3.zero, Vector3.one * .1f, Placeholders.Mat(Glow(c, .55f), "Glow"), name: "Glow");
            k.Key(aura, "Glow", "m_LocalScale", .6f, Vector3.one * .03f); k.Key(aura, "Glow", "m_LocalScale", 1.6f, Vector3.one * big); k.Key(aura, "Glow", "m_LocalScale", 2.2f, Vector3.one * big * .35f);
            k.Show(aura, .6f, 2.4f);
            var beam = k.Holder("Beam", gemAt + new Vector3(0, 0, .05f), 0);
            Placeholders.Prim(PrimitiveType.Cube, beam.transform, Vector3.zero, new Vector3(.08f, .08f, .01f), Placeholders.Mat(Glow(c, .6f), "Glow"), name: "Ray");
            k.Key(beam, "Ray", "m_LocalPosition", 2.3f, Vector3.zero); k.Key(beam, "Ray", "m_LocalScale", 2.3f, new Vector3(.08f, .08f, .01f));
            k.Key(beam, "Ray", "m_LocalPosition", 4.2f, new Vector3(0, .4f, 14f)); k.Key(beam, "Ray", "m_LocalScale", 4.2f, new Vector3(.1f, .1f, 28f));
            k.Key(beam, "Ray", "m_LocalPosition", 6.5f, new Vector3(0, .6f, 20f)); k.Key(beam, "Ray", "m_LocalScale", 6.5f, new Vector3(.1f, .1f, 40f));
            k.Show(beam, 2.3f, 6.5f);
            // the others react: Kira hops, Abi turns to Dan, Dan looks round for where the light leads
            k.Key(kira, "Body", "m_LocalPosition", 2.4f, Vector3.zero); k.Key(kira, "Body", "m_LocalPosition", 2.65f, new Vector3(0, .45f, 0)); k.Key(kira, "Body", "m_LocalPosition", 2.9f, Vector3.zero);
            k.Key(kira, "Body", "m_LocalPosition", 3.1f, Vector3.zero); k.Key(kira, "Body", "m_LocalPosition", 3.35f, new Vector3(0, .35f, 0)); k.Key(kira, "Body", "m_LocalPosition", 3.6f, Vector3.zero);
            k.KeyYaw(abi, "Body", 0f, 0); k.KeyYaw(abi, "Body", 1.4f, 0); k.KeyYaw(abi, "Body", 2.0f, 25f); k.KeyYaw(abi, "Body", 4.0f, 25f); k.KeyYaw(abi, "Body", 4.6f, -30f);
            if (!k.Models.ContainsKey(dan)) { k.KeyQ(dan, "Body/Head", 0f, Quaternion.identity); k.KeyQ(dan, "Body/Head", .5f, Quaternion.Euler(25, 0, 0)); k.KeyQ(dan, "Body/Head", 2.2f, Quaternion.Euler(25, 0, 0)); k.KeyQ(dan, "Body/Head", 2.7f, Quaternion.identity); }
            k.Clips(dan, (PeopleClip("idle"), 0f), (PeopleClip("look"), 2.4f));
            k.Clips(abi, (PeopleClip("idle"), 0f), (PeopleClip("look"), 4.2f));
            k.Clips(kira, (KiraClip("idle", c.kira), 0f), (KiraClip("jump", c.kira), 2.35f), (KiraClip("idle", c.kira), 3.7f));
            // shots
            k.Shot("Dan's necklace", 0f, 2.3f, new Vector3(.25f, 1.45f, 1.5f), new Vector3(0, 1.3f, 0), 38f, new Vector3(.15f, 1.38f, 1.05f));
            k.Shot("The heroes", 2.3f, 4.4f, new Vector3(3.8f, 1.7f, 4.2f), new Vector3(-.2f, 1f, -.2f), 48f);
            k.Shot("Follow the light", 4.4f, 6.5f, new Vector3(.3f, 2.1f, -4.8f), new Vector3(0, 1.5f, 12f), 52f, new Vector3(.3f, 2.6f, -2.8f));
            k.Save();
            NecklaceModel.Tint = IceAgeCast.shard;
        }

        // ================================================================================================ Round 4 (item 3, 5, 6)
        static Vector3 WorldToLocal(GameObject holder, Vector3 p) => holder.transform.InverseTransformPoint(p);

        // a white-out (or fade back in): a ScreenTint keyed by its scale x
        static void Tint(Kit k, params (float t, float amount)[] keys)
        {
            var h = k.Holder("Screen tint", Vector3.zero, 0);
            var a = new GameObject("Amount"); a.transform.SetParent(h.transform, false); a.AddComponent<ScreenTint>();
            foreach (var (t, v) in keys) k.Key(h, "Amount", "m_LocalScale", t, new Vector3(v, 1, 1));
        }

        // a few glowing rings that grow and turn: the vortex (a saved tint per ring, prefab-safe)
        static GameObject VortexRings(Kit k, string name, Vector3 at, Quaternion facing, float t0, float t1, float size, float tEnd, bool golden = false, Color[] colors = null)
        {
            var h = k.Holder(name, at, 0); h.transform.localRotation = facing;
            Color[] cols = colors != null ? colors : golden ? new[] { new Color(1f, .75f, .25f), new Color(1f, .88f, .5f), new Color(1f, .97f, .8f), new Color(1f, .65f, .2f) }   // Round 7: Bethlehem's golden vortex
                : new[] { new Color(.25f, .45f, 1f), new Color(.55f, .4f, 1f), new Color(.75f, .92f, 1f), new Color(.3f, .7f, 1f) };
            for (int i = 0; i < cols.Length; i++)
            {
                var r = Placeholders.Prim(PrimitiveType.Cylinder, h.transform, new Vector3(0, 0, -i * .02f), Vector3.one * .01f, Placeholders.Mat(cols[i], "Glow"), name: "Ring" + i);
                r.transform.localRotation = Quaternion.Euler(90, 0, 0);
                float s = size * (1f - i * .22f);
                k.Key(h, "Ring" + i, "m_LocalScale", t0, new Vector3(.01f, .01f, .01f));
                k.Key(h, "Ring" + i, "m_LocalScale", t1, new Vector3(s, .01f, s));
                k.Key(h, "Ring" + i, "m_LocalScale", tEnd, new Vector3(s * 1.08f, .01f, s * 1.08f));
                float spin = (i % 2 == 0 ? 1 : -1) * (160f + i * 60f);
                for (int j = 0; j <= 8; j++)
                {
                    float t = Mathf.Lerp(t0, tEnd, j / 8f);
                    k.KeyQ(h, "Ring" + i, t, Quaternion.Euler(90, 0, 0) * Quaternion.Euler(0, spin * (t - t0), 0));
                }
            }
            k.Show(h, t0, tEnd);
            return h;
        }

        static void Puff(Kit k, Vector3 at, float t0, float size)
        {
            var h = k.Holder("Snow puff", at, 0);
            var m = Placeholders.Mat(new Color(.95f, .97f, 1f), "Glow");
            for (int i = 0; i < 5; i++)
            {
                var p = Placeholders.Prim(PrimitiveType.Sphere, h.transform, Vector3.zero, Vector3.one * .1f, m, name: "Bit" + i);
                var dir = Quaternion.Euler(0, i * 72f, 0) * new Vector3(size * .6f, size * .25f, 0);
                k.Key(h, "Bit" + i, "m_LocalPosition", t0, Vector3.zero); k.Key(h, "Bit" + i, "m_LocalPosition", t0 + .5f, dir);
                k.Key(h, "Bit" + i, "m_LocalScale", t0, Vector3.one * .1f); k.Key(h, "Bit" + i, "m_LocalScale", t0 + .25f, Vector3.one * size * .6f); k.Key(h, "Bit" + i, "m_LocalScale", t0 + .8f, Vector3.one * .02f);
            }
            k.Show(h, t0, t0 + .8f);
        }

        // Act I (film): Christmas evening in the living room; Kira bats the dark silver bauble off the tree, it shatters, a vortex opens
        // and pulls the three in; white-out. The root is Game.HomeAnchor (the room is built round it, far from every world).
        public static void ActOne()
        {
            DataAssets.Ensure();
            var k = new Kit("ActOne", 19f);
            k.root.AddComponent<CutsceneMood>();   // warm interior light, no fog, no snow indoors (Core/Look.cs)
            // the room (Tools/Blender/home_room.py), unpacked into the cutscene and coloured
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
            // warm lights: the tree, the room, the wreath, a cool glow from the window
            void Lamp(string n, Vector3 p, Color c, float range, float intensity)
            {
                var l = new GameObject(n); l.transform.SetParent(k.root.transform, false); l.transform.localPosition = p;
                var lt = l.AddComponent<Light>(); lt.type = LightType.Point; lt.color = c; lt.range = range; lt.intensity = intensity; lt.shadows = LightShadows.Soft;
            }
            Lamp("Tree light", new Vector3(2.2f, 1.4f, 2.6f), new Color(1f, .72f, .4f), 6f, 2.2f);
            Lamp("Room light", new Vector3(-.5f, 2.8f, 1.6f), new Color(1f, .82f, .62f), 10f, 1.8f);
            Lamp("Wreath light", new Vector3(-1.9f, 2.1f, 4.3f), new Color(1f, .78f, .45f), 4f, 1.2f);
            Lamp("Window light", new Vector3(2f, 1.8f, 4.5f), new Color(.6f, .7f, 1f), 4f, .8f);
            // the actors in their home clothes; Kira the home cat
            var dan = k.Actor("Dan", "dan_home", new Vector3(1.5f, 0, 2.05f), 40);
            var abi = k.Actor("Abi", "abi_home", new Vector3(.5f, 0, 2.45f), 62);
            var kira = k.Actor("Kira", "kira", new Vector3(2.0f, 0, 1.75f), 15);
            // the ornament: swings when Kira bats it, falls, shatters (shown until it hits the floor)
            var ornAt = orn ? k.root.transform.InverseTransformPoint(orn.position) : new Vector3(2.28f, 1.38f, 2.55f);
            var oh = k.Holder("Ornament holder", Vector3.zero, 0);
            if (orn) { orn.SetParent(oh.transform, true); orn.name = "Ornament"; }
            else { var g = Placeholders.Prim(PrimitiveType.Sphere, oh.transform, ornAt, Vector3.one * .2f, Placeholders.Mat(new Color(.28f, .3f, .35f)), name: "Ornament"); _ = g; }
            k.Key(oh, "Ornament", "m_LocalPosition", 0f, ornAt); k.Key(oh, "Ornament", "m_LocalPosition", 8.2f, ornAt);
            k.Key(oh, "Ornament", "m_LocalPosition", 8.65f, ornAt + new Vector3(-.08f, -.75f, -.15f)); k.Key(oh, "Ornament", "m_LocalPosition", 9.0f, new Vector3(ornAt.x - .16f, .3f, ornAt.z - .3f));
            float[] sw = { 0, 0, 24, -18, 13, -8, 5, 40, 160, 300 }; float[] st = { 0, 7.0f, 7.15f, 7.35f, 7.55f, 7.75f, 7.95f, 8.3f, 8.65f, 9.0f };
            for (int i = 0; i < sw.Length; i++) k.KeyQ(oh, "Ornament", st[i], Quaternion.Euler(sw[i], 0, sw[i] * .3f));
            k.Show(oh, 0f, 9.0f);
            // the shatter: dark silver shards burst from the floor, rise and swirl into the vortex
            var impact = new Vector3(ornAt.x - .16f, .12f, ornAt.z - .3f); var vc = new Vector3(2.0f, 1.45f, 2.05f);
            var sh = k.Holder("Shards", Vector3.zero, 0);
            for (int i = 0; i < 10; i++)
            {
                var c = Placeholders.CrystalObject("Shard" + i, i % 3 == 0 ? new Color(.55f, .85f, 1f) : new Color(.45f, .5f, .62f), .12f, i % 3 == 0 ? "Glow" : "Lit");
                c.transform.SetParent(sh.transform, false);
                float a = i * 36f * Mathf.Deg2Rad; var out1 = impact + new Vector3(Mathf.Cos(a) * .7f, .35f + (i % 3) * .15f, Mathf.Sin(a) * .5f);
                var out2 = impact + new Vector3(Mathf.Cos(a) * 1.0f, .02f, Mathf.Sin(a) * .8f);
                k.Key(sh, "Shard" + i, "m_LocalPosition", 9.0f, impact); k.Key(sh, "Shard" + i, "m_LocalPosition", 9.25f, out1); k.Key(sh, "Shard" + i, "m_LocalPosition", 9.6f, out2);
                k.Key(sh, "Shard" + i, "m_LocalPosition", 10.4f, out2 + Vector3.up * .05f);
                var orbit = vc + new Vector3(Mathf.Cos(a + 2f) * .9f, Mathf.Sin(a * 2) * .3f, Mathf.Sin(a + 2f) * .3f);
                k.Key(sh, "Shard" + i, "m_LocalPosition", 11.6f, orbit); k.Key(sh, "Shard" + i, "m_LocalPosition", 12.6f, vc);
                k.KeyQ(sh, "Shard" + i, 9.0f, Quaternion.identity); k.KeyQ(sh, "Shard" + i, 9.6f, Quaternion.Euler(i * 40, i * 70, 90)); k.KeyQ(sh, "Shard" + i, 12.6f, Quaternion.Euler(i * 40 + 300, i * 70, 270));
            }
            k.Show(sh, 9.0f, 12.6f);
            var flash = k.Holder("Flash", impact, 0);
            Placeholders.Prim(PrimitiveType.Sphere, flash.transform, Vector3.zero, Vector3.one, Placeholders.Mat(new Color(.75f, .9f, 1f), "Glow"), name: "Glow");
            k.Key(flash, "Glow", "m_LocalScale", 9.0f, Vector3.one * .05f); k.Key(flash, "Glow", "m_LocalScale", 9.15f, Vector3.one * .9f); k.Key(flash, "Glow", "m_LocalScale", 9.45f, Vector3.one * .02f);
            k.Show(flash, 9.0f, 9.45f);
            VortexRings(k, "Vortex", vc, Quaternion.identity, 10.4f, 13.2f, 2.8f, 19f);
            // Kira: watches the bauble, jumps and bats it, lands; everyone is pulled into the vortex (Kira first, Dan last)
            k.Key(kira, "Body", "m_LocalPosition", 6.5f, Vector3.zero); k.Key(kira, "Body", "m_LocalPosition", 6.95f, new Vector3(.05f, .85f, .55f));
            k.Key(kira, "Body", "m_LocalPosition", 7.2f, new Vector3(.05f, .8f, .55f)); k.Key(kira, "Body", "m_LocalPosition", 7.55f, new Vector3(-.1f, 0, .2f));
            k.Clips(kira, (KiraClip("idle"), 0f), (KiraClip("jump"), 6.45f), (KiraClip("attack"), 6.95f), (KiraClip("land"), 7.5f), (KiraClip("idle"), 7.9f), (KiraClip("fall"), 14.8f));
            k.Clips(dan, (PeopleClip("idle"), 0f), (PeopleClip("look"), 7.3f), (PeopleClip("hurt"), 9.0f), (PeopleClip("look"), 9.7f), (PeopleClip("fall"), 15.7f));
            k.Clips(abi, (PeopleClip("idle"), 0f), (PeopleClip("look"), 7.0f), (PeopleClip("hurt"), 9.05f), (PeopleClip("look"), 9.8f), (PeopleClip("fall"), 15.2f));
            foreach (var (actor, t0, dur) in new[] { (kira, 14.8f, 1.3f), (abi, 15.2f, 1.5f), (dan, 15.7f, 1.6f) })
            {
                var start = actor == kira ? new Vector3(-.1f, 0, .2f) : Vector3.zero;
                var end = WorldToLocal(actor, vc) - (actor == kira ? Vector3.up * .2f : Vector3.up * .8f);
                k.Key(actor, "Body", "m_LocalPosition", t0, start);
                k.Key(actor, "Body", "m_LocalPosition", t0 + dur * .6f, Vector3.Lerp(start, end, .45f) + Vector3.up * .3f);
                k.Key(actor, "Body", "m_LocalPosition", t0 + dur, end);
                k.Key(actor, "Body", "m_LocalScale", t0, Vector3.one); k.Key(actor, "Body", "m_LocalScale", t0 + dur * .7f, Vector3.one * .8f); k.Key(actor, "Body", "m_LocalScale", t0 + dur, Vector3.one * .02f);
                k.KeyYaw(actor, "Body", t0, 0); k.KeyYaw(actor, "Body", t0 + dur, 540f);
            }
            Tint(k, (17.2f, 0f), (18.3f, 1f), (19f, 1f));
            // shots
            k.Shot("Christmas evening", 0f, 3.6f, new Vector3(-2.6f, 1.7f, -1.2f), new Vector3(1.2f, 1.2f, 3.2f), 50f, new Vector3(-1.8f, 1.55f, -.4f));
            k.Shot("Kira and the ornament", 3.6f, 6.4f, new Vector3(1.3f, .42f, .55f), new Vector3(2.2f, 1.15f, 2.5f), 42f, new Vector3(1.38f, .48f, .75f));
            k.Shot("The swipe", 6.4f, 8.2f, new Vector3(.15f, 1.3f, .2f), new Vector3(2.0f, 1.0f, 2.3f), 45f);
            k.Shot("It falls", 8.2f, 10.2f, new Vector3(1.25f, .7f, 1.0f), new Vector3(2.1f, .55f, 2.3f), 45f);
            k.Shot("The vortex", 10.2f, 14.6f, new Vector3(-.6f, 1.4f, -.9f), new Vector3(1.9f, 1.3f, 2.2f), 50f, new Vector3(-.25f, 1.45f, -.3f));
            k.Shot("Pulled in", 14.6f, 19f, new Vector3(-2.0f, 1.8f, -.8f), new Vector3(1.5f, 1.1f, 2.2f), 52f);
            k.Save();
        }

        // Act II's arrival (Round 5 item 1, Dan's story beat): they land in the snow in caveman clothes. Dan gets up and sees what looks
        // like a sabertooth about to attack: Kira on a snow rock, shot from below in silhouette against the bright sky, growling, fangs.
        // Dan scrambles back and falls flat on his back in fright; she leaps onto his chest; close-up: folded ears, amber eyes: it's
        // Kira (relief). She paws at his chest, at the necklace he didn't know he had; it pulses blue. Abi laughs. No dialogue.
        // The root is the landing spot, facing up the valley (+z).
        // Round 6 (item 5, Dan's Mixamo list): with the "laugh" and "hold_up" clips in, the arrival ends with Dan back on his feet,
        // laughing with relief, then holding the necklace's shard up in his hand to look at it (4 s more)
        static bool ArrivalEnding => File.Exists("Assets/Resources/Characters/Clips/laugh.fbx") && File.Exists("Assets/Resources/Characters/Clips/hold_up.fbx");
        public static float ArrivalLength => ArrivalEnding ? 19.5f : 15.5f;
        public static void IceAgeArrival()
        {
            DataAssets.Ensure();
            var k = new Kit("IceAgeArrival", ArrivalLength);
            var dan = k.Actor("Dan", "dan_iceage", Vector3.zero, 0);
            var abi = k.Actor("Abi", "abi_iceage", new Vector3(-1.4f, 0, .5f), 15);
            // the snow rock up the slope in front of Dan; Kira lands behind it (out of sight) and climbs on top
            var rockAt = new Vector3(.3f, 0, 3.4f); float rockTop = 1.25f;
            var rockFile = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/World/iceage_props.fbx");
            var rockSrc = rockFile ? rockFile.transform.Find("Rock2") : null;
            if (rockSrc)
            {
                var rock = Object.Instantiate(rockSrc.gameObject, k.root.transform, false); rock.name = "Snow rock";
                rock.transform.localPosition = rockAt + Vector3.down * .1f; rock.transform.localRotation = Quaternion.Euler(0, 200, 0) * rockSrc.localRotation;
                rock.transform.localScale = rockSrc.localScale * .85f;
                WorldArt.Dress(rock);
                var b = new Bounds(rock.transform.position, Vector3.zero); foreach (var r in rock.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
                rockTop = b.max.y - k.root.transform.position.y - .08f;
                Debug.Log($"[Cutscene] IceAgeArrival: the snow rock is {b.size.y:0.00} m tall (Kira stands at {rockTop:0.00})");
            }
            var kira = k.Actor("Kira", "kira_sabertooth", rockAt + new Vector3(0, 0, 1.3f), 180);   // facing Dan
            Tint(k, (0f, 1f), (.35f, 1f), (1.3f, 0f));
            foreach (var (actor, land) in new[] { (kira, .6f), (abi, .7f), (dan, .78f) })
            {
                k.Key(actor, "Body", "m_LocalPosition", 0f, new Vector3(0, 4.5f, 0)); k.Key(actor, "Body", "m_LocalPosition", land * .6f, new Vector3(0, 2.2f, 0));
                k.Key(actor, "Body", "m_LocalPosition", land, Vector3.zero);
                if (actor != kira) Puff(k, actor.transform.localPosition + Vector3.up * .1f, land, .9f);
            }
            // Kira climbs onto the rock (while the camera is on Dan's face)
            var onRock = WorldToLocal(kira, k.root.transform.TransformPoint(rockAt + Vector3.up * rockTop));
            k.Key(kira, "Body", "m_LocalPosition", 3.7f, Vector3.zero); k.Key(kira, "Body", "m_LocalPosition", 3.85f, Vector3.Lerp(Vector3.zero, onRock, .5f) + Vector3.up * .6f);
            k.Key(kira, "Body", "m_LocalPosition", 4.0f, onRock);
            // Dan: lands hard, gets up, looks round; flinches at the growl, scrambles back and falls flat on his back (and stays down)
            var fallBack = PeopleClip("fall_back");
            if (!ArrivalEnding)   // (with the ending: one clip track for Dan, made with the ending's clips below)
                k.Clips(dan, (PeopleClip("fall"), 0f), (PeopleClip("land_hard"), .78f), (PeopleClip("look"), 2.75f), (PeopleClip("hurt"), 5.2f),
                    (fallBack ? fallBack : PeopleClip("hurt"), 5.55f));
            var scramble = new Vector3(0, 0, -.6f);
            k.Key(dan, "Body", "m_LocalPosition", 5.2f, Vector3.zero); k.Key(dan, "Body", "m_LocalPosition", 6.0f, scramble); k.Key(dan, "Body", "m_LocalPosition", ArrivalLength, scramble);
            k.Clips(abi, (PeopleClip("fall"), 0f), (PeopleClip("land_hard"), .7f), (PeopleClip("idle"), 2.6f), (PeopleClip("look"), 5.0f), (PeopleClip("cheer"), 12.9f), (PeopleClip("idle"), 14.8f));
            // where his chest and face end up: the fall's last pose, sampled on his model
            var chest = new Vector3(0, .3f, -.95f) + scramble; var face = chest + new Vector3(0, .1f, -.45f);
            if (k.Models.TryGetValue(dan, out var danAnim) && fallBack)
            {
                var bone = danAnim.GetBoneTransform(HumanBodyBones.Chest) ?? danAnim.GetBoneTransform(HumanBodyBones.Spine);
                var head = danAnim.GetBoneTransform(HumanBodyBones.Head);
                if (bone)
                {
                    AnimationMode.StartAnimationMode(); AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(danAnim.gameObject, fallBack, fallBack.length);
                    AnimationMode.EndSampling();
                    chest = k.root.transform.InverseTransformPoint(bone.position) + new Vector3(0, .12f, 0) + scramble;
                    if (head) face = k.root.transform.InverseTransformPoint(head.position) + scramble;
                    AnimationMode.StopAnimationMode();
                    Debug.Log($"[Cutscene] IceAgeArrival: Dan's chest after the fall at {chest}, his face at {face}");
                }
            }
            // she leaps from the rock onto his chest (facing his face) and paws at the necklace
            var onChest = WorldToLocal(kira, k.root.transform.TransformPoint(chest));
            k.Key(kira, "Body", "m_LocalPosition", 7.6f, onRock); k.Key(kira, "Body", "m_LocalPosition", 7.85f, Vector3.Lerp(onRock, onChest, .5f) + Vector3.up * .7f);
            k.Key(kira, "Body", "m_LocalPosition", 8.1f, onChest); k.Key(kira, "Body", "m_LocalPosition", ArrivalLength, onChest);
            k.Clips(kira, (KiraClip("fall", "kira_sabertooth"), 0f), (KiraClip("land", "kira_sabertooth"), .6f), (KiraClip("idle", "kira_sabertooth"), 1.1f),
                (KiraClip("jump", "kira_sabertooth"), 3.65f), (KiraClip("land", "kira_sabertooth"), 3.95f), (KiraClip("idle", "kira_sabertooth"), 4.3f),
                (KiraClip("attack", "kira_sabertooth"), 4.6f), (KiraClip("idle", "kira_sabertooth"), 5.3f),
                (KiraClip("jump", "kira_sabertooth"), 7.55f), (KiraClip("land", "kira_sabertooth"), 8.05f), (KiraClip("idle", "kira_sabertooth"), 8.4f),
                (KiraClip("attack", "kira_sabertooth"), 10.6f), (KiraClip("attack", "kira_sabertooth"), 11.3f), (KiraClip("attack", "kira_sabertooth"), 12.0f),
                (KiraClip("idle", "kira_sabertooth"), 12.7f));
            k.Sound("growl", 4.55f, 1f);
            k.Sound("growl", 5.15f, .7f);
            // the silhouette: the sun behind her, the sky bright, the light on her side of the world down (Look.Backlit)
            var bl = k.Holder("Backlight", Vector3.zero, 0);
            var amount = new GameObject("Amount"); amount.transform.SetParent(bl.transform, false); amount.AddComponent<Backlight>();
            foreach (var (t, v) in new[] { (0f, 0f), (3.98f, 0f), (4.0f, 1f), (5.95f, 1f), (5.97f, 0f) }) k.Key(bl, "Amount", "m_LocalScale", t, new Vector3(v, 1, 1));
            // close under the rock's front edge, looking up at her against the sky: she fills the top of the frame
            var kiraHead = rockAt + Vector3.up * (rockTop + .32f); var camC = rockAt + new Vector3(.12f, rockTop - .1f, -1.4f);
            var sunAt = camC + (kiraHead - camC).normalized * 60f + Vector3.up * 2f;
            var sun = k.Holder("Sun disk", sunAt, 0);
            Placeholders.Prim(PrimitiveType.Sphere, sun.transform, Vector3.zero, Vector3.one * 7f, Placeholders.Mat(new Color(1f, .97f, .85f), "Glow"), name: "Disk");
            k.Show(sun, 4.0f, 5.95f);
            // the necklace wakes: the shard pulses blue and a glow blooms round it
            if (k.Necklaces.TryGetValue(dan, out var neck))
            {
                var holder = neck.gameObject;
                float[] ts = { 0f, 11.2f, 11.6f, 12.0f, 12.4f, 12.8f, 13.2f, 13.6f, ArrivalLength }; float[] ss = { 1f, 1f, 2.3f, 1.5f, 2.3f, 1.5f, 2.1f, 1.6f, 1.6f };
                for (int i = 0; i < ts.Length; i++) k.Key(holder, "Shard", "m_LocalScale", ts[i], Vector3.one * ss[i]);
                var aura = new GameObject("Aura"); aura.transform.SetParent(neck, false);
                var shard = neck.Find("Shard"); aura.transform.localPosition = shard ? shard.localPosition : Vector3.zero;
                Placeholders.Prim(PrimitiveType.Sphere, aura.transform, Vector3.zero, Vector3.one * .1f, Placeholders.Mat(new Color(.4f, .7f, 1f), "Glow"), name: "Glow");
                for (int i = 1; i < ts.Length; i++) k.Key(holder, "Aura/Glow", "m_LocalScale", ts[i], Vector3.one * (i % 2 == 0 ? .28f : .1f));
                k.Show(aura, 11.2f, ArrivalLength);
            }
            // shots
            k.Shot("They land", 0f, 2.6f, new Vector3(3.2f, 1.4f, .2f), new Vector3(-.5f, .8f, .3f), 50f);
            k.Shot("Dan gets up", 2.6f, 4.0f, new Vector3(.55f, 1.6f, 1.9f), new Vector3(0, 1.45f, 0), 40f, new Vector3(.45f, 1.58f, 1.6f));
            k.Shot("Something on the rock", 4.0f, 5.95f, camC, kiraHead + Vector3.up * .05f, 34f, camC + new Vector3(0, -.04f, -.15f));
            k.Shot("Dan falls", 5.95f, 8.3f, new Vector3(3.6f, 1.3f, .2f), new Vector3(.35f, .5f, -.6f), 50f);
            // Dan's view: from just behind and over his head, down at her face (she stands on his chest facing his head)
            var toHead = face - chest; toHead.y = 0; toHead = toHead.sqrMagnitude > .0001f ? toHead.normalized : Vector3.back;
            var kiraFace = chest + Vector3.up * .3f + toHead * .18f;
            var eye = face + toHead * .55f + Vector3.up * .5f;   // over his head, about 0.7 m from her face: her ears and eyes fill the frame
            k.Shot("It's Kira", 8.3f, 10.6f, eye, kiraFace, 40f, Vector3.Lerp(eye, kiraFace, .2f));
            k.Shot("The necklace", 10.6f, 13.0f, chest + new Vector3(1.25f, 1.05f, .35f), chest, 40f, chest + new Vector3(.95f, .8f, .25f));
            if (ArrivalEnding && k.Models.TryGetValue(dan, out var danA))
            {
                k.Shot("Abi laughs", 13.0f, 15.5f, new Vector3(2.4f, 1.7f, 2.4f), new Vector3(-.6f, .7f, -.2f), 48f);
                // the cut hides him getting up: on his feet, laughing; Kira hops down beside him
                k.Clips(dan, (PeopleClip("fall"), 0f), (PeopleClip("land_hard"), .78f), (PeopleClip("look"), 2.75f), (PeopleClip("hurt"), 5.2f),
                    (fallBack ? fallBack : PeopleClip("hurt"), 5.55f), (PeopleClip("laugh"), 15.5f), (PeopleClip("hold_up"), 17.4f));
                var kiraDown = WorldToLocal(kira, k.root.transform.TransformPoint(scramble + new Vector3(.8f, 0, .9f)));
                k.Key(kira, "Body", "m_LocalPosition", 15.45f, onChest); k.Key(kira, "Body", "m_LocalPosition", 15.5f, kiraDown); k.Key(kira, "Body", "m_LocalPosition", 19.5f, kiraDown);
                // the shard in his hand (the one on his chest hides meanwhile)
                var hand = danA.GetBoneTransform(HumanBodyBones.RightHand);
                if (hand && neck)
                {
                    var hs = Placeholders.CrystalObject("Hand shard", NecklaceModel.Bright, 1f); hs.transform.SetParent(hand, false);
                    hs.transform.localPosition = Vector3.zero; hs.transform.position = hand.position + hand.up * .09f;
                    var ls = hand.lossyScale; hs.transform.localScale = new Vector3(.07f / ls.x, .07f / ls.y, .07f / ls.z);
                    k.Show(hs, 17.6f, 19.5f);
                    k.Key(neck.gameObject, "Shard", "m_LocalScale", 17.55f, Vector3.one * 1.6f); k.Key(neck.gameObject, "Shard", "m_LocalScale", 17.6f, Vector3.one * .01f);
                    k.Key(neck.gameObject, "Shard", "m_LocalScale", 19.5f, Vector3.one * .01f);
                }
                k.Shot("Relief", 15.5f, 17.4f, scramble + new Vector3(.4f, 1.6f, 2.6f), scramble + new Vector3(0, 1.45f, 0), 40f);
                k.Shot("The shard in his hand", 17.4f, 19.5f, scramble + new Vector3(.9f, 1.75f, 1.2f), scramble + new Vector3(.2f, 1.6f, .25f), 38f, scramble + new Vector3(.7f, 1.7f, .95f));
            }
            else k.Shot("Abi laughs", 13.0f, ArrivalLength, new Vector3(2.4f, 1.7f, 2.4f), new Vector3(-.6f, .7f, -.2f), 48f);
            k.Save();
        }

        // The story shard opens the vortex (Ice Age). The root is the gate (its +z faces the heroes).
        public static void VortexOpens() => VortexOpensIn("VortexOpens", IceAgeCast);
        public static void VortexOpensEgypt() => VortexOpensIn("VortexOpensEgypt", EgyptCast);
        static void VortexOpensIn(string name, Cast c)
        {
            DataAssets.Ensure();
            NecklaceModel.Tint = c.shard;
            var k = new Kit(name, 6.5f);
            var dan = k.Actor("Dan", c.dan, new Vector3(0, 0, 7.2f), 180);
            var abi = k.Actor("Abi", c.abi, new Vector3(-1.4f, 0, 8f), 170);
            var kira = k.Actor("Kira", c.kira, new Vector3(1.3f, 0, 7.8f), 195);
            var shard = k.Holder("Story shard", Vector3.zero, 0);
            var crystal = Placeholders.CrystalObject("Crystal", c.shard, 1.2f); crystal.transform.SetParent(shard.transform, false);
            k.Key(shard, "Crystal", "m_LocalPosition", 0f, new Vector3(0, 1.5f, 6.5f)); k.Key(shard, "Crystal", "m_LocalPosition", .6f, new Vector3(0, 2.4f, 6.2f));
            k.Key(shard, "Crystal", "m_LocalPosition", 2.0f, new Vector3(0, 2.2f, 0));
            k.Key(shard, "Crystal", "m_LocalScale", 0f, Vector3.one * 1.2f); k.Key(shard, "Crystal", "m_LocalScale", 1.8f, Vector3.one * 1.4f); k.Key(shard, "Crystal", "m_LocalScale", 2.1f, Vector3.one * .1f);
            k.Show(shard, 0f, 2.1f);
            VortexRings(k, "Vortex", new Vector3(0, 2.2f, .05f), Quaternion.identity, 2.0f, 3.0f, 3.9f, 6.5f);
            var flash = k.Holder("Flash", new Vector3(0, 2.2f, .1f), 0);
            Placeholders.Prim(PrimitiveType.Sphere, flash.transform, Vector3.zero, Vector3.one, Placeholders.Mat(new Color(.75f, .9f, 1f), "Glow"), name: "Glow");
            k.Key(flash, "Glow", "m_LocalScale", 2.0f, Vector3.one * .2f); k.Key(flash, "Glow", "m_LocalScale", 2.4f, Vector3.one * 2.8f); k.Key(flash, "Glow", "m_LocalScale", 2.9f, Vector3.one * .1f);
            k.Show(flash, 2.0f, 2.9f);
            k.Clips(dan, (PeopleClip("idle"), 0f), (PeopleClip("cheer"), 3.9f));
            k.Clips(abi, (PeopleClip("idle"), 0f), (PeopleClip("cheer"), 3.7f));
            k.Clips(kira, (KiraClip("idle", c.kira), 0f), (KiraClip("jump", c.kira), 3.55f), (KiraClip("jump", c.kira), 4.25f), (KiraClip("idle", c.kira), 4.9f));
            k.Key(kira, "Body", "m_LocalPosition", 3.6f, Vector3.zero); k.Key(kira, "Body", "m_LocalPosition", 3.85f, new Vector3(0, .5f, 0)); k.Key(kira, "Body", "m_LocalPosition", 4.1f, Vector3.zero);
            k.Key(kira, "Body", "m_LocalPosition", 4.3f, Vector3.zero); k.Key(kira, "Body", "m_LocalPosition", 4.55f, new Vector3(0, .35f, 0)); k.Key(kira, "Body", "m_LocalPosition", 4.8f, Vector3.zero);
            k.Shot("The shard flies", 0f, 2.0f, new Vector3(4.8f, 1.8f, 7f), new Vector3(0, 2f, 3.2f), 50f, new Vector3(4.5f, 2.2f, 5f));
            k.Shot("The vortex opens", 2.0f, 4.0f, new Vector3(.6f, 2.0f, 12f), new Vector3(0, 2.2f, 0), 45f, new Vector3(.4f, 2.1f, 10.5f));
            k.Shot("The heroes cheer", 4.0f, 6.5f, new Vector3(2.8f, 2.4f, 4.4f), new Vector3(0, 1f, 7.6f), 50f);
            k.Save();
            NecklaceModel.Tint = IceAgeCast.shard;
        }

        // The Violinist plays and Dan learns a move. The root is the Violinist (the world's own figure plays: World/Violinist.cs), +z
        // toward Dan. No dialogue.
        public static void ViolinistTeaches() => ViolinistTeachesIn("ViolinistTeaches", "Dan", IceAgeCast.dan, IceAgeCast);
        public static void ViolinistTeachesEgypt() => ViolinistTeachesIn("ViolinistTeachesEgypt", "Abi", EgyptCast.abi, EgyptCast);
        public static void ViolinistTeachesBethlehem() => ViolinistTeachesIn("ViolinistTeachesBethlehem", "Kira", BethlehemCast.kira, BethlehemCast);   // Round 7: Kira's pounce
        static void ViolinistTeachesIn(string name, string learner, string model, Cast c)
        {
            DataAssets.Ensure();
            NecklaceModel.Tint = c.shard;
            var k = new Kit(name, 6.5f);
            bool cat = learner == "Kira"; float hy = cat ? .35f : 1.15f;
            var dan = k.Actor(learner, model, new Vector3(0, 0, cat ? 1.6f : 2.3f), 180);
            if (cat)
            {
                // Kira listens, head cocked; crouches low as the tune rises, then a little practice pounce toward him and back
                k.Clips(dan, (KiraClip("idle", model), 0f), (KiraClip("land", model), 2.6f), (KiraClip("idle", model), 3.3f), (KiraClip("jump", model), 4.6f), (KiraClip("idle", model), 5.4f));
                k.Key(dan, "Body", "m_LocalPosition", 4.6f, Vector3.zero); k.Key(dan, "Body", "m_LocalPosition", 4.95f, new Vector3(0, .55f, .7f)); k.Key(dan, "Body", "m_LocalPosition", 5.3f, new Vector3(0, 0, 1.0f));
                k.Key(dan, "Body", "m_LocalPosition", 6.5f, new Vector3(0, 0, 1.0f));
            }
            else k.Clips(dan, (PeopleClip("idle"), 0f), (PeopleClip("look"), .6f), (PeopleClip("idle"), 2.4f), (PeopleClip("cheer"), 4.6f));
            var aura = k.Holder("Aura", new Vector3(0, hy, cat ? 1.6f : 2.3f), 0);
            Placeholders.Prim(PrimitiveType.Sphere, aura.transform, Vector3.zero, Vector3.one * .1f, Placeholders.Mat(new Color(.6f, .85f, 1f), "Glow"), name: "Glow");
            float[] ts = { 2.5f, 3.0f, 3.4f, 3.8f, 4.2f, 4.7f }; float[] ss = { .05f, 1.1f, .7f, 1.2f, .8f, .05f };
            for (int i = 0; i < ts.Length; i++) k.Key(aura, "Glow", "m_LocalScale", ts[i], Vector3.one * ss[i]);
            k.Show(aura, 2.5f, 4.7f);
            k.Shot("He plays", 0f, 2.5f, new Vector3(.9f, 1.95f, 3.7f), new Vector3(0, 1.55f, 0), 40f, new Vector3(.7f, 1.9f, 3.2f));
            if (cat) k.Shot("Kira learns", 2.5f, 4.6f, new Vector3(-.9f, .55f, .3f), new Vector3(0, .35f, 1.6f), 40f);
            else k.Shot("Dan learns", 2.5f, 4.6f, new Vector3(-.7f, 1.5f, .7f), new Vector3(0, 1.2f, 2.3f), 42f);
            k.Shot("A nod", 4.6f, 6.5f, new Vector3(3.6f, 1.8f, 1.2f), new Vector3(0, cat ? .7f : 1.1f, 1.2f), 45f);
            k.Save();
            NecklaceModel.Tint = IceAgeCast.shard;
        }

        // ================================================================================================ Round 6: Egypt
        // Item 6: the vortex sets them down among the pyramids and the busy workers; they're in Egyptian clothes; Kira is a little mummy
        // cat; Dan and Abi react to her; the necklace's shard glows red. The root is the arrival spot (+z north, the great pyramids
        // far ahead). Staging from SourceArt/Reference/Worlds/02-Egypt/trio-arrival-pyramids-*.png. Skippable.
        public const float EgyptArrivalLength = 13.5f;
        public static void EgyptArrival()
        {
            DataAssets.Ensure();
            var c = EgyptCast; NecklaceModel.Tint = c.shard;
            var k = new Kit("EgyptArrival", EgyptArrivalLength);
            var dan = k.Actor("Dan", c.dan, Vector3.zero, 0);
            var abi = k.Actor("Abi", c.abi, new Vector3(-1.5f, 0, .3f), 10);
            var kira = k.Actor("Kira", c.kira, new Vector3(1.0f, 0, 1.4f), 200);
            Tint(k, (0f, 1f), (.3f, 1f), (1.2f, 0f));
            foreach (var (actor, land) in new[] { (kira, .55f), (abi, .65f), (dan, .75f) })
            {
                k.Key(actor, "Body", "m_LocalPosition", 0f, new Vector3(0, 4f, 0)); k.Key(actor, "Body", "m_LocalPosition", land * .6f, new Vector3(0, 2f, 0));
                k.Key(actor, "Body", "m_LocalPosition", land, Vector3.zero);
                SandPuff(k, actor.transform.localPosition + Vector3.up * .1f, land, actor == kira ? .5f : .9f);
            }
            // workers hauling a stone across the plaza behind them, two more far off with a cart
            foreach (var (z, x0, x1, t0) in new[] { (9f, -9f, 9f, 0f), (22f, 12f, -14f, 0f) })
            {
                var w = k.Holder("Workers " + z, new Vector3(x0, 0, z), x1 > x0 ? 90 : -90);
                var grp = new GameObject("Group"); grp.transform.SetParent(w.transform, false);
                Placeholders.Prim(PrimitiveType.Cube, grp.transform, new Vector3(0, .8f, 0), new Vector3(2.2f, 1.4f, 1.6f), WorldArt.Mat("Sandstone"), name: "Stone");
                for (int i = 0; i < 2; i++)
                {
                    var man = new GameObject("Worker" + i); man.transform.SetParent(grp.transform, false); man.transform.localPosition = new Vector3(i * .8f - .4f, 0, 1.6f);
                    Placeholders.Prim(PrimitiveType.Capsule, man.transform, new Vector3(0, .9f, 0), new Vector3(.45f, .9f, .4f), Placeholders.Mat(new Color(.68f, .46f, .32f)), name: "Body");
                    Placeholders.Prim(PrimitiveType.Cylinder, man.transform, new Vector3(0, .65f, 0), new Vector3(.5f, .2f, .42f), Placeholders.Mat(new Color(.96f, .93f, .85f)), name: "Kilt");
                    Placeholders.Prim(PrimitiveType.Sphere, man.transform, new Vector3(0, 1.85f, 0), new Vector3(.32f, .34f, .32f), Placeholders.Mat(new Color(.2f, .25f, .55f)), name: "Head");
                }
                k.Key(w, "Group", "m_LocalPosition", t0, Vector3.zero);
                k.Key(w, "Group", "m_LocalPosition", EgyptArrivalLength, new Vector3(0, 0, Mathf.Abs(x1 - x0)));
            }
            // Dan and Abi get up and look themselves over (new clothes!), then notice Kira: a little mummy cat
            k.Clips(dan, (PeopleClip("fall"), 0f), (PeopleClip("land"), .75f), (PeopleClip("idle"), 1.6f), (PeopleClip("look"), 3.2f), (PeopleClip("idle"), 6.6f),
                (PeopleClip("hurt"), 7.3f), (PeopleClip("idle"), 8.2f), (PeopleClip("look"), 10.6f), (PeopleClip("idle"), 12.4f));
            k.Clips(abi, (PeopleClip("fall"), 0f), (PeopleClip("land"), .65f), (PeopleClip("idle"), 1.5f), (PeopleClip("look"), 3.4f), (PeopleClip("idle"), 6.6f),
                (PeopleClip("cheer"), 8.0f), (PeopleClip("idle"), 10.2f));
            k.KeyYaw(dan, "Body", 0f, 0); k.KeyYaw(dan, "Body", 6.6f, 0); k.KeyYaw(dan, "Body", 7.0f, 45f); k.KeyYaw(dan, "Body", 12.4f, 45f); k.KeyYaw(dan, "Body", 13f, 0);
            k.KeyYaw(abi, "Body", 0f, 0); k.KeyYaw(abi, "Body", 6.6f, 0); k.KeyYaw(abi, "Body", 7.0f, 30f);
            // Kira: lands, shakes, sits, trots a little circle in her bandages, then sits again looking up at them
            k.Clips(kira, (KiraClip("fall", c.kira), 0f), (KiraClip("land", c.kira), .55f), (KiraClip("idle", c.kira), 1.0f), (KiraClip("walk", c.kira), 5.4f),
                (KiraClip("idle", c.kira), 6.9f), (KiraClip("attack", c.kira), 9.2f), (KiraClip("idle", c.kira), 9.9f));
            k.KeyYaw(kira, "Body", 0f, 0); k.KeyYaw(kira, "Body", 5.4f, 0); k.KeyYaw(kira, "Body", 6.2f, 140f); k.KeyYaw(kira, "Body", 6.9f, 20f);
            k.Key(kira, "Body", "m_LocalPosition", 5.4f, Vector3.zero); k.Key(kira, "Body", "m_LocalPosition", 6.2f, new Vector3(.5f, 0, -.4f)); k.Key(kira, "Body", "m_LocalPosition", 6.9f, new Vector3(.2f, 0, -.6f));
            k.Key(kira, "Body", "m_LocalPosition", EgyptArrivalLength, new Vector3(.2f, 0, -.6f));
            // the necklace glows red
            if (k.Necklaces.TryGetValue(dan, out var neck))
            {
                var holder = neck.gameObject;
                float[] ts = { 0f, 11.4f, 11.8f, 12.2f, 12.6f, 13.0f, EgyptArrivalLength }; float[] ss = { 1f, 1f, 2.2f, 1.5f, 2.2f, 1.6f, 1.6f };
                for (int i = 0; i < ts.Length; i++) k.Key(holder, "Shard", "m_LocalScale", ts[i], Vector3.one * ss[i]);
            }
            var kiraAt = new Vector3(1.0f, 0, 2.0f);   // where she sits from 6.9 s (her actor is turned 200 deg, so her body offset turns too); she faces south-west
            k.Shot("They land", 0f, 2.4f, new Vector3(-4.5f, 1.6f, -3.5f), new Vector3(0, 1f, 1f), 52f);
            k.Shot("Egypt!", 2.4f, 5.2f, new Vector3(.4f, 1.9f, -4.2f), new Vector3(0, 6f, 60f), 50f, new Vector3(.2f, 2.6f, -6.5f));
            k.Shot("New clothes", 5.2f, 7.0f, new Vector3(-.6f, 1.5f, 3.4f), new Vector3(-.7f, 1.2f, 0), 44f, new Vector3(-.5f, 1.45f, 2.8f));
            k.Shot("A little mummy", 7.0f, 9.0f, kiraAt + new Vector3(-.9f, .45f, -1.0f), kiraAt + new Vector3(0, .3f, 0), 40f, kiraAt + new Vector3(-.75f, .42f, -.85f));   // in front of her face; Dan behind the camera
            k.Shot("They react", 9.0f, 11.3f, new Vector3(1.8f, 1.3f, -2.6f), new Vector3(-.4f, 1.1f, .4f), 48f);
            k.Shot("The red shard", 11.3f, EgyptArrivalLength, new Vector3(.6f, 1.55f, 1.2f), new Vector3(0, 1.35f, 0), 36f, new Vector3(.45f, 1.5f, .95f));
            k.Sound("camel_snort", 4.0f, .5f);
            k.Save();
            NecklaceModel.Tint = IceAgeCast.shard;
        }

        // The film's big Egypt gag: they walk up to the Great Sphinx, the camera slowly reveals its face (Kira's), Dan does a big
        // double-take between the Sphinx and mummy Kira (sitting in the same pose), Abi rolls her eyes. Comic timing over length. The root
        // is the spot in front of the Sphinx (Egypt.GagSpot), +z toward it: its paws ~6 m ahead, its face ~21 m ahead and ~17 m up.
        public const float SphinxGagLength = 10.5f;
        public static void SphinxGag()
        {
            DataAssets.Ensure();
            var c = EgyptCast; NecklaceModel.Tint = c.shard;
            var k = new Kit("SphinxGag", SphinxGagLength);
            var dan = k.Actor("Dan", c.dan, new Vector3(0, 0, -3f), 0);
            var abi = k.Actor("Abi", c.abi, new Vector3(-1.5f, 0, -3.6f), 0);
            var kira = k.Actor("Kira", c.kira, new Vector3(1.3f, 0, -2.8f), 0);
            // they walk up (3 m)
            foreach (var a in new[] { dan, abi, kira })
            {
                k.Key(a, "Body", "m_LocalPosition", 0f, Vector3.zero); k.Key(a, "Body", "m_LocalPosition", 3.2f, new Vector3(0, 0, 3f)); k.Key(a, "Body", "m_LocalPosition", SphinxGagLength, new Vector3(0, 0, 3f));
            }
            k.Clips(dan, (PeopleClip("walk"), 0f), (PeopleClip("idle"), 3.2f), (PeopleClip("look"), 4.6f), (PeopleClip("hurt"), 6.2f), (PeopleClip("idle"), 6.8f), (PeopleClip("cheer"), 9.0f));
            k.Clips(abi, (PeopleClip("walk"), 0f), (PeopleClip("idle"), 3.2f), (PeopleClip("look"), 8.3f), (PeopleClip("idle"), 9.8f));
            k.Clips(kira, (KiraClip("walk", c.kira), 0f), (KiraClip("idle", c.kira), 3.2f));
            // Kira turns round and sits facing out, like the Sphinx (its face toward the party: -z)
            k.KeyYaw(kira, "Body", 0f, 0); k.KeyYaw(kira, "Body", 4.8f, 0); k.KeyYaw(kira, "Body", 5.3f, 180f); k.KeyYaw(kira, "Body", SphinxGagLength, 180f);
            // Dan's double-take: Sphinx (ahead), Kira (to his right, down), Sphinx, Kira!, then wide eyes at the camera
            float[] dt = { 0f, 5.4f, 5.6f, 5.95f, 6.15f, 6.5f, 6.7f, 7.6f, 8.0f }; float[] dy = { 0f, 0f, 70f, 70f, 0f, 0f, 80f, 80f, 150f };
            for (int i = 0; i < dt.Length; i++) k.KeyYaw(dan, "Body", dt[i], dy[i]);
            k.Key(dan, "Body", "m_LocalPosition", 6.6f, new Vector3(0, 0, 3f)); k.Key(dan, "Body", "m_LocalPosition", 6.8f, new Vector3(.15f, -.08f, 2.9f)); k.Key(dan, "Body", "m_LocalPosition", 7.2f, new Vector3(0, 0, 3f));
            // Abi rolls her eyes: a look up and away, a turn of the shoulders away from Dan
            k.KeyYaw(abi, "Body", 0f, 0); k.KeyYaw(abi, "Body", 8.2f, 0); k.KeyYaw(abi, "Body", 8.6f, -55f); k.KeyYaw(abi, "Body", 9.6f, -55f); k.KeyYaw(abi, "Body", 10.2f, -20f);
            var sphinxFace = new Vector3(0, 16.5f, 21f);
            k.Shot("Walking up", 0f, 3.6f, new Vector3(.8f, 1.1f, -7.5f), new Vector3(0, 2.5f, 8f), 50f, new Vector3(.6f, .8f, -6.8f));
            k.Shot("The reveal", 3.6f, 5.3f, new Vector3(.2f, .9f, -2f), sphinxFace + new Vector3(0, -6f, 0), 46f, new Vector3(.2f, .7f, -1.6f));   // tilting up toward the face
            k.Shot("Its face", 5.3f, 5.9f, new Vector3(0, 9f, 2f), sphinxFace, 24f);
            k.Shot("Double-take", 5.9f, 7.6f, new Vector3(1.1f, 2.2f, 6.8f), new Vector3(.5f, 1.0f, 2.8f), 46f);   // high enough to see over the plinth's edge: Kira in frame
            k.Shot("Same pose", 7.6f, 8.6f, new Vector3(1.5f, .3f, -2.2f), new Vector3(1.3f, 4.5f, 20f), 60f);   // little Kira in front, the Sphinx's face above: the same cat
            k.Shot("Abi's not impressed", 8.6f, SphinxGagLength, new Vector3(-2.4f, 1.6f, 6.4f), new Vector3(-.9f, 1.4f, 2.6f), 42f);
            k.Sound("camel_snort", 8.7f, .0f);
            k.Save();
            NecklaceModel.Tint = IceAgeCast.shard;
        }

        // ================================================================================================ Round 7: Bethlehem
        // Item 3 (film: barn-roof-landing-top.png, barn-roof-hole-top.png): the golden vortex opens over the barn, the three drop out of
        // it and crash through the roof; Abi lands softly in the hay, Dan misses it and hits the floor; Kira pops up out of the hay as a
        // little gray lamb; the manger glows faintly but is empty; they react to lamb Kira; the necklace glows gold. The root is the barn's
        // middle (world (0, floor, 34)), +z north (the door is at -8, the manger at +3, the hay under the hole at (-3, -1.5)). Skippable.
        public const float BethlehemArrivalLength = 14.5f;
        public static void BethlehemArrival()
        {
            DataAssets.Ensure();
            var c = BethlehemCast; NecklaceModel.Tint = c.shard;
            var k = new Kit("BethlehemArrival", BethlehemArrivalLength);
            var hole = new Vector3(-2.6f, 6.6f, -.9f); var hay = new Vector3(-3f, 0, -1.5f);
            var dan = k.Actor("Dan", c.dan, new Vector3(-.9f, 0, .4f), 200);
            var abi = k.Actor("Abi", c.abi, hay + new Vector3(0, .85f, 0), 150);
            var kira = k.Actor("Kira", c.kira, hay + new Vector3(.9f, 0, .9f), 160);
            // the vortex opens high over the barn (seen from outside), they fall: down to the roof, through the hole
            VortexRings(k, "Golden vortex", new Vector3(0, 20f, 0), Quaternion.Euler(90, 0, 0), 0f, 1.0f, 6f, 2.6f, true);
            foreach (var (actor, land, end, delay) in new[] { (abi, .85f, 3.25f, 0f), (kira, 0f, 3.35f, .1f), (dan, 0f, 3.45f, .2f) })
            {
                var home = actor.transform.localPosition; Vector3 L(Vector3 world) => world - home + new Vector3(0, -land, 0) * 0;
                k.Key(actor, "Body", "m_LocalPosition", 0f, new Vector3(0, 30f, 0));
                k.Key(actor, "Body", "m_LocalPosition", 1.0f + delay, new Vector3(-home.x, 19f - home.y, -home.z));   // out of the vortex
                k.Key(actor, "Body", "m_LocalPosition", 2.55f + delay * .5f, hole - home + new Vector3(0, 1.2f, 0));   // onto the roof
                k.Key(actor, "Body", "m_LocalPosition", end, Vector3.zero);                                          // through it, down
                k.Key(actor, "Body", "m_LocalPosition", BethlehemArrivalLength, Vector3.zero);
                _ = L(Vector3.zero);
            }
            HayPuff(k, hole, 2.6f, 2.2f, new Color(.95f, .78f, .35f));          // thatch bursting
            HayPuff(k, hay + new Vector3(0, .9f, 0), 3.25f, 1.4f, new Color(.95f, .82f, .4f));   // Abi lands in the hay
            HayPuff(k, new Vector3(-.9f, .1f, .4f), 3.45f, .8f, new Color(.6f, .48f, .32f));     // Dan hits the floor
            HayPuff(k, hay + new Vector3(.9f, .5f, .9f), 7.0f, .9f, new Color(.95f, .82f, .4f)); // Kira pops out
            // Kira under the hay until she pops up (7 s): hidden low, then a hop out
            k.Key(kira, "Body", "m_LocalPosition", 3.35f, Vector3.zero); k.Key(kira, "Body", "m_LocalPosition", 3.6f, new Vector3(0, -.6f, 0));
            k.Key(kira, "Body", "m_LocalPosition", 6.95f, new Vector3(0, -.6f, 0)); k.Key(kira, "Body", "m_LocalPosition", 7.3f, new Vector3(0, .55f, .2f));
            k.Key(kira, "Body", "m_LocalPosition", 7.65f, new Vector3(0, 0, .4f)); k.Key(kira, "Body", "m_LocalPosition", BethlehemArrivalLength, new Vector3(0, 0, .4f));
            AnimationClip Surprise() => PeopleClip("surprised") ?? PeopleClip("look");
            k.Clips(dan, (PeopleClip("fall"), 0f), (PeopleClip("fall_back"), 3.3f), (PeopleClip("hurt"), 5.2f), (PeopleClip("idle"), 6.2f), (Surprise(), 8.4f), (PeopleClip("idle"), 10.4f), (PeopleClip("look"), 11.2f), (PeopleClip("idle"), 13.0f));
            k.Clips(abi, (PeopleClip("fall"), 0f), (PeopleClip("land"), 3.2f), (PeopleClip("idle"), 4.0f), (Surprise(), 8.2f), (PeopleClip("laugh") ?? PeopleClip("cheer"), 9.6f), (PeopleClip("idle"), 11.4f));
            k.Clips(kira, (KiraClip("fall", c.kira), 0f), (KiraClip("idle", c.kira), 3.4f), (KiraClip("jump", c.kira), 6.95f), (KiraClip("land", c.kira), 7.5f), (KiraClip("idle", c.kira), 8.0f));
            // Dan gets up off the floor (after the fall-back clip lies him down), then they turn to Kira; Abi climbs out of the hay
            k.KeyYaw(dan, "Body", 0f, 0); k.KeyYaw(dan, "Body", 7.6f, 0); k.KeyYaw(dan, "Body", 8.1f, -60f); k.KeyYaw(dan, "Body", 11.0f, -60f); k.KeyYaw(dan, "Body", 11.6f, 140f); k.KeyYaw(dan, "Body", 13.2f, 140f);
            k.KeyYaw(abi, "Body", 0f, 0); k.KeyYaw(abi, "Body", 7.8f, 0); k.KeyYaw(abi, "Body", 8.2f, 50f);
            k.KeyYaw(kira, "Body", 0f, 0); k.KeyYaw(kira, "Body", 8.0f, 0); k.KeyYaw(kira, "Body", 9.0f, 40f);
            // the manger: a faint glow, empty
            var glow = k.Holder("Manger glow", new Vector3(0, 1.0f, 3f), 0);
            Placeholders.Prim(PrimitiveType.Sphere, glow.transform, Vector3.zero, Vector3.one * .1f, Placeholders.Mat(new Color(1f, .85f, .45f), "Glow"), name: "Glow");
            float[] gt = { 10.8f, 11.4f, 12.2f, 13.0f, BethlehemArrivalLength }; float[] gs = { .05f, .45f, .3f, .5f, .35f };
            for (int i = 0; i < gt.Length; i++) k.Key(glow, "Glow", "m_LocalScale", gt[i], Vector3.one * gs[i]);
            k.Show(glow, 10.8f, BethlehemArrivalLength);
            if (k.Necklaces.TryGetValue(dan, out var neck))
            {
                float[] ts = { 0f, 13.2f, 13.6f, 14.0f, BethlehemArrivalLength }; float[] ss = { 1f, 1f, 2.2f, 1.5f, 1.8f };
                for (int i = 0; i < ts.Length; i++) k.Key(neck.gameObject, "Shard", "m_LocalScale", ts[i], Vector3.one * ss[i]);
            }
            var kiraAt = hay + new Vector3(.9f, 0, 1.3f);
            k.Shot("The star opens", 0f, 1.6f, new Vector3(16f, 4f, -30f), new Vector3(0, 12f, 0), 46f, new Vector3(14f, 5f, -27f));
            k.Shot("Onto the roof", 1.6f, 2.9f, new Vector3(6f, 21f, -11f), hole, 44f);
            k.Shot("Crash", 2.9f, 5.0f, new Vector3(3.6f, 2.0f, -6.2f), new Vector3(-2.2f, 1.0f, -.6f), 50f);
            k.Shot("Dan on the floor", 5.0f, 6.9f, new Vector3(1.6f, 1.3f, -2.4f), new Vector3(-.9f, .3f, .4f), 46f);
            k.Shot("A lamb!", 6.9f, 8.6f, kiraAt + new Vector3(.9f, .6f, -1.9f), kiraAt + new Vector3(0, .35f, 0), 42f, kiraAt + new Vector3(.75f, .55f, -1.6f));
            k.Shot("They stare", 8.6f, 10.8f, new Vector3(1.6f, 1.4f, -3.6f), new Vector3(-1.6f, .9f, -.3f), 48f);
            k.Shot("The empty manger", 10.8f, 13.2f, new Vector3(-.6f, 1.6f, -1.2f), new Vector3(0, .9f, 3f), 44f, new Vector3(-.4f, 1.5f, -.4f));
            k.Shot("The golden shard", 13.2f, BethlehemArrivalLength, new Vector3(-1.3f, 1.45f, 1.6f), new Vector3(-.9f, 1.3f, .4f), 36f);   // in front of Dan (his holder 200 + body 140 deg: he faces about north by then)
            k.Sound("roof_crash", 2.6f, .8f); k.Sound("hay_rustle", 3.25f, .6f); k.Sound("sheep_bleat", 7.2f, .55f);
            k.Save();
            NecklaceModel.Tint = IceAgeCast.shard;
        }

        // Item 10 (film): the golden shard is collected; the Wise Men walk in, presents held out, the choir swells ("Hallelujah!"); they
        // see a glowing shard and three confused travellers (one a cat-faced lamb), look at each other and walk out disappointed; then the
        // hay bursts up into a spinning golden vortex that pulls the party in. The root is in the barn 4 m south of the manger (world
        // (0, floor, 33)), +z north: the party by the manger (z +2..+3), the door at -7, the hay at (-3, -.5). Skippable.
        public const float WiseMenLength = 16.5f;
        public static void WiseMenArrive()
        {
            DataAssets.Ensure();
            var c = BethlehemCast; NecklaceModel.Tint = c.shard;
            var k = new Kit("WiseMenArrive", WiseMenLength);
            var dan = k.Actor("Dan", c.dan, new Vector3(.6f, 0, 2.6f), 185);
            var abi = k.Actor("Abi", c.abi, new Vector3(-.9f, 0, 2.3f), 170);
            var kira = k.Actor("Kira", c.kira, new Vector3(.05f, 0, 1.7f), 180);
            // the shard in Dan's hand, held up
            var shard = k.Holder("Golden shard", new Vector3(.75f, 1.45f, 2.25f), 0);
            Placeholders.CrystalObject("Crystal", c.shard, .9f).transform.SetParent(shard.transform, false);
            k.Show(shard, 0f, 13.0f);
            // the Wise Men, presents held out in front
            string[] models = { "wiseman_santa", "wiseman_crown", "wiseman_old" }; string[] gifts = { "GiftRed", "GiftGreen", "GiftBlue" };
            Color[] robes = { new Color(.5f, .14f, .12f), new Color(.95f, .9f, .78f), new Color(.35f, .45f, .2f) };
            var props = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/World/bethlehem_props.fbx");
            var men = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 1.15f;
                var m = men[i] = WiseMan(k, "Wise Man " + i, models[i], new Vector3(x, 0, -10f), 0, robes[i], i);
                if (props && props.transform.Find(gifts[i]) is Transform g)
                {
                    var gi = Object.Instantiate(g.gameObject); gi.name = "Present"; WorldArt.Dress(gi);
                    var body = m.transform.Find("Body"); gi.transform.SetParent(body ? body : m.transform, false);
                    gi.transform.localPosition = CharacterModel.Exists(models[i]) ? new Vector3(0, .78f, .36f) : new Vector3(0, 1.05f, .42f);   // Fix 7.1: in the real men's hands, below their beards gi.transform.localRotation = g.localRotation; gi.transform.localScale = g.localScale;
                }
                // walk in (3 s), stand, look round at each other, turn and walk out
                float stop = -3.6f - Mathf.Abs(i - 1) * .3f;
                k.Key(m, "Body", "m_LocalPosition", 0f, Vector3.zero); k.Key(m, "Body", "m_LocalPosition", 3.2f + i * .15f, new Vector3(0, 0, 10f + stop));
                k.Key(m, "Body", "m_LocalPosition", 11.2f, new Vector3(0, 0, 10f + stop)); k.Key(m, "Body", "m_LocalPosition", 14.2f, new Vector3(0, 0, 1.5f));
                k.Key(m, "Body", "m_LocalPosition", WiseMenLength, new Vector3(0, 0, 1.5f));
                float[] yt = { 0f, 8.6f, 9.0f, 9.6f, 10.0f, 10.8f, 11.4f, WiseMenLength }; float look = i == 0 ? 50f : i == 2 ? -50f : 40f;
                float[] yy = { 0f, 0f, look, look, -look, -look, 180f, 180f };
                for (int j = 0; j < yt.Length; j++) k.KeyYaw(m, "Body", yt[j], yy[j]);
                AnimationClip offer = PeopleClip("offer") ?? PeopleClip("idle"), sad = PeopleClip("sad") ?? PeopleClip("idle");
                k.Clips(m, (PeopleClip("walk"), 0f), (offer, 3.2f + i * .15f), (sad, 8.6f), (PeopleClip("walk"), 11.4f));
            }
            // the party: Dan holds up the shard, Abi gives a little wave, Kira stares (then they're pulled into the vortex)
            k.Clips(dan, (PeopleClip("hold_up") ?? PeopleClip("idle"), 0f), (PeopleClip("idle"), 6.8f), (PeopleClip("surprised") ?? PeopleClip("look"), 13.0f), (PeopleClip("fall"), 14.4f));
            k.Clips(abi, (PeopleClip("idle"), 0f), (PeopleClip("cheer"), 6.0f), (PeopleClip("idle"), 7.4f), (PeopleClip("surprised") ?? PeopleClip("look"), 13.0f), (PeopleClip("fall"), 14.4f));
            k.Clips(kira, (KiraClip("idle", c.kira), 0f), (KiraClip("jump", c.kira), 13.3f), (KiraClip("fall", c.kira), 14.4f));
            // the hay bursts into a golden vortex (13 s) and pulls them in (14.4 -> 15.6 s)
            var vort = new Vector3(-3f, 2.4f, -.6f);
            HayPuff(k, new Vector3(-3f, .8f, -.5f), 13.0f, 3f, new Color(.98f, .82f, .35f));
            VortexRings(k, "Hay vortex", vort, Quaternion.Euler(0, 90, 0), 13.1f, 14.0f, 3.6f, WiseMenLength, true);
            foreach (var (a, h) in new[] { (dan, 1.1f), (abi, 1.0f), (kira, .3f) })
            {
                var home = a.transform.localPosition;
                k.Key(a, "Body", "m_LocalPosition", 14.4f, Vector3.zero);
                k.Key(a, "Body", "m_LocalPosition", 15.6f, vort - home - new Vector3(0, h, 0));
                k.Key(a, "Body", "m_LocalPosition", WiseMenLength, vort - home - new Vector3(0, h, 0));
                k.Key(a, "Body", "m_LocalScale", 14.4f, Vector3.one); k.Key(a, "Body", "m_LocalScale", 15.6f, Vector3.one * .05f); k.Key(a, "Body", "m_LocalScale", WiseMenLength, Vector3.one * .05f);
            }
            Tint(k, (0f, 0f), (15.4f, 0f), (16.2f, 1f), (WiseMenLength, 1f));
            k.Shot("They come in", 0f, 3.4f, new Vector3(.4f, 1.75f, 4.6f), new Vector3(0, 1.4f, -6f), 46f, new Vector3(.3f, 1.7f, 4.1f));
            k.Shot("Gifts held out", 3.4f, 6.0f, new Vector3(.2f, 1.6f, -.8f), new Vector3(0, 1.45f, -4f), 44f);
            k.Shot("What they find", 6.0f, 8.6f, new Vector3(-.1f, 1.45f, -2.6f), new Vector3(0, .95f, 2.2f), 44f, new Vector3(-.1f, 1.35f, -2.1f));
            k.Shot("They look at each other", 8.6f, 11.2f, new Vector3(.5f, 1.7f, -1.0f), new Vector3(0, 1.5f, -3.8f), 42f);
            k.Shot("And leave", 11.2f, 13.0f, new Vector3(5.2f, 2.4f, -1.5f), new Vector3(0, 1.2f, -4f), 50f);
            k.Shot("The hay bursts", 13.0f, WiseMenLength, new Vector3(3.8f, 2.6f, 5.6f), new Vector3(-2f, 1.6f, -.3f), 52f, new Vector3(3.2f, 2.5f, 4.8f));
            k.Sound("choir_hallelujah", 1.4f, .8f); k.Sound("hay_rustle", 13.0f, .7f); k.Sound("necklace", 13.4f, .6f);
            k.Save();
            NecklaceModel.Tint = IceAgeCast.shard;
            _ = men;
        }

        public static void NecklaceWakesBethlehem() => NecklaceWakesIn("NecklaceWakesBethlehem", BethlehemCast);

        // a Wise Man for a cutscene: his model (Dan's Mixamo rig) when it is in, else a robe, a beard and a Santa hat / crown
        static GameObject WiseMan(Kit k, string n, string model, Vector3 pos, float yaw, Color robe, int i)
        {
            if (CharacterModel.Exists(model)) return k.Actor(n, model, pos, yaw);
            var h = k.Holder(n, pos, yaw); var body = new GameObject("Body").transform; body.SetParent(h.transform, false);
            var skin = Placeholders.Mat(new Color(.82f, .62f, .48f));
            Placeholders.Prim(PrimitiveType.Capsule, body, new Vector3(0, .75f, 0), new Vector3(.6f, .75f, .5f), Placeholders.Mat(robe), name: "Robe");
            var head = Placeholders.Prim(PrimitiveType.Sphere, body, new Vector3(0, 1.66f, 0), new Vector3(.36f, .38f, .36f), skin, name: "Head").transform;
            Placeholders.Prim(PrimitiveType.Sphere, head, new Vector3(0, -.45f, .25f), new Vector3(.8f, .7f, .55f), Placeholders.Mat(i == 2 ? new Color(.96f, .96f, .94f) : i == 0 ? new Color(.1f, .08f, .07f) : new Color(.45f, .28f, .15f)), name: "Beard");
            if (i == 0) Placeholders.Prim(PrimitiveType.Cylinder, head, new Vector3(0, .5f, -.05f), new Vector3(1.05f, .35f, 1.05f), Placeholders.Mat(new Color(.85f, .1f, .12f)), name: "Santa hat");
            else Placeholders.Prim(PrimitiveType.Cylinder, head, new Vector3(0, .5f, 0), new Vector3(.95f, .25f, .95f), Placeholders.Mat(new Color(.95f, .72f, .25f)), name: "Crown");
            return h;
        }

        static void HayPuff(Kit k, Vector3 at, float t0, float size, Color col)
        {
            var h = k.Holder("Hay puff", at, 0);
            var m = Placeholders.Mat(col, "Glow");
            for (int i = 0; i < 7; i++)
            {
                Placeholders.Prim(PrimitiveType.Cube, h.transform, Vector3.zero, Vector3.one * .1f, m, name: "Bit" + i);
                var dir = Quaternion.Euler(0, i * 51f, 0) * new Vector3(size * .55f, size * (.3f + .1f * (i % 3)), 0);
                k.Key(h, "Bit" + i, "m_LocalPosition", t0, Vector3.zero); k.Key(h, "Bit" + i, "m_LocalPosition", t0 + .6f, dir);
                k.Key(h, "Bit" + i, "m_LocalScale", t0, Vector3.one * .05f); k.Key(h, "Bit" + i, "m_LocalScale", t0 + .25f, new Vector3(.4f, .06f, .12f) * size); k.Key(h, "Bit" + i, "m_LocalScale", t0 + .9f, Vector3.one * .02f);
            }
            k.Show(h, t0, t0 + .9f);
        }

        static void SandPuff(Kit k, Vector3 at, float t0, float size)
        {
            var h = k.Holder("Sand puff", at, 0);
            var m = Placeholders.Mat(new Color(.95f, .82f, .58f), "Glow");
            for (int i = 0; i < 5; i++)
            {
                Placeholders.Prim(PrimitiveType.Sphere, h.transform, Vector3.zero, Vector3.one * .1f, m, name: "Bit" + i);
                var dir = Quaternion.Euler(0, i * 72f, 0) * new Vector3(size * .6f, size * .25f, 0);
                k.Key(h, "Bit" + i, "m_LocalPosition", t0, Vector3.zero); k.Key(h, "Bit" + i, "m_LocalPosition", t0 + .5f, dir);
                k.Key(h, "Bit" + i, "m_LocalScale", t0, Vector3.one * .1f); k.Key(h, "Bit" + i, "m_LocalScale", t0 + .25f, Vector3.one * size * .6f); k.Key(h, "Bit" + i, "m_LocalScale", t0 + .8f, Vector3.one * .02f);
            }
            k.Show(h, t0, t0 + .8f);
        }

        // ================================================================================================ Round 8: the Medieval Kingdom
        static Cast MedievalCast => new Cast { dan = Have("dan_medieval", Have("dan_bethlehem", "dan_home")), abi = Have("abi_medieval", Have("abi_bethlehem", "abi_home")), kira = Have("kira_dragon", "kira"), shard = new Color(.62f, .32f, 1f) };
        static readonly Color[] PurpleRings = { new Color(.55f, .3f, 1f), new Color(.75f, .5f, 1f), new Color(.92f, .82f, 1f), new Color(.45f, .2f, .85f) };
        public static void NecklaceWakesMedieval() => NecklaceWakesIn("NecklaceWakesMedieval", MedievalCast);
        public static void ViolinistTeachesMedieval() => ViolinistTeachesIn("ViolinistTeachesMedieval", "Dan", MedievalCast.dan, MedievalCast);   // Round 8: Dan's charged punch

        static GameObject MedProp(string prop, Transform parent, Vector3 localPos, float yaw)
        {
            var file = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/World/medieval_props.fbx");
            var src = file ? file.transform.Find(prop) : null;
            if (!src) return null;
            var go = Object.Instantiate(src.gameObject); go.name = prop; WorldArt.Dress(go);
            go.transform.SetParent(parent, false); go.transform.localPosition = localPos; go.transform.localRotation = Quaternion.Euler(0, yaw, 0) * src.localRotation; go.transform.localScale = src.localScale;
            return go;
        }

        // fire: a few glowing tongues under `parent` that grow from t0 to t1 (and flicker)
        static void Flames(Kit k, GameObject holder, string path, Vector3 at, float t0, float t1, float size, float tEnd)
        {
            var parent = path == "" ? holder.transform : holder.transform.Find(path);
            var f = new GameObject("Fire"); f.transform.SetParent(parent, false); f.transform.localPosition = at;
            string fp = (path == "" ? "" : path + "/") + "Fire";
            for (int i = 0; i < 6; i++)
            {
                var c = Placeholders.Prim(PrimitiveType.Sphere, f.transform, new Vector3(Mathf.Cos(i * 1.7f) * .5f, .1f * (i % 3), Mathf.Sin(i * 1.7f) * .9f), Vector3.one * .01f, Placeholders.Mat(i % 2 == 0 ? new Color(1f, .5f, .12f) : new Color(1f, .82f, .3f), "Glow"), name: "Tongue" + i);
                for (float t = t0; t <= tEnd + .01f; t += .35f)
                {
                    float grow = Mathf.Clamp01((t - t0) / Mathf.Max(.01f, t1 - t0)), flick = 1f + .25f * Mathf.Sin(t * 9f + i);
                    k.Key(holder, fp + "/Tongue" + i, "m_LocalScale", t, new Vector3(.6f, 1.1f + .3f * (i % 2), .6f) * size * Mathf.Max(.02f, grow) * flick);
                }
            }
        }

        // a big cartoon blast at `at`: orange and yellow balls swell and fade, a white flash
        static void Blast(Kit k, Vector3 at, float t0, float size)
        {
            var h = k.Holder("Blast", at, 0);
            Color[] cols = { new Color(1f, .55f, .15f), new Color(1f, .85f, .35f), new Color(1f, .97f, .85f), new Color(.35f, .3f, .28f) };
            for (int i = 0; i < 9; i++)
            {
                Placeholders.Prim(PrimitiveType.Sphere, h.transform, Vector3.zero, Vector3.one * .1f, Placeholders.Mat(cols[i % 4], i % 4 == 3 ? "Lit" : "Glow"), name: "Ball" + i);
                var dir = Quaternion.Euler(Random.Range(-10f, 60f), i * 40f, 0) * Vector3.forward * size * (.4f + .1f * (i % 3));
                k.Key(h, "Ball" + i, "m_LocalPosition", t0, Vector3.zero); k.Key(h, "Ball" + i, "m_LocalPosition", t0 + .5f, dir); k.Key(h, "Ball" + i, "m_LocalPosition", t0 + 1.6f, dir * 1.4f + Vector3.up * size * .5f);
                k.Key(h, "Ball" + i, "m_LocalScale", t0, Vector3.one * .1f); k.Key(h, "Ball" + i, "m_LocalScale", t0 + .3f, Vector3.one * size * (.8f + .15f * (i % 3))); k.Key(h, "Ball" + i, "m_LocalScale", t0 + 1.8f, Vector3.one * .05f);
            }
            k.Show(h, t0, t0 + 1.9f);
        }

        // Item 3 (Dan's version of the film: hay-wagon-landing.png, wagon-on-fire.png): the purple vortex opens over the courtyard; the party
        // drops into a horse-drawn hay wagon; they pop up in medieval clothes, Kira a baby dragon; she sneezes a puff of fire and the hay catches;
        // they all leap out; the pony bolts, the wagon (driver aboard) careens across the courtyard into the west wall: a cartoon blast. The
        // driver staggers out singed, his hat smoking; the pony trots out fine; the party slinks off. The root is in the courtyard at world
        // (0, floor, 14), +z north: the wagon at (2, 0, 10) facing west, the west wall at x -30 (the wreck stays at world (-25.5, 0, 24)).
        public const float MedievalArrivalLength = 17f;
        public static void MedievalArrival()
        {
            DataAssets.Ensure();
            var c = MedievalCast; NecklaceModel.Tint = c.shard;
            var k = new Kit("MedievalArrival", MedievalArrivalLength);
            var hay = new Vector3(2f, 1.55f, 10f);
            var dan = k.Actor("Dan", c.dan, new Vector3(3.6f, 0, 6.6f), 160);
            var abi = k.Actor("Abi", c.abi, new Vector3(.8f, 0, 6.8f), 200);
            var kira = k.Actor("Kira", c.kira, new Vector3(2.4f, 0, 6.2f), 180);
            // the wagon: a holder at its start, facing west (the prop's front is its -z); the cart, its fire, then the run
            var wagon = k.Holder("Wagon", new Vector3(2, 0, 10), 90);
            var cart = new GameObject("Cart"); cart.transform.SetParent(wagon.transform, false);
            if (!MedProp("HayWagon", cart.transform, Vector3.zero, 0))
            {
                Placeholders.Prim(PrimitiveType.Cube, cart.transform, new Vector3(0, .8f, 0), new Vector3(2f, .6f, 4f), WorldArt.Mat("Wood"), name: "Bed");
                Placeholders.Prim(PrimitiveType.Sphere, cart.transform, new Vector3(0, 1.5f, 0), new Vector3(2f, 1f, 3.6f), WorldArt.Mat("Hay"), name: "Hay");
            }
            Flames(k, wagon, "Cart", new Vector3(0, 1.6f, .3f), 6.4f, 8.6f, .9f, 12.1f);
            k.Key(wagon, "Cart", "m_LocalPosition", 0f, Vector3.zero); k.Key(wagon, "Cart", "m_LocalPosition", 8.9f, Vector3.zero);
            k.Key(wagon, "Cart", "m_LocalPosition", 10.4f, new Vector3(0, 0, -8f)); k.Key(wagon, "Cart", "m_LocalPosition", 12.0f, new Vector3(0, 0, -27.3f));
            k.Show(wagon, 0f, 12.05f);
            // the pony pulls it (the shafts' end: the cart's -z, 3.6 m), bolts with it and swerves off just before the wall
            var pony = k.Actor("Pony", Have("horse", ""), new Vector3(2 - 3.6f, 0, 10), -90);
            k.Key(pony, "Body", "m_LocalPosition", 0f, Vector3.zero); k.Key(pony, "Body", "m_LocalPosition", 8.9f, Vector3.zero);
            k.Key(pony, "Body", "m_LocalPosition", 10.4f, new Vector3(0, 0, 8f)); k.Key(pony, "Body", "m_LocalPosition", 11.5f, new Vector3(-2f, 0, 19f));
            k.Key(pony, "Body", "m_LocalPosition", 12.6f, new Vector3(-6.5f, 0, 21f)); k.Key(pony, "Body", "m_LocalPosition", 14.5f, new Vector3(-9f, 0, 19.5f)); k.Key(pony, "Body", "m_LocalPosition", MedievalArrivalLength, new Vector3(-9.5f, 0, 19f));
            k.KeyYaw(pony, "Body", 11.2f, 0); k.KeyYaw(pony, "Body", 12.4f, -70f); k.KeyYaw(pony, "Body", 14.5f, -120f);
            k.Clips(pony, (KiraClip("idle", "horse"), 0f), (KiraClip("run", "horse"), 8.6f), (KiraClip("walk", "horse"), 12.8f), (KiraClip("idle", "horse"), 14.6f));
            // the driver, asleep on the seat (sitting), carried off with the wagon; then out of the smoke, singed (a new actor by the wreck)
            var driver = k.Actor("Driver", Have("wagon_driver", ""), new Vector3(2 - 1.6f, 1.05f, 10), -90);
            k.Key(driver, "Body", "m_LocalPosition", 0f, Vector3.zero); k.Key(driver, "Body", "m_LocalPosition", 8.9f, Vector3.zero);
            k.Key(driver, "Body", "m_LocalPosition", 10.4f, new Vector3(0, 0, 8f)); k.Key(driver, "Body", "m_LocalPosition", 12.0f, new Vector3(0, 0, 27.3f));
            k.Clips(driver, (PeopleClip("sit") ?? PeopleClip("idle"), 0f), (PeopleClip("surprised") ?? PeopleClip("idle"), 8.7f), (PeopleClip("sit") ?? PeopleClip("idle"), 9.8f));
            k.Show(driver, 0f, 12.05f);
            var singed = k.Actor("Driver singed", Have("wagon_driver", ""), new Vector3(-23f, 0, 9.5f), 120);
            k.Key(singed, "Body", "m_LocalPosition", 12.6f, Vector3.zero); k.Key(singed, "Body", "m_LocalPosition", 15.5f, new Vector3(0, 0, 1.6f)); k.Key(singed, "Body", "m_LocalPosition", MedievalArrivalLength, new Vector3(0, 0, 1.6f));
            k.Clips(singed, (PeopleClip("limp") ?? PeopleClip("walk"), 12.6f), (PeopleClip("dazed") ?? PeopleClip("idle"), 15.5f));
            k.Show(singed, 12.6f, MedievalArrivalLength);
            for (int i = 0; i < 4; i++)   // his hat smokes
            {
                var sm = k.Holder("Hat smoke " + i, new Vector3(-23f, 1.9f, 9.5f), 0);
                Placeholders.Prim(PrimitiveType.Sphere, sm.transform, Vector3.zero, Vector3.one * .1f, Placeholders.Mat(new Color(.35f, .33f, .32f)), name: "Puff");
                float t0 = 13f + i * .9f;
                k.Key(sm, "Puff", "m_LocalPosition", t0, Vector3.zero); k.Key(sm, "Puff", "m_LocalPosition", t0 + 1.4f, new Vector3(.2f, 1.2f, .5f));
                k.Key(sm, "Puff", "m_LocalScale", t0, Vector3.one * .08f); k.Key(sm, "Puff", "m_LocalScale", t0 + .6f, Vector3.one * .3f); k.Key(sm, "Puff", "m_LocalScale", t0 + 1.4f, Vector3.one * .02f);
                k.Show(sm, t0, Mathf.Min(MedievalArrivalLength, t0 + 1.4f));
            }
            // the wreck appears with the blast (the world keeps its own after)
            var wreck = k.Holder("Wreck", new Vector3(-25.5f, 0, 10f), 80);
            if (!MedProp("WagonWreck", wreck.transform, Vector3.zero, 0)) Placeholders.Prim(PrimitiveType.Cube, wreck.transform, new Vector3(0, .4f, 0), new Vector3(2.4f, .8f, 4f), WorldArt.Mat("Dark"), name: "Wreck");
            Flames(k, wreck, "", new Vector3(0, .5f, 0), 12.1f, 12.4f, .7f, 14.5f);
            k.Show(wreck, 12.05f, MedievalArrivalLength);
            Blast(k, new Vector3(-26f, 1.6f, 10f), 12.0f, 4.5f);
            Tint(k, (0f, 0f), (11.95f, 0f), (12.05f, .55f), (12.5f, 0f), (MedievalArrivalLength, 0f));
            // the purple vortex over the courtyard; the party drops out of it into the hay
            VortexRings(k, "Purple vortex", hay + new Vector3(0, 12f, 0), Quaternion.Euler(90, 0, 0), 0f, 1.2f, 5.5f, 3.4f, colors: PurpleRings);
            var inHay = new[] { (dan, new Vector3(-.5f, -.15f, .3f)), (abi, new Vector3(.6f, -.15f, -.3f)), (kira, new Vector3(.15f, .1f, .5f)) };
            for (int j = 0; j < inHay.Length; j++)
            {
                var (actor, off) = inHay[j]; var home = actor.transform.localPosition; var spot = hay + off - home;
                float d = j * .12f;
                k.Key(actor, "Body", "m_LocalPosition", 0f, spot + new Vector3(0, 14f, 0)); k.Key(actor, "Body", "m_LocalPosition", 1.5f + d, spot + new Vector3(0, 11f, 0));
                k.Key(actor, "Body", "m_LocalPosition", 2.9f + d, spot + new Vector3(0, -.7f, 0));   // into the hay, out of sight
                k.Key(actor, "Body", "m_LocalPosition", 3.6f + d * 2, spot + new Vector3(0, -.7f, 0)); k.Key(actor, "Body", "m_LocalPosition", 4.0f + d * 2, spot);   // pop up
                k.Key(actor, "Body", "m_LocalPosition", 7.6f + d, spot); k.Key(actor, "Body", "m_LocalPosition", 8.0f + d, Vector3.Lerp(spot, Vector3.zero, .5f) + Vector3.up * 1.2f);
                k.Key(actor, "Body", "m_LocalPosition", 8.4f + d, Vector3.zero); k.Key(actor, "Body", "m_LocalPosition", 14.4f, Vector3.zero);
                k.Key(actor, "Body", "m_LocalPosition", MedievalArrivalLength, new Vector3(0, 0, -2.4f));   // and they slink off
            }
            HayPuff(k, hay + new Vector3(0, .3f, 0), 2.95f, 2.2f, new Color(.95f, .8f, .38f));
            HayPuff(k, hay + new Vector3(0, .4f, 0), 3.7f, 1.4f, new Color(.95f, .82f, .4f));
            // Kira's sneeze: a little puff of fire into the hay (6.2 s)
            var sneeze = k.Holder("Sneeze", hay + new Vector3(.15f, .5f, .2f), 0);
            for (int i = 0; i < 4; i++)
            {
                Placeholders.Prim(PrimitiveType.Sphere, sneeze.transform, Vector3.zero, Vector3.one * .05f, Placeholders.Mat(i % 2 == 0 ? new Color(1f, .55f, .15f) : new Color(1f, .9f, .4f), "Glow"), name: "Bit" + i);
                var dir = new Vector3((i - 1.5f) * .12f, -.15f - i * .05f, -.5f - i * .12f);
                k.Key(sneeze, "Bit" + i, "m_LocalPosition", 6.2f, Vector3.zero); k.Key(sneeze, "Bit" + i, "m_LocalPosition", 6.6f, dir);
                k.Key(sneeze, "Bit" + i, "m_LocalScale", 6.2f, Vector3.one * .05f); k.Key(sneeze, "Bit" + i, "m_LocalScale", 6.35f, Vector3.one * .25f); k.Key(sneeze, "Bit" + i, "m_LocalScale", 6.8f, Vector3.one * .03f);
            }
            k.Show(sneeze, 6.2f, 6.8f);
            AnimationClip Surprise() => PeopleClip("surprised") ?? PeopleClip("look");
            k.Clips(dan, (PeopleClip("fall"), 0f), (PeopleClip("idle"), 3.0f), (PeopleClip("look"), 4.2f), (Surprise(), 6.6f), (PeopleClip("jump"), 7.6f), (PeopleClip("land"), 8.3f), (Surprise(), 12.0f), (PeopleClip("idle"), 13.6f), (PeopleClip("walk"), 14.4f));
            k.Clips(abi, (PeopleClip("fall"), 0f), (PeopleClip("idle"), 3.0f), (PeopleClip("laugh") ?? PeopleClip("idle"), 4.6f), (Surprise(), 6.5f), (PeopleClip("jump"), 7.7f), (PeopleClip("land"), 8.4f), (Surprise(), 12.0f), (PeopleClip("idle"), 13.4f), (PeopleClip("walk"), 14.4f));
            k.Clips(kira, (KiraClip("fall", c.kira), 0f), (KiraClip("idle", c.kira), 3.2f), (KiraClip("attack", c.kira), 6.1f), (KiraClip("idle", c.kira), 6.8f), (KiraClip("jump", c.kira), 7.8f), (KiraClip("land", c.kira), 8.5f), (KiraClip("idle", c.kira), 9.0f), (KiraClip("walk", c.kira), 14.4f));
            // they look round at each other in the hay, then at Kira; at the blast they spin to look; then slink off south, glancing back
            k.KeyYaw(dan, "Body", 0f, 0); k.KeyYaw(dan, "Body", 4.4f, 40f); k.KeyYaw(dan, "Body", 5.6f, -30f); k.KeyYaw(dan, "Body", 11.9f, -30f); k.KeyYaw(dan, "Body", 12.3f, 80f); k.KeyYaw(dan, "Body", 14.3f, 80f); k.KeyYaw(dan, "Body", 14.8f, 20f);
            k.KeyYaw(abi, "Body", 0f, 0); k.KeyYaw(abi, "Body", 4.4f, -40f); k.KeyYaw(abi, "Body", 11.9f, -40f); k.KeyYaw(abi, "Body", 12.3f, -110f); k.KeyYaw(abi, "Body", 14.3f, -110f); k.KeyYaw(abi, "Body", 14.8f, -20f);
            k.KeyYaw(kira, "Body", 0f, 0); k.KeyYaw(kira, "Body", 9.0f, 0); k.KeyYaw(kira, "Body", 11.9f, 0); k.KeyYaw(kira, "Body", 12.3f, -90f); k.KeyYaw(kira, "Body", 14.3f, -90f); k.KeyYaw(kira, "Body", 14.8f, 0f);
            if (k.Necklaces.TryGetValue(dan, out var neck))
            {
                float[] ts = { 0f, 15.6f, 16.0f, 16.4f, 16.8f, MedievalArrivalLength }; float[] ss = { 1f, 1f, 2.2f, 1.5f, 2.0f, 1.7f };
                for (int i = 0; i < ts.Length; i++) k.Key(neck.gameObject, "Shard", "m_LocalScale", ts[i], Vector3.one * ss[i]);
            }
            k.Shot("The vortex opens", 0f, 1.8f, new Vector3(9f, 1.4f, -2f), hay + new Vector3(0, 8f, 0), 52f, new Vector3(8f, 1.6f, 0f));
            k.Shot("Into the hay", 1.8f, 3.4f, new Vector3(6.5f, 4.2f, 4.5f), hay + new Vector3(0, .5f, 0), 46f);
            k.Shot("They pop up", 3.4f, 5.8f, new Vector3(4.6f, 2.3f, 7.0f), hay + new Vector3(0, .5f, 0), 42f, new Vector3(4.3f, 2.2f, 7.4f));
            k.Shot("Achoo!", 5.8f, 7.3f, hay + new Vector3(1.4f, .9f, -1.2f), hay + new Vector3(.15f, .55f, .4f), 38f);
            k.Shot("Fire! Jump!", 7.3f, 9.2f, new Vector3(9f, 3.2f, 3f), hay + new Vector3(-.5f, .4f, -1f), 50f);
            k.Shot("Runaway wagon", 9.2f, 11.9f, new Vector3(-6f, 2.4f, 2f), new Vector3(-6f, 1.4f, 10f), 52f, new Vector3(-14f, 2.6f, 3.5f));
            k.Shot("Kaboom", 11.9f, 14.2f, new Vector3(-14f, 3.4f, 1.5f), new Vector3(-25f, 2f, 10f), 50f);
            k.Shot("Slink away", 14.2f, MedievalArrivalLength, new Vector3(3.4f, 1.7f, 1.6f), new Vector3(2.6f, 1.1f, 6.2f), 46f, new Vector3(3.2f, 1.6f, 2.4f));
            k.Sound("hay_rustle", 2.95f, .7f); k.Sound("fire_puff", 6.2f, .7f); k.Sound("hay_fire", 6.5f, .6f); k.Sound("horse_neigh", 8.7f, .6f);
            k.Sound("wagon_blast", 12.0f, .85f);
            k.Save();
            NecklaceModel.Tint = IceAgeCast.shard;
        }

        // Item 10 (the film's scene, muscleman-falls-at-stone.png): the necklace has led them to the stone in the courtyard. Muscle Man is there
        // first: he brags (flexes, laughs at Dan), grips the shard and heaves with everything he has, fails and falls flat on his back. Dan steps
        // up; the shard answers his necklace and slides out with no effort. Muscle Man stares, then kneels: "Master!" The purple vortex opens
        // and sweeps all four in (Dan bewildered). The root is the stone (world (0, floor, 34)), +z north (the keep): the party south of it.
        public const float TheStoneLength = 19f;
        public static void TheStone()
        {
            DataAssets.Ensure();
            var c = MedievalCast; NecklaceModel.Tint = c.shard;
            var k = new Kit("TheStone", TheStoneLength);
            var dan = k.Actor("Dan", c.dan, new Vector3(-.7f, 0, -3.6f), 0);
            var abi = k.Actor("Abi", c.abi, new Vector3(-2.0f, 0, -4.0f), 10);
            var kira = k.Actor("Kira", c.kira, new Vector3(.7f, 0, -3.3f), -10);
            var mm = k.Actor("Muscle Man", Have("muscleman_medieval", ""), new Vector3(1.6f, 0, -1.4f), -130);
            if (!k.Models.ContainsKey(mm)) { var b = mm.transform.Find("Body") ?? mm.transform; Placeholders.Prim(PrimitiveType.Capsule, mm.transform, new Vector3(0, 1f, 0), new Vector3(1f, 1f, .7f), Placeholders.Mat(new Color(.2f, .25f, .4f)), name: "Body"); _ = b; }
            // the shard in the stone (the world's own is hidden while this plays)
            var shard = k.Holder("Purple shard", new Vector3(0, 1.1f, 0), 15);
            Placeholders.CrystalObject("Crystal", c.shard, .9f).transform.SetParent(shard.transform, false);
            k.Key(shard, "Crystal", "m_LocalPosition", 0f, new Vector3(0, .45f, 0)); k.Key(shard, "Crystal", "m_LocalPosition", 11.2f, new Vector3(0, .45f, 0));
            k.Key(shard, "Crystal", "m_LocalPosition", 12.6f, new Vector3(0, 1.3f, -.15f)); k.Key(shard, "Crystal", "m_LocalPosition", 13.4f, new Vector3(-.15f, 1.45f, -.6f));
            k.Key(shard, "Crystal", "m_LocalPosition", 15.6f, new Vector3(-.15f, 1.45f, -.6f));
            k.Show(shard, 0f, 15.8f);
            var aura = k.Holder("Shard aura", new Vector3(0, 1.55f, 0), 0);
            Placeholders.Prim(PrimitiveType.Sphere, aura.transform, Vector3.zero, Vector3.one * .1f, Placeholders.Mat(Glow(c, .5f), "Glow"), name: "Glow");
            float[] at = { 9.6f, 10.2f, 10.8f, 11.4f, 12.0f }; float[] asz = { .05f, .9f, .6f, 1.3f, .05f };
            for (int i = 0; i < at.Length; i++) k.Key(aura, "Glow", "m_LocalScale", at[i], Vector3.one * asz[i]);
            k.Show(aura, 9.6f, 12.0f);
            // Muscle Man: brags at the party, turns to the stone, heaves, falls flat; lies there; then (cut) kneels to Dan
            k.Key(mm, "Body", "m_LocalPosition", 0f, Vector3.zero); k.Key(mm, "Body", "m_LocalPosition", 2.4f, Vector3.zero);
            var grip = Quaternion.Inverse(Quaternion.Euler(0, -130, 0)) * (new Vector3(0, 0, -.95f) - new Vector3(1.6f, 0, -1.4f));
            k.Key(mm, "Body", "m_LocalPosition", 3.0f, grip); k.Key(mm, "Body", "m_LocalPosition", 6.6f, grip);
            k.Key(mm, "Body", "m_LocalPosition", 7.6f, grip + Quaternion.Inverse(Quaternion.Euler(0, -130, 0)) * new Vector3(0, 0, -1.3f));
            k.Key(mm, "Body", "m_LocalPosition", 13.6f, grip + Quaternion.Inverse(Quaternion.Euler(0, -130, 0)) * new Vector3(0, 0, -1.3f));
            var kneelAt = Quaternion.Inverse(Quaternion.Euler(0, -130, 0)) * (new Vector3(1.1f, 0, -2.0f) - new Vector3(1.6f, 0, -1.4f));
            k.Key(mm, "Body", "m_LocalPosition", 13.7f, kneelAt); k.Key(mm, "Body", "m_LocalPosition", 16.0f, kneelAt);
            k.KeyYaw(mm, "Body", 0f, 0); k.KeyYaw(mm, "Body", 2.4f, 0); k.KeyYaw(mm, "Body", 3.0f, 130f); k.KeyYaw(mm, "Body", 13.6f, 130f); k.KeyYaw(mm, "Body", 13.7f, 30f); k.KeyYaw(mm, "Body", 16f, 30f);
            AnimationClip MM(string n, string f) => PeopleClip(n) ?? PeopleClip(f);
            k.Clips(mm, (MM("mm_flex", "cheer"), 0f), (MM("mm_taunt", "laugh"), 1.6f), (PeopleClip("walk"), 2.4f), (MM("mm_heave", "push"), 3.0f), (MM("mm_fall", "fall_back"), 6.6f),
                (MM("surprised", "idle"), 12.4f), (MM("mm_kneel", "idle"), 13.7f), (PeopleClip("fall"), 16.2f));
            // Dan steps up (8.6 -> 10.4), the shard answers his necklace, it slides out, he holds it up; then bewildered
            var danUp = new Vector3(.7f, 0, 2.75f);   // to the stone's south side (local to his holder)
            k.Key(dan, "Body", "m_LocalPosition", 0f, Vector3.zero); k.Key(dan, "Body", "m_LocalPosition", 8.6f, Vector3.zero); k.Key(dan, "Body", "m_LocalPosition", 10.4f, danUp); k.Key(dan, "Body", "m_LocalPosition", 16.0f, danUp);
            k.Clips(dan, (PeopleClip("idle"), 0f), (PeopleClip("look"), 2.0f), (PeopleClip("laugh") ?? PeopleClip("idle"), 7.4f), (PeopleClip("walk"), 8.6f), (PeopleClip("idle"), 10.4f),
                (PeopleClip("hold_up") ?? PeopleClip("cheer"), 11.6f), (PeopleClip("surprised") ?? PeopleClip("look"), 15.0f), (PeopleClip("fall"), 16.2f));
            k.Clips(abi, (PeopleClip("idle"), 0f), (PeopleClip("look"), 3.4f), (PeopleClip("laugh") ?? PeopleClip("idle"), 7.2f), (PeopleClip("idle"), 9.0f), (PeopleClip("cheer"), 13.0f), (PeopleClip("surprised") ?? PeopleClip("look"), 15.0f), (PeopleClip("fall"), 16.2f));
            k.Clips(kira, (KiraClip("idle", c.kira), 0f), (KiraClip("jump", c.kira), 7.0f), (KiraClip("idle", c.kira), 7.7f), (KiraClip("jump", c.kira), 13.1f), (KiraClip("idle", c.kira), 13.8f), (KiraClip("fall", c.kira), 16.2f));
            if (k.Necklaces.TryGetValue(dan, out var neck))
            {
                float[] ts = { 0f, 9.4f, 9.8f, 10.4f, 11.0f, 11.6f, 13f, TheStoneLength }; float[] ss = { 1.5f, 1.5f, 2.4f, 1.8f, 2.6f, 2f, 1.6f, 1.6f };
                for (int i = 0; i < ts.Length; i++) k.Key(neck.gameObject, "Shard", "m_LocalScale", ts[i], Vector3.one * ss[i]);
            }
            // the purple vortex opens over the stone and pulls all four in
            var vort = new Vector3(0, 3.2f, 1.2f);
            VortexRings(k, "Purple vortex", vort, Quaternion.Euler(0, 180, 0), 15.6f, 16.4f, 4.4f, TheStoneLength, colors: PurpleRings);
            foreach (var (a, h) in new[] { (dan, 1.1f), (abi, 1.0f), (kira, .3f), (mm, 1.2f) })
            {
                var home = a.transform.localPosition; var inv = Quaternion.Inverse(a.transform.localRotation);
                var to = inv * (vort - home - new Vector3(0, h, 0));
                k.Key(a, "Body", "m_LocalScale", 16.4f, Vector3.one); k.Key(a, "Body", "m_LocalScale", 17.6f, Vector3.one * .05f); k.Key(a, "Body", "m_LocalScale", TheStoneLength, Vector3.one * .05f);
                k.Key(a, "Body", "m_LocalPosition", 17.6f, to); k.Key(a, "Body", "m_LocalPosition", TheStoneLength, to);
            }
            Tint(k, (0f, 0f), (17.6f, 0f), (18.4f, 1f), (TheStoneLength, 1f));
            k.Shot("He's here first", 0f, 2.4f, new Vector3(-1.6f, 1.6f, -6.4f), new Vector3(1.4f, 1.4f, -1.2f), 44f, new Vector3(-1.2f, 1.6f, -5.8f));
            k.Shot("The heave", 2.4f, 6.6f, new Vector3(3.2f, 1.6f, 1.4f), new Vector3(0, 1.1f, -.6f), 44f, new Vector3(2.8f, 1.4f, .9f));
            k.Shot("Flat on his back", 6.6f, 8.6f, new Vector3(.4f, 3.6f, -4.0f), new Vector3(.6f, .3f, -1.8f), 46f);
            k.Shot("Dan steps up", 8.6f, 11.2f, new Vector3(-2.6f, 1.5f, 1.6f), new Vector3(-.2f, 1.2f, -.6f), 44f);
            k.Shot("It slides out", 11.2f, 13.6f, new Vector3(-.2f, 1.6f, 1.6f), new Vector3(0, 1.7f, -.3f), 38f, new Vector3(-.15f, 1.75f, 1.3f));
            k.Shot("Master!", 13.6f, 15.6f, new Vector3(-1.6f, 1.5f, -3.4f), new Vector3(.5f, 1.0f, -1.6f), 42f);
            k.Shot("The vortex", 15.6f, TheStoneLength, new Vector3(.4f, 2.4f, -7.4f), new Vector3(0, 2.2f, 0), 54f, new Vector3(.3f, 2.6f, -6.4f));
            k.Sound("mm_laugh", 1.6f, .7f); k.Sound("mm_grunt", 3.2f, .7f); k.Sound("mm_grunt", 4.6f, .8f); k.Sound("mm_grunt", 5.8f, .9f); k.Sound("boulder_roll", 6.9f, .4f);
            k.Sound("necklace", 9.6f, .6f); k.Sound("shard_slide", 11.2f, .8f); k.Sound("mm_master", 14.3f, .9f); k.Sound("portal", 15.6f, .6f);
            k.Save();
            NecklaceModel.Tint = IceAgeCast.shard;
        }

        // The story shard opens the portal (the test area). The root is the portal (its +z faces the heroes).
        public static void PortalOpens()
        {
            DataAssets.Ensure();
            var k = new Kit("PortalOpens", 6f);
            var dan = k.Actor("Dan", "dan_home", new Vector3(0, 0, 7.2f), 180);
            var abi = k.Actor("Abi", "abi_home", new Vector3(-1.4f, 0, 8f), 170);
            var kira = k.Actor("Kira", "kira", new Vector3(1.3f, 0, 7.8f), 195);
            // the story shard flies from Dan into the ring
            var shard = k.Holder("Story shard", Vector3.zero, 0);
            var crystal = Placeholders.CrystalObject("Crystal", new Color(.3f, .62f, 1f), 1.2f); crystal.transform.SetParent(shard.transform, false);
            k.Key(shard, "Crystal", "m_LocalPosition", 0f, new Vector3(0, 1.5f, 6.5f));
            k.Key(shard, "Crystal", "m_LocalPosition", .6f, new Vector3(0, 2.1f, 6.2f));
            k.Key(shard, "Crystal", "m_LocalPosition", 2.0f, new Vector3(0, 2.2f, 0));
            k.Key(shard, "Crystal", "m_LocalScale", 0f, Vector3.one * 1.2f); k.Key(shard, "Crystal", "m_LocalScale", 1.8f, Vector3.one * 1.4f); k.Key(shard, "Crystal", "m_LocalScale", 2.1f, Vector3.one * .1f);
            k.Show(shard, 0f, 2.1f);
            // the disc grows and turns
            var disc = k.Holder("Portal disc", new Vector3(0, 2.2f, 0), 0);
            var d = Placeholders.Prim(PrimitiveType.Cylinder, disc.transform, Vector3.zero, new Vector3(.01f, .02f, .01f), Placeholders.Mat(new Color(.55f, .4f, 1f), "Glow"), name: "Disc");
            d.transform.localRotation = Quaternion.Euler(90, 0, 0);
            k.Key(disc, "Disc", "m_LocalScale", 2.0f, new Vector3(.01f, .02f, .01f));
            k.Key(disc, "Disc", "m_LocalScale", 2.9f, new Vector3(4.0f, .02f, 4.0f));
            k.Key(disc, "Disc", "m_LocalScale", 3.3f, new Vector3(3.6f, .02f, 3.6f));
            k.Key(disc, "Disc", "m_LocalScale", 6f, new Vector3(3.6f, .02f, 3.6f));
            k.Show(disc, 2.0f, 6f);
            var flash = k.Holder("Flash", new Vector3(0, 2.2f, .1f), 0);
            Placeholders.Prim(PrimitiveType.Sphere, flash.transform, Vector3.zero, Vector3.one, Placeholders.Mat(new Color(.9f, .85f, 1f), "Glow"), name: "Glow");
            k.Key(flash, "Glow", "m_LocalScale", 2.0f, Vector3.one * .2f); k.Key(flash, "Glow", "m_LocalScale", 2.4f, Vector3.one * 2.8f); k.Key(flash, "Glow", "m_LocalScale", 2.9f, Vector3.one * .1f);
            k.Show(flash, 2.0f, 2.9f);
            // the heroes cheer: Dan and Abi their cheer clip; Kira (and graybox actors) hop
            k.Clips(dan, (PeopleClip("idle"), 0f), (PeopleClip("cheer"), 3.9f));
            k.Clips(abi, (PeopleClip("idle"), 0f), (PeopleClip("cheer"), 3.7f));
            k.Clips(kira, (KiraClip("idle"), 0f), (KiraClip("jump"), 3.55f), (KiraClip("jump"), 4.25f), (KiraClip("idle"), 4.9f));
            foreach (var (actor, t0) in new[] { (kira, 3.6f), (abi, 3.8f), (dan, 4.0f) })
            {
                if (actor != kira && k.Models.ContainsKey(actor)) continue;
                k.Key(actor, "Body", "m_LocalPosition", t0, Vector3.zero); k.Key(actor, "Body", "m_LocalPosition", t0 + .25f, new Vector3(0, .5f, 0)); k.Key(actor, "Body", "m_LocalPosition", t0 + .5f, Vector3.zero);
                k.Key(actor, "Body", "m_LocalPosition", t0 + .7f, Vector3.zero); k.Key(actor, "Body", "m_LocalPosition", t0 + .95f, new Vector3(0, .35f, 0)); k.Key(actor, "Body", "m_LocalPosition", t0 + 1.2f, Vector3.zero);
            }
            // shots
            k.Shot("The shard flies", 0f, 2.0f, new Vector3(4.8f, 1.8f, 7f), new Vector3(0, 2f, 3.2f), 50f, new Vector3(4.5f, 2.2f, 5f));
            k.Shot("The portal opens", 2.0f, 4.0f, new Vector3(.6f, 2.0f, 12f), new Vector3(0, 2.2f, 0), 45f, new Vector3(.4f, 2.1f, 10.5f));
            k.Shot("The heroes cheer", 4.0f, 6f, new Vector3(2.8f, 2.4f, 4.4f), new Vector3(0, 1f, 7.6f), 50f);
            k.Save();
        }
    }
}
