using System.Collections.Generic;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 10: the Pirate Seas' pieces (World/Pirate.cs places them).

    // before the green shard goes overboard the sea is "shallow": below the lock's top (3.5 m down) the water pushes everyone back up
    public sealed class SeaLock : BoxVolume
    {
        public static readonly List<SeaLock> All = new List<SeaLock>();
        float hintAt = -10f;
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public static SeaLock At(Vector3 p) { foreach (var v in All) if (v.Contains(p)) return v; return null; }
        public void Hint() { if (Time.time > hintAt + 6f) { hintAt = Time.time; Hud.Message("The water gets dark and deep down there... better stay near the top for now.", 4f); } }
    }

    // air bubbles rising from a vent on the sea floor: Dan and Abi get their breath back inside (CharacterMotor.Breathe)
    public sealed class AirVolume : BoxVolume
    {
        public static readonly List<AirVolume> All = new List<AirVolume>();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public static AirVolume At(Vector3 p) { foreach (var v in All) if (v.Contains(p)) return v; return null; }

        public void MakeBubbles()
        {
            var go = new GameObject("Bubbles"); go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(transform.position.x, transform.position.y - transform.lossyScale.y / 2 + .3f, transform.position.z);
            go.transform.localScale = new Vector3(1 / transform.lossyScale.x, 1 / transform.lossyScale.y, 1 / transform.lossyScale.z);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.startLifetime = transform.lossyScale.y / 2.2f; main.startSpeed = 2.2f; main.startSize = new ParticleSystem.MinMaxCurve(.12f, .35f);
            main.startColor = new Color(.85f, .97f, 1f, .8f); main.maxParticles = 120; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 14f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = .6f; sh.rotation = new Vector3(-90, 0, 0);
            var noise = ps.noise; noise.enabled = true; noise.strength = .4f; noise.frequency = .6f;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.material = Resources.Load<Material>("Materials/Particle");
            ps.Play();
        }
    }

    // a wet patch of deck: otter Kira's belly-slide keeps its full speed across it
    public sealed class WetDeck : BoxVolume
    {
        public static readonly List<WetDeck> All = new List<WetDeck>();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public static WetDeck At(Vector3 p) { foreach (var v in All) if (v.Contains(p)) return v; return null; }
    }

    // a rope net in the rigging: standing at its foot, Jump (or walking into it) climbs it hand over hand up to `to` (the motor's ClimbOut)
    public sealed class RopeClimb : MonoBehaviour
    {
        public static readonly List<RopeClimb> All = new List<RopeClimb>();
        public float radius = 1f; public Vector3 to;
        public static int Climbs { get; private set; }
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        // called by the party for whoever is played (and by followers never: they warp)
        public static bool Try(Hero h, bool jumpPressed)
        {
            if (!jumpPressed || !h.Motor.Grounded || h.Motor.Climbing) return false;
            foreach (var c in All)
            {
                if (!c || !c.isActiveAndEnabled) continue;
                var d = h.transform.position - c.transform.position; float dy = d.y; d.y = 0;
                if (d.magnitude > c.radius || Mathf.Abs(dy) > .8f) continue;
                Climbs++; PlayLog.Line($"{h.Stats.displayName} climbs the rigging ({c.name}) to {c.to}");
                Sfx.Play("rope_creak", .5f, Random.Range(.95f, 1.05f));
                h.Motor.ClimbOut(c.to, true);
                return true;
            }
            return false;
        }
    }

    // the sea's surface rolls gently (its texture-less material: a slow up-and-down of the plane, a few cm)
    public sealed class SeaWaves : MonoBehaviour
    {
        Vector3 basePos; void Start() => basePos = transform.position;
        void Update() => transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * .7f) * .04f;
    }

    // kelp and other soft things sway in the current
    public sealed class Sway : MonoBehaviour
    {
        Quaternion baseRot; float ph; void Start() { baseRot = transform.rotation; ph = Random.value * 6f; }
        void Update() => transform.rotation = Quaternion.Euler(Mathf.Sin(Time.time * .8f + ph) * 6f, 0, Mathf.Cos(Time.time * .6f + ph) * 5f) * baseRot;
    }

    // a school of friendly tropical fish (the static `fish` model, moved by code): they circle a spot, bobbing, tails wiggling, and part
    // round the player
    public sealed class FishSchool : MonoBehaviour
    {
        struct F { public Transform t; public float a, r, y, speed, ph; }
        readonly List<F> fish = new List<F>();
        public float radius = 8f;
        public static int Count { get; private set; }

        public static FishSchool Make(Transform parent, Vector3 at, float radius, int n)
        {
            var go = new GameObject("Fish school"); go.transform.SetParent(parent, false); go.transform.position = at;
            var s = go.AddComponent<FishSchool>(); s.radius = radius;
            var rnd = new System.Random((int)(at.x * 13 + at.z * 7));
            for (int i = 0; i < n; i++)
            {
                var f = StaticModel.Spawn("fish", go.transform, at, 0, .32f + (float)rnd.NextDouble() * .12f);
                if (!f) f = Placeholders.Prim(PrimitiveType.Sphere, go.transform, at, new Vector3(.12f, .25f, .35f), Placeholders.Mat(new Color(1f, .5f, .2f)), false, "Fish");
                foreach (var r in f.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var holder = new GameObject("Fish").transform; holder.SetParent(go.transform, false); f.transform.SetParent(holder, true); f.transform.localPosition = Vector3.zero;
                s.fish.Add(new F { t = holder, a = (float)rnd.NextDouble() * 6.28f, r = radius * (.4f + (float)rnd.NextDouble() * .6f), y = (float)(rnd.NextDouble() - .5) * 3f,
                                   speed = .25f + (float)rnd.NextDouble() * .15f, ph = (float)rnd.NextDouble() * 6f });
                Count++;
            }
            return s;
        }

        void Update()
        {
            var c = transform.position; Vector3 player = Party.I ? Party.I.Player.Motor.Center : new Vector3(0, 9999, 0);
            bool near = (player - c).sqrMagnitude < (radius + 12) * (radius + 12);
            for (int i = 0; i < fish.Count; i++)
            {
                var f = fish[i]; f.a += f.speed * Time.deltaTime; fish[i] = f;
                var p = c + new Vector3(Mathf.Cos(f.a) * f.r, f.y + Mathf.Sin(Time.time * .9f + f.ph) * .4f, Mathf.Sin(f.a) * f.r);
                if (near) { var away = p - player; float d = away.magnitude; if (d < 2.5f) p += away / Mathf.Max(.1f, d) * (2.5f - d); }   // they part round you
                var dir = new Vector3(-Mathf.Sin(f.a), 0, Mathf.Cos(f.a));
                f.t.SetPositionAndRotation(p, Quaternion.LookRotation(dir) * Quaternion.Euler(0, Mathf.Sin(Time.time * 9f + f.ph) * 12f, 0));
            }
        }
    }

    // the world's watcher: the music's place (above / under the water), Muscle Man's struggle under water
    public sealed class PirateWatch : MonoBehaviour
    {
        bool hidden;
        void Update()
        {
            Pirate.Watch();
            // the world's pirates and crabs stay out of the cutscenes' shots (they keep their place; only hidden)
            bool hide = Game.InCutscene;
            if (hide != hidden) { hidden = hide; foreach (var e in Enemy.All) if (e && e.kind >= Enemy.Kind.Deckhand) foreach (var r in e.GetComponentsInChildren<Renderer>(true)) r.enabled = !hide; }
            if (Party.I && Party.I.Companion) Party.I.Companion.Motor.Struggling = Party.I.Companion.Motor.Mode == MoveMode.Swim;
        }
        void OnDestroy() { if (Party.I && Party.I.Companion) Party.I.Companion.Motor.Struggling = false; }
    }
}
