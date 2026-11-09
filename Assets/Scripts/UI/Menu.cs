using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ShardsOfTime
{
    // Round 1, item 13: the title screen and the pause menu (Hostile Horizon's MenuPanel idea, smaller). Pages of rows, moved with the
    // UI map (D-pad / stick / arrows, A / Enter, B / Esc). Title: Continue / New game / Settings / Controls / Credits / Quit.
    // Pause: Resume / Settings / Controls / Quit to title / Quit game. Settings: volume, screen, rumble, invert camera X / Y, camera
    // speed, Steam Deck. Controls shows the bindings of the device in use.
    public sealed class Menu : MonoBehaviour
    {
        public sealed class Row { public string label; public Func<string> value; public Action submit; public Action<int> change; public Func<bool> enabled; }
        public sealed class Page { public string title; public List<Row> rows = new List<Row>(); public Func<string> body; public Action back; public int sel; }

        public static Menu I { get; private set; }
        public Page Current { get; private set; }
        public bool Open => Current != null;
        Canvas canvas; GameObject panel, titleArt; Text title, body, footer; readonly List<Text> rowTexts = new List<Text>();
        float repeatAt; int lastDir;
        readonly Stack<Page> stack = new Stack<Page>();

        public static Menu Create()
        {
            var c = UiKit.ScreenCanvas("Menu", 20); var m = c.gameObject.AddComponent<Menu>(); I = m; m.canvas = c; m.Build(); return m;
        }

        void Build()
        {
            var root = canvas.transform;
            titleArt = UiKit.Rect(root, "Title art", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            var big = UiKit.Label(titleArt.transform, "Game title", 110, TextAnchor.MiddleCenter, new Color(1f, .92f, .6f), new Vector2(0, .7f), new Vector2(1, .95f), Vector2.zero, Vector2.zero);
            big.text = "SHARDS OF TIME"; big.fontStyle = FontStyle.Bold;
            var sub = UiKit.Label(titleArt.transform, "Subtitle", 30, TextAnchor.MiddleCenter, UiKit.Ink, new Vector2(0, .64f), new Vector2(1, .7f), Vector2.zero, Vector2.zero);
            sub.text = "Dan, Abi and Kira chase the shards of a broken ornament through time";
            var ver = UiKit.Label(titleArt.transform, "Version", 22, TextAnchor.LowerRight, UiKit.Dim, new Vector2(.5f, 0), new Vector2(1, 0), new Vector2(0, 16), new Vector2(-24, 50));
            ver.text = $"Round 1 graybox · version {Application.version}";
            panel = UiKit.Image(root, "Panel", UiKit.Panel, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-460, -330), new Vector2(460, 300)).gameObject;
            var p = panel.transform;
            title = UiKit.Label(p, "Title", 44, TextAnchor.UpperCenter, UiKit.Accent, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -80), new Vector2(-20, -18));
            for (int i = 0; i < 12; i++)
                rowTexts.Add(UiKit.Label(p, "Row " + i, 32, TextAnchor.MiddleLeft, UiKit.Ink, new Vector2(0, 1), new Vector2(1, 1), new Vector2(60, -130 - 44 * i), new Vector2(-40, -88 - 44 * i)));
            body = UiKit.Label(p, "Body", 26, TextAnchor.UpperLeft, UiKit.Ink, new Vector2(0, 0), new Vector2(1, 1), new Vector2(50, 70), new Vector2(-40, -96));
            footer = UiKit.Label(p, "Footer", 22, TextAnchor.LowerCenter, UiKit.Dim, new Vector2(0, 0), new Vector2(1, 0), new Vector2(20, 14), new Vector2(-20, 52));
            panel.SetActive(false); titleArt.SetActive(false);
        }

        public void Show(Page page, bool titleScreen)
        {
            stack.Clear(); Current = page; page.sel = FirstEnabled(page, 0); titleArt.SetActive(titleScreen); onTitle = titleScreen; Draw();
        }
        bool onTitle;
        public void Push(Page page) { if (Current != null) stack.Push(Current); Current = page; page.sel = FirstEnabled(page, 0); Draw(); }
        public void Back()
        {
            Sfx.Play("menu", .4f, .8f);
            if (Current != null && Current.title == "Settings") GameSettings.Save();
            if (stack.Count > 0) { Current = stack.Pop(); Draw(); return; }
            Current?.back?.Invoke();
        }
        public void Close() { stack.Clear(); Current = null; panel.SetActive(false); titleArt.SetActive(false); }

        static int FirstEnabled(Page p, int from)
        {
            for (int i = 0; i < p.rows.Count; i++) { int k = (from + i) % p.rows.Count; if (p.rows[k].enabled == null || p.rows[k].enabled()) return k; }
            return 0;
        }

        void Update()
        {
            if (Current == null) return;
            var nav = Controls.Navigate.ReadValue<Vector2>();
            int dir = nav.y > .5f ? -1 : nav.y < -.5f ? 1 : 0;
            int side = nav.x > .5f ? 1 : nav.x < -.5f ? -1 : 0;
            int code = dir != 0 ? dir * 2 : side;
            if (code == 0) lastDir = 0;
            else if (code != lastDir || Time.unscaledTime > repeatAt)
            {
                repeatAt = Time.unscaledTime + (code != lastDir ? .4f : .12f); lastDir = code;
                if (dir != 0 && Current.rows.Count > 0) Move(dir);
                else if (side != 0 && Current.rows.Count > 0) Current.rows[Current.sel].change?.Invoke(side);
                Sfx.Play("menu", .3f); Draw();
            }
            if (Controls.Submit.WasPressedThisFrame() && Current.rows.Count > 0)
            {
                var r = Current.rows[Current.sel];
                if (r.enabled == null || r.enabled()) { Sfx.Play("menu", .4f, 1.2f); if (r.submit != null) r.submit(); else r.change?.Invoke(1); }
                if (Current != null) Draw();
            }
            else if (Controls.Cancel.WasPressedThisFrame()) { Back(); if (Current != null) Draw(); }
            else if (Current.body != null) Draw();   // live pages (Controls follows the device in use)
        }

        void Move(int d)
        {
            var p = Current; int n = p.rows.Count;
            for (int i = 1; i <= n; i++) { int k = ((p.sel + d * i) % n + n) % n; if (p.rows[k].enabled == null || p.rows[k].enabled()) { p.sel = k; return; } }
        }

        public void Draw()
        {
            if (Current == null) return;
            panel.SetActive(true);
            var p = Current; title.text = p.title;
            bool hasBody = p.body != null;
            // the title screen's own list sits low and narrow, under the game's name; other pages use the big panel
            var prt = (RectTransform)panel.transform;
            bool small = onTitle && stack.Count == 0 && !hasBody;
            titleArt.SetActive(onTitle && stack.Count == 0);
            prt.offsetMin = small ? new Vector2(-280, -470) : new Vector2(-460, -330);
            prt.offsetMax = small ? new Vector2(280, -100) : new Vector2(460, 300);
            body.text = hasBody ? p.body() : ""; body.enabled = hasBody;
            body.rectTransform.offsetMin = new Vector2(50, 70 + 46 * p.rows.Count);   // the rows sit under the text, never over it
            for (int i = 0; i < rowTexts.Count; i++)
            {
                var t = rowTexts[i];
                // with a body, the rows sit at the bottom of the panel
                if (i >= p.rows.Count) { t.enabled = false; continue; }
                t.enabled = true;
                var r = p.rows[i]; bool on = i == p.sel; bool en = r.enabled == null || r.enabled();
                string v = r.value != null ? r.value() : null;
                t.text = (on ? "> " : "   ") + r.label + (v != null ? $"   <color=#ffd27a>{(r.change != null ? "< " + v + " >" : v)}</color>" : "");
                t.color = !en ? new Color(.5f, .52f, .58f) : on ? UiKit.Accent : UiKit.Ink;
                var rt = t.rectTransform;
                float y = hasBody ? -540 + 44 * (p.rows.Count - 1 - i) : (small ? -60 : -130) - 44 * i;
                rt.offsetMin = new Vector2(60, y); rt.offsetMax = new Vector2(-40, y + 42);
            }
            footer.text = $"{Controls.Btn(Controls.Act.Navigate)}: choose   {Controls.Btn(Controls.Act.Submit)}: select   {Controls.Btn(Controls.Act.Cancel)}: back";
        }

        // ---- the pages
        public static Page SettingsPage()
        {
            var s = GameSettings.S; var p = new Page { title = "Settings" };
            p.rows.Add(new Row { label = "Volume", value = () => s.volume + " %", change = d => { s.volume = Mathf.Clamp(s.volume + 10 * d, 0, 100); Sfx.ApplyVolume(); } });
            p.rows.Add(new Row { label = "Effects volume", value = () => s.effectsVolume + " %", change = d => { s.effectsVolume = Mathf.Clamp(s.effectsVolume + 10 * d, 0, 100); Sfx.ApplyVolume(); } });
            p.rows.Add(new Row { label = "Screen", value = () => ScreenSetting.ScreenLabel, change = d => ScreenSetting.Set(!ScreenSetting.Fullscreen) });
            p.rows.Add(new Row { label = "Resolution", value = () => ScreenSetting.ResolutionLabel, change = d => ScreenSetting.StepResolution(d) });
            p.rows.Add(new Row { label = "Steam Deck mode", value = () => SteamDeck.Label, change = d => { s.steamDeck = (s.steamDeck + d + 3) % 3; Look.ApplyQuality(); } });
            p.rows.Add(new Row { label = "Rumble", value = () => s.rumble ? "On" : "Off", change = d => s.rumble = !s.rumble });
            p.rows.Add(new Row { label = "Invert camera X", value = () => s.invertX ? "On" : "Off", change = d => s.invertX = !s.invertX });
            p.rows.Add(new Row { label = "Invert camera Y", value = () => s.invertY ? "On" : "Off", change = d => s.invertY = !s.invertY });
            p.rows.Add(new Row { label = "Camera speed", value = () => s.cameraSpeed + " %", change = d => s.cameraSpeed = Mathf.Clamp(s.cameraSpeed + 10 * d, 50, 200) });
            p.rows.Add(new Row { label = "Reset to defaults", submit = () => { GameSettings.ResetToDefaults(); Sfx.ApplyVolume(); Look.ApplyQuality(); I.Back(); I.Push(SettingsPage()); } });
            p.rows.Add(new Row { label = "Back", submit = () => I.Back() });
            return p;
        }

        public static Page ControlsPage()
        {
            var p = new Page { title = "Controls" };
            p.body = () =>
            {
                var d = Controls.LastDevice; string B(Controls.Act a) => Controls.Btn(a, d);
                return $"<color=#ffd27a>{(d == Controls.Device.Gamepad ? "Controller (Xbox layout / Steam Deck)" : "Keyboard and mouse")}</color>  (shows the device you used last)\n\n" +
                       $"Move  ·  {B(Controls.Act.Move)}\nCamera  ·  {B(Controls.Act.Look)}\nJump  ·  {B(Controls.Act.Jump)}  (hold for higher; again in the air: Abi's double jump, once learned)\n" +
                       $"Attack  ·  {B(Controls.Act.Attack)}  (Dan punches, Abi kicks, Kira claws)\nSwim down  ·  {B(Controls.Act.Dive)}     Swim up  ·  {B(Controls.Act.Jump)}\n" +
                       $"Switch hero  ·  {B(Controls.Act.SwitchPrev)} / {B(Controls.Act.SwitchNext)}{(d == Controls.Device.Keyboard ? "  or 1 / 2 / 3" : "")}\nRecenter camera  ·  {B(Controls.Act.Recenter)}\n" +
                       $"Pause  ·  {B(Controls.Act.Pause)}\nSkip a cutscene  ·  hold {B(Controls.Act.Skip)}";
            };
            p.rows.Add(new Row { label = "Back", submit = () => I.Back() });
            return p;
        }

        public static Page CreditsPage()
        {
            var p = new Page { title = "Credits" };
            p.body = () =>
                "<color=#ffd27a>Shards of Time</color>, based on the animated short film <i>The Shards of Time</i> by Dan.\n" +
                "Game design and art direction: Dan.   Code: Claude Code (Anthropic), from Dan's design.\n\n" +
                "<color=#ffd27a>Made with</color>  Unity 6000.6.1f1 (URP, Input System, Cinemachine, Timeline, Unity Recorder, ProBuilder);\n" +
                "Blender, Meshy, Mixamo, Suno and ElevenLabs (from the next rounds on).\n\n" +
                "<color=#ffd27a>Licences (launcher)</color>  miniz, MIT licence (Rich Geldreich, RAD Game Tools, Valve);\n" +
                "JSON for Modern C++, MIT licence (Niels Lohmann).\n" +
                "Unity packages under the Unity Companion License.";
            p.rows.Add(new Row { label = "Back", submit = () => I.Back() });
            return p;
        }
    }
}
