using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 14: the ending (Acts X and XI). Everyone in their end-of-film looks: Dan, Abi and Kira in their Act I clothes (Kira her own
    // body), Muscle Man in his Medieval clothes from when we first meet him. No film clip exists for these scenes: staging is Code's,
    // from Dan's descriptions (TODO.md Round 14) and the synopsis.
    //  Farewell (Act X, first half): the vortex. The restored shards open a portal to every era round the platform; Muscle Man spots the
    //    purple one (his home), hugs Dan (his air hug), still sure Dan is the greatest hero who ever lived ("My hero!"), and steps through.
    //  TheKing (Act X): the Medieval courtyard. Muscle Man falls out of the purple portal flat on the cobbles in front of the sword in the
    //    stone; gets up, takes the sword and pulls it out with no effort; the crowd roars "King! King! King!".
    //  HomeAgain (Act XI, after Act I played backwards: Game.HomeAgainScene): the living room. Dan and Abi wake on the couch with Kira:
    //    the ornament is whole on the tree... a dream? A violin outside: at the window, the Violinist in the snow, playing. It wasn't a
    //    dream. They turn round just as Kira leaps at the ornament: "No, Kira, no!", cut to black on her mid-leap.
    public static partial class CutsceneAuthoring
    {
        static Cast HomeCast => new Cast { dan = "dan_home", abi = "abi_home", kira = "kira", shard = new Color(.95f, .97f, 1f) };
        static readonly Color[] EraColors = { new Color(.25f, .55f, 1f), new Color(1f, .18f, .14f), new Color(1f, .78f, .2f), new Color(.62f, .32f, 1f), new Color(.2f, .95f, .45f), new Color(.15f, .9f, .85f), new Color(1f, .35f, .82f), new Color(.95f, .97f, 1f) };
        static readonly float[] EraYaws = { -64f, -48f, -32f, -16f, 0f, 16f, 32f, 48f };   // the vortex's paths (Ice Age ... the Moon)

        // ======================================================================================== Farewell (vortex, 20.5 s)
        // The root is the vortex platform's centre (Vortex.Centre), +z forward along the paths.
        public const float FarewellLength = 20.5f;
        public static void Farewell()
        {
            DataAssets.Ensure(); lastQ.Clear();
            var c = HomeCast; NecklaceModel.Tint = c.shard;
            var k = new Kit("Farewell", FarewellLength);
            const float L = FarewellLength;
            var dan = Cast0(k, "Dan", c.dan); var abi = Cast0(k, "Abi", c.abi); var kira = Cast0(k, "Kira", c.kira);
            var mm = Cast0(k, "Muscle Man", Have("muscleman_medieval", ""));
            Vector3 danAt = new Vector3(.6f, 0, -2.5f), abiAt = new Vector3(1.7f, 0, -2.8f), kiraAt = new Vector3(1.15f, 0, -1.9f), mmAt = new Vector3(-.9f, 0, -2.3f);
            // the eight portals open round the platform's rim, one after another (the restored shards' colours)
            GameObject purple = null; Vector3 purpleAt = Vector3.zero;
            for (int i = 0; i < 8; i++)
            {
                var dir = Quaternion.Euler(0, EraYaws[i], 0) * Vector3.forward; var at = dir * 7.2f + Vector3.up * 1.7f;
                var cols = new[] { EraColors[i], Color.Lerp(EraColors[i], Color.white, .5f), EraColors[i] * .8f + new Color(0, 0, 0, 1) };
                float t0 = .6f + i * .32f;
                var h = Halo(k, "Portal " + i, at, Quaternion.LookRotation(dir), cols, 1.35f, t0, t0 + .7f, i == 3 ? 17.6f : L, 70f);
                var disc = Ball(k, "Portal glow " + i, at + dir * .05f, Color.Lerp(EraColors[i], Color.white, .3f), .1f, "Glow");
                k.Key(disc, "Glow", "m_LocalScale", t0, new Vector3(.05f, .05f, .01f)); k.Key(disc, "Glow", "m_LocalScale", t0 + .7f, new Vector3(2.3f, 2.3f, .04f));
                if (i == 3) { k.Key(disc, "Glow", "m_LocalScale", 16.9f, new Vector3(2.3f, 2.3f, .04f)); k.Key(disc, "Glow", "m_LocalScale", 17.6f, new Vector3(.02f, .02f, .01f)); purple = h; purpleAt = at; }
                k.Show(disc, t0, i == 3 ? 17.6f : L);
                Lamp(k, "Portal light " + i, at - dir * .8f, EraColors[i], 6f, 1.6f, t0, i == 3 ? 17.6f : L);
            }
            k.Sound("vortex_portals", .5f, .8f);
            var purpleFloor = new Vector3(purpleAt.x, 0, purpleAt.z) * (6.4f / 7.2f);
            // Muscle Man: sees the portals, spots the purple one (points), turns to Dan, the air hug ("My hero!"), walks to it, waves, steps in
            float toDan = 90f, toPortal = Mathf.Atan2(purpleFloor.x - (-.2f), purpleFloor.z - (-2.3f)) * Mathf.Rad2Deg;
            var hugAt = danAt + new Vector3(-.75f, 0, .05f);
            Path(k, mm, (0f, mmAt, 0f), (4.4f, mmAt, 0f), (4.8f, mmAt, -12f), (7.2f, mmAt, -12f), (7.6f, mmAt + new Vector3(.1f, 0, 0), toDan), (8.3f, hugAt, toDan), (11.9f, hugAt, toDan),
                (12.4f, hugAt + new Vector3(-.2f, 0, .2f), toPortal), (15.0f, purpleFloor - (purpleFloor - hugAt).normalized * 1.0f, toPortal), (15.1f, purpleFloor - (purpleFloor - hugAt).normalized * 1.0f, 180f + toPortal),
                (16.2f, purpleFloor - (purpleFloor - hugAt).normalized * 1.0f, 180f + toPortal), (16.4f, purpleFloor - (purpleFloor - hugAt).normalized * .8f, toPortal), (17.2f, purpleFloor + Vector3.up * .3f, toPortal));
            k.Key(mm, "Body", "m_LocalScale", 16.6f, Vector3.one); k.Key(mm, "Body", "m_LocalScale", 17.2f, Vector3.one * .05f);
            k.Show(mm, 0f, 17.25f);
            AnimationClip MM(string n, string f) => PeopleClip(n) ?? PeopleClip(f);
            k.Clips(mm, (PeopleClip("idle"), 0f), (PeopleClip("surprised"), 2.2f), (MM("mm_taunt", "idle"), 4.6f), (PeopleClip("idle"), 7.0f), (PeopleClip("walk"), 7.6f), (MM("offer", "idle"), 8.3f),
                (MM("mm_cheer", "cheer"), 10.9f), (PeopleClip("walk"), 12.4f), (MM("mm_cheer", "cheer"), 15.1f), (PeopleClip("walk"), 16.2f));
            Burst(k, "Into the portal", purpleFloor + Vector3.up * 1.2f, EraColors[3], 17.1f, 1.4f, 18, 1.2f);
            // Dan, Abi, Kira watch; the hug (Dan surprised, then laughing); they wave him off
            Path(k, dan, (0f, danAt, 0f), (7.6f, danAt, 0f), (8.2f, danAt, -90f), (12.2f, danAt, -90f), (12.8f, danAt, -10f), (L, danAt, -10f));
            Path(k, abi, (0f, abiAt, 0f), (12.4f, abiAt, 0f), (13f, abiAt, -15f), (L, abiAt, -15f));
            Path(k, kira, (0f, kiraAt, 10f), (12.6f, kiraAt, 10f), (13.2f, kiraAt, -20f), (L, kiraAt, -20f));
            k.Clips(dan, (PeopleClip("idle"), 0f), (PeopleClip("surprised"), 1.4f), (PeopleClip("idle"), 3.4f), (PeopleClip("surprised"), 8.4f), (MM("laugh", "idle"), 10.2f), (PeopleClip("idle"), 12.4f), (PeopleClip("cheer"), 15.2f), (PeopleClip("idle"), 18.4f));
            k.Clips(abi, (PeopleClip("idle"), 0f), (PeopleClip("surprised"), 1.6f), (PeopleClip("idle"), 3.6f), (MM("laugh", "idle"), 9.4f), (PeopleClip("idle"), 11.6f), (PeopleClip("cheer"), 15.3f), (PeopleClip("idle"), 18.5f));
            k.Clips(kira, (KiraClip("idle"), 0f), (KiraClip("jump"), 15.4f), (KiraClip("idle"), 16.2f));
            k.Sound("mm_my_hero", 9.0f, .9f); k.Sound("mm_laugh", 11.0f, .5f); k.Sound("portal_whoosh", 16.9f, .6f);
            var lookAtPurple = k.Holder("Purple portal (look)", purpleAt, 0);
            Look(k, mm, lookAtPurple, 3.0f, 7.2f); Look(k, mm, dan, 7.6f, 11.6f); Look(k, dan, mm, 7.8f, 12.0f); Look(k, abi, mm, 8.0f, 16.8f); Look(k, dan, lookAtPurple, 12.6f, 18f);
            // shots
            k.Shot("The portals open", 0f, 4.5f, new Vector3(.2f, 3.0f, -9.5f), new Vector3(0, 1.9f, 4f), 60f, new Vector3(.2f, 2.6f, -8.2f));
            k.Shot("He spots the purple one", 4.5f, 7.4f, mmAt + new Vector3(.9f, 1.75f, 2.0f), mmAt + new Vector3(0, 1.8f, 0), 42f);
            k.Shot("The hug", 7.4f, 12.3f, new Vector3(-.2f, 1.55f, -.2f), hugAt + new Vector3(.35f, 1.35f, 0), 46f);
            k.Shot("To the purple portal", 12.3f, 16.8f, new Vector3(1.3f, 1.8f, -5.2f), purpleAt + Vector3.down * .4f, 46f, new Vector3(1.1f, 1.75f, -4.4f));
            k.Shot("Goodbye, Muscle Man", 16.8f, L, new Vector3(1.0f, 1.55f, 1.2f), new Vector3(1.1f, 1.25f, -2.6f), 48f);
            TintC(k, Color.white, (0f, 0f), (19.7f, 0f), (20.4f, 1f), (L, 1f));
            k.Save();
            NecklaceModel.Tint = IceAgeCast.shard;
        }

        // ======================================================================================== TheKing (the Medieval courtyard, 20 s)
        // The root is the stone (world (0, floor, 34), as TheStone), +z north: the crowd south and round the sides.
        public const float TheKingLength = 20f;
        public static void TheKing()
        {
            DataAssets.Ensure(); lastQ.Clear();
            var k = new Kit("TheKing", TheKingLength);
            const float L = TheKingLength;
            var mm = Cast0(k, "Muscle Man", Have("muscleman_medieval", ""));
            if (!k.Models.ContainsKey(mm)) Placeholders.Prim(PrimitiveType.Capsule, mm.transform, new Vector3(0, 1f, 0), new Vector3(1f, 1f, .7f), Placeholders.Mat(new Color(.2f, .25f, .4f)), name: "Body");
            // the sword in the stone, and the same sword in his right hand (shown from the moment he grips it)
            var stoneSword = k.Holder("Sword in the stone", new Vector3(0, 1.32f, 0), 0);
            Sword(stoneSword.transform);
            k.Show(stoneSword, 0f, 11.0f);
            Transform hand = null;
            if (k.Models.TryGetValue(mm, out var anim) && anim.isHuman) hand = anim.GetBoneTransform(HumanBodyBones.RightHand);
            if (hand)
            {
                var a = anim;
                Vector3 idx = a.GetBoneTransform(HumanBodyBones.RightIndexProximal).position, lit = a.GetBoneTransform(HumanBodyBones.RightLittleProximal).position, mid = a.GetBoneTransform(HumanBodyBones.RightMiddleProximal).position;
                var palm = Vector3.Lerp(hand.position, mid, .55f);
                var up = (lit - idx).normalized;                       // the grip runs through the fist: the pommel out by the little finger, the blade out by the thumb
                var fingers = (mid - hand.position).normalized;
                var held = new GameObject("Sword in hand"); held.transform.SetParent(hand, false);
                held.transform.position = palm - up * .07f;
                held.transform.rotation = Quaternion.LookRotation(Vector3.Cross(up, fingers).normalized, up);
                held.transform.localScale = new Vector3(1f / hand.lossyScale.x, 1f / hand.lossyScale.y, 1f / hand.lossyScale.z);
                Sword(held.transform);
                k.Show(held, 11.0f, L);
            }
            // the purple portal over the cobbles; he shoots out of it, lands on his feet and topples flat on his back
            var portal = Spiral(k, "Purple portal", new Vector3(0, 3.6f, -.9f), Quaternion.identity, new[] { new Color(.62f, .32f, 1f), new Color(.85f, .7f, 1f), new Color(.45f, .2f, .9f) }, 1.6f, 0f, .8f, 3.2f, 160f, new Color(.4f, .15f, .7f), 5, true, 2.2f);
            Lamp(k, "Portal light", new Vector3(0, 3.4f, -1.6f), new Color(.7f, .45f, 1f), 9f, 3f, 0f, 3.2f);
            _ = portal;
            var land = new Vector3(0, 0, -1.7f); var ready = new Vector3(0, 0, -.72f);
            Path(k, mm, (0f, new Vector3(0, 3.3f, -.95f), 180f), (1.0f, new Vector3(0, 3.3f, -.95f), 180f), (1.55f, land, 0f), (8.6f, land, 0f), (9.8f, ready, 0f), (L, ready, 0f));
            Appear(k, mm, 1.0f, L, L);
            AnimationClip MM(string n, string f) => PeopleClip(n) ?? PeopleClip(f);
            k.Clips(mm, (PeopleClip("fall"), 1.0f), (MM("mm_fall", "fall_back"), 1.5f), (MM("mm_getup", "idle"), 5.0f), (PeopleClip("idle"), 8.2f), (PeopleClip("walk"), 8.6f), (PeopleClip("idle"), 9.8f),
                (MM("offer", "idle"), 10.2f), (MM("mm_cheer", "cheer"), 11.0f), (MM("mm_flex", "cheer"), 15.2f), (MM("mm_cheer", "cheer"), 17.6f));
            Burst(k, "Sword sparkle", new Vector3(0, 1.4f, 0), new Color(1f, .92f, .6f), 11.0f, 1.1f, 14, 1.0f);
            Burst(k, "Landing dust", land + new Vector3(0, .1f, -.3f), new Color(.75f, .68f, .58f), 1.85f, 1.0f, 12, .9f, .3f, "Lit");
            // the crowd: knights, archers, the wagon driver and the bard round the courtyard
            string[] models = { "knight", "archer", "wagon_driver", "knight", "violinist_medieval", "archer", "knight", "wagon_driver", "archer", "knight", "archer", "knight" };
            var crowd = new List<GameObject>();
            for (int i = 0; i < models.Length; i++)
            {
                float a = Mathf.Lerp(-160f, -20f, i / (models.Length - 1f)) * Mathf.Deg2Rad; float r = 5.6f + (i % 3) * .9f;
                var at = new Vector3(Mathf.Cos(a) * r, 0, -1.0f + Mathf.Sin(a) * r);
                var who = Cast0(k, "Crowd " + i, Have(models[i], "dan_medieval"));
                float yaw = Mathf.Atan2(-at.x, -.9f - at.z) * Mathf.Rad2Deg;
                Path(k, who, (0f, at, yaw), (L, at, yaw));
                float d = (i % 4) * .15f;
                k.Clips(who, (PeopleClip("idle"), 0f), (PeopleClip("surprised"), 1.7f + d), (PeopleClip("idle"), 4.2f + d), (PeopleClip("surprised"), 11.2f + d), (PeopleClip("cheer"), 12.4f + d), (MM("mm_cheer", "cheer"), 14.6f + d), (PeopleClip("cheer"), 16.6f + d), (MM("mm_cheer", "cheer"), 18.4f + d));
                crowd.Add(who);
            }
            var swordMark = k.Holder("Sword (look)", new Vector3(0, 1.5f, 0), 0);
            Look(k, mm, swordMark, 7.4f, 10.4f);
            foreach (var who in crowd) Look(k, who, mm, 1.6f, L - .4f);
            k.Sound("portal_whoosh", .9f, .8f); k.Sound("crowd_gasp", 1.9f, .7f); k.Sound("mm_grunt", 5.4f, .6f);
            k.Sound("sword_draw", 11.0f, .9f); k.Sound("crowd_gasp", 11.3f, .8f); k.Sound("king_chant", 12.5f, .9f); k.Sound("king_chant", 16.2f, .9f); k.Sound("mm_laugh", 15.4f, .5f);
            Lamp(k, "Sun bounce", new Vector3(.5f, 3f, -3f), new Color(1f, .9f, .75f), 9f, 1.2f, 0f, L);
            // shots
            k.Shot("The purple portal", 0f, 1.7f, new Vector3(0, 2.4f, -9f), new Vector3(0, 2.4f, -.8f), 52f);
            k.Shot("Flat on the cobbles", 1.7f, 5.0f, new Vector3(2.4f, .55f, -3.4f), land + new Vector3(0, .3f, 0), 46f);
            k.Shot("He gets up and sees it", 5.0f, 9.0f, new Vector3(-1.5f, 1.8f, -4.4f), new Vector3(0, 1.35f, -.2f), 44f, new Vector3(-1.2f, 1.8f, -3.8f));
            k.Shot("He pulls it out", 9.0f, 11.8f, new Vector3(1.4f, 1.65f, 1.7f), new Vector3(0, 1.5f, -.7f), 42f);
            k.Shot("The crowd roars", 11.8f, 15.6f, new Vector3(.2f, 3.1f, 3.6f), new Vector3(0, 1.6f, -5f), 58f, new Vector3(.2f, 3.3f, 4.2f));
            k.Shot("King! King! King!", 15.6f, L, new Vector3(.45f, .8f, -3.5f), new Vector3(0, 2.3f, -.7f), 50f, new Vector3(.35f, .9f, -2.9f));
            TintC(k, Color.white, (0f, 0f), (19.3f, 0f), (19.9f, 1f), (L, 1f));
            k.Save();
        }

        // a knight's sword, its crossguard at the holder's origin, the blade down (-y), the grip up (+y)
        static void Sword(Transform t)
        {
            var steel = WorldArt.Mat("Silver"); var gold = WorldArt.Mat("Gold"); var grip = WorldArt.Mat("Wood");
            Placeholders.Prim(PrimitiveType.Cube, t, new Vector3(0, -.48f, 0), new Vector3(.07f, .9f, .016f), steel, name: "Blade");
            Placeholders.Prim(PrimitiveType.Cube, t, new Vector3(0, -.95f, 0), new Vector3(.035f, .06f, .014f), steel, name: "Tip");
            Placeholders.Prim(PrimitiveType.Cube, t, Vector3.zero, new Vector3(.32f, .045f, .05f), gold, name: "Crossguard");
            Placeholders.Prim(PrimitiveType.Cylinder, t, new Vector3(0, .12f, 0), new Vector3(.04f, .1f, .04f), grip, name: "Grip");
            Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, .24f, 0), Vector3.one * .065f, gold, name: "Pommel");
        }

        // ======================================================================================== HomeAgain (the living room, 23.5 s)
        // Plays after Act I has run backwards (Game.HomeAgainScene). The root is Game.HomeAnchor (the room), as ActOne. The window's night
        // backdrop goes: outside is a snowy garden where the Violinist stands (Game places the real Violinist there: his fiddle by IK).
        public const float HomeAgainLength = 23.5f;
        public static readonly Vector3 HomeViolinistAt = new Vector3(2f, 0, 8.4f);
        public static void HomeAgain()
        {
            DataAssets.Ensure(); lastQ.Clear();
            var k = new Kit("HomeAgain", HomeAgainLength);
            const float L = HomeAgainLength;
            var mood = k.root.AddComponent<CutsceneMood>(); _ = mood;
            var src = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/World/home_room.fbx");
            Vector3 orn = new Vector3(2.28f, 1.22f, 2.55f);   // the silver bauble's glass, whole on the tree again
            if (src)
            {
                var roomInst = (GameObject)PrefabUtility.InstantiatePrefab(src); PrefabUtility.UnpackPrefabInstance(roomInst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                roomInst.name = "Living room"; roomInst.transform.SetParent(k.root.transform, false);
                WorldArt.Dress(roomInst);
                // the window looks out on a real garden now (the backdrop, the snow box and its flakes go)
                var gone = new List<GameObject>();
                foreach (Transform t in roomInst.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("Night") || t.name.StartsWith("SnowOutside") || t.name.StartsWith("Flake")) gone.Add(t.gameObject);
                foreach (var g in gone) if (g) Object.DestroyImmediate(g);
                var heart = roomInst.transform.Find("HeartOrnament");
                if (heart)
                {
                    var tg = new GameObject("Initials"); tg.transform.SetParent(heart, false); tg.transform.localPosition = new Vector3(0, .01f, -.075f);
                    var tm = tg.AddComponent<TextMesh>(); tm.text = "D + A"; tm.font = UiKit.Font; tm.fontSize = 48; tm.characterSize = .006f; tm.anchor = TextAnchor.MiddleCenter; tm.color = new Color(1f, .9f, .75f);
                }
                var o = roomInst.transform.Find("Ornament"); if (o) orn = o.position + Vector3.down * .16f;
            }
            void RoomLamp(string n, Vector3 p, Color c, float range, float intensity)
            {
                var l = new GameObject(n); l.transform.SetParent(k.root.transform, false); l.transform.localPosition = p;
                var lt = l.AddComponent<Light>(); lt.type = LightType.Point; lt.color = c; lt.range = range; lt.intensity = intensity; lt.shadows = LightShadows.Soft;
            }
            RoomLamp("Tree light", new Vector3(2.4f, 1.5f, 2.4f), new Color(1f, .72f, .4f), 6f, 2.4f);
            RoomLamp("Room light", new Vector3(-.5f, 2.8f, 2.6f), new Color(1f, .82f, .62f), 10f, 1.9f);
            RoomLamp("Wreath light", new Vector3(-1.9f, 2.1f, 4.3f), new Color(1f, .78f, .45f), 4f, 1.2f);
            // the garden outside the window: snow, pines, a night sky, moonlight on the Violinist
            var garden = k.Holder("Garden", Vector3.zero, 0);
            var snow = WorldArt.Mat("Snow");
            Placeholders.Prim(PrimitiveType.Cube, garden.transform, new Vector3(2f, -.1f, 13f), new Vector3(24f, .2f, 15.6f), snow, name: "Snow ground");
            Placeholders.Prim(PrimitiveType.Cube, garden.transform, new Vector3(2f, 6f, 24f), new Vector3(60f, 20f, .2f), WorldArt.Mat("NightSky"), name: "Night sky");
            var rnd = new System.Random(5);
            for (int i = 0; i < 9; i++)
            {
                var p = new Vector3(-6f + i * 2.1f + (float)rnd.NextDouble(), 0, 13f + (float)rnd.NextDouble() * 7f);
                if (Mathf.Abs(p.x - 2f) < 2.2f && p.z < 15f) p.z += 4f;
                Placeholders.Prim(PrimitiveType.Cylinder, garden.transform, p + new Vector3(0, .5f, 0), new Vector3(.25f, .5f, .25f), WorldArt.Mat("Bark"), name: "Trunk");
                for (int j = 0; j < 3; j++) Placeholders.Prim(PrimitiveType.Sphere, garden.transform, p + new Vector3(0, 1.4f + j * .9f, 0), new Vector3(1.9f - j * .5f, 1.2f, 1.9f - j * .5f), WorldArt.Mat(j == 2 ? "Snow" : "Pine"), name: "Pine");
            }
            for (int i = 0; i < 60; i++) Placeholders.Prim(PrimitiveType.Sphere, garden.transform, new Vector3(-2f + (float)rnd.NextDouble() * 8f, .3f + (float)rnd.NextDouble() * 3.2f, 5.5f + (float)rnd.NextDouble() * 6f), Vector3.one * (.025f + (float)rnd.NextDouble() * .03f), snow, name: "Flake");
            Lamp(k, "Moonlight", HomeViolinistAt + new Vector3(-.5f, 3.2f, -1.2f), new Color(.7f, .8f, 1f), 9f, 2.2f, 0f, L);
            Lamp(k, "Window glow on him", HomeViolinistAt + new Vector3(0, 1.6f, -1.6f), new Color(1f, .8f, .55f), 4f, 1.4f, 0f, L);

            var dan = Cast0(k, "Dan", "dan_home"); var abi = Cast0(k, "Abi", "abi_home"); var kira = Cast0(k, "Kira", "kira");
            // asleep on the couch (it faces -z: its seat z 4.1..4.7, top .58); awake with a start; the ornament; a laugh; the violin;
            // to the window; the Violinist; smiles; they turn round as Kira leaps
            Vector3 danSit = new Vector3(-1.35f, .02f, 4.42f), abiSit = new Vector3(-2.55f, .02f, 4.42f), kiraSit = new Vector3(-1.95f, .58f, 4.4f);
            Vector3 danUp = new Vector3(-1.3f, 0, 3.55f), abiUp = new Vector3(-2.45f, 0, 3.55f);
            Vector3 danWin = new Vector3(1.55f, 0, 4.3f), abiWin = new Vector3(2.45f, 0, 4.3f);
            Path(k, dan, (0f, danSit, 180f), (3.0f, danSit, 180f), (3.5f, danUp, 180f), (5.4f, danUp, 140f), (9.8f, danUp, 140f), (10.3f, danUp + new Vector3(.4f, 0, .1f), 70f),
                (12.3f, danWin, 0f), (19.4f, danWin, 0f), (19.9f, danWin, 180f), (L, danWin, 180f));
            Path(k, abi, (0f, abiSit, 180f), (3.0f, abiSit, 180f), (3.5f, abiUp, 180f), (5.4f, abiUp, 140f), (9.9f, abiUp, 140f), (10.4f, abiUp + new Vector3(.5f, 0, .1f), 70f),
                (12.6f, abiWin, 0f), (19.5f, abiWin, 0f), (20.0f, abiWin, 180f), (L, abiWin, 180f));
            k.Clips(dan, (PeopleClip("sit"), 0f), (PeopleClip("surprised"), 3.0f), (PeopleClip("idle"), 5.0f), (PeopleClip("laugh"), 7.9f), (PeopleClip("idle"), 9.6f), (PeopleClip("walk"), 10.3f), (PeopleClip("idle"), 12.3f),
                (PeopleClip("surprised"), 13.0f), (PeopleClip("laugh"), 16.6f), (PeopleClip("idle"), 18.6f), (PeopleClip("surprised"), 20.1f));
            k.Clips(abi, (PeopleClip("sit"), 0f), (PeopleClip("surprised"), 3.05f), (PeopleClip("idle"), 5.0f), (PeopleClip("laugh"), 8.1f), (PeopleClip("idle"), 9.7f), (PeopleClip("walk"), 10.4f), (PeopleClip("idle"), 12.6f),
                (PeopleClip("surprised"), 13.1f), (PeopleClip("cheer"), 16.8f), (PeopleClip("idle"), 18.8f), (PeopleClip("surprised"), 20.2f));
            // Kira: curled on the couch between them, hops down, wanders to the tree behind them; crouches; the leap at the bauble
            var kiraFloor = new Vector3(-1.95f, 0, 3.75f); var kiraTree = new Vector3(2.62f, 0, 1.8f);
            var leapTop = orn + new Vector3(.02f, -.18f, -.32f);
            Path(k, kira, (0f, kiraSit, 180f), (3.2f, kiraSit, 180f), (3.7f, kiraFloor, 180f), (10.6f, kiraFloor, 120f), (13.2f, kiraTree, 60f), (19.6f, kiraTree, 20f),
                (20.9f, kiraTree, 15f), (21.55f, Vector3.Lerp(kiraTree, leapTop, .78f) + Vector3.up * .18f, 15f), (L, Vector3.Lerp(kiraTree, leapTop, .78f) + Vector3.up * .18f, 15f));
            k.Clips(kira, (KiraClip("idle"), 0f), (KiraClip("jump"), 3.2f), (KiraClip("land"), 3.65f), (KiraClip("idle"), 4.0f), (KiraClip("walk"), 10.6f), (KiraClip("idle"), 13.2f), (KiraClip("land"), 20.2f), (KiraClip("jump"), 20.9f), (KiraClip("attack"), 21.25f));
            var ornMark = k.Holder("Ornament (look)", orn, 0); var outside = k.Holder("Violinist (look)", HomeViolinistAt + new Vector3(0, 1.6f, 0), 0);
            Look(k, dan, ornMark, 5.4f, 7.8f); Look(k, abi, ornMark, 5.5f, 7.8f); Look(k, dan, abi, 8.0f, 9.6f); Look(k, abi, dan, 8.1f, 9.6f);
            Look(k, dan, outside, 12.4f, 16.3f); Look(k, abi, outside, 12.7f, 16.3f); Look(k, dan, abi, 16.5f, 18.6f); Look(k, abi, dan, 16.6f, 18.6f);
            Look(k, dan, kira, 20.3f, L); Look(k, abi, kira, 20.4f, L);
            k.Sound("violinist_tune", 9.6f, .85f); k.Sound("violinist_tune", 13.8f, .7f); k.Sound("no_kira", 20.85f, 1f);
            // shots
            k.Shot("Asleep on the couch", 0f, 3.0f, new Vector3(-1.95f, 1.15f, 2.1f), new Vector3(-1.95f, .85f, 4.4f), 46f, new Vector3(-1.95f, 1.1f, 2.4f));
            k.Shot("Awake", 3.0f, 5.4f, new Vector3(-1.85f, 1.45f, 1.5f), new Vector3(-1.9f, 1.35f, 3.6f), 46f);
            k.Shot("The ornament, whole", 5.4f, 7.9f, orn + new Vector3(-.15f, .08f, -.95f), orn, 30f, orn + new Vector3(-.12f, .06f, -.75f));
            k.Shot("A dream?", 7.9f, 10.2f, new Vector3(-1.4f, 1.5f, 1.85f), new Vector3(-1.85f, 1.4f, 3.6f), 44f);
            k.Shot("A violin", 10.2f, 12.6f, new Vector3(.2f, 1.55f, .9f), new Vector3(2.0f, 1.35f, 4.4f), 50f);
            k.Shot("The Violinist outside", 12.6f, 16.5f, new Vector3(2.0f, 1.75f, 2.9f), HomeViolinistAt + new Vector3(0, 1.4f, 0), 38f, new Vector3(2.0f, 1.7f, 3.4f));
            k.Shot("It wasn't a dream", 16.5f, 19.4f, new Vector3(2.0f, 1.55f, 6.8f), new Vector3(2.0f, 1.5f, 4.3f), 40f);
            k.Shot("They turn round", 19.4f, 20.9f, new Vector3(2.75f, 1.35f, 2.1f), new Vector3(2.0f, 1.45f, 4.3f), 46f);
            k.Shot("Kira leaps", 20.9f, L, new Vector3(1.25f, .95f, .85f), Vector3.Lerp(kiraTree, orn, .6f), 52f);
            TintC(k, Color.black, (0f, 1f), (1.2f, 0f), (21.45f, 0f), (21.5f, 1f), (L, 1f));
            k.Save();
        }
    }
}
