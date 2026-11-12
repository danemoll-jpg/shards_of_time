using UnityEngine;

namespace ShardsOfTime
{
    // Round 11, item 9: Victorian Steampunk's enemies. Goofy factory workers (Mixamo model `factory_worker`; the deckhand's model stands in
    // until it is in). They patrol their own floor, grumble "Oi!" and waddle after you (2.6 m/s), swing when close. They slip on oil patches
    // (OilPatch) and a face of steam from a jet (SteamJet) sends them reeling: harmless for a moment (the deckhand's fall and get-up). 3 hits.
    public sealed partial class Enemy
    {
        public int Slips { get; private set; }
        public static int SlipsAll { get; private set; }   // (checks)

        public void Slip(string why)
        {
            if (kind != Kind.Worker || Time.time < tripUntil) return;
            tripUntil = Time.time + 2.9f; recoverAt = Time.time + 2.05f; Slips++; SlipsAll++;
            if (Anim) Anim.Trigger("mm_fall"); Sfx.Play("worker_slip", .5f, Random.Range(.9f, 1.1f));
            PlayLog.Line($"a factory worker {why} ({name})");
        }

        Vector3 WorkerMove(Vector3 player, Vector3 pos)
        {
            if (recoverAt > 0 && Time.time >= recoverAt) { recoverAt = -1f; if (Anim) Anim.Trigger("land_hard"); }
            if (Time.time < tripUntil) { Chasing = false; return Vector3.zero; }
            if (OilPatch.At(pos + Vector3.up * .2f) && speedNow > .8f) { Slip("slips on the oil"); return Vector3.zero; }
            if (SteamJet.Blowing(pos + Vector3.up * 1.2f)) { Slip("gets a face full of steam"); return Vector3.zero; }
            bool was = Chasing; var m = PatrolMove(Mathf.Abs(player.y - pos.y) > 1.5f ? player + Vector3.up * 500f : player, pos);   // only someone on his own floor
            if (Chasing && !was && Time.time > arrAt + 5f) { arrAt = Time.time; Sfx.Play("worker_grumble", .5f, Random.Range(.9f, 1.1f)); if (Anim) Anim.Trigger("yell"); }
            var flat = player - pos; flat.y = 0;
            if (Chasing && flat.magnitude < 2.0f && Time.time > swingAt + 1.6f) { swingAt = Time.time; if (Anim) Anim.Trigger("attack"); }
            if (Chasing && flat.magnitude < 1.3f) return Vector3.zero;
            return m;
        }

        // a goofy burly factory worker (Mixamo `factory_worker`; until it is in: the deckhand's model)
        public static Enemy Worker(Transform parent, Vector3 a, Vector3 b)
        {
            var e = Body("Worker", parent, a, 1.8f, .45f); e.kind = Kind.Worker; e.a = a; e.b = b; e.hp = 3;
            e.patrolSpeed = 1.2f; e.chaseSpeed = 2.6f; e.sight = 8f; e.leash = 14f;
            var t = e.transform;
            Placeholders.Prim(PrimitiveType.Capsule, t, new Vector3(0, .85f, 0), new Vector3(.9f, .85f, .75f), new Material(Placeholders.Mat(new Color(.5f, .5f, .52f))), name: "Work vest");
            Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.55f, 0), new Vector3(.45f, .45f, .45f), Placeholders.Mat(new Color(.9f, .7f, .55f)), name: "Head");
            Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.72f, 0), new Vector3(.48f, .18f, .48f), Placeholders.Mat(new Color(.45f, .3f, .18f)), name: "Cap");
            string model = CharacterModel.Exists("factory_worker") ? "factory_worker" : "pirate";
            e.Anim = CharacterModel.Creature(e.transform, model, 1.2f, 2.6f);
            if (e.Anim) e.Anim.Drive = () => (e.stun > 0 ? "dazed" : Time.time < e.tripUntil ? "idle" : null, e.speedNow);
            return e;
        }
    }
}
