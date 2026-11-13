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
        protected virtual string Fallback => "mm_lift";
        public static string LastClip = "";
        protected virtual bool Allowed(Hero player) => true;
        protected virtual Vector3 StandAt(Hero player) { var d = player.transform.position - transform.position; d.y = 0; return transform.position + (d.sqrMagnitude > .01f ? d.normalized : -transform.forward) * (Reach + .6f); }
        protected virtual float Reach => 2.2f;
        protected virtual bool StandBeside => false;
        public static string LastStand = "";
        Vector3 Beside(Hero player, Vector3 front)
        {
            var mm = Party.I.Companion; var cam = CameraRig.I ? CameraRig.I.Cam : Camera.main;
            var c = transform.position; var d = player.transform.position - c; d.y = 0; d = d.sqrMagnitude > .01f ? d.normalized : -transform.forward;
            var side = Vector3.Cross(Vector3.up, d);
            float r = Reach + .6f;
            var tries = new[] { (c + side * r + d * .4f, "right side"), (c - side * r + d * .4f, "left side"), (c + side * r, "right"), (c - side * r, "left"),
                (front + side * .95f, "in front, to the right"), (front - side * .95f, "in front, to the left"), (front + side * .6f + d * -.5f, "close in, to the right") };
            float camMin = mm ? mm.Stats.height * .5f + 2.2f : 3f;
            for (int pass = 0; pass < 2; pass++, camMin = 1.4f)   // (a narrow place: a second pass lets him nearer the camera)
            foreach (var (p0, label) in tries)
            {
                var p = p0;
                if (Physics.Raycast(new Vector3(p.x, player.transform.position.y + 1.2f, p.z), Vector3.down, out var h, 3.5f, ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << Hero.EnemyLayer) | (1 << 2) | (1 << 4)), QueryTriggerInteraction.Ignore) && !h.collider.transform.IsChildOf(transform)) p.y = h.point.y;
                else continue;
                if (Mathf.Abs(p.y - player.transform.position.y) > 1.0f || (mm && !Party.Fits(mm, p + Vector3.up * .05f))) continue;
                if (cam)
                {
                    if (Vector3.Distance(cam.transform.position, p + Vector3.up * 1.1f) < camMin) continue;
                    var vp = cam.WorldToViewportPoint(p + Vector3.up * 1.1f); if (vp.z < .2f || vp.x < .05f || vp.x > .95f) continue;
                }
                LastStand = label; return p;
            }
            LastStand = "in front (no free side)"; return front;
        }
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

        // Round 12 item 0b: a job is offered only when its object is there to be seen: the player within reach of the object's own
        // surface (not its centre: the Egypt quarry's 12 m block offered itself from inside the walled quarry with the camera turned
        // away), on the same level, the object on screen and nothing solid between the camera and it, and not behind the player's
        // back. Once done (also after a reload: the save's opened list) it is never offered again.
        protected virtual float SurfaceReach => 2.6f;
        protected virtual bool UsesCentre => false;            // a boost spot is a place, not a thing: its centre and radius
        protected virtual Vector3 SeeAt => transform.position;  // what has to be on screen for a place (a boost spot: where it lands you)
        protected virtual Vector3 FaceTarget => transform.position + transform.forward * 3f;   // what a place's user faces (a boost spot: its ledge)

        public Vector3 ClosestPoint(Vector3 pos)
        {
            Vector3 best = transform.position; float bd = float.MaxValue;
            foreach (var c in GetComponentsInChildren<Collider>())
            {
                if (!c.enabled || c.isTrigger) continue;
                var q = (c is MeshCollider mc && !mc.convex) ? c.bounds.ClosestPoint(pos) : c.ClosestPoint(pos);
                float d = (q - pos).sqrMagnitude; if (d < bd) { bd = d; best = q; }
            }
            if (bd == float.MaxValue)
                foreach (var r in GetComponentsInChildren<Renderer>())
                {
                    if (!r.enabled) continue;
                    var q = r.bounds.ClosestPoint(pos); float d = (q - pos).sqrMagnitude; if (d < bd) { bd = d; best = q; }
                }
            return best;
        }

        // why it is or isn't offered (null = offered; the check logs the reason)
        public string WhyNot(Hero player)
        {
            if (!this || !isActiveAndEnabled) return "inactive";
            if (IsDone) return "done";
            if (!Allowed(player)) return "not for this hero";
            var pos = player.transform.position;
            Vector3 at;
            if (UsesCentre)
            {
                var d = pos - transform.position; if (Mathf.Abs(d.y) > 1.6f) return "another level"; d.y *= .5f;
                if (d.magnitude > radius) return $"too far ({d.magnitude:0.0} m from the spot)";
                at = SeeAt;
                // a boost spot's thing is the ledge it throws you to: not with your back to it
                var face = FaceTarget - pos; face.y = 0; var pf = player.transform.forward; pf.y = 0;
                if (face.sqrMagnitude > .25f && Vector3.Dot(pf.normalized, face.normalized) < -.2f) return "facing away from the ledge";
            }
            else
            {
                at = ClosestPoint(pos + Vector3.up * .9f);
                var d = at - pos; float dy = d.y; d.y = 0;
                if (d.magnitude > SurfaceReach) return $"too far ({d.magnitude:0.0} m from its surface)";
                if (dy < -1.6f || dy > 2.6f) return "another level";
                // not behind the player's back
                var fwd = player.transform.forward; fwd.y = 0;
                if (d.sqrMagnitude > .25f && Vector3.Dot(fwd.normalized, d.normalized) < -.35f) return "behind the player";
            }
            var cam = CameraRig.I ? CameraRig.I.Cam : Camera.main;
            if (cam)
            {
                // on screen and seen: any of a few points of it (its nearest surface, its middle, its top) in view with a clear line from the
                // camera (a hit within 0.7 m of the point itself is its own frame or trim: still seen)
                string why = "off screen";
                var pts = new List<Vector3> { at };
                var rs = GetComponentsInChildren<Renderer>(); if (rs.Length > 0) { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); pts.Add(b.center); pts.Add(new Vector3(b.center.x, b.max.y - .1f, b.center.z)); }
                foreach (var pt in pts)
                {
                    var vp = cam.WorldToViewportPoint(pt);
                    if (vp.z < .2f || vp.x < .03f || vp.x > .97f || vp.y < .03f || vp.y > .97f) continue;
                    var from = cam.transform.position; var dir = pt - from; float len = dir.magnitude;
                    string block = null;
                    foreach (var h in Physics.RaycastAll(from, dir / len, len, ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << Hero.EnemyLayer) | (1 << 2) | (1 << 4)), QueryTriggerInteraction.Ignore))
                        if (!h.collider.transform.IsChildOf(transform) && len - h.distance > .7f) { block = h.collider.name; break; }
                    if (block == null) return null;
                    why = $"hidden behind {block}";
                }
                return why;
            }
            return null;
        }
        public bool Offered(Hero player) => WhyNot(player) == null;
        float Score(Hero player) => UsesCentre ? (player.transform.position - transform.position).magnitude : (ClosestPoint(player.transform.position + Vector3.up * .9f) - player.transform.position).magnitude;

        public static MuscleJob Near(Hero player)
        {
            MuscleJob best = null; float bd = float.MaxValue;
            foreach (var j in All)
            {
                if (!j || !j.Offered(player)) continue;
                float m = j.Score(player);
                if (m < bd) { bd = m; best = j; }
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

        public string Prompt => $"{Controls.Btn(Controls.Act.Attack)}: Muscle Man {Verb}";
        public static int PromptsShown { get; private set; }
        public static MuscleJob Prompting { get; private set; }   // the job whose prompt is on screen now (checks)

        void Update()
        {
            if (Prompting == this && (!Ready || !Party.I.Player || !Offered(Party.I.Player))) { Hud.ClearMessage(Prompt); Prompting = null; hintAt = -10f; }
            if (!Ready || !Party.I.Player || IsDone || Time.time < hintAt + 3f) return;
            var p = Party.I.Player;
            if (Near(p) == this) { hintAt = Time.time; Prompting = this; PromptsShown++; Hud.Message(Prompt, 3.5f); }
        }

        IEnumerator Run(Hero player)
        {
            var mm = Party.I.Companion;
            Hud.ClearMessage(Prompt); if (Prompting == this) Prompting = null;
            Game.I.SetBusy(true); Jobs++; Done++;
            PlayLog.Line($"Muscle Man's job: {GetType().Name} {id} ({Verb})");
            mm.Follower.enabled = false; mm.Working = true;
            // Fix 13.2: his spot on the player's own level (a ship's deck, a roof): the floor under it from just above the player's feet.
            // A boulder-like job: beside the object where he fits, in view and not right by the camera (between the player and the object
            // the close camera hid him as a too-near follower: in the ship's hold nobody saw him lift the crates)
            var at = StandAt(player);
            if (StandBeside) at = Beside(player, at);
            if (Physics.Raycast(new Vector3(at.x, player.transform.position.y + 1.2f, at.z), Vector3.down, out var fh, 3.5f, ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << Hero.EnemyLayer) | (1 << 2) | (1 << 4)), QueryTriggerInteraction.Ignore) && !fh.collider.transform.IsChildOf(transform)) at.y = fh.point.y;
            else at.y = Game.OnGround(at.x, at.z).y;
            var look = transform.position - at; look.y = 0;
            mm.Motor.Teleport(at + Vector3.up * .05f, Quaternion.LookRotation(look.sqrMagnitude > .01f ? look : Vector3.forward));
            Placeholders.Puff(at + Vector3.up * .5f, new Color(.9f, .85f, .7f));
            // Fix 13.2 item 3: his clip is held for the whole job (it was a one-shot cancelled at once: the teleport left him "in the air",
            // so he played his fall loop, arms flailing like a swimmer; the bend job's loop was skipped altogether)
            string clip = mm.Anim && mm.Anim.Has(Clip) ? Clip : mm.Anim && mm.Anim.Has(Fallback) ? Fallback : null;
            if (clip != null) mm.Anim.Hold(clip, 12f);
            LastClip = clip ?? "(none)"; PlayLog.Line($"Muscle Man's job clip: {LastClip}; he stands {(StandBeside ? LastStand : "at his spot")} {at}");
            Sfx.Play("mm_grunt", .7f, Random.Range(.92f, 1.05f));
            yield return Work(player, mm);
            if (mm.Anim) mm.Anim.Release();
            if (!string.IsNullOrEmpty(id)) { var prog = SaveGame.Current.World(Game.World.worldId); if (!prog.opened.Contains(id)) prog.opened.Add(id); SaveGame.Save("Muscle Man: " + id); }
            mm.Follower.enabled = true; mm.Follower.ResetTimers(); mm.Working = false;
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
        public string verbText;                  // Round 10: "rolls the cannon aside", "lifts the crates aside", "lifts the anchor away"
        public bool lifts;                       // Round 10: lifted and set down (crates, a cannon, the anchor), not rolled
        public GameObject[] goneWhenMoved, shownWhenMoved;   // Fix 13.2: what stays at the old spot (the car's hatch cover) goes; what it uncovers shows
        protected override string Verb => string.IsNullOrEmpty(verbText) ? "rolls the boulder away" : verbText;
        protected override string Clip => lifts ? "mm_lift" : "mm_push";   // Fix 13.2: Lifting Object / Pushing
        protected override float Reach => Mathf.Max(1.8f, Mathf.Max(transform.lossyScale.x, transform.lossyScale.z) * .5f);
        protected override bool StandBeside => true;
        public static event System.Action<Boulder, Hero> Tried;   // someone pushed it without Muscle Man (the rival laughs)

        void Start() { if (IsDone) Opened(); else Uncover(false); }
        protected override void Opened() { transform.position += Offset; Free(); Uncover(true); }
        void Uncover(bool moved)
        {
            if (goneWhenMoved != null) foreach (var g in goneWhenMoved) if (g) g.SetActive(!moved);
            if (shownWhenMoved != null) foreach (var g in shownWhenMoved) if (g) g.SetActive(moved);
        }
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
                transform.SetPositionAndRotation(Vector3.Lerp(from, to, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * (lifts ? 1.8f : .6f), lifts ? r0 : Quaternion.AngleAxis(e * 160f, axis) * r0);
                yield return null;
            }
            transform.position = to; Free(); Uncover(true);
            Placeholders.Puff(to, new Color(.7f, .65f, .55f));
            mm.Cheer();
            Hud.Message(lifts ? "Muscle Man lifts it like a feather." : "Muscle Man rolls it away like a pebble.", 3f);
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
        protected override string Clip => "mm_throw";   // Fix 13.2: the Overhead Squat's drive up: arms up under the bars
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
        protected override bool UsesCentre => true;
        protected override Vector3 SeeAt => transform.position + Vector3.up * .3f;   // the spot where you stand (marked on the ground)
        protected override Vector3 FaceTarget => landAt != Vector3.zero ? landAt : transform.position + transform.forward * 3f;
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
