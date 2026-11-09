using System.Collections.Generic;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 1: graybox bodies and props from simple shapes, coloured per hero (no real models this round). The same builders make the
    // cutscene actors (Editor/CutsceneAuthoring.cs), so a cutscene looks like the game. Materials come from the material assets in
    // Resources/Materials (made by Editor/DataAssets.cs, so the build keeps their shader variants), tinted per object.
    public static class Placeholders
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Mesh crystal;

        // Editor authoring (cutscene prefabs) needs materials and meshes saved as assets: Editor/DataAssets.cs sets these hooks.
        public static System.Func<Color, string, Material> EditorMat;
        public static System.Func<Mesh, Mesh> EditorMesh;

        // kind: "Lit", "Glow" (unlit), "Water" (transparent lit)
        public static Material Mat(Color c, string kind = "Lit")
        {
            if (!Application.isPlaying && EditorMat != null) return EditorMat(c, kind);
            string key = kind + ColorUtility.ToHtmlStringRGBA(c);
            if (cache.TryGetValue(key, out var m) && m) return m;
            var baseMat = Resources.Load<Material>("Materials/" + kind);
            if (baseMat) m = new Material(baseMat);
            else m = new Material(Shader.Find(kind == "Glow" ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
            m.name = key;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            m.color = c;
            if (kind == "Lit" && m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", .25f);
            cache[key] = m; return m;
        }

        public static GameObject Prim(PrimitiveType t, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool keepCollider = false, string name = null)
        {
            var go = GameObject.CreatePrimitive(t);
            if (name != null) go.name = name;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>(); r.sharedMaterial = mat;
            return go;
        }

        // A hero's body: a capsule in the hero's colour with eyes (so you can see which way they face). Dan has the necklace gem;
        // Abi a ponytail; Kira is small and grey with folded ears that never rise, golden-amber eyes and a tail (rule 9, the Kira lock).
        public static Transform HeroBody(CharacterStats s, Transform parent)
        {
            var root = new GameObject("Body").transform; root.SetParent(parent, false);
            var col = Mat(s.color); var eyeWhite = Mat(Color.white); var pupil = Mat(new Color(.08f, .08f, .1f));
            if (s.displayName == "Kira")
            {
                float h = s.height;   // 0.5 m at the shoulder-ish
                Prim(PrimitiveType.Capsule, root, new Vector3(0, h * .45f, -.05f), new Vector3(.36f, .3f, .36f), col).transform.localRotation = Quaternion.Euler(90, 0, 0);   // body, lying along z
                var head = Prim(PrimitiveType.Sphere, root, new Vector3(0, h * .72f, .26f), new Vector3(.3f, .27f, .27f), col, name: "Head").transform;
                // folded ears: flat, tipped forward, never raised
                Prim(PrimitiveType.Sphere, head, new Vector3(-.28f, .38f, .1f), new Vector3(.32f, .14f, .3f), col, name: "EarL").transform.localRotation = Quaternion.Euler(25, 0, 20);
                Prim(PrimitiveType.Sphere, head, new Vector3(.28f, .38f, .1f), new Vector3(.32f, .14f, .3f), col, name: "EarR").transform.localRotation = Quaternion.Euler(25, 0, -20);
                var amber = Mat(new Color(1f, .72f, .12f));
                Prim(PrimitiveType.Sphere, head, new Vector3(-.2f, .05f, .42f), new Vector3(.2f, .22f, .12f), amber);
                Prim(PrimitiveType.Sphere, head, new Vector3(.2f, .05f, .42f), new Vector3(.2f, .22f, .12f), amber);
                Prim(PrimitiveType.Sphere, head, new Vector3(-.2f, .05f, .48f), new Vector3(.06f, .16f, .05f), pupil);
                Prim(PrimitiveType.Sphere, head, new Vector3(.2f, .05f, .48f), new Vector3(.06f, .16f, .05f), pupil);
                var tail = Prim(PrimitiveType.Capsule, root, new Vector3(0, h * .7f, -.32f), new Vector3(.07f, .2f, .07f), col, name: "Tail").transform;
                tail.localRotation = Quaternion.Euler(-30, 0, 0);
                for (int i = 0; i < 4; i++) Prim(PrimitiveType.Capsule, root, new Vector3(i < 2 ? -.09f : .09f, h * .14f, i % 2 == 0 ? .12f : -.2f), new Vector3(.08f, .1f, .08f), col);
                return root;
            }
            float H = s.height, r = s.radius;
            Prim(PrimitiveType.Capsule, root, new Vector3(0, H * .4f, 0), new Vector3(r * 2f, H * .4f, r * 2f), col, name: "Torso");
            var hd = Prim(PrimitiveType.Sphere, root, new Vector3(0, H * .86f, 0), Vector3.one * (H * .2f), Mat(new Color(.96f, .8f, .68f)), name: "Head").transform;
            Prim(PrimitiveType.Sphere, hd, new Vector3(-.2f, .08f, .4f), new Vector3(.22f, .26f, .16f), eyeWhite);
            Prim(PrimitiveType.Sphere, hd, new Vector3(.2f, .08f, .4f), new Vector3(.22f, .26f, .16f), eyeWhite);
            Prim(PrimitiveType.Sphere, hd, new Vector3(-.2f, .08f, .47f), new Vector3(.1f, .14f, .06f), pupil);
            Prim(PrimitiveType.Sphere, hd, new Vector3(.2f, .08f, .47f), new Vector3(.1f, .14f, .06f), pupil);
            if (s.displayName == "Dan")
            {
                Prim(PrimitiveType.Sphere, hd, new Vector3(0, .42f, -.05f), new Vector3(1.04f, .5f, 1.04f), Mat(new Color(.45f, .3f, .17f)), name: "Hair");
                var gem = Prim(PrimitiveType.Cube, root, new Vector3(0, H * .64f, r + .02f), new Vector3(.09f, .13f, .05f), Mat(new Color(.35f, .85f, 1f), "Glow"), name: "Necklace");
                gem.transform.localRotation = Quaternion.Euler(0, 0, 45);
            }
            else if (s.displayName == "Abi")
            {
                Prim(PrimitiveType.Sphere, hd, new Vector3(0, .4f, -.08f), new Vector3(1.06f, .52f, 1.06f), Mat(new Color(.18f, .11f, .08f)), name: "Hair");
                Prim(PrimitiveType.Capsule, hd, new Vector3(0, -.3f, -.55f), new Vector3(.35f, .7f, .35f), Mat(new Color(.18f, .11f, .08f)), name: "Ponytail");
            }
            return root;
        }

        // the attack's sweep (shown for a moment when attacking)
        public static Transform Swipe(Transform hero, CharacterStats s)
        {
            var go = Prim(PrimitiveType.Sphere, hero, new Vector3(0, s.height * .5f, s.attackRange), Vector3.one * s.attackRadius * 1.6f, Mat(new Color(1f, 1f, .85f, 1f), "Glow"), name: "Swipe");
            go.transform.localScale = new Vector3(s.attackRadius * 2f, s.attackRadius * .5f, s.attackRadius * 1.2f);
            go.SetActive(false);
            return go.transform;
        }

        public static Mesh Crystal
        {
            get
            {
                if (crystal) return crystal;
                // a stretched octahedron: the shard
                var v = new[] { new Vector3(0, .5f, 0), new Vector3(.22f, 0, 0), new Vector3(0, 0, .22f), new Vector3(-.22f, 0, 0), new Vector3(0, 0, -.22f), new Vector3(0, -.35f, 0) };
                var tris = new[] { 0, 2, 1, 0, 3, 2, 0, 4, 3, 0, 1, 4, 5, 1, 2, 5, 2, 3, 5, 3, 4, 5, 4, 1 };
                var verts = new Vector3[tris.Length]; var idx = new int[tris.Length];
                for (int i = 0; i < tris.Length; i++) { verts[i] = v[tris[i]]; idx[i] = i; }
                crystal = new Mesh { name = "Crystal", vertices = verts, triangles = idx }; crystal.RecalculateNormals(); crystal.RecalculateBounds();
                if (!Application.isPlaying && EditorMesh != null) crystal = EditorMesh(crystal);
                return crystal;
            }
        }

        public static GameObject CrystalObject(string name, Color c, float size, string kind = "Glow")
        {
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = Crystal;
            go.AddComponent<MeshRenderer>().sharedMaterial = Mat(c, kind);
            go.transform.localScale = Vector3.one * size;
            return go;
        }

        public static GameObject PickupVisual(Pickup.Kind kind, Color c, string id)
        {
            switch (kind)
            {
                case Pickup.Kind.Shard: { var g = CrystalObject("Shard " + id, c, 1.1f); return g; }
                case Pickup.Kind.StoryShard: { var g = CrystalObject("Story shard " + id, c, 2.4f); return g; }
                case Pickup.Kind.Heal:
                {
                    var g = new GameObject("Heal heart"); var m = Mat(c, "Glow");
                    Prim(PrimitiveType.Sphere, g.transform, new Vector3(-.12f, .08f, 0), Vector3.one * .3f, m); Prim(PrimitiveType.Sphere, g.transform, new Vector3(.12f, .08f, 0), Vector3.one * .3f, m);
                    Prim(PrimitiveType.Cube, g.transform, new Vector3(0, -.08f, 0), Vector3.one * .26f, m).transform.localRotation = Quaternion.Euler(0, 0, 45);
                    return g;
                }
                case Pickup.Kind.HeartUpgrade:
                {
                    var g = new GameObject("Heart upgrade " + id); var m = Mat(new Color(1f, .78f, .2f), "Glow"); var core = Mat(new Color(1f, .3f, .45f), "Glow");
                    Prim(PrimitiveType.Sphere, g.transform, new Vector3(-.2f, .14f, 0), Vector3.one * .5f, m); Prim(PrimitiveType.Sphere, g.transform, new Vector3(.2f, .14f, 0), Vector3.one * .5f, m);
                    Prim(PrimitiveType.Cube, g.transform, new Vector3(0, -.14f, 0), Vector3.one * .44f, m).transform.localRotation = Quaternion.Euler(0, 0, 45);
                    Prim(PrimitiveType.Sphere, g.transform, new Vector3(0, .05f, -.2f), Vector3.one * .3f, core);
                    return g;
                }
                default:
                {
                    var g = new GameObject("Ability " + id);
                    Prim(PrimitiveType.Capsule, g.transform, Vector3.zero, new Vector3(.25f, .5f, .08f), Mat(new Color(1f, .97f, .8f), "Glow")).transform.localRotation = Quaternion.Euler(0, 0, 25);
                    Prim(PrimitiveType.Cylinder, g.transform, Vector3.zero, new Vector3(1.2f, .02f, 1.2f), Mat(c, "Glow")).transform.localRotation = Quaternion.Euler(90, 0, 0);
                    return g;
                }
            }
        }

        public static void Puff(Vector3 at, Color c)
        {
            var root = new GameObject("Puff"); root.transform.position = at;
            var m = Mat(c, "Glow");
            for (int i = 0; i < 8; i++)
            {
                var p = Prim(PrimitiveType.Sphere, root.transform, Vector3.zero, Vector3.one * .18f, m);
                p.AddComponent<PuffBit>().dir = Quaternion.Euler(Random.Range(-60f, 10f), i * 45f, 0) * Vector3.forward * Random.Range(2f, 3.5f);
            }
            Object.Destroy(root, .6f);
        }

        sealed class PuffBit : MonoBehaviour
        {
            public Vector3 dir; float t;
            void Update() { t += Time.deltaTime; transform.localPosition += dir * Time.deltaTime; dir *= .9f; transform.localScale = Vector3.one * Mathf.Lerp(.2f, 0f, t / .6f); }
        }
    }
}
