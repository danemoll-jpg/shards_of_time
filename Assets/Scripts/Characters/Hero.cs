using UnityEngine;

namespace ShardsOfTime
{
    // One hero (Dan, Abi, Kira): stats, motor, body, attack. Controlled by the Party: as the player (input) or as a follower.
    // Body: the graybox shapes ("Graybox") and, when the hero has a real model, the model ("Model ...": Round 2, Kira's forms); the
    // graybox stays as the fallback when a model is missing.
    public sealed class Hero : MonoBehaviour
    {
        public const int PlayerLayer = 8, FollowerLayer = 9, EnemyLayer = 10;
        public CharacterStats Stats { get; private set; }
        public CharacterMotor Motor { get; private set; }
        public Follower Follower { get; private set; }
        public Transform Body { get; private set; }
        public Transform NecklaceGem { get; private set; }
        public Transform Graybox { get; private set; }
        public GameObject Model { get; private set; }
        public CreatureAnimator Anim { get; private set; }
        public KiraForm Form { get; private set; }          // Kira only (Round 2, item 7)
        public bool IsPlayer { get; private set; }
        public int Attacks { get; private set; }
        float attackTimer; Transform swipe; float swipeTimer;
        float squash;

        public static Hero Create(CharacterStats stats, Vector3 pos)
        {
            var go = new GameObject(stats.displayName);
            go.transform.position = pos;
            var cc = go.AddComponent<CharacterController>();
            var h = go.AddComponent<Hero>();
            h.Stats = stats;
            h.Motor = go.AddComponent<CharacterMotor>(); h.Motor.Configure(stats);
            h.Follower = go.AddComponent<Follower>(); h.Follower.Hero = h;
            h.Body = Placeholders.HeroBody(stats, go.transform);
            h.Graybox = new GameObject("Graybox").transform; h.Graybox.SetParent(h.Body, false);
            for (int i = h.Body.childCount - 1; i >= 0; i--) { var c = h.Body.GetChild(i); if (c != h.Graybox) c.SetParent(h.Graybox, false); }
            if (stats.displayName == "Dan")
            {
                h.NecklaceGem = NecklaceModel.Attach(h.Body, stats);
                var cube = h.Graybox.Find("Necklace");
                if (h.NecklaceGem) { if (cube) Object.Destroy(cube.gameObject); }
                else h.NecklaceGem = cube;   // no necklace model: the Round 1 gem
            }
            if (stats.displayName == "Kira") h.SetForm(KiraForm.Load(KiraForm.Home));
            Health.Hurt += h.OnHurt;
            h.Motor.Event += h.OnMotorEvent;
            h.swipe = Placeholders.Swipe(go.transform, stats);
            _ = cc;
            return h;
        }

        void OnDestroy() => Health.Hurt -= OnHurt;
        void OnHurt() { if (IsPlayer && Anim) Anim.Trigger("hurt"); }

        public bool HasAbility(string id) => Form && Form.Has(id);

        // Kira's form (Round 2, item 7): swaps her model and her abilities. Same form again: nothing happens.
        public void SetForm(KiraForm f)
        {
            if (!f || (Form && Form.id == f.id)) return;
            Form = f;
            if (Model) { Destroy(Model); Model = null; Anim = null; }
            Model = CharacterModel.Build(f.model, Body, Motor);
            Anim = Model ? Model.GetComponent<CreatureAnimator>() : null;
            Graybox.gameObject.SetActive(!Model);
            PlayLog.Line($"Kira's form: {f.displayName} ({(Model ? "model " + f.model : "graybox: no model")}, abilities [{string.Join(", ", f.abilities)}])");
        }

        public void SetPlayer(bool player)
        {
            IsPlayer = player;
            gameObject.layer = player ? PlayerLayer : FollowerLayer;
            gameObject.tag = player ? "Player" : "Untagged";
            Follower.enabled = !player;
        }

        void OnMotorEvent(string e)
        {
            if (e == "land") { squash = .18f; if (Anim) Anim.Trigger("land"); }
            if (e == "jump" || e == "leap") { squash = -.12f; if (IsPlayer) Sfx.Play("jump", .5f); if (Anim) Anim.Trigger("jump"); }
            if (e == "doublejump") { squash = -.18f; if (IsPlayer) Sfx.Play("jump", .6f, 1.3f); Placeholders.Puff(transform.position, Stats.color); }
            if (e == "splash" && IsPlayer) Sfx.Play("splash", .5f);
            if (e == "outofbreath" && IsPlayer) Health.Damage(1, Vector3.zero, "out of breath");
        }

        // Each hero's basic attack: a short sweep in front that hits enemies (Punch, Kick, Claws).
        public bool TryAttack()
        {
            if (attackTimer > 0) return false;
            attackTimer = Stats.attackCooldown; Attacks++;
            swipeTimer = .15f; swipe.gameObject.SetActive(true);
            if (Anim) Anim.Trigger("attack");
            Sfx.Play("attack", .4f, Stats.displayName == "Kira" ? 1.4f : Stats.displayName == "Abi" ? 1.15f : .9f);
            var c = transform.position + Vector3.up * Stats.height * .5f + transform.forward * Stats.attackRange;
            foreach (var col in Physics.OverlapSphere(c, Stats.attackRadius, ~0, QueryTriggerInteraction.Collide))
            {
                var e = col.GetComponentInParent<Enemy>();
                if (e) e.Hit(Stats.attackDamage, transform.forward, Stats.attackName);
                var ice = col.GetComponentInParent<IceWall>();
                if (ice) ice.Claw(this);
            }
            return true;
        }

        void Update()
        {
            attackTimer -= Time.deltaTime;
            if (swipeTimer > 0) { swipeTimer -= Time.deltaTime; if (swipeTimer <= 0) swipe.gameObject.SetActive(false); }
            // a little squash and stretch on jumps and landings (placeholder life)
            squash = Mathf.MoveTowards(squash, 0, Time.deltaTime * 1.2f);
            // a follower right in front of the camera (walls pulled it in) hides, so it never blocks the view of the player
            if (!IsPlayer && CameraRig.I && !Game.InCutscene)
                Body.gameObject.SetActive(Vector3.Distance(CameraRig.I.Cam.transform.position, Motor.Center) > Stats.height * .5f + 1.2f);
            else if (!Body.gameObject.activeSelf && !Game.InCutscene) Body.gameObject.SetActive(true);
            float s = Model ? 1f : 1f - squash;   // a real model has its own jump / land clips
            Body.localScale = new Vector3(1f / Mathf.Sqrt(Mathf.Max(.5f, s)), s, 1f / Mathf.Sqrt(Mathf.Max(.5f, s)));
        }
    }
}
