using UnityEngine;

namespace ShardsOfTime
{
    // Round 10, item 9: the Pirate Seas' enemies (Enemy.Kind.Deckhand / Crab / Puffer). Light combat, no boss.
    //  Deckhand: a goofy round pirate with a peg leg (Mixamo model `pirate`). Patrols, waddles after you (2.8 m/s), swings a punch when close;
    //    "Arr!" when he spots you. Now and then he trips over his own peg leg (a short stumble: your chance). 3 hits.
    //  Crab: grumpy, on the beaches and the forecastle. Scuttles sideways (it faces across its way), snaps its big claw when close. 2 hits.
    //  Puffer: a pufferfish under the sea. Drifts at its spot; when you swim close it puffs up (spiky: touching hurts) and bumps toward
    //    you slowly, then deflates (4 s). Deflated it's harmless and one hit sends it off. It never leaves the water.
    public sealed partial class Enemy
    {
        public bool Puffed { get; private set; }
        public int Puffs { get; private set; }
        public int Trips { get; private set; }
        float puffT, tripUntil, recoverAt = -1f, nextTrip = 6f, arrAt = -10f; Transform puffBody; Vector3 puffHome; float puffBase = 1f;

        Vector3 PirateMove(Vector3 player, Vector3 pos)
        {
            var flat = player - pos; flat.y = 0; float dist = flat.magnitude;
            switch (kind)
            {
                case Kind.Deckhand:
                {
                    // Fix 10.1, item 2: the trip falls flat (mm_fall), then he picks himself up (the hard landing's rise)
                    if (recoverAt > 0 && Time.time >= recoverAt) { recoverAt = -1f; if (Anim) Anim.Trigger("land_hard"); }
                    if (Time.time < tripUntil) { Chasing = false; return Vector3.zero; }
                    bool was = Chasing; var m = PatrolMove(Mathf.Abs(player.y - pos.y) > 1.5f ? player + Vector3.up * 500f : player, pos);   // only someone on his own deck
                    if (Chasing && !was && Time.time > arrAt + 5f) { arrAt = Time.time; Sfx.Play("pirate_arr", .5f, Random.Range(.9f, 1.1f)); if (Anim) Anim.Trigger("cheer"); }
                    nextTrip -= Time.deltaTime;
                    if (Chasing && nextTrip <= 0) { nextTrip = Random.Range(7f, 11f); tripUntil = Time.time + 2.9f; recoverAt = Time.time + 2.05f; Trips++; if (Anim) Anim.Trigger("mm_fall"); Sfx.Play("barrel_thud", .4f, 1.3f); PlayLog.Line($"a deckhand trips over his peg leg ({name})"); }
                    if (Chasing && dist < 2.0f && Time.time > swingAt + 1.6f) { swingAt = Time.time; if (Anim) Anim.Trigger("attack"); }
                    if (Chasing && dist < 1.3f) return Vector3.zero;
                    return m;
                }
                case Kind.Crab:
                {
                    bool was = Chasing; var m = PatrolMove(player, pos);
                    if (Chasing && !was) Sfx.Play("crab_click", .5f, Random.Range(.9f, 1.15f));
                    if (Chasing && dist < 1.4f && Time.time > swingAt + 1.2f) { swingAt = Time.time; Sfx.Play("crab_click", .45f, 1.4f); if (puffBody) puffBody.localRotation = Quaternion.Euler(-15, 0, 0); }
                    else if (puffBody && Time.time > swingAt + .25f) puffBody.localRotation = Quaternion.identity;
                    return m;
                }
                default:   // pufferfish
                {
                    Chasing = false;
                    var pl = Party.I.Player; bool inWater = pl.Motor.Mode == MoveMode.Swim;
                    float d3 = Vector3.Distance(player + Vector3.up * .8f, pos + Vector3.up * .3f);
                    if (!Puffed && inWater && d3 < 4.5f && Time.time > puffT + 2f) { Puffed = true; Puffs++; puffT = Time.time; Sfx.Play("puffer_puff", .55f, Random.Range(.95f, 1.1f)); PlayLog.Line($"a pufferfish puffs up ({name})"); }
                    if (Puffed && Time.time > puffT + 4f) { Puffed = false; puffT = Time.time; }
                    float want = Puffed ? 1f : .55f;
                    if (puffBody) { var s = puffBody.localScale.x / puffBase; s = Mathf.MoveTowards(s, want, Time.deltaTime * (Puffed ? 4f : 1f)); puffBody.localScale = Vector3.one * s * puffBase; }
                    var bob = Vector3.up * Mathf.Sin(Time.time * 1.3f + puffHome.x) * .4f;
                    Vector3 goal = Puffed && inWater && Vector3.Distance(player, puffHome) < 12f ? player + Vector3.up * .8f : puffHome + bob;
                    var dir = goal - pos; if (dir.sqrMagnitude < .04f) return Vector3.zero;
                    return dir.normalized * (Puffed ? 1.6f : .8f);
                }
            }
        }

