using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 12: Tomorrowland's pieces (World/Tomorrowland.cs places them).

    // ---- a boost ramp: robot Kira rolling onto it on her wheels (the wheel dash) is launched up `height` m, keeping her speed
    public sealed class BoostRamp : BoxVolume
    {
        public static readonly List<BoostRamp> All = new List<BoostRamp>();
        public float height = 6f; public int Launched;
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public static BoostRamp At(Vector3 p) { foreach (var v in All) if (v.Contains(p)) return v; return null; }
    }

    // ---- robot Kira's terminals take over machines (they are GearSockets with ability PlugIn; these are their machines)
    // a moving walkway turned round for good (it then carries you up its ramp to a roof)
    public sealed class FlipWalkway : WindUpMachine
    {
        public Conveyor walkway; public Vector3 flipped;
        protected override void Apply(bool instant)
        {
            if (walkway) walkway.dir = flipped;
            var m = walkway ? walkway.GetComponent<WalkwayLook>() : null; if (m) m.dir = flipped;
            if (!instant) { Sfx.Play("walkway_hum", .6f, 1.2f); Hud.Message("The walkway shudders... and runs the other way!", 3f); }
        }
    }

    // a flying car called down: it starts its route (a MovingPlatform path) between a street pad and a tower top, for good
    public sealed class CallCar : WindUpMachine
    {
        public MovingPlatform car; public float speed = 4f;
        protected override void Apply(bool instant)
        {
            if (car) car.speed = speed;
            if (!instant) { Sfx.Play("hovercar_whoosh", .7f); Hud.Message("A flying car answers the call: hop in for a ride up the towers!", 4f); }
        }
    }

    // a door that slides up into its frame and stays open
    public sealed class SlideDoor : WindUpMachine
    {
        public float lift = 3.4f;
        protected override void Apply(bool instant)
        {
            if (instant) { transform.position += Vector3.up * lift; Physics.SyncTransforms(); return; }
            Game.I.StartCoroutine(Open());
        }
        IEnumerator Open()
        {
            Sfx.Play("door_whoosh", .6f);
            var from = transform.position;
            for (float t = 0; t < 1f; t += Time.deltaTime / .9f) { transform.position = from + Vector3.up * lift * Mathf.SmoothStep(0, 1, t); yield return null; }
            Physics.SyncTransforms(); Hud.Message($"Whoosh: {what} slides open.", 3f);
        }
    }

    // the cleanbots' friendly switch: set off again any time (no id): every cleanbot is friendly for a while
    public sealed class FriendlySwitch : WindUpMachine
    {
        public float seconds = 25f;
        protected override void Apply(bool instant)
        {
            if (instant) return;
            Enemy.CleanbotsFriendlyUntil = Time.time + seconds; Enemy.FriendlySwitches++;
            Sfx.Play("robot_beep", .6f, 1.3f); Hud.Message($"Kira hacks the cleaning network: the cleanbots are friendly for {seconds:0} seconds!", 4f);
        }
    }

    // a moving walkway's look: stripes that slide the way it runs (the Conveyor carries)
    public sealed class WalkwayLook : MonoBehaviour
    {
        public Vector3 dir = Vector3.forward; public float speed = 2.4f; Material m; Vector2 off;
        void Start() { var r = GetComponent<Renderer>(); if (r) m = r.material; }
        void Update()
        {
            if (!m) return;
            var local = transform.InverseTransformDirection(dir); off += new Vector2(0, (local.z >= 0 ? -1 : 1) * speed * .25f * Time.deltaTime);
            if (m.HasProperty("_BaseMap")) m.SetTextureOffset("_BaseMap", off);
        }
    }

    // a malfunctioning walkway: it flips direction every `every` seconds (a hazard: it can carry you back)
    public sealed class Malfunction : MonoBehaviour
    {
        public Conveyor walkway; public float every = 5.5f; float t; public int Flips;
        void Update()
        {
            if (!walkway || !Game.Playing) return;
            t += Time.deltaTime; if (t < every) return; t = 0; Flips++;
            walkway.dir = -walkway.dir; var l = walkway.GetComponent<WalkwayLook>(); if (l) l.dir = walkway.dir;
            if (Party.I && Vector3.Distance(Party.I.Player.transform.position, transform.position) < 14f) { Sfx.Play("walkway_glitch", .5f); Placeholders.Puff(transform.position + Vector3.up * .4f, new Color(1f, .5f, .8f)); }
        }
    }

    // ---- a hover-car lane across the street: cars zip along it at knee height; in the way = bumped aside (a bump, never a lost heart)
    public sealed class HoverLane : MonoBehaviour
    {
        public Vector3 from, to; public float speed = 11f, gap = 7f; public readonly List<Transform> cars = new List<Transform>();
        public static int Bumps { get; private set; }
        float bumpAt = -10, whooshAt = -10;
        void Update()
        {
            if (!Game.Playing && !Game.InCutscene) return;
            var dir = (to - from); float len = dir.magnitude; dir /= len;
            for (int i = 0; i < cars.Count; i++)
            {
                float d = Mathf.Repeat(Time.time * speed + i * (len / cars.Count), len);
                var p = from + dir * d; cars[i].position = p + Vector3.up * (.5f + Mathf.Sin(Time.time * 3f + i) * .08f);
                cars[i].rotation = Quaternion.LookRotation(dir);
                if (!Party.I) continue;
                foreach (var h in Party.I.Everyone)
                {
                    if (!h) continue;
                    var o = h.transform.position - p; if (o.y > 1.8f || o.y < -.5f) continue; o.y = 0;
                    var side = o - dir * Vector3.Dot(o, dir);
                    if (Mathf.Abs(Vector3.Dot(o, dir)) < 2.1f && side.magnitude < 1.4f)
                    {
                        var push = side.sqrMagnitude > .01f ? side.normalized : Vector3.Cross(Vector3.up, dir);
                        h.Motor.Knockback(push * 7f + dir * 4f + Vector3.up * 4.5f);
                        if (h == Party.I.Player && Time.time > bumpAt + .8f) { bumpAt = Time.time; Bumps++; Sfx.Play("car_honk", .6f); Hud.Message("Honk! Mind the hover-car lane.", 2f); PlayLog.Line($"a hover car bumps {h.Stats.displayName}"); }
                    }
                }
                if (Party.I && Time.time > whooshAt + .6f && Vector3.Distance(p, Party.I.Player.transform.position) < 6f) { whooshAt = Time.time; Sfx.Play("hovercar_whoosh", .35f, Random.Range(.9f, 1.15f)); }
            }
        }
    }

    // ---- the greeter robot: rolls on its wheel round its spot, waves; says its line once when you come close; Muscle Man's hug gag
    public sealed class Greeter : MonoBehaviour
    {
        public static readonly List<Greeter> All = new List<Greeter>();
        public string line = "Your rocket is ready for departure!", sound = "greeter_ready";
        public Vector3 pointAt; public float roam = 2.5f; public bool hugGag = true;
        public int Said { get; private set; }
        public static int Hugs { get; private set; }
        RigidParts parts; Vector3 home; float saidAt = -100, hugAt = -100; bool hugging;
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        void Start() { home = transform.position; parts = GetComponentInChildren<RigidParts>(); }
        void Update()
        {
            if (!Party.I || hugging) return;
            float t = Time.time * .35f;
            var to = home + new Vector3(Mathf.Sin(t), 0, Mathf.Sin(t * 2f) * .5f) * roam;
            var d = to - transform.position; d.y = 0;
            if (parts) parts.roll = d.magnitude > .05f ? 260f : 0f;
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(to.x, transform.position.y, to.z), Time.deltaTime * .8f);
            var p = Party.I.Player.transform.position; var pd = p - transform.position; pd.y = 0;
            var look = pd.magnitude < 7f ? pd : (pointAt != Vector3.zero ? pointAt - transform.position : d); look.y = 0;
            if (look.sqrMagnitude > .01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(look), 120f * Time.deltaTime);
            if (parts) parts.wave = pd.magnitude < 6f ? 1f : 0f;
            if (!Game.Playing || Game.I.Busy) return;
            if (pd.magnitude < 4.5f && Time.time > saidAt + 20f) { saidAt = Time.time; Said++; Sfx.Play(sound, .8f); Hud.Message($"Greeter robot: \"{line}\"", 4f); PlayLog.Line($"greeter: {line}"); }
            var mm = Party.I.Companion;
            if (hugGag && mm && Time.time > hugAt + 40f && Vector3.Distance(mm.transform.position, transform.position) < 2.6f) { hugAt = Time.time; StartCoroutine(Hug(mm)); }
        }
        // Muscle Man is delighted by everything: he tries to hug the robot; it beeps in alarm and rolls off
        IEnumerator Hug(Hero mm)
        {
            hugging = true; Hugs++;
            PlayLog.Line("Muscle Man tries to hug the greeter robot");
            // Dan, 2026-11-12: Mixamo has no hug: his cheer opens his arms wide, he steps in, the robot beeps and rolls back, he hugs the air (offer)
            var away = transform.position - mm.transform.position; away.y = 0; away = away.sqrMagnitude > .01f ? away.normalized : transform.forward;
            bool follow = mm.Follower.enabled; mm.Follower.enabled = false;
            mm.transform.rotation = Quaternion.LookRotation(away);
            if (mm.Anim) mm.Anim.Trigger(mm.Anim.Has("mm_hug") ? "mm_hug" : "cheer");
            Sfx.Play("mm_laugh", .6f);
            if (Game.Playing) Hud.Message("Muscle Man: \"Aww, come here, little buddy!\"   Robot: \"BEEP BEEP! PERSONAL SPACE!\"", 3.5f);
            for (float t = 0; t < .6f; t += Time.deltaTime) { mm.Motor.Cc.Move(away * 1.4f * Time.deltaTime + Vector3.down * .05f); yield return null; }   // he steps in
            Sfx.Play("robot_alarm", .7f); if (parts) parts.wave = 3f;
            if (mm.Anim) mm.Anim.Trigger(mm.Anim.Has("offer") ? "offer" : "cheer", 1.8f);   // ... and hugs the air (Fix 13.1: offer is a one-shot now)
            for (float t = 0; t < 1.2f; t += Time.deltaTime) { transform.position += away * 3.2f * Time.deltaTime; if (parts) parts.roll = 900f; yield return null; }
            yield return new WaitForSeconds(.6f);
            mm.Follower.enabled = follow; if (follow) mm.Follower.ResetTimers();
            home = transform.position; hugging = false;
        }
    }

    // ---- the Skyline Tower's glass lift: dark until the necklace wakes (it carries you to the pink shard on the roof), then up and down
    public sealed class ShardLift : MonoBehaviour
    {
        public Vector3 bottom, top; public float period = 14f; public GameObject light;
        MovingPlatform mp; bool on;
        void Start() { mp = GetComponent<MovingPlatform>(); mp.Hold(bottom); }
        void Update()
        {
            if (!Game.World) return;
            var pr = SaveGame.Current.World(Game.World.worldId);
            bool want = pr.necklaceAwake || pr.storyShard;
            if (want == on) return;
            on = want; if (light) light.SetActive(on);
            if (on) { mp.a = bottom; mp.b = top; mp.period = period; PlayLog.Line("the Skyline Tower's lift powers up"); }
            else mp.Hold(bottom);
        }
        public bool On => on;
    }

    // ---- the fuel socket at the rocket's foot: with the pink shard in hand, walking up to it plays the launch
    public sealed class RocketReach : MonoBehaviour
    {
        public float radius = 3.2f; float hintAt = -10;
        void Update()
        {
            if (!Game.Playing || !Party.I || Game.I.Busy || !Game.InTomorrowland) return;
            var d = Party.I.Player.transform.position - transform.position; d.y = 0;
            if (d.magnitude > radius) return;
            var pr = SaveGame.Current.World(Game.World.worldId);
            if (pr.storyShard && !pr.opened.Contains(Tomorrowland.LaunchFlag)) { Game.I.StartCoroutine(Game.I.RocketFinale()); return; }
            if (!pr.storyShard && Time.time > hintAt + 8f) { hintAt = Time.time; Hud.Message("The rocket's fuel socket is empty. \"Fuel cell missing!\" (the pink shard)", 3.5f); }
        }
    }

    // ---- neon: a sign or strip that flickers a little now and then
    public sealed class NeonFlicker : MonoBehaviour
    {
        Renderer r; float next; void Start() { r = GetComponent<Renderer>(); next = Time.time + Random.Range(2f, 9f); }
        void Update() { if (!r || Time.time < next) return; r.enabled = !r.enabled; next = Time.time + (r.enabled ? Random.Range(3f, 12f) : Random.Range(.05f, .15f)); }
    }
}
