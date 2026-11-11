using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 2, items 7-9: the characters in Assets/Resources/Characters (copied there by Tools/import_characters.py).
    //  - Import settings: Kira's forms Generic (her own rig, clips as takes, loops: idle walk run fall swim); people (dan_*, abi_*)
    //    Humanoid with Unity's mesh LODs (made from the rigged mesh, so they keep the skin weights); the people's shared Mixamo
    //    clips in Characters/Clips Humanoid, trimmed / looped per PeopleClips (Round 3); the necklace a plain mesh. No materials
    //    from the FBX (URP materials are made below).
    //  - MakeMaterials: <name>.mat (URP Lit) from Meshy's textures <name>_albedo / _normal / _mask.png.
    //  - KiraLock: rule 9 checked on every Kira form model: no ear bones, solid grey fur with no white, golden-amber eyes.
    //    Written to Resources/Data/KiraLock.txt (the exe check logs it) and to BUILD_INFO.txt.
    public sealed class CharacterImport : AssetPostprocessor
    {
        const string Dir = "Assets/Resources/Characters";
        static readonly HashSet<string> Loops = new HashSet<string> { "idle", "walk", "run", "fall", "swim", "stunned" };

        static bool Ours(string path) => path.Replace('\\', '/').StartsWith(Dir + "/");
        static bool IsKira(string path) => IsGeneric(path);
        // Round 5: Kira's forms and the Ice Age creatures share Code's four-legged Blender rig (Generic, clips as takes)
        public static readonly string[] Creatures = { "wolf", "snow_critter", "mammoth", "camel", "guardian", "goat", "sheep", "guard_dog" };   // Round 6: Egypt's camel and temple guardian; Round 7: Bethlehem's animals
        static bool IsGeneric(string path) { string n = Path.GetFileNameWithoutExtension(path); return n.StartsWith("kira") || System.Array.IndexOf(Creatures, n) >= 0; }
        public static bool IsPerson(string name) => name.StartsWith("dan_") || name.StartsWith("abi_") || name.StartsWith("violinist_") || name == "mummy" || name.StartsWith("wiseman_") || name == "watchman"
            || name.StartsWith("muscleman_") || name == "knight" || name == "archer" || name == "wagon_driver";   // Round 8: the Medieval people (Mixamo)   // Round 7: the Wise Men and the watchman (Mixamo)   // Round 6: the Egypt Violinist and the mummy (Mixamo)
        static bool IsClip(string path) => path.Replace('\\', '/').StartsWith(Dir + "/Clips/");

        // The people's clips (Round 3; frames as Blender / Mixamo number them, 30 fps; 0 = the whole take). loop; the vertical
        // motion kept in the pose (false: the motor does the height: jump, fall); XZ kept in the pose (false: clips that travel
        // are held in place: swim, push, hurt). Measured on the clips in Blender (Docs/MODELS-NEEDED.md, Round 3).
        public sealed class ClipSpec { public bool loop, bakeY = true, bakeXZ = true; public int first, last; }
        public static readonly Dictionary<string, ClipSpec> PeopleClips = new Dictionary<string, ClipSpec>
        {
            ["idle"] = new ClipSpec { loop = true }, ["walk"] = new ClipSpec { loop = true }, ["run_slow"] = new ClipSpec { loop = true },
            ["run"] = new ClipSpec { loop = true }, ["look"] = new ClipSpec { loop = true },
            ["jump"] = new ClipSpec { first = 23, last = 37, bakeY = false },          // from take-off (the wind-up cut) to the top
            ["fall"] = new ClipSpec { loop = true, bakeY = false },
            ["land"] = new ClipSpec { first = 9, last = 33 },                          // from touch-down
            ["land_hard"] = new ClipSpec { first = 2, last = 61 },
            ["swim"] = new ClipSpec { loop = true, bakeXZ = false }, ["tread"] = new ClipSpec { loop = true },
            ["push"] = new ClipSpec { loop = true, first = 25, last = 199, bakeXZ = false },
            ["punch"] = new ClipSpec(), ["kick"] = new ClipSpec(), ["cheer"] = new ClipSpec(),
            ["hurt"] = new ClipSpec { first = 1, last = 20, bakeXZ = false },          // the first 0.6 s (the stumble back cut)
            ["fall_back"] = new ClipSpec(),                                            // Round 4: Dan's backward fall in the snow (cutscene)
            // Round 5: the Violinist's own clips (his Mixamo set)
            ["violinist_play"] = new ClipSpec { loop = true }, ["violinist_bow"] = new ClipSpec(), ["violinist_nod"] = new ClipSpec(),
            // Round 6: Dan's laugh and holding up the necklace (the Ice Age arrival); the mummy's own set
            ["laugh"] = new ClipSpec(), ["hold_up"] = new ClipSpec(),
            ["mummy_idle"] = new ClipSpec { loop = true }, ["mummy_walk"] = new ClipSpec { loop = true }, ["mummy_run_slow"] = new ClipSpec { loop = true },
            ["mummy_run"] = new ClipSpec { loop = true }, ["mummy_punch"] = new ClipSpec(), ["mummy_hurt"] = new ClipSpec(), ["mummy_cheer"] = new ClipSpec(),
            // Round 7: the Wise Men's and the watchman's (Dan's Mixamo downloads), everyone's surprise
            ["sit"] = new ClipSpec { loop = true }, ["argue"] = new ClipSpec { loop = true }, ["map"] = new ClipSpec { loop = true }, ["offer"] = new ClipSpec { loop = true },
            ["sad"] = new ClipSpec { loop = true }, ["doze"] = new ClipSpec { loop = true }, ["yell"] = new ClipSpec(), ["surprised"] = new ClipSpec(),
            // Round 8: Muscle Man's own (show off, mock, lift, push, heave at the stone, fall flat, cheer, boost, kneel), the knight's,
            // the archer's, the wagon driver's (Dan's Mixamo downloads; names in Tools/import_characters.py EXTRA_CLIPS)
            ["mm_flex"] = new ClipSpec(), ["mm_taunt"] = new ClipSpec(), ["mm_lift"] = new ClipSpec(), ["mm_push"] = new ClipSpec { loop = true, bakeXZ = false },
            ["mm_heave"] = new ClipSpec { loop = true, bakeXZ = false }, ["mm_fall"] = new ClipSpec(), ["mm_cheer"] = new ClipSpec(), ["mm_boost"] = new ClipSpec(),
            ["mm_kneel"] = new ClipSpec(),
            ["kn_idle"] = new ClipSpec { loop = true }, ["kn_walk"] = new ClipSpec { loop = true }, ["kn_attack"] = new ClipSpec(), ["kn_block"] = new ClipSpec { loop = true },
            ["ar_draw"] = new ClipSpec(), ["ar_aim"] = new ClipSpec { loop = true }, ["ar_shoot"] = new ClipSpec(), ["ar_nervous"] = new ClipSpec { loop = true },
            ["drv_dazed"] = new ClipSpec { loop = true }, ["drv_walk"] = new ClipSpec { loop = true },
        };

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
            string n = Path.GetFileNameWithoutExtension(assetPath);
            if (IsPerson(n) || IsClip(assetPath))
            {
                m.animationType = ModelImporterAnimationType.Human;
                m.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                m.importAnimation = IsClip(assetPath);
                m.optimizeGameObjects = false;              // the necklace needs Dan's Spine2 bone
                if (IsPerson(n)) m.generateMeshLods = true; // Unity's mesh LODs from the rigged mesh (skin weights kept)
                return;
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
            if (!Ours(assetPath)) return;
            var m = (ModelImporter)assetImporter;
            if (IsClip(assetPath))
            {
                string game = Path.GetFileNameWithoutExtension(assetPath);
                if (!PeopleClips.TryGetValue(game, out var spec)) spec = new ClipSpec();
                var takes = m.defaultClipAnimations;
                if (takes.Length == 0) return;
                var c = takes[0];
                float f0 = c.firstFrame;   // the take's first frame = Blender's frame 1
                c.name = game;
                if (spec.first > 0) c.firstFrame = f0 + spec.first - 1;
                if (spec.last > 0) c.lastFrame = Mathf.Min(c.lastFrame, f0 + spec.last - 1);
                c.loopTime = spec.loop; c.loopPose = spec.loop && game == "push";
                c.lockRootRotation = true; c.keepOriginalOrientation = true;
                c.lockRootHeightY = spec.bakeY; c.keepOriginalPositionY = true; c.heightFromFeet = false;
                c.lockRootPositionXZ = spec.bakeXZ; c.keepOriginalPositionXZ = true;
                m.clipAnimations = new[] { c };
                return;
            }
            if (!IsKira(assetPath)) return;
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
            // Round 6, item 3: the hard parts' plain material (tusks, fangs, claws: submesh 1), colour from <name>_hard.txt
            foreach (var txt in Directory.GetFiles(Dir, "*_hard.txt"))
            {
                string n = Path.GetFileName(txt).Replace("_hard.txt", ""), path = $"{Dir}/{n}_hard.mat";
                var v = File.ReadAllText(txt).Trim().Split(' ');
                var c = new Color(float.Parse(v[0], System.Globalization.CultureInfo.InvariantCulture), float.Parse(v[1], System.Globalization.CultureInfo.InvariantCulture), float.Parse(v[2], System.Globalization.CultureInfo.InvariantCulture));
                var m = AssetDatabase.LoadAssetAtPath<Material>(path); bool fresh = !m;
                if (fresh) m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = n + "_hard" };
                m.SetColor("_BaseColor", c); m.SetFloat("_Metallic", 0f); m.SetFloat("_Smoothness", .55f);
                if (fresh) AssetDatabase.CreateAsset(m, path); else EditorUtility.SetDirty(m);
            }
            foreach (var mat in Directory.GetFiles(Dir, "*_hard.mat"))
                if (!File.Exists(mat.Replace("_hard.mat", "_hard.txt"))) AssetDatabase.DeleteAsset(mat.Replace("\\", "/"));
            AssetDatabase.SaveAssets();
        }

        // ---- Round 3: the people's models and clips as imported (rig, size, LODs, clip lengths). Written to Resources/Data/Models.txt
        // (the exe check logs it) and to BUILD_INFO.txt.
        [MenuItem("Shards of Time/Report Character Imports")]
        public static List<string> PeopleReport()
        {
            var lines = new List<string>();
            foreach (var n in new[] { "dan_home", "dan_iceage", "abi_home", "abi_iceage" })
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir}/{n}.fbx");
                if (!go) { lines.Add($"MODEL {n}: missing (graybox)"); continue; }
                var avatar = AssetDatabase.LoadAllAssetsAtPath($"{Dir}/{n}.fbx").OfType<Avatar>().FirstOrDefault();
                var smr = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var mesh = smr.Length > 0 ? smr[0].sharedMesh : null;
                var b = mesh ? mesh.bounds : default;
                int bones = go.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("mixamorig"));
                bool ok = avatar && avatar.isValid && avatar.isHuman && mesh;
                lines.Add($"MODEL {n}: {(ok ? "OK" : "FAILED")}: humanoid {(avatar ? avatar.isHuman && avatar.isValid : false)}, {bones} bones, " +
                          $"{(mesh ? mesh.triangles.Length / 3 : 0)} triangles, mesh LODs {(mesh ? mesh.lodCount : 0)}, mesh height {b.size.y:0.00} (local units), " +
                          $"material {(File.Exists($"{Dir}/{n}.mat") ? "yes" : "NO")}");
            }
            foreach (var f in Directory.GetFiles(Dir + "/Clips", "*.fbx").OrderBy(x => x))
            {
                var clip = AssetDatabase.LoadAllAssetsAtPath(f.Replace('\\', '/')).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
                lines.Add(clip ? $"CLIP {clip.name}: {clip.length:0.00} s, {(clip.isLooping ? "loop" : "once")}, humanoid {clip.isHumanMotion}" : $"CLIP {Path.GetFileName(f)}: FAILED (no clip)");
            }
            Directory.CreateDirectory("Assets/Resources/Data");
            File.WriteAllLines("Assets/Resources/Data/Models.txt", lines);
            AssetDatabase.ImportAsset("Assets/Resources/Data/Models.txt");
            foreach (var l in lines) if (l.Contains("FAILED")) Debug.LogError(l); else Debug.Log(l);
            return lines;
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
                float needGrey = f.id == KiraForm.Mummy ? 20f : 35f;   // Round 6: mummy Kira's bandages cover most of her; grey wherever fur shows
                bool ok = ears.Count == 0 && pg > needGrey && pw < 1.5f && pa > .05f;
                lines.Add($"KIRA LOCK {f.id} ({f.model}): {(ok ? "OK" : "FAILED")}: ear bones {ears.Count}{(ears.Count > 0 ? " (" + string.Join(", ", ears) + ")" : "")}; " +
                          $"grey fur {pg:0}% of the texture (need > {needGrey:0}%); white {pw:0.0}% (need < 1.5%); golden-amber eyes {pa:0.00}% (need > 0.05%)");
            }
            Directory.CreateDirectory("Assets/Resources/Data");
            File.WriteAllLines("Assets/Resources/Data/KiraLock.txt", lines);
            AssetDatabase.ImportAsset("Assets/Resources/Data/KiraLock.txt");
            foreach (var l in lines) if (l.Contains("FAILED")) Debug.LogError(l); else Debug.Log(l);
            return lines;
        }
    }
}
