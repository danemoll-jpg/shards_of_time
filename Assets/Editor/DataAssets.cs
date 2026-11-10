using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShardsOfTime
{
    // Round 1: the data and material assets the game loads from Resources, made with their defaults when missing (never overwritten:
    // once they exist they are Dan's to edit in the Inspector).
    //   Resources/Data/Characters/Dan|Abi|Kira.asset   CharacterStats (item 6)
    //   Resources/Data/Worlds/Playground.asset         WorldData: shards, requirements, what each needs (item 9)
    //   Resources/Data/KiraForms/Home|Sabertooth.asset KiraForm: Kira's model and abilities per form (Round 2, item 7)
    //   Resources/Materials/Lit|Glow|Water.mat         base materials (URP Lit, Unlit, transparent Lit) the game tints at run time
    //   Resources/Materials/Tints/*.mat, Resources/Meshes/*  saved tints / meshes used by authored prefabs (cutscene actors)
    public static class DataAssets
    {
        const string Data = "Assets/Resources/Data", Mats = "Assets/Resources/Materials";

        [MenuItem("Shards of Time/Make Missing Data Assets")]
        public static void Ensure()
        {
            Directory.CreateDirectory(Data + "/Characters"); Directory.CreateDirectory(Data + "/Worlds"); Directory.CreateDirectory(Mats + "/Tints");
            foreach (var n in new[] { "Dan", "Abi", "Kira" })
            {
                string path = $"{Data}/Characters/{n}.asset";
                if (!File.Exists(path)) AssetDatabase.CreateAsset(CharacterStats.Defaults(n), path);
            }
            string wp = Data + "/Worlds/Playground.asset";
            if (!File.Exists(wp)) AssetDatabase.CreateAsset(WorldData.Defaults(), wp);
            else Upgrade(AssetDatabase.LoadAssetAtPath<WorldData>(wp));
            // Round 4: the Ice Age (made once; then Dan's to edit)
            string ia = Data + "/Worlds/IceAge.asset";
            // Round 5: the bigger valley (60 shards, requirement 35, gates as data): a Round 4 asset (no gates) is made again
            if (File.Exists(ia) && AssetDatabase.LoadAssetAtPath<WorldData>(ia) is WorldData old && old.gates.Count == 0) { AssetDatabase.DeleteAsset(ia); Debug.Log("DataAssets: IceAge.asset remade for Round 5"); }
            if (!File.Exists(ia)) AssetDatabase.CreateAsset(WorldData.IceAgeDefaults(), ia);
            string eg = Data + "/Worlds/" + WorldData.Egypt + ".asset";   // Round 6
            if (!File.Exists(eg)) AssetDatabase.CreateAsset(WorldData.EgyptDefaults(), eg);
            // Round 4: Dan learns the ground pound (an existing Dan.asset had no locked ability)
            var danStats = AssetDatabase.LoadAssetAtPath<CharacterStats>(Data + "/Characters/Dan.asset");
            if (danStats && string.IsNullOrEmpty(danStats.lockedAbility)) { danStats.lockedAbility = Abilities.GroundPound; EditorUtility.SetDirty(danStats); }
            // Round 7: Kira learns the pounce (Bethlehem's Violinist)
            var kiraStats = AssetDatabase.LoadAssetAtPath<CharacterStats>(Data + "/Characters/Kira.asset");
            if (kiraStats && string.IsNullOrEmpty(kiraStats.lockedAbility)) { kiraStats.lockedAbility = Abilities.Pounce; EditorUtility.SetDirty(kiraStats); }
            string be = Data + "/Worlds/" + WorldData.Bethlehem + ".asset";   // Round 7
            if (!File.Exists(be)) AssetDatabase.CreateAsset(WorldData.BethlehemDefaults(), be);
            Directory.CreateDirectory(Data + "/KiraForms");
            foreach (var f in new[] { KiraForm.Home, KiraForm.Sabertooth, KiraForm.Mummy, KiraForm.Lamb })
            {
                string path = $"{Data}/KiraForms/{f}.asset";
                if (!File.Exists(path)) AssetDatabase.CreateAsset(KiraForm.Defaults(f), path);
            }
            BaseMaterial("Lit", "Universal Render Pipeline/Lit", false);
            BaseMaterial("Glow", "Universal Render Pipeline/Unlit", false);
            BaseMaterial("Water", "Universal Render Pipeline/Lit", true);
            ParticleMaterial();
            LyreClip.Ensure();   // Fix 6.1: the Violinist's lyre-playing clip
            AssetDatabase.SaveAssets();
            HookPlaceholders();
        }

        // Round 2: an existing world asset gets what's new in the defaults (Kira's form per area, new shards such as B06 behind the ice
        // wall, the abilities a new form brings on the first visit) and keeps everything Dan has changed.
        static void Upgrade(WorldData w)
        {
            if (!w) return;
            var d = WorldData.Defaults(); bool changed = false;
            foreach (var da in d.areas)
            {
                var a = w.Area(da.id); if (a == null) continue;
                if (string.IsNullOrEmpty(a.kiraForm)) { a.kiraForm = da.kiraForm; changed = true; }
                if (string.IsNullOrEmpty(a.outfit)) { a.outfit = da.outfit; changed = true; }   // Round 3
                foreach (var x in da.firstVisit)
                    if (System.Array.IndexOf(a.firstVisit, x) < 0) { a.firstVisit = a.firstVisit.Concat(new[] { x }).ToArray(); changed = true; }
            }
            foreach (var ds in d.shards)
                if (!w.shards.Any(s => s.id == ds.id)) { w.shards.Add(ds); changed = true; }
            if (changed) { EditorUtility.SetDirty(w); Debug.Log("DataAssets: Playground world data upgraded (new defaults added)"); }
        }

        // Round 4: the particles' material (snow, steam, the Violinist's notes): a soft round dot, unlit, alpha-blended
        static void ParticleMaterial()
        {
            string path = $"{Mats}/Particle.mat";
            if (File.Exists(path)) return;
            string texPath = $"{Mats}/ParticleDot.png";
            if (!File.Exists(texPath))
            {
                var t = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(32, 32)) / 32f;
                        t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1 - d) * Mathf.Clamp01(1 - d)));
                    }
                File.WriteAllBytes(texPath, t.EncodeToPNG()); Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(texPath);
                var ti = (TextureImporter)AssetImporter.GetAtPath(texPath); ti.alphaIsTransparency = true; ti.mipmapEnabled = false; ti.SaveAndReimport();
            }
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Particle" };
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)RenderQueue.Transparent; m.SetOverrideTag("RenderType", "Transparent");
            AssetDatabase.CreateAsset(m, path);
        }

        static void BaseMaterial(string name, string shader, bool transparent)
        {
            string path = $"{Mats}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m) return;
            m = new Material(Shader.Find(shader)) { name = name };
            m.SetColor("_BaseColor", Color.white);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", .25f);
            if (transparent) MakeTransparent(m);
            AssetDatabase.CreateAsset(m, path);
        }

        public static void MakeTransparent(Material m)
        {
            m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One); m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0); m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("DepthOnly", false); m.SetShaderPassEnabled("ShadowCaster", false);
        }

        // Editor-made objects (cutscene prefabs) get tints and meshes saved as assets, so a prefab keeps them.
        [InitializeOnLoadMethod]
        public static void HookPlaceholders()
        {
            Placeholders.EditorMat = (c, kind) =>
            {
                string key = kind + "_" + ColorUtility.ToHtmlStringRGBA(c);
                string path = $"{Mats}/Tints/{key}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m) return m;
                var baseMat = AssetDatabase.LoadAssetAtPath<Material>($"{Mats}/{kind}.mat");
                if (!baseMat) { Ensure(); baseMat = AssetDatabase.LoadAssetAtPath<Material>($"{Mats}/{kind}.mat"); }
                Directory.CreateDirectory(Mats + "/Tints");
                m = new Material(baseMat) { name = key }; m.SetColor("_BaseColor", c);
                AssetDatabase.CreateAsset(m, path);
                return m;
            };
            Placeholders.EditorMesh = mesh =>
            {
                Directory.CreateDirectory("Assets/Resources/Meshes");
                string path = $"Assets/Resources/Meshes/{mesh.name}.asset";
                var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (saved) return saved;
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            };
        }
    }
}
