using UnityEngine;

namespace ShardsOfTime
{
    // Round 7, item 8: Bethlehem's enemies (Enemy.Kind.Goat / Dog / Scorpion). Light combat, no boss. None of them notices lamb Kira
    // hiding in a flock (Stealth.Hidden).
    //  Goat: grazes round its rocks; sees you (9 m, in front) -> lowers its head (0.8 s, a warning), charges straight (9 m/s, 1.3 s),
    //    a headbutt knocks you back (a heart); then shakes its head (1.5 s). 3 hits.
    //  Dog: sniffs round its yard; sees you (8 m) -> barks (the nearest watchman comes to look) and chases a short way (5 m/s, 10 m
    //    from home), a nip hurts; then trots home. 2 hits.
    //  Scorpion: small, scuttles about, a short quick chase (3.4 m/s), the snap hurts. 1 hit.
    public sealed partial class Enemy
    {
        enum G { Graze, Lower, Charge, Shake }
        G goat = G.Graze; float goatT, barkAt = -10f; Vector3 chargeDir;
        public int Charges { get; private set; }
        public int Barks { get; private set; }
        public string GoatState => goat.ToString();

        Vector3 BethlehemMove(Vector3 player, Vector3 pos)
        {
            var pl = Party.I.Player; var flat = player - pos; flat.y = 0; float dist = flat.magnitude;
            bool hidden = Stealth.Hidden(pl);
            bool sees = !hidden && dist < sight && Mathf.Abs(player.y - pos.y) < 2.5f && Vector3.Distance(player, home) < leash;
            switch (kind)
            {
                case Kind.Goat:
                    goatT += Time.deltaTime;
                    switch (goat)
                    {
                        case G.Graze:
                            Chasing = false;
                            if (sees && Vector3.Angle(transform.forward, flat) < 80f) { goat = G.Lower; goatT = 0; chargeDir = flat.normalized; Sfx.Play("goat_charge", .55f); if (Anim) Anim.Trigger("attack"); PlayLog.Line($"a goat lowers its head at {pl.Stats.displayName}"); }
                            else if (sees) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat), 160f * Time.deltaTime);
                            return sees ? Vector3.zero : PatrolMove(player + Vector3.up * 999f, pos);
                        case G.Lower:
                            if (dist > .1f) chargeDir = Vector3.RotateTowards(chargeDir, flat.normalized, 1.2f * Time.deltaTime, 0);
                            transform.rotation = Quaternion.LookRotation(chargeDir);
                            if (goatT > .8f) { goat = G.Charge; goatT = 0; Charges++; Chasing = true; }
                            return Vector3.zero;
                        case G.Charge:
                            if (goatT > 1.3f || hidden) { goat = G.Shake; goatT = 0; Chasing = false; }
                            return chargeDir * 9f;
                        default:
                            if (goatT > 1.5f) { goat = G.Graze; goatT = 0; }
                            return Vector3.zero;
                    }
                case Kind.Dog:
                {
                    if (sees && Time.time > barkAt + 6f)
                    {
                        barkAt = Time.time; Barks++; Sfx.Play("dog_bark", .65f, Random.Range(.95f, 1.08f)); if (Anim) Anim.Trigger("attack");
                        var w = Guard.Alert(pos);
                        if (Barks == 1 || w) Hud.Message(w ? "Woof! WOOF! The dog is calling the watchman!" : "Woof! WOOF!", 3f);
                    }
                    Chasing = sees && Time.time < barkAt + 4f;
                    if (Chasing) return dist > .4f ? flat.normalized * chaseSpeed : Vector3.zero;
                    var homeFlat = home - pos; homeFlat.y = 0;
                    if (homeFlat.magnitude > 1.5f && Vector3.Distance(pos, a) > 1.5f && Vector3.Distance(pos, b) > 1.5f) return homeFlat.normalized * patrolSpeed;
                    return PatrolMove(player + Vector3.up * 999f, pos);   // sniffing round its yard (the player out of its patrol's reach)
                }
                default:   // scorpion
                {
                    if (hidden) { Chasing = false; return PatrolMove(player + Vector3.up * 999f, pos); }
                    bool was = Chasing; var m = PatrolMove(player, pos);
                    if (Chasing && !was) Sfx.Play("scorpion_click", .5f, Random.Range(.9f, 1.15f));
                    return m;
                }
            }
        }

        // a grumpy goat: shaggy brown and cream, curled horns, a beard (1 m)
        public static Enemy Goat(Transform parent, Vector3 a, Vector3 b)
        {
            var e = Body("Goat", parent, a, 1.05f, .42f); e.kind = Kind.Goat; e.a = a; e.b = b; e.hp = 3;
            e.patrolSpeed = 1.1f; e.chaseSpeed = 9f; e.sight = 9f; e.leash = 22f;
            var t = e.transform; var fur = new Material(Placeholders.Mat(new Color(.6f, .38f, .2f))); var cream = Placeholders.Mat(new Color(.92f, .86f, .72f)); var horn = Placeholders.Mat(new Color(.55f, .45f, .33f));
            Placeholders.Prim(PrimitiveType.Capsule, t, new Vector3(0, .62f, -.05f), new Vector3(.45f, .5f, .45f), fur, name: "Body").transform.localRotation = Quaternion.Euler(90, 0, 0);
            var head = Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, .85f, .55f), new Vector3(.3f, .32f, .42f), fur, name: "Head").transform;
            Placeholders.Prim(PrimitiveType.Capsule, head, new Vector3(0, -.75f, .3f), new Vector3(.3f, .45f, .3f), cream, name: "Beard");
            foreach (float s in new[] { -1f, 1f })
            {
                Placeholders.Prim(PrimitiveType.Capsule, head, new Vector3(s * .35f, .6f, -.3f), new Vector3(.22f, .55f, .22f), horn, name: "Horn").transform.localRotation = Quaternion.Euler(-60, 0, s * 25f);
                Placeholders.Prim(PrimitiveType.Cube, head, new Vector3(s * .3f, .3f, .55f), new Vector3(.3f, .08f, .08f), Placeholders.Mat(new Color(.2f, .12f, .08f)), name: "Brow").transform.localRotation = Quaternion.Euler(0, 0, s * 18f);
                foreach (float z in new[] { .3f, -.38f }) Placeholders.Prim(PrimitiveType.Cylinder, t, new Vector3(s * .15f, .2f, z), new Vector3(.1f, .2f, .1f), cream, name: "Leg");
            }
            e.Anim = CharacterModel.Creature(e.transform, "goat", 1.1f, 6f);
            if (e.Anim) e.Anim.Drive = () => (e.stun > 0 ? "stunned" : null, e.speedNow);
            return e;
        }

        // a scruffy guard dog: lanky, sandy brown, one ear up, a collar (0.85 m)
        public static Enemy Dog(Transform parent, Vector3 a, Vector3 b)
        {
            var e = Body("Guard dog", parent, a, .85f, .35f); e.kind = Kind.Dog; e.a = a; e.b = b; e.hp = 2;
            e.patrolSpeed = 1.4f; e.chaseSpeed = 5f; e.sight = 8f; e.leash = 10f;
            var t = e.transform; var fur = new Material(Placeholders.Mat(new Color(.72f, .5f, .28f))); var dark = Placeholders.Mat(new Color(.12f, .1f, .08f));
            Placeholders.Prim(PrimitiveType.Capsule, t, new Vector3(0, .55f, -.05f), new Vector3(.32f, .45f, .32f), fur, name: "Body").transform.localRotation = Quaternion.Euler(90, 0, 0);
            var head = Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, .82f, .45f), new Vector3(.28f, .28f, .36f), fur, name: "Head").transform;
            Placeholders.Prim(PrimitiveType.Sphere, head, new Vector3(0, -.15f, .65f), new Vector3(.4f, .32f, .3f), dark, name: "Nose");
            Placeholders.Prim(PrimitiveType.Cube, head, new Vector3(-.4f, .6f, 0), new Vector3(.18f, .55f, .08f), fur, name: "Ear up");
            Placeholders.Prim(PrimitiveType.Cube, head, new Vector3(.45f, .1f, 0), new Vector3(.18f, .5f, .08f), fur, name: "Ear down").transform.localRotation = Quaternion.Euler(0, 0, 35);
            Placeholders.Prim(PrimitiveType.Cylinder, t, new Vector3(0, .68f, .3f), new Vector3(.3f, .04f, .3f), Placeholders.Mat(new Color(.4f, .22f, .12f)), name: "Collar");
            foreach (float s in new[] { -1f, 1f }) foreach (float z in new[] { .28f, -.36f }) Placeholders.Prim(PrimitiveType.Cylinder, t, new Vector3(s * .12f, .18f, z), new Vector3(.08f, .18f, .08f), fur, name: "Leg");
            Placeholders.Prim(PrimitiveType.Capsule, t, new Vector3(0, .7f, -.5f), new Vector3(.12f, .3f, .12f), fur, name: "Tail").transform.localRotation = Quaternion.Euler(-40, 0, 0);
            e.Anim = CharacterModel.Creature(e.transform, "guard_dog", 1.4f, 5f);
            if (e.Anim) e.Anim.Drive = () => (e.stun > 0 ? "stunned" : null, e.speedNow);
            return e;
        }

        // a small cute scorpion: amber shell, big eyes, pincers up, a curled tail (Meshy's model, moved by code like the scarab)
        public static Enemy Scorpion(Transform parent, Vector3 a, Vector3 b)
        {
            var e = Body("Scorpion", parent, a, .35f, .25f); e.kind = Kind.Scorpion; e.a = a; e.b = b; e.hp = 1;
            e.patrolSpeed = 1.2f; e.chaseSpeed = 3.4f; e.sight = 4.5f; e.leash = 9f;
            var t = e.transform; var shell = new Material(Placeholders.Mat(new Color(.9f, .5f, .15f)));
            Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, .12f, 0), new Vector3(.3f, .14f, .4f), shell, name: "Shell");
            Placeholders.Prim(PrimitiveType.Capsule, t, new Vector3(0, .32f, -.25f), new Vector3(.08f, .2f, .08f), shell, name: "Tail").transform.localRotation = Quaternion.Euler(-30, 0, 0);
            foreach (float s in new[] { -1f, 1f }) Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(s * .16f, .14f, .25f), new Vector3(.12f, .08f, .12f), shell, name: "Pincer");
            var model = StaticModel.Spawn("scorpion", e.transform, e.transform.position, e.transform.eulerAngles.y + 180f, .3f);
            if (model) { foreach (Transform c in e.transform) if (c != model.transform) c.gameObject.SetActive(false); model.AddComponent<Skitter>(); }
            return e;
        }
    }
}
