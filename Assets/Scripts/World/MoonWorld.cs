using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 13: the Moon's pieces (World/Moon.cs places them).

    // ---- a bouncy crater rim: anyone landing on it is thrown up high and floaty (Hero.OnMotorEvent "land")
    public sealed class MoonBounce : MonoBehaviour
    {
        public float height = 7f;
        public static int Bounces { get; private set; }
        public void Bounce(Hero h)
        {
            h.Motor.Launch(height); Bounces++;
            if (h.IsPlayer) { Sfx.Play("critter_boing", .55f, .7f); PlayLog.Line($"{h.Stats.displayName} bounces off a crater rim ({height} m)"); }
            Placeholders.Puff(h.transform.position + Vector3.up * .1f, new Color(.8f, .8f, .85f));
        }
    }

    // ---- KiraFloats (the film's gag): once, on the way to the rover
    public sealed class FloatsZone : MonoBehaviour
    {
        public float radius = 7f;
        void Update()
        {
            if (!Game.Playing || !Party.I || Game.I.Busy || !Game.InMoon) return;
            var pr = SaveGame.Current.World(Game.World.worldId); if (pr.opened.Contains(Moon.FloatsFlag)) { enabled = false; return; }
            var d = Party.I.Player.transform.position - transform.position; d.y = 0;
            if (d.magnitude < radius && Party.I.Player.Motor.Grounded) Game.I.StartCoroutine(Game.I.KiraFloatsScene(transform.position));
        }
    }

    // ---- the rover's control panel: the light puzzle (the film: only Abi solves it). Fix 13.2 (Dan: "it sux"): the panel's four big lights
    // ARE the controller's face buttons, in the pad's own diamond (Y top, X left, B right, A bottom), coloured like them and each with its
    // letter big on it (A B X Y; on a PlayStation pad the cross / circle / square / triangle shapes; on the keyboard the keys I J K L in
    // the same diamond). The white shard's light flashes a pattern; Abi presses the same buttons; each press lights that button, plays its
    // tone and her Button Pushing. Three rounds (4, 6, 8); a wrong press just replays the pattern. A row of dots shows the steps.
    // Dan or Muscle Man puzzle over it ("Switch to Abi"). Push the stick to step back.
    public sealed class RoverPanel : MonoBehaviour
    {
        public static readonly int[] Lengths = { 4, 6, 8 };
        public static readonly Vector2[] Diamond = { new Vector2(0, -.7f), new Vector2(-.82f, 0), new Vector2(.82f, 0), new Vector2(0, .7f) };   // A, B, X, Y (board space: seen from the front, -x is on the right)
        public bool Active { get; private set; }
        public bool Showing { get; private set; }
        public int Round { get; private set; }
        public int Wrongs { get; private set; }
        public int[] Pattern { get; private set; }
        public int Step { get; private set; }
        public int Flashing { get; private set; } = -1;     // the step being flashed now (checks)
        public static int Solved { get; private set; }
        public static int Presses;   // Fix 13.1
        public Controls.PadStyle ShownStyle { get; private set; }
        public string[] ShownLabels { get; private set; } = new string[4];
        public bool IsLit(int i) => discs[i] && discs[i].sharedMaterial == on[i];
        static IEnumerator LogClip(Hero abi) { yield return null; yield return null; PlayLog.Line($"Abi presses the panel (clip {abi.Anim.State})"); }
        float hintAt = -10, awayFor; bool mustLeave;

        readonly Renderer[] discs = new Renderer[4]; readonly Material[] on = new Material[4], off = new Material[4];
        readonly TextMesh[] letters = new TextMesh[4]; readonly GameObject[] shapes = new GameObject[4];
        readonly List<Renderer> dots = new List<Renderer>(); Material dotOn, dotOff, dotNow; TextMesh roundText;
        Transform board;

        // the board: an upright dark screen on the desk, facing +z (where Abi stands)
        public void BuildBoard()
        {
            board = new GameObject("Button board").transform; board.SetParent(transform, false); board.localPosition = new Vector3(0, 2.85f, -.15f);   // (above a standing Abi's helmet, big: Fix 13.2 sheet)
            Placeholders.Prim(PrimitiveType.Cube, board, Vector3.zero, new Vector3(3.3f, 2.6f, .12f), WorldArt.Mat("ChromeDark"), true, "Board");
            Placeholders.Prim(PrimitiveType.Cube, board, new Vector3(0, 0, .065f), new Vector3(3.15f, 2.45f, .01f), Placeholders.Mat(new Color(.05f, .06f, .1f)), false, "Screen");
            Placeholders.Prim(PrimitiveType.Cube, board, new Vector3(0, -1.55f, -.05f), new Vector3(.35f, .6f, .1f), WorldArt.Mat("ChromeDark"), true, "Board post");
            var white = Placeholders.Mat(Color.white, "Glow");
            for (int i = 0; i < 4; i++)
            {
                var c = Diamond[i];
                var d = Placeholders.Prim(PrimitiveType.Cylinder, board, new Vector3(c.x, c.y - .1f, .09f), new Vector3(.62f, .02f, .62f), Placeholders.Mat(Color.grey), false, "Button light " + i);
                d.transform.localRotation = Quaternion.Euler(90, 0, 0); discs[i] = d.GetComponent<Renderer>();
                var t = new GameObject("Letter " + i); t.transform.SetParent(board, false); t.transform.localPosition = new Vector3(c.x, c.y - .1f, .115f); t.transform.localRotation = Quaternion.Euler(0, 180, 0);
                var tm = t.AddComponent<TextMesh>(); tm.font = UiKit.Font; tm.fontSize = 96; tm.characterSize = .068f; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.fontStyle = FontStyle.Bold; tm.color = Color.white;
                UiKit.WorldText(tm); letters[i] = tm;
                // the PlayStation shapes, drawn with bars (the font has no such glyphs)
                var g = new GameObject("Shape " + i); g.transform.SetParent(board, false); g.transform.localPosition = new Vector3(c.x, c.y - .1f, .112f); g.transform.localScale = Vector3.one * 1.35f;
                void Bar(Vector2 a, Vector2 b2)
                {
                    var mid = (a + b2) / 2; var dd = b2 - a;
                    var bar = Placeholders.Prim(PrimitiveType.Cube, g.transform, new Vector3(mid.x, mid.y, 0), new Vector3(dd.magnitude + .035f, .035f, .01f), white, false, "Bar");
                    bar.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dd.y, dd.x) * Mathf.Rad2Deg);
                }
                if (i == 0) { Bar(new Vector2(-.11f, -.11f), new Vector2(.11f, .11f)); Bar(new Vector2(-.11f, .11f), new Vector2(.11f, -.11f)); }
                else if (i == 1) for (int k = 0; k < 20; k++) { float a0 = k * Mathf.PI / 10, a1 = (k + 1) * Mathf.PI / 10; Bar(new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * .12f, new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * .12f); }
                else if (i == 2) { var q = new[] { new Vector2(-.11f, -.11f), new Vector2(.11f, -.11f), new Vector2(.11f, .11f), new Vector2(-.11f, .11f) }; for (int k = 0; k < 4; k++) Bar(q[k], q[(k + 1) % 4]); }
                else { var q = new[] { new Vector2(0, .13f), new Vector2(.12f, -.08f), new Vector2(-.12f, -.08f) }; for (int k = 0; k < 3; k++) Bar(q[k], q[(k + 1) % 3]); }
                shapes[i] = g;
            }
            // the steps: up to 8 dots along the top; the round under the diamond
            dotOn = Placeholders.Mat(new Color(1f, .97f, .85f), "Glow"); dotOff = Placeholders.Mat(new Color(.25f, .27f, .32f)); dotNow = Placeholders.Mat(new Color(.6f, .9f, 1f), "Glow");
            for (int k = 0; k < 8; k++) { var dd = Placeholders.Prim(PrimitiveType.Sphere, board, new Vector3(-1.05f + k * .3f, 1.05f, .09f), Vector3.one * .14f, dotOff, false, "Step dot"); dots.Add(dd.GetComponent<Renderer>()); }
            var rt = new GameObject("Round"); rt.transform.SetParent(board, false); rt.transform.localPosition = new Vector3(0, -1.17f, .115f); rt.transform.localRotation = Quaternion.Euler(0, 180, 0);
            roundText = rt.AddComponent<TextMesh>(); roundText.font = UiKit.Font; roundText.fontSize = 64; roundText.characterSize = .028f; roundText.anchor = TextAnchor.MiddleCenter; roundText.color = new Color(.85f, .9f, 1f); UiKit.WorldText(roundText);
            Restyle(true); Dots();
        }

        // the letters / shapes and colours follow the pad in use (or the keyboard)
        void Restyle(bool force = false)
        {
            var st = Controls.Style; if (!force && st == ShownStyle) return; ShownStyle = st;
            bool ps = st == Controls.PadStyle.PlayStation;
            for (int i = 0; i < 4; i++)
            {
                var c = Controls.FaceColor(i, st);
                on[i] = Placeholders.Mat(Color.Lerp(c, Color.white, .25f), "Glow"); off[i] = Placeholders.Mat(c * .42f);
                letters[i].text = ps ? "" : Controls.FaceName(i, st); letters[i].gameObject.SetActive(!ps);
                shapes[i].SetActive(ps);
                ShownLabels[i] = Controls.FaceName(i, st);
                Light(i, false);
            }
            PlayLog.Line($"the rover's buttons show {st}: {string.Join(" ", ShownLabels)}");
        }

        void Dots()
        {
            int n = Pattern != null && Active ? Pattern.Length : 0;
            for (int k = 0; k < dots.Count; k++)
            {
                dots[k].gameObject.SetActive(k < n);
                dots[k].sharedMaterial = Showing ? (k == Flashing ? dotNow : k < Flashing ? dotOn : dotOff) : (k < Step ? dotOn : k == Step ? dotNow : dotOff);
            }
            if (roundText) roundText.text = Active ? (Showing ? $"Round {Round + 1} of 3: watch..." : $"Round {Round + 1} of 3: your turn") : "";
        }

        void Update()
        {
            if (!Game.InMoon || !Party.I || !board) return;
            Restyle();
            var pr = SaveGame.Current.World(Game.World.worldId);
            var p = Party.I.Player; var d = p.transform.position - transform.position; d.y = 0;
            bool near = d.magnitude < 2.2f && Mathf.Abs(p.transform.position.y - transform.position.y) < 1.5f;
            if (mustLeave) { if (d.magnitude > 2.8f) mustLeave = false; else return; }
            if (Active)
            {
                if (!near || p.Stats.displayName != "Abi") { Stop("stepped away"); return; }
                // push the stick (or WASD) for a moment: step back
                awayFor = Controls.Move.ReadValue<Vector2>().magnitude > .7f ? awayFor + Time.deltaTime : 0;
                if (awayFor > .45f) { Stop("stepped back"); mustLeave = true; return; }
                if (Showing) return;
                for (int i = 0; i < 4; i++) if (Controls.Face[i].WasPressedThisFrame()) { Press(i); break; }
                return;
            }
            if (!near || !Game.Playing || Game.I.Busy || !pr.necklaceAwake || pr.storyShard) return;
            if (p.Stats.displayName != "Abi")
            {
                if (Time.time > hintAt + 6f) { hintAt = Time.time; Hud.Message(p.IsMuscleMan || p.Stats.displayName == "Dan" ? $"{p.Stats.displayName} stares at the blinking buttons and scratches his head... Switch to Abi!" : "Kira paws at the buttons... nothing. Switch to Abi!", 4f); }
                return;
            }
            Begin();
        }

        string Names => string.Join(" ", ShownLabels);
        void Begin()
        {
            Active = true; Round = 0; Wrongs = 0; awayFor = 0; Party.I.Frozen = true;
            PlayLog.Line("the rover's light puzzle starts (Abi)");
            Hud.Message($"Watch the buttons light up, then press the same ones ({Names}). Push {Controls.Btn(Controls.Act.Move)} to step back.", 6f);
            NewRound();
        }
        void Stop(string why) { Active = false; Showing = false; StopAllCoroutines(); Flashing = -1; if (Party.I) Party.I.Frozen = false; for (int i = 0; i < 4; i++) Light(i, false); Dots(); PlayLog.Line($"the light puzzle stops ({why})"); }

        void NewRound()
        {
            var rnd = new System.Random(7 + Round * 31);   // the same patterns every time (the Violinist's tune hints at the rhythm)
            Pattern = new int[Lengths[Round]]; for (int i = 0; i < Pattern.Length; i++) Pattern[i] = rnd.Next(4);
            Step = 0; StartCoroutine(Show());
        }
        IEnumerator Show()
        {
            Showing = true; Flashing = -1; Dots(); yield return new WaitForSeconds(1.0f);
            for (int i = 0; i < Pattern.Length; i++)
            {
                int k = Pattern[i]; Flashing = i; Dots();
                Light(k, true); Sfx.Play("rover_tone_" + (k + 1), .7f); yield return new WaitForSeconds(.55f);
                Light(k, false); yield return new WaitForSeconds(.35f);
            }
            Showing = false; Flashing = -1; Step = 0; Dots();
            PlayLog.Line($"the rover shows round {Round + 1}: {string.Join(" ", System.Array.ConvertAll(Pattern, k => ShownLabels[k]))}");
        }
        void Light(int i, bool lit)
        {
            if (discs[i]) { discs[i].sharedMaterial = lit ? on[i] : off[i]; discs[i].transform.localScale = lit ? new Vector3(.72f, .02f, .72f) : new Vector3(.62f, .02f, .62f); }
        }

        void Press(int k)
        {
            StartCoroutine(Blink(k));
            var abi = Party.I ? Party.I.Player : null;   // Fix 13.1: Dan's Button Pushing (Clips/button) on each press
            if (abi && abi.Anim && abi.Anim.Has("button")) { abi.Anim.Trigger("button"); if (Presses++ == 0) StartCoroutine(LogClip(abi)); }
            if (k != Pattern[Step]) { Wrongs++; Sfx.Play("rover_wrong", .6f); PlayLog.Line($"wrong button ({ShownLabels[k]}, wanted {ShownLabels[Pattern[Step]]}): the pattern again"); Step = 0; StartCoroutine(Show()); return; }
            Sfx.Play("rover_tone_" + (k + 1), .7f); Step++; Dots();
            if (Step < Pattern.Length) return;
            Round++;
            if (Round < Lengths.Length) { Hud.Message($"Round {Round + 1} of 3...", 2f); NewRound(); return; }
            Solved++; Active = false; Party.I.Frozen = false; Dots();
            PlayLog.Line($"the light puzzle is solved ({Wrongs} wrong presses)");
            Game.I.StartCoroutine(Game.I.WhiteShardFinale());
        }
        IEnumerator Blink(int k) { Light(k, true); yield return new WaitForSeconds(.3f); if (!Showing) Light(k, false); }
    }
}
