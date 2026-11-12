using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShardsOfTime
{
    // Fix 10.1, item 4: thin cloth, rope and masts never push the camera in and never hide the player. A world registers its see-through
    // parts (by part name: <part>__<Material>); each gets a copy of its mesh as a collider in a physics scene of its own (so no other
    // query in the game ever hits a sail), or keeps its own solid one moved to SolidLayer (the player still bumps into a mast; the camera's deoccluder ignores the
    // layer). Every frame two sight lines (player's middle and head <-> camera) find what is in between; those parts fade to see-through
    // (a transparent copy of their material, Materials/Water's URP Lit transparent variant) and back when clear. Off in cutscenes.
    public sealed class SeeThrough : MonoBehaviour
    {
        public const int SolidLayer = 29;
        public const float FadedAlpha = .2f, FadeTime = .15f;
        public static readonly string[] PirateParts = { "Sail", "Sailcloth", "Rope", "RopeLadder", "Mast", "Yard", "Flag", "FlagPole", "Bowsprit", "BrokenMast" };
        public static readonly string[] MedievalParts = { "Banner", "Bunting", "Pennant" };

        sealed class Entry { public Renderer r; public Material[] orig, fade; public Color[] col; public float a = 1f; public bool hit; public GameObject probe; }
        static readonly List<Entry> entries = new List<Entry>();
        static readonly Dictionary<Collider, Entry> byCollider = new Dictionary<Collider, Entry>();
        static readonly RaycastHit[] hits = new RaycastHit[32];
        static Material template; static Scene probeScene; static PhysicsScene probes;
        public static int Registered => entries.Count;
        public static int FadedNow { get; private set; }
        public static int FadesEver { get; private set; }
        public static string FadedNames { get; private set; } = "";

        public static void Register(Transform root, string[] parts)
        {
            if (!probeScene.IsValid()) { probeScene = SceneManager.CreateScene("SeeThrough probes", new CreateSceneParameters(LocalPhysicsMode.Physics3D)); probes = probeScene.GetPhysicsScene(); }
            foreach (var e in entries) if (!e.r && e.probe) Destroy(e.probe);
            entries.RemoveAll(e => !e.r);
            var dead = new List<Collider>(); foreach (var k in byCollider.Keys) if (!k) dead.Add(k); foreach (var k in dead) byCollider.Remove(k);
            if (!root) return;
            int n0 = entries.Count;
            var found = new List<MeshFilter>();
            foreach (var mf0 in root.GetComponentsInChildren<MeshFilter>(true))
            {
                string nm0 = mf0.name; int k0 = nm0.IndexOf("__"); string part0 = k0 > 0 ? nm0.Substring(0, k0) : nm0;
                if (System.Array.IndexOf(parts, part0) >= 0 && mf0.sharedMesh) found.AddRange(Split(mf0));
            }
            foreach (var mf in found)
            {
                string nm = mf.name;
                var r = mf.GetComponent<Renderer>(); if (!r) continue;
                var e = new Entry { r = r, orig = r.sharedMaterials };
                Collider c = mf.GetComponent<MeshCollider>();
                if (c) mf.gameObject.layer = SolidLayer;
                else
                {
                    var probe = new GameObject("SeeThrough probe " + nm); SceneManager.MoveGameObjectToScene(probe, probeScene);
                    probe.transform.SetPositionAndRotation(mf.transform.position, mf.transform.rotation); probe.transform.localScale = mf.transform.lossyScale;
                    var mc = probe.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; c = mc; e.probe = probe;
                }
                entries.Add(e); byCollider[c] = e;
            }
            PlayLog.Line($"see-through parts: {entries.Count - n0} under {root.name}");
        }

        // one merged part (all the sails, all the ropes) becomes one piece per connected island (vertices welded by position), so only
        // the sail in the way fades. A solid part's collider moves onto its pieces.
        static List<MeshFilter> Split(MeshFilter mf)
        {
            var one = new List<MeshFilter> { mf }; var m = mf.sharedMesh;
            if (!m.isReadable || m.subMeshCount != 1) return one;
            var v = m.vertices; var tri = m.triangles; int n = v.Length;
            var par = new int[n]; for (int i = 0; i < n; i++) par[i] = i;
            int Find(int x) { while (par[x] != x) { par[x] = par[par[x]]; x = par[x]; } return x; }
            void Join(int a, int b) { a = Find(a); b = Find(b); if (a != b) par[a] = b; }
            var weld = new Dictionary<Vector3Int, int>();
            for (int i = 0; i < n; i++) { var key = Vector3Int.RoundToInt(v[i] * 500f); if (weld.TryGetValue(key, out int j)) Join(i, j); else weld[key] = i; }
            for (int t = 0; t < tri.Length; t += 3) { Join(tri[t], tri[t + 1]); Join(tri[t], tri[t + 2]); }
            var groups = new Dictionary<int, List<int>>();
            for (int t = 0; t < tri.Length; t += 3) { int r = Find(tri[t]); if (!groups.TryGetValue(r, out var l)) groups[r] = l = new List<int>(); l.Add(t); }
            if (groups.Count < 2 || groups.Count > 120) return one;
            var nor = m.normals; var uv = m.uv; var col = m.colors;
            var src = mf.GetComponent<MeshRenderer>(); var solid = mf.GetComponent<MeshCollider>();
            var outList = new List<MeshFilter>(); int gi = 0;
            foreach (var g in groups.Values)
            {
                var map = new Dictionary<int, int>(); var nv = new List<Vector3>(); var nn = new List<Vector3>(); var nu = new List<Vector2>(); var nc = new List<Color>(); var nt = new List<int>();
                foreach (int t in g) for (int c = 0; c < 3; c++)
                {
                    int o = tri[t + c];
                    if (!map.TryGetValue(o, out int ni)) { ni = nv.Count; map[o] = ni; nv.Add(v[o]); if (nor.Length == n) nn.Add(nor[o]); if (uv.Length == n) nu.Add(uv[o]); if (col.Length == n) nc.Add(col[o]); }
                    nt.Add(ni);
                }
                var mesh = new Mesh { name = m.name + " piece " + gi }; mesh.SetVertices(nv); if (nn.Count == nv.Count) mesh.SetNormals(nn); if (nu.Count == nv.Count) mesh.SetUVs(0, nu); if (nc.Count == nv.Count) mesh.SetColors(nc);
                mesh.SetTriangles(nt, 0); mesh.RecalculateBounds();
                var go = new GameObject(mf.name + " piece " + gi++); go.transform.SetParent(mf.transform.parent, false);
                go.transform.localPosition = mf.transform.localPosition; go.transform.localRotation = mf.transform.localRotation; go.transform.localScale = mf.transform.localScale;
                go.layer = mf.gameObject.layer;
                var f = go.AddComponent<MeshFilter>(); f.sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>(); if (src) { r.sharedMaterials = src.sharedMaterials; r.shadowCastingMode = src.shadowCastingMode; r.receiveShadows = src.receiveShadows; }
                if (solid) go.AddComponent<MeshCollider>().sharedMesh = mesh;
                outList.Add(f);
            }
            if (src) src.enabled = false; if (solid) Destroy(solid);
            return outList;
        }

        static Material[] FadeMats(Entry e)
        {
            if (e.fade != null) return e.fade;
            if (!template) template = Resources.Load<Material>("Materials/Water");
            e.fade = new Material[e.orig.Length]; e.col = new Color[e.orig.Length];
            for (int i = 0; i < e.orig.Length; i++)
            {
                var o = e.orig[i]; var m = new Material(template) { name = (o ? o.name : "?") + " (see-through)" };
                var c = o && o.HasProperty("_BaseColor") ? o.GetColor("_BaseColor") : o && o.HasProperty("_Color") ? o.color : Color.white;
                if (o && o.HasProperty("_BaseMap") && o.GetTexture("_BaseMap")) { m.SetTexture("_BaseMap", o.GetTexture("_BaseMap")); m.SetTextureScale("_BaseMap", o.GetTextureScale("_BaseMap")); }
                else m.SetTexture("_BaseMap", null);
                foreach (var p in new[] { "_Smoothness", "_Metallic" }) if (o && o.HasProperty(p) && m.HasProperty(p)) m.SetFloat(p, o.GetFloat(p));
                e.col[i] = c; m.SetColor("_BaseColor", c); e.fade[i] = m;
            }
            return e.fade;
        }

        void LateUpdate()
        {
            if (entries.Count == 0) { FadedNow = 0; return; }
            bool on = Party.I && CameraRig.I && !Game.InCutscene && Game.I && Game.I.State != Game.Mode.Title;
            foreach (var e in entries) e.hit = false;
            if (on)
            {
                var cam = CameraRig.I.Cam.transform.position; var c = Party.I.Player.Motor.Center;
                foreach (var t in new[] { c, c + Vector3.up * .6f })
                {
                    var d = cam - t; float len = d.magnitude; if (len < .05f) continue; d /= len;
                    Mark(Physics.RaycastNonAlloc(t, d, hits, len, 1 << SolidLayer, QueryTriggerInteraction.Ignore));
                    Mark(Physics.RaycastNonAlloc(cam, -d, hits, len, 1 << SolidLayer, QueryTriggerInteraction.Ignore));
                    if (probes.IsValid()) { Mark(probes.Raycast(t, d, hits, len)); Mark(probes.Raycast(cam, -d, hits, len)); }
                }
            }
            int faded = 0; string names = "";
            foreach (var e in entries)
            {
                if (!e.r) continue;
                float was = e.a;
                e.a = Mathf.MoveTowards(e.a, e.hit ? FadedAlpha : 1f, Time.unscaledDeltaTime / FadeTime * (1f - FadedAlpha));
                if (e.a < .999f)
                {
                    var f = FadeMats(e);
                    if (was >= .999f) { e.r.sharedMaterials = f; FadesEver++; }
                    for (int i = 0; i < f.Length; i++) { var col = e.col[i]; col.a = e.a; f[i].SetColor("_BaseColor", col); }
                    faded++; if (names.Length < 200) names += e.r.name + " ";
                }
                else if (was < .999f) e.r.sharedMaterials = e.orig;
            }
            FadedNow = faded; FadedNames = names;
        }

        static void Mark(int n) { for (int i = 0; i < n; i++) if (hits[i].collider && byCollider.TryGetValue(hits[i].collider, out var e)) e.hit = true; }
    }
}
