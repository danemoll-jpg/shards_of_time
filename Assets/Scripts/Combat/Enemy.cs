using System.Collections.Generic;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 1, item 8: the placeholder enemy. Patrols between two points, chases the player when close (and not too far from home),
    // touching it hurts, one or two hits defeat it (Dan's punch: one; Abi's kick and Kira's claws: two). Drops a heal heart.
    // Round 4, item 7: kinds. Blob (the playground's), Critter (the Ice Age's snow critter: patrols, bumps into you) and Wolf (spots
    // the player, circles at a distance, then lunges; only one wolf lunges at a time). Dan's ground pound stuns any of them (Stun).
    // Placeholder shapes until their models exist (Docs/MODELS-NEEDED.md #7, #8).
    [RequireComponent(typeof(CharacterController))]
    public sealed class Enemy : MonoBehaviour
    {
        public enum Kind { Blob, Critter, Wolf }
        public static readonly List<Enemy> All = new List<Enemy>();
        public Kind kind = Kind.Blob;
        public Vector3 a, b; public int hp = 2;
        public float patrolSpeed = 2.2f, chaseSpeed = 4.2f, sight = 7f, leash = 14f;
        public bool Dead { get; private set; }
        public bool Chasing { get; private set; }
        public int HitsTaken { get; private set; }
        public int Lunges { get; private set; }
        public float StunnedFor => stun;
        public string WolfState => wolf.ToString();
        CharacterController cc; Renderer[] rends; Color baseColor; float flash, stun, vy; bool toB = true; Vector3 home;
        public CreatureAnimator Anim;   // Round 5: the creature's model (null: placeholder shapes)
        float speedNow;
        enum W { Prowl, Circle, Lunge, Recover }
        W wolf = W.Prowl; float wolfT, angle, circleDir = 1; Vector3 lungeDir;
        static Enemy lunger;   // the wolf lunging now (one at a time)

        void OnEnable() => All.Add(this);
        void OnDisable() { All.Remove(this); if (lunger == this) lunger = null; }

        void Start()
        {
            cc = GetComponent<CharacterController>(); rends = GetComponentsInChildren<Renderer>(); home = transform.position;
            baseColor = rends.Length > 0 ? rends[0].material.color : Color.red;
            circleDir = Random.value < .5f ? 1 : -1;
        }

        void Update()
        {
            if (Dead || Time.deltaTime <= 0 || !Game.Playing && !Game.InCutscene) return;
            var party = Party.I; if (!party || party.Frozen) return;
            var player = party.Player.transform.position;
            var pos = transform.position;
            Vector3 move = kind == Kind.Wolf ? WolfMove(player, pos) : PatrolMove(player, pos);
            if (stun > 0) { stun -= Time.deltaTime; move = Vector3.zero; if (!Anim) transform.Rotate(0, 400f * Time.deltaTime, 0); }
            speedNow = new Vector3(move.x, 0, move.z).magnitude;
            vy = cc.isGrounded ? -2f : vy - 30f * Time.deltaTime;
            cc.Move((move + Vector3.up * vy) * Time.deltaTime);
            if (move.sqrMagnitude > .01f && stun <= 0)
            {
                var face = kind == Kind.Wolf && wolf == W.Circle ? (player - pos) : move; face.y = 0;
                if (face.sqrMagnitude > .01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(face), 540f * Time.deltaTime);
            }
            // touching hurts (not while stunned)
            var pc = party.Player.Motor;
            float reach = cc.radius + pc.Stats.radius + .15f;
            var d = player - pos; float h = d.y; d.y = 0;
            if (stun <= 0 && d.magnitude < reach && h > -pc.Stats.height && h < cc.height && Game.Playing)
            {
                if (Health.Damage(1, pos, kind == Kind.Wolf ? "wolf" : kind == Kind.Critter ? "snow critter" : "enemy")) { stun = .8f; if (kind == Kind.Wolf) { wolf = W.Recover; wolfT = 0; if (lunger == this) lunger = null; } }
            }
            if (flash > 0) { flash -= Time.deltaTime; if (rends.Length > 0) rends[0].material.color = flash > 0 ? Color.white : baseColor; }
            if (transform.position.y < Party.KillY) Die(false);
        }

        Vector3 PatrolMove(Vector3 player, Vector3 pos)
        {
            Vector3 goal;
            Chasing = Vector3.Distance(player, pos) < sight && Vector3.Distance(player, home) < leash && Mathf.Abs(player.y - pos.y) < 3f;
            if (Chasing) goal = player;
            else { goal = toB ? b : a; if (new Vector3(goal.x - pos.x, 0, goal.z - pos.z).magnitude < .4f) toB = !toB; }
            var dir = goal - pos; dir.y = 0;
            float speed = Chasing ? chaseSpeed : patrolSpeed;
            return dir.sqrMagnitude > .01f ? dir.normalized * speed : Vector3.zero;
        }

        // Wolf: prowl (patrol) -> circle the player at 5.5 m -> lunge straight in (11 m/s, 0.5 s) -> back off -> circle again
        Vector3 WolfMove(Vector3 player, Vector3 pos)
        {
            wolfT += Time.deltaTime;
            bool sees = Vector3.Distance(player, pos) < sight && Vector3.Distance(player, home) < leash && Mathf.Abs(player.y - pos.y) < 3f;
            Chasing = sees && wolf != W.Prowl;
            var flat = player - pos; flat.y = 0;
            switch (wolf)
            {
                case W.Prowl:
                    if (sees) { wolf = W.Circle; wolfT = 0; angle = Mathf.Atan2(-flat.z, -flat.x); if (lunger == null) Sfx.Play("growl", .5f); }
                    return PatrolMove(player, pos);   // (not seen: PatrolMove only patrols)
                case W.Circle:
                {
                    if (!sees) { wolf = W.Prowl; wolfT = 0; return Vector3.zero; }
                    angle += circleDir * .55f * Time.deltaTime;
                    var target = player + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 5.5f;
                    var dir = target - pos; dir.y = 0;
                    if (wolfT > 2.6f && (lunger == null || !lunger || lunger.Dead) && flat.magnitude < 8f)
                    {
                        wolf = W.Lunge; wolfT = 0; lunger = this; Lunges++; lungeDir = flat.normalized; Sfx.Play("growl", .6f, 1.2f); if (Anim) Anim.Trigger("attack");
                        PlayLog.Line($"wolf lunges ({name})");
                    }
                    return dir.magnitude > .3f ? dir.normalized * Mathf.Min(chaseSpeed, dir.magnitude * 3f) : Vector3.zero;
                }
                case W.Lunge:
                    if (wolfT > .5f) { wolf = W.Recover; wolfT = 0; if (lunger == this) lunger = null; }
                    return lungeDir * 11f;
                default:   // recover: back off a little, then circle again
                    if (wolfT > 1f) { wolf = W.Circle; wolfT = Random.Range(0f, 1f); angle = Mathf.Atan2(-flat.z, -flat.x); }
                    return -flat.normalized * 3f;
            }
        }

        public void Stun(float seconds) { if (Dead) return; stun = Mathf.Max(stun, seconds); if (kind == Kind.Wolf) { wolf = W.Recover; wolfT = -seconds; if (lunger == this) lunger = null; } }

        public void Hit(int damage, Vector3 dir, string by)
        {
            if (Dead) return;
            hp -= damage; HitsTaken++; flash = .12f; stun = Mathf.Max(stun, .5f);
            cc.Move(new Vector3(dir.x, 0, dir.z).normalized * .6f);
            PlayLog.Line($"{kind} hit by {by} ({damage}), hp {Mathf.Max(0, hp)}");
            Sfx.Play("hit", .5f); if (Anim) Anim.Trigger("hurt");
            if (kind == Kind.Wolf && hp > 0) { wolf = W.Recover; wolfT = 0; if (lunger == this) lunger = null; }
            if (hp <= 0) Die(true);
        }

        void Die(bool drop)
        {
            Dead = true; if (lunger == this) lunger = null;
            Placeholders.Puff(transform.position + Vector3.up * .6f, kind == Kind.Critter ? new Color(.92f, .96f, 1f) : kind == Kind.Wolf ? new Color(.55f, .62f, .75f) : new Color(.9f, .3f, .2f));
            PlayLog.Line($"{kind} defeated");
            if (drop) Pickup.HealHeart(transform.position + Vector3.up * .6f, transform.parent);
            gameObject.SetActive(false);
        }

        // ---- the Ice Age's placeholders (Round 4)
        static Enemy Body(string name, Transform parent, Vector3 at, float height, float radius)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = at + Vector3.up * .05f;
            go.layer = Hero.EnemyLayer;
            var cc = go.AddComponent<CharacterController>(); cc.height = height; cc.radius = radius; cc.center = new Vector3(0, height / 2, 0); cc.stepOffset = .3f;
            return go.AddComponent<Enemy>();
        }

        // a snow critter: a round white-and-blue fluffball with a frown, on stubby feet (0.6 m)
        public static Enemy Critter(Transform parent, Vector3 a, Vector3 b)
        {
            var e = Body("Snow critter", parent, a, .7f, .38f); e.kind = Kind.Critter; e.a = a; e.b = b; e.hp = 2;
            e.patrolSpeed = 1.6f; e.chaseSpeed = 3.2f; e.sight = 6f; e.leash = 12f;
            var t = e.transform; var fur = new Material(Placeholders.Mat(new Color(.9f, .95f, 1f)));
            Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, .38f, 0), new Vector3(.75f, .7f, .72f), fur, name: "Fur");
            Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, .75f, -.05f), new Vector3(.2f, .22f, .2f), Placeholders.Mat(new Color(.75f, .87f, 1f)), name: "Tuft");
            var black = Placeholders.Mat(new Color(.08f, .08f, .1f)); var brow = Placeholders.Mat(new Color(.7f, .8f, .95f));
            foreach (float s in new[] { -1f, 1f })
            {
                Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(s * .12f, .45f, .33f), new Vector3(.09f, .1f, .06f), black, name: "Eye");
                Placeholders.Prim(PrimitiveType.Cube, t, new Vector3(s * .13f, .55f, .32f), new Vector3(.18f, .05f, .06f), brow, name: "Brow").transform.localRotation = Quaternion.Euler(0, 0, -s * 22f);   // the frown
                Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(s * .16f, .05f, .08f), new Vector3(.2f, .1f, .28f), Placeholders.Mat(new Color(.6f, .72f, .9f)), name: "Foot");
                Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(s * .26f, .66f, 0), new Vector3(.1f, .1f, .06f), fur, name: "Ear");
            }
            Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, .4f, .37f), new Vector3(.08f, .06f, .05f), Placeholders.Mat(new Color(1f, .6f, .7f)), name: "Nose");
            e.Anim = CharacterModel.Creature(e.transform, "snow_critter", 1.6f, 3.2f); if (e.Anim) e.Anim.Drive = () => (e.stun > 0 ? "stunned" : null, e.speedNow);
            return e;
        }

        // a wolf: lean, blue-grey with a cream belly and yellow eyes (0.9 m at the shoulder)
        public static Enemy Wolf(Transform parent, Vector3 a, Vector3 b)
        {
            var e = Body("Wolf", parent, a, 1.0f, .45f); e.kind = Kind.Wolf; e.a = a; e.b = b; e.hp = 3;
            e.patrolSpeed = 2.4f; e.chaseSpeed = 5.5f; e.sight = 13f; e.leash = 26f;
            var t = e.transform; var fur = new Material(Placeholders.Mat(new Color(.42f, .5f, .62f))); var dark = Placeholders.Mat(new Color(.25f, .28f, .35f)); var cream = Placeholders.Mat(new Color(.9f, .86f, .74f));
            Placeholders.Prim(PrimitiveType.Capsule, t, new Vector3(0, .72f, -.05f), new Vector3(.5f, .55f, .5f), fur, name: "Body").transform.localRotation = Quaternion.Euler(90, 0, 0);
            Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, .6f, 0), new Vector3(.38f, .3f, .8f), cream, name: "Belly");
            var head = Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.0f, .62f), new Vector3(.46f, .42f, .46f), fur, name: "Head").transform;
            Placeholders.Prim(PrimitiveType.Capsule, head, new Vector3(0, -.15f, .55f), new Vector3(.45f, .4f, .45f), cream, name: "Snout").transform.localRotation = Quaternion.Euler(90, 0, 0);
            Placeholders.Prim(PrimitiveType.Sphere, head, new Vector3(0, -.08f, 1.0f), new Vector3(.18f, .14f, .12f), Placeholders.Mat(new Color(.08f, .08f, .1f)), name: "Nose");
            foreach (float s in new[] { -1f, 1f })
            {
                Placeholders.Prim(PrimitiveType.Cube, head, new Vector3(s * .28f, .5f, -.05f), new Vector3(.18f, .35f, .08f), dark, name: "Ear").transform.localRotation = Quaternion.Euler(-10, 0, s * -15f);
                Placeholders.Prim(PrimitiveType.Sphere, head, new Vector3(s * .2f, .12f, .4f), new Vector3(.16f, .14f, .1f), Placeholders.Mat(new Color(1f, .82f, .15f), "Glow"), name: "Eye");
                Placeholders.Prim(PrimitiveType.Cube, head, new Vector3(s * .2f, .26f, .42f), new Vector3(.22f, .05f, .06f), dark, name: "Brow").transform.localRotation = Quaternion.Euler(0, 0, s * 20f);   // sly
                foreach (float z in new[] { .35f, -.4f }) Placeholders.Prim(PrimitiveType.Cylinder, t, new Vector3(s * .18f, .25f, z), new Vector3(.13f, .25f, .13f), dark, name: "Leg");
            }
            Placeholders.Prim(PrimitiveType.Capsule, t, new Vector3(0, .7f, -.75f), new Vector3(.22f, .4f, .22f), fur, name: "Tail").transform.localRotation = Quaternion.Euler(-60, 0, 0);
            e.Anim = CharacterModel.Creature(e.transform, "wolf", 2.4f, 6.0f); if (e.Anim) e.Anim.Drive = () => (e.stun > 0 ? "stunned" : null, e.speedNow);
            return e;
        }
    }

    // Round 1, item 8: the hazard. Spikes hurt and bounce the player up and back. (Round 4: the vents' lava pools use it too.)
    public sealed class Spikes : BoxVolume
    {
        public int Hits { get; private set; }
        public string cause = "spikes";
        void Update()
        {
            if (!Game.Playing || !Party.I) return;
            var p = Party.I.Player.transform.position;
            var box = Box; box.Expand(new Vector3(.4f, .2f, .4f));
            if (box.Contains(p + Vector3.up * .1f) && Health.Damage(1, new Vector3(p.x, p.y - 1, p.z) - Party.I.Player.transform.forward, cause)) Hits++;
        }
    }
}
