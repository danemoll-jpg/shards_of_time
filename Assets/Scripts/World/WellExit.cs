using System.Collections.Generic;
using UnityEngine;

namespace ShardsOfTime
{
    // Fix 7.2, item 1: a way out of a round shaft of water (Bethlehem's well). A hero swimming at the top who presses Jump, or swims
    // against the wall, climbs the rope up and over the rim (the motor's ClimbOut, as with the Ice Age's holes) and is set down outside.
    public sealed class WellExit : MonoBehaviour
    {
        public static readonly List<WellExit> All = new List<WellExit>();
        public Vector3 centre;     // the shaft's middle (y unused)
        public float radius = 2.1f;// the inside of the wall
        public float rimTop;       // a height clear of the wall's top
        public float outAt = 4.4f; // where the climb sets the hero down (from the middle)
        public Vector3 fixedExit;  // Round 10: a rope ladder: always out to this spot (zero: over the rim on the side the hero heads for)
        public string hintText = "In the well! Grab the rope: {0} climbs out.";
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        float hintAt = -10f;
        void Update()
        {
            if (!Game.Playing || !Party.I) return;
            var m = Party.I.Player.Motor;
            if (m.Mode == MoveMode.Swim && At(m.transform.position) == this && Time.time > hintAt + 10f)
            { hintAt = Time.time; Hud.Message(string.Format(hintText, Controls.Btn(Controls.Act.Jump)), 4f); }
        }

        public static WellExit At(Vector3 p)
        {
            foreach (var w in All) if (Flat(p - w.centre).magnitude < w.radius + .3f) return w;
            return null;
        }
        public bool AtWall(Vector3 p) => Flat(p - centre).magnitude > radius - 1f;
        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

        // over the rim on the side the hero heads for (or the side they're nearest)
        public Vector3 ExitFrom(Vector3 p, Vector3 heading)
        {
            if (fixedExit != Vector3.zero) return fixedExit;
            var dir = Flat(heading).sqrMagnitude > .04f ? Flat(heading).normalized : Flat(p - centre).sqrMagnitude > .01f ? Flat(p - centre).normalized : Vector3.forward;
            var e = centre + dir * outAt; e.y = rimTop; return e;
        }
    }
}
