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
        public const int Version = 7;
        static readonly (string name, System.Action build)[] All =
        {
            ("NecklaceWakes", NecklaceWakes), ("PortalOpens", PortalOpens), ("ActOne", ActOne), ("IceAgeArrival", IceAgeArrival),
            ("VortexOpens", VortexOpens), ("ViolinistTeaches", ViolinistTeaches),
        };

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
        public static void NecklaceWakes()
        {
            DataAssets.Ensure();
            var k = new Kit("NecklaceWakes", 6.5f);
            var dan = k.Actor("Dan", "dan_iceage", Vector3.zero, 0);   // Round 4: it happens in the Ice Age
            var abi = k.Actor("Abi", "abi_iceage", new Vector3(-1.3f, 0, -1.1f), 30);
            var kira = k.Actor("Kira", "kira_sabertooth", new Vector3(1.2f, 0, -.8f), -30);
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
            Placeholders.Prim(PrimitiveType.Sphere, aura.transform, Vector3.zero, Vector3.one * .1f, Placeholders.Mat(new Color(.55f, .9f, 1f), "Glow"), name: "Glow");
            k.Key(aura, "Glow", "m_LocalScale", .6f, Vector3.one * .03f); k.Key(aura, "Glow", "m_LocalScale", 1.6f, Vector3.one * big); k.Key(aura, "Glow", "m_LocalScale", 2.2f, Vector3.one * big * .35f);
            k.Show(aura, .6f, 2.4f);
            var beam = k.Holder("Beam", gemAt + new Vector3(0, 0, .05f), 0);
            Placeholders.Prim(PrimitiveType.Cube, beam.transform, Vector3.zero, new Vector3(.08f, .08f, .01f), Placeholders.Mat(new Color(.6f, .95f, 1f), "Glow"), name: "Ray");
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
            k.Clips(kira, (KiraClip("idle", "kira_sabertooth"), 0f), (KiraClip("jump", "kira_sabertooth"), 2.35f), (KiraClip("idle", "kira_sabertooth"), 3.7f));
            // shots
            k.Shot("Dan's necklace", 0f, 2.3f, new Vector3(.25f, 1.45f, 1.5f), new Vector3(0, 1.3f, 0), 38f, new Vector3(.15f, 1.38f, 1.05f));
            k.Shot("The heroes", 2.3f, 4.4f, new Vector3(3.8f, 1.7f, 4.2f), new Vector3(-.2f, 1f, -.2f), 48f);
            k.Shot("Follow the light", 4.4f, 6.5f, new Vector3(.3f, 2.1f, -4.8f), new Vector3(0, 1.5f, 12f), 52f, new Vector3(.3f, 2.6f, -2.8f));
            k.Save();
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
        static GameObject VortexRings(Kit k, string name, Vector3 at, Quaternion facing, float t0, float t1, float size, float tEnd)
        {
            var h = k.Holder(name, at, 0); h.transform.localRotation = facing;
            Color[] cols = { new Color(.25f, .45f, 1f), new Color(.55f, .4f, 1f), new Color(.75f, .92f, 1f), new Color(.3f, .7f, 1f) };
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
        public const float ArrivalLength = 15.5f;
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
            k.Shot("Abi laughs", 13.0f, ArrivalLength, new Vector3(2.4f, 1.7f, 2.4f), new Vector3(-.6f, .7f, -.2f), 48f);
            k.Save();
        }

        // The story shard opens the vortex (Ice Age). The root is the gate (its +z faces the heroes).
        public static void VortexOpens()
        {
            DataAssets.Ensure();
            var k = new Kit("VortexOpens", 6.5f);
            var dan = k.Actor("Dan", "dan_iceage", new Vector3(0, 0, 7.2f), 180);
            var abi = k.Actor("Abi", "abi_iceage", new Vector3(-1.4f, 0, 8f), 170);
            var kira = k.Actor("Kira", "kira_sabertooth", new Vector3(1.3f, 0, 7.8f), 195);
            var shard = k.Holder("Story shard", Vector3.zero, 0);
            var crystal = Placeholders.CrystalObject("Crystal", new Color(.25f, .55f, 1f), 1.2f); crystal.transform.SetParent(shard.transform, false);
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
            k.Clips(kira, (KiraClip("idle", "kira_sabertooth"), 0f), (KiraClip("jump", "kira_sabertooth"), 3.55f), (KiraClip("jump", "kira_sabertooth"), 4.25f), (KiraClip("idle", "kira_sabertooth"), 4.9f));
            k.Key(kira, "Body", "m_LocalPosition", 3.6f, Vector3.zero); k.Key(kira, "Body", "m_LocalPosition", 3.85f, new Vector3(0, .5f, 0)); k.Key(kira, "Body", "m_LocalPosition", 4.1f, Vector3.zero);
            k.Key(kira, "Body", "m_LocalPosition", 4.3f, Vector3.zero); k.Key(kira, "Body", "m_LocalPosition", 4.55f, new Vector3(0, .35f, 0)); k.Key(kira, "Body", "m_LocalPosition", 4.8f, Vector3.zero);
            k.Shot("The shard flies", 0f, 2.0f, new Vector3(4.8f, 1.8f, 7f), new Vector3(0, 2f, 3.2f), 50f, new Vector3(4.5f, 2.2f, 5f));
            k.Shot("The vortex opens", 2.0f, 4.0f, new Vector3(.6f, 2.0f, 12f), new Vector3(0, 2.2f, 0), 45f, new Vector3(.4f, 2.1f, 10.5f));
            k.Shot("The heroes cheer", 4.0f, 6.5f, new Vector3(2.8f, 2.4f, 4.4f), new Vector3(0, 1f, 7.6f), 50f);
            k.Save();
        }

        // The Violinist plays and Dan learns a move. The root is the Violinist (the world's own figure plays: World/Violinist.cs), +z
        // toward Dan. No dialogue.
        public static void ViolinistTeaches()
        {
            DataAssets.Ensure();
            var k = new Kit("ViolinistTeaches", 6.5f);
            var dan = k.Actor("Dan", "dan_iceage", new Vector3(0, 0, 2.3f), 180);
            k.Clips(dan, (PeopleClip("idle"), 0f), (PeopleClip("look"), .6f), (PeopleClip("idle"), 2.4f), (PeopleClip("cheer"), 4.6f));
            var aura = k.Holder("Aura", new Vector3(0, 1.15f, 2.3f), 0);
            Placeholders.Prim(PrimitiveType.Sphere, aura.transform, Vector3.zero, Vector3.one * .1f, Placeholders.Mat(new Color(.6f, .85f, 1f), "Glow"), name: "Glow");
            float[] ts = { 2.5f, 3.0f, 3.4f, 3.8f, 4.2f, 4.7f }; float[] ss = { .05f, 1.1f, .7f, 1.2f, .8f, .05f };
            for (int i = 0; i < ts.Length; i++) k.Key(aura, "Glow", "m_LocalScale", ts[i], Vector3.one * ss[i]);
            k.Show(aura, 2.5f, 4.7f);
            k.Shot("He plays", 0f, 2.5f, new Vector3(.9f, 1.95f, 3.7f), new Vector3(0, 1.55f, 0), 40f, new Vector3(.7f, 1.9f, 3.2f));
            k.Shot("Dan learns", 2.5f, 4.6f, new Vector3(-.7f, 1.5f, .7f), new Vector3(0, 1.2f, 2.3f), 42f);
            k.Shot("A nod", 4.6f, 6.5f, new Vector3(3.6f, 1.8f, 1.2f), new Vector3(0, 1.1f, 1.2f), 45f);
            k.Save();
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
