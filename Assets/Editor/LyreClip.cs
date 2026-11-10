using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ShardsOfTime
{
    // Fix 6.1, item 2: the Violinist's lyre-playing clip (Resources/Characters/Clips/violinist_lyre.anim), a humanoid clip made here
    // by script: the people's idle for the legs and the root, then his own upper body: a gentle side-to-side sway, the chest turned a
    // little toward the lyre at his left hip, the head tilted down toward the strings, the arms low and forward, the left fingers
    // curled round the frame and the right fingers plucking. World/Violinist.cs then puts his hands on the lyre (two-bone IK).
    // Remade when missing or when Version changes. Menu: Shards of Time > Make Lyre Clip.
    public static class LyreClip
    {
        public const int Version = 1;
        const string Path_ = "Assets/Resources/Characters/Clips/violinist_lyre.anim", Clips = "Assets/Resources/Characters/Clips/";

        // made when missing or older than Version (the version is kept in Assets/Editor/violinist_lyre.version.txt)
        public static void Ensure()
        {
            const string stamp = "Assets/Editor/violinist_lyre.version.txt";
            if (File.Exists(Path_) && File.Exists(stamp) && File.ReadAllText(stamp).Trim() == Version.ToString()) return;
            Make(); File.WriteAllText(stamp, Version.ToString());
        }

        [MenuItem("Shards of Time/Make Lyre Clip")]
        public static void Make()
        {
            var idle = Load("idle"); if (!idle) { Debug.LogWarning("LyreClip: no idle clip"); return; }
            float L = idle.length;   // 8.33 s: 4 sways, 16 plucks
            var clip = new AnimationClip { name = "violinist_lyre", frameRate = 30 };
            foreach (var b in AnimationUtility.GetCurveBindings(idle)) AnimationUtility.SetEditorCurve(clip, b, AnimationUtility.GetEditorCurve(idle, b));
            // value = base + amp * sin(2 pi (cycles t / L + phase)), sampled at 15 per second (loops: whole cycles over L)
            void Set(string muscle, float bas, float amp = 0, int cycles = 4, float phase = 0)
            {
                var keys = new System.Collections.Generic.List<Keyframe>();
                int n = Mathf.CeilToInt(L * 15f);
                for (int i = 0; i <= n; i++) { float t = L * i / n; keys.Add(new Keyframe(t, bas + amp * Mathf.Sin(2 * Mathf.PI * (cycles * t / L + phase)))); }
                var c = new AnimationCurve(keys.ToArray());
                for (int i = 0; i < c.length; i++) c.SmoothTangents(i, 0);
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), muscle), c);
            }
            // the body: a gentle sway side to side, turned a little toward the lyre at his left hip, leaning over it a touch
            Set("Spine Left-Right", -.03f, .07f); Set("Chest Left-Right", 0, .05f, 4, .1f); Set("UpperChest Left-Right", 0, .04f, 4, .15f);
            Set("Spine Twist Left-Right", -.08f, .02f, 4, .25f); Set("Chest Twist Left-Right", -.04f);
            Set("Spine Front-Back", .05f, .02f, 8); Set("Chest Front-Back", .04f); Set("UpperChest Front-Back", .02f);
            // the head tilted down toward the strings, turned toward them, swaying with the tune a beat behind the body
            Set("Neck Nod Down-Up", -.12f); Set("Neck Turn Left-Right", -.12f); Set("Neck Tilt Left-Right", -.08f, .04f, 4, .3f);
            Set("Head Nod Down-Up", -.3f, .05f, 8, .2f); Set("Head Turn Left-Right", -.3f, .05f, 4, .35f); Set("Head Tilt Left-Right", -.22f, .08f, 4, .3f);
            // the arms low and forward, elbows bent (the game's hand IK puts the hands on the lyre); the shoulders relaxed
            Set("Left Shoulder Down-Up", -.6f); Set("Left Arm Down-Up", -.42f); Set("Left Arm Front-Back", .4f); Set("Left Arm Twist In-Out", .1f);
            Set("Left Forearm Stretch", -.1f); Set("Left Forearm Twist In-Out", .3f); Set("Left Hand Down-Up", .1f); Set("Left Hand In-Out", .1f);
            Set("Right Shoulder Down-Up", -1.1f); Set("Right Arm Down-Up", -.35f); Set("Right Arm Front-Back", .55f); Set("Right Arm Twist In-Out", -.1f);
            Set("Right Forearm Stretch", -.05f); Set("Right Forearm Twist In-Out", .2f); Set("Right Hand Down-Up", -.15f, .1f, 16); Set("Right Hand In-Out", .0f);
            // the left fingers curled round the frame; the right fingers plucking (index and middle in turn, 16 plucks a loop)
            foreach (var f in new[] { "Index", "Middle", "Ring", "Little" })
            {
                Set($"LeftHand.{f}.1 Stretched", -.35f); Set($"LeftHand.{f}.2 Stretched", -.6f); Set($"LeftHand.{f}.3 Stretched", -.4f);
                float ph = f == "Middle" ? .5f : 0f; bool plucks = f == "Index" || f == "Middle";
                Set($"RightHand.{f}.1 Stretched", plucks ? .05f : -.45f, plucks ? .3f : 0, 16, ph);
                Set($"RightHand.{f}.2 Stretched", plucks ? -.25f : -.7f, plucks ? .3f : 0, 16, ph);
                Set($"RightHand.{f}.3 Stretched", plucks ? -.1f : -.5f, plucks ? .2f : 0, 16, ph);
            }
            Set("LeftHand.Thumb.1 Stretched", -.2f); Set("LeftHand.Thumb.2 Stretched", -.2f); Set("LeftHand.Thumb.3 Stretched", -.2f);
            Set("RightHand.Thumb.1 Stretched", .1f); Set("RightHand.Thumb.2 Stretched", -.1f, .15f, 16, .25f); Set("RightHand.Thumb.3 Stretched", -.1f);
            var s = AnimationUtility.GetAnimationClipSettings(clip); s.loopTime = true; s.loopBlend = true; s.keepOriginalOrientation = true; s.keepOriginalPositionY = true; s.keepOriginalPositionXZ = true;
            s.loopBlendOrientation = true; s.loopBlendPositionY = true; s.loopBlendPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(clip, s);
            if (File.Exists(Path_)) AssetDatabase.DeleteAsset(Path_);
            AssetDatabase.CreateAsset(clip, Path_); AssetDatabase.SaveAssets();
            Debug.Log($"LyreClip: {Path_} made (version {Version}, {L:0.00} s, human {clip.humanMotion}, {AnimationUtility.GetCurveBindings(clip).Length} curves)");
        }

        static AnimationClip Load(string file) => AssetDatabase.LoadAllAssetsAtPath(Clips + file + ".fbx").OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));

        // the muscles of a clip: their value at a few times (to read the signs and ranges of a pose)
        [MenuItem("Shards of Time/Diagnostics/Dump Violinist Clips")]
        public static void Dump()
        {
            var sb = new StringBuilder();
            foreach (var f in new[] { "idle", "violinist_play" })
            {
                var c = Load(f); if (!c) { sb.AppendLine(f + ": missing"); continue; }
                sb.AppendLine($"== {f}: {c.name} length {c.length:0.00} s, human {c.humanMotion}");
                foreach (var b in AnimationUtility.GetCurveBindings(c))
                {
                    var cv = AnimationUtility.GetEditorCurve(c, b);
                    float mn = cv.keys.Min(k => k.value), mx = cv.keys.Max(k => k.value);
                    sb.AppendLine($"  {b.propertyName} [{b.type.Name}] t0 {cv.Evaluate(0):0.000} mid {cv.Evaluate(c.length / 2):0.000} min {mn:0.000} max {mx:0.000}");
                }
            }
            Directory.CreateDirectory("local/fix61"); File.WriteAllText("local/fix61/clip_dump.txt", sb.ToString());
            Debug.Log("LyreClip: dump written to local/fix61/clip_dump.txt");
        }
    }
}
