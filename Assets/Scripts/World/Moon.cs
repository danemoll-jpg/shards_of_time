using UnityEngine;

namespace ShardsOfTime
{
    // Round 13: the Moon (placeholder until Round 13 builds it: the vortex shows it "to be continued" and the launch ends in the vortex).
    public static class Moon
    {
        public static bool Real => false;
        public const string Start = "MO-start", VortexCp = "MO-vortex", ArrivedFlag = "MO-arrived";
        public static Portal Gate { get; private set; }
        public static Vector3 OnGround(float x, float z) => new Vector3(x, 0, z);
        public static Transform Build(WorldData w) => new GameObject("World The Moon (to be continued)").transform;
        public static void FilmSpot(string name, out Vector3 at, out Quaternion facing) { at = Vector3.zero; facing = Quaternion.identity; }
    }
}
