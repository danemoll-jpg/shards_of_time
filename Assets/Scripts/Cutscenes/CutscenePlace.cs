using UnityEngine;

namespace ShardsOfTime
{
    // Round 9: a "set" inside a cutscene that stands somewhere else in the world than the cutscene's anchor (Act I's fall over the Ice Age,
    // Egypt's dune, the Wise Men out in the field): on spawning it moves to world (x, ground + up, z), turned `yaw`, whatever the anchor
    // is. The ground is the highest world collider there (not the cutscene's own, not heroes, enemies, water or trigger volumes).
    public sealed class CutscenePlace : MonoBehaviour
    {
        public float x, z, yaw, up;
        public bool ground = true;

        void Awake()
        {
            float y = up;
            if (ground)
            {
                int mask = ~((1 << 8) | (1 << 9) | (1 << 10) | (1 << 4) | (1 << 2));
                float best = float.NegativeInfinity; var mine = transform.root;
                foreach (var h in Physics.RaycastAll(new Vector3(x, 500f, z), Vector3.down, 1000f, mask, QueryTriggerInteraction.Ignore))
                    if (!h.collider.transform.IsChildOf(mine) && h.point.y > best) best = h.point.y;
                if (best > float.NegativeInfinity) y += best;
            }
            transform.SetPositionAndRotation(new Vector3(x, y, z), Quaternion.Euler(0, yaw, 0));
        }
    }
}
