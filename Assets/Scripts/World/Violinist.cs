using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ShardsOfTime
{
    // Round 4, item 6: the Violinist teaches moves (DECIDED, Dan 2026-11-09). He stands somewhere in each world; when the hero the move is
    // for reaches him, he plays a short tune (the cutscene ViolinistTeaches: no dialogue, he never explains himself) and that hero
    // learns the move (saved). Anyone else gets a hint that he is looking past them. Placeholder figure (Ice Age look: fur cloak, silver
    // beard) with the Blender bone fiddle until his model exists (Docs/MODELS-NEEDED.md #10).
    public sealed class Violinist : MonoBehaviour
    {
        public static Violinist I { get; private set; }
        public string ability = Abilities.GroundPound, learner = "Dan", moveName = "Ground Pound";
        public float radius = 2.6f;
        public bool Playing { get; private set; }
        public int Taught { get; private set; }
        Transform bowArm, bow, head; float hintAt = -10f; ParticleSystem notes;

        void OnEnable() => I = this;
        void OnDisable() { if (I == this) I = null; }

        public string instrument = "Fiddle";   // Round 6, item 4: per world (WorldData.violinistInstrument)

        public static Violinist Make(Transform parent, Vector3 at, float yaw, string instrument = "Fiddle")
        {
            var go = new GameObject("Violinist"); go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            var t = go.transform; var v = go.AddComponent<Violinist>(); v.instrument = instrument;
            var hide = Placeholders.Mat(new Color(.72f, .58f, .4f)); var cloak = Placeholders.Mat(new Color(.86f, .86f, .88f)); var skin = Placeholders.Mat(new Color(.93f, .78f, .66f));
            var silver = Placeholders.Mat(new Color(.82f, .83f, .86f)); var wrap = Placeholders.Mat(new Color(.55f, .5f, .46f));
            Placeholders.Prim(PrimitiveType.Capsule, t, new Vector3(0, .95f, 0), new Vector3(.5f, .62f, .38f), hide, name: "Tunic");
            Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.45f, -.05f), new Vector3(.78f, .42f, .55f), cloak, name: "Cloak");
            Placeholders.Prim(PrimitiveType.Capsule, t, new Vector3(0, 1.15f, -.16f), new Vector3(.62f, .45f, .25f), cloak, name: "Cloak back");
            foreach (float s in new[] { -1f, 1f }) Placeholders.Prim(PrimitiveType.Capsule, t, new Vector3(s * .12f, .3f, 0), new Vector3(.16f, .32f, .16f), wrap, name: "Leg");
            v.head = Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.72f, 0), new Vector3(.3f, .34f, .3f), skin, name: "Head").transform;
            Placeholders.Prim(PrimitiveType.Sphere, v.head, new Vector3(0, .32f, -.12f), new Vector3(1.05f, .55f, 1.0f), silver, name: "Hair");
            Placeholders.Prim(PrimitiveType.Sphere, v.head, new Vector3(0, -.42f, .25f), new Vector3(.7f, .6f, .55f), silver, name: "Beard");
            foreach (float s in new[] { -1f, 1f }) Placeholders.Prim(PrimitiveType.Sphere, v.head, new Vector3(s * .2f, .06f, .44f), new Vector3(.12f, .1f, .06f), Placeholders.Mat(new Color(.35f, .6f, .9f)), name: "Eye");
            // the fiddle under his chin (left arm), the bow in his right hand
            var leftArm = Placeholders.Prim(PrimitiveType.Capsule, t, new Vector3(-.28f, 1.35f, .2f), new Vector3(.12f, .3f, .12f), cloak, name: "Left arm").transform;
            leftArm.localRotation = Quaternion.Euler(70, 25, 0);
            var fiddle = WorldArt.Prop("instruments", instrument, t, Vector3.zero);
            if (fiddle) { fiddle.transform.SetParent(t, false); fiddle.transform.localPosition = new Vector3(-.12f, 1.48f, .25f); fiddle.transform.localRotation = Quaternion.Euler(-80, 0, 30); }
            var armPivot = new GameObject("Bow arm").transform; armPivot.SetParent(t, false); armPivot.localPosition = new Vector3(.27f, 1.45f, 0);
            Placeholders.Prim(PrimitiveType.Capsule, armPivot, new Vector3(0, -.25f, .1f), new Vector3(.12f, .28f, .12f), cloak, name: "Right arm").transform.localRotation = Quaternion.Euler(30, 0, 0);
            var bowGo = Placeholders.Prim(PrimitiveType.Cylinder, armPivot, new Vector3(-.2f, -.4f, .3f), new Vector3(.02f, .35f, .02f), Placeholders.Mat(new Color(.42f, .28f, .18f)), name: "Bow");
            bowGo.transform.localRotation = Quaternion.Euler(0, 0, 70);
            v.bowArm = armPivot; v.bow = bowGo.transform;
            v.notes = MakeNotes(t);
            v.UseModel(fiddle);
            return v;
        }

        // Round 5: his Meshy model (Mixamo rig, Resources/Characters/violinist_iceage), his own clips (Clips/violinist_play, _bow, _nod)
        // and the shared idle / walk replace the placeholder figure, which stays hidden as the fallback. The bone fiddle rides his left hand.
        public const string Model = "violinist_iceage";   // the Ice Age's; Round 6: each world's own when it exists (ModelName)
        public string ModelName => instrument == "Lyre" && CharacterModel.Exists("violinist_egypt") ? "violinist_egypt" : Model;
        PlayableGraph graph; AnimationMixerPlayable mix; readonly Dictionary<string, int> clipIndex = new Dictionary<string, int>(); float[] weights;
        string loop = "idle", oneShot; float oneShotEnd;
        public string ClipNow => oneShot ?? loop;
        public bool HasModel => graph.IsValid();

        void UseModel(GameObject fiddle)
        {
            if (!CharacterModel.Exists(ModelName)) return;
            var gray = new GameObject("Graybox").transform; gray.SetParent(transform, false);
            var kids = new List<Transform>(); foreach (Transform c in transform) if (c != gray && c.name != "Notes") kids.Add(c);
            foreach (var c in kids) c.SetParent(gray, true);
            gray.gameObject.SetActive(false); bowArm = null; bow = null; head = null;
            var go = CharacterModel.Spawn(ModelName, transform);
            var anim = go.GetComponentInChildren<Animator>(); anim.applyRootMotion = false; anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            graph = PlayableGraph.Create("Violinist clips"); graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            var clips = new List<AnimationClip>();
            // his own clips (play, bow, nod) and the people's shared idle and walk (Dan's choice, Round 5)
            foreach (var (n, file) in new[] { ("idle", "idle"), ("play", "violinist_play"), ("walk", "walk"), ("bow", "violinist_bow"), ("nod", "violinist_nod") })
            {
                AnimationClip c = null;
                foreach (var a in Resources.LoadAll<AnimationClip>("Characters/Clips/" + file)) if (!a.name.StartsWith("__preview__")) { c = a; break; }
                if (c) { clipIndex[n] = clips.Count; clips.Add(c); }
            }
            mix = AnimationMixerPlayable.Create(graph, clips.Count);
            for (int i = 0; i < clips.Count; i++) { var cp = AnimationClipPlayable.Create(graph, clips[i]); graph.Connect(cp, 0, mix, i); }
            weights = new float[clips.Count];
            AnimationPlayableOutput.Create(graph, "out", anim).SetSourcePlayable(mix);
            graph.Play();
            // Round 6, item 4: his instrument (and the fiddle's bow) placed from his bones every frame (LateUpdate, world space, so no bone
            // scale inflates them); his arms are bent onto it by a small two-bone IK after the clip
            if (anim.isHuman && fiddle)
            {
                B = b => anim.GetBoneTransform(b);
                inst = fiddle.transform; inst.SetParent(transform, true); inst.localScale = Vector3.one;
                if (instrument == "Fiddle") { var bg = WorldArt.Prop("instruments", "Bow", transform, transform.position); if (bg) { bowT = bg.transform; bowT.localScale = Vector3.one; } }
            }
            else if (fiddle) fiddle.SetActive(false);
            PlayLog.Line($"the Violinist: model {ModelName} ({instrument}), clips [{string.Join(", ", clipIndex.Keys)}]");
        }

        void OnDestroy() { if (graph.IsValid()) graph.Destroy(); }
        System.Func<HumanBodyBones, Transform> B; Transform inst, bowT;
        public Transform Instrument => inst;
        public string InstrumentPose { get; private set; } = "";

        static Transform M(Transform root, string n) { foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == n) return t; return null; }

        // turn and move `root` so marker a sits at `at`, a->b points along `dir` and a->up leans toward `up`
        static void Align(Transform root, Transform a, Transform b, Transform upM, Vector3 at, Vector3 dir, Vector3 up)
        {
            if (!a || !b) return;
            var curF = b.position - a.position; var curU = upM ? upM.position - a.position : root.up;
            if (curF.sqrMagnitude < 1e-8f || dir.sqrMagnitude < 1e-8f) return;
            root.rotation = Quaternion.LookRotation(dir, up) * Quaternion.Inverse(Quaternion.LookRotation(curF, curU)) * root.rotation;
            root.position += at - a.position;
        }

        // bend a two-bone chain (upper arm, forearm, hand) so the hand reaches `target`, the elbow toward `pole`
        static void Reach(Transform up, Transform low, Transform hand, Vector3 target, Vector3 pole)
        {
            if (!up || !low || !hand) return;
            float la = Vector3.Distance(up.position, low.position), lb = Vector3.Distance(low.position, hand.position);
            var to = target - up.position; float d = Mathf.Clamp(to.magnitude, .01f, la + lb - .002f);
            var dirT = to.normalized; var axis = Vector3.Cross(dirT, pole - up.position);
            if (axis.sqrMagnitude < 1e-8f) axis = Vector3.Cross(dirT, Vector3.down);
            float cosA = Mathf.Clamp((la * la + d * d - lb * lb) / (2 * la * d), -1f, 1f);
            var elbow = up.position + Quaternion.AngleAxis(Mathf.Acos(cosA) * Mathf.Rad2Deg, axis.normalized) * dirT * la;
            up.rotation = Quaternion.FromToRotation(low.position - up.position, elbow - up.position) * up.rotation;
            low.rotation = Quaternion.FromToRotation(hand.position - low.position, up.position + dirT * d - low.position) * low.rotation;
        }

        // Round 6, item 4: playing = the instrument where it's played, his hands on it; walking = on his back; otherwise held in his hands
        void LateUpdate()
        {
            if (!inst || B == null) { InstrumentPose = inst ? "no humanoid" : "no instrument"; return; }
            Transform neck = B(HumanBodyBones.Neck), head = B(HumanBodyBones.Head), hips = B(HumanBodyBones.Hips);
            Transform chest = B(HumanBodyBones.UpperChest); if (!chest) chest = B(HumanBodyBones.Chest); if (!chest) chest = B(HumanBodyBones.Spine);   // Round 6: the Ice Age rig maps no chest
            Transform lu = B(HumanBodyBones.LeftUpperArm), ll = B(HumanBodyBones.LeftLowerArm), lh = B(HumanBodyBones.LeftHand);
            Transform ru = B(HumanBodyBones.RightUpperArm), rl = B(HumanBodyBones.RightLowerArm), rh = B(HumanBodyBones.RightHand);
            if (!neck || !head || !lh || !rh || !hips || !chest)
            {
                InstrumentPose = $"missing bones:{(neck ? "" : " neck")}{(head ? "" : " head")}{(lh ? "" : " left hand")}{(rh ? "" : " right hand")}{(hips ? "" : " hips")}{(chest ? "" : " chest")}";
                return;
            }
            Vector3 f = transform.forward, r = transform.right, u = Vector3.up, l = -r;
            string pose = ClipNow == "play" ? "play" : ClipNow == "walk" ? "back" : "hold"; InstrumentPose = pose;
            if (instrument == "Fiddle")
            {
                Transform chin = M(inst, "M_Chin"), scroll = M(inst, "M_Scroll"), upM = M(inst, "M_Up"), grip = M(inst, "M_Grip"), contact = M(inst, "M_Bow");
                Transform frog = bowT ? M(bowT, "M_Frog") : null, tip = bowT ? M(bowT, "M_Tip") : null;
                if (!chin || !scroll || !upM || !grip || !contact) return;
                if (pose == "play")
                {
                    // under the chin on his left collarbone, the neck out to his front-left, the strings up and turned toward the bow
                    var jaw = Vector3.Lerp(neck.position, head.position, .25f);
                    var dir = (f * .72f + l * .68f + u * .08f).normalized;
                    Align(inst, chin, scroll, upM, jaw + l * .07f + f * .03f - u * .05f, dir, (u * .85f + r * .35f).normalized);
                    Reach(lu, ll, lh, grip.position, lu.position - u * .45f + f * .25f);
                    // the bow across the strings by the bridge, sawing (his right hand at the frog)
                    var su = (upM.position - chin.position).normalized;
                    var across = Vector3.Cross(su, dir).normalized; if (Vector3.Dot(across, r) < 0) across = -across;
                    float k = .3f + Mathf.Sin(Time.time * 4.2f) * .2f;
                    Reach(ru, rl, rh, contact.position + across * k + su * .02f, ru.position + r * .45f - u * .25f);
                    if (bowT) { var toC = (contact.position - rh.position).normalized; Align(bowT, frog, tip, null, rh.position + toC * .04f, toC, su); }
                }
                else if (pose == "back")
                {
                    var at = chest.position - f * .2f - u * .25f + l * .12f; var d = (u * .9f + r * .45f).normalized;
                    Align(inst, chin, scroll, upM, at, d, -f);
                    if (bowT) Align(bowT, frog, tip, null, at + r * .05f - f * .03f, d, -f);
                }
                else
                {
                    // held by the neck in his left hand, the body hanging below; the bow in his right hand, pointing down
                    Align(inst, grip, scroll, upM, lh.position + f * .03f, (u * .9f + f * .3f).normalized, f);
                    if (bowT) Align(bowT, frog, tip, null, rh.position + f * .03f, (-u * .9f + f * .35f).normalized, f);
                }
            }
            else
            {
                Transform bas = M(inst, "M_Base"), top = M(inst, "M_Top"), front = M(inst, "M_Front"), hold = M(inst, "M_Hold"), pluck = M(inst, "M_Pluck");
                if (!bas || !top || !front || !hold || !pluck) return;
                if (pose == "play")
                {
                    // upright against his left side, the yoke at shoulder height; the left hand on its arm, the right hand plucking
                    var at = hips.position + f * .34f + l * .14f - u * .12f;   // its top at his chest, his face clear (Round 6 close-up)
                    Align(inst, bas, top, front, at, (u * .9f + l * .36f + f * .2f).normalized, (f * .8f + r * .6f).normalized);
                    Reach(lu, ll, lh, hold.position, lu.position - u * .3f + l * .35f);
                    var side = (hold.position - pluck.position).normalized;
                    var pl = pluck.position + side * Mathf.Sin(Time.time * 6f) * .05f + (front.position - pluck.position).normalized * .02f;
                    Reach(ru, rl, rh, pl, ru.position + r * .4f - u * .3f);
                }
                else if (pose == "back")
                    Align(inst, bas, top, front, chest.position - f * .22f - u * .35f, (u * .95f + r * .2f).normalized, -f);
                else
                {
                    Align(inst, hold, top, front, lh.position + f * .04f, u, f);
                }
            }
        }

        // a one-shot (bow, nod) now; the loop comes back when it ends
        public void OneShot(string n)
        {
            if (!HasModel || !clipIndex.TryGetValue(n, out int i)) return;
            oneShot = n; var p = mix.GetInput(i); p.SetTime(0); oneShotEnd = Time.time + ((AnimationClipPlayable)p).GetAnimationClip().length * .95f;
        }

        void Blend()
        {
            if (!HasModel || weights.Length == 0) return;
            if (oneShot != null && Time.time > oneShotEnd) oneShot = null;
            string want = ClipNow; if (!clipIndex.ContainsKey(want)) want = "idle";
            float sum = 0;
            for (int i = 0; i < weights.Length; i++) { weights[i] = Mathf.MoveTowards(weights[i], clipIndex.TryGetValue(want, out int k) && k == i ? 1 : 0, Time.deltaTime / .25f); sum += weights[i]; }
            for (int i = 0; i < weights.Length; i++) mix.SetInputWeight(i, sum > 0 ? weights[i] / sum : 0);
        }

        static ParticleSystem MakeNotes(Transform t)
        {
            var go = new GameObject("Notes"); go.transform.SetParent(t, false); go.transform.localPosition = new Vector3(0, 1.8f, .3f);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.startLifetime = 2.2f; main.startSpeed = .8f; main.startSize = .18f; main.startColor = new Color(.7f, .9f, 1f, 1f); main.maxParticles = 60;
            var em = ps.emission; em.rateOverTime = 0;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 35; sh.radius = .1f; sh.rotation = new Vector3(-90, 0, 0);
            var r = go.GetComponent<ParticleSystemRenderer>(); r.material = Resources.Load<Material>("Materials/Particle");
            return ps;
        }

        void Update()
        {
            // the tune: the bow arm saws, notes drift up (during his cutscene, and a slow idle sway otherwise)
            bool play = Playing || (Cutscenes.Playing != null && Cutscenes.Playing.StartsWith("ViolinistTeaches"));
            var em = notes.emission; em.rateOverTime = play ? 6f : 0f;
            var nl = play ? "play" : "idle"; if (nl != loop) PlayLog.Line($"the Violinist: {nl}"); loop = nl; Blend();
            if (bowArm) bowArm.localRotation = Quaternion.Euler(0, play ? Mathf.Sin(Time.time * 7f) * 25f : 0, play ? 0 : 10f);
            if (head) head.localRotation = Quaternion.Euler(play ? 8f : 0, 0, play ? Mathf.Sin(Time.time * 1.5f) * 8f : Mathf.Sin(Time.time * .7f) * 3f);
            if (!Game.Playing || !Party.I || Game.I.Busy) return;
            var p = Party.I.Player; var d = p.transform.position - transform.position; d.y = 0;
            if (d.magnitude > radius) return;
            if (SaveGame.Current.Has(ability)) return;
            if (p.Stats.displayName == learner) { Game.I.StartCoroutine(Teach(p)); return; }
            if (Time.time > hintAt + 6f) { hintAt = Time.time; Hud.Message($"The musician smiles, but he is looking past you, at {learner}.", 4f); }
        }

        IEnumerator NodLater() { yield return new WaitForSeconds(1.6f); OneShot("nod"); }

        IEnumerator Teach(Hero hero)
        {
            Game.I.SetBusy(true); Playing = true;
            PlayLog.Line($"the Violinist plays for {hero.Stats.displayName}");
            Sfx.Play(instrument == "Lyre" ? "lyre_tune" : "violinist_tune", .8f);
            var face = Quaternion.LookRotation(Vector3.ProjectOnPlane(hero.transform.position - transform.position, Vector3.up).normalized);
            transform.rotation = face;
            yield return Cutscenes.Play(Game.InEgypt ? "ViolinistTeachesEgypt" : "ViolinistTeaches", transform.position, face);
            Playing = false;
            OneShot("bow"); Game.I.StartCoroutine(NodLater());
            if (!SaveGame.Current.abilities.Contains(ability)) SaveGame.Current.abilities.Add(ability);
            Party.I.ApplyAbilities(); Taught++;
            Sfx.Play("story", .6f, 1.2f);
            Hud.Message(ability == Abilities.DoubleJump
                ? $"{hero.Stats.displayName} learned the {moveName}! Press {Controls.Btn(Controls.Act.Jump)} again in the air. High ledges everywhere are in reach now (the Ice Age's too)."
                : $"{hero.Stats.displayName} learned the {moveName}! Jump, then press {Controls.Btn(Controls.Act.Dive)}. It breaks cracked ice and stuns enemies.", 7f);
            PlayLog.Line($"ability {ability} learned from the Violinist");
            SaveGame.Save("learned " + ability);
            Game.I.SetBusy(false);
        }
    }
}
