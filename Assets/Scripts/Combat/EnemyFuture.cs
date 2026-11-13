using System.Collections.Generic;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 12, item 9: Tomorrowland's goofy runaway cleanbots (Dan's Tripo model `cleanbot`, its top wobbling). They trundle along their
    // beat, chase you when close (a bump costs a heart) and vacuum up loose shards they roll over (the shard is carried inside, never lost):
    // bop one and it coughs them all back out. Robot Kira's friendly switch makes every cleanbot friendly for a while (it just cleans).
    // Round 13, item 8: the Moon's critters (Dan's Tripo model `moon_critter`, its eye stalks wobbling): they bounce toward you in big slow
    // low-gravity hops; a bump knocks you back (no heart lost); bopped once they curl up and roll away like little moons, harmless.
    public sealed partial class Enemy
    {
        public static float CleanbotsFriendlyUntil;
        public static int FriendlySwitches, Coughs, Sucks, CritterBumps, CritterRolls;
        public static bool CleanbotsFriendly => Time.time < CleanbotsFriendlyUntil;
        readonly List<Pickup> inside = new List<Pickup>();
        public int Carrying => inside.Count;
        bool rolled; Vector3 rollDir; float rollT, hopT, bumpAt = -10, noSuckUntil;
        bool FutureHarmless => (kind == Kind.Cleanbot && CleanbotsFriendly) || (kind == Kind.MoonCritter);   // critters bump (below), never hurt

        Vector3 FutureMove(Vector3 player, Vector3 pos)
        {
            if (kind == Kind.MoonCritter) return CritterMove(player, pos);
            // the vacuum: a loose shard under it goes inside
            if (Time.time > noSuckUntil) foreach (var p in Pickup.All)   // (not while it is still coughing)
            {
                if (!p || !p.gameObject.activeSelf || p.kind != Pickup.Kind.Shard) continue;
                var d = p.transform.position - pos; d.y = 0;
                if (d.magnitude < .9f && Mathf.Abs(p.transform.position.y - pos.y) < 2f) { p.gameObject.SetActive(false); inside.Add(p); Sucks++; Sfx.Play("vacuum_slurp", .5f); PlayLog.Line($"a cleanbot vacuums up shard {p.id} ({name})"); break; }
            }
            if (CleanbotsFriendly) { Chasing = false; var dir = (toB ? b : a) - pos; dir.y = 0; if (dir.magnitude < .4f) toB = !toB; return dir.sqrMagnitude > .01f ? dir.normalized * patrolSpeed : Vector3.zero; }
            return PatrolMove(Mathf.Abs(player.y - pos.y) > 1.5f ? player + Vector3.up * 500f : player, pos);
        }

        // bopped: everything it vacuumed comes back out around it
        void Cough()
        {
            if (inside.Count == 0) { Sfx.Play("robot_beep", .4f, .8f); return; }
            Coughs++; Sfx.Play("vacuum_cough", .6f); noSuckUntil = Time.time + 6f;
            for (int i = 0; i < inside.Count; i++)
            {
                var p = inside[i]; if (!p) continue;
                float a = i * Mathf.PI * 2f / inside.Count;
                var at = transform.position + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 2.2f;
                at.y = Game.OnGround(at.x, at.z).y + 1f; if (Mathf.Abs(at.y - transform.position.y) > 3f) at.y = transform.position.y + 1f;
                p.transform.position = at; p.gameObject.SetActive(true);
            }
            PlayLog.Line($"a cleanbot coughs {inside.Count} shard(s) back out ({name})");
            inside.Clear();
            Placeholders.Puff(transform.position + Vector3.up * .5f, new Color(.95f, .75f, .85f));
        }

        Vector3 CritterMove(Vector3 player, Vector3 pos)
        {
            if (rolled)
            {
                rollT -= Time.deltaTime; if (Anim) { }
                transform.Rotate(Vector3.Cross(Vector3.up, rollDir) * 420f * Time.deltaTime, Space.World);
                return rollT > 0 ? rollDir * 4.5f : Vector3.zero;
            }
            hopT -= Time.deltaTime;
            var m = PatrolMove(Mathf.Abs(player.y - pos.y) > 3f ? player + Vector3.up * 500f : player, pos);
            if (cc.isGrounded && hopT <= 0 && m.sqrMagnitude > .01f) { vy = 4.2f; hopT = 1.1f; if (Party.I && Vector3.Distance(player, pos) < 12f) Sfx.Play("critter_boing", .35f, Random.Range(.9f, 1.2f)); }
            // a bump: knocked back, no heart lost
            var d = player - pos; float h = d.y; d.y = 0;
            if (Chasing && d.magnitude < .95f && h > -1f && h < 1.2f && Time.time > bumpAt + 1.2f && Game.Playing)
            {
                bumpAt = Time.time; CritterBumps++;
                Party.I.Player.Motor.Knockback((d.sqrMagnitude > .01f ? d.normalized : transform.forward) * 6f + Vector3.up * 4f);
                Sfx.Play("critter_boing", .7f, .8f); PlayLog.Line($"a moon critter bumps {Party.I.Player.Stats.displayName}");
            }
            return m;
        }

        // bopped critter: curls up and rolls away, harmless for good
        void RollAway(Vector3 dir)
        {
            if (rolled) return;
            rolled = true; CritterRolls++; rollDir = new Vector3(dir.x, 0, dir.z).sqrMagnitude > .01f ? new Vector3(dir.x, 0, dir.z).normalized : transform.forward; rollT = 2.4f;
            Chasing = false; Sfx.Play("critter_roll", .6f); PlayLog.Line($"a moon critter rolls away like a little moon ({name})");
        }
        public bool Rolled => rolled;

        // a runaway cleanbot (Dan's Tripo model; until it is in: a pink drum with a red light)
        public static Enemy Cleanbot(Transform parent, Vector3 a, Vector3 b)
        {
            var e = Body("Cleanbot", parent, a, .75f, .36f); e.kind = Kind.Cleanbot; e.a = a; e.b = b; e.hp = 3;
            e.patrolSpeed = 1.4f; e.chaseSpeed = 3.0f; e.sight = 7f; e.leash = 16f;
            var m = StaticModel.Spawn("cleanbot", e.transform, e.transform.position, 0, .75f);
            if (m) { m.transform.localPosition = Vector3.zero; m.AddComponent<RigidParts>(); }
            else
            {
                Placeholders.Prim(PrimitiveType.Cylinder, e.transform, new Vector3(0, .3f, 0), new Vector3(.6f, .3f, .6f), Placeholders.Mat(new Color(.98f, .66f, .72f)), name: "Drum");
                Placeholders.Prim(PrimitiveType.Sphere, e.transform, new Vector3(0, .66f, 0), new Vector3(.14f, .14f, .14f), Placeholders.Mat(new Color(1f, .2f, .15f), "Glow"), name: "Light");
            }
            return e;
        }

        // a moon critter (Dan's Tripo model; until it is in: a lavender ball with three eyes)
        public static Enemy MoonCritter(Transform parent, Vector3 a, Vector3 b)
        {
            var e = Body("Moon critter", parent, a, .6f, .3f); e.kind = Kind.MoonCritter; e.a = a; e.b = b; e.hp = 99;
            e.patrolSpeed = 1.6f; e.chaseSpeed = 3.0f; e.sight = 8f; e.leash = 18f;
            var m = StaticModel.Spawn("moon_critter", e.transform, e.transform.position, 0, .6f);
            if (m) { m.transform.localPosition = Vector3.zero; m.AddComponent<RigidParts>().wobble = 1.6f; }
            else Placeholders.Prim(PrimitiveType.Sphere, e.transform, new Vector3(0, .3f, 0), new Vector3(.55f, .55f, .55f), Placeholders.Mat(new Color(.7f, .55f, .95f)), name: "Blob");
            return e;
        }
    }
}
