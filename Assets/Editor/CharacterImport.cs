using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 2, items 7-9: the characters in Assets/Resources/Characters (copied there by Tools/import_characters.py).
    //  - Import settings: Kira's forms Generic (her own rig, clips as takes, loops: idle walk run fall swim), people Humanoid
    //    (Round 3), the necklace a plain mesh. No materials from the FBX (URP materials are made below).
    //  - MakeMaterials: <name>.mat (URP Lit) from Meshy's textures <name>_albedo / _normal / _mask.png.
    //  - KiraLock: rule 9 checked on every Kira form model: no ear bones, solid grey fur with no white, golden-amber eyes.
    //    Written to Resources/Data/KiraLock.txt (the exe check logs it) and to BUILD_INFO.txt.
    public sealed class CharacterImport : AssetPostprocessor
    {
        const string Dir = "Assets/Resources/Characters";
        static readonly HashSet<string> Loops = new HashSet<string> { "idle", "walk", "run", "fall", "swim" };

        static bool Ours(string path) => path.Replace('\\', '/').StartsWith(Dir + "/");
        static bool IsKira(string path) => Path.GetFileNameWithoutExtension(path).StartsWith("kira");

        void OnPreprocessModel()
        {
            if (!Ours(assetPath)) return;
            var m = (ModelImporter)assetImporter;
            m.materialImportMode = ModelImporterMaterialImportMode.None;
            m.bakeAxisConversion = true;
            if (Path.GetFileNameWithoutExtension(assetPath) == "necklace")
            {
                m.animationType = ModelImporterAnimationType.None; m.importAnimation = false; return;
            }
            if (IsKira(assetPath))
            {
                m.animationType = ModelImporterAnimationType.Generic;
                m.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                m.importAnimation = true;
            }
        }

        void OnPreprocessAnimation()
        {
            if (!Ours(assetPath) || !IsKira(assetPath)) return;
            var m = (ModelImporter)assetImporter;
            var clips = m.defaultClipAnimations;
            foreach (var c in clips)
            {
                string n = c.name.Contains("|") ? c.name.Substring(c.name.LastIndexOf('|') + 1) : c.name;
                c.name = n; c.loopTime = Loops.Contains(n); c.loopPose = false;
            }
            m.clipAnimations = clips;
        }

        void OnPreprocessTexture()
        {
            if (!Ours(assetPath)) return;
            var t = (TextureImporter)assetImporter;
            string n = Path.GetFileNameWithoutExtension(assetPath);
            if (n.EndsWith("_normal")) t.textureType = TextureImporterType.NormalMap;
            if (n.EndsWith("_mask")) t.sRGBTexture = false;
            t.maxTextureSize = 2048;
        }

        [MenuItem("Shards of Time/Make Character Materials")]
        public static void MakeMaterials()
        {
            if (!Directory.Exists(Dir)) return;
            foreach (var albedo in Directory.GetFiles(Dir, "*_albedo.png"))
            {
                string n = Path.GetFileName(albedo).Replace("_albedo.png", "");
                string path = $"{Dir}/{n}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                bool fresh = !m;
                if (fresh) m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = n };
                m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dir}/{n}_albedo.png"));
                m.SetColor("_BaseColor", Color.white);
                var normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dir}/{n}_normal.png");
                if (normal) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
                // Meshy's metallic map is noisy (fur came out speckled, partly metallic): skin, cloth and fur are not metal
                m.SetTexture("_MetallicGlossMap", null); m.DisableKeyword("_METALLICSPECGLOSSMAP");
                m.SetFloat("_Metallic", 0f); m.SetFloat("_Smoothness", .18f);
                if (fresh) AssetDatabase.CreateAsset(m, path); else EditorUtility.SetDirty(m);
            }
            AssetDatabase.SaveAssets();
        }

        // ---- rule 9, the Kira lock
        public static List<string> KiraLock()
        {
            var lines = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:KiraForm", new[] { "Assets/Resources/Data/KiraForms" }))
            {
                var f = AssetDatabase.LoadAssetAtPath<KiraForm>(AssetDatabase.GUIDToAssetPath(guid));
                var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir}/{f.model}.fbx");
                string tex = $"{Dir}/{f.model}_albedo.png";
                if (!model || !File.Exists(tex)) { lines.Add($"KIRA LOCK {f.id}: no model yet ({f.model}): graybox body (folded ears, grey, amber eyes by construction)"); continue; }
                var ears = model.GetComponentsInChildren<Transform>(true).Where(t => t.name.ToLowerInvariant().Contains("ear")).Select(t => t.name).ToList();
                var img = new Texture2D(2, 2); img.LoadImage(File.ReadAllBytes(tex));
                var px = img.GetPixels32(); int n = 0, grey = 0, white = 0, amber = 0;
                for (int i = 0; i < px.Length; i += 7)
                {
                    var c = px[i]; n++;
                    Color.RGBToHSV(c, out _, out float s, out float v);
                    if (s < .18f && v > .25f && v < .85f) grey++;
                    if (s < .12f && v > .9f) white++;
                    if (c.r > 178 && c.g > 140 && c.r - c.b > 38 && c.g - c.b > 25 && c.r - c.g < 51) amber++;
                }
                Object.DestroyImmediate(img);
                float pg = 100f * grey / n, pw = 100f * white / n, pa = 100f * amber / n;
                bool ok = ears.Count == 0 && pg > 35f && pw < 1.5f && pa > .05f;
                lines.Add($"KIRA LOCK {f.id} ({f.model}): {(ok ? "OK" : "FAILED")}: ear bones {ears.Count}{(ears.Count > 0 ? " (" + string.Join(", ", ears) + ")" : "")}; " +
                          $"grey fur {pg:0}% of the texture (need > 35%); white {pw:0.0}% (need < 1.5%); golden-amber eyes {pa:0.00}% (need > 0.05%)");
            }
            Directory.CreateDirectory("Assets/Resources/Data");
            File.WriteAllLines("Assets/Resources/Data/KiraLock.txt", lines);
            AssetDatabase.ImportAsset("Assets/Resources/Data/KiraLock.txt");
            foreach (var l in lines) if (l.Contains("FAILED")) Debug.LogError(l); else Debug.Log(l);
            return lines;
        }
    }
}
