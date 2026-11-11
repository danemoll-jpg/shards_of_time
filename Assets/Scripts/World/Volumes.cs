using System.Collections.Generic;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 1, item 4: the volumes that switch the motor's mode. Boxes (axis-aligned, from the transform's position and scale), found by
    // a point test, so they need no physics callbacks and work the same for the player and the followers.
    public abstract class BoxVolume : MonoBehaviour
    {
        public Bounds Box => new Bounds(transform.position, transform.lossyScale);
        public bool Contains(Vector3 p) => Box.Contains(p);
    }

    public sealed class WaterVolume : BoxVolume
    {
        public static readonly List<WaterVolume> All = new List<WaterVolume>();
        public float Top => Box.max.y;
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public static WaterVolume At(Vector3 p) { foreach (var w in All) if (w.Contains(p)) return w; return null; }
    }

    public sealed class LowGravityVolume : BoxVolume
    {
        public static readonly List<LowGravityVolume> All = new List<LowGravityVolume>();
        public float gravityScale = .3f;   // Moon-like: gravity x .3, so a jump goes jumpHeight / .3 high (Dan 5.3 m, Abi 7 m, Kira 4.7 m)
        public float jumpScale = 1f;       // take-off speed multiplier (height x jumpScale squared)
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public static LowGravityVolume At(Vector3 p) { foreach (var v in All) if (v.Contains(p)) return v; return null; }
    }

    // Moves between two points (or turns) in Update before the characters (execution order), so riders are carried the same frame.
    [DefaultExecutionOrder(-100)]
    public sealed class MovingPlatform : MonoBehaviour
    {
        public Vector3 a, b; public float period = 6f; public float spinDegPerSec; float t;
        // Round 6: a platform something else moves (the camel, the lift, the turning bridge): it only carries riders (a == b: it stays put)
        public void Hold(Vector3 at) { a = b = at; }
        // Round 4: or a closed loop of points walked at `speed` m/s, turning to face the way it goes (the mammoths)
        public Vector3[] path; public float speed = 2f, startDistance, turnDegPerSec = 30f; float along;
        void Start() { along = startDistance; if (path != null && path.Length > 1) { transform.position = PathPoint(along, out var dir); transform.rotation = Quaternion.LookRotation(dir); } }
        Vector3 PathPoint(float d, out Vector3 dir)
        {
            float total = 0; for (int i = 0; i < path.Length; i++) total += Vector3.Distance(path[i], path[(i + 1) % path.Length]);
            d = Mathf.Repeat(d, total);
            for (int i = 0; i < path.Length; i++)
            {
                var p0 = path[i]; var p1 = path[(i + 1) % path.Length]; float l = Vector3.Distance(p0, p1);
                if (d <= l) { dir = (p1 - p0).normalized; return Vector3.Lerp(p0, p1, d / Mathf.Max(.001f, l)); }
                d -= l;
            }
            dir = Vector3.forward; return path[0];
        }
        void Update()
        {
            if (path != null && path.Length > 1)
            {
                // slow down while turning a corner, so the back stays easy to stand on
                var facing = transform.forward;
                PathPoint(along, out var want);
                float off = Vector3.Angle(facing, want);
                along += speed * Time.deltaTime * Mathf.Lerp(1f, .25f, Mathf.Clamp01(off / 60f));
                transform.position = PathPoint(along, out want);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(new Vector3(want.x, 0, want.z)), turnDegPerSec * Time.deltaTime);
                return;
            }
            t += Time.deltaTime;
            if (a != b) transform.position = Vector3.Lerp(a, b, .5f - .5f * Mathf.Cos(t * 2f * Mathf.PI / period));
            if (spinDegPerSec != 0) transform.Rotate(0, spinDegPerSec * Time.deltaTime, 0, Space.World);
        }
    }

    // Round 1, item 6: a heavy block only Dan can push (CharacterStats.canPushHeavy). Slides along the push's main axis, never off a
    // ledge, never through walls.
    public sealed class PushBlock : MonoBehaviour
    {
        public float speed = 1.6f;
        public Bounds limits;                 // the block's centre stays inside (the push yard)
        public float PushedDistance { get; private set; }
        public string Refused { get; private set; } = "never pushed";
        float lastMessage = -10;

        public void Pushed(CharacterMotor m, Vector3 dir)
        {
            if (!m.Stats.canPushHeavy)
            {
                if (m.CompareTag("Player") && Time.time - lastMessage > 4) { lastMessage = Time.time; Hud.Message($"Too heavy for {m.Stats.displayName}. Dan can push this."); }
                return;
            }
            if (!m.Grounded) { Refused = "not grounded"; return; }
            m.LastPush = Time.time;   // Dan leans into it (the push clip), moving or not
            dir.y = 0;
            dir = Mathf.Abs(dir.x) > Mathf.Abs(dir.z) ? new Vector3(Mathf.Sign(dir.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(dir.z));
            var step = dir * speed * Time.deltaTime;
            var size = transform.lossyScale;
            var next = transform.position + step;
            if (!limits.Contains(new Vector3(next.x, limits.center.y, next.z))) { Refused = "limits"; return; }
            // nothing solid in the way
            var hits = Physics.OverlapBox(next + Vector3.up * .05f, size * .48f, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            foreach (var h in hits) if (h.transform != transform && !h.GetComponent<CharacterController>() && h.transform.root != transform.root) { Refused = "blocked by " + h.name; return; }
            Refused = "";
            transform.position = next; PushedDistance += step.magnitude;
        }
    }

    // Round 1: the boulder that "needs Muscle Man" moved to World/MuscleJobs.cs (Round 8: he moves it)
}
