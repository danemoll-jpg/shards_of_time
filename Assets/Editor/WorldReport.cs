using System.Text;
using UnityEditor;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 5 item 2: a report of every world prop as Unity imports it (the root's rotation, which way its tallest side points, its
    // lowest point). Written at every build to Resources/Data/WorldProps.txt; a prop must stand upright (its up = the file's up).
    public static class WorldReport
    {
        [MenuItem("Shards of Time/World Props Report")]
        public static void Write()
        {
            var sb = new StringBuilder(); int bad = 0;
            foreach (var file in new[] { "iceage_props" })
            {
                var f = Resources.Load<GameObject>("World/" + file); if (!f) { sb.AppendLine(file + ": missing"); continue; }
                foreach (Transform p in f.transform)
                {
                    var go = Object.Instantiate(p.gameObject); go.transform.SetPositionAndRotation(Vector3.zero, p.localRotation);   // as WorldArt.Prop places it
                    var b = new Bounds(); bool any = false;
                    foreach (var r in go.GetComponentsInChildren<MeshFilter>()) { var bb = TransformBounds(r); if (!any) { b = bb; any = true; } else b.Encapsulate(bb); }
                    bool tall = p.name == "Pine" || p.name == "Stone";
                    bool ok = (!tall || b.size.y > Mathf.Max(b.size.x, b.size.z)) && b.min.y > -.5f * b.size.y && b.min.y < .3f;
                    if (!ok) bad++;
                    sb.AppendLine($"{(ok ? "OK  " : "BAD ")}{file}/{p.name}: root turn {p.localRotation.eulerAngles}; placed: size {b.size} lowest y {b.min.y:0.00}");
                    Object.DestroyImmediate(go);
                }
            }
            sb.Insert(0, bad == 0 ? "World props: all upright and on the ground\n" : $"World props: {bad} NOT upright / on the ground\n");
            System.IO.Directory.CreateDirectory("Assets/Resources/Data");
            System.IO.File.WriteAllText("Assets/Resources/Data/WorldProps.txt", sb.ToString());
            Debug.Log("[WorldReport]\n" + sb);
        }
        static Bounds TransformBounds(MeshFilter mf)
        {
            var m = mf.sharedMesh; var t = mf.transform; var b = new Bounds(t.TransformPoint(m.vertices[0]), Vector3.zero);
            foreach (var v in m.vertices) b.Encapsulate(t.TransformPoint(v));
            return b;
        }
    }
}
