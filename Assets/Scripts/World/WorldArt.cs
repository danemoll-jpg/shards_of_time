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
            // Round 8: the Medieval Kingdom (Tools/Blender/medieval_world.py): torch-lit sandstone, cobbles, slate roofs, timber, banners,
            // flowers, steel, the purple story shard
            ["CastleStone"] = (new Color(.80f, .66f, .48f), "Lit", .12f),
            ["Cobble"] = (new Color(.6f, .53f, .46f), "Lit", .25f),
            ["Flagstone"] = (new Color(.86f, .77f, .62f), "Lit", .15f),
            ["Slate"] = (new Color(.3f, .34f, .44f), "Lit", .35f),
            ["Timber"] = (new Color(.28f, .18f, .12f), "Lit", .2f),
            ["BannerRed"] = (new Color(.78f, .1f, .14f), "Lit", .2f),
            ["BannerBlue"] = (new Color(.15f, .28f, .72f), "Lit", .2f),
            ["BannerPurple"] = (new Color(.45f, .2f, .7f), "Lit", .2f),
            ["Canvas"] = (new Color(.95f, .9f, .78f), "Lit", .1f),
            ["FlowerPink"] = (new Color(.98f, .45f, .65f), "Lit", .2f),
            ["FlowerYellow"] = (new Color(1f, .82f, .2f), "Lit", .2f),
            ["FlowerViolet"] = (new Color(.55f, .35f, .9f), "Lit", .2f),
            ["Leaf"] = (new Color(.22f, .48f, .2f), "Lit", .15f),
            ["Steel"] = (new Color(.78f, .8f, .84f), "Lit", .8f),
            ["Bramble"] = (new Color(.26f, .24f, .14f), "Lit", .1f),
            ["PurpleShard"] = (new Color(.7f, .4f, 1f), "Glow", 0),
            // Round 10: the Pirate Seas (Tools/Blender/pirate_world.py): the red-brown ship, honey decks, pink sails, brass, sand, jungle, corals
            ["ShipRed"] = (new Color(.55f, .2f, .14f), "Lit", .35f),
            ["DeckWood"] = (new Color(.74f, .54f, .33f), "Lit", .25f),
            ["SailPink"] = (new Color(.98f, .72f, .8f), "Lit", .1f),
            ["Brass"] = (new Color(.86f, .66f, .3f), "Lit", .7f),
            ["SeaSand"] = (new Color(.93f, .84f, .64f), "Lit", .1f),
            ["Jungle"] = (new Color(.24f, .55f, .26f), "Lit", .15f),
            ["PalmTrunk"] = (new Color(.62f, .46f, .3f), "Lit", .15f),
            ["PalmLeaf"] = (new Color(.25f, .62f, .28f), "Lit", .2f),
            ["CoralPink"] = (new Color(1f, .45f, .6f), "Lit", .3f),
            ["CoralOrange"] = (new Color(1f, .55f, .25f), "Lit", .3f),
            ["CoralPurple"] = (new Color(.6f, .35f, .9f), "Lit", .3f),
            ["CoralYellow"] = (new Color(1f, .85f, .3f), "Lit", .3f),
            ["Seaweed"] = (new Color(.2f, .58f, .38f), "Lit", .2f),
            ["WreckWood"] = (new Color(.36f, .3f, .25f), "Lit", .15f),
            ["ChestWood"] = (new Color(.5f, .3f, .15f), "Lit", .3f),
            ["GreenShard"] = (new Color(.35f, 1f, .5f), "Glow", 0),
            ["Starfish"] = (new Color(1f, .5f, .35f), "Lit", .3f),
            ["Black"] = (new Color(.08f, .07f, .07f), "Lit", .2f),
            // Round 11: Victorian Steampunk (Tools/Blender/steampunk_world.py): red brick, cream stone, iron, copper, teal lamps
            ["Brick"] = (new Color(.62f, .25f, .18f), "Lit", .1f),
            ["BrickDark"] = (new Color(.45f, .18f, .14f), "Lit", .1f),
            ["CreamStone"] = (new Color(.93f, .86f, .7f), "Lit", .15f),
            ["FactoryFloor"] = (new Color(.36f, .34f, .36f), "Lit", .3f),
            ["IronGrate"] = (new Color(.3f, .31f, .34f), "Lit", .5f),
            ["IronMesh"] = (new Color(.28f, .29f, .32f), "Lit", .5f),
            ["Copper"] = (new Color(.78f, .45f, .28f), "Lit", .65f),
            ["BeltRubber"] = (new Color(.16f, .16f, .18f), "Lit", .15f),
            ["BlueRoller"] = (new Color(.2f, .45f, .85f), "Lit", .5f),
            ["NightGlass"] = (new Color(.25f, .35f, .7f), "Glow", 0),
            ["WarmGlass"] = (new Color(1f, .8f, .45f), "Glow", 0),
            ["TealGlow"] = (new Color(.2f, .95f, .85f), "Glow", 0),
            ["TealShard"] = (new Color(.2f, 1f, .9f), "Glow", 0),
            ["Teal"] = (new Color(.15f, .6f, .6f), "Lit", .4f),
            ["ClockFace"] = (new Color(.97f, .94f, .85f), "Lit", .3f),
            ["ClockGreen"] = (new Color(.12f, .32f, .25f), "Lit", .45f),
            ["Glass"] = (new Color(.55f, .7f, .8f), "Lit", .9f),
            ["Oil"] = (new Color(.05f, .05f, .07f), "Lit", .9f),
            ["Rubble"] = (new Color(.5f, .3f, .25f), "Lit", .1f),
            ["RedAlarm"] = (new Color(1f, .15f, .1f), "Glow", 0),
            // Round 12: Tomorrowland (built in code: World/Tomorrowland.cs): chrome, pink and teal neon, lavender street, brick shops, gold hall
            ["Chrome"] = (new Color(.83f, .86f, .92f), "Lit", .93f),
            ["ChromeDark"] = (new Color(.5f, .54f, .62f), "Lit", .9f),
            ["NeonPink"] = (new Color(1f, .3f, .78f), "Glow", 0),
            ["NeonTeal"] = (new Color(.2f, 1f, .92f), "Glow", 0),
            ["NeonYellow"] = (new Color(1f, .9f, .4f), "Glow", 0),
            ["NeonLilac"] = (new Color(.78f, .55f, 1f), "Glow", 0),
            ["TLStreet"] = (new Color(.27f, .22f, .34f), "Lit", .55f),
            ["TLWalk"] = (new Color(.86f, .82f, .93f), "Lit", .45f),
            ["TLPlaza"] = (new Color(.93f, .86f, .9f), "Lit", .4f),
            ["ShopBrick"] = (new Color(.74f, .38f, .34f), "Lit", .15f),
            ["ShopCream"] = (new Color(.97f, .92f, .84f), "Lit", .3f),
            ["TealWindow"] = (new Color(.28f, .75f, .8f), "Lit", .92f),
            ["HallGold"] = (new Color(.96f, .76f, .38f), "Lit", .8f),
            ["PinkShard"] = (new Color(1f, .35f, .82f), "Glow", 0),
            ["Walkway"] = (new Color(.55f, .6f, .72f), "Lit", .6f),
            // Round 13: the Moon (built in code: World/Moon.cs)
            ["MoonDust"] = (new Color(.6f, .6f, .62f), "Lit", .04f),
            ["MoonRock"] = (new Color(.42f, .42f, .45f), "Lit", .08f),
            ["BaseWhite"] = (new Color(.93f, .94f, .96f), "Lit", .5f),
            ["BaseFloor"] = (new Color(.55f, .58f, .64f), "Lit", .4f),
            ["GardenGreen"] = (new Color(.3f, .7f, .35f), "Lit", .2f),
            ["WhiteShard"] = (new Color(.95f, .97f, 1f), "Glow", 0),
            ["EarthBlue"] = (new Color(.3f, .55f, 1f), "Glow", 0),
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
