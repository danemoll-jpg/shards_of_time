using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 4, item 5: travel between worlds = the vortex (DECIDED, Dan 2026-11-09). A world's story shard opens its vortex gate (blue for
    // the Ice Age); stepping in takes the party into the vortex space, a floating platform in a swirling tunnel with a path per world:
    // every world unlocked so far, the next world, and Home. Home is visible but blocked until all eight story shards are rebuilt (it
    // shows how many are still missing). This round: Ice Age (back to the valley), Egypt ("to be continued", saved), Home (blocked).
    // The vortex replaces Round 1's portal (the playground keeps its portals as a test area).
    public static class Vortex
    {
        public static readonly Vector3 Centre = new Vector3(0, 600f, 2500f);   // far from every world; above the kill height
        public const string Checkpoint = "VX";
        public const int StoryShardsTotal = 8;
        public static bool Inside(Vector3 p) => (p - Centre).sqrMagnitude < 120f * 120f;

        // the paths: id, label, colour, the world it leads to
        public sealed class PathInfo { public string id, label, world; public Color color; public float yaw; }
        public static readonly PathInfo[] Paths =
        {
            new PathInfo { id = "IceAge", label = "The Ice Age", world = WorldData.IceAge, color = new Color(.3f, .6f, 1f), yaw = -54f },
            new PathInfo { id = "Egypt", label = "Ancient Egypt", world = WorldData.Egypt, color = new Color(1f, .62f, .2f), yaw = -18f },
            // Round 6: Egypt is real; Bethlehem is next (to be continued once Egypt's red story shard is found)
            new PathInfo { id = "Bethlehem", label = "Bethlehem", world = "Bethlehem", color = new Color(.95f, .9f, .55f), yaw = 18f },
            new PathInfo { id = "Home", label = "Home", world = "Home", color = new Color(1f, .9f, .7f), yaw = 54f },
        };

        public static void Build(Transform root)
        {
            var space = new GameObject("Vortex space").transform; space.SetParent(root, false); space.position = Centre;
            // the tunnel: a long tube seen from inside, a scrolling swirl (VortexSwirl)
            var tube = new GameObject("Tunnel"); tube.transform.SetParent(space, false);
            tube.AddComponent<MeshFilter>().sharedMesh = TubeMesh(70f, 160f, 48, 10);
            var tr = tube.AddComponent<MeshRenderer>(); tr.sharedMaterial = SwirlMaterial(); tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tube.transform.localRotation = Quaternion.Euler(90, 0, 0); tube.transform.localPosition = new Vector3(0, 0, 0);
            tube.AddComponent<VortexSwirl>();
            // the platform and the three paths, each ending in a door
            var floor = Placeholders.Mat(new Color(.55f, .6f, .95f, 1f), "Glow");
            var plat = Placeholders.Prim(PrimitiveType.Cylinder, space, new Vector3(0, -.25f, 0), new Vector3(16f, .25f, 16f), Placeholders.Mat(new Color(.3f, .32f, .55f)), false, "Platform");
            plat.AddComponent<MeshCollider>().sharedMesh = plat.GetComponent<MeshFilter>().sharedMesh;   // a flat disc (a cylinder's own collider is a capsule)
            Placeholders.Prim(PrimitiveType.Cylinder, space, new Vector3(0, .01f, 0), new Vector3(14f, .01f, 14f), floor, name: "Platform glow");
            Playground.CheckpointHere(Checkpoint, Centre + new Vector3(0, 0, -4f), 0, space, visible: false);
            // low glowing rails round the platform (gaps where the paths leave) and along the bridges: nobody walks off by accident
            var rail = Placeholders.Mat(new Color(.7f, .8f, 1f), "Glow");
            for (int i = 0; i < 48; i++)
            {
                float yaw = i * 7.5f;
                if (Paths.Any(p => Mathf.Abs(Mathf.DeltaAngle(yaw, p.yaw)) < 11f)) continue;
                var r = Placeholders.Prim(PrimitiveType.Cube, space, Quaternion.Euler(0, yaw, 0) * new Vector3(0, .3f, 7.8f), new Vector3(1.1f, .6f, .15f), rail, true, "Rail");
                r.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            }
            foreach (var p in Paths)
            {
                var arm = new GameObject("Path " + p.id).transform; arm.SetParent(space, false); arm.localRotation = Quaternion.Euler(0, p.yaw, 0);
                Placeholders.Prim(PrimitiveType.Cube, arm, new Vector3(0, -.2f, 13f), new Vector3(3.2f, .4f, 12f), Placeholders.Mat(p.color * .6f + new Color(0, 0, 0, .4f)), true, "Bridge");
                Placeholders.Prim(PrimitiveType.Cube, arm, new Vector3(0, .02f, 13f), new Vector3(1f, .02f, 11.5f), Placeholders.Mat(p.color, "Glow"), name: "Bridge glow");
                foreach (float sx in new[] { -1.65f, 1.65f }) Placeholders.Prim(PrimitiveType.Cube, arm, new Vector3(sx, .3f, 14.5f), new Vector3(.12f, .6f, 9f), rail, true, "Bridge rail");
                Placeholders.Prim(PrimitiveType.Cube, arm, new Vector3(0, -.2f, 21.5f), new Vector3(7f, .4f, 5f), Placeholders.Mat(p.color * .5f + new Color(0, 0, 0, .5f)), true, "Landing");
                var door = new GameObject("Door " + p.id); door.transform.SetParent(arm, false); door.transform.localPosition = new Vector3(0, 0, 22.5f);
                var ring = Placeholders.Mat(p.color * .8f + new Color(.1f, .1f, .1f, .2f));
                for (int i = 0; i < 18; i++)
                {
                    float a = i * Mathf.PI * 2 / 18; if (Mathf.Sin(a) < -.8f) continue;
                    var s = Placeholders.Prim(PrimitiveType.Cube, door.transform, new Vector3(Mathf.Cos(a) * 2.2f, 2.4f + Mathf.Sin(a) * 2.2f, 0), new Vector3(.7f, .7f, .5f), ring, true);
                    s.transform.localRotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg);
                }
                var disc = Placeholders.Prim(PrimitiveType.Cylinder, door.transform, new Vector3(0, 2.4f, 0), new Vector3(3.9f, .02f, 3.9f), Placeholders.Mat(p.color, "Glow"), name: "Disc");
                disc.transform.localRotation = Quaternion.Euler(90, 0, 0);
                var label = new GameObject("Label"); label.transform.SetParent(door.transform, false); label.transform.localPosition = new Vector3(0, 5.6f, 0);
                // TextMesh reads from its -z side: unturned, it faces the platform
                var tm = label.AddComponent<TextMesh>(); tm.text = p.label; tm.font = UiKit.Font; tm.fontSize = 64; tm.characterSize = .09f;
                tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = Color.Lerp(p.color, Color.white, .5f);
                label.GetComponent<MeshRenderer>().sharedMaterial = UiKit.Font.material;
                var d = door.AddComponent<VortexDoor>(); d.info = p; d.label = tm;
                if (p.id == "Home" || p.id == "Bethlehem")
                {
                    var bar = Placeholders.Prim(PrimitiveType.Cube, door.transform, new Vector3(0, 2.4f, -.6f), new Vector3(4.6f, 4.6f, .2f), Placeholders.Mat(new Color(1f, .85f, .6f, .35f), "Water"), true, "Barrier");
                    d.barrier = bar;
                }
            }
        }

        static Material swirl;
        public static Material SwirlMaterial()
        {
            if (swirl) return swirl;
            // a swirl texture made in code: soft diagonal bands in blue, violet and white
            int w = 256, h = 64; var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = "Swirl" };
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)w, v = y / (float)h;
                    float band = .5f + .5f * Mathf.Sin((u * 3f + v * 1f) * Mathf.PI * 2f + Mathf.Sin(v * Mathf.PI * 2f) * 1.5f);
                    float band2 = .5f + .5f * Mathf.Sin((u * 7f - v * 2f) * Mathf.PI * 2f);
                    var c = Color.Lerp(new Color(.12f, .14f, .45f), new Color(.45f, .3f, .95f), band);
                    c = Color.Lerp(c, new Color(.75f, .9f, 1f), Mathf.Pow(band2, 6f) * .8f);
                    px[y * w + x] = c;
                }
            tex.SetPixels(px); tex.Apply();
            swirl = new Material(Placeholders.Mat(Color.white, "Glow")) { name = "Vortex swirl" };
            swirl.SetTexture("_BaseMap", tex); swirl.mainTexture = tex; swirl.SetColor("_BaseColor", Color.white);
            return swirl;
        }

        // a tube (radius r, length l) with its faces inward, UVs round (u) and along (v)
        static Mesh TubeMesh(float r, float l, int segs, int rings)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int j = 0; j <= rings; j++)
                for (int i = 0; i <= segs; i++)
                {
                    float a = i / (float)segs * Mathf.PI * 2f, y = (j / (float)rings - .5f) * l;
                    v.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r)); uv.Add(new Vector2(i / (float)segs * 2f, j / (float)rings * 3f));
                }
            for (int j = 0; j < rings; j++)
                for (int i = 0; i < segs; i++)
                {
                    int a = j * (segs + 1) + i, b = a + 1, c = a + segs + 1, d = c + 1;
                    t.AddRange(new[] { a, b, c, b, d, c, a, c, b, b, c, d });   // both sides (seen from anywhere inside)
                }
            var m = new Mesh { name = "Vortex tunnel" }; m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }
    }

    // the tunnel turns and its swirl flows
    public sealed class VortexSwirl : MonoBehaviour
    {
        Material m;
        void Start() { m = GetComponent<Renderer>().sharedMaterial; }
        void Update()
        {
            transform.Rotate(0, 12f * Time.deltaTime, 0, Space.Self);
            if (m) m.SetTextureOffset("_BaseMap", new Vector2(Time.time * .03f, Time.time * .12f));
        }
    }

    // a door in the vortex space: walking in takes the party where its path leads (or bounces off Home's barrier)
    public sealed class VortexDoor : MonoBehaviour
    {
        public Vortex.PathInfo info; public TextMesh label; public GameObject barrier;
        public static readonly List<VortexDoor> All = new List<VortexDoor>();
        float cool;
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public bool Blocked => (info.id == "Home" && SaveGame.Current.StoryShards < Vortex.StoryShardsTotal) ||
                               (info.id == "Bethlehem" && !SaveGame.Current.World(WorldData.Egypt).storyShard);

        void Update()
        {
            if (info.id == "Home" && label) label.text = Blocked ? $"Home\n{SaveGame.Current.StoryShards} of {Vortex.StoryShardsTotal} story shards" : "Home";
            if (info.id == "Bethlehem" && label) label.text = Blocked ? "Bethlehem\n(after Egypt's red shard)" : "Bethlehem";
            if (barrier) barrier.SetActive(Blocked);
            cool -= Time.deltaTime;
            if (cool > 0 || !Game.Playing || !Party.I || Game.I.Busy) return;
            var local = transform.InverseTransformPoint(Party.I.Player.Motor.Center);
            if (Mathf.Abs(local.z) > 1.2f || new Vector2(local.x, local.y - 2.4f).magnitude > 2f) return;
            cool = 2.5f;
            Game.I.TakeVortexPath(info, this);
        }
    }
}
