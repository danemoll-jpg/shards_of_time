using System.Collections.Generic;
using UnityEngine;

namespace ShardsOfTime
{
    // Rounds 12-13: shapes the primitives don't have, made in code: a torus (the ring tops of Tomorrowland's towers), a dome seen from
    // both sides (the launch hall's golden dome, the bubble domes, the moon base), a heightfield (the Moon's cratered ground).
    public static class ProcMesh
    {
        public static Mesh Torus(float r, float tube, int seg = 48, int side = 12)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i <= seg; i++)
                for (int j = 0; j <= side; j++)
                {
                    float a = i * Mathf.PI * 2 / seg, b = j * Mathf.PI * 2 / side;
                    var c = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    var p = c * (r + Mathf.Cos(b) * tube) + Vector3.up * Mathf.Sin(b) * tube;
                    v.Add(p); n.Add((p - c * r).normalized);
                }
            for (int i = 0; i < seg; i++)
                for (int j = 0; j < side; j++)
                {
                    int a = i * (side + 1) + j, b = a + side + 1;
                    t.AddRange(new[] { a, a + 1, b, b, a + 1, b + 1 });
                }
            var m = new Mesh { name = "Torus" }; m.SetVertices(v); m.SetNormals(n); m.SetTriangles(t, 0); m.RecalculateBounds(); return m;
        }

        // a dome (upper half of an ellipsoid r x h x r), both faces drawn (outside and inside), open at the bottom
        public static Mesh Dome(float r, float h, int seg = 40, int rings = 14, bool inside = true, bool outside = true)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            void Add(bool flip)
            {
                int o = v.Count;
                for (int i = 0; i <= rings; i++)
                    for (int j = 0; j <= seg; j++)
                    {
                        float el = i * Mathf.PI / 2 / rings, az = j * Mathf.PI * 2 / seg;
                        var p = new Vector3(Mathf.Cos(el) * Mathf.Cos(az) * r, Mathf.Sin(el) * h, Mathf.Cos(el) * Mathf.Sin(az) * r);
                        var nn = new Vector3(p.x / (r * r), p.y / (h * h), p.z / (r * r)).normalized;
                        v.Add(p); n.Add(flip ? -nn : nn);
                    }
                for (int i = 0; i < rings; i++)
                    for (int j = 0; j < seg; j++)
                    {
                        int a = o + i * (seg + 1) + j, b = a + seg + 1;
                        if (!flip) t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); else t.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
                    }
            }
            if (outside) Add(false); if (inside) Add(true);
            var m = new Mesh { name = "Dome" }; m.SetVertices(v); m.SetNormals(n); m.SetTriangles(t, 0); m.RecalculateBounds(); return m;
        }

        // a ground heightfield: size x size metres centred on the origin, `cells` per side, heights from f(x, z), UVs every `uvTile` m
        public static Mesh Heightfield(float size, int cells, System.Func<float, float, float> f, float uvTile = 8f)
        {
            var v = new Vector3[(cells + 1) * (cells + 1)]; var uv = new Vector2[v.Length]; var t = new int[cells * cells * 6];
            for (int i = 0; i <= cells; i++)
                for (int j = 0; j <= cells; j++)
                {
                    float x = -size / 2 + size * j / cells, z = -size / 2 + size * i / cells;
                    v[i * (cells + 1) + j] = new Vector3(x, f(x, z), z); uv[i * (cells + 1) + j] = new Vector2(x / uvTile, z / uvTile);
                }
            int k = 0;
            for (int i = 0; i < cells; i++)
                for (int j = 0; j < cells; j++)
                {
                    int a = i * (cells + 1) + j, b = a + cells + 1;
                    t[k++] = a; t[k++] = b; t[k++] = a + 1; t[k++] = a + 1; t[k++] = b; t[k++] = b + 1;
                }
            var m = new Mesh { name = "Heightfield", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.vertices = v; m.uv = uv; m.triangles = t; m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        public static GameObject Object(string name, Mesh mesh, Material mat, Transform parent, Vector3 at, bool collider = false, float yaw = 0)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }
    }
}
