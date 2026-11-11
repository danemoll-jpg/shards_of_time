using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 7: Bethlehem's world pieces (World/Bethlehem.cs places them). All built in code (none is saved in a prefab).

    // Item 6: springy hay or a wool pile. Lamb Kira (Abilities.WoolBounce) landing on it bounces high; anyone else just lands.
    public sealed class BouncePad : MonoBehaviour
    {
        public float height = 5.5f;
        public static int Bounces { get; private set; }
        float hintAt = -10f;
        public void Bounce(Hero h)
        {
            h.Motor.Launch(height); Bounces++;
            Sfx.Play("hay_bounce", .55f, Random.Range(.95f, 1.1f));
            Placeholders.Puff(h.transform.position + Vector3.up * .1f, new Color(.95f, .82f, .45f));
            PlayLog.Line($"lamb Kira bounces off {name} ({height:0.0} m)");
        }
        void Update()
        {
            // a hint for the others: standing on it does nothing for them
            if (!Game.Playing || !Party.I || Time.time < hintAt + 8f) return;
            var pl = Party.I.Player; var g = pl.Motor.GroundCollider;
            if (!g || g.GetComponentInParent<BouncePad>() != this || pl.HasAbility(Abilities.WoolBounce)) return;
            hintAt = Time.time; Hud.Message("Soft, springy hay. A little lamb could bounce sky-high on this... (Kira)", 4f);
        }
    }

    // Item 6: a flock of sheep. It grazes round its home (or walks a loop with its shepherd); lamb Kira inside it (FlockHide, within
    // `radius` of its middle) is just another sheep to watchmen, dogs and goats. The sheep stare at her (she has a cat's face).
    public sealed class Flock : MonoBehaviour
    {
        public static readonly List<Flock> All = new List<Flock>();
        public Vector3[] path; public float speed = 1.1f, radius = 4.5f, wander = 7f;   // radius: lamb Kira within it is hidden (Round 7 check: 3.6 m let a turning flock leave her at its edge)
        public readonly List<Transform> Sheep = new List<Transform>();
        readonly List<CreatureAnimator> anims = new List<CreatureAnimator>(); readonly List<Vector3> offsets = new List<Vector3>(); readonly List<float> sheepSpeed = new List<float>();
        int leg; float pause, bleatAt, stareMsgAt = -100f; Vector3 home, goal; static int stares;
        public Vector3 Middle => transform.position;
        public static int HideCount { get; private set; }
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public static Flock Make(Transform parent, string name, Vector3 at, int count, Vector3[] path = null)
        {
            var go = new GameObject("Flock " + name); go.transform.SetParent(parent, false); go.transform.position = at;
            var f = go.AddComponent<Flock>(); f.home = at; f.goal = at; f.path = path;
            var rnd = new System.Random(name.GetHashCode());
            for (int i = 0; i < count; i++)
            {
                float a = i * 2.4f, r = 1.0f + 2.2f * (float)rnd.NextDouble();
                var off = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                var s = new GameObject("Sheep"); s.transform.SetParent(parent, false); s.transform.position = Game.OnGround(at.x + off.x, at.z + off.z);
                s.transform.rotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 360f, 0);
                var wool = Placeholders.Mat(new Color(.95f, .92f, .82f)); var black = Placeholders.Mat(new Color(.1f, .09f, .09f));
                Placeholders.Prim(PrimitiveType.Sphere, s.transform, new Vector3(0, .58f, 0), new Vector3(.75f, .62f, 1.05f), wool, name: "Wool");
                Placeholders.Prim(PrimitiveType.Sphere, s.transform, new Vector3(0, .62f, .55f), new Vector3(.3f, .32f, .38f), black, name: "Face");
                foreach (float x in new[] { -.18f, .18f }) foreach (float z in new[] { -.3f, .3f }) Placeholders.Prim(PrimitiveType.Cylinder, s.transform, new Vector3(x, .15f, z), new Vector3(.08f, .15f, .08f), black, name: "Leg");
                var an = CharacterModel.Creature(s.transform, "sheep", .9f, 2.6f);
                int k = f.Sheep.Count; f.Sheep.Add(s.transform); f.offsets.Add(off); f.sheepSpeed.Add(0); f.anims.Add(an);
                if (an) an.Drive = () => (null, f.sheepSpeed[k]);
            }
            return f;
        }

        // is this hero hidden in a flock right now? (lamb Kira only)
        public static Flock Hiding(Hero h)
        {
            if (!h || h.Stats.displayName != "Kira" || !h.HasAbility(Abilities.FlockHide)) return null;
            foreach (var f in All) { var d = h.transform.position - f.Middle; d.y = 0; if (d.magnitude < f.radius && Mathf.Abs(h.transform.position.y - f.Middle.y) < 1.5f) return f; }
            return null;
        }
        public static bool Hides(Hero h) { bool hid = Hiding(h); if (hid) HideCount++; return hid; }

        void Update()
        {
            if (Time.deltaTime <= 0 || !Game.Playing && !Game.InCutscene) return;
            // the middle moves: along its path (a shepherd driving it), or from one grazing spot to the next round home
            if (pause > 0) pause -= Time.deltaTime;
            else
            {
                Vector3 to = path != null && path.Length > 1 ? path[(leg + 1) % path.Length] : goal;
                var d = to - transform.position; d.y = 0;
                if (d.magnitude < .4f)
                {
                    if (path != null && path.Length > 1) { leg = (leg + 1) % path.Length; pause = 1.5f; }
                    else { var r = Random.insideUnitCircle * wander; goal = home + new Vector3(r.x, 0, r.y); pause = Random.Range(3f, 7f); }
                }
                else { var p = transform.position + d.normalized * speed * Time.deltaTime; p.y = Game.OnGround(p.x, p.z).y; transform.position = p; }
            }
            // each sheep trots to its place round the middle (they stare at lamb Kira when she's close)
            var kira = Party.I ? Party.I.Heroes.Find(h => h.Stats.displayName == "Kira") : null;
            bool kiraIn = kira && kira.HasAbility(Abilities.FlockHide) && Vector3.Distance(kira.transform.position, Middle) < radius + 2f;
            for (int i = 0; i < Sheep.Count; i++)
            {
                var s = Sheep[i]; var want = Middle + offsets[i]; var d = want - s.position; d.y = 0;
                float v = d.magnitude > .3f ? Mathf.Min(2.6f, d.magnitude * 1.2f) : 0f; sheepSpeed[i] = v;
                if (v > 0) { var p = s.position + d.normalized * v * Time.deltaTime; p.y = Egypt.Ground(p.x, p.z, p.y + 2f); s.position = p; s.rotation = Quaternion.RotateTowards(s.rotation, Quaternion.LookRotation(d), 200f * Time.deltaTime); }
                else if (kiraIn) { var k = kira.transform.position - s.position; k.y = 0; if (k.sqrMagnitude > .01f) s.rotation = Quaternion.RotateTowards(s.rotation, Quaternion.LookRotation(k), 120f * Time.deltaTime); }
            }
            if (kiraIn && kira.IsPlayer && Time.time > stareMsgAt + 40f)
            {
                stareMsgAt = Time.time; stares++;
                Hud.Message(stares == 1 ? "The sheep stop chewing and stare at the lamb with a cat's face. One of them blinks. Slowly." : "The flock closes round lamb Kira. Nobody will notice her in here.", 4f);
                Sfx.Play("sheep_bleat", .5f, .8f);
            }
            if (Time.time > bleatAt) { bleatAt = Time.time + Random.Range(6f, 14f); if (Party.I && Vector3.Distance(Party.I.Player.transform.position, Middle) < 25f) Sfx.Play("sheep_bleat", .25f, Random.Range(.9f, 1.2f)); }
        }
    }

    // Item 4: the golden vortex hovering over the barn, its beam down through the roof (everyone in the world calls it "the star")
    public sealed class StarRing : MonoBehaviour
    {
        readonly List<Transform> rings = new List<Transform>();
        public static StarRing Make(Transform parent, Vector3 at, float groundY)
        {
            var go = new GameObject("The star (the vortex)"); go.transform.SetParent(parent, false); go.transform.position = at;
            var s = go.AddComponent<StarRing>();
            Color[] cols = { new Color(1f, .82f, .35f), new Color(1f, .93f, .6f), new Color(1f, .7f, .25f) };
            for (int i = 0; i < 3; i++)
            {
                var r = new GameObject("Ring" + i).transform; r.SetParent(go.transform, false);
                float rad = 5.5f - i * 1.4f; int n = 24 - i * 4;
                for (int k = 0; k < n; k++)
                {
                    float a = k * Mathf.PI * 2 / n;
                    var b = Placeholders.Prim(PrimitiveType.Cube, r, new Vector3(Mathf.Cos(a) * rad, 0, Mathf.Sin(a) * rad), new Vector3(.5f, .35f, rad * 6.3f / n), Placeholders.Mat(cols[i], "Glow"), name: "Glow");
                    b.transform.localRotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0);
                }
                r.localRotation = Quaternion.Euler(70 + i * 12, i * 40, 0); s.rings.Add(r);
            }
            Placeholders.Prim(PrimitiveType.Sphere, go.transform, Vector3.zero, Vector3.one * 2.2f, Placeholders.Mat(new Color(1f, .95f, .75f), "Glow"), name: "Heart");
            float h = at.y - groundY;
            var beam = Placeholders.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0, -h / 2, 0), new Vector3(2.2f, h / 2, 2.2f), Placeholders.Mat(new Color(1f, .85f, .4f, .16f), "Water"), name: "Beam");
            beam.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var l = new GameObject("Star light"); l.transform.SetParent(go.transform, false); l.transform.localPosition = Vector3.down * 3f;
            var lt = l.AddComponent<Light>(); lt.type = LightType.Point; lt.range = 40; lt.intensity = 2.5f; lt.color = new Color(1f, .85f, .5f); lt.shadows = LightShadows.None;
            return s;
        }
        void Update() { for (int i = 0; i < rings.Count; i++) rings[i].Rotate(0, (i % 2 == 0 ? 1 : -1) * (25f + i * 15f) * Time.deltaTime, 0, Space.Self); }
    }

    // Item 11: the starry sky: a dome of small glowing quads far away, following the camera (one mesh, no shadows)
    public sealed class StarDome : MonoBehaviour
    {
        public static StarDome Make(Transform parent, int count = 900, float r = 1800f)
        {
            var go = new GameObject("Stars"); go.transform.SetParent(parent, false);
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>(); var rnd = new System.Random(3);
            for (int i = 0; i < count; i++)
            {
                var d = new Vector3((float)rnd.NextDouble() * 2 - 1, (float)rnd.NextDouble() * .92f + .08f, (float)rnd.NextDouble() * 2 - 1).normalized;
                if (d.y < .06f) continue;
                float size = (rnd.NextDouble() < .06 ? 9f : 3.5f) + (float)rnd.NextDouble() * 3f;
                var right = Vector3.Cross(d, Vector3.up).normalized * size; var up = Vector3.Cross(right, d).normalized * size;
                var p = d * r; int k = v.Count;
                v.AddRange(new[] { p - right - up, p + right - up, p + right + up, p - right + up });
                var col = Color.Lerp(new Color(.85f, .9f, 1f), new Color(1f, .95f, .8f), (float)rnd.NextDouble());
                c.AddRange(new[] { col, col, col, col });
                t.AddRange(new[] { k, k + 2, k + 1, k, k + 3, k + 2, k, k + 1, k + 2, k, k + 2, k + 3 });
            }
            var m = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; m.SetVertices(v); m.SetColors(c); m.SetTriangles(t, 0); m.bounds = new Bounds(Vector3.zero, Vector3.one * r * 3);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = Placeholders.Mat(new Color(.95f, .95f, 1f), "Glow"); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            return go.AddComponent<StarDome>();
        }
        void LateUpdate() { if (CameraRig.I) transform.position = CameraRig.I.Cam.transform.position; }
    }

    // Item 4: the Wise Men's caravan. Three camels, a Wise Man on each (with a wrapped present), walk the road toward "the star" the
    // whole time (end to end and back). At some stops they argue about the way or check the map; near them you hear a line of it.
    public sealed class Caravan : MonoBehaviour
    {
        public static Caravan I { get; private set; }
        public Vector3[] road; public float speed = 1.7f, spacing = 5.5f;
        public int Stops { get; private set; }
        public int LinesHeard { get; private set; }
        public float Along { get; private set; }
        public string Doing { get; private set; } = "walk";
        readonly List<Transform> camels = new List<Transform>(); readonly List<Transform> riders = new List<Transform>(); readonly List<CreatureAnimator> camelAnims = new List<CreatureAnimator>();
        readonly List<CreatureAnimator> riderAnims = new List<CreatureAnimator>();
        float length, stopT, nextStop = 45f, lineAt = -100f; int dir = 1, lineN; bool stopped;
        public static readonly string[] Models = { "wiseman_santa", "wiseman_crown", "wiseman_old" };
        static readonly string[] Gifts = { "GiftRed", "GiftGreen", "GiftBlue" };
        static readonly string[][] Lines =
        {
            new[] { "\"It's definitely that star.\"", "\"You said that about the last three stars.\"", "\"This one has a BEAM, Balthazar.\"" },
            new[] { "\"Left at the olive grove.\"", "\"There is no olive grove.\"", "\"Then we're early.\"" },
            new[] { "\"Is that a star, or a very large lamp?\"", "\"Stars don't hover over barns.\"", "\"This one does. It's special.\"" },
            new[] { "\"Remind me why we brought presents with bows on?\"", "\"Presentation matters.\"" },
            new[] { "(The one in the Santa hat studies the map upside down.)", "\"We are... here.\"", "\"That's a stain.\"" },
        };
        void OnEnable() => I = this;
        void OnDisable() { if (I == this) I = null; }

        public static Caravan Make(Transform parent, Vector3[] road)
        {
            var go = new GameObject("The Wise Men's caravan"); go.transform.SetParent(parent, false);
            var c = go.AddComponent<Caravan>(); c.road = road;
            for (int i = 1; i < road.Length; i++) c.length += Flat(road[i] - road[i - 1]).magnitude;
            for (int i = 0; i < 3; i++)
            {
                var camel = new GameObject("Caravan camel " + i).transform; camel.SetParent(go.transform, false);
                var fur = Placeholders.Mat(new Color(.8f, .6f, .36f));
                Placeholders.Prim(PrimitiveType.Capsule, camel, new Vector3(0, 1.55f, 0), new Vector3(1f, 1.1f, 1f), fur, name: "Body").transform.localRotation = Quaternion.Euler(90, 0, 0);
                Placeholders.Prim(PrimitiveType.Sphere, camel, new Vector3(0, 2.15f, -.1f), new Vector3(.8f, .7f, .9f), fur, name: "Hump");
                foreach (float x in new[] { -.3f, .3f }) foreach (float z in new[] { -.6f, .65f }) Placeholders.Prim(PrimitiveType.Capsule, camel, new Vector3(x, .55f, z), new Vector3(.18f, .55f, .18f), fur, name: "Leg");
                Placeholders.Prim(PrimitiveType.Capsule, camel, new Vector3(0, 2.3f, 1.2f), new Vector3(.32f, .55f, .32f), fur, name: "Neck").transform.localRotation = Quaternion.Euler(35, 0, 0);
                Placeholders.Prim(PrimitiveType.Capsule, camel, new Vector3(0, 2.75f, 1.6f), new Vector3(.32f, .38f, .32f), fur, name: "Snout").transform.localRotation = Quaternion.Euler(90, 0, 0);
                var an = CharacterModel.Creature(camel, "camel", 1.4f, 3.2f);
                float top = 2.35f;
                if (an)
                {
                    an.Drive = () => (null, c.stopped ? 0f : c.speed);
                    var b = new Bounds(camel.position, Vector3.zero); foreach (var r in an.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds); top = b.size.y * .8f;
                }
                c.camelAnims.Add(an);
                // a plain cream blanket on its back (the film's Wise Men ride plainer camels than Egypt's)
                // Fix 7.1: the real (sitting) Wise Men sit on the camel's own saddle blanket; the cream block only goes under the placeholders
                bool real = CharacterModel.Exists(Models[i]) && Resources.LoadAll<AnimationClip>("Characters/Clips/sit").Length > 0;
                if (!real) Placeholders.Prim(PrimitiveType.Cube, camel, new Vector3(0, top - .25f, -.1f), new Vector3(1.15f, .7f, 1.0f), Placeholders.Mat(new Color(.9f, .85f, .72f)), name: "Blanket");
                var rider = new GameObject("Wise Man " + Models[i]).transform; rider.SetParent(camel, false); rider.localPosition = new Vector3(0, real ? top - .42f : top - .05f, -.15f);
                c.riders.Add(rider); c.riderAnims.Add(Rider(rider, i));
                var gift = WorldArt.Prop("bethlehem_props", Gifts[i], rider, rider.position + rider.forward * .45f + Vector3.up * .55f, 0, 1f);
                if (gift) gift.transform.SetParent(rider, true);
                c.camels.Add(camel);
            }
            c.Place();
            return c;
        }

        // a Wise Man: his model sitting (Dan's Mixamo "Sitting Idle"), else a placeholder in robe and hat / crown
        static CreatureAnimator Rider(Transform t, int i)
        {
            Color robe = i == 0 ? new Color(.5f, .14f, .12f) : i == 1 ? new Color(.95f, .9f, .78f) : new Color(.35f, .45f, .2f);
            var skin = Placeholders.Mat(new Color(.82f, .62f, .48f));
            Placeholders.Prim(PrimitiveType.Capsule, t, new Vector3(0, .55f, 0), new Vector3(.55f, .5f, .5f), Placeholders.Mat(robe), name: "Robe");
            var head = Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.2f, 0), new Vector3(.34f, .36f, .34f), skin, name: "Head").transform;
            Placeholders.Prim(PrimitiveType.Sphere, head, new Vector3(0, -.45f, .25f), new Vector3(.8f, .7f, .55f), Placeholders.Mat(i == 2 ? new Color(.96f, .96f, .94f) : i == 0 ? new Color(.1f, .08f, .07f) : new Color(.45f, .28f, .15f)), name: "Beard");
            if (i == 0)
            {
                Placeholders.Prim(PrimitiveType.Cylinder, head, new Vector3(0, .5f, -.05f), new Vector3(1.05f, .35f, 1.05f), Placeholders.Mat(new Color(.85f, .1f, .12f)), name: "Santa hat");
                Placeholders.Prim(PrimitiveType.Sphere, head, new Vector3(.3f, .85f, -.2f), new Vector3(.3f, .3f, .3f), Placeholders.Mat(Color.white), name: "Pom-pom");
            }
            else Placeholders.Prim(PrimitiveType.Cylinder, head, new Vector3(0, .5f, 0), new Vector3(.95f, .25f, .95f), WorldArt.Mat("Gold"), name: "Crown");
            if (!CharacterModel.Exists(Models[i])) return null;
            var probe = Resources.LoadAll<AnimationClip>("Characters/Clips/sit");
            if (probe.Length == 0) return null;   // no sitting clip yet: a standing model on a camel looks wrong, keep the placeholder
            var an = CharacterModel.Creature(t, Models[i], 1.2f, 3f);
            if (an) { an.Drive = () => (I && I.Doing == "argue" && an.Has("argue") ? "argue" : I && I.Doing == "map" && an.Has("map") && an == I.riderAnims[0] ? "map" : "sit", 0f); an.transform.localPosition = new Vector3(0, -.45f, 0); }
            return an;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
        Vector3 PointAt(float s, out Vector3 fwd)
        {
            s = Mathf.Clamp(s, 0, length);
            for (int i = 1; i < road.Length; i++)
            {
                float seg = Flat(road[i] - road[i - 1]).magnitude;
                if (s <= seg || i == road.Length - 1) { fwd = Flat(road[i] - road[i - 1]).normalized; var p = Vector3.Lerp(road[i - 1], road[i], Mathf.Clamp01(s / seg)); return Game.OnGround(p.x, p.z); }
                s -= seg;
            }
            fwd = Vector3.forward; return road[0];
        }
        void Place()
        {
            for (int i = 0; i < camels.Count; i++)
            {
                var p = PointAt(Along - dir * i * spacing, out var f);
                camels[i].position = p; if (f.sqrMagnitude > .01f) camels[i].rotation = Quaternion.LookRotation(f * dir);
            }
        }

        void Update()
        {
            if (Time.deltaTime <= 0 || !Game.Playing) return;
            if (stopped)
            {
                stopT -= Time.deltaTime;
                // they turn to each other while they argue
                for (int i = 0; i < riders.Count; i++) riders[i].localRotation = Quaternion.Euler(0, Doing == "argue" ? Mathf.Sin(Time.time * 2.2f + i) * 35f + (i == 0 ? 40 : i == 2 ? -40 : 0) : 0, 0);
                if (stopT <= 0) { stopped = false; Doing = "walk"; nextStop = Along + dir * Random.Range(55f, 90f); foreach (var r in riders) r.localRotation = Quaternion.identity; }
            }
            else
            {
                Along += dir * speed * Time.deltaTime;
                if (Along >= length + spacing * 2 || Along <= -spacing * 2) { dir = -dir; Along = Mathf.Clamp(Along, -spacing * 2, length + spacing * 2); nextStop = Along + dir * 30f; }
                if ((dir > 0 && Along >= nextStop) || (dir < 0 && Along <= nextStop)) { stopped = true; Stops++; stopT = Random.Range(7f, 11f); Doing = Stops % 2 == 0 ? "map" : "argue"; }
                Place();
            }
            // a line of their argument when you're close
            if (!Party.I) return;
            float near = camels.Min(c => Vector3.Distance(c.position, Party.I.Player.transform.position));
            if (near < 12f && Time.time > lineAt + 9f)
            {
                lineAt = Time.time; var l = Lines[lineN % Lines.Length]; lineN++; LinesHeard++;
                Hud.Message("The Wise Men: " + string.Join(" ", l), 6f);
                PlayLog.Line($"the Wise Men ({Doing}): {l[0]}");
            }
        }
        public Vector3 Lead => camels.Count > 0 ? camels[0].position : transform.position;
    }

    // Item 4: the inn ("no room"): its sign and the voice through the door
    public sealed class NoRoom : MonoBehaviour
    {
        public static int Told { get; private set; }
        float at = -100f;
        static readonly string[] Voices =
        {
            "A voice through the door: \"No room! We're full. Census week.\"",
            "\"Still no room. Try the barn, everybody else does.\"",
            "\"Look, I've got a family of four in the broom cupboard. NO. ROOM.\"",
        };
        void Update()
        {
            if (!Game.Playing || !Party.I || Time.time < at + 12f) return;
            if (Vector3.Distance(Party.I.Player.transform.position, transform.position) > 3.2f) return;
            at = Time.time; Hud.Message(Voices[Told % Voices.Length], 5f); Told++; Sfx.Play("door_knock", .5f);
            PlayLog.Line("the inn: no room");
        }
    }

    public static class Stealth
    {
        // Item 5 / 6: anyone hunting the player (watchmen, dogs, goats) ignores lamb Kira in a flock
        public static bool Hidden(Hero h) => Flock.Hiding(h);
    }
}
