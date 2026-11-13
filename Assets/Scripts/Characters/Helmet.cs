using UnityEngine;

namespace ShardsOfTime
{
    // Round 13, item 1: the bubble helmets (Dan's sheet note: the Moon models have none on purpose; clear glass doesn't survive image-to-3D).
    // A see-through glass bubble on the head bone of a Moon character, in play and in the cutscenes: round for Dan, Abi and Muscle Man, on a
    // brass ring for the Violinist, silver-rimmed and small for Kira. Two faces (the inside shows too), no shadow, no collider.
    public static class Helmet
    {
        public static int Made { get; private set; }

        public static Transform HeadOf(GameObject model)
        {
            if (!model) return null;
            var an = model.GetComponentInChildren<Animator>();
            if (an && an.isHuman) { var h = an.GetBoneTransform(HumanBodyBones.Head); if (h) return h; }
            foreach (var t in model.GetComponentsInChildren<Transform>()) { var n = t.name.ToLowerInvariant(); if (n == "head" || n.EndsWith(":head") || n.EndsWith("_head") || n == "def-head") return t; }
            foreach (var t in model.GetComponentsInChildren<Transform>()) if (t.name.ToLowerInvariant().Contains("head") && !t.name.ToLowerInvariant().Contains("end")) return t;
            return null;
        }

        // kind: "person", "big" (Muscle Man), "violinist", "kira"
        public static GameObject Attach(GameObject model, string kind)
        {
            var head = HeadOf(model); if (!head) return null;
            var old = head.Find("Bubble helmet"); if (old) return old.gameObject;
            bool kira = kind == "kira";
            // the head bone's scale in the world (models are scaled at import): the bubble is sized in world metres
            float s = head.lossyScale.x > 1e-4f ? 1f / head.lossyScale.x : 1f;
            float r = kira ? .2f : kind == "big" ? .27f : .24f;
            var root = new GameObject("Bubble helmet"); root.transform.SetParent(head, false);
            root.transform.localPosition = (kira ? new Vector3(0, .02f, .04f) : new Vector3(0, .09f, .02f)) * s;
            root.transform.localScale = Vector3.one * s;
            var glass = Placeholders.Mat(new Color(.82f, .93f, 1f, .2f), "Water");
            var bubble = new GameObject("Glass"); bubble.transform.SetParent(root.transform, false);
            bubble.AddComponent<MeshFilter>().sharedMesh = SphereBoth(r);
            var mr = bubble.AddComponent<MeshRenderer>(); mr.sharedMaterial = glass; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // the neck ring: brass for the Violinist, silver for the rest (Kira's rim is silver too)
            var ringMat = Placeholders.Mat(kind == "violinist" ? new Color(.86f, .66f, .3f) : new Color(.82f, .84f, .88f));
            var ring = new GameObject("Ring"); ring.transform.SetParent(root.transform, false); ring.transform.localPosition = new Vector3(0, -r * .72f, 0);
            ring.AddComponent<MeshFilter>().sharedMesh = ProcMesh.Torus(r * .72f, r * .09f, 32, 8);
            ring.AddComponent<MeshRenderer>().sharedMaterial = ringMat;
            // a glint: a little glowing highlight on the glass
            var glint = Placeholders.Prim(PrimitiveType.Sphere, root.transform, new Vector3(-r * .45f, r * .5f, r * .6f), Vector3.one * r * .16f, Placeholders.Mat(new Color(1f, 1f, 1f), "Glow"), false, "Glint");
            glint.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Made++;
            return root;
        }

        public static string KindFor(string modelName) =>
            modelName.StartsWith("kira") ? "kira" : modelName.StartsWith("muscleman") ? "big" : modelName.StartsWith("violinist") ? "violinist" : "person";

        static Mesh sphere; static float sphereR;
        static Mesh SphereBoth(float r)
        {
            // a UV sphere drawn from both sides (the outside, and the inside seen through it)
            var top = ProcMesh.Dome(r, r, 28, 10);
            var v = top.vertices; var n = top.normals; var t = top.triangles;
            var v2 = new Vector3[v.Length * 2]; var n2 = new Vector3[n.Length * 2]; var t2 = new int[t.Length * 2];
            for (int i = 0; i < v.Length; i++) { v2[i] = v[i]; n2[i] = n[i]; v2[i + v.Length] = new Vector3(v[i].x, -v[i].y, v[i].z); n2[i + v.Length] = new Vector3(n[i].x, -n[i].y, n[i].z); }
            for (int i = 0; i < t.Length; i += 3) { t2[i] = t[i]; t2[i + 1] = t[i + 1]; t2[i + 2] = t[i + 2]; t2[t.Length + i] = t[i] + v.Length; t2[t.Length + i + 1] = t[i + 2] + v.Length; t2[t.Length + i + 2] = t[i + 1] + v.Length; }
            var m = new Mesh { name = "Helmet bubble" }; m.vertices = v2; m.normals = n2; m.triangles = t2; m.RecalculateBounds();
            return m;
        }
    }
}
