using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 11: Victorian Steampunk's pieces (World/Steampunk.cs places them).
    //  Conveyor: a belt; what stands on it is carried (CharacterMotor reads Conveyor.At under the feet).
    //  Stamper: a hammer head going up and down; caught under it = bonked back along its belt or lane (no heart lost).
    //  SteamJet: a vent that puffs now and then: a hop for whoever stands in it; a worker gets it in the face and reels.
    //  OilPatch: workers slip on it.
    //  GearSocket + WindUpMachine (WindLift, WindBridge, WindGearDoor, WindTurntable): mechanical Kira stands on the brass plate and winds
    //    the machine; it stays wound (saved by id).
    //  MagnetPillar: mechanical Kira, Jump at its foot: up the rivet strip to the girder on top (the motor's ClimbOut).
    //  BendJob: Muscle Man bends bars or pipes aside (the straight ones swap for bent ones).
    //  Carriage: a brass steam carriage on its rail loop (a MovingPlatform: ride the roof); walking into its way = a bump, not a hurt.
    //  ClockReach: at the great clock's foot with the necklace awake: the finale (Game.ClockFinale).
    //  HorseGag: the first time Muscle Man comes by the clockwork horse, he pets it and it puffs steam in his face.
    public sealed class Conveyor : BoxVolume
    {
        public static readonly List<Conveyor> All = new List<Conveyor>();
        public Vector3 dir = Vector3.forward; public float speed = 2.4f;
        public Vector3 Push => dir * speed;
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public static Conveyor At(Vector3 p) { foreach (var v in All) if (v.speed > .01f && v.Contains(p)) return v; return null; }
    }

    public sealed class OilPatch : BoxVolume
    {
        public static readonly List<OilPatch> All = new List<OilPatch>();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public static OilPatch At(Vector3 p) { foreach (var v in All) if (v.Contains(p)) return v; return null; }
    }

    public sealed class Stamper : MonoBehaviour
    {
        public static readonly List<Stamper> All = new List<Stamper>();
        public Transform head; public float top = 2.2f, period = 2.6f, phase, downFor = .55f;
        public Vector3 bonkDir = Vector3.back; public Vector2 footprint = new Vector2(1.9f, 1.5f);
        public bool loud = true;
        public int Bonks { get; private set; }
        public static int BonksAll { get; private set; }
        Vector3 rest; float lastSound = -10;
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        void Start() { if (head) rest = head.localPosition; }
        // 0 = down on the belt, 1 = up
        public float Lift
        {
            get
            {
                float t = Mathf.Repeat(Time.time + phase, period);
                if (t < downFor) return 0f;                                 // down
                if (t < downFor + .5f) return Mathf.SmoothStep(0, 1, (t - downFor) / .5f);   // rising
                float fallAt = period - .18f;
                if (t > fallAt) return 1f - (t - fallAt) / .18f;            // slamming down
                return 1f;
            }
        }
        void Update()
        {
            if (!head) return;
            float l = Lift;
            head.localPosition = rest + Vector3.up * top * l;
            float t = Mathf.Repeat(Time.time + phase, period);
            if (loud && t < .05f && Time.time > lastSound + .5f && Party.I && Vector3.Distance(Party.I.Player.transform.position, transform.position) < 25f) { lastSound = Time.time; Sfx.Play("stamper", .35f, Random.Range(.9f, 1.1f)); }
            if (l > .35f || !Party.I) return;
            foreach (var h in Party.I.Heroes)
            {
                if (!h || !h.gameObject.activeInHierarchy) continue;
                var local = transform.InverseTransformPoint(h.transform.position);
                if (Mathf.Abs(local.x) > footprint.x * .5f + h.Stats.radius || Mathf.Abs(local.z) > footprint.y * .5f + h.Stats.radius || local.y > 2.2f || local.y < -.6f) continue;
                if (!h.IsPlayer) { h.Motor.Teleport(h.transform.position + bonkDir.normalized * (footprint.y + .8f), h.transform.rotation); continue; }
                // out from under the head the way it bonks (the belt's: forward, never back), with a little hop
                var fwd = bonkDir.normalized; float along = Vector3.Dot(h.transform.position - transform.position, fwd);
                if (along < footprint.y * .5f + h.Stats.radius) h.Motor.Teleport(h.transform.position + fwd * (footprint.y * .5f + h.Stats.radius + .15f - along) + Vector3.up * .05f, h.transform.rotation);
                h.Motor.Knockback(fwd * 3.5f + Vector3.up * 3f);
                Bonks++; BonksAll++;
                Sfx.Play("hurt", .35f, 1.3f); Hud.Message("Bonk! (the stamper)", 1.2f);
                PlayLog.Line($"a stamper bonks {h.Stats.displayName} back at {h.transform.position}");
            }
        }
    }

    public sealed class SteamJet : MonoBehaviour
    {
        public static readonly List<SteamJet> All = new List<SteamJet>();
        public float period = 3.2f, phase, blowFor = .9f, radius = .8f, height = 2.6f, hop = 1.4f;
        public bool hurtsNobody = true;
        public static int HopsAll { get; private set; }
        float lastPuff = -10; readonly HashSet<CharacterMotor> hopped = new HashSet<CharacterMotor>();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public bool BlowingNow => Mathf.Repeat(Time.time + phase, period) < blowFor;
        public static bool Blowing(Vector3 p)
        {
            foreach (var j in All) { if (!j || !j.BlowingNow) continue; var d = p - j.transform.position; if (d.y < -.3f || d.y > j.height) continue; d.y = 0; if (d.magnitude < j.radius + .3f) return true; }
            return false;
        }
        void Update()
        {
            bool on = BlowingNow;
            if (!on) { hopped.Clear(); return; }
            if (Time.time > lastPuff + .25f) { lastPuff = Time.time; Placeholders.Puff(transform.position + Vector3.up * (.4f + Random.value * 1.4f), new Color(.92f, .94f, .98f)); }
            if (Mathf.Repeat(Time.time + phase, period) < Time.deltaTime * 1.5f && Party.I && Vector3.Distance(Party.I.Player.transform.position, transform.position) < 20f) Sfx.Play("steam_hiss", .3f, Random.Range(.9f, 1.15f));
            if (!Party.I) return;
            foreach (var h in Party.I.Heroes)
            {
                if (!h || hopped.Contains(h.Motor)) continue;
                var d = h.transform.position - transform.position; if (d.y < -.4f || d.y > height) continue; d.y = 0;
                if (d.magnitude > radius + h.Stats.radius) continue;
                hopped.Add(h.Motor); h.Motor.Launch(hop); HopsAll++;
            }
        }
    }

    // ---- mechanical Kira's wind-up
    public abstract class WindUpMachine : MonoBehaviour
    {
        public static readonly List<WindUpMachine> All = new List<WindUpMachine>();
        public string id;
        public string what = "the machine";
        public bool Powered => !string.IsNullOrEmpty(id) && Game.World && SaveGame.Current.World(Game.World.worldId).opened.Contains(id);
        public static int Winds { get; private set; }
        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);
        protected virtual void Start() { if (Powered) Apply(true); }
        public void Wind()
        {
            if (Powered) return;
            var prog = SaveGame.Current.World(Game.World.worldId); prog.opened.Add(id); Winds++;
            PlayLog.Line($"mechanical Kira winds up {what} ({id})");
            Sfx.Play("gears_tick", .55f);
            Apply(false);
            SaveGame.Save("wound up " + id);
        }
        public void OpenNow() { if (Powered) return; SaveGame.Current.World(Game.World.worldId).opened.Add(id); Apply(true); }   // (checks)
        protected abstract void Apply(bool instant);
    }

    public sealed class GearSocket : MonoBehaviour
    {
        public static readonly List<GearSocket> All = new List<GearSocket>();
        public WindUpMachine machine; public float radius = .95f;
        float on, hintAt = -10; Transform plate;
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        void Start() { plate = transform.childCount > 0 ? transform.GetChild(0) : null; }
        void Update()
        {
            if (!machine || !Game.Playing || !Party.I || Game.I.Busy) return;
            var p = Party.I.Player; var d = p.transform.position - transform.position; float dy = d.y; d.y = 0;
            bool near = d.magnitude < radius && Mathf.Abs(dy) < .7f;
            if (machine.Powered) { if (plate) plate.Rotate(0, 90f * Time.deltaTime, 0, Space.World); return; }
            bool kira = p.HasAbility(Abilities.WindUp);
            if (!near) { on = 0; if (d.magnitude < 3f && Time.time > hintAt + 6f && !kira) { hintAt = Time.time; Hud.Message("A brass gear socket. Something small and clockwork could wind it... (mechanical Kira)", 3.5f); } return; }
            if (!kira) return;
            if (on == 0) { Sfx.Play("windup", .6f); Hud.Message($"Kira winds up {machine.what}...", 2f); if (p.Anim) p.Anim.Trigger("attack"); }
            on += Time.deltaTime;
            if (plate) plate.Rotate(0, 400f * Time.deltaTime, 0, Space.World);
            if (on > .9f) { machine.Wind(); on = 0; }
        }
    }

    // a lift: wound up, it rides up and down for good (a MovingPlatform between its two stops)
    public sealed class WindLift : WindUpMachine
    {
        public Vector3 bottom, topAt; public float period = 12f;
        protected override void Apply(bool instant)
        {
            var mp = GetComponent<MovingPlatform>(); if (!mp) return;
            mp.a = bottom; mp.b = topAt; mp.period = period;
            if (!instant) Hud.Message("The lift whirs into life: up and down, up and down.", 3f);
        }
    }

    // a catwalk bridge hinged at one end, stowed upright: wound up, it swings down level and stays
    public sealed class WindBridge : WindUpMachine
    {
        public Quaternion upRot, downRot;
        protected override void Apply(bool instant)
        {
            if (instant) { transform.rotation = downRot; Physics.SyncTransforms(); return; }
            Game.I.StartCoroutine(Lower());
        }
        IEnumerator Lower()
        {
            Sfx.Play("chain_line", .5f);
            for (float t = 0; t < 1f; t += Time.deltaTime / 2.2f) { transform.rotation = Quaternion.Slerp(upRot, downRot, Mathf.SmoothStep(0, 1, t)); yield return null; }
            transform.rotation = downRot; Physics.SyncTransforms();
            Hud.Message("Clank! The catwalk bridge swings down to the gantry.", 3f);
        }
    }

    // a round gear door (or the great gear gate): wound up, it rolls aside along its own right and stays open
    public sealed class WindGearDoor : WindUpMachine
    {
        public float roll = 4.4f, radius = 2.1f;
        protected override void Apply(bool instant)
        {
            if (instant) { transform.position += transform.right * roll; Free(); return; }
            Game.I.StartCoroutine(Roll());
        }
        void Free() { foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false; }
        IEnumerator Roll()
        {
            var from = transform.position; var r0 = transform.rotation; var to = from + transform.right * roll;
            float turn = roll / (2 * Mathf.PI * radius) * 360f;
            for (float t = 0; t < 1f; t += Time.deltaTime / 2f)
            {
                float e = Mathf.SmoothStep(0, 1, t);
                transform.SetPositionAndRotation(Vector3.Lerp(from, to, e), Quaternion.AngleAxis(-turn * e, r0 * Vector3.forward) * r0);
                yield return null;
            }
            transform.position = to; Free();
            Hud.Message("The gear rolls aside: the way is open.", 3f);
        }
    }

    // the carriage turntable: wound up, it turns for good (a MovingPlatform that spins)
    public sealed class WindTurntable : WindUpMachine
    {
        public float spin = 14f;
        protected override void Apply(bool instant)
        {
            var mp = GetComponent<MovingPlatform>(); if (mp) mp.spinDegPerSec = spin;
            if (!instant) Hud.Message("The turntable creaks round and round.", 3f);
        }
    }

    // ---- mechanical Kira's magnet paws: Jump at the riveted pillar's foot = up it to the girder
    public sealed class MagnetPillar : MonoBehaviour
    {
        public static readonly List<MagnetPillar> All = new List<MagnetPillar>();
        public float radius = 1.3f; public Vector3 to;
        public static int Climbs { get; private set; }
        float hintAt = -10;
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public static bool Try(Hero h, bool jumpPressed)
        {
            if (!jumpPressed || !h.Motor.Grounded || h.Motor.Climbing) return false;
            foreach (var c in All)
            {
                if (!c || !c.isActiveAndEnabled) continue;
                var d = h.transform.position - c.transform.position; float dy = d.y; d.y = 0;
                if (d.magnitude > c.radius || Mathf.Abs(dy) > .8f) continue;
                if (!h.HasAbility(Abilities.MagnetPaws))
                {
                    if (Time.time > c.hintAt + 5f) { c.hintAt = Time.time; Hud.Message("An iron pillar with a strip of rivets. Little magnet paws would stick to it... (mechanical Kira)", 3.5f); }
                    return false;
                }
                Climbs++; PlayLog.Line($"{h.Stats.displayName} climbs the riveted pillar ({c.name}) to {c.to}");
                Sfx.Play("magnet_clank", .6f, Random.Range(.95f, 1.05f));
                h.Motor.ClimbOut(c.to, true);
                return true;
            }
            return false;
        }
    }

    // ---- Muscle Man bends bars or pipes aside
    public sealed class BendJob : MuscleJob
    {
        public GameObject straight, bent; public string verbText = "bends the bars aside";
        float hintAt = -10;
        protected override string Verb => verbText;
        protected override string Clip => "mm_heave";
        protected override float Reach => 1.4f;
        protected override Vector3 StandAt(Hero player) { var side = Vector3.Dot(player.transform.position - transform.position, -transform.forward) >= 0 ? -1 : 1; return transform.position + transform.forward * side * 1.4f; }
        void Start() { if (IsDone) Opened(); else if (bent) bent.SetActive(false); }
        protected override void Opened() { if (straight) straight.SetActive(false); if (bent) bent.SetActive(true); }
        void LateUpdate()
        {
            if (IsDone || SaveGame.Current.Has(Abilities.MuscleMan) || !Game.Playing || !Party.I) return;
            var d = Party.I.Player.transform.position - transform.position; d.y = 0;
            if (d.magnitude < 2.6f && Time.time > hintAt + 6f) { hintAt = Time.time; Hud.Message("Thick iron, far too strong to bend... for most people.", 3.5f); }
        }
        protected override IEnumerator Work(Hero player, Hero mm)
        {
            yield return new WaitForSeconds(.7f);
            Sfx.Play("bars_bend", .7f);
            yield return new WaitForSeconds(1.0f);
            Opened(); Placeholders.Puff(transform.position + Vector3.up * 1.2f, new Color(.6f, .55f, .5f));
            mm.Cheer();
            Hud.Message("Muscle Man bends them wide like warm toffee.", 3f);
            yield return new WaitForSeconds(.5f);
        }
    }

    // ---- a brass steam carriage on its rail loop: ride its roof (MovingPlatform); in its way = bumped aside
    public sealed class Carriage : MonoBehaviour
    {
        public static readonly List<Carriage> All = new List<Carriage>();
        public Vector3 size = new Vector3(2.2f, 2.8f, 4.0f);
        public static int Bumps { get; private set; }
        float bumpAt = -10, rollAt = -10;
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        void Update()
        {
            if (!Party.I || !Game.Playing) return;
            var p = Party.I.Player;
            var local = transform.InverseTransformPoint(p.transform.position);
            if (Time.time > rollAt + 3.2f && local.magnitude < 18f) { rollAt = Time.time; Sfx.Play("carriage_roll", .25f, Random.Range(.95f, 1.05f)); }
            // the body (not the roof): from the ground up to 2.4 m, its box with a little margin
            if (Mathf.Abs(local.x) < size.x * .5f + .35f && Mathf.Abs(local.z) < size.z * .5f + .35f && local.y < size.y - .5f && local.y > -.5f && Time.time > bumpAt + 1f)
            {
                bumpAt = Time.time; Bumps++;
                var side = transform.right * Mathf.Sign(local.x == 0 ? 1 : local.x) + transform.forward * .5f;
                p.Motor.Knockback(side.normalized * 7f + Vector3.up * 4.5f);
                Sfx.Play("hurt", .3f, 1.4f); Hud.Message("Bump! Mind the carriages.", 1.5f);
                PlayLog.Line($"a carriage bumps {p.Stats.displayName} aside at {p.transform.position}");
            }
        }
    }

    // ---- the great clock's foot: the finale once the necklace is awake
    public sealed class ClockReach : MonoBehaviour
    {
        public float radius = 4.2f; float hintAt = -10;
        void Update()
        {
            if (!Game.Playing || !Party.I || Game.I.Busy) return;
            var d = Party.I.Player.transform.position - transform.position; d.y = 0; if (d.magnitude > radius) return;
            var prog = SaveGame.Current.World(Game.World.worldId);
            if (prog.storyShard) return;
            if (!prog.necklaceAwake) { if (Time.time > hintAt + 8f) { hintAt = Time.time; Hud.Message("The great clock. Something teal glints in the middle of its face... far too high up. (Dan's necklace is still asleep)", 4f); } return; }
            Game.I.StartCoroutine(Game.I.ClockFinale());
        }
    }

    // ---- the comic beat: Muscle Man pets the clockwork horse; it puffs steam in his face
    public sealed class HorseGag : MonoBehaviour
    {
        public const string Flag = "SP-horse-gag";
        public static int Played { get; private set; }
        public Transform horse; public CreatureAnimator anim;
        bool running;
        void Update()
        {
            if (running || !Game.Playing || !Party.I || !Party.I.Companion || Game.I.Busy || !SaveGame.Current.Has(Abilities.MuscleMan)) return;
            var prog = SaveGame.Current.World(Game.World.worldId); if (prog.opened.Contains(Flag)) return;
            var d = Party.I.Player.transform.position - transform.position; d.y = 0; if (d.magnitude > 5f) return;
            Game.I.StartCoroutine(Run(prog));
        }
        IEnumerator Run(WorldProgress prog)
        {
            running = true; Game.I.SetBusy(true); Played++;
            var mm = Party.I.Companion; mm.Follower.enabled = false;
            var at = transform.position + transform.right * 1.6f; at.y = Game.OnGround(at.x, at.z).y;
            var look = transform.position - at; look.y = 0;
            mm.Motor.Teleport(at + Vector3.up * .05f, Quaternion.LookRotation(look));
            Hud.Message("Muscle Man: \"Who's a good shiny horsey?\"", 2.5f);
            if (mm.Anim) mm.Anim.Trigger(mm.Anim.Has("offer") ? "offer" : "attack");
            yield return new WaitForSeconds(1.3f);
            Sfx.Play("horse_puff", .7f);
            for (int i = 0; i < 6; i++) Placeholders.Puff(Vector3.Lerp(horse ? horse.position + horse.forward * 1.2f + Vector3.up * 1.7f : at, at + Vector3.up * 1.8f, i / 5f), new Color(.95f, .96f, 1f));
            if (mm.Anim) mm.Anim.Trigger(mm.Anim.Has("dazed") ? "dazed" : "hurt");
            yield return new WaitForSeconds(.4f);
            Hud.Message("PFFSSHH! The clockwork horse puffs steam right in Muscle Man's face. He coughs... and laughs.", 4f);
            yield return new WaitForSeconds(1.6f);
            Sfx.Play("mm_laugh", .6f);
            if (mm.Anim) mm.Anim.Trigger(mm.Anim.Has("mm_cheer") ? "mm_cheer" : "cheer");
            yield return new WaitForSeconds(1.2f);
            prog.opened.Add(Flag); PlayLog.Line("the horse gag: steam in Muscle Man's face");
            mm.Follower.enabled = true; mm.Follower.ResetTimers();
            Game.I.SetBusy(false); running = false;
        }
    }

    // ---- a clockwork horse walking its loop (the Medieval pony's rig, tinted copper, until its own model is in)
    public sealed class HorseWalk : MonoBehaviour
    {
        public Vector3[] path; public float speed = 1.4f; float along;
        public CreatureAnimator anim; public bool still;
        void Start() { if (anim) anim.Drive = () => (still ? "idle" : "walk", still ? 0f : speed); }
        void Update()
        {
            if (still || path == null || path.Length < 2) return;
            along += speed * Time.deltaTime;
            float total = 0; for (int i = 0; i < path.Length; i++) total += Vector3.Distance(path[i], path[(i + 1) % path.Length]);
            float d = Mathf.Repeat(along, total);
            for (int i = 0; i < path.Length; i++)
            {
                var p0 = path[i]; var p1 = path[(i + 1) % path.Length]; float l = Vector3.Distance(p0, p1);
                if (d <= l) { transform.position = Vector3.Lerp(p0, p1, d / l); var dir = p1 - p0; dir.y = 0; transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), 60f * Time.deltaTime); break; }
                d -= l;
            }
        }
    }

    // a big decorative gear turning on a wall
    public sealed class Spin : MonoBehaviour
    {
        public Vector3 axis = Vector3.forward; public float speed = 20f;
        void Update() => transform.Rotate(axis, speed * Time.deltaTime, Space.Self);
    }
}
