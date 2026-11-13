using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ShardsOfTime
{
    // Plays a rigged model's clips from the motor's state (no Animator Controller asset: a small Playables mixer with crossfades).
    //  - Kira (Round 2, Generic): clips from her own FBX (takes "...|idle", "...|walk", ...).
    //  - People (Round 3, Humanoid): the shared Mixamo clips in Resources/Characters/Clips (Humanoid retargeting plays them on Dan and
    //    Abi in both outfits). Walk / slow run / fast run blend by speed, played at the rate that matches the measured stance-foot speed
    //    of each clip (no foot sliding). Punch / cheer / hit play on the upper body only while the hero moves.
    // Loops: idle, the gait, fall, swim / tread, push. One-shots (to the end, then back to the loop): jump, land, land_hard, the attack
    // (attack / punch / kick), hurt, cheer. Landing and cheering stop as soon as the hero runs off.
    public sealed class CreatureAnimator : MonoBehaviour
    {
        public static readonly string[] KiraClips = { "idle", "walk", "run", "jump", "fall", "land", "swim", "attack", "hurt", "stunned", "slide" };   // Round 10: otter Kira's belly-slide
        public static readonly string[] PeopleClips = { "idle", "walk", "run_slow", "run", "jump", "fall", "land", "land_hard", "swim", "tread", "push", "punch", "kick", "hurt", "cheer",
            // Round 7 (Dan's Mixamo downloads; missing = skipped): the Wise Men (sit on the camel, argue, read the map, hold out a gift, disappointed),
            // the watchman (yell "Halt!", doze), everyone's surprise
            "sit", "argue", "map", "offer", "sad", "yell", "doze", "surprised",
            // Round 8 (Dan's Mixamo downloads): Muscle Man's own (show off, mock, lift, push, heave, fall flat, cheer, boost, kneel), the knight's
            // block, the archer's draw / aim / shoot / nervous look, the wagon driver's daze and limp
            "mm_flex", "mm_taunt", "mm_lift", "mm_push", "mm_heave", "mm_fall", "mm_cheer", "mm_boost", "mm_kneel",
            "block", "draw", "aim", "shoot", "nervous", "dazed", "limp",
            // Round 10 (Dan's Mixamo downloads): Muscle Man's struggling swim, getting up after his crash, his leap overboard
            "mm_struggle", "mm_getup", "mm_leap", "mm_hold", "mm_throw",   // Round 11
            "button" };   // Fix 13.1: Abi at the rover panel
        static readonly HashSet<string> OneShots = new HashSet<string> { "jump", "land", "land_hard", "attack", "punch", "kick", "hurt", "cheer", "yell", "surprised",
            "mm_flex", "mm_taunt", "mm_lift", "mm_fall", "mm_cheer", "mm_boost", "mm_kneel", "draw", "shoot", "button", "offer" };   // Round 8; Fix 13.1 (offer: the hug gag's air hug; before, Trigger skipped it)
        static readonly HashSet<string> UpperBody = new HashSet<string> { "punch", "cheer", "hurt" };
        static readonly HashSet<string> CancelOnMove = new HashSet<string> { "land", "land_hard", "cheer", "mm_lift", "mm_boost", "mm_flex", "mm_taunt", "mm_cheer" };   // Round 8: Muscle Man stops a show-off to follow
        static readonly Dictionary<string, float> Rate = new Dictionary<string, float> { ["punch"] = 1.4f, ["kick"] = 1.5f };
        const float Fade = .12f;
        // Mixamo clips: stance-foot speed in m/s at the clip skeleton's hips height (measured in Blender, Round 3: 1.41 / 2.49 / 4.84),
        // x1.09 from the exe check (planted feet drifted back 5-10% at the Blender numbers)
        public const float ClipHips = .88f;
        static readonly (string clip, float speed)[] PeopleGait = { ("walk", 1.54f), ("run_slow", 2.71f), ("run", 5.28f) };
        const float PushClipSpeed = .36f;

        public string State { get; private set; } = "idle";
        public int ClipCount => clips.Count;
        public bool Humanoid { get; private set; }
        public float HipsHeight { get; private set; }        // the model's hips over its feet in the bind pose (m)
        public float GaitRate { get; private set; }          // playback rate of the walk / run blend (1 = the clip's own speed)
        public float GaitSpeed { get; private set; }         // the speed the blended gait is made for at rate 1 (m/s)
        public string AttackClip = "attack";                 // Dan: punch, Abi: kick
        public float WalkSpeed = 1.2f, RunSpeed = 7.6f;      // Kira's clips (m/s)
        CharacterMotor motor;
        PlayableGraph graph; AnimationMixerPlayable baseMix, upperMix; AnimationLayerMixerPlayable layers;
        readonly Dictionary<string, int> index = new Dictionary<string, int>(), upperIndex = new Dictionary<string, int>();
        readonly List<AnimationClip> clips = new List<AnimationClip>(), upperClips = new List<AnimationClip>();
        float[] w, target; float upperW; int lastUpper = -1;
        (int i, float speed)[] gait; float gaitSeen = -10f;
        string oneShot; float oneShotStart, oneShotEnd; bool oneShotUpper;

        // Round 5: a creature with no motor (enemies, mammoths) says what to play: a loop name (null = idle / gait by speed) and its speed
        public System.Func<(string loop, float speed)> Drive;

        public static CreatureAnimator Attach(GameObject model, string resource, CharacterMotor motor, float walk = 1.2f, float run = 7.6f)
        {
            var anim = model.GetComponentInChildren<Animator>() ?? model.AddComponent<Animator>();
            anim.applyRootMotion = false; anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;   // three heroes: cheap; and a batch-mode check only draws at screenshots
            bool human = anim.avatar && anim.avatar.isHuman;
            var all = Resources.LoadAll<AnimationClip>(human ? "Characters/Clips" : "Characters/" + resource);
            var c = model.AddComponent<CreatureAnimator>(); c.motor = motor; c.Humanoid = human; c.WalkSpeed = walk; c.RunSpeed = run;
            if (human)
            {
                var hips = anim.GetBoneTransform(HumanBodyBones.Hips);
                c.HipsHeight = hips ? hips.position.y - model.transform.position.y : ClipHips;
            }
            c.graph = PlayableGraph.Create(model.name + " clips");
            c.graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            foreach (var n in human ? PeopleClips : KiraClips)
            {
                AnimationClip clip = null;
                // Round 6: a humanoid with its own clip of this name (Clips/<model>_<clip>: the mummy's shamble) plays that instead
                if (human) foreach (var a in Resources.LoadAll<AnimationClip>("Characters/Clips/" + resource + "_" + n)) if (!a.name.StartsWith("__preview__")) { clip = a; break; }
                if (!clip) foreach (var a in all) if (a.name == n || a.name.EndsWith("|" + n)) { clip = a; break; }
                if (!clip) continue;
                c.index[n] = c.clips.Count; c.clips.Add(clip);
                if (human && UpperBody.Contains(n)) { c.upperIndex[n] = c.upperClips.Count; c.upperClips.Add(clip); }
            }
            c.baseMix = Mixer(c.graph, c.clips);
            c.w = new float[c.clips.Count]; c.target = new float[c.clips.Count];
            if (c.index.TryGetValue("idle", out int idle)) { c.w[idle] = 1; c.baseMix.SetInputWeight(idle, 1); }
            Playable top = c.baseMix;
            if (human)
            {
                c.upperMix = Mixer(c.graph, c.upperClips);
                c.layers = AnimationLayerMixerPlayable.Create(c.graph, 2);
                c.graph.Connect(c.baseMix, 0, c.layers, 0); c.graph.Connect(c.upperMix, 0, c.layers, 1);
                c.layers.SetInputWeight(0, 1); c.layers.SetInputWeight(1, 0);
                c.layers.SetLayerMaskFromAvatarMask(1, UpperMask());
                top = c.layers;
            }
            AnimationPlayableOutput.Create(c.graph, "out", anim).SetSourcePlayable(top);
            // the gait: clips and the speed each is made for, slowest first
            var g = new List<(int, float)>();
            if (human) { foreach (var (n, v) in PeopleGait) if (c.index.TryGetValue(n, out int k)) g.Add((k, v * c.HipsHeight / ClipHips)); }
            else
            {
                if (c.index.TryGetValue("walk", out int k)) g.Add((k, c.WalkSpeed));
                if (c.index.TryGetValue("run", out k)) g.Add((k, c.RunSpeed));
            }
            c.gait = g.ToArray();
            c.graph.Play();
            return c;
        }

        static AnimationMixerPlayable Mixer(PlayableGraph graph, List<AnimationClip> list)
        {
            var m = AnimationMixerPlayable.Create(graph, list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                var p = AnimationClipPlayable.Create(graph, list[i]);
                p.SetApplyFootIK(true);
                graph.Connect(p, 0, m, i); m.SetInputWeight(i, 0);
            }
            return m;
        }

        static AvatarMask upperMask;
        static AvatarMask UpperMask()
        {
            if (upperMask) return upperMask;
            upperMask = new AvatarMask { name = "Upper body" };
            for (var p = AvatarMaskBodyPart.Root; p < AvatarMaskBodyPart.LastBodyPart; p++)
                upperMask.SetHumanoidBodyPartActive(p, p == AvatarMaskBodyPart.Body || p == AvatarMaskBodyPart.Head || p == AvatarMaskBodyPart.LeftArm ||
                    p == AvatarMaskBodyPart.RightArm || p == AvatarMaskBodyPart.LeftFingers || p == AvatarMaskBodyPart.RightFingers);
            return upperMask;
        }

        void OnDestroy() { if (graph.IsValid()) graph.Destroy(); }

        // Fix 13.2 item 5 (nobody runs in place): the speed the clips follow is the motor's (or Drive's) speed, but never more than the
        // body really moves (a frozen party kept its last velocity and ran on the spot; a follower against a wall too). A motor that
        // hasn't been stepped for a moment (a cutscene, a held party, Muscle Man at a job) stands on the ground.
        Vector3 lastPos; bool hasLast; float realSpeed;
        bool Stepped => motor && Time.time - motor.LastStepTime < .15f;
        float RawSpeed { get { if (!motor) return Drive != null ? Drive().speed : 0; if (!Stepped) return 0; var v = motor.Velocity; v.y = 0; return v.magnitude; } }
        float FlatSpeed => Mathf.Min(RawSpeed, realSpeed + .1f);
        bool Grounded => !motor || motor.Grounded || !Stepped;
        bool Moving => motor && (FlatSpeed > 1f || (!Grounded && motor.Mode != MoveMode.Swim));
        public float RealSpeed => realSpeed;
        // the check: a walk / run clip on a body that doesn't move for more than half a second
        public static int RunInPlaceCount { get; private set; }
        public static string RunInPlaceLast = "";
        float inPlaceFor;
        public bool Has(string clip) => index.ContainsKey(clip == "attack" ? AttackClip : clip);

        // Fix 13.2 item 3: any clip (a loop too: pushing, pulling) held for a while, not cancelled by moving (Muscle Man's jobs); Release() ends it
        bool forced;
        public void Hold(string clip, float seconds)
        {
            if (!index.ContainsKey(clip)) return;
            oneShot = clip; oneShotUpper = false; oneShotStart = Time.time; oneShotEnd = Time.time + seconds; forced = true;
            PlayLog.Line($"[Anim] {name}: holds {clip} ({(isActiveAndEnabled ? "active" : "INACTIVE")})");
            var p = baseMix.GetInput(index[clip]); p.SetTime(0); p.SetSpeed(1);
        }
        public void Release() { if (forced) { oneShot = null; forced = false; } }
        void OnDisable() { if (forced) PlayLog.Line($"[Anim] {name}: disabled while holding {oneShot}"); }

        // a one-shot clip now (attack, hurt, jump, land, cheer); the loop comes back when it ends
        public void Trigger(string clip, float maxSeconds = 0)   // Fix 13.1: maxSeconds > 0 cuts a long clip (offer, 10 s) short
        {
            if (clip == "attack") clip = AttackClip;
            if (clip == "land_hard" && !index.ContainsKey(clip)) clip = "land";
            if (!index.ContainsKey(clip) || !OneShots.Contains(clip)) return;
            if (forced && (clip == "land" || clip == "land_hard" || clip == "jump" || clip == "hurt")) return;   // Fix 13.2: a held job clip isn't cut by a landing
            if ((clip == "land" || clip == "land_hard") && oneShot != null && (oneShot == AttackClip || oneShot == "hurt")) return;
            bool upper = Humanoid && UpperBody.Contains(clip) && Moving;
            Rate.TryGetValue(clip, out float rate); if (rate <= 0) rate = 1;
            if (forced) PlayLog.Line($"[Anim] {name}: the held {oneShot} is replaced by {clip}");
            oneShot = clip; oneShotUpper = upper; oneShotStart = Time.time; forced = false;
            var c = upper ? upperClips[upperIndex[clip]] : clips[index[clip]];
            oneShotEnd = Time.time + c.length / rate * .95f;
            if (maxSeconds > 0) oneShotEnd = Mathf.Min(oneShotEnd, Time.time + maxSeconds);
            var p = upper ? upperMix.GetInput(upperIndex[clip]) : baseMix.GetInput(index[clip]);
            p.SetTime(0); p.SetSpeed(rate);
        }

        string Loop(float s)
        {
            if (!motor) { var d = Drive != null ? Drive().loop : null; if (d != null && (d == "walk" || d == "run" || d == "run_slow") && s < .25f) d = null; return d ?? (s < .25f ? "idle" : "gait"); }
            if (!Stepped) return "idle";
            if (motor.Mode == MoveMode.Swim && motor.Struggling && index.ContainsKey("mm_struggle")) return "mm_struggle";   // Round 10
            if (motor.Sliding && index.ContainsKey("slide")) return "slide";
            if (motor.Mode == MoveMode.Swim) return (s > .6f || Mathf.Abs(motor.Velocity.y) > 1f || !index.ContainsKey("tread")) ? "swim" : "tread";
            if (!Grounded) return "fall";
            if (Time.time - motor.LastPush < .2f && index.ContainsKey("push")) return "push";
            return s < .25f ? "idle" : "gait";
        }

        void Update()
        {
            if (!graph.IsValid() || clips.Count == 0) return;
            float dt = Time.deltaTime;
            // how fast the body really moves (smoothed; a teleport resets it)
            var body = motor ? motor.transform : transform;
            if (dt > 0)
            {
                var d = body.position - lastPos; d.y = 0;
                if (!hasLast || d.magnitude > 4f) { realSpeed = RawSpeed; hasLast = true; }
                else realSpeed = Mathf.Lerp(realSpeed, d.magnitude / dt, 1f - Mathf.Exp(-dt * 12f));
                lastPos = body.position;
            }
            float s = FlatSpeed;
            if (oneShot != null)
            {
                bool end = Time.time > oneShotEnd;
                if (!forced && CancelOnMove.Contains(oneShot) && Time.time - oneShotStart > .15f && (s > 1.2f || !Grounded)) end = true;
                if (oneShot == "jump" && motor && motor.Grounded && Time.time - oneShotStart > .1f) end = true;
                if (end) { if (forced) PlayLog.Line($"[Anim] {name}: the held {oneShot} ends (time)"); oneShot = null; forced = false; }
            }
            string loop = Loop(s);
            for (int i = 0; i < target.Length; i++) target[i] = 0;
            string state = loop;
            if (oneShot != null && !oneShotUpper) target[index[oneShot]] = 1;
            else if (loop == "gait" && gait.Length > 0) state = GaitWeights(s, dt);
            else if (index.TryGetValue(loop, out int li)) target[li] = 1;
            else if (index.TryGetValue("idle", out li)) target[li] = 1;
            State = oneShot ?? state;
            bool gaitClip = oneShot == null && (loop == "gait" || state == "walk" || state == "run" || state == "run_slow");
            inPlaceFor = gaitClip && realSpeed < .15f ? inPlaceFor + dt : 0;
            if (inPlaceFor > .5f && inPlaceFor - dt <= .5f) { RunInPlaceCount++; RunInPlaceLast = $"{(motor ? motor.name : name)} plays {state} at {realSpeed:0.00} m/s"; PlayLog.Line("[RunInPlace] " + RunInPlaceLast); }
            // crossfade, weights kept summing to 1
            float sum = 0, step = dt / Fade;
            for (int i = 0; i < w.Length; i++) { w[i] = Mathf.MoveTowards(w[i], target[i], step); sum += w[i]; }
            for (int i = 0; i < w.Length; i++) baseMix.SetInputWeight(i, sum > 0 ? w[i] / sum : 0);
            if (Humanoid)
            {
                upperW = Mathf.MoveTowards(upperW, oneShot != null && oneShotUpper ? 1 : 0, step);
                layers.SetInputWeight(1, upperW);
                if (oneShot != null && oneShotUpper) lastUpper = upperIndex[oneShot];   // kept while the layer fades out
                for (int i = 0; i < upperClips.Count; i++) upperMix.SetInputWeight(i, i == lastUpper ? 1 : 0);
            }
            // loops that follow the speed: push, swim
            if (loop == "push" && index.TryGetValue("push", out int pk)) baseMix.GetInput(pk).SetSpeed(Mathf.Clamp(s / (PushClipSpeed * Mathf.Max(.5f, HipsHeight) / ClipHips), .6f, 2.5f));
            if (loop == "swim" && index.TryGetValue("swim", out int sk)) baseMix.GetInput(sk).SetSpeed(Mathf.Clamp(.7f + s / Mathf.Max(1f, motor.Stats.swimSpeed) * .6f, .7f, 1.5f));
        }

        // walk / slow run / run by speed: the two clips around the speed share the weight, played in step (same share of their cycle),
        // at the rate that makes their feet move as fast as the hero (rate 1 between two clips; slower / faster at the ends)
        string GaitWeights(float s, float dt)
        {
            int a = 0, b = 0; float t = 0;
            if (s <= gait[0].speed) { a = b = 0; }
            else if (s >= gait[gait.Length - 1].speed) { a = b = gait.Length - 1; }
            else for (int i = 0; i < gait.Length - 1; i++)
                if (s <= gait[i + 1].speed) { a = i; b = i + 1; t = (s - gait[i].speed) / (gait[i + 1].speed - gait[i].speed); break; }
            float made = Mathf.Lerp(gait[a].speed, gait[b].speed, t);
            GaitSpeed = made; GaitRate = Mathf.Clamp(s / made, .5f, 1.8f);
            // every gait clip runs at the same share of its cycle per second (in step): resynced when the gait starts
            float la = clips[gait[a].i].length, lb = clips[gait[b].i].length, cycle = Mathf.Lerp(la, lb, t);
            bool starting = Time.time - gaitSeen > .2f; gaitSeen = Time.time;
            foreach (var (k, _) in gait)
            {
                var p = baseMix.GetInput(k);
                if (starting) p.SetTime(0);
                p.SetSpeed(GaitRate * clips[k].length / cycle);
            }
            target[gait[a].i] += 1 - t; target[gait[b].i] += t;
            string n = clips[gait[t < .5f ? a : b].i].name;
            return n.Contains("|") ? n.Substring(n.LastIndexOf('|') + 1) : n;
        }
    }
}
