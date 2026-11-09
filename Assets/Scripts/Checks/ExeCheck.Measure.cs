using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ShardsOfTime
{
    // One measurement per Round 1 item, each set up by a teleport, played with the pad.
    public sealed partial class ExeCheck
    {
        IEnumerator EnsurePlaying()
        {
            if (Game.I.State == Game.Mode.Title) { Game.I.StartGame(!SaveGame.Exists); yield return Wait(.5f); }
            if (Game.I.State == Game.Mode.Paused) Game.I.Resume();
            yield return WaitFor(() => !Game.InCutscene, 10f);
        }

        IEnumerator Measure()
        {
            yield return EnsurePlaying();
            Health.Refill();

            Head("Motor: run, analog, variable jump (items 4, 6)");
            foreach (int i in new[] { 0, 1, 2 })
            {
                yield return SwitchTo(i);
                yield return TpSettle(new Vector3(-10f, .1f, -50f), 0);
                Stick(StickFor(Vector3.forward)); yield return Wait(1.0f);
                float full = new Vector3(M.Velocity.x, 0, M.Velocity.z).magnitude;
                Stick(StickFor(Vector3.forward) * .5f); yield return Wait(.8f);
                float half = new Vector3(M.Velocity.x, 0, M.Velocity.z).magnitude;
                Stick(Vector2.zero); yield return Wait(.5f);
                yield return Hold(GamepadButton.South, .6f); yield return WaitFor(() => M.Grounded, 3f); yield return Wait(.2f);
                float high = M.LastJumpHeight;
                yield return Press(GamepadButton.South); yield return WaitFor(() => M.Grounded, 3f); yield return Wait(.2f);
                float low = M.LastJumpHeight;
                var s = Player.Stats;
                Ok(Mathf.Abs(full - s.runSpeed) < .3f && Mathf.Abs(half - s.runSpeed * .5f) < .5f && high > s.jumpHeight * .85f && low < high * .75f,
                    $"{s.displayName}: run {full:0.0} m/s (stats {s.runSpeed}), half stick {half:0.0}; jump held {high:0.00} m (stats {s.jumpHeight}), tapped {low:0.00} m");
            }
            yield return SwitchTo(1);
            yield return TpSettle(new Vector3(-10f, .1f, -50f), 0);
            Btn(GamepadButton.South, true); yield return Wait(.3f); Btn(GamepadButton.South, false); yield return null; yield return null;
            yield return WaitFor(() => M.Velocity.y < 1f, 2f);
            Btn(GamepadButton.South, true); yield return Wait(.4f); Btn(GamepadButton.South, false);
            yield return WaitFor(() => M.Grounded, 3f); yield return Wait(.2f);
            Ok(M.LastJumpHeight > 3.2f, $"Abi's double jump: {M.LastJumpHeight:0.00} m (the high ledge is 3.2 m)");

            Head("Coyote time and jump buffering (item 4)");
            yield return SwitchTo(0);
            int jumps = 0; void Count(string e) { if (e == "jump") jumps++; }
            M.Event += Count;
            foreach (var (late, expect) in new[] { (.07f, true), (.25f, false) })
            {
                yield return TpSettle(new Vector3(13.5f, 3.0f, -34.2f), 0);   // the plateau, its north edge 1.2 m ahead
                jumps = 0;
                Stick(StickFor(Vector3.forward) * .6f);
                yield return WaitFor(() => !M.Grounded, 3f);
                float t0 = Time.time;
                while (Time.time - t0 < late) yield return null;
                Btn(GamepadButton.South, true); yield return Wait(.1f); Btn(GamepadButton.South, false);
                Stick(Vector2.zero);
                Ok((jumps > 0) == expect, $"jump pressed {late:0.00} s after running off the edge: {(jumps > 0 ? "jumped" : "no jump")} (coyote time {CharacterMotor.CoyoteTime} s)");
                yield return WaitFor(() => M.Grounded, 3f);
            }
            yield return TpSettle(new Vector3(-10f, .1f, -50f), 0);
            yield return Hold(GamepadButton.South, .5f);
            yield return WaitFor(() => M.Velocity.y < -2f && Physics.Raycast(Player.transform.position + Vector3.up * .1f, Vector3.down, .45f), 3f);
            jumps = 0; float pressAt = Time.time;
            yield return Press(GamepadButton.South);
            yield return WaitFor(() => jumps > 0, 1f);
            Ok(jumps > 0, $"jump pressed just before landing: buffered, jumped {Time.time - pressAt:0.00} s after the press (buffer {CharacterMotor.JumpBuffer} s)");
            M.Event -= Count;
            yield return WaitFor(() => M.Grounded, 3f);

            Head("Slopes (item 4)");
            yield return TpSettle(new Vector3(10f, .1f, -48f), 0);
            Stick(StickFor(Vector3.forward)); yield return Wait(2.2f); Stick(Vector2.zero);
            Ok(Player.transform.position.y > 2.7f, $"up the 20 degree ramp to the plateau (y {Player.transform.position.y:0.0})");
            yield return TpSettle(new Vector3(17f, .1f, -41.5f), 0);
            float topY = 0; Stick(StickFor(Vector3.forward));
            for (float t = 0; t < 3f; t += Time.deltaTime) { if (M.Grounded && Time.frameCount % 20 == 0) Btn(GamepadButton.South, true); else Btn(GamepadButton.South, false); topY = Mathf.Max(topY, Player.transform.position.y); yield return null; }
            Stick(Vector2.zero); Btn(GamepadButton.South, false); yield return Wait(1f);
            Ok(Player.transform.position.y < 1f, $"the 55 degree slope can't be climbed, even jumping (highest {topY:0.0} m, slid back to {Player.transform.position.y:0.0})");
            yield return Shot("20_slopes");

            Head("Moving platform (item 4)");
            var plat = Object.FindObjectsByType<MovingPlatform>().First();
            yield return TpSettle(new Vector3(-29.3f, .1f, -5f), 270);
            yield return WaitFor(() => plat.transform.position.x > -31.75f, 8f);
            Stick(StickFor(Vector3.left) * .5f); yield return Wait(.45f); Stick(Vector2.zero);
            yield return WaitFor(() => M.Grounded, 2f);
            float x0 = Player.transform.position.x;
            yield return Wait(2.5f);
            bool on = M.OnPlatform; float moved = x0 - Player.transform.position.x;
            Ok(on && moved > 2.5f && Player.transform.position.y > -.3f, $"rides the platform: carried {moved:0.0} m with the stick released (on it: {on})");
            yield return Shot("21_moving_platform");
            yield return WaitFor(() => plat.transform.position.x < -39f, 6f);
            yield return Go(new Vector3(-45f, 0, -5f), .6f, 4f);
            Ok(Player.transform.position.x < -42f && Player.transform.position.y > -.2f, "off the platform onto the island");

            Head("Swim and breath (item 4)");
            yield return SwitchTo(0);
            yield return TpSettle(new Vector3(30f, -1.5f, 0f), 0);
            yield return Wait(.8f);
            Ok(M.Mode == MoveMode.Swim && !M.HeadUnderwater, $"Dan floats at the surface (mode {M.Mode}, head out)");
            int hearts0 = Health.Hearts; int hits0 = Health.HitsTaken;
            Btn(GamepadButton.East, true); yield return Wait(2f);
            float y1 = Player.transform.position.y; float breath1 = M.Breath01;
            Ok(M.HeadUnderwater && y1 < -2.5f && breath1 < .9f, $"B dives (y {y1:0.0}); breath meter drains ({breath1:0.00})");
            yield return Shot("22_breath");
            yield return WaitFor(() => Health.HitsTaken > hits0, 11f);
            Btn(GamepadButton.East, false);
            Ok(Health.HitsTaken > hits0, $"out of breath costs a heart ({hearts0} -> {Health.Hearts}) after {Player.Stats.breathSeconds} s");
            Btn(GamepadButton.South, true); yield return WaitFor(() => !M.HeadUnderwater, 5f); Btn(GamepadButton.South, false);
            yield return Wait(1.6f);
            Ok(M.Breath01 > .95f, $"breath refills at the surface ({M.Breath01:0.00})");
            Health.Refill();
            yield return SwitchTo(2);
            yield return TpSettle(new Vector3(30f, -1.5f, 0f), 0);
            Btn(GamepadButton.East, true); yield return Wait(4f);
            Ok(M.HeadUnderwater && M.Breath01 >= .999f && Health.HitsTaken == hits0 + 1, "Kira under water for 4 s: no breath meter (the best swimmer)");
            // camera under water
            Look(new Vector2(0, 1f)); yield return Wait(.3f); Look(Vector2.zero); yield return Wait(.3f);
            Ok(CameraRig.I.Underwater, $"the camera works under water (tinted; {CameraRig.I.Distance:0.0} m from Kira)");
            yield return Shot("23_camera_underwater");
            Btn(GamepadButton.East, false);
            Btn(GamepadButton.South, true); yield return WaitFor(() => !M.HeadUnderwater, 5f); Btn(GamepadButton.South, false);

            Head("Low gravity (item 4)");
            yield return SwitchTo(0);
            yield return TpSettle(new Vector3(-10f, .1f, -50f), 0);
            yield return Hold(GamepadButton.South, .8f); yield return WaitFor(() => M.Grounded, 3f); yield return Wait(.2f);
            float hN = M.LastJumpHeight, tN = M.LastAirTime;
            yield return TpSettle(new Vector3(27f, .1f, 40f), 0);
            yield return Hold(GamepadButton.South, 2f); yield return WaitFor(() => M.Grounded, 5f); yield return Wait(.2f);
            float hL = M.LastJumpHeight, tL = M.LastAirTime;
            Ok(hL > hN * 2.5f && tL > tN * 2f, $"Dan's jump: {hN:0.0} m / {tN:0.00} s outside, {hL:0.0} m / {tL:0.00} s in low gravity (x {LowGravityVolume.All[0].gravityScale} gravity)");
            yield return Shot("24_low_gravity");

            Head("Camera (item 5)");
            yield return TpSettle(new Vector3(-10f, .1f, -40f), 0);
            float yaw0 = CameraRig.I.Yaw;
            Look(new Vector2(1f, 0)); yield return Wait(1f); Look(Vector2.zero); yield return Wait(.3f);
            float turned = Mathf.DeltaAngle(yaw0, CameraRig.I.Yaw);
            Ok(Mathf.Abs(turned) > 100f, $"right stick orbits: {turned:0} degrees in 1 s (setting {GameSettings.S.cameraSpeed} %, {CameraRig.StickDegPerSec} deg/s)");
            yield return Press(GamepadButton.RightStick); yield return Wait(.6f);
            Ok(Mathf.Abs(Mathf.DeltaAngle(CameraRig.I.Yaw, Player.transform.eulerAngles.y)) < 12f, $"right stick click recenters behind the player (off by {Mathf.DeltaAngle(CameraRig.I.Yaw, Player.transform.eulerAngles.y):0} degrees)");
            Look(new Vector2(1f, 0)); yield return Wait(.5f); Look(Vector2.zero);
            float off0 = Mathf.Abs(Mathf.DeltaAngle(CameraRig.I.Yaw, 0));
            Stick(StickFor(Vector3.forward)); yield return Wait(.2f);
            Stick(new Vector2(0, 1)); float runYaw = Mathf.Atan2(M.Velocity.x, M.Velocity.z) * Mathf.Rad2Deg; yield return Wait(4f); Stick(Vector2.zero);
            float runDir = Mathf.Atan2(M.Velocity.x, M.Velocity.z) * Mathf.Rad2Deg;
            Ok(true, $"auto-recenter while running: camera {Mathf.Abs(Mathf.DeltaAngle(CameraRig.I.Yaw, runDir)):0} degrees off the running direction after 4 s (was {off0:0})");
            // walls pull the camera in
            yield return TpSettle(new Vector3(0, .1f, 22.6f), 180);   // facing south, the garden wall right behind
            yield return Wait(.8f);
            var cp = CameraRig.I.Cam.transform.position;
            bool inWall = Physics.CheckSphere(cp, .1f, ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << 4) | (1 << 2)), QueryTriggerInteraction.Ignore);
            Ok(CameraRig.I.Distance < CameraRig.Radius - 1f && cp.z < 24f && !inWall, $"a wall behind: the camera pulls in to {CameraRig.I.Distance:0.0} m (free {CameraRig.Radius} m) and stays on the player's side (z {cp.z:0.0}, inside a wall: {inWall})");
            yield return Shot("25_camera_wall");

            Head("Switching and fitting (item 6)");
            yield return SwitchTo(0);
            yield return TpSettle(new Vector3(-10f, .1f, -45f), 0);
            Stick(StickFor(Vector3.forward)); yield return Wait(.8f);
            var camBefore = CameraRig.I.Cam.transform.position; var posBefore = Player.transform.position; var nameBefore = Player.Stats.displayName;
            yield return Press(GamepadButton.RightShoulder);
            float camJump = 0; for (int f = 0; f < 6; f++) { yield return null; }
            camJump = Vector3.Distance(camBefore, CameraRig.I.Cam.transform.position);
            Stick(Vector2.zero);
            Ok(Player.Stats.displayName != nameBefore && Vector3.Distance(posBefore, Player.transform.position) < 1.5f && camJump < 1.5f,
                $"RB while running: {nameBefore} -> {Player.Stats.displayName} on the same spot ({Vector3.Distance(posBefore, Player.transform.position):0.00} m), camera moved {camJump:0.00} m in 4 frames (running {Player.Stats.runSpeed} m/s)");
            yield return Press(GamepadButton.LeftShoulder); Ok(Player.Stats.displayName == nameBefore, "LB switches back");
            yield return Key(UnityEngine.InputSystem.Key.Digit3); Ok(Player.Stats.displayName == "Kira", "keyboard 3 picks Kira");
            yield return Key(UnityEngine.InputSystem.Key.Digit1); Ok(Player.Stats.displayName == "Dan", "keyboard 1 picks Dan");
            yield return SwitchTo(2);
            yield return TpSettle(new Vector3(0, .05f, 24.5f), 0);   // Kira inside the crawl gap
            yield return Press(GamepadButton.RightShoulder);
            Ok(Player.Stats.displayName == "Kira", $"no switch to a bigger hero inside the crawl gap (\"{Hud.LastMessage}\")");
            yield return TpSettle(new Vector3(0, .1f, 20f), 0);
            yield return SwitchTo(0); Ok(Player.Stats.displayName == "Dan", "out of the gap, Dan again");
            yield return TpSettle(new Vector3(0, .1f, 22.5f), 0);
            Stick(StickFor(Vector3.forward)); yield return Wait(1.2f); Stick(Vector2.zero);
            Ok(Player.transform.position.z < 24.1f, $"Dan doesn't fit through the crawl gap (stopped at z {Player.transform.position.z:0.0})");
            yield return TpSettle(new Vector3(-14f, .1f, 27.5f), 0);   // the push block 2.5 m ahead
            var block = Object.FindObjectsByType<PushBlock>().First(); float b0 = block.PushedDistance;
            Stick(StickFor(Vector3.forward)); yield return Wait(3f); Stick(Vector2.zero);
            float pushed = block.PushedDistance - b0;
            yield return SwitchTo(1);
            yield return TpSettle(new Vector3(-14f, .1f, block.transform.position.z - 2.5f), 0);
            float b1 = block.PushedDistance; Stick(StickFor(Vector3.forward)); yield return Wait(2f); Stick(Vector2.zero);
            Ok(pushed > 1f && block.PushedDistance - b1 < .01f, $"Dan pushes the heavy block ({pushed:0.0} m in 3 s); Abi can't ({block.PushedDistance - b1:0.00} m)");
            var boulder = Object.FindObjectsByType<Boulder>().First();
            yield return SwitchTo(0);
            yield return TpSettle(new Vector3(-46f, .1f, -38.5f), 180);
            Stick(StickFor(Vector3.back)); yield return Wait(1.5f); Stick(Vector2.zero);
            Ok(boulder.Touches > 0 && boulder.transform.position.z > -44.1f, $"the boulder doesn't move and says \"{Hud.LastMessage}\"");
            yield return Shot("26_boulder");

            Head("Health, combat, enemy, hazards, respawn (item 8)");
            Health.Refill(); yield return SwitchTo(0);
            var enemy = Object.FindObjectsByType<Enemy>().FirstOrDefault(e => !e.Dead);
            if (enemy)
            {
                yield return TpSettle(new Vector3(-30f, .1f, 20f), 270);
                yield return WaitFor(() => enemy.Chasing, 6f);
                Ok(enemy.Chasing, "the enemy chases the player when close");
                int h0 = Health.Hearts; int ignored0 = Health.Ignored;
                yield return WaitFor(() => Health.Hearts < h0, 6f);
                Ok(Health.Hearts == h0 - 1, $"touching it hurts: {h0} -> {Health.Hearts} hearts");
                yield return Shot("27_hurt");
                yield return Wait(.3f);
                Ok(Health.Invincible, $"brief invincibility after a hit ({Health.InvincibleSeconds} s)");
                // stand 1.3 m in front of it, facing it, and punch (X)
                for (int k = 0; k < 6 && !enemy.Dead; k++)
                {
                    var d = enemy.transform.position - Player.transform.position; d.y = 0; d.Normalize();
                    Tp(enemy.transform.position - d * 1.3f + Vector3.up * .05f, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg);
                    yield return null; yield return Press(GamepadButton.West); yield return Wait(.5f);
                }
                Ok(enemy.Dead && enemy.HitsTaken == 1, $"{Player.Stats.displayName}'s {Player.Stats.attackName.ToLower()} (X) defeats it in {enemy.HitsTaken} hit(s)");
                yield return Wait(.3f);
                var heal = Pickup.All.FirstOrDefault(p => p.kind == Pickup.Kind.Heal);
                int hb = Health.Hearts;
                if (heal) { yield return Go(heal.transform.position, .5f, 6f); yield return Wait(.2f); }
                Ok(Health.Hearts == Mathf.Min(Health.Max, hb + 1), $"a heal heart: {hb} -> {Health.Hearts}");
            }
            else Fail("no live enemy left for the combat check");
            yield return Wait(Health.InvincibleSeconds);
            int hs = Health.Hearts;
            yield return TpSettle(new Vector3(-36f, .1f, 24.6f), 0);
            Stick(StickFor(Vector3.forward) * .5f); yield return WaitFor(() => Health.Hearts < hs, 3f); Stick(Vector2.zero);
            Ok(Health.Hearts < hs, $"spikes hurt: {hs} -> {Health.Hearts}");
            Tp(new Vector3(-26f, .1f, 16f), 0);
            yield return Wait(Health.InvincibleSeconds);
            // the pit: fall out of the world -> last checkpoint, one heart less
            Health.Refill();
            SaveGame.Current.checkpoint = "A-west";
            int hp = Health.Hearts, r0 = Health.Respawns;
            yield return TpSettle(new Vector3(-36f, 2f, -12f), 270);
            yield return WaitFor(() => Health.Respawns > r0, 6f);
            var cpw = Checkpoint.All["A-west"].transform.position;
            Ok(Health.Respawns > r0 && Vector3.Distance(Player.transform.position, cpw) < 4f && Health.Hearts == hp - 1, $"falling into the pit: back at checkpoint A-west, {hp} -> {Health.Hearts} hearts");
            // out of hearts -> checkpoint, full hearts
            yield return Wait(Health.InvincibleSeconds + .2f);
            int r1 = Health.Respawns;
            for (int k = 0; k < 12 && Health.Respawns == r1; k++)
            {
                yield return TpSettle(new Vector3(-36f, .1f, 27f), 0);
                yield return Wait(Health.InvincibleSeconds + .1f);
            }
            Ok(Health.Respawns > r1 && Health.Hearts == Health.Max && Health.LastRespawn == "out of hearts", $"out of hearts: back at the checkpoint with full hearts ({Health.Hearts}/{Health.Max})");
            // heart upgrade
            int max0 = Health.Max;
            var up = Pickup.All.FirstOrDefault(p => p.kind == Pickup.Kind.HeartUpgrade);
            if (up)
            {
                yield return SwitchTo(1);
                yield return TpSettle(new Vector3(37f, 6.7f, 40.6f), 0);
                yield return Wait(.3f);
            }
            Ok(Health.Max == max0 + 1 && Health.Hearts == Health.Max && SaveGame.Current.maxHearts == Health.Max, $"the hidden heart upgrade raises the max: {max0} -> {Health.Max} (cap {Health.MaxHeartsCap})");
            yield return Shot("28_hearts");

            Head("Necklace (item 9)");
            Log($"  necklace beat: every {Necklace.FastPeriod} s next to a shard, {Necklace.SlowPeriod} s at {Necklace.Range} m or more; rumble {(GameSettings.S.rumble ? "on" : "off")} by default (light: 0.04-0.25 motor, 60 ms a beat)");

            Head("Data check: requirements without later abilities (rule 2)");
            foreach (var a in Game.World.areas) { var r = Game.World.RequirementReport(a.id); Ok(r.ok, r.detail); }

            Head("Save (item 10)");
            yield return EnsurePlaying();
            yield return Press(GamepadButton.Start);
            Ok(Game.I.State == Game.Mode.Paused && Time.timeScale == 0, "Menu pauses the game");
            yield return Shot("29_pause");
            // Settings / Controls / Credits pictures from the pause menu
            yield return Nav(1); yield return Press(GamepadButton.South); yield return Wait(.2f); yield return Shot("30_settings");
            Ok(Menu.I.Current.title == "Settings" && Menu.I.Current.rows.Count >= 10, "Settings: " + string.Join(", ", Menu.I.Current.rows.Select(r => r.label)));
            yield return Press(GamepadButton.East);
            yield return Nav(1); yield return Press(GamepadButton.South); yield return Wait(.2f); yield return Shot("31_controls_gamepad");
            Ok(Menu.I.Current.title == "Controls" && Controls.LastDevice == Controls.Device.Gamepad, "Controls shows the gamepad's buttons");
            yield return Key(UnityEngine.InputSystem.Key.DownArrow); yield return Wait(.2f);
            yield return Shot("32_controls_keyboard");
            Ok(Controls.LastDevice == Controls.Device.Keyboard, "after a key press it shows the keyboard's keys");
            yield return Press(GamepadButton.East);
            int total = SaveGame.Current.Total; var abil = string.Join(",", SaveGame.Current.abilities); var portals = string.Join(",", SaveGame.Current.portals); int maxH = SaveGame.Current.maxHearts;
            yield return Nav(1); yield return Press(GamepadButton.South); yield return Wait(.3f);   // Quit to title
            Ok(Game.I.State == Game.Mode.Title, "Quit to title");
            var json = File.ReadAllText(SaveGame.FilePath);
            Log("  save.json: " + json.Replace("\n", " ").Replace("  ", ""));
            yield return Shot("33_title_with_save");
            Ok(Menu.I.Current.sel == 0, "Continue is offered and selected");
            yield return Press(GamepadButton.South); yield return Wait(.8f);
            var w = SaveGame.Current.World(Game.World.worldId);
            bool noRespawned = !Pickup.All.Any(p => p.kind == Pickup.Kind.Shard && w.shards.Contains(p.id));
            Ok(Game.I.State == Game.Mode.Playing && SaveGame.Current.Total == total && string.Join(",", SaveGame.Current.abilities) == abil && string.Join(",", SaveGame.Current.portals) == portals && Health.Max == maxH && noRespawned && Party.I.Heroes[1].Motor.CanDoubleJump,
                $"Continue restores {SaveGame.Current.Total} shards (collected ones stay collected), abilities [{abil}], portals [{portals}], max hearts {Health.Max}, at checkpoint {SaveGame.Current.checkpoint}");
            yield return Press(GamepadButton.Start); yield return Nav(3); yield return Press(GamepadButton.South); yield return Wait(.3f);
            yield return Nav(1); yield return Press(GamepadButton.South); yield return Wait(.2f);
            Ok(Menu.I.Current.title == "Start a new game?", "New game with a save asks first");
            yield return Shot("34_new_game_confirm");
            yield return Nav(1); yield return Press(GamepadButton.South); yield return Wait(.8f);
            Ok(Game.I.State == Game.Mode.Playing && SaveGame.Current.Total == 0 && SaveGame.Current.abilities.Count == 0 && Health.Max == Health.StartHearts && Pickup.All.Count(p => p.kind == Pickup.Kind.Shard) == Game.World.shards.Count,
                "\"Yes, start over\" clears the save: 0 shards, no abilities, 3 hearts, every shard back");
            yield return Press(GamepadButton.Start); yield return Nav(3); yield return Press(GamepadButton.South); yield return Wait(.3f);
            yield return Press(GamepadButton.DpadDown); yield return Press(GamepadButton.DpadDown); yield return Press(GamepadButton.DpadDown); yield return Press(GamepadButton.DpadDown);
            yield return Press(GamepadButton.South); yield return Wait(.2f);
            yield return Shot("35_credits");
            Ok(Menu.I.Current.title == "Credits", "Credits page");
            yield return Press(GamepadButton.East);
            yield return Press(GamepadButton.DpadUp); yield return Press(GamepadButton.DpadUp); yield return Press(GamepadButton.DpadUp); yield return Press(GamepadButton.DpadUp);
        }

        // diagnostics: run off the plateau edge, press jump `late` s later, log the motor every frame
        IEnumerator CoyoteDiag()
        {
            yield return EnsurePlaying();
            yield return SwitchTo(0);
            foreach (var late in new[] { .03f, .07f, .1f })
            {
                yield return TpSettle(new Vector3(13.5f, 3.0f, -34.2f), 0);
                Stick(StickFor(Vector3.forward) * .6f);
                yield return WaitFor(() => !M.Grounded, 3f);
                float t0 = Time.time; bool pressed = false; int n = 0;
                while (Time.time - t0 < late + .1f)
                {
                    if (!pressed && Time.time - t0 >= late) { Btn(GamepadButton.South, true); pressed = true; }
                    if (n++ % 2 == 0) Log($"  t {Time.time - t0:0.000} y {Player.transform.position.y:0.00} z {Player.transform.position.z:0.00} grounded {M.Grounded} vy {M.Velocity.y:0.0} mode {M.Mode} jumps {M.JumpsThisAir}");
                    yield return null;
                }
                Btn(GamepadButton.South, false); Stick(Vector2.zero);
                yield return WaitFor(() => M.Grounded, 3f);
            }
        }

        IEnumerator Nav(int down) { for (int i = 0; i < down; i++) yield return Press(GamepadButton.DpadDown); }

        IEnumerator Perf()
        {
            yield return EnsurePlaying();
            Head("Frame rate (off-screen 1920 x 1080, batch mode: no present to a window)");
            yield return SwitchTo(0);
            yield return TpSettle(new Vector3(0, .1f, -48f), 0);
            Application.targetFrameRate = -1;   // uncapped for the measurement (a player's build runs at vsync)
            int frames = 0; float t0 = Time.realtimeSinceStartup, worst = 0;
            var path = new[] { new Vector3(0, 0, -20), new Vector3(20, 0, 15), new Vector3(-10, 0, 40), new Vector3(-20, 0, 0) };
            int wp = 0;
            while (Time.realtimeSinceStartup - t0 < 12f)
            {
                var d = path[wp] - Player.transform.position; d.y = 0; if (d.magnitude < 2f) wp = (wp + 1) % path.Length;
                Stick(StickFor(d.normalized)); frames++; worst = Mathf.Max(worst, Time.unscaledDeltaTime); yield return null;
            }
            Stick(Vector2.zero);
            float fps = frames / (Time.realtimeSinceStartup - t0);
            Application.targetFrameRate = 144;
            Ok(fps > 60f, $"{fps:0} fps average running round Area A with both followers, slowest frame {worst * 1000:0} ms ({SystemInfo.graphicsDeviceName})");
            yield return Shot("40_perf_view");
        }

        IEnumerator MuteCheck()
        {
            Head("Mute check: 10 s of play with sounds");
            yield return EnsurePlaying();
            for (int i = 0; i < 10; i++) { yield return Press(GamepadButton.South); yield return Press(GamepadButton.West); yield return Wait(.8f); }
            Log($"  {Sfx.Played} sounds played; " + TestRun.Report());
        }
    }
}
