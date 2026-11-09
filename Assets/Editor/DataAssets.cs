using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShardsOfTime
{
    // Round 1: the data and material assets the game loads from Resources, made with their defaults when missing (never overwritten:
    // once they exist they are Dan's to edit in the Inspector).
    //   Resources/Data/Characters/Dan|Abi|Kira.asset   CharacterStats (item 6)
    //   Resources/Data/Worlds/Playground.asset         WorldData: shards, requirements, what each needs (item 9)
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
            BaseMaterial("Lit", "Universal Render Pipeline/Lit", false);
            BaseMaterial("Glow", "Universal Render Pipeline/Unlit", false);
            BaseMaterial("Water", "Universal Render Pipeline/Lit", true);
            AssetDatabase.SaveAssets();
            HookPlaceholders();
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
