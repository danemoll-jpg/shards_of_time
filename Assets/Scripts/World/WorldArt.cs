using System.Collections.Generic;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 4: the Blender-made world art (Assets/Resources/World/*.fbx, from Tools/Blender/iceage_world.py and home_room.py). Each part
    // is named <part>__<Material>; Dress colours it from the palette below (the game's own URP materials, Placeholders.Mat), so the
    // FBX carries no materials. Spawn puts one prop (a top-level object of a file) in the world.
    public static class WorldArt
    {
        // name -> colour, kind ("Lit", "Glow", "Water"), smoothness
        static readonly Dictionary<string, (Color c, string kind, float smooth)> Palette = new Dictionary<string, (Color, string, float)>
        {
            // the Ice Age
            ["Snow"] = (new Color(.84f, .89f, .97f), "Lit", .3f),
            ["Basalt"] = (new Color(.27f, .22f, .21f), "Lit", .1f),
            ["Rock"] = (new Color(.52f, .39f, .3f), "Lit", .1f),
            ["Stone"] = (new Color(.58f, .55f, .53f), "Lit", .1f),
            ["Ice"] = (new Color(.6f, .85f, 1f), "Lit", .85f),
            ["Lava"] = (new Color(1f, .5f, .12f), "Glow", 0),
            ["Bone"] = (new Color(.93f, .88f, .76f), "Lit", .3f),
            ["Pine"] = (new Color(.17f, .4f, .32f), "Lit", .1f),
            ["Bark"] = (new Color(.42f, .28f, .18f), "Lit", .1f),
            // the living room (Act I)
            ["Wall"] = (new Color(.62f, .25f, .62f), "Lit", .1f),
            ["Floor"] = (new Color(.55f, .33f, .2f), "Lit", .35f),
            ["Ceiling"] = (new Color(.8f, .6f, .75f), "Lit", .1f),
            ["Cream"] = (new Color(.95f, .89f, .78f), "Lit", .15f),
            ["Red"] = (new Color(.82f, .1f, .14f), "Lit", .35f),
            ["Velvet"] = (new Color(.62f, .06f, .1f), "Lit", .2f),
            ["Lime"] = (new Color(.66f, .82f, .2f), "Lit", .2f),
            ["Green"] = (new Color(.1f, .42f, .18f), "Lit", .15f),
            ["Gold"] = (new Color(.95f, .72f, .25f), "Lit", .7f),
            ["Purple"] = (new Color(.55f, .2f, .75f), "Lit", .7f),
            ["Silver"] = (new Color(.75f, .77f, .82f), "Lit", .8f),
            ["DarkSilver"] = (new Color(.28f, .3f, .35f), "Lit", .92f),
            ["WarmLight"] = (new Color(1f, .82f, .45f), "Glow", 0),
            ["NightSky"] = (new Color(.5f, .62f, .85f), "Glow", 0),
            ["Frame"] = (new Color(.96f, .95f, .92f), "Lit", .3f),
            ["Picture"] = (new Color(.85f, .75f, .8f), "Lit", .2f),
            ["Wood"] = (new Color(.45f, .28f, .16f), "Lit", .3f),
            // Round 6: Egypt (Tools/Blender/egypt_world.py)
            ["Sand"] = (new Color(.93f, .78f, .52f), "Lit", .15f),
            ["Sandstone"] = (new Color(.87f, .7f, .48f), "Lit", .12f),
            ["PastelPink"] = (new Color(.98f, .68f, .72f), "Lit", .2f),
            ["PastelTeal"] = (new Color(.45f, .82f, .8f), "Lit", .2f),
            ["PastelLilac"] = (new Color(.75f, .65f, .9f), "Lit", .2f),
            ["PastelGold"] = (new Color(.98f, .82f, .45f), "Lit", .3f),
            ["PastelPeach"] = (new Color(.99f, .78f, .6f), "Lit", .2f),
            ["Turquoise"] = (new Color(.2f, .72f, .75f), "Lit", .5f),
            ["AwningRed"] = (new Color(.85f, .25f, .25f), "Lit", .15f),
            ["AwningWhite"] = (new Color(.97f, .95f, .9f), "Lit", .15f),
            ["AwningBlue"] = (new Color(.2f, .4f, .8f), "Lit", .15f),
            ["Wicker"] = (new Color(.72f, .55f, .32f), "Lit", .15f),
            ["Palm"] = (new Color(.25f, .55f, .25f), "Lit", .2f),
            ["Reed"] = (new Color(.55f, .6f, .3f), "Lit", .15f),
            ["Plaster"] = (new Color(.96f, .92f, .85f), "Lit", .15f),
            ["Dark"] = (new Color(.15f, .12f, .1f), "Lit", .2f),
            ["Terracotta"] = (new Color(.78f, .42f, .28f), "Lit", .2f),
            // Round 6: the Violinist's instruments
            ["Ebony"] = (new Color(.12f, .09f, .08f), "Lit", .6f),
            ["Gut"] = (new Color(.95f, .92f, .8f), "Lit", .4f),
            ["Hair"] = (new Color(.97f, .96f, .92f), "Lit", .2f),
            ["Lapis"] = (new Color(.12f, .25f, .7f), "Lit", .6f),
            // Round 7: Bethlehem (Tools/Blender/bethlehem_world.py): night grass, the barn's boards and thatch, hay, mud brick, presents
            ["Grass"] = (new Color(.42f, .45f, .22f), "Lit", .1f),
            ["Dirt"] = (new Color(.55f, .4f, .25f), "Lit", .1f),
            ["Plank"] = (new Color(.55f, .36f, .2f), "Lit", .2f),
            ["Thatch"] = (new Color(.78f, .6f, .3f), "Lit", .1f),
            ["Hay"] = (new Color(.93f, .78f, .38f), "Lit", .1f),
            ["Mudbrick"] = (new Color(.78f, .62f, .45f), "Lit", .1f),
            ["GiftRed"] = (new Color(.85f, .12f, .15f), "Lit", .45f),
            ["GiftGreen"] = (new Color(.12f, .6f, .3f), "Lit", .45f),
            ["GiftBlue"] = (new Color(.2f, .35f, .85f), "Lit", .45f),
            ["Olive"] = (new Color(.3f, .4f, .2f), "Lit", .15f),
            ["Rope"] = (new Color(.75f, .62f, .4f), "Lit", .1f),
            ["Iron"] = (new Color(.25f, .25f, .27f), "Lit", .6f),
            ["Ember"] = (new Color(1f, .45f, .12f), "Glow", 0),
            ["Water"] = (new Color(.2f, .35f, .5f, .75f), "Water", .8f),
        };

        public static Material Mat(string name)
        {
            if (!Palette.TryGetValue(name, out var p)) p = (Color.magenta, "Lit", .2f);
            var m = Placeholders.Mat(p.c, p.kind);
            if (p.kind == "Lit" && m.HasProperty("_Smoothness") && Application.isPlaying) m.SetFloat("_Smoothness", p.smooth);
            return m;
        }

        // colour every part of `root` by the word after "__" in its name; parts with no colour word keep theirs
        public static void Dress(GameObject root, bool shadows = true)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.gameObject.name; int k = n.LastIndexOf("__");
                if (k < 0) continue;
                string mat = n.Substring(k + 2); int dot = mat.IndexOf('.'); if (dot >= 0) mat = mat.Substring(0, dot);   // Blender's ".001"
                var m = Mat(mat);
                var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = m; r.sharedMaterials = ms;
                r.shadowCastingMode = shadows && Palette.TryGetValue(mat, out var p) && p.kind != "Glow" ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        public static GameObject File(string file) => Resources.Load<GameObject>("World/" + file);

        // the whole file (terrain, volcanoes, the room), dressed
        public static GameObject SpawnFile(string file, Transform parent)
        {
            var src = File(file); if (!src) { Debug.LogWarning("[WorldArt] missing World/" + file); return null; }
            var go = Object.Instantiate(src, parent, false); go.name = file;
            Dress(go);
            return go;
        }

        // one prop (a top-level object of the file) at a spot, turned and scaled
        static readonly Dictionary<string, Transform> propCache = new Dictionary<string, Transform>();
        public static GameObject Prop(string file, string prop, Transform parent, Vector3 pos, float yaw = 0, float scale = 1, bool collider = false)
        {
            string key = file + "/" + prop;
            if (!propCache.TryGetValue(key, out var src) || !src)
            {
                var f = File(file); src = f ? f.transform.Find(prop) : null; propCache[key] = src;
            }
            if (!src) { Debug.LogWarning($"[WorldArt] missing prop {key}"); return null; }
            var go = Object.Instantiate(src.gameObject, parent, false); go.name = prop;
            // keep the file's own turn (Blender's Z-up -> Unity's Y-up sits on the prop's root): Round 4 replaced it with the yaw and laid
            // every prop on its side (Round 5 item 2)
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0) * src.localRotation); go.transform.localScale = src.localScale * scale;
            Dress(go);
            if (collider) foreach (var mf in go.GetComponentsInChildren<MeshFilter>()) { var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; mc.convex = true; }
            return go;
        }
    }
}
