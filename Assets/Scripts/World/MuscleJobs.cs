using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 8, item 11: Muscle Man's jobs (after the stone he follows the party). Near a job the HUD says what he'll do; the attack button
    // (X / left click) sets him to it: he steps up, does it (his own clip), the thing stays done (saved by id), and he follows again.
    // Kinds: a boulder or huge block (every world: he rolls it aside), a portcullis (he heaves it up; it stays up) and a boost spot (he
    // throws the player up to a high ledge; any time). Before he has joined, a boulder only says it needs someone much stronger.
    public abstract class MuscleJob : MonoBehaviour
    {
        public static readonly List<MuscleJob> All = new List<MuscleJob>();
        public string id;                       // saved in the world's opened list when done (null: can be done any number of times)
        public float radius = 3.6f;
        public int Done { get; protected set; }
        protected abstract string Verb { get; }
        protected abstract string Clip { get; }
        protected virtual bool Allowed(Hero player) => true;
        protected virtual Vector3 StandAt(Hero player) { var d = player.transform.position - transform.position; d.y = 0; return transform.position + (d.sqrMagnitude > .01f ? d.normalized : -transform.forward) * (Reach + .6f); }
        protected virtual float Reach => 2.2f;
        protected abstract IEnumerator Work(Hero player, Hero mm);
        protected virtual void Opened() { }
        // done at once, no Muscle Man needed (the never-stuck check opens a gate before dropping a hero in behind it)
        public void OpenNow()
        {
            if (IsDone || string.IsNullOrEmpty(id)) return;
            var prog = SaveGame.Current.World(Game.World.worldId); prog.opened.Add(id); Opened();
            PlayLog.Line($"{GetType().Name} {id} opened at once");
        }
        float hintAt = -10f;
        public static int Jobs { get; private set; }

        public bool IsDone => !string.IsNullOrEmpty(id) && Game.World && SaveGame.Current.World(Game.World.worldId).opened.Contains(id);
        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);

        static bool Ready => Party.I && Party.I.Companion && SaveGame.Current.Has(Abilities.MuscleMan) && Game.Playing && !Game.I.Busy;

        public static MuscleJob Near(Hero player)
        {
            MuscleJob best = null; float bd = float.MaxValue;
            foreach (var j in All)
            {
                if (!j || !j.isActiveAndEnabled || j.IsDone || !j.Allowed(player)) continue;
                var d = player.transform.position - j.transform.position; d.y *= .5f; float m = d.magnitude;
                if (m < j.radius && m < bd) { bd = m; best = j; }
            }
            return best;
        }

        public static bool TryStart(Hero player)
        {
            if (!Ready) return false;
            var j = Near(player); if (!j) return false;
            Game.I.StartCoroutine(j.Run(player));
            return true;
        }

        void Update()
        {
            if (!Ready || !Party.I.Player || IsDone || Time.time < hintAt + 5f) return;
            var p = Party.I.Player; if (!Allowed(p)) return;
            var d = p.transform.position - transform.position; d.y *= .5f;
            if (d.magnitude < radius) { hintAt = Time.time; Hud.Message($"{Controls.Btn(Controls.Act.Attack)}: Muscle Man {Verb}", 3.5f); }
        }

        IEnumerator Run(Hero player)
        {
            var mm = Party.I.Companion;
            Game.I.SetBusy(true); Jobs++; Done++;
            PlayLog.Line($"Muscle Man's job: {GetType().Name} {id} ({Verb})");
            mm.Follower.enabled = false;
            var at = StandAt(player); at.y = Game.OnGround(at.x, at.z).y;
            var look = transform.position - at; look.y = 0;
            mm.Motor.Teleport(at + Vector3.up * .05f, Quaternion.LookRotation(look.sqrMagnitude > .01f ? look : Vector3.forward));
            Placeholders.Puff(at + Vector3.up * .5f, new Color(.9f, .85f, .7f));
            if (mm.Anim && mm.Anim.Has(Clip)) mm.Anim.Trigger(Clip);
            Sfx.Play("mm_grunt", .7f, Random.Range(.92f, 1.05f));
            yield return Work(player, mm);
            if (!string.IsNullOrEmpty(id)) { var prog = SaveGame.Current.World(Game.World.worldId); if (!prog.opened.Contains(id)) prog.opened.Add(id); SaveGame.Save("Muscle Man: " + id); }
            mm.Follower.enabled = true; mm.Follower.ResetTimers();
            Game.I.SetBusy(false);
        }
    }

    // a boulder or a huge block: before Muscle Man, "way too heavy"; with him, he heaves it aside (it rolls 3.5 m out of the way; its
    // collider goes, so the way is open; reloaded already aside)
    public sealed class Boulder : MuscleJob
    {
        float lastMessage = -10;
        public int Touches { get; private set; }
        public Vector3 aside;                    // where it ends up (world offset); zero = 3.5 m to its right
        protected override string Verb => "rolls the boulder away";
        protected override string Clip => "mm_lift";
        protected override float Reach => Mathf.Max(1.8f, Mathf.Max(transform.lossyScale.x, transform.lossyScale.z) * .5f);
        public static event System.Action<Boulder, Hero> Tried;   // someone pushed it without Muscle Man (the rival laughs)

        void Start() { radius = Mathf.Max(radius, Reach + 2.5f); if (IsDone) Opened(); }
        protected override void Opened() { transform.position += Offset; Free(); }
        Vector3 Offset => aside.sqrMagnitude > .01f ? aside : Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized * 3.5f;
        void Free() { foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false; }

        public void Touched(CharacterMotor m)
        {
            if (!m.CompareTag("Player")) return;
            if (SaveGame.Current.Has(Abilities.MuscleMan)) return;
            Touches++;
            Tried?.Invoke(this, m.GetComponent<Hero>());
            if (Time.time - lastMessage > 4) { lastMessage = Time.time; Hud.Message("Way too heavy. Someone much stronger could move this... (needs Muscle Man)"); }
        }

        protected override IEnumerator Work(Hero player, Hero mm)
        {
            yield return new WaitForSeconds(.9f);
            Sfx.Play("boulder_roll", .7f);
            Vector3 from = transform.position, to = from + Offset; var r0 = transform.rotation;
            var axis = Vector3.Cross(Vector3.up, Offset.normalized);
            for (float t = 0; t < 1f; t += Time.deltaTime / 1.4f)
            {
                float e = Mathf.SmoothStep(0, 1, t);
                transform.SetPositionAndRotation(Vector3.Lerp(from, to, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * .6f, Quaternion.AngleAxis(e * 160f, axis) * r0);
                yield return null;
            }
            transform.position = to; Free();
            Placeholders.Puff(to, new Color(.7f, .65f, .55f));
            mm.Cheer();
            Hud.Message("Muscle Man rolls it away like a pebble.", 3f);
            PlayLog.Line($"boulder {id} moved by Muscle Man");
            yield return new WaitForSeconds(.6f);
        }
    }

    // a portcullis (Medieval): Muscle Man heaves it up; it stays up
    public sealed class Portcullis : MuscleJob
    {
        public float lift = 3.4f;
        bool hinted; float hintAt = -10;
        protected override string Verb => "lifts the portcullis";
        protected override string Clip => "mm_lift";
        protected override float Reach => 1.2f;
        protected override Vector3 StandAt(Hero player) { var side = Vector3.Dot(player.transform.position - transform.position, transform.forward) >= 0 ? 1 : -1; return transform.position + transform.forward * side * 1.2f; }

        void Start() { if (IsDone) Opened(); }
        protected override void Opened() { transform.position += Vector3.up * lift; foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false; }

        void LateUpdate()
        {
            // before Muscle Man: a hint at the bars
            if (IsDone || SaveGame.Current.Has(Abilities.MuscleMan) || !Game.Playing || !Party.I) return;
            var d = Party.I.Player.transform.position - transform.position; d.y = 0;
            if (d.magnitude < 3f && Time.time > hintAt + 6f) { hintAt = Time.time; hinted = true; Hud.Message("An iron portcullis, rusted shut. Only someone enormously strong could lift it... (later)", 4f); }
            _ = hinted;
        }

        protected override IEnumerator Work(Hero player, Hero mm)
        {
            yield return new WaitForSeconds(.8f);
            Sfx.Play("portcullis", .7f);
            var from = transform.position;
            for (float t = 0; t < 1f; t += Time.deltaTime / 1.6f) { transform.position = from + Vector3.up * lift * Mathf.SmoothStep(0, 1, t); yield return null; }
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            mm.Cheer();
            Hud.Message("Muscle Man heaves the portcullis up... and wedges it there.", 3f);
            yield return new WaitForSeconds(.5f);
        }
    }

    // a boost spot under a high ledge: Muscle Man throws Dan or Abi up to `height` (the film: he lifts Dan to the clock)
    public sealed class BoostSpot : MuscleJob
    {
        public float height = 7f;
        public Vector3 landAt;                  // where the throw lands (zero: straight up)
        public int Boosts { get; private set; }
        protected override string Verb => "boosts you up";
        protected override string Clip => "mm_boost";
        protected override bool Allowed(Hero player) => player.IsPerson;
        protected override float Reach => .9f;
        protected override Vector3 StandAt(Hero player) => transform.position - transform.forward * .9f;

        protected override IEnumerator Work(Hero player, Hero mm)
        {
            player.Motor.Teleport(transform.position + Vector3.up * .05f, transform.rotation);
            yield return new WaitForSeconds(.45f);
            Sfx.Play("jump", .7f, .8f);
            if (landAt != Vector3.zero)
            {
                // the throw's arc: up to `height`, down onto landAt (the horizontal speed that gets there in that time)
                float g = player.Motor.Gravity, up = Mathf.Sqrt(2f * height / g), down = Mathf.Sqrt(2f * Mathf.Max(.1f, transform.position.y + height - landAt.y) / g);
                var d = landAt - transform.position; d.y = 0;
                player.Motor.Throw(d / (up + down), height);
            }
            else player.Motor.Launch(height);
            Boosts++;
            PlayLog.Line($"Muscle Man boosts {player.Stats.displayName} up {height} m at {transform.position}");
            yield return new WaitForSeconds(.4f);
        }
    }
}
