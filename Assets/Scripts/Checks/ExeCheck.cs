using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ShardsOfTime
{
    // Round 1, item 14: the exe self-check, in the built game:  ShardsOfTime.exe -batchmode -mute -exeCheck <dir> [-exeCheckOnly all|play|measure|mute|perf]
    // (start it with python Tools/run_check.py <dir>: muted, no window, Below Normal). It plays from the title screen with a virtual
    // gamepad added to the Input System, so every press goes through the same input actions a player's pad does. "play" = the whole
    // playground as a player would (followers watched all the way); "measure" = one targeted measurement per Round 1 item (teleports
    // set up each measurement; the input is still the pad). Screenshots from the player's view, exe_check.txt, then it quits.
    public sealed partial class ExeCheck : MonoBehaviour
    {
        Gamepad pad; Keyboard kb; GamepadState state; readonly StringBuilder log = new StringBuilder(); string dir;
        int fails; readonly List<string> failList = new List<string>();
        Party P => Party.I; Hero Player => Party.I.Player; CharacterMotor M => Party.I.Player.Motor;

        IEnumerator Start()
        {
            dir = CheckRun.Dir; Directory.CreateDirectory(dir);
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            pad = InputSystem.AddDevice<Gamepad>("CheckPad");
            kb = Keyboard.current ?? InputSystem.AddDevice<Keyboard>("CheckKeyboard");
            Log($"Shards of Time exe check {DateTime.Now:yyyy-MM-dd HH:mm}, version {Application.version}; {(TestRun.Headless ? $"{TestRun.View.width}x{TestRun.View.height} off-screen" : $"{Screen.width}x{Screen.height}")}; GPU {SystemInfo.graphicsDeviceName}; Steam Deck setting {(SteamDeck.On ? "on" : "off")}");
            Log(TestRun.WindowReport() + "; " + TestRun.Report());
            yield return null; yield return null;
            string only = CheckRun.Only;
            IEnumerator run = only == "fix101" ? Fix101() : only == "minplayer" ? MinPlayer() : only == "tpose" ? TPoseSweep() : only == "sails" ? SailsView() : only == "theater" ? TheaterTitle() : only == "seareach" ? SeaReach() : only == "pirate" ? PirateOnly() : only == "perfpirate" ? PerfPirate() : only == "stuckpirate" ? StuckPirate() : only == "piratediag" ? PirateDiag() : only == "fix81pirate" ? Fix81Pirate() : only == "deepgate" ? DeepGateOnly() : only == "r9" ? R9() : only == "fix81" ? Fix81() : only == "fix81med" ? Fix81Medieval() : only == "floors" ? Floors81() : only == "platforms" ? Platforms81() : only == "reach" ? Reach81() : only == "rule81" ? Rule81Only() : only == "medieval" ? MedievalOnly() : only == "perfmed" ? PerfMedieval() : only == "meddiag" ? MedDiag() : only == "stuckmed" ? StuckMedieval() : only == "fix72" ? Fix72() : only == "stuck" ? StuckAll() : only == "well" ? WellOnly() : only == "bethlehem" ? BethlehemOnly() : only == "fullbeth" ? FullBethlehem() : only == "perfbeth" ? PerfBethlehem() : only == "bethdiag" ? BethDiag() : only == "fix61" ? Fix61() : only == "coyote" ? CoyoteDiag() : only == "mute" ? MuteCheck() : only == "diag" ? Diag() : only == "tour" ? Tour() : only == "film" ? Film() : only == "zones" ? Zones() : only == "violinist" ? ViolinistCheck() : only == "straight" ? Straight() : only == "underice" ? UnderIceCheck() : only == "egypt" ? EgyptOnly() : only == "full" ? Full() : only == "perfegypt" ? PerfEgypt() : only == "heads" ? Heads() : only == "egyptdiag" ? EgyptDiag() : only == "tomb" ? TombOnly() : only == "camel" ? CamelOnly() : only == "play" ? IceAgePlay() : only == "playground" ? Play() : only == "fixes" ? Fixes() : only == "models" ? Models() : only == "measure" ? Measure() : only == "perf" ? PerfIceAge() : only == "perfplayground" ? Perf() : All();
            while (true)
            {
                object cur;
                try { if (!run.MoveNext()) break; cur = run.Current; }
                catch (Exception e) { Fail("the check stopped with an exception: " + e); break; }
                yield return cur;
            }
            Release();
            Log("");
            Log(fails == 0 ? "RESULT: all checks passed" : $"RESULT: {fails} check(s) failed: " + string.Join(" | ", failList));
            Log("end: " + TestRun.WindowReport() + "; " + TestRun.Report());
            File.WriteAllText(Path.Combine(dir, "exe_check.txt"), log.ToString());
            yield return new WaitForSecondsRealtime(.5f);
            Application.Quit(0);
        }

        void Log(string s) { log.AppendLine(s); Debug.Log("[ExeCheck] " + s); }
        float beat;
        void Update()
        {
            if (Time.realtimeSinceStartup < beat || !Party.I) return;
            beat = Time.realtimeSinceStartup + 5f;
            Debug.Log($"[ExeCheck] (heartbeat frame {Time.frameCount}, {1f / Mathf.Max(.0001f, Time.smoothDeltaTime):0} fps, player {Player.Stats.displayName} at {Player.transform.position}, mode {M.Mode}, cutscene {Cutscenes.Playing ?? "none"})");
        }
        void Ok(bool pass, string what) { if (pass) Log("  ok    " + what); else Fail(what); }
        void Fail(string what) { fails++; failList.Add(what); Log("  FAIL  " + what); }
        void Head(string s) { Log(""); Log("== " + s); }

        IEnumerator All()
        {
            yield return IceAgePlay();
            yield return Fixes();
            yield return PerfIceAge();
        }

        // ---------------------------------------------------------------- the virtual pad
        void Set() => InputSystem.QueueStateEvent(pad, state);
        void Release() { state = default; Set(); }
        void Btn(GamepadButton b, bool down) { state = state.WithButton(b, down); Set(); }
        IEnumerator Press(GamepadButton b) { Btn(b, true); yield return null; yield return null; Btn(b, false); yield return null; yield return null; }
        IEnumerator Hold(GamepadButton b, float s) { Btn(b, true); yield return Wait(s); Btn(b, false); yield return null; }
        void Stick(Vector2 v) { state.leftStick = v; Set(); }
        void Look(Vector2 v) { state.rightStick = v; Set(); }
        IEnumerator Key(Key k) { InputSystem.QueueStateEvent(kb, new KeyboardState(k)); yield return null; yield return null; InputSystem.QueueStateEvent(kb, new KeyboardState()); yield return null; yield return null; }

        // the stick that moves the player toward a world direction (the game turns the stick by the camera's yaw)
        Vector2 StickFor(Vector3 worldDir)
        {
            var local = Quaternion.Euler(0, -CameraRig.I.Yaw, 0) * new Vector3(worldDir.x, 0, worldDir.z);
            return new Vector2(local.x, local.z);
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            Debug.Log($"[ExeCheck] (taking {name}: camera at {CameraRig.I.Cam.transform.position}, player at {Player.transform.position})");
            if (TestRun.Headless) TestRun.SaveView(CameraRig.I.Cam, Path.Combine(dir, name + ".png"));
            else ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"));
            yield return null; yield return null;
            Log($"  screenshot {name}.png");
        }
        // game time (slow frames on a busy PC don't shorten a held button), real time only while paused
        IEnumerator WaitFor(Func<bool> f, float seconds) { bool paused = Time.timeScale == 0; float end = (paused ? Time.realtimeSinceStartup : Time.time) + seconds; while (!f() && (paused ? Time.realtimeSinceStartup : Time.time) < end) yield return null; }
        IEnumerator Wait(float s) { if (Time.timeScale == 0) yield return new WaitForSecondsRealtime(s); else yield return new WaitForSeconds(s); }

        // ---------------------------------------------------------------- the bot (pad only)
        bool reached;
        // run to a point (flat distance), at a share of full stick; hops when blocked by a step
        IEnumerator Go(Vector3 target, float tol = .7f, float timeout = 25f, float speed = 1f, Func<bool> stop = null)
        {
            float end = Time.time + timeout, blocked = 0; reached = false;
            while (Time.time < end)
            {
                if (stop != null && stop()) { reached = true; break; }
                var d = target - Player.transform.position; d.y = 0;
                if (d.magnitude < tol) { reached = true; break; }
                Stick(StickFor(d.normalized) * Mathf.Clamp(d.magnitude / 1.5f, .35f, 1f) * speed);
                var hv = M.Velocity; hv.y = 0;
                blocked = M.Grounded && hv.magnitude < .5f ? blocked + Time.deltaTime : 0;
                if (blocked > .5f) { blocked = 0; yield return Press(GamepadButton.South); }
                yield return null;
            }
            Stick(Vector2.zero);
            if (!reached) Log($"  (could not reach {Fmt(target)} in {timeout:0} s, at {Fmt(Player.transform.position)})");
        }

        // walk toward `target` at `approach` stick, jump (held) when `jumpWhen` is true, steer to the target in the air, until landed
        IEnumerator JumpTo(Vector3 target, Func<bool> jumpWhen, float approach = .5f, float hold = .35f, bool doubleJump = false, float timeout = 8f)
        {
            float end = Time.time + timeout; bool jumped = false, dj = false, released = false; float jt = 0; reached = false;
            while (Time.time < end)
            {
                var d = target - Player.transform.position; d.y = 0;
                if (!jumped)
                {
                    Stick(StickFor(d.normalized) * approach);
                    if (jumpWhen() && M.Grounded) { jumped = true; Btn(GamepadButton.South, true); jt = 0; }
                }
                else
                {
                    jt += Time.deltaTime;
                    if (jt > hold && !doubleJump) Btn(GamepadButton.South, false);
                    // a double jump as a player does it: hold A to the top of the first jump (letting go early cuts it short), tap again
                    if (doubleJump && !dj && !released && jt > .15f && M.Velocity.y < 1.5f) { Btn(GamepadButton.South, false); released = true; jt = .15f; }
                    else if (doubleJump && !dj && released && jt > .2f) { Btn(GamepadButton.South, true); dj = true; jt = .2f; }
                    if (doubleJump && dj && jt > .2f + hold) Btn(GamepadButton.South, false);
                    Stick(StickFor(d.normalized) * Mathf.Clamp(d.magnitude / 1.2f, 0f, 1f));
                    if (M.Grounded && jt > .15f) { reached = d.magnitude < 1.5f; break; }
                }
                yield return null;
            }
            Btn(GamepadButton.South, false); Stick(Vector2.zero);
            yield return null;
        }

        IEnumerator SwitchTo(int i)
        {
            int guard = 0;
            while (P.Index != i && guard++ < 3) yield return Press((i - P.Index + 3) % 3 == 1 ? GamepadButton.RightShoulder : GamepadButton.LeftShoulder);
            yield return null;
        }

        void Tp(Vector3 pos, float yaw) { P.PlaceAll(pos, Quaternion.Euler(0, yaw, 0)); CameraRig.I.SnapBehind(); }
        IEnumerator TpSettle(Vector3 pos, float yaw) { Tp(pos, yaw); yield return Wait(.4f); }

        bool Has(string shard) => SaveGame.Current.World(Game.World.worldId).shards.Contains(shard);
        static string Fmt(Vector3 v) => $"({v.x:0.0}, {v.y:0.0}, {v.z:0.0})";

        string Followers()
        {
            var sb = new StringBuilder();
            foreach (var h in P.Heroes) if (h != Player) sb.Append($"{h.Stats.displayName} {Vector3.Distance(h.transform.position, Player.transform.position):0.0} m ({h.Follower.Warps} warps{(h.Follower.Warps > 0 ? ", last: " + h.Follower.LastWarpReason : "")}); ");
            return sb.ToString();
        }
        // both followers within reach a moment after the player stops (rule 4: none stuck)
        IEnumerator FollowersArrive(string where)
        {
            yield return WaitFor(() => P.Heroes.Where(h => h != Player).All(h => Vector3.Distance(h.transform.position, Player.transform.position) < 5f), 6f);
            bool ok = P.Heroes.Where(h => h != Player).All(h => Vector3.Distance(h.transform.position, Player.transform.position) < 5f);
            Ok(ok, $"followers arrived at {where}: {Followers()}");
        }
    }
}
