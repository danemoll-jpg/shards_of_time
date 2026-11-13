using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ShardsOfTime
{
    // Round 14: the ending (Acts X and XI) and the credits. Through the Home path once all eight story shards are rebuilt:
    //  Farewell (the vortex) -> TheKing (the Medieval courtyard: the world is built for it) -> HomeAgain = Act I played backwards (picture
    //  and Dan's act1.mp3 reversed: Music/act1_reversed.mp3) from the warp tunnel back to just before Kira knocks the ornament off,
    //  then the waking scene (the real Violinist in the snow outside the window) -> the credits (skippable) -> free roam: back in the
    //  vortex of the world the party left, every path open, Muscle Man in the party again; the save remembers the ending is seen.
    public sealed partial class Game
    {
        public const float ActOneBackFrom = 49f, ActOneBackTo = 9.8f;            // ActOne's warp tunnel ends at 49 s; Kira leaps at 9.85 s
        public static readonly Vector3 HomeViolinistAt = new Vector3(1.5f, 0, 8.4f);   // outside the living-room window (CutsceneAuthoring.HomeAgain)
        public int EndingsPlayed { get; private set; }
        public string EndingStep { get; private set; } = "";
        public bool CreditsRolling { get; private set; }
        public int CreditsShown { get; private set; }
        public bool CreditsSkipped { get; private set; }
        public float CreditsSeconds { get; private set; }
        public string CreditsStill { get; private set; } = "";
        public static string CreditsText { get; private set; } = "";
        public Violinist HomeViolinist { get; private set; }
        public static bool HomeAgainNoBackwards;
        public const bool EndingReal = true;    // Round 14: the Home path plays the ending (false: "to be continued", the 0.13.2 build)
        static string creditsTrack;   // Dan's credits song when he has made one (Docs/MUSIC.md), else his title song
        public static string CreditsTrack => creditsTrack ??= Resources.Load<AudioClip>("Music/credits") ? "credits" : "title";   // the T-pose sweep samples the waking scene alone

        IEnumerator Ending()
        {
            Busy = true; Party.I.Frozen = true; EndingsPlayed++;
            PlayLog.Line("the ending: the Home path with all eight story shards");
            SaveGame.Save("the ending");
            yield return EndingFilm(false);
            // free roam: the vortex of the world they left, Muscle Man with them again
            if (!SaveGame.Current.endingSeen) { SaveGame.Current.endingSeen = true; }
            if (!SaveGame.Current.Has(Abilities.MuscleMan)) SaveGame.Current.abilities.Add(Abilities.MuscleMan);
            Hud.SetFade(1f);
            World = WorldData.Load(SavedWorld); BuildWorld();
            SaveGame.Current.checkpoint = Vortex.Checkpoint; Checkpoint.RespawnParty(); CameraRig.I.SnapBehind();
            if (!Party.I.Companion) Party.I.AddCompanion(Party.I.Player.transform.position + Vector3.back * 3f, Quaternion.identity);
            SaveGame.Save("the ending seen: free roam");
            PlayLog.Line($"free roam: the vortex, Muscle Man {(Party.I.Companion ? "back in the party" : "MISSING")}");
            yield return null;
            for (float t = 1; t > 0; t -= Time.deltaTime / .8f) { Hud.SetFade(t); yield return null; }
            Hud.SetFade(0f);
            Hud.Message("Muscle Man is back! Every path in the vortex is open: find the shards and secrets still hidden in all eight worlds.", 8f);
            Party.I.Frozen = false; Busy = false;
        }

        // the four parts in a row (the game's ending, and the Theater's / film render's "Ending")
        IEnumerator EndingFilm(bool theater)
        {
            EndingStep = "Farewell";
            yield return FarewellScene();
            EndingStep = "TheKing";
            yield return KingScene();
            EndingStep = "HomeAgain";
            yield return HomeAgainScene();
            EndingStep = "Credits";
            yield return Credits();
            EndingStep = "";
        }

        // the film render / the Theater: one part (or "Ending": all four) without touching the save
        public bool FilmBusy { get; private set; }
        IEnumerator FilmEnding(string name, bool theater)
        {
            FilmBusy = true;
            SaveGame.Disabled = true; Cutscenes.AllowSkip = theater; Busy = true;
            Menu.I.Close(); titleCam.Priority = -10; State = Mode.Playing;
            Party.I.Frozen = true;
            if (!theater) yield return new WaitForSeconds(.5f); else yield return null;
            if (name == "Ending") yield return EndingFilm(theater);
            else if (name == "Farewell") yield return FarewellScene();
            else if (name == "TheKing") yield return KingScene();
            else if (name == "HomeAgain") yield return HomeAgainScene();
            else yield return Credits();
            FilmBusy = false;
        }

        IEnumerator FarewellScene()
        {
            yield return UnderBlack(Cutscenes.Play("Farewell", Vortex.Centre, Quaternion.identity));
        }

        // a scene starts under black: the screen clears once its own camera has taken over (no frame of play between the scenes)
        IEnumerator UnderBlack(IEnumerator play)
        {
            Hud.SetFade(1f);
            var co = StartCoroutine(play);
            yield return null; yield return null;
            Hud.SetFade(0f);
            yield return co;
            Hud.SetFade(1f);
        }

        // the Medieval courtyard: the world is built for it (its enemies and the rival out of the way)
        IEnumerator KingScene()
        {
            Hud.SetFade(1f);
            if (World.worldId != WorldData.Medieval) { World = WorldData.Load(WorldData.Medieval); BuildWorld(); }
            foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)) e.gameObject.SetActive(false);
            foreach (var r in Object.FindObjectsByType<RivalMuscleMan>(FindObjectsSortMode.None)) r.gameObject.SetActive(false);
            var at = new Vector3(Medieval.StoneAt.x, Medieval.Ground(0, 31f, 3f), Medieval.StoneAt.z);
            Party.I.PlaceAll(at + new Vector3(0, .1f, -6f), Quaternion.identity); Party.I.Frozen = true;
            yield return null;
            yield return UnderBlack(Cutscenes.Play("TheKing", at, Quaternion.identity));
        }

        // Act I backwards (music too), then the waking scene with the Violinist outside the window
        public IEnumerator HomeAgainScene()
        {
            if (!HomeAgainNoBackwards) yield return UnderBlack(Cutscenes.Play("ActOne", HomeAnchor, Quaternion.identity, ActOneBackFrom, ActOneBackTo, "HomeAgain"));
            HomeViolinist = Violinist.Make(worldRoot, HomeAnchor + HomeViolinistAt, 180f, "Fiddle", "violinist_play");
            HomeViolinist.serenade = true;
            yield return UnderBlack(Cutscenes.Play("HomeAgain", HomeAnchor, Quaternion.identity));
            if (HomeViolinist) Destroy(HomeViolinist.gameObject);
            Hud.SetFade(1f);   // (it ends on black)
        }

        // ---- the credits: a gentle scroll over stills from each world (Resources/Credits), Dan's title song; hold Skip to skip
        public const float CreditsLength = 62f;
        public IEnumerator Credits()
        {
            CreditsRolling = true; CreditsShown++;
            if (!SaveGame.Disabled && !SaveGame.Current.seen.Contains("Credits")) SaveGame.Current.seen.Add("Credits"); CreditsSkipped = false; float start = Time.time;
            var ta = Resources.Load<TextAsset>("Data/Credits");
            CreditsText = ta ? ta.text : "Shards of Time";
            var canvas = UiKit.ScreenCanvas("Credits", 900);
            UiKit.Image(canvas.transform, "Black", Color.black, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var stills = Resources.LoadAll<Texture2D>("Credits").OrderBy(t => t.name).ToArray();
            var picA = MakeStill(canvas.transform, "Still A"); var picB = MakeStill(canvas.transform, "Still B");
            UiKit.Image(canvas.transform, "Shade", new Color(0, 0, 0, .55f), new Vector2(.5f, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            var col = UiKit.Rect(canvas.transform, "Column", new Vector2(.52f, 0), new Vector2(.98f, 1), Vector2.zero, Vector2.zero);
            var mask = col.gameObject.AddComponent<RectMask2D>(); _ = mask;
            var text = UiKit.Label(col, "Roll", 40, TextAnchor.UpperCenter, UiKit.Ink, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -6000), new Vector2(0, 0));
            text.text = Format(CreditsText); text.lineSpacing = 1.1f;
            var rt = text.rectTransform; rt.pivot = new Vector2(.5f, 1);
            var skip = UiKit.Label(canvas.transform, "Skip hint", 26, TextAnchor.LowerRight, new Color(1, 1, 1, .5f), new Vector2(.5f, 0), new Vector2(.98f, .06f), Vector2.zero, Vector2.zero);
            Canvas.ForceUpdateCanvases();
            float height = Mathf.Max(800f, text.preferredHeight), screenH = ((RectTransform)canvas.transform).rect.height;
            float speed = (height + screenH) / (CreditsLength - 4f);
            int shown = -1; float per = stills.Length > 0 ? (CreditsLength - 2f) / stills.Length : 1f;
            float held = 0; bool armed = !Controls.Skip.IsPressed();
            PlayLog.Line($"the credits roll ({stills.Length} stills, {height:0} px of text)");
            while (Time.time - start < CreditsLength)
            {
                float t = Time.time - start;
                rt.anchoredPosition = new Vector2(0, -screenH + t * speed);
                if (stills.Length > 0)
                {
                    int i = Mathf.Min(stills.Length - 1, (int)(t / per)); float f = Mathf.Clamp01((t - i * per) / 1.2f);
                    if (i != shown) { picB.texture = picA.texture; picA.texture = stills[i]; shown = i; CreditsStill = stills[i].name; }
                    picA.color = new Color(1, 1, 1, f); picB.color = new Color(1, 1, 1, i > 0 ? 1f - f : 0f);
                    float z = 1.02f + (t - i * per) * .006f; picA.rectTransform.localScale = new Vector3(z, z, 1);   // a slow push in
                }
                if (!Controls.Skip.IsPressed()) armed = true;
                held = armed && Controls.Skip.IsPressed() ? held + Time.unscaledDeltaTime : 0;
                skip.text = held > 0 ? $"Skipping... {Mathf.CeilToInt(Mathf.Max(0, 1f - held))}" : $"Hold {Controls.Btn(Controls.Act.Skip)} to skip";
                if (held >= 1f) { CreditsSkipped = true; break; }
                yield return null;
            }
            CreditsSeconds = Time.time - start;
            PlayLog.Line($"the credits {(CreditsSkipped ? "skipped" : "ended")} after {CreditsSeconds:0.0} s");
            Destroy(canvas.gameObject);
            CreditsRolling = false;
        }
        static RawImage MakeStill(Transform parent, string name)
        {
            var img = UiKit.Rect(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<RawImage>();
            img.raycastTarget = false; img.color = new Color(1, 1, 1, 0);
            var fit = img.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = 16f / 9f;
            return img;
        }
        // "# Title" lines big and gold, "## Heading" lines in the accent colour, the rest as they are
        static string Format(string s)
        {
            var lines = s.Replace("\r", "").Split('\n').Select(l =>
                l.StartsWith("## ") ? $"<size=30><color=#FFC64D>{l.Substring(3)}</color></size>" :
                l.StartsWith("# ") ? $"<size=78><b><color=#FFD27A>{l.Substring(2)}</color></b></size>" : l);
            return string.Join("\n", lines);
        }
    }
}
