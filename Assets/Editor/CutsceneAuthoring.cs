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
    public static partial class CutsceneAuthoring
    {
        public const string TimelineDir = "Assets/Cutscenes", PrefabDir = "Assets/Resources/Cutscenes";

        // Round 4: a cutscene made by an older version of this code is rebuilt (CutsceneVersion on its root). Bump Version when a
        // cutscene's code changes. Hand-edited cutscenes: set their CutsceneVersion to 999 to keep them.
        public const int Version = 30;   // Fix 10.1: look-at in every scene with a line or reaction; PirateArrival's fall into the barrels
        //   // Round 9: the eight film scenes redone from Dan's clips (CutsceneAuthoring.Film.cs)
        public const int OldVersion = 19;   // Round 8: the Medieval cutscenes   // Fix 7.1: the Wise Men, Dan and Abi in their Bethlehem models   // Round 7: Bethlehem's cutscenes (11: Round 6's Egypt)
        static readonly (string name, System.Action build)[] All =
        {
            ("NecklaceWakes", NecklaceWakes), ("PortalOpens", PortalOpens), ("ActOne", ActOne), ("IceAgeArrival", IceAgeArrival),
            ("VortexOpens", VortexOpens), ("ViolinistTeaches", ViolinistTeaches),
            ("EgyptArrival", EgyptArrival), ("SphinxGag", SphinxGag), ("NecklaceWakesEgypt", NecklaceWakesEgypt), ("VortexOpensEgypt", VortexOpensEgypt),
            ("ViolinistTeachesEgypt", ViolinistTeachesEgypt),
            ("BethlehemArrival", BethlehemArrival), ("WiseMenArrive", WiseMenArrive), ("NecklaceWakesBethlehem", NecklaceWakesBethlehem),
            ("ViolinistTeachesBethlehem", ViolinistTeachesBethlehem),
            ("MedievalArrival", MedievalArrival), ("TheStone", TheStone), ("NecklaceWakesMedieval", NecklaceWakesMedieval), ("ViolinistTeachesMedieval", ViolinistTeachesMedieval),   // Round 8
            ("PirateArrival", PirateArrival), ("ShardOverboard", ShardOverboard), ("SeaVortex", SeaVortex), ("NecklaceWakesPirate", NecklaceWakesPirate),
            ("ViolinistTeachesPirate", ViolinistTeachesPirate),   // Round 10
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

            // Round 9: while `under` is set, new holders (actors, shots, props) go into that set (Set below) instead of the root
            public Transform under;
            public GameObject Holder(string n, Vector3 pos, float yaw)
            {
                var h = new GameObject(n); h.transform.SetParent(under ? under : root.transform, false); h.transform.localPosition = pos; h.transform.localRotation = Quaternion.Euler(0, yaw, 0);
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

            // Round 9: a set: a group that stands at world (x, ground + up, z), turned yaw, wherever the cutscene's anchor is (CutscenePlace)
            public Transform Set(string n, float x, float z, float yaw, float up = 0f, bool ground = true)
            {
                var g = new GameObject(n); g.transform.SetParent(root.transform, false);
                var p = g.AddComponent<CutscenePlace>(); p.x = x; p.z = z; p.yaw = yaw; p.up = up; p.ground = ground;
                return g.transform;
            }

            // clips on the actor's model, one after another (each runs to the next start + a short overlap that blends them)
            public void Clips(GameObject actor, params (AnimationClip clip, float start)[] list)
            {
                if (!Models.TryGetValue(actor, out var anim)) return;
                // Round 9: the people's Mixamo "fall" carries a downward root motion that a Timeline adds up: over a long fall the figure
                // drops metres below where it is keyed. A long fall plays as restarting 0.6 s pieces (each starts from no offset).
                var split = new List<(AnimationClip clip, float start)>();
                for (int i = 0; i < list.Length; i++)
                {
                    split.Add(list[i]);
                    var c0 = list[i].clip; if (!c0 || c0.name != "fall" || !c0.humanMotion) continue;
                    float end = i + 1 < list.Length ? list[i + 1].start : duration;
                    for (float t = list[i].start + .6f; t < end - .3f; t += .6f) split.Add((c0, t));
                }
                list = split.ToArray();
                var tr = tl.CreateTrack<AnimationTrack>(null, actor.name + " clips");
                tr.trackOffset = TrackOffset.ApplySceneOffsets;
                for (int i = 0; i < list.Length; i++)
                {
                    if (!list[i].clip) continue;
                    float end = i + 1 < list.Length ? list[i + 1].start + .25f : duration;
                    var src = list[i].clip;
                    var c = tr.CreateClip(src); c.start = list[i].start; c.duration = Mathf.Max(.1f, end - list[i].start);
                    ((AnimationPlayableAsset)c.asset).removeStartOffset = true;
                    // Fix 10.1, item 2 (the T-pose sweep): a one-shot shorter than its slot left the rig in its bind pose after its last frame
                    // (sabertooth Kira between her snarls). It now holds its last frame until the next clip takes over.
                    if (!src.isLooping && src.length + .05f < end - list[i].start)
                    {
                        c.duration = src.length;
                        var h = tr.CreateClip(src); h.start = list[i].start + src.length; h.duration = Mathf.Max(.05f, end - (float)h.start);
                        h.clipIn = Mathf.Max(0f, src.length - 1f / 30f); h.timeScale = .0001; h.displayName = src.name + " (held)";
                        ((AnimationPlayableAsset)h.asset).removeStartOffset = true;
                    }
                }
                dir.SetGenericBinding(tr, anim);
            }

            // a shot: a Cinemachine camera at pos looking at `look` (both in the root's space), used from t0 to t1; `dollyTo` moves it
            public CinemachineCamera Shot(string n, float t0, float t1, Vector3 pos, Vector3 look, float fov = 45f, Vector3? dollyTo = null, Vector3? lookTo = null)
            {
                var h = Holder("Shot " + n, Vector3.zero, 0);
                var camGo = new GameObject("Camera"); camGo.transform.SetParent(h.transform, false);
                camGo.transform.localPosition = pos; camGo.transform.localRotation = Quaternion.LookRotation(look - pos);
                var cam = camGo.AddComponent<CinemachineCamera>(); cam.Priority = -1; cam.Lens.FieldOfView = fov; cam.Lens.NearClipPlane = .05f; cam.Lens.FarClipPlane = 2500f;
                if (dollyTo.HasValue || lookTo.HasValue)   // Round 9: lookTo pans to a second target while it moves
                {
                    var end = dollyTo ?? pos;
                    Key(h, "Camera", "m_LocalPosition", t0, pos); Key(h, "Camera", "m_LocalPosition", t1, end);
                    var r0 = camGo.transform.localRotation; var r = Quaternion.LookRotation((lookTo ?? look) - end);
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
            // Fix 10.1: a float on a component (CutsceneLookAt's weight / which); step = held until the next key
            public void KeyF(GameObject holder, string path, System.Type type, string prop, float t, float v, bool step = false)
            {
                var c = ClipFor(holder);
                var b = EditorCurveBinding.FloatCurve(path, type, prop);
                var curve = AnimationUtility.GetEditorCurve(c, b) ?? new AnimationCurve();
                int i = curve.AddKey(new Keyframe(t, v));
                if (i < 0) { for (int j = 0; j < curve.length; j++) if (Mathf.Abs(curve[j].time - t) < 1e-4f) { curve.MoveKey(j, new Keyframe(t, v)); i = j; break; } }
                if (step) for (int j = 0; j < curve.length; j++) { AnimationUtility.SetKeyLeftTangentMode(curve, j, AnimationUtility.TangentMode.Constant); AnimationUtility.SetKeyRightTangentMode(curve, j, AnimationUtility.TangentMode.Constant); }
                AnimationUtility.SetEditorCurve(c, b, curve);
            }

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
