using UnityEngine;

namespace ShardsOfTime
{
    // Round 8, item 8: the Medieval Kingdom's enemies (Enemy.Kind.Knight / Archer / Rat). Light combat, no boss.
    //  Knight: clumsy, in too-big armour. Patrols, plods after you (3 m/s), swings his sword when close. His shield is up toward the
    //    front: hits from in front clang off; Dan's charged punch knocks the shield away (then any hit lands), or go round him. 3 hits.
    //  Archer: nervous, up on the walls. Sees you (24 m, below him), draws (1 s) and lets a slow arrow fly (11 m/s, a gentle arc) at
    //    where you were: step aside. Come close and he flinches back, looking about. 1 hit.
    //  Rat: dungeon rats, quick little chases (4.6 m/s), a nip hurts. Dragon Kira's fire puff sends them squeaking away (3 s). 1 hit.
    public sealed partial class Enemy
    {
        public bool ShieldUp { get; private set; }
        public int Blocks_ { get; private set; }
        public int Shots { get; private set; }
        public bool Scared => Time.time < scaredUntil;
        public int Scares { get; private set; }
        float scaredUntil, drawT = -1, shootAt = -10f, clankAt, swingAt = -10f; Vector3 scaredFrom; Transform shield;

        bool Blocks(Vector3 dir)
        {
            if (kind != Kind.Knight || !ShieldUp) return false;
            dir.y = 0; if (dir.sqrMagnitude < .01f) return false;
            if (Vector3.Dot(dir.normalized, transform.forward) > -.25f) return false;   // from the side or behind: it lands
            Blocks_++; flash = .08f; Sfx.Play("shield_block", .55f, Random.Range(.95f, 1.08f));
            if (Anim) Anim.Trigger("block");
            if (Blocks_ % 3 == 1) Hud.Message($"Clang! The shield's in the way. Go round him{(Party.I && Party.I.Player.HasAbility(Abilities.ChargedPunch) ? $", or charge a punch (hold {Controls.Btn(Controls.Act.Attack)})" : "")}.", 3f);
            PlayLog.Line($"knight blocks ({name})");
            return true;
        }

        // Dan's charged punch: a knight's shield flies off; anything else takes a big hit
        public void HitCharged(Vector3 dir, Hero by)
        {
            if (Dead) return;
            if (kind == Kind.Knight && ShieldUp)
            {
                ShieldUp = false; Sfx.Play("shield_break", .7f); stun = Mathf.Max(stun, 1.6f);
                if (shield) { shield.SetParent(transform.parent, true); var rb = shield.gameObject.AddComponent<Rigidbody>(); rb.AddForce((dir + Vector3.up) * 4f, ForceMode.VelocityChange); Destroy(shield.gameObject, 4f); shield = null; }
                PlayLog.Line($"knight's shield knocked away ({name})");
                Hud.Message("The shield goes flying!", 2.5f);
                return;
            }
            Hit(3, dir, "Charged punch");
        }

        public void Scare(Vector3 from)
        {
            if (Dead || kind != Kind.Rat) return;
            if (!Scared) { Scares++; Sfx.Play("rat_squeak", .6f, Random.Range(1f, 1.2f)); PlayLog.Line($"a rat flees the fire ({name})"); }
            scaredUntil = Time.time + 3f; scaredFrom = from;
        }