        bool Harmless => (kind == Kind.Puffer && !Puffed) || (kind == Kind.Deckhand && Time.time < tripUntil);

        // a goofy pirate deckhand (Mixamo model `pirate`: Dan's rig; shared people's clips)
        public static Enemy Deckhand(Transform parent, Vector3 a, Vector3 b)
        {
            var e = Body("Deckhand", parent, a, 1.65f, .42f); e.kind = Kind.Deckhand; e.a = a; e.b = b; e.hp = 3;
            e.patrolSpeed = 1.3f; e.chaseSpeed = 2.8f; e.sight = 8f; e.leash = 14f;
            var t = e.transform;
            Placeholders.Prim(PrimitiveType.Capsule, t, new Vector3(0, .8f, 0), new Vector3(.8f, .8f, .7f), new Material(Placeholders.Mat(new Color(.85f, .2f, .2f))), name: "Striped shirt");
            Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.45f, 0), new Vector3(.45f, .45f, .45f), Placeholders.Mat(new Color(.9f, .7f, .55f)), name: "Head");
            Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.6f, 0), new Vector3(.47f, .2f, .47f), Placeholders.Mat(new Color(.2f, .35f, .8f)), name: "Bandana");
            e.Anim = CharacterModel.Creature(e.transform, "pirate", 1.2f, 2.8f);
            if (e.Anim) e.Anim.Drive = () => (e.stun > 0 ? "dazed" : Time.time < e.tripUntil ? "idle" : null, e.speedNow);
            return e;
        }

        // a grumpy red crab (the static `crab` model with a skitter)
        public static Enemy Crab(Transform parent, Vector3 a, Vector3 b)
        {
            var e = Body("Crab", parent, a, .45f, .3f); e.kind = Kind.Crab; e.a = a; e.b = b; e.hp = 2;
            e.patrolSpeed = 1.2f; e.chaseSpeed = 3.0f; e.sight = 5f; e.leash = 9f;
            var holder = new GameObject("Crab body").transform; holder.SetParent(e.transform, false);
            var m = StaticModel.Spawn("crab", holder, e.transform.position, 0, .45f);
            if (m) { m.transform.SetParent(holder, true); m.AddComponent<Skitter>(); }
            else Placeholders.Prim(PrimitiveType.Sphere, holder, new Vector3(0, .2f, 0), new Vector3(.55f, .3f, .4f), Placeholders.Mat(new Color(.95f, .3f, .15f)), name: "Shell");
            e.puffBody = holder;
            return e;
        }

        // a worried yellow pufferfish (the static `pufferfish` model, puffed = full size, deflated = 55 %)
        public static Enemy Pufferfish(Transform parent, Vector3 at)
        {
            var e = Body("Pufferfish", parent, at, .6f, .3f); e.kind = Kind.Puffer; e.a = at; e.b = at; e.hp = 1;
            e.patrolSpeed = .8f; e.chaseSpeed = 1.6f; e.sight = 4.5f; e.leash = 12f; e.puffHome = at;
            var holder = new GameObject("Puffer body").transform; holder.SetParent(e.transform, false); holder.localPosition = new Vector3(0, .3f, 0);
            var m = StaticModel.Spawn("pufferfish", holder, e.transform.position + Vector3.up * .0f, 0, .6f);
            if (m) { m.transform.SetParent(holder, true); m.transform.localPosition = new Vector3(0, -.3f, 0); }
            else Placeholders.Prim(PrimitiveType.Sphere, holder, Vector3.zero, Vector3.one * .6f, Placeholders.Mat(new Color(1f, .85f, .3f)), name: "Puffer");
            foreach (var r in holder.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            e.puffBody = holder; holder.localScale = Vector3.one * .55f;
            return e;
        }
    }
}
