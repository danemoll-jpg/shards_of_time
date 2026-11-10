using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ShardsOfTime
{
    // Round 1: the HUD. Hearts (shared pool), shard total + the area's requirement, Dan's necklace (pulses; awake it points to the
    // story shard), the party strip (who you are, how to switch), breath (non-swimmers under water), messages, hurt flash, underwater
    // tint, cutscene letterbox + "hold to skip", and a black fade.
    public sealed class Hud : MonoBehaviour
    {
        static Hud I;
        Canvas canvas; RectTransform heartsRow; readonly List<Image> hearts = new List<Image>();
        Text shardText, areaText, messageText, skipText, partyHint, titleBig, titleSmall;
        Image tint; Image flash, water, fade, necklace, necklaceGem, necklaceArrow, breathBack, breathFill, skipFill;
        RectTransform necklaceRt, partyRow; readonly List<Text> partyNames = new List<Text>(); readonly List<Image> partyBacks = new List<Image>();
        GameObject playLayer, cutLayer; float messageUntil, flashT;
        public static string LastMessage { get; private set; } = "";
        public static float FadeAmount { get; private set; }

        public static Hud Create()
        {
            var c = UiKit.ScreenCanvas("HUD", 10); var h = c.gameObject.AddComponent<Hud>(); I = h; h.canvas = c; h.Build(); return h;
        }

        void Build()
        {
            var root = canvas.transform;
            water = UiKit.Image(root, "Underwater", new Color(.1f, .4f, .65f, .28f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); water.enabled = false;
            flash = UiKit.Image(root, "Hurt flash", new Color(1f, .1f, .1f, 0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            playLayer = UiKit.Rect(root, "Play HUD", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            var p = playLayer.transform;
            heartsRow = UiKit.Box(p, "Hearts", new Vector2(0, 1), new Vector2(40, -36), new Vector2(700, 60));
            // shards (top right)
            var sIcon = UiKit.Box(p, "Shard icon", new Vector2(1, 1), new Vector2(-210, -34), new Vector2(40, 56)).gameObject.AddComponent<Image>(); sIcon.sprite = UiKit.Diamond; sIcon.color = new Color(.45f, .9f, 1f);
            shardText = UiKit.Label(p, "Shards", 46, TextAnchor.MiddleLeft, UiKit.Ink, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-160, -96), new Vector2(-30, -24));
            areaText = UiKit.Label(p, "Area", 26, TextAnchor.MiddleRight, UiKit.Dim, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-640, -140), new Vector2(-34, -100));
            // necklace (bottom left)
            necklaceRt = UiKit.Box(p, "Necklace", new Vector2(0, 0), new Vector2(60, 50), new Vector2(110, 110));
            var ring = necklaceRt.gameObject.AddComponent<Image>(); ring.sprite = UiKit.Ring; ring.color = new Color(.85f, .75f, .45f);
            necklace = UiKit.Image(necklaceRt, "Glow", new Color(.35f, .85f, 1f, .0f), new Vector2(-.2f, -.2f), new Vector2(1.2f, 1.2f), Vector2.zero, Vector2.zero, UiKit.Circle);
            necklaceGem = UiKit.Image(necklaceRt, "Gem", new Color(.35f, .85f, 1f), new Vector2(.32f, .22f), new Vector2(.68f, .78f), Vector2.zero, Vector2.zero, UiKit.Diamond);
            necklaceArrow = UiKit.Image(necklaceRt, "Arrow", new Color(1f, .95f, .6f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-18, 60), new Vector2(18, 100), UiKit.Arrow);
            necklaceArrow.rectTransform.pivot = new Vector2(.5f, -1.5f); necklaceArrow.rectTransform.anchoredPosition = Vector2.zero; necklaceArrow.rectTransform.sizeDelta = new Vector2(36, 40);
            // party strip (bottom right)
            partyRow = UiKit.Box(p, "Party", new Vector2(1, 0), new Vector2(-40, 40), new Vector2(480, 70));
            string[] names = { "Dan", "Abi", "Kira" };
            for (int i = 0; i < 3; i++)
            {
                var back = UiKit.Image(partyRow, names[i], UiKit.Panel, new Vector2(i / 3f, 0), new Vector2((i + 1) / 3f, 1), new Vector2(4, 0), new Vector2(-4, 0));
                var t = UiKit.Label(back.transform, "Name", 30, TextAnchor.MiddleCenter, UiKit.Ink, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); t.text = names[i];
                partyBacks.Add(back); partyNames.Add(t);
            }
            partyHint = UiKit.Label(p, "Switch hint", 22, TextAnchor.MiddleRight, UiKit.Dim, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-560, 112), new Vector2(-44, 146));
            // breath (centre)
            breathBack = UiKit.Image(p, "Breath", UiKit.Panel, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-160, 210), new Vector2(160, 236));
            breathFill = UiKit.Image(breathBack.transform, "Fill", new Color(.6f, .9f, 1f), Vector2.zero, Vector2.one, new Vector2(3, 3), new Vector2(-3, -3));
            var bl = UiKit.Label(breathBack.transform, "Label", 20, TextAnchor.MiddleCenter, UiKit.Ink, Vector2.zero, Vector2.one, new Vector2(0, 26), new Vector2(0, 50)); bl.text = "AIR";
            messageText = UiKit.Label(root, "Message", 36, TextAnchor.MiddleCenter, UiKit.Ink, new Vector2(.15f, 0), new Vector2(.85f, 0), new Vector2(0, 128), new Vector2(0, 196));   // Round 5: under the hero, over the buttons
            // cutscene layer
            cutLayer = UiKit.Rect(root, "Cutscene", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            UiKit.Image(cutLayer.transform, "Bar top", Color.black, new Vector2(0, .89f), Vector2.one, Vector2.zero, Vector2.zero);
            UiKit.Image(cutLayer.transform, "Bar bottom", Color.black, Vector2.zero, new Vector2(1, .11f), Vector2.zero, Vector2.zero);
            skipText = UiKit.Label(cutLayer.transform, "Skip", 26, TextAnchor.MiddleRight, UiKit.Dim, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-640, 30), new Vector2(-150, 90));
            var skipBack = UiKit.Image(cutLayer.transform, "Skip back", new Color(1, 1, 1, .15f), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-130, 52), new Vector2(-40, 66));
            skipFill = UiKit.Image(skipBack.transform, "Fill", UiKit.Accent, Vector2.zero, new Vector2(0, 1), Vector2.zero, Vector2.zero);
            cutLayer.SetActive(false);
            fade = UiKit.Image(root, "Fade", new Color(0, 0, 0, 0), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            tint = UiKit.Image(root, "Cutscene tint", new Color(1, 1, 1, 0), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); tint.raycastTarget = false;
            // Round 4: a title card over the fade ("Ancient Egypt / To be continued...")
            titleBig = UiKit.Label(root, "Title card", 96, TextAnchor.MiddleCenter, new Color(1f, .9f, .6f), new Vector2(0, .5f), new Vector2(1, .7f), Vector2.zero, Vector2.zero);
            titleSmall = UiKit.Label(root, "Title card small", 44, TextAnchor.MiddleCenter, UiKit.Ink, new Vector2(0, .36f), new Vector2(1, .5f), Vector2.zero, Vector2.zero);
            titleBig.enabled = titleSmall.enabled = false;
        }

        void Update()
        {
            bool play = Game.I && Game.I.State == Game.Mode.Playing || Game.I && Game.I.State == Game.Mode.Paused;
            playLayer.SetActive(play && !Game.InCutscene);
            cutLayer.SetActive(Game.InCutscene);
            if (Game.InCutscene) { skipText.text = $"Hold {Controls.Btn(Controls.Act.Skip)} to skip"; skipFill.rectTransform.anchorMax = new Vector2(Cutscenes.SkipHeld01, 1); }
            messageText.enabled = Time.unscaledTime < messageUntil && !Game.InCutscene && Game.Playing;
            flashT = Mathf.MoveTowards(flashT, 0, Time.unscaledDeltaTime * 2.5f);
            flash.color = new Color(1f, .1f, .1f, flashT * .35f);
            fade.color = new Color(0, 0, 0, FadeAmount);
            if (!play || !Party.I) return;

            // hearts
            while (hearts.Count < Health.Max) { var h = UiKit.Box(heartsRow, "Heart", new Vector2(0, 1), new Vector2(hearts.Count * 62, 0), new Vector2(54, 50)).gameObject.AddComponent<Image>(); h.sprite = UiKit.Heart; hearts.Add(h); }
            for (int i = 0; i < hearts.Count; i++)
            {
                hearts[i].gameObject.SetActive(i < Health.Max);
                bool full = i < Health.Hearts;
                hearts[i].color = full ? new Color(1f, .25f, .35f) : new Color(.15f, .15f, .2f, .6f);
                float blink = Health.Invincible && full ? (Mathf.Sin(Time.time * 30f) > 0 ? 1f : .6f) : 1f;
                hearts[i].transform.localScale = Vector3.one * blink;
            }
            // shards
            var s = SaveGame.Current; var g = Game.I;
            shardText.text = s.Total.ToString();
            var area = g.Area; var wp = s.World(Game.World.worldId);
            if (area != null && area.requirement > 0)
            {
                int have = g.AreaShards(area.id);
                areaText.text = wp.storyShard ? $"{area.name}: story shard found, the {(Game.TestArea ? "portal" : "vortex")} is open" : wp.necklaceAwake ? $"{area.name}: the necklace is awake - follow it to the story shard" : $"{area.name}: {have} / {area.requirement} shards to wake the necklace";
            }
            else areaText.text = area != null ? $"{area.name}: {g.AreaShards(area.id)} / {Game.World.InArea(area.id).Count()} shards" : "";
            // necklace
            float pulse = Necklace.Pulse;
            necklace.color = new Color(.35f, .85f, 1f, pulse * .55f);
            necklaceGem.transform.localScale = Vector3.one * (1f + pulse * .35f);
            necklaceGem.color = Color.Lerp(new Color(.25f, .5f, .7f), new Color(.6f, .95f, 1f), Necklace.Target ? .4f + pulse * .6f : 0f);
            necklaceArrow.enabled = Necklace.Awake && Necklace.Target;
            if (necklaceArrow.enabled) necklaceArrow.rectTransform.localRotation = Quaternion.Euler(0, 0, Necklace.ScreenAngle());
            // the gem on Dan pulses too
            foreach (var hero in Party.I.Heroes)
                if (hero.NecklaceGem && hero.NecklaceGem.name == "Necklace") hero.NecklaceGem.localScale = new Vector3(.09f, .13f, .05f) * (1f + pulse * .5f);   // the Round 1 gem
                else NecklaceModel.Pulse(hero.NecklaceGem, pulse, Necklace.Target);
            // party
            for (int i = 0; i < 3; i++)
            {
                bool on = Party.I.Index == i;
                partyBacks[i].color = on ? new Color(Party.I.Heroes[i].Stats.color.r, Party.I.Heroes[i].Stats.color.g, Party.I.Heroes[i].Stats.color.b, .95f) : UiKit.Panel;
                partyNames[i].fontSize = on ? 34 : 26;
            }
            partyHint.text = Controls.LastDevice == Controls.Device.Gamepad ? "Switch: LB / RB" : "Switch: Q / E or 1 2 3";
            // breath
            var m = Party.I.Player.Motor;
            bool showBreath = !m.Stats.swimmer && m.Breath01 < .999f;
            breathBack.gameObject.SetActive(showBreath);
            if (showBreath) { breathFill.rectTransform.anchorMax = new Vector2(m.Breath01, 1); breathFill.color = m.Breath01 < .3f ? new Color(1f, .4f, .3f) : new Color(.6f, .9f, 1f); }
        }

        public static void Message(string s, float seconds = 3.5f) { if (!I) return; I.messageText.text = s; I.messageUntil = Time.unscaledTime + seconds; LastMessage = s; PlayLog.Line("message: " + s); }
        public static void Flash() { if (I) I.flashT = 1f; }
        public static void Tint(Color c, float amount) { if (I) I.tint.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(amount)); }
        public static string TitleShown { get; private set; }
        public static void Title(string big, string small)
        {
            if (!I) return; TitleShown = big;
            I.titleBig.enabled = I.titleSmall.enabled = big != null;
            I.titleBig.text = big ?? ""; I.titleSmall.text = small ?? "";
        }
        public static void SetUnderwater(bool on) { if (I) I.water.enabled = on && Game.I && Game.I.State != Game.Mode.Title; }
        public static void SetFade(float a) => FadeAmount = Mathf.Clamp01(a);
        public static Canvas Canvas => I ? I.canvas : null;
    }
}