        Vector3 MedievalMove(Vector3 player, Vector3 pos)
        {
            var pl = Party.I.Player; var flat = player - pos; flat.y = 0; float dist = flat.magnitude;
            switch (kind)
            {
                case Kind.Knight:
                {
                    var m = PatrolMove(player, pos);
                    if (Chasing && Time.time > clankAt) { clankAt = Time.time + 1.4f; Sfx.Play("armor_clank", .35f, Random.Range(.9f, 1.1f)); }
                    if (Chasing && dist < 2.2f && Time.time > swingAt + 1.8f) { swingAt = Time.time; if (Anim) Anim.Trigger("attack"); }
                    if (Chasing && dist < 1.4f) return Vector3.zero;   // stands and swings
                    return m;
                }
                case Kind.Archer:
                {
                    Chasing = false;
                    bool below = player.y < pos.y - 1.5f;
                    if (dist < 3.2f && !below) { if (Anim && Random.value < .01f) Anim.Trigger("hurt"); return -flat.normalized * 1.2f; }   // too close: he edges back, nervous
                    bool sees = dist < sight && (below || Mathf.Abs(player.y - pos.y) < 3f) && !Physics.Linecast(pos + Vector3.up * 1.6f, player + Vector3.up * 1f, ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << Hero.EnemyLayer) | (1 << 4) | (1 << 2)), QueryTriggerInteraction.Ignore);
                    if (sees && flat.sqrMagnitude > .01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat), 200f * Time.deltaTime);
                    if (drawT >= 0)
                    {
                        drawT += Time.deltaTime;
                        if (drawT > 1.0f) { drawT = -1; shootAt = Time.time; Shots++; if (Anim) Anim.Trigger("shoot"); Arrow.Fire(transform.parent, pos + Vector3.up * 1.45f + transform.forward * .5f, player + Vector3.up * .9f); Sfx.Play("arrow_twang", .5f); }
                    }
                    else if (sees && Time.time > shootAt + 2.6f) { drawT = 0; if (Anim) Anim.Trigger("draw"); }
                    return Vector3.zero;
                }
                default:   // rat
                {
                    if (Scared) { Chasing = false; var away = pos - scaredFrom; away.y = 0; return away.sqrMagnitude > .01f ? away.normalized * 5.5f : transform.forward * 5.5f; }
                    bool was = Chasing; var m = PatrolMove(player, pos);
                    if (Chasing && !was) Sfx.Play("rat_squeak", .45f, Random.Range(.9f, 1.15f));
                    return m;
                }
            }
        }

        // a clumsy knight: short and stocky in too-big shiny armour (Mixamo model `knight`; sword and shield props in his hands)
        public static Enemy Knight(Transform parent, Vector3 a, Vector3 b)
        {
            var e = Body("Knight", parent, a, 1.6f, .45f); e.kind = Kind.Knight; e.a = a; e.b = b; e.hp = 3;
            e.patrolSpeed = 1.4f; e.chaseSpeed = 3.0f; e.sight = 9f; e.leash = 16f; e.ShieldUp = true;
            var t = e.transform; var steel = new Material(Placeholders.Mat(new Color(.75f, .77f, .82f)));
            Placeholders.Prim(PrimitiveType.Capsule, t, new Vector3(0, .8f, 0), new Vector3(.8f, .8f, .7f), steel, name: "Armour");
            Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.45f, 0), new Vector3(.5f, .5f, .5f), steel, name: "Helmet");
            Placeholders.Prim(PrimitiveType.Cube, t, new Vector3(0, .85f, .36f), new Vector3(.5f, .7f, .06f), Placeholders.Mat(new Color(.75f, .12f, .12f)), name: "Tabard");
            e.Anim = CharacterModel.Creature(e.transform, "knight", 1.3f, 3f);
            if (e.Anim) e.Anim.Drive = () => (e.stun > 0 ? "idle" : null, e.speedNow);
            var anim = e.Anim ? e.Anim.GetComponentInChildren<Animator>() : null;
            Transform Hand(HumanBodyBones b) => anim && anim.isHuman ? anim.GetBoneTransform(b) : null;
            var sw = Hand(HumanBodyBones.RightHand); var sh = Hand(HumanBodyBones.LeftHand);
            var sword = WorldArt.Prop("medieval_props", "Sword", sw ? sw : t, Vector3.zero);
            if (sword) { sword.transform.SetParent(sw ? sw : t, false); sword.transform.localPosition = sw ? Vector3.zero : new Vector3(.45f, .9f, .3f); sword.transform.localRotation = Quaternion.Euler(0, 0, sw ? 90 : 0); Unscale(sword.transform); }
            var shield = WorldArt.Prop("medieval_props", "Shield", sh ? sh : t, Vector3.zero);
            if (!shield) shield = Placeholders.Prim(PrimitiveType.Cube, t, Vector3.zero, new Vector3(.6f, .75f, .08f), Placeholders.Mat(new Color(.75f, .12f, .12f)), name: "Shield");
            shield.transform.SetParent(sh ? sh : t, false); shield.transform.localPosition = sh ? Vector3.zero : new Vector3(-.3f, .95f, .45f); shield.transform.localRotation = Quaternion.identity; Unscale(shield.transform);
            if (sh) { shield.transform.position = sh.position + t.forward * .18f; shield.transform.rotation = Quaternion.LookRotation(t.forward, Vector3.up); }
            e.shield = shield.transform;
            return e;
        }

        // a skinny nervous archer up on the walls (Mixamo model `archer`; his bow a prop in his left hand)
        public static Enemy Archer(Transform parent, Vector3 at, float yaw)
        {
            var e = Body("Archer", parent, at, 1.8f, .35f); e.kind = Kind.Archer; e.a = at; e.b = at; e.hp = 1;
            e.patrolSpeed = 0; e.chaseSpeed = 0; e.sight = 24f; e.leash = 999f;
            e.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var t = e.transform;
            Placeholders.Prim(PrimitiveType.Capsule, t, new Vector3(0, .9f, 0), new Vector3(.45f, .9f, .4f), new Material(Placeholders.Mat(new Color(.25f, .5f, .25f))), name: "Tunic");
            Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.65f, 0), new Vector3(.3f, .32f, .3f), Placeholders.Mat(new Color(.9f, .72f, .58f)), name: "Head");
            e.Anim = CharacterModel.Creature(e.transform, "archer", 1.3f, 3f);
            if (e.Anim) e.Anim.Drive = () => (e.drawT >= 0 ? "aim" : "nervous", 0f);
            var anim = e.Anim ? e.Anim.GetComponentInChildren<Animator>() : null;
            var lh = anim && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.LeftHand) : null;
            var bow = WorldArt.Prop("medieval_props", "Bow", lh ? lh : t, Vector3.zero);
            if (bow) { bow.transform.SetParent(lh ? lh : t, false); bow.transform.localPosition = lh ? Vector3.zero : new Vector3(-.35f, 1.1f, .3f); bow.transform.localRotation = Quaternion.identity; Unscale(bow.transform); }
            return e;
        }

        // a chubby cheeky dungeon rat (Code's rig `rat`)
        public static Enemy Rat(Transform parent, Vector3 a, Vector3 b)
        {
            var e = Body("Rat", parent, a, .35f, .22f); e.kind = Kind.Rat; e.a = a; e.b = b; e.hp = 1;
            e.patrolSpeed = 1.6f; e.chaseSpeed = 4.6f; e.sight = 6f; e.leash = 12f;
            var t = e.transform; var fur = new Material(Placeholders.Mat(new Color(.5f, .45f, .4f)));
            Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, .17f, 0), new Vector3(.3f, .26f, .5f), fur, name: "Body");
            Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, .22f, .27f), new Vector3(.2f, .2f, .22f), fur, name: "Head");
            foreach (float s in new[] { -1f, 1f }) Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(s * .1f, .34f, .27f), new Vector3(.12f, .12f, .04f), Placeholders.Mat(new Color(.95f, .65f, .7f)), name: "Ear");
            Placeholders.Prim(PrimitiveType.Capsule, t, new Vector3(0, .08f, -.45f), new Vector3(.04f, .25f, .04f), Placeholders.Mat(new Color(.95f, .65f, .7f)), name: "Tail").transform.localRotation = Quaternion.Euler(80, 0, 0);
            e.Anim = CharacterModel.Creature(e.transform, "rat", 1.4f, 4.6f);
            if (e.Anim) e.Anim.Drive = () => (e.stun > 0 ? "stunned" : null, e.speedNow);
            return e;
        }

        // a prop parented to a bone keeps its own size (bones of Mixamo rigs carry a 0.01 scale)
        static void Unscale(Transform t) { var s = t.lossyScale; var ls = t.localScale; t.localScale = new Vector3(ls.x / Mathf.Max(1e-4f, s.x), ls.y / Mathf.Max(1e-4f, s.y), ls.z / Mathf.Max(1e-4f, s.z)); }
    }

    // a slow arrow: flies at 11 m/s in a gentle arc toward where the player was; touching the player hurts (a heart), anything solid stops it
    public sealed class Arrow : MonoBehaviour
    {
        Vector3 v; float life; bool prop;
        public static int Hits { get; private set; }
        public static void Fire(Transform parent, Vector3 from, Vector3 to)
        {
            var go = WorldArt.Prop("medieval_props", "Arrow", parent, from) ?? Placeholders.Prim(PrimitiveType.Cylinder, parent, from, new Vector3(.03f, .4f, .03f), Placeholders.Mat(new Color(.5f, .35f, .2f)), name: "Arrow");
            bool isProp = go.name == "Arrow" && !go.GetComponent<MeshFilter>(); go.name = isProp ? "Arrow prop" : "Arrow";
            var a = go.AddComponent<Arrow>(); a.prop = isProp;
            const float speed = 11f, g = 4f;
            var d = to - from; float t = new Vector3(d.x, 0, d.z).magnitude / speed;
            a.v = new Vector3(d.x, 0, d.z).normalized * speed + Vector3.up * (d.y / Mathf.Max(.1f, t) + .5f * g * t);
        }
        void Update()
        {
            if (!Game.Playing) return;
            life += Time.deltaTime; v += Vector3.down * 4f * Time.deltaTime;
            var step = v * Time.deltaTime; var p = transform.position;
            if (step.sqrMagnitude > 1e-6f) transform.rotation = prop ? Quaternion.LookRotation(-step) : Quaternion.LookRotation(step) * Quaternion.Euler(90, 0, 0);   // the prop points -z
            var pl = Party.I ? Party.I.Player : null;
            if (pl && Vector3.Distance(p, pl.Motor.Center) < pl.Stats.radius + .35f && Health.Damage(1, p - v.normalized, "an arrow")) { Hits++; Destroy(gameObject); return; }
            if (life > 5f || Physics.Raycast(p, step.normalized, step.magnitude, ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << Hero.EnemyLayer) | (1 << 4) | (1 << 2)), QueryTriggerInteraction.Ignore)) { Destroy(gameObject, life > 5f ? 0f : 1.5f); enabled = false; return; }
            transform.position = p + step;
        }
    }
}
