using System.Collections.Generic;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 4: the Ice Age's world pieces (World/IceAge.cs places them). Systems, not one-offs: each works anywhere it is put.

    // Warm steam from a volcano vent: inside the box a character rises at `lift` m/s (CharacterMotor), so a vent is a vertical route.
    public sealed class UpdraftVolume : BoxVolume
    {
        public static readonly List<UpdraftVolume> All = new List<UpdraftVolume>();
        public float lift = 9f, accel = 45f;
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public static UpdraftVolume At(Vector3 p) { foreach (var v in All) if (v.Contains(p)) return v; return null; }
    }

    // A cracked plank (the ice bridge): the player standing on it makes it shake, then it drops; it comes back a few seconds later.
    public sealed class CrumblePlatform : MonoBehaviour
    {
        public float shakeSeconds = .75f, backSeconds = 5f;
        public int Drops { get; private set; }
        Vector3 home; float timer = -1, gone = -1; Collider col; Renderer[] rends;

        void Start() { home = transform.position; col = GetComponent<Collider>(); rends = GetComponentsInChildren<Renderer>(); }

        void Update()
        {
            if (gone >= 0)
            {
                gone += Time.deltaTime; transform.position += Vector3.down * 9f * Time.deltaTime;
                if (gone > backSeconds) { gone = -1; timer = -1; transform.position = home; col.enabled = true; foreach (var r in rends) r.enabled = true; }
                return;
            }
            var p = Party.I ? Party.I.Player.Motor : null;
            if (timer < 0 && p && p.Grounded && p.GroundCollider == col) { timer = 0; Sfx.Play("crack", .5f, 1.2f); }
            if (timer < 0) return;
            timer += Time.deltaTime;
            transform.position = home + new Vector3(Mathf.Sin(Time.time * 70f), 0, Mathf.Cos(Time.time * 53f)) * .04f;
            if (timer > shakeSeconds)
            {
                gone = 0; Drops++; col.enabled = false;
                Placeholders.Puff(transform.position, new Color(.8f, .93f, 1f));
                Sfx.Play("crack", .6f, .8f);
                Invoke(nameof(Hide), .8f);
            }
        }
        void Hide() { foreach (var r in rends) r.enabled = false; }
    }

    // Cracked ice over a hole: Dan's ground pound breaks it (saved in WorldProgress.opened); standing on it gives a hint.
    public sealed class CrackedFloor : MonoBehaviour
    {
        public static readonly List<CrackedFloor> All = new List<CrackedFloor>();
        public string id;
        public bool Broken { get; private set; }
        float hintAt = -10f;
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Update()
        {
            if (Broken || !Game.Playing || !Party.I) return;
            var m = Party.I.Player.Motor;
            if (m.Grounded && m.GroundCollider && m.GroundCollider.transform.IsChildOf(transform) && Time.time > hintAt + 6f)
            {
                hintAt = Time.time;
                Hud.Message(SaveGame.Current.Has(Abilities.GroundPound)
                    ? (Party.I.Player.Stats.displayName == "Dan" ? $"Cracked ice! Jump, then press {Controls.Btn(Controls.Act.Dive)} to ground pound." : "Cracked ice: Dan's ground pound would break it.")
                    : "The ice is cracked here. Something heavy landing on it might break through...", 4f);
            }
        }

        public void Pound(Hero by)
        {
            if (Broken) return;
            Broken = true;
            var prog = SaveGame.Current.World(Game.World.worldId);
            if (!prog.opened.Contains(id)) prog.opened.Add(id);
            Sfx.Play("crack", .8f, .7f);
            Placeholders.Puff(transform.position, new Color(.75f, .92f, 1f));
            PlayLog.Line($"cracked ice {id} broken by {by.Stats.displayName}'s ground pound");
            gameObject.SetActive(false);
            SaveGame.Save("cracked ice " + id);
        }
    }

    // A dark place (the ice cave): while the player is inside, the daylight dims and Dan's necklace gives a light.
    public sealed class DarkVolume : BoxVolume
    {
        public static readonly List<DarkVolume> All = new List<DarkVolume>();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public static DarkVolume At(Vector3 p) { foreach (var v in All) if (v.Contains(p)) return v; return null; }
    }

    // A tutorial hint: the first time the player comes within `radius`, a HUD message (button names of the device in use).
    public sealed class HintZone : MonoBehaviour
    {
        public float radius = 6f; public System.Func<string> text; public bool shown;
        void Update()
        {
            if (shown || !Game.Playing || !Party.I || text == null) return;
            var d = Party.I.Player.transform.position - transform.position; d.y = 0;
            if (d.magnitude < radius) { shown = true; Hud.Message(text(), 5f); }
        }
    }

    // A friendly mammoth (placeholder shapes until its model exists, Docs/MODELS-NEEDED.md #9): walks a loop (MovingPlatform's path),
    // its flat back is a platform the heroes ride. Heroes it walks into are pushed aside (it never walks through them).
    public sealed class Mammoth : MonoBehaviour
    {
        public const float BackTop = 4.0f;
        Transform[] legs; float phase; CreatureAnimator anim;
        public static readonly List<Mammoth> All = new List<Mammoth>();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public static Mammoth Make(Transform parent, Vector3[] path, float speed, float startAt)
        {
            var go = new GameObject("Mammoth"); go.transform.SetParent(parent, false);
            var mp = go.AddComponent<MovingPlatform>(); mp.path = path; mp.speed = speed; mp.startDistance = startAt;
            var fur = Placeholders.Mat(new Color(.55f, .33f, .2f)); var dark = Placeholders.Mat(new Color(.36f, .22f, .14f)); var tusk = Placeholders.Mat(new Color(.95f, .9f, .78f));
            var t = go.transform;
            Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, 2.75f, 0), new Vector3(2.8f, 2.4f, 4.6f), fur, name: "Body");
            Placeholders.Prim(PrimitiveType.Cube, t, new Vector3(0, BackTop - .15f, -.1f), new Vector3(2.3f, .3f, 3.6f), dark, true, "Back");   // the platform
            var head = Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, 3.0f, 2.5f), new Vector3(1.7f, 1.7f, 1.7f), dark, name: "Head").transform;
            for (int i = 0; i < 4; i++)
            {
                var seg = Placeholders.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 2.4f - i * .55f, 3.25f + i * .08f), new Vector3(.42f - i * .05f, .3f, .42f - i * .05f), dark, name: "Trunk");
                seg.transform.localRotation = Quaternion.Euler(10 + i * 6, 0, 0);
            }
            foreach (float s in new[] { -1f, 1f })
            {
                Placeholders.Prim(PrimitiveType.Cylinder, t, new Vector3(s * .55f, 2.4f, 3.3f), new Vector3(.16f, .55f, .16f), tusk, name: "Tusk").transform.localRotation = Quaternion.Euler(70, 0, s * 18);
                Placeholders.Prim(PrimitiveType.Cylinder, t, new Vector3(s * .7f, 2.15f, 3.9f), new Vector3(.12f, .35f, .12f), tusk, name: "Tusk tip").transform.localRotation = Quaternion.Euler(-20, 0, s * 25);
                Placeholders.Prim(PrimitiveType.Sphere, head, new Vector3(s * .5f, .1f, .1f), new Vector3(.12f, .6f, .5f), fur, name: "Ear");
                Placeholders.Prim(PrimitiveType.Sphere, head, new Vector3(s * .28f, .18f, .42f), Vector3.one * .1f, Placeholders.Mat(new Color(.08f, .06f, .05f)), name: "Eye");
            }
            var m = go.AddComponent<Mammoth>(); m.legs = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                var pivot = new GameObject("Leg pivot").transform; pivot.SetParent(t, false); pivot.localPosition = new Vector3(i < 2 ? -.8f : .8f, 1.9f, i % 2 == 0 ? 1.4f : -1.4f);
                Placeholders.Prim(PrimitiveType.Cylinder, pivot, new Vector3(0, -.95f, 0), new Vector3(.9f, .95f, .9f), fur, name: "Leg");
                m.legs[i] = pivot;
            }
            Placeholders.Prim(PrimitiveType.Capsule, t, new Vector3(0, 2.9f, -2.3f), new Vector3(.25f, .5f, .25f), dark, name: "Tail").transform.localRotation = Quaternion.Euler(-30, 0, 0);
            // Round 5: the Meshy mammoth (Code's rig) when it exists; the flat Back stays the ride (its box hidden)
            m.anim = CharacterModel.Creature(t, "mammoth", 2f, 6f, "Back");
            if (m.anim) { float sp = speed; m.anim.Drive = () => (null, sp); }
            return m;
        }

        void Update()
        {
            phase += Time.deltaTime * 2.2f;
            for (int i = 0; i < legs.Length; i++) legs[i].localRotation = Quaternion.Euler(Mathf.Sin(phase + (i == 0 || i == 3 ? 0 : Mathf.PI)) * 14f, 0, 0);
            if (!Party.I) return;
            // push heroes out of the body (not the ones riding on the back)
            foreach (var h in Party.I.Heroes)
            {
                var local = transform.InverseTransformPoint(h.Motor.Center);
                if (local.y > BackTop - .3f || local.y < -.5f) continue;
                var e = new Vector2(local.x / 1.7f, local.z / 2.6f);
                if (e.sqrMagnitude >= 1f) continue;
                var outDir = e.sqrMagnitude < 1e-4f ? Vector2.right : e.normalized;
                var target = new Vector3(outDir.x * 1.75f, local.y, outDir.y * 2.65f);
                var move = transform.TransformPoint(target) - h.Motor.Center; move.y = 0;
                h.Motor.Cc.Move(move);
            }
        }
    }

    // A campfire: logs, flickering flames and an orange light.
    public sealed class Campfire : MonoBehaviour
    {
        Light lt; Transform[] flames;
        public static Campfire Make(Transform parent, Vector3 at)
        {
            var go = new GameObject("Campfire"); go.transform.SetParent(parent, false); go.transform.position = at;
            var wood = Placeholders.Mat(new Color(.4f, .25f, .15f));
            for (int i = 0; i < 4; i++) Placeholders.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0, .15f, 0), new Vector3(.18f, .6f, .18f), wood).transform.localRotation = Quaternion.Euler(80, i * 45f, 0);
            for (int i = 0; i < 6; i++) Placeholders.Prim(PrimitiveType.Sphere, go.transform, new Vector3(Mathf.Cos(i) * .7f, .1f, Mathf.Sin(i) * .7f), new Vector3(.4f, .25f, .35f), Placeholders.Mat(new Color(.45f, .43f, .42f)));
            var c = go.AddComponent<Campfire>(); c.flames = new Transform[3];
            Color[] cols = { new Color(1f, .45f, .1f), new Color(1f, .7f, .2f), new Color(1f, .9f, .5f) };
            // Round 5 (clean-up): small tapered glowing tongues + rising flame particles (Round 4's were solid orange blobs)
            for (int i = 0; i < 3; i++) c.flames[i] = Placeholders.Prim(PrimitiveType.Sphere, go.transform, new Vector3((i - 1) * .12f, .28f + i * .05f, (i % 2) * .08f - .04f), new Vector3(.17f - i * .03f, .5f - i * .08f, .17f - i * .03f), Placeholders.Mat(cols[i], "Glow")).transform;
            var fx = new GameObject("Flames"); fx.transform.SetParent(go.transform, false); fx.transform.localPosition = new Vector3(0, .3f, 0);
            var ps = fx.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.startLifetime = new ParticleSystem.MinMaxCurve(.5f, .9f); main.startSpeed = new ParticleSystem.MinMaxCurve(.6f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(.18f, .34f); main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .55f, .12f), new Color(1f, .85f, .35f)); main.maxParticles = 80;
            var em = ps.emission; em.rateOverTime = 45;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 8; sh.radius = .22f; sh.rotation = new Vector3(-90, 0, 0);
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, .1f));
            var col = ps.colorOverLifetime; col.enabled = true; var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(1f, .9f, .5f), 0), new GradientColorKey(new Color(1f, .35f, .08f), 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            col.color = g;
            fx.GetComponent<ParticleSystemRenderer>().material = Resources.Load<Material>("Materials/Particle");
            ps.Play();
            var l = new GameObject("Fire light"); l.transform.SetParent(go.transform, false); l.transform.localPosition = new Vector3(0, 1.2f, 0);
            c.lt = l.AddComponent<Light>(); c.lt.type = LightType.Point; c.lt.color = new Color(1f, .6f, .25f); c.lt.range = 14f; c.lt.intensity = 3f; c.lt.shadows = LightShadows.None;
            return c;
        }
        void Update()
        {
            float t = Time.time;
            lt.intensity = 2.6f + Mathf.PerlinNoise(t * 6f, 0) * 1.4f;
            for (int i = 0; i < flames.Length; i++)
            {
                float k = .85f + Mathf.PerlinNoise(t * 5f + i * 3, i) * .35f;
                flames[i].localScale = new Vector3((.17f - i * .03f) * (1.1f - k * .1f), (.5f - i * .08f) * k, .17f - i * .03f);   // Round 5: slim tongues
                flames[i].localPosition = new Vector3((i - 1) * .12f, .22f + (.5f - i * .08f) * k * .5f, (i % 2) * .08f - .04f);
            }
        }
    }

    // An ash plume over a volcano: a stack of grey puffs drifting up and swelling, slowly.
    public sealed class AshPlume : MonoBehaviour
    {
        readonly List<Transform> puffs = new List<Transform>(); float height;
        public static AshPlume Make(Transform parent, Vector3 crater, float size)
        {
            var go = new GameObject("Ash plume"); go.transform.SetParent(parent, false); go.transform.position = crater;
            var a = go.AddComponent<AshPlume>(); a.height = size * 6f;
            var mats = new[] { Placeholders.Mat(new Color(.36f, .34f, .34f)), Placeholders.Mat(new Color(.46f, .44f, .44f)), Placeholders.Mat(new Color(.28f, .26f, .27f)) };
            for (int i = 0; i < 14; i++)
            {
                var p = Placeholders.Prim(PrimitiveType.Sphere, go.transform, Vector3.zero, Vector3.one, mats[i % 3]).transform;
                p.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                a.puffs.Add(p);
            }
            a.size = size;
            return a;
        }
        float size;
        void Update()
        {
            float t = Time.time * .03f;
            for (int i = 0; i < puffs.Count; i++)
            {
                float k = (i / (float)puffs.Count + t) % 1f;   // 0 at the crater, 1 at the top
                float r = size * (.5f + k * 1.6f) * (1 + .15f * Mathf.Sin(i * 1.7f));
                puffs[i].localPosition = new Vector3(Mathf.Sin(i * 2.1f) * size * .4f * k + k * size * 1.5f, k * height, Mathf.Cos(i * 1.3f) * size * .3f * k);
                puffs[i].localScale = Vector3.one * r;
            }
        }
    }

    // Blowing snow round the camera (a particle box that follows it), drifting with the wind.
    public static class SnowFall
    {
        static GameObject current;
        public static void Show(bool on) { if (current) current.SetActive(on); }
        public static ParticleSystem Make(Transform parent)
        {
            var go = new GameObject("Snowfall"); go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.startLifetime = 6f; main.startSpeed = 0f; main.startSize = new ParticleSystem.MinMaxCurve(.05f, .14f);
            main.maxParticles = 900; main.simulationSpace = ParticleSystemSimulationSpace.World; main.startColor = new Color(1f, 1f, 1f, .9f);
            var em = ps.emission; em.rateOverTime = 140f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(40f, 2f, 40f); sh.position = new Vector3(0, 14f, 0);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(1.5f, 4f); vel.y = new ParticleSystem.MinMaxCurve(-2.6f, -1.6f); vel.z = new ParticleSystem.MinMaxCurve(-.5f, 1f);
            var noise = ps.noise; noise.enabled = true; noise.strength = .6f; noise.frequency = .3f;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.material = Resources.Load<Material>("Materials/Particle"); r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<FollowCamera>();
            current = go;
            ps.Play();
            return ps;
        }
        sealed class FollowCamera : MonoBehaviour
        {
            void LateUpdate() { if (CameraRig.I) { var p = CameraRig.I.Cam.transform.position; transform.position = new Vector3(p.x, p.y, p.z); } }
        }
    }

    // Steam rising from a vent (the updraft's look): puffs going up its column.
    public static class Steam
    {
        public static ParticleSystem Make(Transform parent, Vector3 at, float height)
        {
            var go = new GameObject("Steam"); go.transform.SetParent(parent, false); go.transform.position = at;
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.startLifetime = height / 7f; main.startSpeed = 7f; main.startSize = new ParticleSystem.MinMaxCurve(.8f, 1.8f);
            main.startColor = new Color(1f, 1f, 1f, .45f); main.maxParticles = 200;
            var em = ps.emission; em.rateOverTime = 30f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = .9f; sh.rotation = new Vector3(-90, 0, 0);
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, .6f, 1, 2.2f));
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(.5f, 0), new GradientAlphaKey(0, 1) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.material = Resources.Load<Material>("Materials/Particle"); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
            return ps;
        }
    }
}
