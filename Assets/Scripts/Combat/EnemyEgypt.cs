using UnityEngine;

namespace ShardsOfTime
{
    // Round 6, item 11: Egypt's enemies (Enemy.Kind.Mummy / Scarab / Guardian), placeholders until their models are in.
    //  Mummy: asleep (standing still) until someone comes within 6 m; then it shambles after the player (1.5 m/s) and grabs (touching
    //    hurts). It never wakes for mummy Kira (item 8: Abilities.MummyHush) and falls back asleep if she's the one near.
    //  Scarab: small and fast (6 m/s chase), one hit; in threes (ScarabSwarm).
    //  Guardian: a stone cat statue on a plinth; it wakes when you pass within 5 m, chases for 5 s, then walks back and turns to stone.
    public sealed partial class Enemy
    {
        public bool Awake { get; private set; }
        public int Wakes { get; private set; }
        float chaseFor; Quaternion homeRot;

        bool HushedBy(Hero h) => h.Stats.displayName == "Kira" && h.HasAbility(Abilities.MummyHush);

        Vector3 EgyptMove(Vector3 player, Vector3 pos)
        {
            var pl = Party.I.Player; var flat = player - pos; flat.y = 0; float dist = flat.magnitude;
            switch (kind)
            {
                case Kind.Mummy:
                {
                    bool hushed = HushedBy(pl);
                    if (!Awake && !hushed && dist < 6f && Mathf.Abs(player.y - pos.y) < 2.5f) { Awake = true; Wakes++; Sfx.Play("mummy_groan", .6f); PlayLog.Line($"a mummy wakes ({pl.Stats.displayName} came near)"); }
                    if (Awake && (hushed || dist > leash)) { Awake = false; PlayLog.Line(hushed ? "the mummy goes back to sleep (mummy Kira)" : "the mummy goes back to sleep"); }
                    Chasing = Awake;
                    if (!Awake) return Vector3.zero;
                    return dist > .3f ? flat.normalized * chaseSpeed : Vector3.zero;
                }
                case Kind.Guardian:
                {
                    var homeFlat = home - pos; homeFlat.y = 0;
                    if (!Awake && dist < 5f && Mathf.Abs(player.y - pos.y) < 2.5f) { Awake = true; Wakes++; chaseFor = 5f; Sfx.Play("stone_grind", .6f); PlayLog.Line("a temple guardian wakes"); }
                    if (!Awake) { transform.rotation = homeRot; return Vector3.zero; }
                    chaseFor -= Time.deltaTime; Chasing = chaseFor > 0;
                    if (Chasing) return dist > .3f ? flat.normalized * chaseSpeed : Vector3.zero;
                    if (homeFlat.magnitude < .3f) { Awake = false; transform.rotation = homeRot; return Vector3.zero; }   // back on its plinth: stone again
                    return homeFlat.normalized * patrolSpeed;
                }
                default:   // scarab
                {
                    bool was = Chasing; var m = PatrolMove(player, pos);
                    if (Chasing && !was) Sfx.Play("scarab_skitter", .45f, Random.Range(.9f, 1.15f));
                    return m;
                }
            }
        }

        static Enemy EgyptBody(string name, Transform parent, Vector3 at, float height, float radius, Kind k)
        {
            var e = Body(name, parent, at, height, radius); e.kind = k; e.a = at; e.b = at; return e;
        }

