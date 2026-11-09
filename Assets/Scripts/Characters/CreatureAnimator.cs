using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ShardsOfTime
{
    // Round 2, item 9: plays a rigged model's clips from the motor's state (no Animator Controller asset: a small Playables mixer with
    // crossfades). Clips come from the model's FBX (Resources/Characters/<model>.fbx, takes named "...|idle", "...|walk", ...).
    // Loops: idle, walk, run, fall, swim. One-shots (played to the end, then back to the loop): jump, land, attack, hurt.
    public sealed class CreatureAnimator : MonoBehaviour
    {
        public static readonly string[] ClipNames = { "idle", "walk", "run", "jump", "fall", "land", "swim", "attack", "hurt" };
        static readonly HashSet<string> OneShots = new HashSet<string> { "jump", "land", "attack", "hurt" };
        const float Fade = .12f;

        public string State { get; private set; } = "idle";
        public int ClipCount => clips.Count;
        public float WalkSpeed = 1.2f, RunSpeed = 7.6f;   // m/s the walk / run clips are made for (Kira)
        CharacterMotor motor;
        PlayableGraph graph; AnimationMixerPlayable mixer;
        readonly Dictionary<string, int> index = new Dictionary<string, int>();
        readonly List<AnimationClip> clips = new List<AnimationClip>();
        string oneShot; float oneShotEnd;

        public static CreatureAnimator Attach(GameObject model, string resource, CharacterMotor motor)
        {
            var anim = model.GetComponentInChildren<Animator>() ?? model.AddComponent<Animator>();
            anim.applyRootMotion = false; anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            var all = Resources.LoadAll<AnimationClip>("Characters/" + resource);
            var c = model.AddComponent<CreatureAnimator>(); c.motor = motor;
            c.graph = PlayableGraph.Create(model.name + " clips");
            c.graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            foreach (var n in ClipNames)
            {
                AnimationClip clip = null;
                foreach (var a in all) if (a.name == n || a.name.EndsWith("|" + n)) { clip = a; break; }
                if (clip) { c.index[n] = c.clips.Count; c.clips.Add(clip); }
            }
            c.mixer = AnimationMixerPlayable.Create(c.graph, c.clips.Count);
            for (int i = 0; i < c.clips.Count; i++)
            {
                var p = AnimationClipPlayable.Create(c.graph, c.clips[i]);
                c.graph.Connect(p, 0, c.mixer, i);
                c.mixer.SetInputWeight(i, c.clips[i].name.EndsWith("idle") ? 1 : 0);
            }
            AnimationPlayableOutput.Create(c.graph, "out", anim).SetSourcePlayable(c.mixer);
            c.graph.Play();
            return c;
        }

        void OnDestroy() { if (graph.IsValid()) graph.Destroy(); }

        // a one-shot clip now (attack, hurt, jump, land); the loop comes back when it ends
        public void Trigger(string clip)
        {
            if (!index.TryGetValue(clip, out int i)) return;
            if (clip == "land" && (oneShot == "attack" || oneShot == "hurt")) return;
            oneShot = clip; oneShotEnd = Time.time + clips[i].length * .95f;
            var p = mixer.GetInput(i); p.SetTime(0); p.SetSpeed(1);
        }

        string Loop()
        {
            if (!motor) return "idle";
            var v = motor.Velocity; v.y = 0; float s = v.magnitude;
            if (motor.Mode == MoveMode.Swim) return "swim";
            if (!motor.Grounded) return motor.Velocity.y > .5f && oneShot == "jump" ? "jump" : "fall";
            if (s < .25f) return "idle";
            return s < WalkSpeed * 1.6f ? "walk" : "run";
        }

        void Update()
        {
            if (!graph.IsValid() || clips.Count == 0) return;
            if (oneShot != null && Time.time > oneShotEnd) oneShot = null;
            State = oneShot ?? Loop();
            float dt = Time.deltaTime / Fade;
            for (int i = 0; i < clips.Count; i++)
            {
                bool on = clips[i].name == State || clips[i].name.EndsWith("|" + State);
                mixer.SetInputWeight(i, Mathf.MoveTowards(mixer.GetInputWeight(i), on ? 1 : 0, dt));
            }
            // walk / run play at the speed the hero moves (no foot sliding)
            if (motor && (State == "walk" || State == "run") && index.TryGetValue(State, out int k))
            {
                var v = motor.Velocity; v.y = 0;
                mixer.GetInput(k).SetSpeed(Mathf.Clamp(v.magnitude / (State == "walk" ? WalkSpeed : RunSpeed), .5f, 1.6f));
            }
        }
    }
}
