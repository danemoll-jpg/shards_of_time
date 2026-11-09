using UnityEngine;

namespace ShardsOfTime
{
    // Round 1, item 8: the placeholder enemy. Patrols between two points, chases the player when close (and not too far from home),
    // touching it hurts, one or two hits defeat it (Dan's punch: one; Abi's kick and Kira's claws: two). Drops a heal heart.
    [RequireComponent(typeof(CharacterController))]
    public sealed class Enemy : MonoBehaviour
    {
        public Vector3 a, b; public int hp = 2;
        public float patrolSpeed = 2.2f, chaseSpeed = 4.2f, sight = 7f, leash = 14f;
        public bool Dead { get; private set; }
        public bool Chasing { get; private set; }
        public int HitsTaken { get; private set; }
        CharacterController cc; Renderer[] rends; Color baseColor; float flash, stun, vy; bool toB = true; Vector3 home;

        void Start()
        {
            cc = GetComponent<CharacterController>(); rends = GetComponentsInChildren<Renderer>(); home = transform.position;
            baseColor = rends.Length > 0 ? rends[0].material.color : Color.red;
        }

        void Update()
        {
            if (Dead || Time.deltaTime <= 0 || !Game.Playing && !Game.InCutscene) return;
            var party = Party.I; if (!party || party.Frozen) return;
            var player = party.Player.transform.position;
            var pos = transform.position;
            Vector3 goal;
            Chasing = Vector3.Distance(player, pos) < sight && Vector3.Distance(player, home) < leash && Mathf.Abs(player.y - pos.y) < 3f;
            if (Chasing) goal = player;
            else { goal = toB ? b : a; if (new Vector3(goal.x - pos.x, 0, goal.z - pos.z).magnitude < .4f) toB = !toB; }
            var dir = goal - pos; dir.y = 0;
            float speed = Chasing ? chaseSpeed : patrolSpeed;
            if (stun > 0) { stun -= Time.deltaTime; speed = 0; }
            var move = dir.sqrMagnitude > .01f ? dir.normalized * speed : Vector3.zero;
            vy = cc.isGrounded ? -2f : vy - 30f * Time.deltaTime;
            cc.Move((move + Vector3.up * vy) * Time.deltaTime);
            if (move.sqrMagnitude > .01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(move), 540f * Time.deltaTime);
            // touching hurts
            var pc = party.Player.Motor;
            float reach = cc.radius + pc.Stats.radius + .15f;
            var d = player - pos; float h = d.y; d.y = 0;
            if (d.magnitude < reach && h > -pc.Stats.height && h < cc.height && Game.Playing) { if (Health.Damage(1, pos, "enemy")) stun = .8f; }
            if (flash > 0) { flash -= Time.deltaTime; if (rends.Length > 0) rends[0].material.color = flash > 0 ? Color.white : baseColor; }
            if (transform.position.y < Party.KillY) Die(false);
        }

        public void Hit(int damage, Vector3 dir, string by)
        {
            if (Dead) return;
            hp -= damage; HitsTaken++; flash = .12f; stun = .5f;
            cc.Move(new Vector3(dir.x, 0, dir.z).normalized * .6f);
            PlayLog.Line($"enemy hit by {by} ({damage}), hp {Mathf.Max(0, hp)}");
            Sfx.Play("hit", .5f);
            if (hp <= 0) Die(true);
        }

        void Die(bool drop)
        {
            Dead = true;
            Placeholders.Puff(transform.position + Vector3.up * .6f, new Color(.9f, .3f, .2f));
            PlayLog.Line("enemy defeated");
            if (drop) Pickup.HealHeart(transform.position + Vector3.up * .6f, transform.parent);
            gameObject.SetActive(false);
        }
    }

    // Round 1, item 8: the hazard. Spikes hurt and bounce the player up and back.
    public sealed class Spikes : BoxVolume
    {
        public int Hits { get; private set; }
        void Update()
        {
            if (!Game.Playing || !Party.I) return;
            var p = Party.I.Player.transform.position;
            var box = Box; box.Expand(new Vector3(.4f, .2f, .4f));
            if (box.Contains(p + Vector3.up * .1f) && Health.Damage(1, new Vector3(p.x, p.y - 1, p.z) - Party.I.Player.transform.forward, "spikes")) Hits++;
        }
    }
}