        // a mummy: bandaged, one eye peeking out, arms out (1.75 m)
        public static Enemy Mummy(Transform parent, Vector3 a, Vector3 b)
        {
            var e = EgyptBody("Mummy", parent, a, 1.75f, .38f, Kind.Mummy); e.b = b; e.hp = 3;
            e.patrolSpeed = 1.2f; e.chaseSpeed = 1.5f; e.sight = 6f; e.leash = 14f;
            var t = e.transform; var wrap = new Material(Placeholders.Mat(new Color(.88f, .82f, .68f))); var dark = Placeholders.Mat(new Color(.55f, .48f, .38f));
            Placeholders.Prim(PrimitiveType.Capsule, t, new Vector3(0, .95f, 0), new Vector3(.62f, .75f, .5f), wrap, name: "Body");
            Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.6f, 0), new Vector3(.42f, .44f, .42f), wrap, name: "Head");
            Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(.08f, 1.63f, .19f), new Vector3(.12f, .12f, .06f), Placeholders.Mat(new Color(1f, .7f, .15f), "Glow"), name: "Eye");
            for (int i = 0; i < 6; i++) Placeholders.Prim(PrimitiveType.Cylinder, t, new Vector3(0, .35f + i * .22f, 0), new Vector3(.66f, .015f, .54f), dark, name: "Band").transform.localRotation = Quaternion.Euler(i * 7 - 18, 0, i % 2 == 0 ? 8 : -8);
            foreach (float s in new[] { -1f, 1f }) Placeholders.Prim(PrimitiveType.Capsule, t, new Vector3(s * .34f, 1.2f, .32f), new Vector3(.16f, .35f, .16f), wrap, name: "Arm").transform.localRotation = Quaternion.Euler(80, 0, 0);
            e.Anim = CharacterModel.Creature(e.transform, "mummy", 1.2f, 1.5f); if (e.Anim) e.Anim.Drive = () => (e.stun > 0 ? "stunned" : null, e.speedNow);
            return e;
        }

        // scarabs: three small iridescent beetles (0.4 m) patrolling between a and b
        public static void ScarabSwarm(Transform parent, Vector3 a, Vector3 b)
        {
            for (int i = 0; i < 3; i++)
            {
                var off = new Vector3((i - 1) * 1.2f, 0, (i % 2) * 1.1f);
                var e = EgyptBody("Scarab", parent, a + off, .4f, .28f, Kind.Scarab); e.b = b + off; e.hp = 1;
                e.patrolSpeed = 2.4f; e.chaseSpeed = 5f; e.sight = 7f; e.leash = 14f;   // Round 6 tuning: 22 hits in the first full bot run
                var t = e.transform; var shell = new Material(Placeholders.Mat(new Color(.1f, .45f, .55f)));
                Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, .25f, -.03f), new Vector3(.5f, .32f, .62f), shell, name: "Shell");
                Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, .2f, .3f), new Vector3(.3f, .22f, .22f), Placeholders.Mat(new Color(.12f, .3f, .35f)), name: "Head");
                Placeholders.Prim(PrimitiveType.Cube, t, new Vector3(0, .3f, -.03f), new Vector3(.02f, .3f, .6f), WorldArt.Mat("Gold"), name: "Seam");
                foreach (float s in new[] { -1f, 1f }) foreach (float z in new[] { -.15f, .05f, .22f })
                        Placeholders.Prim(PrimitiveType.Capsule, t, new Vector3(s * .28f, .1f, z), new Vector3(.05f, .12f, .05f), WorldArt.Mat("Dark"), name: "Leg").transform.localRotation = Quaternion.Euler(0, 0, s * 60f);
                var model = StaticModel.Spawn("scarab", e.transform, e.transform.position, e.transform.eulerAngles.y + 180f, .4f);   // Round 6: Meshy's scarab, moved by code
                if (model)
                {
                    foreach (Transform c in e.transform) if (c != model.transform) c.gameObject.SetActive(false);
                    model.AddComponent<Skitter>();
                }
            }
        }

        // a temple guardian: a stone jackal-cat on its plinth (1.7 m), wakes when you pass
        public static Enemy Guardian(Transform parent, Vector3 at, float yaw)
        {
            var e = EgyptBody("Temple guardian", parent, at, 1.7f, .45f, Kind.Guardian); e.hp = 3;
            e.patrolSpeed = 2.4f; e.chaseSpeed = 4.6f; e.sight = 5f; e.leash = 16f;
            e.transform.rotation = Quaternion.Euler(0, yaw, 0); e.homeRot = e.transform.rotation;
            var t = e.transform; var stone = new Material(Placeholders.Mat(new Color(.82f, .66f, .42f))); var gold = WorldArt.Mat("Gold");
            Placeholders.Prim(PrimitiveType.Capsule, t, new Vector3(0, .85f, -.1f), new Vector3(.55f, .6f, .55f), stone, name: "Body").transform.localRotation = Quaternion.Euler(90, 0, 0);
            Placeholders.Prim(PrimitiveType.Capsule, t, new Vector3(0, 1.25f, .35f), new Vector3(.3f, .4f, .3f), stone, name: "Neck").transform.localRotation = Quaternion.Euler(-20, 0, 0);
            Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.55f, .5f), new Vector3(.34f, .32f, .4f), stone, name: "Head");
            foreach (float s in new[] { -1f, 1f })
            {
                Placeholders.Prim(PrimitiveType.Cube, t, new Vector3(s * .12f, 1.82f, .45f), new Vector3(.1f, .28f, .06f), stone, name: "Ear").transform.localRotation = Quaternion.Euler(0, 0, s * -12f);
                Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(s * .1f, 1.6f, .68f), new Vector3(.08f, .05f, .04f), Placeholders.Mat(new Color(.25f, .6f, 1f), "Glow"), name: "Eye");
                foreach (float z in new[] { .3f, -.45f }) Placeholders.Prim(PrimitiveType.Cylinder, t, new Vector3(s * .18f, .3f, z), new Vector3(.13f, .3f, .13f), stone, name: "Leg");
            }
            Placeholders.Prim(PrimitiveType.Cube, t, new Vector3(0, 1.2f, .45f), new Vector3(.4f, .1f, .25f), gold, name: "Collar");
            e.Anim = CharacterModel.Creature(e.transform, "guardian", 2.4f, 4.6f); if (e.Anim) e.Anim.Drive = () => (e.stun > 0 ? "stunned" : e.Awake ? null : "idle", e.Awake ? e.speedNow : 0f);
            return e;
        }
    }
}
