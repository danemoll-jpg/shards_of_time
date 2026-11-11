using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace ShardsOfTime
{
    // Fix 7.2: never stuck. `fix72` = the never-stuck sweep over every world (`stuck`), Bethlehem's well from a jump and a fall with
    // each hero (`well`), and the pause menu's "Return to checkpoint" + the stuck hint (in Bethlehem and the vortex).
    public sealed partial class ExeCheck
    {
        // moves the story teaches by Bethlehem (a spot that needs one to get in is tested with it); anything else is a later move
        static readonly HashSet<string> TaughtSoFar = new HashSet<string> { Abilities.GroundPound, Abilities.DoubleJump, Abilities.Pounce, Abilities.ChargedPunch, Abilities.MuscleMan };   // Round 8: the charged punch, Muscle Man

        IEnumerator Fix72()
        {
            yield return StuckAll();
            yield return WellCheck();
            yield return ResetCheck();
        }

        IEnumerator StuckAll()
        {
            yield return EnsurePlaying(false);
            yield return StuckWorld(WorldData.IceAge, null);
            yield return StuckWorld(WorldData.Egypt, Egypt.Start);
            yield return StuckWorld(WorldData.Bethlehem, Bethlehem.Start);
            yield return StuckWorld(WorldData.Medieval, Medieval.Start);   // Round 8
        }

        IEnumerator StuckMedieval() { yield return EnsurePlaying(false); yield return StuckWorld(WorldData.Medieval, Medieval.Start); }   // Round 8

        IEnumerator WellOnly()
        {
            yield return EnsurePlaying(false);
            yield return ToWorld(WorldData.Bethlehem, Bethlehem.Start);
            yield return WellCheck();
            yield return ResetCheck();
        }

        IEnumerator ToWorld(string world, string cp)
        {
            if (Game.World.worldId == world) yield break;
            string flag = world == WorldData.Egypt ? "EG-arrived" : world == WorldData.Bethlehem ? "BE-arrived" : world == WorldData.Medieval ? "ME-arrived" : null;
            if (flag != null && !SaveGame.Current.World(world).opened.Contains(flag)) SaveGame.Current.World(world).opened.Add(flag);
            SaveGame.Current.checkpoint = cp; yield return StartWorld(world); yield return WaitFor(() => !Game.I.Busy, 10f);
            yield return Wait(1f);
        }

        // ---------------------------------------------------------------- item 2: every listed spot, every hero that can get in
        IEnumerator StuckWorld(string world, string cp)
        {
            if (cp != null) yield return ToWorld(world, cp);
            var wd = Game.World;
            Head($"Fix 7.2, item 2: never stuck in {wd.displayName} ({wd.lowSpots.Count} spots listed in its world data)");
            Ok(wd.lowSpots.Count > 0, $"{wd.displayName} lists its low / enclosed spots");
            // every water volume in the world is on the list (so a new pool can't be forgotten)
            foreach (var wv in WaterVolume.All.ToList())
            {
                var b = wv.Box;
                bool listed = wd.lowSpots.Any(s => s.kind == "water" && s.drop.x > b.min.x && s.drop.x < b.max.x && s.drop.z > b.min.z && s.drop.z < b.max.z);
                Ok(listed, $"water '{wv.name}' (x {b.min.x:0}..{b.max.x:0}, z {b.min.z:0}..{b.max.z:0}) has a spot on the list");
            }
            var had = new List<string>(SaveGame.Current.abilities);
            Log($"  (moves held: [{string.Join(", ", had)}]; each spot is tried with only these plus what it takes to get in)");
            bool firstShot = true;
            foreach (var s in wd.lowSpots)
            {
                if (!string.IsNullOrEmpty(s.needs) && !TaughtSoFar.Contains(s.needs) && !had.Contains(s.needs))
                {
                    Log($"  closed {s.id} ({s.note}): needs {s.needs} to get in, a later move: not enterable yet");
                    continue;
                }
                if (!string.IsNullOrEmpty(s.needs) && TaughtSoFar.Contains(s.needs) && !SaveGame.Current.abilities.Contains(s.needs)) SaveGame.Current.abilities.Add(s.needs);
                if (!string.IsNullOrEmpty(s.opens)) { var cap = CrackedFloor.All.FirstOrDefault(c => c.id == s.opens); if (cap) cap.Pound(Player); }
                // Round 8: a cracked wall, a boulder or a portcullis in front of it: opened first (as the charged punch / Muscle Man would)
                if (!string.IsNullOrEmpty(s.opens)) { var rw = RockWall.All.FirstOrDefault(r => r.id == s.opens); if (rw) rw.Punch(P.Heroes[0], true); }
                if (!string.IsNullOrEmpty(s.opens)) { var mj = MuscleJob.All.FirstOrDefault(j => j.id == s.opens); if (mj) mj.OpenNow(); }
                for (int i = 0; i < P.Heroes.Count; i++)
                {
                    string who = P.Heroes[i].Stats.displayName;
                    if (!string.IsNullOrEmpty(s.heroes) && !s.heroes.Contains(who)) continue;
                    yield return SwitchTo(i);
                    Health.Refill();
                    if (!string.IsNullOrEmpty(s.checkpoint) && Checkpoint.All.ContainsKey(s.checkpoint)) SaveGame.Current.checkpoint = s.checkpoint;
                    var to = s.exit - s.drop;
                    Tp(s.drop, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg);
                    yield return Wait(.8f);
                    if (firstShot || s.kind != "enclosed" && i == 0) { yield return Shot($"72_{s.id}_{who}_in"); firstShot = false; }
                    float t0 = Time.time;
                    yield return Escape(s, 20f);
                    Ok(escaped, $"{s.id} ({s.note}): {who} {(escaped ? $"out in {Time.time - t0:0.0} s" : "NOT out in 20 s")} at {Fmt(Player.transform.position)}{(escapedBy != "" ? " (" + escapedBy + ")" : "")}");
                    if (!escaped) yield return Shot($"72_{s.id}_{who}_stuck");
                }
                SaveGame.Current.abilities.Clear(); SaveGame.Current.abilities.AddRange(had);
            }
            Health.Refill();
        }

        bool escaped; string escapedBy = "";
        float RimAt(Vector3 exit)
        {
            int mask = ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << Hero.EnemyLayer) | (1 << 4) | (1 << 2));
            return Physics.Raycast(exit + Vector3.up * 2.5f, Vector3.down, out var hit, 8f, mask, QueryTriggerInteraction.Ignore) ? hit.point.y : exit.y;
        }

        // the bot gets out as a player would: the stick toward the way out (via, then exit), Jump in a rhythm (leaps out of water, climbs
        // ropes and holes, hops steps), a turn aside when it stops making progress. Pad only.
        IEnumerator Escape(LowSpot s, float limit)
        {
            escaped = false; escapedBy = "";
            float end = Time.time + limit, cyc = 0, lastT = Time.time, dodge = 0, dodgeUntil = 0, rim = RimAt(s.exit);
            bool viaDone = s.via == Vector3.zero; var prev = Player.transform.position; var lastP = prev; int climbs = M.ClimbOuts;
            while (Time.time < end)
            {
                var p = Player.transform.position;
                if ((p - prev).magnitude > 12f) { escapedBy = "sent back to the checkpoint"; escaped = true; break; }
                prev = p;
                var fd = p - s.drop; fd.y = 0;
                bool sand = Quicksand.All.Any(q => { var b = q.Box; b.Expand(new Vector3(0, 1.2f, 0)); return b.Contains(p); });
                if (M.Grounded && M.Mode != MoveMode.Swim && !M.Climbing && fd.magnitude >= s.radius && p.y >= rim - .6f && !sand)
                { escaped = true; if (M.ClimbOuts > climbs) escapedBy = "climbed out"; break; }
                var tgt = viaDone ? s.exit : s.via; var d = tgt - p; d.y = 0;
                if (!viaDone && d.magnitude < 1.5f) viaDone = true;
                var dir = d.sqrMagnitude > .01f ? d.normalized : Vector3.forward;
                if (Time.time < dodgeUntil) dir = Quaternion.Euler(0, dodge, 0) * dir;
                Stick(StickFor(dir));
                cyc += Time.deltaTime; float c = cyc % 1.1f;
                Btn(GamepadButton.South, c < .4f || (c > .55f && c < .8f));
                if (Time.time - lastT > 2.5f)
                {
                    if ((p - lastP).magnitude < .8f) { dodge = dodge == 0 ? 60 : -dodge; dodgeUntil = Time.time + 1.2f; }
                    lastP = p; lastT = Time.time;
                }
                yield return null;
            }
            Btn(GamepadButton.South, false); Stick(Vector2.zero);
            yield return Wait(.3f);
        }

        // ---------------------------------------------------------------- item 1: Bethlehem's well, each hero, from a jump and a fall
        static readonly Vector3 WellAt = new Vector3(108, 0, 72);
        float FromWell => new Vector2(Player.transform.position.x - WellAt.x, Player.transform.position.z - WellAt.z).magnitude;

        IEnumerator WellCheck()
        {
            yield return ToWorld(WorldData.Bethlehem, Bethlehem.Start);
            Head("Fix 7.2, item 1: out of Bethlehem's well (a rope and bucket inside; Jump or swimming to the wall climbs out)");
            Ok(WellExit.All.Count == 1, $"the well has its way out ({WellExit.All.Count} well exit)");
            for (int i = 0; i < P.Heroes.Count; i++)
            {
                yield return SwitchTo(i); Health.Refill();
                string who = Player.Stats.displayName;
                // a jump: run at the rim from the south side and jump over it
                yield return TpSettle(Bethlehem.OnGround(108, 79.5f), 180);
                yield return JumpTo(WellAt, () => FromWell < 3.9f, .7f, .45f);
                yield return WaitFor(() => M.Mode == MoveMode.Swim, 4f);
                if (M.Mode == MoveMode.Swim) Ok(true, $"{who} jumped over the rim into the well (swimming at {Fmt(Player.transform.position)})");
                else
                {
                    // the rim is 1.8 m: only Abi's jump (2.1 m) clears it from the ground; the others can only fall in (a bounce, a pounce)
                    Log($"  ({who} can't jump the 1.8 m rim from the ground (jump {Player.Stats.jumpHeight:0.0} m, last {M.LastJumpHeight:0.0} m): dropped in instead)");
                    Ok(who != "Abi", $"{who}: only Abi should clear the rim with a jump");
                    Tp(WellAt + Vector3.up * 3f, 180); yield return null; yield return null; yield return WaitFor(() => M.Mode == MoveMode.Swim && Player.transform.position.y < -1f, 4f);
                }
                yield return Wait(1f);
                if (i == 0) yield return Shot($"72_well_{who}_in_after_jump");
                int c0 = M.WellClimbs;
                yield return Hold(GamepadButton.South, 1.5f);   // Jump (held: swims up, then climbs at the top)
                yield return WaitFor(() => M.WellClimbs > c0 && !M.Climbing && M.Grounded, 6f);
                Ok(M.WellClimbs > c0 && M.Grounded && FromWell > 3.5f, $"{who}: Jump climbs the rope out of the well ({Fmt(Player.transform.position)}, {FromWell:0.0} m from the middle; climbs {M.WellClimbs - c0}, mode {M.Mode})");
                if (i == 0) yield return Shot($"72_well_{who}_out");
                // a fall: dropped in from above the shaft, then just swims to the wall (no Jump)
                Tp(WellAt + Vector3.up * 3f, 0); yield return null; yield return null;
                yield return WaitFor(() => M.Mode == MoveMode.Swim && Player.transform.position.y < -1f, 4f);
                Ok(M.Mode == MoveMode.Swim, $"{who} fell into the well (swimming at {Fmt(Player.transform.position)})");
                yield return Wait(1.5f);
                if (i == 2) yield return Shot($"72_well_{who}_in_after_fall");
                c0 = M.WellClimbs; float t0 = Time.time;
                Stick(StickFor(Vector3.forward));
                yield return WaitFor(() => M.WellClimbs > c0 && !M.Climbing && M.Grounded, 6f);
                Stick(Vector2.zero);
                Ok(M.WellClimbs > c0 && M.Grounded && FromWell > 3.5f, $"{who}: swimming to the wall climbs out too, in {Time.time - t0:0.0} s ({Fmt(Player.transform.position)}; climbs {M.WellClimbs - c0}, mode {M.Mode})");
                yield return Wait(.5f);
            }
            Log($"  (the well's hint: \"{Hud.LastMessage}\")");
        }

        // ---------------------------------------------------------------- item 3: Return to checkpoint + the stuck hint
        IEnumerator PauseAndReturn()
        {
            yield return Press(GamepadButton.Start);
            yield return Wait(.4f);
            var page = Menu.I.Current;
            int row = page != null ? page.rows.FindIndex(r => r.label == "Return to checkpoint") : -1;
            Ok(Game.I.State == Game.Mode.Paused && row >= 0, $"the pause menu has \"Return to checkpoint\" (row {row + 1}: {(page != null ? string.Join(" / ", page.rows.Select(r => r.label)) : "no menu")})");
            if (row < 0) yield break;
            yield return Shot("72_pause_menu");
            for (int k = 0; k < row; k++) { yield return Press(GamepadButton.DpadDown); yield return Wait(.15f); }
            yield return Press(GamepadButton.South);
            yield return Wait(.2f);
        }

        IEnumerator ResetCheck()
        {
            yield return ToWorld(WorldData.Bethlehem, Bethlehem.Start);
            Head("Fix 7.2, item 3: Pause > Return to checkpoint, and the stuck hint");
            yield return SwitchTo(0); Health.Refill();
            // in Bethlehem: from the far hills back to the hills' checkpoint
            SaveGame.Current.checkpoint = "BE-hills";
            var cp = Checkpoint.All["BE-hills"].transform.position;
            yield return TpSettle(Bethlehem.OnGround(160, 60), 0);
            int hearts = Health.Hearts, total = SaveGame.Current.Total, n0 = Game.I.CheckpointReturns;
            yield return PauseAndReturn();
            yield return WaitFor(() => Game.I.CheckpointReturns > n0 && !Game.I.Busy, 4f);
            yield return Wait(.5f);
            float d = Vector3.Distance(Player.transform.position, cp);
            Ok(Game.I.CheckpointReturns > n0 && d < 6f && Game.I.State == Game.Mode.Playing, $"back at checkpoint BE-hills ({d:0.0} m from its flag), playing");
            Ok(Health.Hearts == hearts && SaveGame.Current.Total == total, $"no heart lost ({Health.Hearts} of {Health.Max}, was {hearts}), shards kept ({SaveGame.Current.Total})");
            Ok(P.Heroes.All(h => Vector3.Distance(h.transform.position, Player.transform.position) < 5f), $"the party came along: {Followers()}");
            yield return Shot("72_returned_to_checkpoint");
            // the stuck hint: pushing into the barn's east wall for ~16 s
            int h0 = Game.I.StuckHints;
            yield return TpSettle(new Vector3(7f, .3f, 34), 270);
            Stick(StickFor(Vector3.left));
            yield return WaitFor(() => Game.I.StuckHints > h0, Game.StuckSeconds + 4f);
            Stick(Vector2.zero);
            Ok(Game.I.StuckHints > h0 && Hud.LastMessage.StartsWith("Stuck?"), $"pushing against a wall for {Game.StuckSeconds:0} s shows the hint: \"{Hud.LastMessage}\"");
            yield return Wait(.2f);
            yield return Shot("72_stuck_hint");
            // the vortex: Return to checkpoint works there too
            SaveGame.Current.checkpoint = Vortex.Checkpoint; Checkpoint.RespawnParty(); CameraRig.I.SnapBehind();
            yield return Wait(.6f);
            var vx = Player.transform.position;
            yield return Go(VPath(Yaw("Egypt"), 12), .8f, 8f);
            n0 = Game.I.CheckpointReturns;
            yield return PauseAndReturn();
            yield return WaitFor(() => Game.I.CheckpointReturns > n0 && !Game.I.Busy, 4f);
            yield return Wait(.5f);
            Ok(Game.I.CheckpointReturns > n0 && Vector3.Distance(Player.transform.position, vx) < 4f && Vortex.Inside(Player.transform.position), $"in the vortex too: back at its start ({Vector3.Distance(Player.transform.position, vx):0.0} m)");
            yield return Shot("72_vortex_returned");
        }
    }
}
