using System.Collections.Generic;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 6, item 2: getting out from under the ice is always easy. A hole in the ice (x / z box from the surface down); a hero who
    // swims up into one is lifted out onto the ice by the motor (CharacterMotor.ClimbOut); a player kept under the ice too long is
    // brought to the nearest hole (Watch); the camera stays low and close under the ice (CameraRig).
    public sealed class IceHole : MonoBehaviour
    {
        public static readonly List<IceHole> All = new List<IceHole>();
        public static Bounds Region;   // the frozen water these holes are in (the lake): "under the ice" only counts inside it
        public Rect area;          // x / z of the hole
        public float iceTop;       // the ice's top surface around it
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public static IceHole At(Vector3 p) { foreach (var h in All) if (h.area.Contains(new Vector2(p.x, p.z))) return h; return null; }

        public Vector3 Centre => new Vector3(area.center.x, iceTop, area.center.y);

        // the spot on the ice just past the hole's nearest edge (or the edge the hero is heading for)
        public Vector3 ExitFrom(Vector3 p, Vector3 heading)
        {
            var c = area.center; float dx0 = p.x - area.xMin, dx1 = area.xMax - p.x, dz0 = p.z - area.yMin, dz1 = area.yMax - p.z;
            Vector3 dir;
            if (heading.sqrMagnitude > .04f) dir = Mathf.Abs(heading.x) > Mathf.Abs(heading.z) ? new Vector3(Mathf.Sign(heading.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(heading.z));
            else
            {
                float m = Mathf.Min(Mathf.Min(dx0, dx1), Mathf.Min(dz0, dz1));
                dir = m == dx0 ? Vector3.left : m == dx1 ? Vector3.right : m == dz0 ? Vector3.back : Vector3.forward;
            }
            var e = new Vector3(Mathf.Clamp(p.x, area.xMin + .8f, area.xMax - .8f), iceTop, Mathf.Clamp(p.z, area.yMin + .8f, area.yMax - .8f));
            if (dir.x < 0) e.x = area.xMin - 1.1f; else if (dir.x > 0) e.x = area.xMax + 1.1f; else if (dir.z < 0) e.z = area.yMin - 1.1f; else e.z = area.yMax + 1.1f;
            return e;
        }

        // in the water with ice (any solid collider) right overhead: the hero is under the ice, not in a hole. (Not "head under the water
        // line": under the ice the water's top is the ice's underside, so a floating hero's head can be just above it.)
        public static bool UnderIce(CharacterMotor m)
        {
            if (!m.Water || All.Count == 0 || !Region.Contains(m.transform.position) || At(m.transform.position)) return false;
            return Physics.Raycast(m.Center, Vector3.up, 10f, ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << Hero.EnemyLayer) | (1 << 4) | (1 << 2)), QueryTriggerInteraction.Ignore);
        }
        // down in a hole's water, below the ice (the camera keeps close here too)
        public static bool InHoleBelowIce(CharacterMotor m) { var h = m.Water ? At(m.transform.position) : null; return h && m.Head.y < h.iceTop - .3f; }

        public static float UnderFor { get; private set; }
        public static int Rescues { get; private set; }
        public const float SwimmerLimit = 14f;   // Kira never runs out of air, but she's brought up after this long under the ice too

        // the player under the ice: after too long (a third of the breath left, or 14 s for a swimmer) the necklace pulls them up the nearest hole
        public static void Watch(Hero h)
        {
            if (All.Count == 0 || !h) return;
            var m = h.Motor;
            UnderFor = UnderIce(m) ? UnderFor + Time.deltaTime : 0;
            bool limit = h.Stats.swimmer ? UnderFor > SwimmerLimit : UnderFor > 0 && (m.Breath01 < .34f || UnderFor > h.Stats.breathSeconds * .66f);
            if (!limit) return;
            IceHole best = null; float bd = float.MaxValue;
            foreach (var x in All) { float d = (x.Centre - h.transform.position).sqrMagnitude; if (d < bd) { bd = d; best = x; } }
            if (!best || !m.Water) return;
            var p = best.Centre; p.y = m.Water.Top - h.Stats.height * .75f;
            m.Teleport(p, h.transform.rotation);
            m.ClimbOut(best.ExitFrom(p, Vector3.zero));
            UnderFor = 0; Rescues++;
            Hud.Message("Up through the hole! (Head for the light.)");
        }
    }
}
