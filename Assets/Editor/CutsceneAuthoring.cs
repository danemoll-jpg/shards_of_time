using System.Collections.Generic;
using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace ShardsOfTime
{
    // Round 1, item 11: builds the two graybox cutscenes as real Timeline + Cinemachine assets (Docs/CUTSCENES.md):
    //   Assets/Cutscenes/<Name>.playable              the Timeline (actor animation tracks, a Cinemachine shot track, activation tracks)
    //   Assets/Resources/Cutscenes/<Name>.prefab      the scene: PlayableDirector + actors + shot cameras + effects, bound to the Timeline
    // Made once if missing (PlayerBuild); after that they are hand-edited in Unity's Timeline window. "Rebuild Graybox Cutscenes"
    // overwrites them (asks first). Everything is placed relative to the prefab's root, which the game puts at the cutscene's anchor.
    public static class CutsceneAuthoring
    {
        public const string TimelineDir = "Assets/Cutscenes", PrefabDir = "Assets/Resources/Cutscenes";

        public static void EnsureAll()
        {
            if (!File.Exists($"{PrefabDir}/NecklaceWakes.prefab")) NecklaceWakes();
            if (!File.Exists($"{PrefabDir}/PortalOpens.prefab")) PortalOpens();
        }

        [MenuItem("Shards of Time/Cutscenes/Rebuild Graybox Cutscenes")]
        static void RebuildMenu()
        {
            if (!EditorUtility.DisplayDialog("Rebuild graybox cutscenes", "This overwrites NecklaceWakes and PortalOpens (Timeline + prefab) with the code-made graybox versions. Hand edits are lost.", "Rebuild", "Cancel")) return;
            NecklaceWakes(); PortalOpens();
        }

        // ---- a small authoring kit: an actor / camera / prop is a holder (Animator) with an animated child, so the clips animate
        // child paths only (no root-motion offsets) and the holder stays where it was placed.
        sealed class Kit
        {
            public GameObject root; public PlayableDirector dir; public TimelineAsset tl; public CinemachineTrack shots; public string name;
            readonly Dictionary<GameObject, AnimationClip> clips = new Dictionary<GameObject, AnimationClip>();
            readonly Dictionary<GameObject, AnimationTrack> tracks = new Dictionary<GameObject, AnimationTrack>();
            public float duration;

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

            public GameObject Actor(string hero, Vector3 pos, float yaw)
            {
                var h = Holder(hero, pos, yaw);
                Placeholders.HeroBody(CharacterStats.Defaults(hero), h.transform);
                return h;
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
                Debug.Log($"[Cutscene] built {name}: {path} + {TimelineDir}/{name}.playable ({duration} s)");
            }
        }

        // The 10th shard of Area A: Dan's necklace wakes. The root is where the player stands, facing the story shard.
        public static void NecklaceWakes()
        {
            DataAssets.Ensure();
            var k = new Kit("NecklaceWakes", 6.5f);
            var dan = k.Actor("Dan", Vector3.zero, 0);
            var abi = k.Actor("Abi", new Vector3(-1.3f, 0, -1.1f), 30);
            var kira = k.Actor("Kira", new Vector3(1.2f, 0, -.8f), -30);
            // the gem grows and glows; an aura blooms; a beam reaches out toward the story shard
            k.Key(dan, "Body/Necklace", "m_LocalScale", 0f, new Vector3(.09f, .13f, .05f));
            k.Key(dan, "Body/Necklace", "m_LocalScale", 1.2f, new Vector3(.2f, .3f, .1f));
            k.Key(dan, "Body/Necklace", "m_LocalScale", 1.5f, new Vector3(.16f, .24f, .08f));
            k.Key(dan, "Body/Necklace", "m_LocalScale", 6.5f, new Vector3(.16f, .24f, .08f));
            var aura = k.Holder("Aura", new Vector3(0, 1.15f, .35f), 0);
            Placeholders.Prim(PrimitiveType.Sphere, aura.transform, Vector3.zero, Vector3.one * .1f, Placeholders.Mat(new Color(.55f, .9f, 1f), "Glow"), name: "Glow");
            k.Key(aura, "Glow", "m_LocalScale", .6f, Vector3.one * .05f); k.Key(aura, "Glow", "m_LocalScale", 1.6f, Vector3.one * .7f); k.Key(aura, "Glow", "m_LocalScale", 2.2f, Vector3.one * .25f);
            k.Show(aura, .6f, 2.4f);
            var beam = k.Holder("Beam", new Vector3(0, 1.15f, .4f), 0);
            Placeholders.Prim(PrimitiveType.Cube, beam.transform, Vector3.zero, new Vector3(.08f, .08f, .01f), Placeholders.Mat(new Color(.6f, .95f, 1f), "Glow"), name: "Ray");
            k.Key(beam, "Ray", "m_LocalPosition", 2.3f, Vector3.zero); k.Key(beam, "Ray", "m_LocalScale", 2.3f, new Vector3(.08f, .08f, .01f));
            k.Key(beam, "Ray", "m_LocalPosition", 4.2f, new Vector3(0, .4f, 14f)); k.Key(beam, "Ray", "m_LocalScale", 4.2f, new Vector3(.1f, .1f, 28f));
            k.Key(beam, "Ray", "m_LocalPosition", 6.5f, new Vector3(0, .6f, 20f)); k.Key(beam, "Ray", "m_LocalScale", 6.5f, new Vector3(.1f, .1f, 40f));
            k.Show(beam, 2.3f, 6.5f);
            // the others react: Kira hops, Abi turns to Dan, Dan looks down at the gem then up
            k.Key(kira, "Body", "m_LocalPosition", 2.4f, Vector3.zero); k.Key(kira, "Body", "m_LocalPosition", 2.65f, new Vector3(0, .45f, 0)); k.Key(kira, "Body", "m_LocalPosition", 2.9f, Vector3.zero);
            k.Key(kira, "Body", "m_LocalPosition", 3.1f, Vector3.zero); k.Key(kira, "Body", "m_LocalPosition", 3.35f, new Vector3(0, .35f, 0)); k.Key(kira, "Body", "m_LocalPosition", 3.6f, Vector3.zero);
            k.KeyYaw(abi, "Body", 0f, 0); k.KeyYaw(abi, "Body", 1.4f, 0); k.KeyYaw(abi, "Body", 2.0f, 25f); k.KeyYaw(abi, "Body", 4.0f, 25f); k.KeyYaw(abi, "Body", 4.6f, -30f);
            k.KeyQ(dan, "Body/Head", 0f, Quaternion.identity); k.KeyQ(dan, "Body/Head", .5f, Quaternion.Euler(25, 0, 0)); k.KeyQ(dan, "Body/Head", 2.2f, Quaternion.Euler(25, 0, 0)); k.KeyQ(dan, "Body/Head", 2.7f, Quaternion.identity);
            // shots
            k.Shot("Dan's necklace", 0f, 2.3f, new Vector3(.25f, 1.35f, 1.5f), new Vector3(0, 1.15f, 0), 38f, new Vector3(.15f, 1.25f, 1.05f));
            k.Shot("The heroes", 2.3f, 4.4f, new Vector3(3.8f, 1.7f, 4.2f), new Vector3(-.2f, 1f, -.2f), 48f);
            k.Shot("Follow the light", 4.4f, 6.5f, new Vector3(.3f, 2.1f, -4.8f), new Vector3(0, 1.5f, 12f), 52f, new Vector3(.3f, 2.6f, -2.8f));
            k.Save();
        }

        // The story shard opens the portal. The root is the portal (its +z faces the heroes).
        public static void PortalOpens()
        {
            DataAssets.Ensure();
            var k = new Kit("PortalOpens", 6f);
            var dan = k.Actor("Dan", new Vector3(0, 0, 7.2f), 180);
            var abi = k.Actor("Abi", new Vector3(-1.4f, 0, 8f), 170);
            var kira = k.Actor("Kira", new Vector3(1.3f, 0, 7.8f), 195);
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
            // the heroes cheer
            foreach (var (actor, t0) in new[] { (kira, 3.6f), (abi, 3.8f), (dan, 4.0f) })
            {
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
