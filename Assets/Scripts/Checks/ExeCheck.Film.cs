using System.Collections;
using System.IO;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 5 (Dan: the editor render played sound and took his focus): a cutscene rendered from the built game, as a check run:
    //   ShardsOfTime.exe -batchmode -mute -exeCheck <dir> -exeCheckOnly film -film <Name>    (Tools/render_cutscene.py starts it)
    // Batch mode, no window, muted from the first frame (TestRun). Game time runs at a fixed 30 frames a second (Time.captureFramerate),
    // so every frame is one video frame whatever the PC's speed; each is drawn off-screen and saved as <dir>/frames/00000.png ...
    // The HUD shows nothing during a film (no skip prompt) but keeps the cutscene's white-outs. ffmpeg makes the MP4 afterwards.
    public sealed partial class ExeCheck
    {
        static Transform FindDeep(Transform t, string n) { foreach (Transform c in t) { if (c.name == n) return c; var f = FindDeep(c, n); if (f) return f; } return null; }
        public const int FilmFps = 30;

        IEnumerator Film()
        {
            string name = LauncherBridge.Argument("-film") ?? "IceAgeArrival";
            Head($"Film: {name} at {FilmFps} fps, {TestRun.View?.width}x{TestRun.View?.height}, drawn off-screen");
            var frames = Path.Combine(dir, "frames"); Directory.CreateDirectory(frames);
            bool ending = name == "Farewell" || name == "TheKing" || name == "HomeAgain" || name == "Credits" || name == "Ending";   // Round 14: played by Game.FilmEnding (HomeAgain is two parts; the credits are no Timeline)
            if (!ending && !Cutscenes.Load(name)) { Fail("no cutscene " + name); yield break; }
            if (ending)
            {
                yield return Wait(1f);
                Time.captureFramerate = FilmFps;
                Game.I.StartCoroutine(Game.I.Film(name));
                yield return null;
                int k = 0;
                while (Game.I.FilmBusy)
                {
                    yield return new WaitForEndOfFrame();
                    TestRun.SaveView(CameraRig.I.Cam, Path.Combine(frames, $"{k:00000}.png"));
                    k++; yield return null;
                }
                Time.captureFramerate = 0;
                Ok(k > FilmFps * 10, $"{name}: {k} frames saved ({k / (float)FilmFps:0.00} s)");
                Log("  " + TestRun.Report());
                yield break;
            }
            yield return Wait(1f);   // the world and the party are built
            Time.captureFramerate = FilmFps;
            Game.I.StartCoroutine(Game.I.Film(name));
            float guard = Time.realtimeSinceStartup + 60f;
            while (Cutscenes.Playing != name && Time.realtimeSinceStartup < guard) yield return null;
            if (Cutscenes.Playing != name) { Fail($"{name} didn't start"); yield break; }
            int n = 0; double length = Cutscenes.Director ? Cutscenes.Director.duration : 0;
            while (Cutscenes.Playing == name)
            {
                yield return new WaitForEndOfFrame();
                TestRun.SaveView(CameraRig.I.Cam, Path.Combine(frames, $"{n:00000}.png"));
                if (n % 45 == 0 && Cutscenes.Director)   // Round 9: where the late actors really are (a diagnosis line every 1.5 s)
                    foreach (var nm in new[] { "Dan falling", "Dan tumbling" })
                        if (Cutscenes.Director.transform.Find(nm) is Transform h || (h = FindDeep(Cutscenes.Director.transform, nm)))
                        {
                            var body = h.Find("Body"); var model = body && body.childCount > 0 ? body.GetChild(0) : null;
                            Debug.Log($"[Film] t {Cutscenes.Director.time:0.0}: {nm} holder {h.position} body {(body ? body.position.ToString() : "-")} model {(model ? model.position.ToString() : "-")} active {h.gameObject.activeInHierarchy}; camera {CameraRig.I.Cam.transform.position}");
                        }
                n++;
                yield return null;
            }
            Time.captureFramerate = 0;
            int expect = (int)(length * FilmFps);
            Ok(n >= expect - 2, $"{name}: {n} frames saved ({n / (float)FilmFps:0.00} s; the cutscene is {length:0.00} s)");
            Log("  " + TestRun.Report());
        }
    }
}
