using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ShardsOfTime
{
    // Round 6: Dan's fixes on 0.5.0 and Egypt.
    public sealed partial class ExeCheck
    {
        // ---------------------------------------------------------------- item 1: "they can't walk a straight line"
        // Each hero on the flat landing snowfield, forward held for 10 s: exactly up, up with a 3 deg thumb tilt (a real pad is never
        // exactly straight), and W on the keyboard. Per second: the path's heading, the hero's yaw, the camera's yaw, the raw stick.
        // Drift = how fast the path's heading turns (deg/s, seconds 1-10). Pass: under 1 deg/s.
        IEnumerator Straight()
        {
            yield return EnsurePlaying(false);
            Head("Round 6, item 1: holding forward goes straight (10 s per run)");
            for (int hero = 0; hero < 3; hero++)
            {
                yield return SwitchTo(hero);
                foreach (var (label, tilt, key) in new[] { ("stick up", 0f, false), ("stick up, 3 deg tilt", 3f, false), ("stick up, 8 deg tilt", 8f, false), ("keyboard W", 0f, true) })
                {
                    yield return TpSettle(IceAge.OnGround(-22, 8) + Vector3.up * .1f, 0); yield return Wait(.6f);
                    var raw = new Vector2(Mathf.Sin(tilt * Mathf.Deg2Rad), Mathf.Cos(tilt * Mathf.Deg2Rad));
                    if (key) InputSystem.QueueStateEvent(kb, new KeyboardState(UnityEngine.InputSystem.Key.W)); else Stick(raw);
                    var sb = new System.Text.StringBuilder(); float h1 = float.NaN, hPrev = float.NaN, worst = 0, turned = 0; var hs = new System.Collections.Generic.List<float>(); Vector3 p0 = Player.transform.position, pPrev = p0;
                    for (int s = 1; s <= 10; s++)
                    {
                        yield return Wait(1f);
                        var p = Player.transform.position; var d = p - pPrev; d.y = 0; pPrev = p;
                        float heading = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                        if (s == 1) h1 = heading; else { float step = Mathf.Abs(Mathf.DeltaAngle(hPrev, heading)); worst = Mathf.Max(worst, step); turned += step; }
                        hPrev = heading; hs.Add(heading);
                        var st = Controls.Move.ReadValue<Vector2>();
                        sb.Append($"\n      t{s,2}: path {heading,6:0.0}  hero {Player.transform.eulerAngles.y,6:0.0}  camera {CameraRig.I.Yaw,6:0.0} (orbit {CameraRig.I.Orbit.HorizontalAxis.Value,6:0.0})  stick ({st.x:0.000}, {st.y:0.000})  speed {d.magnitude:0.0} m/s");
                    }
                    if (key) InputSystem.QueueStateEvent(kb, new KeyboardState()); else Stick(Vector2.zero);
                    // the path's turn: the median heading of seconds 6-10 against seconds 1-5, over the 5 s between them (a bump into a rock
                    // for a second doesn't count; a steady curve does: the Round 5 bug turned 35 deg every second)
                    float Med(System.Collections.Generic.IEnumerable<float> v) { var l = v.OrderBy(x => x).ToList(); return l[l.Count / 2]; }
                    float drift = Mathf.Abs(Mathf.DeltaAngle(Med(hs.Take(5)), Med(hs.Skip(5)))) / 5f;
                    if (turned / 9f > 20f) drift = turned / 9f;   // circling: the medians can wrap round; every second's turn added up
                    Ok(drift < 1f, $"{Player.Stats.displayName}, {label}: drift {drift:0.00} deg/s (median heading, first 5 s against last 5 s; worst 1 s step {worst:0.0} deg), ran {Vector3.Distance(p0, Player.transform.position):0} m" + sb);
                    yield return Wait(.5f);
                }
            }
            // steering still works: stick turned 90 deg right for 1 s turns the path
            yield return SwitchTo(0);
            yield return TpSettle(IceAge.OnGround(-22, 8) + Vector3.up * .1f, 0); yield return Wait(.6f);
            Stick(Vector2.up); yield return Wait(1.5f);
            var a = Player.transform.position; Stick(new Vector2(.7071f, .7071f)); yield return Wait(1f); var b = Player.transform.position; yield return Wait(.6f); var c = Player.transform.position;
            Stick(Vector2.zero);
            float turn = Mathf.Atan2(c.x - b.x, c.z - b.z) * Mathf.Rad2Deg;
            Ok(turn > 30f, $"steering: stick to up-right turns the path to {turn:0} deg (from 0)");
            Log("  " + TestRun.Report());
        }

        // ---------------------------------------------------------------- item 2: under the ice
        int camInIce; float underMax, breathLow;
        void WatchUnderIce()
        {
            var cam = CameraRig.I.Cam.transform.position;
            if (Physics.CheckSphere(cam, .05f, ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << Hero.EnemyLayer) | (1 << 4) | (1 << 2)), QueryTriggerInteraction.Ignore))
            {
                camInIce++;
                if (camInIce <= 4) Log($"    (camera in a solid at {Fmt(cam)}: player {Fmt(Player.transform.position)} mode {M.Mode} climbing {M.Climbing} under the ice {IceHole.UnderIce(M)}, camera {CameraRig.I.Distance:0.0} m from the focus)");
            }
            underMax = Mathf.Max(underMax, IceHole.UnderFor); breathLow = Mathf.Min(breathLow, M.Breath01);
        }
        IEnumerator Watched(IEnumerator e) { while (e.MoveNext()) { WatchUnderIce(); yield return e.Current; } }

        // dive in at the near hole, swim the channel (L05), come up in the exit hole: lifted out onto the ice
        IEnumerator ThroughTheChannel(string shot)
        {
            camInIce = 0; underMax = 0; breathLow = 1; int outs = M.ClimbOuts;
            yield return Go(XZ(-118, 76), .6f, 20f); yield return Go(XZ(-118, 82), .4f, 6f, .6f);
            yield return Watched(WaitFor(() => M.Mode == MoveMode.Swim, 4f));
            Btn(GamepadButton.East, true); yield return Watched(WaitFor(() => Player.transform.position.y < -3.5f, 3f));
            float t0 = Time.time;
            for (int i = 0; i < 8 && Player.transform.position.z < 90f; i++)   // dive held and forward until under the ice (as a player does)
            {
                yield return Watched(Go(XZ(-118, 90.5f), .6f, 1f));
                var hitName = Physics.Raycast(M.Center, Vector3.forward, out var fh, 1.5f, ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << 4) | (1 << 2)), QueryTriggerInteraction.Ignore) ? $"{fh.collider.name} at {Fmt(fh.point)}" : "nothing";
                Log($"    (diving in: {Fmt(Player.transform.position)} v {Fmt(M.Velocity)} under the ice {IceHole.UnderIce(M)}; ahead: {hitName})");
            }
            Btn(GamepadButton.East, false);
            yield return Watched(Go(XZ(-118, 96), .6f, 12f));
            if (shot != null) yield return Shot(shot + "_under");
            yield return Watched(Go(XZ(-118, 106), .6f, 12f));
            if (shot != null) yield return Shot(shot + "_exit");
            Btn(GamepadButton.South, true); yield return Watched(WaitFor(() => M.Climbing || M.Mode != MoveMode.Swim, 5f)); Btn(GamepadButton.South, false);
            yield return Watched(WaitFor(() => !M.Climbing && M.Grounded, 4f));
            float took = Time.time - t0;
            if (shot != null) yield return Shot(shot + "_out");
            Ok(M.ClimbOuts > outs && M.Grounded && Player.transform.position.y > -.2f,
               $"{Player.Stats.displayName}: through the channel and lifted out onto the ice in {took:0.0} s (longest under the ice {underMax:0.0} s, breath {(Player.Stats.swimmer ? "not needed" : $"{Player.Stats.breathSeconds:0} s, lowest {breathLow * 100:0}%")}; at {Fmt(Player.transform.position)})");
            Ok(camInIce == 0, $"{Player.Stats.displayName}: the camera never inside the ice ({camInIce} frames)");
        }

        IEnumerator UnderIceCheck()
        {
            yield return EnsurePlaying(false);
            Head("Round 6, item 2: under the ice");
            Log($"  holes: {IceHole.All.Count} ({string.Join(", ", IceHole.All.Select(h => $"{h.area.width:0}x{h.area.height:0} m at ({h.area.center.x:0}, {h.area.center.y:0})"))}); the channel near -> exit hole: 16 m");
            yield return TpSettle(IceAge.OnGround(-104, 76) + Vector3.up * .1f, 270);
            yield return SwitchTo(0); yield return ThroughTheChannel("70_dan");
            Ok(Has("L05"), "L05 in the channel");
            yield return SwitchTo(2); yield return ThroughTheChannel("71_kira");
            yield return SwitchTo(1); yield return ThroughTheChannel(null);
            // a quick out: drop in, come straight back up in the same hole: A lifts the hero out
            yield return SwitchTo(0);
            yield return Go(XZ(-118, 76), .6f, 20f); yield return Go(XZ(-118, 82), .4f, 6f, .6f); yield return WaitFor(() => M.Mode == MoveMode.Swim, 4f);
            Btn(GamepadButton.East, true); yield return Wait(1.2f); Btn(GamepadButton.East, false); int o = M.ClimbOuts;
            yield return WaitFor(() => Player.transform.position.y > -2.6f, 8f);
            Log($"    (before A: {Fmt(Player.transform.position)} mode {M.Mode} hole {(IceHole.At(Player.transform.position) ? "yes" : "no")} water top {(M.Water ? M.Water.Top : 0):0.00} climbs {M.ClimbOuts})");
            yield return Press(GamepadButton.South);
            Log($"    (after A: {Fmt(Player.transform.position)} mode {M.Mode} climbing {M.Climbing} climbs {M.ClimbOuts} playing {Game.Playing})");
            yield return WaitFor(() => M.ClimbOuts > o, 3f);
            yield return WaitFor(() => !M.Climbing && M.Grounded, 4f);
            Ok(M.ClimbOuts > o && M.Grounded, $"down and straight back up the near hole, A: lifted out onto the ice ({Fmt(Player.transform.position)})");
            // stuck: Dan parked under the ice against the channel's side, doing nothing: brought to a hole and out
            int r = IceHole.Rescues, hearts = Health.Hearts; Stick(Vector2.zero);
            M.Teleport(new Vector3(-120.4f, -4f, 96f), Quaternion.identity); CameraRig.I.SnapBehind();
            float t = Time.time; camInIce = 0;
            yield return Watched(WaitFor(() => IceHole.Rescues > r, 20f)); float rescuedAt = Time.time - t;
            yield return Wait(.15f); yield return Shot("72_rescued");
            yield return WaitFor(() => !M.Climbing && M.Grounded, 4f);
            Ok(IceHole.Rescues > r && M.Grounded && Player.transform.position.y > -.2f && Health.Hearts == hearts,
               $"Dan left under the ice: brought up a hole after {rescuedAt:0.0} s and out ({Fmt(Player.transform.position)}), no heart lost ({hearts} -> {Health.Hearts}): \"{Hud.LastMessage}\"");
            yield return Shot("73_on_the_ice");
            Log("  " + TestRun.Report());
        }

        // ---------------------------------------------------------------- item 3: the hard parts, close up in the game
        // the head end of a model: its "Head" bone if it has one, else the front top of its renderers
        static Transform HeadBone(Transform root) => root.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Head" || t.name.EndsWith(":Head"));
        IEnumerator HeadShots(Transform who, string name, float size)
        {
            var head = HeadBone(who); var at = head ? head.position : who.position + Vector3.up * size;
            var fwd = who.forward; fwd.y = 0; fwd.Normalize(); var side = Vector3.Cross(Vector3.up, fwd);
            yield return CloseUpAt(at + fwd * size * 1.3f + Vector3.up * size * .15f, at - Vector3.up * size * .15f, name + "_front");
            yield return CloseUpAt(at + side * size * 1.3f + fwd * size * .3f, at - Vector3.up * size * .1f, name + "_side");
            yield return CloseUpAt(at + (fwd + side).normalized * size * 1.25f - Vector3.up * size * .2f, at - Vector3.up * size * .2f, name + "_34low");
        }

        IEnumerator Heads()
        {
            yield return EnsurePlaying(false);
            Head("Round 6, item 3: tusks, fangs, claws: their own clean pieces and plain material (close-ups in the game)");
            foreach (var n in new[] { "mammoth", "wolf", "kira_sabertooth", "snow_critter" })
            {
                var hard = CharacterModel.HardMat(n);
                Log($"  {n}: hard-parts material {(hard ? $"{hard.name} {hard.GetColor("_BaseColor")}" : "none (no hard parts)")}");
            }
            Ok(CharacterModel.HardMat("mammoth") && CharacterModel.HardMat("wolf") && CharacterModel.HardMat("kira_sabertooth"), "mammoth tusks, wolf claws and sabertooth Kira's fangs have their own plain material");
            Time.timeScale = 0f;   // hold everyone still for the photos
            var m = Mammoth.All.FirstOrDefault();
            if (m)
            {
                var r = m.GetComponentsInChildren<SkinnedMeshRenderer>().FirstOrDefault();
                Ok(r && r.sharedMaterials.Length > 1 && r.sharedMaterials[1].name.EndsWith("_hard"), $"the mammoth in the game: {(r ? string.Join(", ", r.sharedMaterials.Select(x => x ? x.name : "none")) : "no model")}");
                yield return HeadShots(m.transform, "75_mammoth_head", 2.2f);
            }
            var wolf = Enemy.All.FirstOrDefault(e => e.kind == Enemy.Kind.Wolf);
            if (wolf) { yield return HeadShots(wolf.transform, "76_wolf_head", .6f); yield return CloseUpAt(wolf.transform.position + wolf.transform.forward * 1.1f + Vector3.up * .35f, wolf.transform.position + wolf.transform.forward * .4f, "76d_wolf_paws"); }
            var critter = Enemy.All.FirstOrDefault(e => e.kind == Enemy.Kind.Critter);
            if (critter) yield return HeadShots(critter.transform, "77_critter_head", .5f);
            Time.timeScale = 1f;
            yield return SwitchTo(2); yield return Wait(.5f);
            Time.timeScale = 0f;
            yield return HeadShots(Player.transform, "78_kira_sabertooth_head", .32f);
            Time.timeScale = 1f;
            Log("  " + TestRun.Report());
        }
    }
}
