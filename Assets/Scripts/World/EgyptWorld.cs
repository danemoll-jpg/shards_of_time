using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 6: Egypt's world pieces (World/Egypt.cs places them). All built in code (none is saved in a prefab).

    // Item 8: a cat-shaped plate in front of a door. Kira (any form) standing on it wakes the mechanism: the door sinks into the floor
    // (saved in WorldProgress.opened). Anyone else gets a hint.
    public sealed class CatPlate : MonoBehaviour
    {
        public static readonly List<CatPlate> All = new List<CatPlate>();
        public string id; public Transform door; public bool Open { get; private set; }
        float onFor, hintAt = -10f, sink; Vector3 doorAt; Transform eyes;
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public static CatPlate Make(Transform parent, Vector3 at, string id, Transform door, WorldProgress prog)
        {
            var go = new GameObject("Cat plate " + id); go.transform.SetParent(parent, false); go.transform.position = at;
            Placeholders.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0, .03f, 0), new Vector3(1.6f, .03f, 1.6f), WorldArt.Mat("Gold"), name: "Plate");
            Placeholders.Prim(PrimitiveType.Sphere, go.transform, new Vector3(0, .07f, .1f), new Vector3(.7f, .04f, .6f), WorldArt.Mat("Dark"), name: "Cat head");
            foreach (float s in new[] { -1f, 1f }) Placeholders.Prim(PrimitiveType.Cube, go.transform, new Vector3(s * .22f, .07f, .45f), new Vector3(.2f, .04f, .25f), WorldArt.Mat("Dark"), name: "Ear").transform.localRotation = Quaternion.Euler(0, s * 25, 0);
            var p = go.AddComponent<CatPlate>(); p.id = id; p.door = door; p.doorAt = door ? door.position : Vector3.zero;
            if (prog.opened.Contains(id)) { p.Open = true; if (door) door.gameObject.SetActive(false); }
            return p;
        }

        void Update()
        {
            if (Open)
            {
                if (door && door.gameObject.activeSelf)
                {
                    sink += Time.deltaTime * 1.6f; door.position = doorAt + Vector3.down * sink;
                    if (sink > door.lossyScale.y + .1f) door.gameObject.SetActive(false);
                }
                return;
            }
            if (!Game.Playing || !Party.I) return;
            var pl = Party.I.Player; var d = pl.transform.position - transform.position; d.y = 0;
            bool on = d.magnitude < .9f && Mathf.Abs(pl.transform.position.y - transform.position.y) < .6f;
            if (!on) { onFor = 0; return; }
            if (pl.Stats.displayName != "Kira")
            {
                if (Time.time > hintAt + 5f) { hintAt = Time.time; Hud.Message("Nothing happens. The plate is shaped for a cat's paws... (Kira)", 4f); }
                return;
            }
            onFor += Time.deltaTime;
            if (onFor < .5f) return;
            Open = true; var prog = SaveGame.Current.World(Game.World.worldId); if (!prog.opened.Contains(id)) prog.opened.Add(id);
            Sfx.Play("stone_grind", .6f); Sfx.Play("necklace", .3f, 1.4f);
            Hud.Message("The cat statue's eyes glow... the door opens for Kira!", 4f);
            PlayLog.Line($"cat plate {id}: Kira opened it");
            SaveGame.Save("cat door " + id);
        }
    }

    // Item 7: a merchant at a stall (placeholder figure: robe, headcloth, a basket on the head)
    public sealed class Merchant : MonoBehaviour
    {
        public static void Make(Transform parent, Vector3 at, float yaw, int kind)
        {
            var go = new GameObject("Merchant"); go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            var robe = Placeholders.Mat(kind == 1 ? new Color(.95f, .9f, .8f) : new Color(.55f, .75f, .85f)); var skin = Placeholders.Mat(new Color(.72f, .5f, .35f));
            Placeholders.Prim(PrimitiveType.Capsule, go.transform, new Vector3(0, .85f, 0), new Vector3(.55f, .85f, .45f), robe, name: "Robe");
            Placeholders.Prim(PrimitiveType.Sphere, go.transform, new Vector3(0, 1.82f, 0), new Vector3(.32f, .34f, .32f), skin, name: "Head");
            Placeholders.Prim(PrimitiveType.Sphere, go.transform, new Vector3(0, 1.95f, -.03f), new Vector3(.36f, .22f, .36f), Placeholders.Mat(new Color(.95f, .95f, .9f)), name: "Headcloth");
            if (kind == 2) WorldArt.Prop("egypt_props", "Basket", go.transform, at + Vector3.up * 2.08f, 0, .7f);
            go.AddComponent<Merchant>();
        }
        void Update() { transform.GetChild(1).localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * .8f + transform.position.x) * 25f, 0); }
    }

    // Item 7: a worker hauling a stone (rides along with it: a child of the moving block)
    public sealed class Worker : MonoBehaviour
    {
        Transform armL, armR;
        public static void Make(Transform parent, Transform with, Vector3 local)
        {
            var go = new GameObject("Worker"); go.transform.SetParent(with, false); go.transform.localPosition = local;
            var s = with.lossyScale; go.transform.localScale = new Vector3(1 / s.x, 1 / s.y, 1 / s.z);
            var kilt = Placeholders.Mat(new Color(.96f, .93f, .85f)); var skin = Placeholders.Mat(new Color(.68f, .46f, .32f));
            Placeholders.Prim(PrimitiveType.Capsule, go.transform, new Vector3(0, 1.15f, 0), new Vector3(.45f, .45f, .35f), skin, name: "Chest");
            Placeholders.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0, .62f, 0), new Vector3(.5f, .2f, .4f), kilt, name: "Kilt");
            foreach (float x in new[] { -.12f, .12f }) Placeholders.Prim(PrimitiveType.Capsule, go.transform, new Vector3(x, .3f, 0), new Vector3(.15f, .3f, .15f), skin, name: "Leg");
            Placeholders.Prim(PrimitiveType.Sphere, go.transform, new Vector3(0, 1.75f, 0), new Vector3(.3f, .32f, .3f), skin, name: "Head");
            Placeholders.Prim(PrimitiveType.Sphere, go.transform, new Vector3(0, 1.84f, -.02f), new Vector3(.33f, .2f, .33f), Placeholders.Mat(new Color(.2f, .25f, .55f)), name: "Headband");
            var w = go.AddComponent<Worker>();
            w.armL = Placeholders.Prim(PrimitiveType.Capsule, go.transform, new Vector3(-.3f, 1.25f, -.25f), new Vector3(.12f, .32f, .12f), skin, name: "Arm").transform;
            w.armR = Placeholders.Prim(PrimitiveType.Capsule, go.transform, new Vector3(.3f, 1.25f, -.25f), new Vector3(.12f, .32f, .12f), skin, name: "Arm").transform;
        }
        void Update()
        {
            // leaning into the haul, arms out toward the stone, a little bob
            transform.GetChild(0).localRotation = Quaternion.Euler(-12, 0, 0);
            float b = Mathf.Sin(Time.time * 6f) * 8f;
            if (armL) armL.localRotation = Quaternion.Euler(-70 + b, 0, 0); if (armR) armR.localRotation = Quaternion.Euler(-70 - b, 0, 0);
        }
    }

    // Item 7: a guard on patrol (light stealth, not combat). Walks a <-> b; if the player is within 10 m, in front of him (70 deg each
    // side) and nothing solid in between, he shouts and the party is sent back to the last checkpoint (no heart lost).
    public sealed class Guard : MonoBehaviour
    {
        public static readonly List<Guard> All = new List<Guard>();
        public Vector3 a, b; public float speed = 1.6f, sight = 8f, halfAngle = 50f;
        public int Spotted { get; private set; }
        bool toB = true; float pause, cool; Transform head;
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public static Guard Make(Transform parent, Vector3 a, Vector3 b, string name)
        {
            a = Egypt.OnGround(a.x, a.z); b = Egypt.OnGround(b.x, b.z);
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = a;
            var kilt = Placeholders.Mat(new Color(.96f, .93f, .85f)); var skin = Placeholders.Mat(new Color(.62f, .42f, .3f)); var blue = Placeholders.Mat(new Color(.15f, .3f, .7f));
            Placeholders.Prim(PrimitiveType.Capsule, go.transform, new Vector3(0, 1.2f, 0), new Vector3(.55f, .5f, .4f), skin, name: "Chest");
            Placeholders.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0, .7f, 0), new Vector3(.58f, .25f, .45f), kilt, name: "Kilt");
            foreach (float x in new[] { -.13f, .13f }) Placeholders.Prim(PrimitiveType.Capsule, go.transform, new Vector3(x, .32f, 0), new Vector3(.17f, .32f, .17f), skin, name: "Leg");
            var g = go.AddComponent<Guard>(); g.a = a; g.b = b;
            g.head = Placeholders.Prim(PrimitiveType.Sphere, go.transform, new Vector3(0, 1.85f, 0), new Vector3(.34f, .36f, .34f), skin, name: "Head").transform;
            Placeholders.Prim(PrimitiveType.Sphere, g.head, new Vector3(0, .25f, -.1f), new Vector3(1.15f, .7f, 1.2f), blue, name: "Nemes");
            foreach (float s in new[] { -1f, 1f }) Placeholders.Prim(PrimitiveType.Sphere, g.head, new Vector3(s * .2f, .05f, .42f), new Vector3(.14f, .1f, .06f), Placeholders.Mat(new Color(.08f, .08f, .1f)), name: "Eye");
            var spear = Placeholders.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(.38f, 1.3f, .1f), new Vector3(.05f, 1.2f, .05f), WorldArt.Mat("Wood"), name: "Spear");
            Placeholders.Prim(PrimitiveType.Cube, spear.transform, new Vector3(0, 1.05f, 0), new Vector3(2.4f, .15f, 1f), WorldArt.Mat("Gold"), name: "Spearhead");
            Placeholders.Prim(PrimitiveType.Cube, go.transform, new Vector3(-.4f, 1.2f, .15f), new Vector3(.08f, .7f, .5f), Placeholders.Mat(new Color(.75f, .55f, .3f)), name: "Shield");
            // his sight on the ground: a faint cone so the player can read it
            var cone = new GameObject("Sight"); cone.transform.SetParent(go.transform, false); cone.transform.localPosition = new Vector3(0, .05f, 0);
            cone.AddComponent<MeshFilter>().sharedMesh = SightMesh(g.sight, g.halfAngle);
            var mr = cone.AddComponent<MeshRenderer>(); mr.sharedMaterial = Placeholders.Mat(new Color(1f, .85f, .3f, .18f), "Water"); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g;
        }

        static Mesh SightMesh(float r, float half)
        {
            var v = new List<Vector3> { Vector3.zero }; var t = new List<int>(); int n = 16;
            for (int i = 0; i <= n; i++) { float a = Mathf.Lerp(-half, half, i / (float)n) * Mathf.Deg2Rad; v.Add(new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * r); }
            for (int i = 1; i <= n; i++) t.AddRange(new[] { 0, i, i + 1 });
            var m = new Mesh(); m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); return m;
        }

        public bool Sees(Vector3 p)
        {
            var eye = transform.position + Vector3.up * 1.7f; var to = p + Vector3.up * .8f - eye;
            var flat = new Vector3(to.x, 0, to.z);
            if (flat.magnitude > sight || Mathf.Abs(to.y) > 4f || Vector3.Angle(transform.forward, flat) > halfAngle) return false;
            return !Physics.Raycast(eye, to.normalized, to.magnitude - .4f, ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << Hero.EnemyLayer) | (1 << 4) | (1 << 2)), QueryTriggerInteraction.Ignore);
        }

        void Update()
        {
            if (!Game.Playing || !Party.I) return;
            cool -= Time.deltaTime;
            var goal = toB ? b : a; var d = goal - transform.position; d.y = 0;
            if (pause > 0) { pause -= Time.deltaTime; if (head) head.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * 1.6f) * 50f, 0); }
            else if (d.magnitude < .3f) { toB = !toB; pause = 1.6f; }
            else
            {
                transform.position += d.normalized * speed * Time.deltaTime;
                transform.position = new Vector3(transform.position.x, Egypt.Ground(transform.position.x, transform.position.z, transform.position.y + 3f), transform.position.z);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 240f * Time.deltaTime);
                if (head) head.localRotation = Quaternion.identity;
            }
            if (cool > 0 || Game.I.Busy || !Sees(Party.I.Player.transform.position)) return;
            cool = 4f; Spotted++;
            Sfx.Play("guard_halt", .7f);
            Hud.Message($"\"Halt! No wandering in the market!\" The guard marches {Party.I.Player.Stats.displayName} back.", 4f);
            PlayLog.Line($"{name} spotted {Party.I.Player.Stats.displayName}: back to checkpoint {SaveGame.Current.checkpoint}");
            Checkpoint.RespawnParty();
        }
    }

    // Item 7: quicksand (the quarry's pit, the tomb's sand hall): standing in it sinks you, then back to the last checkpoint (a heart)
    public sealed class Quicksand : BoxVolume
    {
        public static readonly List<Quicksand> All = new List<Quicksand>();
        public int Sunk { get; private set; }
        float inFor;
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        void Update()
        {
            if (!Game.Playing || !Party.I) return;
            var m = Party.I.Player.Motor; var box = Box; box.Expand(new Vector3(0, .6f, 0));
            bool inside = box.Contains(m.transform.position + Vector3.up * .05f) && m.Grounded && !m.OnPlatform;
            inFor = inside ? inFor + Time.deltaTime : 0;
            if (inside) m.SpeedScale = .35f;
            if (inFor > .9f) { inFor = 0; Sunk++; Sfx.Play("sand_sink", .6f); Hud.Message("Sinking sand! Stay on the stones.", 3f); Health.FellOut(); }
        }
    }

    // a pit's bottom: falling in = back to the checkpoint (a heart)
    public sealed class FallVolume : BoxVolume
    {
        void Update() { if (Game.Playing && Party.I && Contains(Party.I.Player.transform.position)) Health.FellOut(); }
    }

    // Item 7: a swinging blade (the tomb): a pendulum from the ceiling across the corridor; the blade hurts
    public sealed class SwingBlade : MonoBehaviour
    {
        public float phase, amp = 70f, period = 2.6f; Transform blade;
        public int Hits { get; private set; }
        public static SwingBlade Make(Transform parent, Vector3 pivot, float phase)
        {
            var go = new GameObject("Swinging blade"); go.transform.SetParent(parent, false); go.transform.position = pivot;
            var s = go.AddComponent<SwingBlade>(); s.phase = phase;
            Placeholders.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0, -1.2f, 0), new Vector3(.08f, 1.2f, .08f), WorldArt.Mat("Bark"), name: "Arm");
            s.blade = Placeholders.Prim(PrimitiveType.Cube, go.transform, new Vector3(0, -2.6f, 0), new Vector3(1.3f, .5f, .06f), WorldArt.Mat("Silver"), name: "Blade").transform;
            Placeholders.Prim(PrimitiveType.Sphere, go.transform, new Vector3(0, -2.6f, 0), new Vector3(.3f, .3f, .3f), WorldArt.Mat("Gold"), name: "Hub");
            return s;
        }
        void Update()
        {
            transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin((Time.time + phase) * Mathf.PI * 2 / period) * amp);
            if (!Game.Playing || !Party.I) return;
            var p = Party.I.Player.Motor.Center;
            if (Vector3.Distance(p, blade.position) < .85f && Health.Damage(1, blade.position, "a swinging blade")) Hits++;
        }
    }

    // Item 7: a dart wall (the tomb): every few seconds a dart flies across the corridor at chest height
    public sealed class DartWall : MonoBehaviour
    {
        public Vector3 dir; public float width, every; Transform dart; float t, flyT = -1;
        public static DartWall Make(Transform parent, Vector3 at, Vector3 dir, float width, float every)
        {
            var go = new GameObject("Dart wall"); go.transform.SetParent(parent, false); go.transform.position = at;
            var d = go.AddComponent<DartWall>(); d.dir = dir.normalized; d.width = width; d.every = every;
            for (int i = 0; i < 4; i++) Placeholders.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(-.05f, (i % 2) * .4f, (i - 1.5f) * .7f), new Vector3(.12f, .02f, .12f), WorldArt.Mat("Dark"), name: "Hole").transform.localRotation = Quaternion.Euler(0, 0, 90);
            d.dart = Placeholders.Prim(PrimitiveType.Cylinder, go.transform, Vector3.zero, new Vector3(.05f, .25f, .05f), WorldArt.Mat("Bark"), name: "Dart").transform;
            d.dart.rotation = Quaternion.FromToRotation(Vector3.up, d.dir); d.dart.gameObject.SetActive(false);
            return d;
        }
        void Update()
        {
            t += Time.deltaTime;
            if (flyT < 0 && t > every) { t = 0; flyT = 0; dart.gameObject.SetActive(true); Sfx.Play("trap_click", .4f); }
            if (flyT < 0) return;
            flyT += Time.deltaTime;
            float along = flyT * 12f; dart.position = transform.position + dir * along + Vector3.forward * Mathf.Sin(Time.time) * 0f;
            if (along > width) { flyT = -1; dart.gameObject.SetActive(false); return; }
            if (Game.Playing && Party.I && Vector3.Distance(Party.I.Player.Motor.Center, dart.position) < .7f) { Health.Damage(1, dart.position - dir, "a dart"); flyT = -1; dart.gameObject.SetActive(false); }
        }
    }

    // Item 7: the turning bridge (the tomb's ancient machinery): a quarter turn (3 s), then a pause (4 s) lined up with two of the room's
    // ledges, then the next quarter turn. Readable: walk on and off during the pause. (Round 6: a steady spin threw the bot off 30 times.)
    [DefaultExecutionOrder(-100)]   // moves before the heroes (as MovingPlatform), so riders are carried the same frame
    public sealed class StepTurner : MonoBehaviour
    {
        public float turnSeconds = 3f, pauseSeconds = 4f;
        public bool Paused { get; private set; } = true;
        float t, from; int step;
        void Start() { from = transform.eulerAngles.y; }
        void Update()
        {
            t += Time.deltaTime;
            if (Paused) { if (t > pauseSeconds) { Paused = false; t = 0; Sfx.Play("stone_grind", .35f, .8f); } return; }
            float k = Mathf.SmoothStep(0, 1, t / turnSeconds);
            transform.rotation = Quaternion.Euler(0, from + 90f * (step + k), 0);
            if (t >= turnSeconds) { step++; Paused = true; t = 0; transform.rotation = Quaternion.Euler(0, from + 90f * step, 0); }
        }
    }

    // Item 7: the counterweight lift (the tomb): while the stone weight sits on its golden plate, the lift rises to the high ledge
    [DefaultExecutionOrder(-100)]   // moves before the heroes (as MovingPlatform), so riders are carried the same frame
    public sealed class Counterweight : MonoBehaviour
    {
        public Transform weight, plate; public Vector3 low, high;
        public bool Up { get; private set; }
        void Update()
        {
            var w = weight.position; var p = plate.position;
            Up = Mathf.Abs(w.x - p.x) < 1.2f && Mathf.Abs(w.z - p.z) < 1.2f;
            // it rises with someone on it (the weight on its plate), and comes back down when they step off
            var g = Party.I ? Party.I.Player.Motor.GroundCollider : null; bool rider = g && g.transform == transform;
            var to = Up && (rider || Vector3.Distance(transform.position, low) > .05f && Party.I && Party.I.Player.transform.position.y > transform.position.y - .2f && Vector3.Distance(Party.I.Player.transform.position, transform.position) < 2.2f) ? high : low;
            transform.position = Vector3.MoveTowards(transform.position, to, 1.4f * Time.deltaTime);
        }
    }

    // Item 7: a model that is never rigged (the Great Sphinx with Kira's face, 22 m; the scarabs, moved by code): Resources/World/<name>.fbx
    // (Tools/Blender/static_export.py) + <name>_albedo.png, scaled to a height
    public static class StaticModel
    {
        public static GameObject Spawn(string name, Transform parent, Vector3 at, float yaw, float height)
        {
            var src = Resources.Load<GameObject>("World/" + name);
            if (!src) { PlayLog.Line($"{name}: no model (World/{name}.fbx): placeholder"); return null; }
            var go = Object.Instantiate(src, parent, false); go.name = "Model " + name;
            go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0) * src.transform.localRotation);
            var tex = Resources.Load<Texture2D>("World/" + name + "_albedo");
            var m = new Material(Placeholders.Mat(Color.white)) { name = name };
            if (tex) { m.SetTexture("_BaseMap", tex); m.mainTexture = tex; }
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", .15f);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) { var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = m; r.sharedMaterials = ms; }
            var b = new Bounds(go.transform.position, Vector3.zero); foreach (var r in go.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
            if (b.size.y > .001f && Mathf.Abs(b.size.y - height) > height * .03f) go.transform.localScale *= height / b.size.y;
            return go;
        }
    }

    // a scarab model's skitter: a quick bob and wiggle while it moves (it has no rig)
    public sealed class Skitter : MonoBehaviour
    {
        Vector3 last, baseLocal; Quaternion baseRot; void Start() { last = transform.position; baseLocal = transform.localPosition; baseRot = transform.localRotation; }
        void Update()
        {
            float v = (transform.position - last).magnitude / Mathf.Max(Time.deltaTime, 1e-4f); last = transform.position;
            float k = Mathf.Clamp01(v / 3f);
            transform.localPosition = baseLocal + Vector3.up * Mathf.Abs(Mathf.Sin(Time.time * 26f)) * .025f * k;
            transform.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * 26f) * 6f * k, Mathf.Sin(Time.time * 13f) * 4f * k) * baseRot;
        }
    }

    // Item 7 / the film: the first time the party reaches the Sphinx's front, SphinxGag plays (saved: once)
    public sealed class SphinxGagZone : MonoBehaviour
    {
        public static int Played { get; private set; }
        void Update()
        {
            if (!Game.Playing || !Party.I || Game.I.Busy) return;
            var prog = SaveGame.Current.World(Game.World.worldId);
            if (prog.opened.Contains("EG-sphinx-gag")) return;
            var d = Party.I.Player.transform.position - transform.position; d.y = 0;
            if (d.magnitude > 6f || !Party.I.Player.Motor.Grounded) return;
            prog.opened.Add("EG-sphinx-gag"); Played++;
            Game.I.StartCoroutine(Game.I.SphinxGag(transform.position));
        }
    }

    // Item 7: the judgmental camel. It waits at the north gate; the whole party rides it (stand on its saddle) across the dunes to the
    // Sphinx and back, waiting at each end. While waiting it stares at whichever hero you play, snorts, and turns away (the film's gag).
    [DefaultExecutionOrder(-100)]   // moves before the heroes (as MovingPlatform), so riders are carried the same frame
    public sealed class Camel : MonoBehaviour
    {
        public Vector3[] route; public float speed = 3.2f, wait = 5f;
        public int Snorts { get; private set; }
        public int Trips { get; private set; }
        public bool Moving => waitT <= 0;
        public CreatureAnimator Anim;
        Transform saddle;
        int leg; bool forward = true; float waitT = 3f, stareT, lookYaw, lookPitch, lonely; Transform neck, head, boneNeck, boneHead;
        enum Gag { None, Stare, Snort, Away } Gag gag; float gagT, gagCool;

        public static Camel Make(Transform parent, Vector3[] route)
        {
            var go = new GameObject("Camel"); go.transform.SetParent(parent, false); go.transform.position = route[0];
            go.transform.rotation = Quaternion.LookRotation(Flat(route[1] - route[0]));
            var c = go.AddComponent<Camel>(); c.route = route;
            var fur = Placeholders.Mat(new Color(.8f, .6f, .36f)); var dark = Placeholders.Mat(new Color(.45f, .3f, .18f));
            var body = Placeholders.Prim(PrimitiveType.Capsule, go.transform, new Vector3(0, 1.55f, 0), new Vector3(1.0f, 1.1f, 1.0f), fur, name: "Body"); body.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Placeholders.Prim(PrimitiveType.Sphere, go.transform, new Vector3(0, 2.15f, -.1f), new Vector3(.8f, .7f, .9f), fur, name: "Hump");
            foreach (float x in new[] { -.3f, .3f }) foreach (float z in new[] { -.6f, .65f }) Placeholders.Prim(PrimitiveType.Capsule, go.transform, new Vector3(x, .55f, z), new Vector3(.18f, .55f, .18f), fur, name: "Leg");
            c.neck = new GameObject("Neck").transform; c.neck.SetParent(go.transform, false); c.neck.localPosition = new Vector3(0, 1.8f, .9f);
            Placeholders.Prim(PrimitiveType.Capsule, c.neck, new Vector3(0, .35f, .25f), new Vector3(.32f, .55f, .32f), fur, name: "Neck").transform.localRotation = Quaternion.Euler(35, 0, 0);
            c.head = new GameObject("Head").transform; c.head.SetParent(c.neck, false); c.head.localPosition = new Vector3(0, .8f, .55f);
            Placeholders.Prim(PrimitiveType.Capsule, c.head, new Vector3(0, 0, .2f), new Vector3(.32f, .38f, .32f), fur, name: "Snout").transform.localRotation = Quaternion.Euler(90, 0, 0);
            foreach (float s in new[] { -1f, 1f })
            {
                Placeholders.Prim(PrimitiveType.Sphere, c.head, new Vector3(s * .14f, .12f, .12f), new Vector3(.12f, .09f, .1f), Placeholders.Mat(new Color(.95f, .95f, .92f)), name: "Eye");
                Placeholders.Prim(PrimitiveType.Cube, c.head, new Vector3(s * .14f, .17f, .14f), new Vector3(.16f, .05f, .1f), dark, name: "Lid");   // heavy, unimpressed lids
            }
            // the saddle: a flat cloth the party stands on (the motor rides moving platforms)
            var saddle = Placeholders.Prim(PrimitiveType.Cube, go.transform, new Vector3(0, 2.35f, -.1f), new Vector3(2f, .2f, 2.6f), Placeholders.Mat(new Color(.75f, .2f, .2f)), true, "Saddle");
            c.saddle = saddle.transform;

            Placeholders.Prim(PrimitiveType.Cube, saddle.transform, new Vector3(0, -.6f, 0), new Vector3(1.05f, 1f, .9f), Placeholders.Mat(new Color(.2f, .35f, .75f)), name: "Blanket");
            go.AddComponent<MovingPlatform>().Hold(go.transform.position);   // a == b: it moves itself; the motor carries riders with it
            var bodyCol = go.AddComponent<CapsuleCollider>(); bodyCol.center = new Vector3(0, 1.4f, 0); bodyCol.radius = .55f; bodyCol.height = 2.6f; bodyCol.direction = 2;
            go.layer = 2; saddle.layer = 2;   // its own ground ray (Egypt.Ground) must not find its saddle and climb it (Round 6: it flew off)
            c.Anim = CharacterModel.Creature(go.transform, "camel", 1.4f, 3.2f, "Saddle");
            if (c.Anim)
            {
                c.Anim.Drive = () => (null, c.Moving ? c.speed : 0f);
                // the model's own neck and head turn for the stare (after its clip: LateUpdate); the saddle sits on its hump
                foreach (var t in c.Anim.GetComponentsInChildren<Transform>()) { if (t.name.EndsWith("Neck")) c.boneNeck = t; else if (t.name.EndsWith("Head")) c.boneHead = t; }
                var b = new Bounds(go.transform.position, Vector3.zero); foreach (var r in c.Anim.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
                saddle.transform.localPosition = new Vector3(0, b.size.y * .8f - .1f, -.15f);
                PlayLog.Line($"the camel: model ({b.size.y:0.00} m tall), saddle at {saddle.transform.localPosition.y + .1f:0.00} m, neck bone {(c.boneNeck ? c.boneNeck.name : "none")}");
            }
            return c;
        }

        // setting off: the whole party in the saddle (as in the film), held in their seats while it walks; Jump hops off
        readonly Dictionary<Hero, Vector3> seats = new Dictionary<Hero, Vector3>();
        public int Seated => seats.Count;
        void SeatParty()
        {
            if (!saddle) return;
            seats.Clear(); int k = 0;
            foreach (var h in Party.I.Heroes)
            {
                var off = h.IsPlayer ? Vector3.zero : new Vector3(0, 0, k++ == 0 ? .75f : -.75f);
                seats[h] = transform.InverseTransformPoint(saddle.position + Vector3.up * (saddle.lossyScale.y / 2f + .02f)) + off;
                h.Motor.Teleport(transform.TransformPoint(seats[h]), transform.rotation);
            }
        }
        void HoldSeats()
        {
            foreach (var h in new List<Hero>(seats.Keys))
            {
                var to = transform.TransformPoint(seats[h]); var d = to - h.transform.position;
                if (h.IsPlayer && (Controls.Jump.WasPressedThisFrame() || d.magnitude > 2f)) { seats.Clear(); PlayLog.Line("off the camel"); return; }
                if (d.y < 0) d.y = 0;
                h.Motor.Cc.Move(d);
                h.transform.rotation = Quaternion.RotateTowards(h.transform.rotation, transform.rotation, 180f * Time.deltaTime);
            }
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v.sqrMagnitude > 1e-6f ? v : Vector3.forward; }

        public bool Riding(Hero h)
        {
            var g = h.Motor.GroundCollider; return g && saddle && g.transform.IsChildOf(saddle);   // on the saddle (not just against its side)
        }

        void Update()
        {
            if (Time.deltaTime <= 0 || !Game.Playing && !Game.InCutscene) return;
            bool riders = Party.I && Riding(Party.I.Player);
            if (waitT > 0)
            {
                waitT -= Time.deltaTime;
                if (riders && waitT > 1.2f) waitT = 1.2f;   // someone's up: off it goes
                // nobody on it: it waits for the party; left alone at the Sphinx end for 20 s, it walks home empty
                bool home = leg == 0, alone = Party.I && Vector3.Distance(Party.I.Player.transform.position, transform.position) > 25f;
                lonely = !riders && alone && !home ? lonely + Time.deltaTime : 0;
                if (!riders && waitT <= 0 && lonely < 20f) waitT = .5f;
                Judge();
                if (waitT <= 0) { gag = Gag.None; if (Party.I && riders) { Trips++; SeatParty(); PlayLog.Line($"the camel sets off with {Party.I.Player.Stats.displayName} on its saddle (the party aboard)"); } }
                return;
            }
            // walk the route (ping-pong), following the ground
            int next = forward ? leg + 1 : leg - 1;
            var to = route[next]; var d = Flat(to - transform.position);
            float step = speed * Time.deltaTime;
            if (new Vector3(to.x - transform.position.x, 0, to.z - transform.position.z).magnitude <= step)
            {
                leg = next; if (leg == route.Length - 1 || leg == 0) { forward = !forward; waitT = wait; Sfx.Play("camel_snort", .4f); if (seats.Count > 0) PlayLog.Line("the camel arrives"); seats.Clear(); }
                return;
            }
            var p = transform.position + d.normalized * step;
            p.y = Egypt.Ground(p.x, p.z, p.y + 4f);
            transform.position = p;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 60f * Time.deltaTime);
            HoldSeats();
            lookYaw = Mathf.MoveTowardsAngle(lookYaw, 0, 90f * Time.deltaTime); lookPitch = 0;
        }

        // the stare: turn the head to the hero you play, hold it, snort, turn away (then a while before the next one)
        void Judge()
        {
            if (!Party.I || !neck) return;
            var pl = Party.I.Player; var to = pl.transform.position - transform.position; to.y = 0;
            gagCool -= Time.deltaTime;
            if (gag == Gag.None && to.magnitude < 9f && gagCool <= 0 && !Riding(pl)) { gag = Gag.Stare; gagT = 0; }
            if (gag == Gag.None) { lookYaw = Mathf.MoveTowardsAngle(lookYaw, 0, 90f * Time.deltaTime); lookPitch = 0; return; }
            gagT += Time.deltaTime;
            float want = Mathf.Clamp(Mathf.DeltaAngle(transform.eulerAngles.y, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg), -100, 100);
            if (gag == Gag.Stare) { lookYaw = Mathf.MoveTowardsAngle(lookYaw, want, 120f * Time.deltaTime); if (gagT > 1.6f) { gag = Gag.Snort; gagT = 0; Snorts++; Sfx.Play("camel_snort", .7f); PlayLog.Line($"the camel stares at {pl.Stats.displayName}, snorts"); if (Snorts == 1) Hud.Message($"The camel looks {pl.Stats.displayName} up and down... and snorts.", 3.5f); } }
            else if (gag == Gag.Snort) { lookPitch = Mathf.Sin(gagT * 30f) * 4f; if (gagT > .7f) { gag = Gag.Away; gagT = 0; } }
            else { lookYaw = Mathf.MoveTowardsAngle(lookYaw, -Mathf.Sign(want) * 75f, 140f * Time.deltaTime); if (gagT > 2.2f) { gag = Gag.None; gagCool = 6f; } }
            lookPitch = gag == Gag.Away ? -12f : 0;
        }

        // the stare on the placeholder's neck, or added onto the model's neck / head bones after their clip
        void LateUpdate()
        {
            if (boneNeck)
            {
                boneNeck.rotation = Quaternion.AngleAxis(lookYaw * .55f, Vector3.up) * boneNeck.rotation;
                if (boneHead) boneHead.rotation = Quaternion.AngleAxis(lookYaw * .45f, Vector3.up) * Quaternion.AngleAxis(lookPitch, transform.right) * boneHead.rotation;
            }
            else if (neck) { neck.localRotation = Quaternion.Euler(lookPitch, lookYaw, 0); if (head) head.localRotation = Quaternion.Euler(lookPitch * 1.2f, 0, 0); }
        }
    }
}
