using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace ShardsOfTime
{
    // Round 11: Victorian Steampunk as a player plays it, with the virtual pad, from the vortex (a save with the first five worlds done to their
    // story shards, each one's shards counted that rule 2 counts on arrival here, Muscle Man along, no moves learned on the way):
    //  steampunk: the path -> SteampunkArrival -> the belt run (the bot rides it, hops the stampers) -> MuscleManHero -> the factory (mechanical
    //    Kira winds the lift, the gear door and the bridge, climbs a riveted pillar; Muscle Man bends the cage's bars) -> the yard and the streets
    //    (the day look; the Violinist teaches Abi the wall jump; up the narrow alley by wall jumps; a carriage ride and a bump; the horse gag;
    //    the gear gate; Muscle Man's boost) -> the necklace -> the great clock: ClockShard -> the vortex (Home 6 of 8, Tomorrowland to be
    //    continued) -> the Medieval village's chimney with the wall jump (an earlier world's gate).
    //  stucksteam (rule 12), fix81steam (floors, invisible walls, moving platforms, reach), perfsteam (frame rate), steamdiag (looks), walljump.
    public sealed partial class ExeCheck
    {
        int ShardsSP => SaveGame.Current.World(WorldData.Steampunk).shards.Count;
        Vector3 SpotSP(string id) => Steampunk.ShardPos(WorldData.Load(WorldData.Steampunk).shards.First(s => s.id == id).position);
        IEnumerator SShard(string id, float tol = .5f, float timeout = 15f)
        {
            if (Has(id)) yield break;
            yield return GoF(SpotSP(id), tol, timeout);
            yield return WaitFor(() => Has(id), 1.5f);
            Ok(Has(id), $"{id} ({Game.World.shards.First(s => s.id == id).note}) at {Fmt(Player.transform.position)}; {ShardsSP} here, {SaveGame.Current.Total} in all");
        }

        IEnumerator SteamSetup()
        {
            yield return EnsurePlaying(false);
            var sv = WorldData.Load(WorldData.Steampunk).areas[0].firstVisit;
            foreach (var wid in new[] { WorldData.IceAge, WorldData.Egypt, WorldData.Bethlehem, WorldData.Medieval, WorldData.Pirate })
            {
                var prog = SaveGame.Current.World(wid); prog.storyShard = true; prog.necklaceAwake = true;
                var wd = WorldData.Load(wid); var fv = wd.areas[0].firstVisit.Concat(sv).ToArray();
                foreach (var sp in wd.shards) if (sp.requires.All(fv.Contains) && !prog.shards.Contains(sp.id)) prog.shards.Add(sp.id);
                if (!SaveGame.Current.worldsReached.Contains(wid)) SaveGame.Current.worldsReached.Add(wid);
            }
            foreach (var f in new[] { (WorldData.Egypt, "EG-arrived"), (WorldData.Bethlehem, "BE-arrived"), (WorldData.Medieval, "ME-arrived"), (WorldData.Pirate, "PS-arrived"), (WorldData.Pirate, Pirate.OverboardFlag) })
                if (!SaveGame.Current.World(f.Item1).opened.Contains(f.Item2)) SaveGame.Current.World(f.Item1).opened.Add(f.Item2);
            foreach (var p in new[] { "IA-vortex", "EG-vortex-gate", "BE-vortex-gate", "ME-vortex-gate", "PS-vortex-gate" }) if (!SaveGame.Current.portals.Contains(p)) SaveGame.Current.portals.Add(p);
            if (!SaveGame.Current.abilities.Contains(Abilities.MuscleMan)) SaveGame.Current.abilities.Add(Abilities.MuscleMan);
            Log($"  (set up: {SaveGame.Current.Total} shards in all from the first five worlds (what rule 2 counts on arrival here), Muscle Man along, no moves learned)");
            SaveGame.Current.checkpoint = Vortex.Checkpoint; Checkpoint.RespawnParty(); CameraRig.I.SnapBehind();
            if (!P.Companion) P.AddCompanion(Player.transform.position + Vector3.back * 3f, Quaternion.identity);
            yield return Wait(.6f);
        }

        // straight into the world past the press (stuck, fix81, perf, diag)
        IEnumerator SteamEnter(string cp)
        {
            var pr = SaveGame.Current.World(WorldData.Steampunk);
            foreach (var f in new[] { Steampunk.ArrivedFlag, Steampunk.PressFlag }) if (!pr.opened.Contains(f)) pr.opened.Add(f);
            if (!SaveGame.Current.Has(Abilities.MuscleMan)) SaveGame.Current.abilities.Add(Abilities.MuscleMan);
            SaveGame.Current.checkpoint = cp; yield return StartWorld(WorldData.Steampunk); yield return WaitFor(() => !Game.I.Busy, 10f);
            if (!P.Companion) P.AddCompanion(Player.transform.position + Vector3.back * 3f, Quaternion.identity);
            yield return Wait(.6f);
        }

        // the belt run as a player: hang back a little against the belt, hop now and then (the stampers bonk, the jets hop): until the press
        IEnumerator BeltRunBot(bool shots)
        {
            float t0 = Time.time; int bonk0 = Stamper.BonksAll, hop0 = SteamJet.HopsAll; int hearts = Health.Hearts; float minY = 99, maxX = 0; bool shot1 = false, shot2 = false;
            while (!Game.InCutscene && !Game.I.Busy && Time.time - t0 < 80f)
            {
                var p = Player.transform.position; minY = Mathf.Min(minY, p.y); maxX = Mathf.Max(maxX, Mathf.Abs(p.x));
                Stick(Steampunk.BeltGateOpen ? StickFor(Vector3.forward) * .7f : StickFor(Vector3.back) * (p.z > 70f ? .3f : .12f));   // hang back until the gate opens, then on to the press
                float c = (Time.time - t0) % 1.6f; Btn(JumpBtn, c < .25f);
                if (shots && !shot1 && Time.time - t0 > 6f) { shot1 = true; yield return Shot("410_belt_run"); }
                if (shots && !shot2 && Steampunk.BeltGateOpen) { shot2 = true; yield return Shot("411_belt_gate_open"); }
                yield return null;
            }
            Stick(Vector2.zero); Btn(JumpBtn, false);
            float took = Time.time - t0;
            Ok(Cutscenes.Playing == "MuscleManHero" || Game.I.PressScenes > 0, $"the belt carried them to the press in {took:0.0} s (gate after {Steampunk.BeltGateAfter:0} s; stamper bonks {Stamper.BonksAll - bonk0}, steam hops {SteamJet.HopsAll - hop0})");
            Ok(took >= 29f && took <= 45f, $"the belt run lasts 30-40 s ({took:0.0} s)");
            Ok(minY > Steampunk.BeltTop - 1f && maxX < 2.6f && Health.Hearts == hearts, $"nobody left the belt (lowest {minY:0.0}, widest x {maxX:0.0}); no heart lost ({Health.Hearts})");
        }

        // a wall jump up a narrow shaft: find the nearest wall, jump at it; on each touch: Jump again, the stick away from that wall
        IEnumerator WallJumpUp(Vector3 bottom, float topY, Vector3 exitDir, float limit = 14f)
        {
            yield return TpSettle(bottom + Vector3.up * .1f, 0);
            Vector3 dir = Vector3.forward; float best = 99;
            foreach (var d in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
                if (Physics.Raycast(M.Center, d, out var h, 3.5f, ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << Hero.EnemyLayer) | (1 << 4) | (1 << 2)), QueryTriggerInteraction.Ignore) && h.distance < best) { best = h.distance; dir = d; }
            int w0 = CharacterMotor.WallJumpsAll; float end = Time.time + limit, cool = 0; float topSeen = 0;
            Stick(StickFor(dir)); Btn(JumpBtn, true); yield return null; yield return null;
            while (Time.time < end)
            {
                var p = Player.transform.position; topSeen = Mathf.Max(topSeen, p.y);
                if (p.y > topY) { Stick(StickFor(exitDir)); Btn(JumpBtn, true); if (M.Grounded && p.y > topY - .3f) break; yield return null; continue; }
                if (!M.Grounded && M.TouchingWall && Time.time > cool)
                {
                    var n = M.WallNormal; Btn(JumpBtn, false); yield return null;
                    Btn(JumpBtn, true); Stick(StickFor(n)); cool = Time.time + .18f;
                }
                if (M.Grounded && Time.time > cool) { Btn(JumpBtn, false); yield return null; Btn(JumpBtn, true); Stick(StickFor(dir)); cool = Time.time + .3f; }
                yield return null;
            }
            Btn(JumpBtn, false); Stick(Vector2.zero); yield return Wait(.6f);
            Log($"  (wall jumps {CharacterMotor.WallJumpsAll - w0}, highest {topSeen:0.0} m, now at {Fmt(Player.transform.position)})");
        }

        IEnumerator WindAt(Vector3 socket, string id)
        {
            yield return SwitchTo(2);
            yield return TpSettle(socket + new Vector3(0, .1f, -2f), 0);
            yield return Go(socket, .3f, 5f, .5f);
            yield return WaitFor(() => SaveGame.Current.World(Game.World.worldId).opened.Contains(id), 5f); yield return Wait(2.5f);
            Ok(SaveGame.Current.World(Game.World.worldId).opened.Contains(id), $"mechanical Kira winds up {id} from its socket ({WindUpMachine.Winds} wound so far)");
        }

        IEnumerator MMJob(Vector3 from, float yaw, string id)
        {
            yield return WaitFor(() => !Game.I.Busy && !Game.InCutscene, 20f);   // (Round 12: a necklace waking mid-test)
            yield return SwitchTo(0);
            foreach (var e in Enemy.All) if (e && !e.Dead && Vector3.Distance(e.transform.position, from) < 12f) e.Stun(10f);   // (Round 12: a cleanbot's bump mid-press)
            yield return TpSettle(from, yaw); yield return Wait(1.2f);
            yield return Press(AttackBtn); yield return WaitFor(() => Game.I.Busy, 3f); yield return WaitFor(() => !Game.I.Busy, 9f);
            bool done = SaveGame.Current.World(Game.World.worldId).opened.Contains(id);
            Ok(done, $"{id}: Muscle Man {(done ? "did it" : "did NOT do it")}");
            yield return Shot("44x_" + id);
        }

        IEnumerator SteampunkOnly()
        {
            yield return SteamSetup();
            Cutscenes.Started -= OnPlayed; Cutscenes.Started += OnPlayed;
            float t0 = Time.time;
            Head("Round 11, item 4: the vortex's Victorian Steampunk path, SteampunkArrival");
            var door = VortexDoor.All.FirstOrDefault(d => d.info.id == "Steampunk"); var tl = VortexDoor.All.FirstOrDefault(d => d.info.id == "Tomorrowland");
            Ok(door && !door.Blocked, $"the Victorian Steampunk path is open after the green shard ({(door ? door.label.text.Replace("\n", " ") : "none")})");
            Ok(tl && tl.Blocked, $"Tomorrowland is there and closed until the teal shard ({(tl ? tl.label.text.Replace("\n", " ") : "none")}); {VortexDoor.All.Count} paths");
            float yaw = Yaw("Steampunk");
            yield return Go(VPath(yaw, 4), .8f); yield return Go(VPath(yaw, 19), .6f);
            yield return Go(VPath(yaw, 24), .5f, 6f, 1f, () => Game.I.Busy);
            yield return WaitFor(() => Cutscenes.Playing == "SteampunkArrival", 8f);
            Ok(Game.InSteampunk && Cutscenes.Playing == "SteampunkArrival", $"the path loads Victorian Steampunk and plays its arrival (world {Game.World.worldId}, cutscene {Cutscenes.Playing ?? "none"})");
            foreach (var (t, n) in new[] { (1.0f, "400a_gear_and_streak"), (5.5f, "400b_teal_spiral"), (10.5f, "400c_over_the_domes"), (13.6f, "400d_onto_the_chain"), (16.5f, "400e_in_the_bucket"), (20.3f, "400f_tipped_out"), (22.4f, "400g_on_the_belt") })
            { yield return WaitFor(() => Cutscenes.Director && Cutscenes.Director.time >= t || Cutscenes.Playing == null, 40f); yield return Shot(n); }
            yield return WaitFor(() => !Game.InCutscene, 30f); yield return Wait(.5f);

            Head("Round 11, item 5: the belt run, then MuscleManHero");
            Ok(Steampunk.BeltRunning && Player.transform.position.y > 2.8f, $"on the belt, carried toward the press ({Fmt(Player.transform.position)}; Muscle Man {(P.Companion ? "along" : "missing")})");
            Ok(Music.Track == "steampunk", $"Dan's steampunk song plays ({Music.Track}; {(Music.HasClip ? "loaded" : "MISSING")})");
            yield return BeltRunBot(true);
            yield return WaitFor(() => Cutscenes.Playing == "MuscleManHero", 6f);
            foreach (var (t, n) in new[] { (1.5f, "412a_tumbling"), (5f, "412b_here_it_comes"), (6.9f, "412c_heads_down"), (8.4f, "412d_he_holds_it"), (10.2f, "412e_heave"), (12.5f, "412f_through_the_wall") })
            { yield return WaitFor(() => Cutscenes.Director && Cutscenes.Director.time >= t || Cutscenes.Playing == null, 20f); yield return Shot(n); }
            yield return WaitFor(() => !Game.I.Busy && !Game.InCutscene, 20f); yield return Wait(1f);
            Ok(Steampunk.PressGone && Game.Playing && Vector3.Distance(Player.transform.position, Steampunk.AfterPress) < 3f, $"the press is gone, play starts on the factory floor ({Fmt(Player.transform.position)})");
            var kira = P.Heroes.First(h => h.Stats.displayName == "Kira");
            Ok(kira.Form && kira.Form.id == KiraForm.Mechanical, $"Kira is mechanical (model {(kira.Model ? kira.Model.name : "graybox")}, abilities [{string.Join(", ", kira.Form.abilities)}]); Dan wears {P.Heroes[0].Model?.name}; Muscle Man {P.Companion?.Model?.name}");
            yield return Shot("413_factory_player_view");
            Log($"  HUD: {Hud.TotalShown} / {Hud.UnderTotal}");

            Head("Round 11, items 3 and 6: the factory: catwalks, mechanical Kira's wind-up and magnet paws");
            yield return SwitchTo(0);
            yield return TpSettle(new Vector3(32f, .1f, 24f), 180); yield return Go(new Vector3(32f, 7f, 9f), .6f, 10f);
            Ok(Player.transform.position.y > 6.8f, $"up the east stair onto the catwalk ({Fmt(Player.transform.position)})");
            yield return Go(new Vector3(34.7f, 7f, 11.2f), .5f, 6f); yield return Go(new Vector3(34.7f, 7f, 36f), .6f, 10f); yield return SShard("F13"); yield return Shot("420_catwalk");
            yield return WindAt(new Vector3(34.75f, 7f, 32f), "SP-bridge"); yield return Shot("421_bridge_down");
            yield return TpSettle(new Vector3(34.7f, 7.1f, 28f), 270); yield return Go(new Vector3(20, 7f, 28f), .6f, 8f);
            Ok(Player.transform.position.y > 6.8f && Player.transform.position.x < 23.5f, $"across the lowered bridge onto the gantry ({Fmt(Player.transform.position)})");
            yield return SShard("F24"); yield return SShard("F25");
            yield return WindAt(new Vector3(-28f, 0, 44f), "SP-lift");
            var lift = Object.FindObjectsByType<WindLift>(FindObjectsSortMode.None).First();
            yield return WaitFor(() => lift.transform.position.y < .5f, 14f);
            yield return TpSettle(lift.transform.position + Vector3.up * .3f, 0);
            yield return WaitFor(() => Player.transform.position.y > 11.6f, 14f); yield return Wait(.3f);
            Ok(Player.transform.position.y > 11.6f, $"rode the lift up to the crane deck's height ({Fmt(Player.transform.position)})");
            yield return Go(new Vector3(-29f, 12f, 53.5f), .5f, 4f); yield return SShard("F22"); yield return SShard("F23"); yield return Shot("422_crane_deck");
            yield return WindAt(new Vector3(-21f, 0, 9.5f), "SP-geardoor-boiler"); yield return Shot("423_gear_door_open");
            yield return Go(new Vector3(-28f, 0, 6f), .6f, 8f); yield return SShard("F20"); yield return SShard("F21");
            yield return TpSettle(new Vector3(20f, .1f, 69.5f), 0); int m0 = MagnetPillar.Climbs;
            yield return Go(new Vector3(20f, 0, 70.6f), .3f, 3f, .5f); yield return Press(JumpBtn);
            yield return WaitFor(() => !M.Climbing && MagnetPillar.Climbs > m0, 6f); yield return Wait(.6f);
            Ok(MagnetPillar.Climbs > m0 && Player.transform.position.y > 11.8f, $"magnet paws: up the riveted pillar to its girder ({Fmt(Player.transform.position)})");
            yield return SShard("F26"); yield return Shot("424_girder");
            yield return SwitchTo(0); yield return TpSettle(new Vector3(18.4f, .1f, 72f), 0); int c0 = MagnetPillar.Climbs; yield return Go(new Vector3(20f, 0, 70.6f), .3f, 3f, .5f); yield return Press(JumpBtn); yield return Wait(1f);
            Ok(MagnetPillar.Climbs == c0, "Dan can't climb the riveted pillar (only mechanical Kira)");

            Head("Round 11, item 8: Muscle Man's jobs in the factory");
            yield return MMJob(new Vector3(25.5f, .1f, 44f), 90, "SP-bars-cage"); yield return Go(new Vector3(31, 0, 44f), .6f, 6f); yield return SShard("F28"); yield return SShard("F29");
            yield return MMJob(new Vector3(-26f, .1f, 33f), 270, "SP-crank-nook"); yield return Go(new Vector3(-33f, 0, 33f), .6f, 6f); yield return SShard("F30");
            yield return MMJob(new Vector3(20f, .1f, 60.9f), 0, "SP-stamper-lane"); yield return SShard("F19");
            foreach (var id in new[] { "F01", "F02", "F03", "F04", "F05", "F06", "F09" }) { var sp = SpotSP(id); yield return TpSettle(sp + new Vector3(0, -.9f, -3f), 0); yield return SShard(id, .5f, 8f); }
            Log($"  workers slipped {Enemy.SlipsAll} times so far; {Enemy.All.Count(e => e && e.kind == Enemy.Kind.Worker)} workers");

            Head("Round 11, items 3 and 12: through the hole into the streets (the day look, the city's sound)");
            yield return TpSettle(new Vector3(0, .1f, 90f), 0); yield return Go(new Vector3(0, 0, 103f), .8f, 8f); yield return Wait(.8f);
            Ok(Player.transform.position.z > 100f && !ShardsOfTime.Look.Dark, $"walked out through the hole the press made into the yard ({Fmt(Player.transform.position)})");
            yield return Shot("430_yard_morning");
            yield return SShard("S05");
            foreach (var id in new[] { "S01", "S02", "S03", "S04" }) { var sp = SpotSP(id); yield return TpSettle(sp + new Vector3(0, -.9f, -3f), 0); yield return SShard(id, .5f, 8f); }
            yield return Shot("431_avenue");

            Head("Round 11, item 7: the Violinist teaches Abi the wall jump; up the narrow alley");
            yield return SwitchTo(1);
            yield return TpSettle(Steampunk.ViolinistAt + new Vector3(-4f, .1f, 2.5f), 120);
            yield return Go(Steampunk.ViolinistAt + new Vector3(-1.6f, 0, 1f), .4f, 6f, .6f);
            yield return WaitFor(() => played.Contains("ViolinistTeachesSteampunk"), 6f); yield return Wait(2f); yield return Shot("440_street_musician");
            yield return WaitFor(() => !Game.I.Busy && !Game.InCutscene, 15f);
            Ok(SaveGame.Current.Has(Abilities.WallJump) && Player.Motor.CanWallJump, $"Abi has the wall jump ({Player.Stats.displayName}: {Player.Motor.CanWallJump})");
            yield return WallJumpUp(new Vector3(-14f, 0, 139.2f), 14.8f, Vector3.forward);
            yield return GoF(SpotSP("S10"), .5f, 6f); yield return WaitFor(() => Has("S10"), 1.5f);
            Ok(Has("S10"), $"S10: wall-jumped up the narrow alley onto the tall house's roof ({Fmt(Player.transform.position)})");
            yield return Shot("441_alley_roof");

            Head("Round 11, items 8-9: the square: carriages, the horse gag, the boost; the gear gate; the signal tower");
            yield return SwitchTo(0);
            var car = Carriage.All.First();
            yield return TpSettle(car.transform.position + Vector3.up * 3.0f, car.transform.eulerAngles.y); var cs = car.transform.position;
            yield return Wait(3f);
            Ok(Player.transform.position.y > 2.6f && Vector3.Distance(car.transform.position, cs) > 5f, $"rode a carriage's roof ({Fmt(Player.transform.position)}, the carriage moved {Vector3.Distance(car.transform.position, cs):0.0} m)");
            yield return Shot("450_carriage_ride");
            int b0 = Carriage.Bumps;
            for (int tr = 0; tr < 4 && Carriage.Bumps == b0; tr++)   // 3.5 m ahead of it on a straight (not just before a corner, where it turns away)
            {
                yield return WaitFor(() => Mathf.Abs(car.transform.position.x) < 13f || Mathf.Abs(car.transform.position.z - 200f) < 13f, 12f);   // the middle of a straight
                var ahead = car.transform.position + car.transform.forward * 3.5f; ahead.y = .1f;
                yield return TpSettle(ahead, car.transform.eulerAngles.y + 180); yield return WaitFor(() => Carriage.Bumps > b0, 3f);
            }
            Ok(Carriage.Bumps > b0 && Health.Hearts > 0, $"standing in a carriage's way: bumped aside, no harm ({Carriage.Bumps - b0} bumps, {Health.Hearts} hearts)");
            int g0 = HorseGag.Played; yield return TpSettle(new Vector3(-31f, .1f, 188f), 0);
            yield return WaitFor(() => HorseGag.Played > g0, 4f); yield return Wait(2.6f); yield return Shot("451_horse_gag"); yield return WaitFor(() => !Game.I.Busy, 10f); yield return Wait(.5f);
            Ok(HorseGag.Played > g0, "Muscle Man pets the clockwork horse; it puffs steam in his face");
            yield return SShard("S26");
            yield return SwitchTo(0); yield return TpSettle(new Vector3(35.4f, .1f, 183f), 90); yield return Go(new Vector3(36.5f, 0, 183f), .4f, 4f, .5f);
            yield return WaitFor(() => P.Companion && Vector3.Distance(P.Companion.transform.position, Player.transform.position) < 6f, 5f);
            yield return Press(AttackBtn); yield return WaitFor(() => Game.I.Busy, 3f); yield return WaitFor(() => !Game.I.Busy, 8f); yield return WaitFor(() => M.Grounded, 6f); yield return Wait(.5f);
            Ok(Player.transform.position.y > 8.6f, $"Muscle Man boosts Dan up onto the cream house's roof ({Fmt(Player.transform.position)})");
            yield return SShard("S25");
            foreach (var id in new[] { "S06", "S07", "S08", "S09", "S27", "S28" }) { var sp = SpotSP(id); yield return TpSettle(sp + new Vector3(0, -.9f, -2.5f), 0); yield return SShard(id, .5f, 8f); }
            yield return WindAt(new Vector3(64f, 0, 195f), "SP-geargate"); yield return Shot("452_gear_gate_open");
            yield return Go(new Vector3(80, 0, 200), .8f, 10f); yield return SShard("S16"); yield return SShard("S17");
            yield return SwitchTo(2); yield return TpSettle(new Vector3(-80f, .1f, 202f), 0); m0 = MagnetPillar.Climbs;
            yield return Go(new Vector3(-80f, 0, 204f), .3f, 3f, .5f); yield return Press(JumpBtn);
            yield return WaitFor(() => !M.Climbing && MagnetPillar.Climbs > m0, 6f); yield return Wait(.5f); yield return SShard("S19");
            Log($"  {ShardsSP} from Steampunk, {SaveGame.Current.Total} in all (threshold {Game.World.areas[0].requirement}, {Game.World.areas[0].fromHere} from here)");

            Head("Round 11, item 11: the necklace, the great clock, ClockShard, the teal vortex");
            yield return SwitchTo(0); yield return TpSettle(new Vector3(0, .1f, 175f), 0);
            yield return WaitFor(() => played.Contains("NecklaceWakesSteampunk"), 12f);
            Ok(played.Contains("NecklaceWakesSteampunk") && SaveGame.Current.World(WorldData.Steampunk).necklaceAwake, $"the necklace wakes at {SaveGame.Current.Total} in all, {ShardsSP} from here (NecklaceWakesSteampunk, teal)");
            yield return WaitFor(() => !Game.I.Busy && !Game.InCutscene, 15f); yield return Wait(.5f);
            Ok(Necklace.Target == Game.I.StoryShard.transform, $"it leads to the great clock (target {(Necklace.Target ? Necklace.Target.name : "none")})");
            yield return Shot("460_necklace_to_clock");
            yield return Go(Steampunk.ClockAt + new Vector3(0, 0, -3.5f), .6f, 12f);
            yield return WaitFor(() => Cutscenes.Playing == "ClockShard", 6f);
            Ok(Cutscenes.Playing == "ClockShard", "at the clock's foot: ClockShard");
            foreach (var (t, n) in new[] { (1.5f, "461a_kira_spots_it"), (4.6f, "461b_muscle_man_lifts_dan"), (6.3f, "461c_dan_takes_it"), (9.5f, "461d_teal_vortex"), (11.5f, "461e_pulled_in") })
            { yield return WaitFor(() => Cutscenes.Director && Cutscenes.Director.time >= t || Cutscenes.Playing == null, 20f); yield return Shot(n); }
            yield return WaitFor(() => !Game.I.Busy && !Game.InCutscene, 20f); yield return Wait(1f);
            var home = VortexDoor.All.First(d => d.info.id == "Home"); tl = VortexDoor.All.First(d => d.info.id == "Tomorrowland");
            Ok(Vortex.Inside(Player.transform.position) && SaveGame.Current.World(WorldData.Steampunk).storyShard && home.label.text.Contains("6 of 8"), $"into the vortex with the teal shard; Home says \"{home.label.text.Replace("\n", " ")}\"");
            Ok(P.Companion && Vector3.Distance(P.Companion.transform.position, Player.transform.position) < 9f, "Muscle Man came too");
            yield return Go(VPath(Yaw("Tomorrowland"), 4), .8f); yield return Go(VPath(Yaw("Tomorrowland"), 19), .6f);
            yield return Go(VPath(Yaw("Tomorrowland"), 24), .5f, 6f, 1f, () => Game.I.Busy);
            yield return WaitFor(() => Game.I.ToBeContinuedShown > 0, 6f);
            Ok(Game.I.ToBeContinuedShown > 0 && !tl.Blocked, $"Tomorrowland: to be continued ({tl.label.text.Replace("\n", " ")})");
            yield return WaitFor(() => !Game.I.Busy, 10f);
            Log($"  Victorian Steampunk from the arrival to the vortex took the bot {(Time.time - t0) / 60f:0.0} min");
            yield return WallJumpMedieval();
        }

        // Round 11, item 7: an earlier world's wall-jump gate with the new move (the Medieval village's chimney, J01)
        IEnumerator WallJumpMedieval()
        {
            Head("Round 11, item 7: an earlier world's wall-jump gate (the Medieval village's chimney)");
            if (!SaveGame.Current.Has(Abilities.WallJump)) { SaveGame.Current.abilities.Add(Abilities.WallJump); P.ApplyAbilities(); }
            SaveGame.Current.checkpoint = Medieval.VortexCp; yield return StartWorld(WorldData.Medieval); yield return WaitFor(() => !Game.I.Busy, 10f);
            yield return SwitchTo(1);
            var gate = WorldData.Load(WorldData.Medieval).gates.First(g => g.id == "ME-chimney-village");
            var j = Medieval.ShardPos(WorldData.Load(WorldData.Medieval).shards.First(s => s.id == "J01").position);
            yield return WallJumpUp(Medieval.OnGround(gate.position.x, gate.position.z, 3f), j.y - 1.6f, Vector3.back);
            yield return GoF(j, .4f, 4f); yield return WaitFor(() => Has("J01"), 1.5f);
            Ok(Has("J01") && j.y > 8f, $"J01 on the chimney's ledge by wall jumps ({Fmt(Player.transform.position)}, shard at {Fmt(j)})");
            yield return Shot("470_medieval_chimney");
        }
        IEnumerator WallJumpOnly() { yield return EnsurePlaying(false); if (!SaveGame.Current.Has(Abilities.MuscleMan)) SaveGame.Current.abilities.Add(Abilities.MuscleMan); yield return WallJumpMedieval(); }

        IEnumerator R11() { yield return SteampunkOnly(); }
        IEnumerator StuckSteam() { yield return EnsurePlaying(false); yield return SteamEnter(Steampunk.FactoryCp); yield return StuckWorld(WorldData.Steampunk, null); }

        IEnumerator PerfSteam()
        {
            Head($"Round 11, item 12: frame rate in Victorian Steampunk, the full view drawn every frame ({(TestRun.Headless ? $"{TestRun.View.width} x {TestRun.View.height} off-screen" : "window")})");
            if (!Game.InSteampunk) { yield return EnsurePlaying(false); yield return SteamEnter(Steampunk.FactoryCp); }
            yield return SwitchTo(0);
            float worstAvg = 9999; string where = "";
            foreach (var (name, from, path) in new[]
            {
                ("the factory floor (steam, stampers, lamps)", new Vector3(8, .1f, 30), new[] { new Vector3(12, 0, 70), new Vector3(-12, 0, 70), new Vector3(-12, 0, 20) }),
                ("the east catwalk over the hall", new Vector3(34.7f, 7.1f, 30), new[] { new Vector3(34.7f, 7, 80), new Vector3(34.7f, 7, 40) }),
                ("the square and the clock", new Vector3(0, .1f, 178), new[] { new Vector3(25, 0, 200), new Vector3(0, 0, 220), new Vector3(-25, 0, 200) }),
                ("the avenue toward the square", new Vector3(0, .1f, 115), new[] { new Vector3(0, 0, 170), new Vector3(0, 0, 120) }),
            })
            {
                yield return TpSettle(from, 0);
                Application.targetFrameRate = -1;
                int frames = 0, wp = 0; float t0 = Time.realtimeSinceStartup, worstFrame = 0;
                while (Time.realtimeSinceStartup - t0 < 8f)
                {
                    var d = path[wp] - Player.transform.position; d.y = 0; if (d.magnitude < 2.5f) wp = (wp + 1) % path.Length;
                    Stick(StickFor(d.normalized));
                    frames++; worstFrame = Mathf.Max(worstFrame, Time.unscaledDeltaTime);
                    if (TestRun.Headless) { yield return new WaitForEndOfFrame(); CameraRig.I.Cam.Render(); }
                    yield return null;
                }
                Stick(Vector2.zero);
                float fps = frames / (Time.realtimeSinceStartup - t0);
                Application.targetFrameRate = 144;
                Log($"  {name}: {fps:0} fps average, slowest frame {worstFrame * 1000:0} ms");
                if (fps < worstAvg) { worstAvg = fps; where = name; }
                yield return Shot("490_perf_" + name.Split('(')[0].Trim().Replace(' ', '_'));
            }
            float need = SteamDeck.On ? 40f : 60f;
            Ok(worstAvg > need, $"Victorian Steampunk: {worstAvg:0} fps at the slowest place ({where}); need {need:0} ({SystemInfo.graphicsDeviceName}, {Screen.width}x{Screen.height})");
        }

        IEnumerator SteamDiag()
        {
            yield return EnsurePlaying(false);
            yield return SteamEnter(Steampunk.FactoryCp);
            yield return Wait(1.5f);
            yield return Shot("d0_factory_player_view");
            yield return CloseUpAt(new Vector3(-20, 9, 4), new Vector3(10, 3, 60), "d1_hall_from_sw");
            yield return CloseUpAt(new Vector3(6, 6, 70), new Vector3(0, 4, 90), "d2_belt_end_hole");
            yield return CloseUpAt(new Vector3(0, 4, 112), new Vector3(0, 3, 96), "d3_yard_back_at_factory");
            yield return CloseUpAt(new Vector3(0, 5, 160), new Vector3(0, 5, 200), "d4_avenue_to_clock");
            yield return CloseUpAt(new Vector3(-20, 6, 185), new Vector3(0, 6, 200), "d5_square_clock");
            yield return CloseUpAt(new Vector3(0, 3, 193), new Vector3(0, 6.4f, 198.7f), "d6_clock_face");
            yield return CloseUpAt(new Vector3(50, 4, 200), new Vector3(72, 4, 200), "d7_gear_gate");
            yield return CloseUpAt(new Vector3(-50, 4, 196), new Vector3(-62, 1, 200), "d8_turntable");
            yield return CloseUpAt(new Vector3(-8, 2, 139.2f), new Vector3(-16, 8, 139.2f), "d9_alley");
        }
    }
}
