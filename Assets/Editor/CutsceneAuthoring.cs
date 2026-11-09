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

        public static void EnsureAll()
        {
            if (Stale("NecklaceWakes")) NecklaceWakes();
            if (Stale("PortalOpens")) PortalOpens();
        }

        // missing, or made with graybox actors while Dan's model now exists
        static bool Stale(string name)
        {
            string path = $"{PrefabDir}/{name}.prefab";
            if (!File.Exists(path)) return true;
            if (!CharacterModel.Exists("dan_home")) return false;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab && !NecklaceModel.Find(prefab.transform, "Model dan_home");
        }

        [MenuItem("Shards of Time/Cutscenes/Rebuild Cutscenes")]
        static void RebuildMenu()
        {
            if (!EditorUtility.DisplayDialog("Rebuild cutscenes", "This overwrites NecklaceWakes and PortalOpens (Timeline + prefab) with the code-made versions. Hand edits are lost.", "Rebuild", "Cancel")) return;
            NecklaceWakes(); PortalOpens();
        }

        // a clip from the people's shared set (Resources/Characters/Clips/<name>.fbx) or from a Kira model's own takes
        static AnimationClip ClipAsset(string file, string clip)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(file))
                if (o is AnimationClip c && !c.name.StartsWith("__preview__") && (c.name == clip || c.name.EndsWith("|" + clip))) return c;
            Debug.LogWarning($"[Cutscene] no clip {clip} in {file}");
            return null;
        }
        public static AnimationClip PeopleClip(string clip) => ClipAsset($"Assets/Resources/Characters/Clips/{clip}.fbx", clip);
        public static AnimationClip KiraClip(string clip) => ClipAsset("Assets/Resources/Characters/kira.fbx", clip);

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
                var cam = camGo.AddComponent<CinemachineCamera>(); cam.Priority = -1; cam.Lens.FieldOfView = fov; cam.Lens.NearClipPlane = .05f; cam.Lens.FarClipPlane = 600f;
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
            var dan = k.Actor("Dan", "dan_home", Vector3.zero, 0);
            var abi = k.Actor("Abi", "abi_home", new Vector3(-1.3f, 0, -1.1f), 30);
            var kira = k.Actor("Kira", "kira", new Vector3(1.2f, 0, -.8f), -30);
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
            k.Clips(kira, (KiraClip("idle"), 0f), (KiraClip("jump"), 2.35f), (KiraClip("idle"), 3.7f));
            // shots
            k.Shot("Dan's necklace", 0f, 2.3f, new Vector3(.25f, 1.45f, 1.5f), new Vector3(0, 1.3f, 0), 38f, new Vector3(.15f, 1.38f, 1.05f));
            k.Shot("The heroes", 2.3f, 4.4f, new Vector3(3.8f, 1.7f, 4.2f), new Vector3(-.2f, 1f, -.2f), 48f);
            k.Shot("Follow the light", 4.4f, 6.5f, new Vector3(.3f, 2.1f, -4.8f), new Vector3(0, 1.5f, 12f), 52f, new Vector3(.3f, 2.6f, -2.8f));
            k.Save();
        }

        // The story shard opens the portal. The root is the portal (its +z faces the heroes).
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
