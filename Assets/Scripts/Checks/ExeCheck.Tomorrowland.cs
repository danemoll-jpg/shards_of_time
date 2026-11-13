using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace ShardsOfTime
{
    // Round 12: Tomorrowland as a player plays it, with the virtual pad, from the vortex (a save with the first six worlds done to their story
    // shards, each one's shards counted that rule 2 counts on arrival here, Muscle Man along, no moves learned on the way):
    //  tomorrowland: the path -> TomorrowlandArrival -> the avenue (the greeter's line, Muscle Man's hug, the walkways) -> robot Kira (the
    //    escalator, the diner door, the Orbit car, the gantry lift; the wheel dash up the boost ramp) -> Muscle Man's jobs -> the cleanbots (one
    //    vacuums a shard and coughs it out; the friendly switch) -> the hover-car lane (a bump) -> the Violinist teaches Abi the air dash, the
    //    far pad -> the necklace -> the Skyline Tower's lift -> the pink shard -> the fuel socket: RocketLaunch -> the vortex (the Moon to be
    //    continued, nine paths) -> Steampunk's north street with the air dash (an earlier world's gate).
    //  stucktl (rule 12), fix81tl (floors, invisible walls, moving platforms, reach), perftl (frame rate), tldiag (looks), dashgate.
    public sealed partial class ExeCheck
    {
        int ShardsTL => SaveGame.Current.World(WorldData.Tomorrowland).shards.Count;
        Vector3 SpotTL(string id) => Tomorrowland.ShardPos(WorldData.Load(WorldData.Tomorrowland).shards.First(s => s.id == id).position);
        IEnumerator TShard(string id, float tol = .5f, float timeout = 15f)
        {
            if (Has(id)) yield break;
            yield return GoF(SpotTL(id), tol, timeout);
            yield return WaitFor(() => Has(id), 1.5f);
            Ok(Has(id), $"{id} ({Game.World.shards.First(s => s.id == id).note}) at {Fmt(Player.transform.position)}; {ShardsTL} here, {SaveGame.Current.Total} in all");
        }

        // the first n worlds done to their story shards, each one's shards that rule 2 counts on arrival in `here`
        IEnumerator WorldsDone(string[] worlds, string here)
        {
            yield return EnsurePlaying(false);
            var hv = WorldData.Load(here).areas[0].firstVisit;
            foreach (var wid in worlds)
            {
                var prog = SaveGame.Current.World(wid); prog.storyShard = true; prog.necklaceAwake = true;
                var wd = WorldData.Load(wid); var fv = wd.areas[0].firstVisit.Concat(hv).ToArray();
                foreach (var sp in wd.shards) if (sp.requires.All(fv.Contains) && !prog.shards.Contains(sp.id)) prog.shards.Add(sp.id);
                if (!SaveGame.Current.worldsReached.Contains(wid)) SaveGame.Current.worldsReached.Add(wid);
            }
            foreach (var f in new[] { (WorldData.Egypt, "EG-arrived"), (WorldData.Bethlehem, "BE-arrived"), (WorldData.Medieval, "ME-arrived"), (WorldData.Pirate, "PS-arrived"), (WorldData.Pirate, Pirate.OverboardFlag),
                                      (WorldData.Steampunk, Steampunk.ArrivedFlag), (WorldData.Steampunk, Steampunk.PressFlag), (WorldData.Tomorrowland, Tomorrowland.ArrivedFlag), (WorldData.Tomorrowland, Tomorrowland.LaunchFlag) })
                if (worlds.Contains(f.Item1) && !SaveGame.Current.World(f.Item1).opened.Contains(f.Item2)) SaveGame.Current.World(f.Item1).opened.Add(f.Item2);
            foreach (var p in new[] { ("IA-vortex", WorldData.IceAge), ("EG-vortex-gate", WorldData.Egypt), ("BE-vortex-gate", WorldData.Bethlehem), ("ME-vortex-gate", WorldData.Medieval), ("PS-vortex-gate", WorldData.Pirate), ("SP-vortex-gate", WorldData.Steampunk), ("TL-vortex-gate", WorldData.Tomorrowland) })
                if (worlds.Contains(p.Item2) && !SaveGame.Current.portals.Contains(p.Item1)) SaveGame.Current.portals.Add(p.Item1);
            if (!SaveGame.Current.abilities.Contains(Abilities.MuscleMan)) SaveGame.Current.abilities.Add(Abilities.MuscleMan);
            if (worlds.Contains(WorldData.Tomorrowland) && !SaveGame.Current.abilities.Contains(Abilities.Rocket)) SaveGame.Current.abilities.Add(Abilities.Rocket);
            Log($"  (set up: {SaveGame.Current.Total} shards in all from {worlds.Length} worlds (what rule 2 counts on arrival in {here}), Muscle Man along, no moves learned)");
            SaveGame.Current.checkpoint = Vortex.Checkpoint; Checkpoint.RespawnParty(); CameraRig.I.SnapBehind();
            if (!P.Companion) P.AddCompanion(Player.transform.position + Vector3.back * 3f, Quaternion.identity);
            yield return Wait(.6f);
        }
        static readonly string[] SixWorlds = { WorldData.IceAge, WorldData.Egypt, WorldData.Bethlehem, WorldData.Medieval, WorldData.Pirate, WorldData.Steampunk };

        IEnumerator TLEnter(string cp)
        {
            var pr = SaveGame.Current.World(WorldData.Tomorrowland); if (!pr.opened.Contains(Tomorrowland.ArrivedFlag)) pr.opened.Add(Tomorrowland.ArrivedFlag);
            if (!SaveGame.Current.Has(Abilities.MuscleMan)) SaveGame.Current.abilities.Add(Abilities.MuscleMan);
            SaveGame.Current.checkpoint = cp; yield return StartWorld(WorldData.Tomorrowland); yield return WaitFor(() => !Game.I.Busy, 10f);
            if (!P.Companion) P.AddCompanion(Player.transform.position + Vector3.back * 3f, Quaternion.identity);
            yield return Wait(.6f);
        }

        // robot Kira onto a terminal pad (the plug-in): its machine works
        IEnumerator PlugAt(Vector3 pad, string id, string what)
        {
            yield return WaitFor(() => !Game.I.Busy && !Game.InCutscene, 20f);   // (a necklace waking mid-test)
            yield return SwitchTo(2);
            yield return TpSettle(pad + new Vector3(0, .1f, -2f), 0);
            yield return Go(pad, .3f, 5f, .5f);
            yield return WaitFor(() => SaveGame.Current.World(Game.World.worldId).opened.Contains(id), 5f); yield return Wait(1.5f);
            Ok(SaveGame.Current.World(Game.World.worldId).opened.Contains(id), $"robot Kira plugs in at the terminal: {what} ({id}; {WindUpMachine.Winds} so far)");
        }

        IEnumerator TomorrowlandOnly()
        {
            yield return WorldsDone(SixWorlds, WorldData.Tomorrowland);
            Cutscenes.Started -= OnPlayed; Cutscenes.Started += OnPlayed;
            float t0 = Time.time;
            Head("Round 12, item 4: the vortex's nine paths, Tomorrowland's path, TomorrowlandArrival");
            var door = VortexDoor.All.FirstOrDefault(d => d.info.id == "Tomorrowland"); var moon = VortexDoor.All.FirstOrDefault(d => d.info.id == "Moon");
            Ok(VortexDoor.All.Count == 9, $"nine paths ({string.Join(", ", VortexDoor.All.Select(d => $"{d.info.id} {d.info.yaw:0}"))})");
            Ok(door && !door.Blocked, $"the Tomorrowland path is open after the teal shard ({(door ? door.label.text.Replace("\n", " ") : "none")})");
            Ok(moon && moon.Blocked, $"the Moon is there and closed until the rocket ({(moon ? moon.label.text.Replace("\n", " ") : "none")})");
            yield return Shot("500_vortex_nine_paths");
            float yaw = Yaw("Tomorrowland");
            yield return Go(VPath(yaw, 4), .8f); yield return Go(VPath(yaw, 19), .6f);
            yield return Go(VPath(yaw, 24), .5f, 6f, 1f, () => Game.I.Busy);
            yield return WaitFor(() => Cutscenes.Playing == "TomorrowlandArrival", 8f);
            Ok(Game.InTomorrowland && Cutscenes.Playing == "TomorrowlandArrival", $"the path loads Tomorrowland and plays its arrival (world {Game.World.worldId}, cutscene {Cutscenes.Playing ?? "none"})");
            foreach (var (t, n) in new[] { (1.8f, "501a_vortex_at_dusk"), (3.4f, "501b_landed"), (4.7f, "501c_the_shimmer"), (7.3f, "501d_a_flying_car"), (11.4f, "501e_the_greeter"), (14.5f, "501f_necklace_pink") })
            { yield return WaitFor(() => Cutscenes.Director && Cutscenes.Director.time >= t || Cutscenes.Playing == null, 30f); yield return Shot(n); }
            yield return WaitFor(() => !Game.InCutscene && !Game.I.Busy, 30f); yield return Wait(.8f);
            var kira = P.Heroes.First(h => h.Stats.displayName == "Kira");
            Ok(Game.Playing && Player.transform.position.z < 16f, $"play starts on the neon avenue ({Fmt(Player.transform.position)})");
            Ok(kira.Form && kira.Form.id == KiraForm.Robot, $"Kira is the robot (model {(kira.Model ? kira.Model.name : "graybox")}, abilities [{string.Join(", ", kira.Form.abilities)}]); Dan wears {P.Heroes[0].Model?.name}; Muscle Man {P.Companion?.Model?.name}");
            Ok(Music.Track == "tomorrowland", $"Dan's tomorrowland song plays ({Music.Track}; {(Music.HasClip ? "loaded" : "MISSING")})");
            yield return Shot("502_avenue_player_view");
            Log($"  HUD: {Hud.TotalShown} / {Hud.UnderTotal}");

            Head("Round 12, items 4, 8: the greeter robot (its line; Muscle Man's hug); the walkways");
            var gr = Greeter.All.OrderBy(g => g.transform.position.z).First();
            yield return SwitchTo(0); yield return TpSettle(gr.transform.position + new Vector3(-2.5f, .1f, -2.5f), 45); yield return WaitFor(() => gr.Said > 0, 4f);
            Ok(gr.Said > 0, $"the greeter: \"{gr.line}\" ({Hud.LastMessage})");
            yield return Shot("503_greeter_line");
            int h0 = Greeter.Hugs; if (P.Companion) P.Companion.Motor.Teleport(gr.transform.position + Vector3.left * 1.6f, Quaternion.identity);
            yield return WaitFor(() => Greeter.Hugs > h0, 4f); yield return Wait(.8f);
            if (P.Companion && P.Companion.Anim) Ok(P.Companion.Anim.State == "offer", $"Fix 13.1: he hugs the air (playing: {P.Companion.Anim.State})");
            yield return Shot("504_hug_beep");
            Ok(Greeter.Hugs > h0, "Muscle Man tries to hug the greeter robot; it beeps in alarm and rolls off");
            yield return TpSettle(new Vector3(11.25f, .35f, 40f), 0); var w0 = Player.transform.position; yield return Wait(2f);
            Ok(Player.transform.position.z - w0.z > 3f, $"the east walkway carries you north ({Player.transform.position.z - w0.z:0.0} m in 2 s, no input)");
            var mal = Object.FindObjectsByType<Malfunction>(FindObjectsSortMode.None).First(); int f0 = mal.Flips; yield return Wait(mal.every + .5f);
            Ok(mal.Flips > f0, $"the west walkway malfunctions: it flipped {mal.Flips - f0} time(s)");
            foreach (var id in new[] { "A01", "A02", "A03", "A04", "A05", "A06", "A07", "A08", "A10", "A11" }) { var sp = SpotTL(id); yield return TpSettle(sp + new Vector3(0, -.9f, -2.5f), 0); yield return TShard(id, .5f, 8f); }

            Head("Round 12, item 6: robot Kira: the plug-in (the escalator, the diner, the Orbit car, the gantry lift) and the wheel dash");
            yield return SwitchTo(0); yield return TpSettle(new Vector3(13f, .2f, 86.5f), 90); yield return Stick2(Vector3.right, 2.5f);
            Ok(Player.transform.position.y < 2f, $"before Kira: the escalator runs down too fast to climb ({Fmt(Player.transform.position)})");
            yield return PlugAt(new Vector3(12.8f, .15f, 92.5f), "TL-escalator", "the escalator turns round");
            yield return SwitchTo(0); yield return TpSettle(new Vector3(13.2f, .2f, 87f), 90);
            Stick(StickFor(Vector3.right)); yield return WaitFor(() => Player.transform.position.y > 6.6f && M.Grounded, 8f); Stick(Vector2.zero); yield return Wait(.3f);
            Ok(Player.transform.position.y > 6.5f, $"up the escalator onto the rooftops ({Fmt(Player.transform.position)})");
            yield return Shot("510_up_the_escalator");
            yield return TShard("R05", .5f, 8f); yield return Go(new Vector3(25f, 7f, 83.4f), .5f, 5f); yield return Go(new Vector3(21f, 7f, 82f), .6f, 5f);
            yield return TShard("R01", .5f, 10f);
            foreach (var p in new[] { new Vector3(21f, 7f, 83.2f), new Vector3(25f, 7f, 83.4f), new Vector3(25f, 7f, 90.6f), new Vector3(21f, 7f, 91.5f) }) yield return Go(p, .5f, 5f);   // round the escalator's gap by its landing
            foreach (var id in new[] { "R02", "R03", "R04" }) yield return TShard(id, .5f, 14f);
            yield return PlugAt(new Vector3(36 - 8 - 2.4f, .02f, 186 - 2.2f), "TL-door-diner", "the diner's door slides open");
            yield return Go(new Vector3(26, 0, 186), .6f, 6f); yield return Go(new Vector3(33, 0, 186), .8f, 8f); yield return Shot("511_atomic_diner");
            foreach (var id in new[] { "D01", "D02", "D03" }) yield return TShard(id, .5f, 8f);
            yield return Go(new Vector3(41, 0, 182), .6f, 6f);
            yield return PlugAt(new Vector3(40, .02f, 170), "TL-call-orbit", "the Orbit car is called");
            var orbit = Tomorrowland.Cars.First(c => c.name == "Orbit car");
            yield return SwitchTo(0); yield return WaitFor(() => Vector3.Distance(orbit.transform.position, new Vector3(44, .3f, 169f)) < 3f, 40f);
            yield return TpSettle(orbit.transform.position + Vector3.up * 1.0f, orbit.transform.eulerAngles.y); yield return Wait(.3f);
            yield return WaitFor(() => Player.transform.position.y > 23.5f && orbit.transform.position.y > 23.5f, 30f); yield return Shot("512_orbit_car_ride");
            yield return Wait(.6f); var lr = new Vector3(56f, 24f, 200f); Stick(StickFor(lr - Player.transform.position)); yield return Wait(.5f); Stick(Vector2.zero);
            yield return Go(lr, .8f, 5f); yield return WaitFor(() => M.Grounded, 3f);
            Ok(Player.transform.position.y > 23.5f, $"rode the Orbit car up to the tower's roof ({Fmt(Player.transform.position)})");
            foreach (var id in new[] { "O01", "O02", "O03" }) yield return TShard(id, .5f, 8f);
            yield return Go(new Vector3(62f, 24f, 204f), .5f, 5f);
            yield return PlugAt(new Vector3(17.2f, .05f, 290.5f), "TL-gantry-lift", "the gantry's service lift starts");
            var lift = Object.FindObjectsByType<WindLift>(FindObjectsSortMode.None).First(l => l.id == "TL-gantry-lift");
            yield return WaitFor(() => lift.transform.position.y < .6f, 18f); yield return TpSettle(lift.transform.position + Vector3.up * .3f, 270);
            yield return WaitFor(() => Player.transform.position.y > 17.6f, 18f); yield return Wait(.2f); Stick(StickFor(Vector3.left)); yield return Wait(.6f); Stick(Vector2.zero);
            yield return WaitFor(() => M.Grounded, 3f);
            Ok(Player.transform.position.y > 17.6f, $"rode the service lift to the gantry's top ({Fmt(Player.transform.position)})");
            yield return TShard("T07", .5f, 6f); yield return TShard("T08", .5f, 6f); yield return Shot("513_gantry_top");
            // the wheel dash up the boost ramp onto the billboard
            yield return SwitchTo(2); yield return TpSettle(new Vector3(0, .1f, 106f), 0); int ramps = BoostRampLaunched;
            Stick(StickFor(Vector3.forward)); yield return Wait(.3f); yield return Press(DiveBtn);
            yield return WaitFor(() => BoostRampLaunched > ramps, 3f); yield return Wait(.25f); Stick(StickFor(Vector3.forward) * .6f); yield return WaitFor(() => M.Grounded, 5f); Stick(Vector2.zero); yield return Wait(.3f);
            Ok(BoostRampLaunched > ramps && Player.transform.position.y > 8f, $"wheel dash up the boost ramp: onto the billboard ({Fmt(Player.transform.position)}; ramp launches {BoostRampLaunched - ramps})");
            yield return Shot("514_billboard");
            foreach (var id in new[] { "K01", "K02", "K03" }) yield return TShard(id, .4f, 6f);

            Head("Round 12, item 8: Muscle Man's jobs (the parked car, the vending machine, the chrome bars, the boost)");
            yield return MMJob(new Vector3(-4f, .1f, 43.2f), 0, "TL-car-hatch"); yield return Shot("520_car_lifted");
            yield return Go(new Vector3(-4, -2.6f, 49), .8f, 10f); foreach (var id in new[] { "P01", "P02" }) yield return TShard(id, .5f, 8f);
            yield return Go(new Vector3(-4, 0, 40f), .9f, 10f);
            Ok(Player.transform.position.y > -.5f, $"out of the pit up its steps ({Fmt(Player.transform.position)})");
            yield return MMJob(new Vector3(-13.3f, .2f, 117.75f), 270, "TL-vending");   // (on the sidewalk: x -12.5..-10 is the walkway) yield return Go(new Vector3(-30, 0, 118), .8f, 12f);
            foreach (var id in new[] { "V01", "V02" }) yield return TShard(id, .5f, 8f);
            yield return MMJob(new Vector3(12.4f, .2f, 104.5f), 90, "TL-bars-nook"); yield return Go(new Vector3(26, 0, 104.5f), .8f, 8f);
            foreach (var id in new[] { "B01", "B02" }) yield return TShard(id, .5f, 8f);
            yield return SwitchTo(0); yield return TpSettle(new Vector3(-10.5f, .3f, 98f), 270); yield return Go(new Vector3(-11.6f, .15f, 98f), .4f, 4f, .5f);
            yield return WaitFor(() => P.Companion && Vector3.Distance(P.Companion.transform.position, Player.transform.position) < 6f, 5f);
            yield return Press(AttackBtn); yield return WaitFor(() => Game.I.Busy, 3f); yield return WaitFor(() => !Game.I.Busy, 8f); yield return WaitFor(() => M.Grounded, 6f); yield return Wait(.5f);
            Ok(Player.transform.position.y > 6.5f, $"Muscle Man boosts Dan onto the west roofs ({Fmt(Player.transform.position)})");
            foreach (var id in new[] { "R06", "R07", "R08" }) yield return TShard(id, .5f, 10f);

            Head("Round 12, item 9: the cleanbots (one vacuums a shard and coughs it back out), the friendly switch; the hover-car lane");
            var bot = Enemy.All.First(e => e && e.kind == Enemy.Kind.Cleanbot && e.isActiveAndEnabled);
            var loose = Pickup.All.First(p => p && p.kind == Pickup.Kind.Shard && p.gameObject.activeSelf && p.id == "Q06");
            yield return TpSettle(loose.transform.position + new Vector3(0, -.9f, -7f), 0);
            int s0 = Enemy.Sucks; var bcc = bot.GetComponent<CharacterController>(); bcc.enabled = false; bot.transform.position = loose.transform.position + new Vector3(.3f, -1f, 0); bcc.enabled = true;   // a cleanbot rolls over a shard
            yield return WaitFor(() => Enemy.Sucks > s0, 3f);
            Ok(Enemy.Sucks > s0 && bot.Carrying > 0, $"a cleanbot vacuums up a loose shard (it carries {bot.Carrying})");
            yield return SwitchTo(0); int c0 = Enemy.Coughs;
            for (int i = 0; i < 4 && Enemy.Coughs == c0; i++) { yield return TpSettle(bot.transform.position - bot.transform.forward * 1.3f + Vector3.up * .1f, bot.transform.eulerAngles.y); yield return Press(AttackBtn); yield return Wait(.5f); }
            Ok(Enemy.Coughs > c0 && loose.gameObject.activeSelf, $"bopped, it coughs the shard back out (at {Fmt(loose.transform.position)})");
            bot.Stun(5f);   // (before it rolls over it again)
            yield return Shot("530_cleanbot_cough");
            yield return GoF(loose.transform.position, .5f, 8f); yield return WaitFor(() => Has("Q06"), 1.5f); Ok(Has("Q06"), "the coughed-out shard is collected (Q06)");
            yield return SwitchTo(2); yield return TpSettle(new Vector3(6, .1f, 163.5f), 0); yield return Go(new Vector3(6, 0, 166), .3f, 4f, .5f); yield return Wait(1.5f);
            Ok(Enemy.CleanbotsFriendly, $"robot Kira's friendly switch: the cleanbots are friendly ({Enemy.FriendlySwitches} switches)");
            yield return SwitchTo(0); int hb = HoverLane.Bumps, hearts = Health.Hearts;
            yield return TpSettle(new Vector3(-30f, .1f, 154f), 90); yield return WaitFor(() => HoverLane.Bumps > hb, 14f); yield return Wait(.8f);
            Ok(HoverLane.Bumps > hb && Health.Hearts == hearts, $"in the hover-car lane: bumped aside, no heart lost ({HoverLane.Bumps - hb} bumps, {Health.Hearts} hearts)");
            yield return Shot("531_hover_lane");

            Head("Round 12, item 7: the Violinist teaches Abi the air dash; the far pad");
            if (!SaveGame.Current.abilities.Contains(Abilities.DoubleJump)) { SaveGame.Current.abilities.Add(Abilities.DoubleJump); Log("  (the double jump, learned in Egypt, added for the far pad)"); }
            yield return SwitchTo(1); P.ApplyAbilities();
            yield return TpSettle(Tomorrowland.ViolinistAt + new Vector3(-4f, .1f, -3f), 45);
            yield return Go(Tomorrowland.ViolinistAt + new Vector3(-1.4f, 0, -1.4f), .4f, 6f, .6f);
            yield return WaitFor(() => played.Contains("ViolinistTeachesTomorrowland"), 6f); yield return Wait(2f); yield return Shot("540_chrome_violin");
            yield return WaitFor(() => !Game.I.Busy && !Game.InCutscene, 15f);
            Ok(SaveGame.Current.Has(Abilities.AirDash) && Player.Motor.CanAirDash, $"Abi has the air dash ({Player.Motor.CanAirDash})");
            yield return DashGap(new Vector3(-37.2f, 4.05f, 212f), new Vector3(-51.75f, 4f, 212f), "X01", "the far pad");
            yield return TShard("X02", .4f, 4f);

            Head("Round 12, items 2, 3: the rest of the city (the sky deck by the flying car, the square, the garden, the hall)");
            yield return SwitchTo(0);
            var sky = Tomorrowland.Cars.First(c => c.name == "Sky car");
            yield return WaitFor(() => Vector3.Distance(sky.transform.position, new Vector3(-24, .3f, 165)) < 4f, 40f);
            yield return TpSettle(sky.transform.position + Vector3.up * 1.0f, sky.transform.eulerAngles.y);
            yield return WaitFor(() => Player.transform.position.y > 15.8f && sky.transform.position.y > 15.8f, 30f); yield return Wait(.4f);
            Stick(StickFor(Vector3.left)); yield return Wait(.7f); Stick(Vector2.zero); yield return WaitFor(() => M.Grounded, 3f);
            Ok(Player.transform.position.y > 15.5f, $"rode the sky car up to the Ring Tower's sky deck ({Fmt(Player.transform.position)})");
            yield return Shot("550_sky_deck");
            foreach (var id in new[] { "Y01", "Y02", "Y03", "Y04" }) yield return TShard(id, .5f, 8f);
            foreach (var id in new[] { "Q01", "Q02", "Q03", "Q04", "Q05", "G01", "G02", "G03", "T01", "T02", "T03", "T04" }) { var sp = SpotTL(id); yield return TpSettle(sp + new Vector3(0, -.9f, -2.5f), 0); yield return TShard(id, .5f, 8f); }
            yield return TpSettle(new Vector3(-17.6f, .1f, 272f), 0); yield return Go(new Vector3(-17.6f, 6f, 292f), .8f, 10f);
            Ok(Player.transform.position.y > 5.6f, $"up the west gantry's ramp ({Fmt(Player.transform.position)})");
            yield return TShard("T05", .5f, 6f); yield return TShard("T06", .5f, 6f);
            Log($"  {ShardsTL} from Tomorrowland, {SaveGame.Current.Total} in all (threshold {Game.World.areas[0].requirement}, {Game.World.areas[0].fromHere} from here)");
            yield return Shot("551_launch_hall");

            Head("Round 12, items 5, 11: the necklace, the Skyline Tower's lift, the pink shard, the fuel socket: RocketLaunch");
            yield return TpSettle(new Vector3(-20, .1f, 196f), 270);
            yield return WaitFor(() => played.Contains("NecklaceWakesTomorrowland"), 12f);
            Ok(played.Contains("NecklaceWakesTomorrowland") && SaveGame.Current.World(WorldData.Tomorrowland).necklaceAwake, $"the necklace wakes at {SaveGame.Current.Total} in all, {ShardsTL} from here (NecklaceWakesTomorrowland, pink)");
            yield return WaitFor(() => !Game.I.Busy && !Game.InCutscene, 15f); yield return Wait(.6f);
            Ok(Necklace.Target == Game.I.StoryShard.transform && Tomorrowland.Lift.On, $"it leads to the Skyline Tower's roof; the lift is lit (target {(Necklace.Target ? Necklace.Target.name : "none")})");
            yield return Shot("560_necklace_to_tower");
            var lt = Tomorrowland.Lift.transform;
            yield return WaitFor(() => lt.position.y < .8f, 20f); yield return TpSettle(lt.position + Vector3.up * .3f, 270);
            yield return WaitFor(() => Player.transform.position.y > Tomorrowland.SkylineTop - .5f, 20f); yield return Wait(.2f);
            Stick(StickFor(Vector3.left)); yield return Wait(.6f); Stick(Vector2.zero); yield return WaitFor(() => M.Grounded, 3f);
            Ok(Player.transform.position.y > Tomorrowland.SkylineTop - .5f, $"rode the glass lift to the Skyline Tower's roof ({Fmt(Player.transform.position)})");
            yield return Shot("561_skyline_roof");
            int pk = Game.I.PinkShards; yield return GoF(Game.I.StoryShard.transform.position, .5f, 8f); yield return WaitFor(() => Game.I.PinkShards > pk, 2f);
            Ok(Game.I.PinkShards > pk && SaveGame.Current.World(WorldData.Tomorrowland).storyShard, $"the neon pink shard ({Hud.LastMessage})");
            yield return TpSettle(Tomorrowland.SocketAt + new Vector3(0, .1f, -7f), 0); yield return Go(Tomorrowland.SocketAt + new Vector3(0, 0, -2f), .6f, 8f);
            yield return WaitFor(() => Cutscenes.Playing == "RocketLaunch", 6f);
            Ok(Cutscenes.Playing == "RocketLaunch", "at the fuel socket: RocketLaunch");
            foreach (var (t, n) in new[] { (2.0f, "562a_dan_places_the_shard"), (4.4f, "562b_small_before_the_rocket"), (6.2f, "562c_boarding"), (8.4f, "562d_doors_glow"), (10.6f, "562e_ignition"), (13.2f, "562f_into_the_stars") })
            { yield return WaitFor(() => Cutscenes.Director && Cutscenes.Director.time >= t || Cutscenes.Playing == null, 25f); yield return Shot(n); }
            yield return WaitFor(() => !Game.I.Busy && !Game.InCutscene, 30f); yield return Wait(1f);
            moon = VortexDoor.All.FirstOrDefault(d => d.info.id == "Moon"); door = VortexDoor.All.FirstOrDefault(d => d.info.id == "Tomorrowland");
            if (!Moon.Real)
            {
                Ok(Vortex.Inside(Player.transform.position) && Game.I.ToBeContinuedShown > 0, $"the launch ends in the vortex, the Moon to be continued ({SaveGame.Current.StoryShards} story shards)");
                Ok(moon && !moon.Blocked && door && !door.Blocked, $"the Moon's path is there ({(moon ? moon.label.text.Replace("\n", " ") : "-")}), Tomorrowland's stays open");
                Ok(P.Companion && Vector3.Distance(P.Companion.transform.position, Player.transform.position) < 9f, "Muscle Man came too");
                yield return Shot("563_vortex_after_launch");
            }
            Log($"  Tomorrowland from the arrival to the launch took the bot {(Time.time - t0) / 60f:0.0} min");
            yield return DashGateSteampunk();
        }

        static int BoostRampLaunched => BoostRamp.All.Sum(r => r ? r.Launched : 0);
        IEnumerator Stick2(Vector3 dir, float s) { Stick(StickFor(dir)); yield return Wait(s); Stick(Vector2.zero); yield return Wait(.3f); }

        // Abi on a platform: run, jump, double jump at the top, air dash toward the far one; lands on it
        IEnumerator DashGap(Vector3 from, Vector3 to, string shard, string what)
        {
            yield return SwitchTo(1);
            var dir = to - from; dir.y = 0; dir.Normalize();
            int d0 = CharacterMotor.AirDashesAll;
            for (int attempt = 0; attempt < 3 && !Has(shard); attempt++)
            {
                yield return TpSettle(from - dir * 1.4f, Quaternion.LookRotation(dir).eulerAngles.y);
                Stick(StickFor(dir)); yield return Wait(.25f);
                Btn(JumpBtn, true); yield return WaitFor(() => !M.Grounded, .5f); yield return WaitFor(() => M.Velocity.y < 1.2f, 1.2f);
                Btn(JumpBtn, false); yield return null; Btn(JumpBtn, true); yield return Wait(.12f);
                yield return WaitFor(() => M.Velocity.y < .5f, 1.2f); Btn(JumpBtn, false);
                yield return Press(DiveBtn);
                yield return WaitFor(() => M.Grounded, 4f); Stick(Vector2.zero); yield return Wait(.3f);
                yield return GoF(to + Vector3.up * .05f, .5f, 2f, .5f);
                yield return WaitFor(() => Has(shard), 1.5f);
            }
            Ok(Has(shard) && CharacterMotor.AirDashesAll > d0, $"{shard}: jump, double jump, air dash onto {what} ({Fmt(Player.transform.position)}; {CharacterMotor.AirDashesAll - d0} dashes)");
            yield return Shot("541_air_dash_" + shard);
        }

        // Round 12, item 7: an earlier world's air-dash gate (Steampunk's north street: S31)
        IEnumerator DashGateSteampunk()
        {
            Head("Round 12, item 7: an earlier world's air-dash gap (Victorian Steampunk's north street)");
            foreach (var a in new[] { Abilities.AirDash, Abilities.DoubleJump }) if (!SaveGame.Current.Has(a)) SaveGame.Current.abilities.Add(a);
            P.ApplyAbilities();
            yield return SteamEnter(Steampunk.StreetCp);
            yield return SwitchTo(1);
            Ok(Game.World.shards.Any(s => s.id == "S31"), "Steampunk's data has the air-dash shard S31 (Steampunk.asset remade)");
            yield return DashGap(new Vector3(0, 3.65f, 262f), new Vector3(0, 3.6f, 277.5f), "S31", "the far lamp platform over the north street");
        }
        IEnumerator DashGateOnly() { yield return EnsurePlaying(false); if (!SaveGame.Current.Has(Abilities.MuscleMan)) SaveGame.Current.abilities.Add(Abilities.MuscleMan); yield return DashGateSteampunk(); }

        IEnumerator StuckTL() { yield return EnsurePlaying(false); yield return TLEnter(Tomorrowland.Start); yield return StuckWorld(WorldData.Tomorrowland, null); }

        IEnumerator PerfTL()
        {
            Head($"Round 12, item 12: frame rate in Tomorrowland, the full view drawn every frame ({(TestRun.Headless ? $"{TestRun.View.width} x {TestRun.View.height} off-screen" : "window")})");
            if (!Game.InTomorrowland) { yield return EnsurePlaying(false); yield return TLEnter(Tomorrowland.Start); }
            yield return SwitchTo(0);
            float worstAvg = 9999; string where = "";
            foreach (var (name, from, path) in new[]
            {
                ("the neon avenue (neon, glass, walkways, cars)", new Vector3(0, .1f, 10), new[] { new Vector3(0, 0, 70), new Vector3(0, 0, 140), new Vector3(0, 0, 40) }),
                ("the square (domes, towers, the fountain)", new Vector3(0, .1f, 168), new[] { new Vector3(25, 0, 200), new Vector3(0, 0, 225), new Vector3(-25, 0, 200) }),
                ("the launch hall (the golden dome, the rocket)", new Vector3(0, .1f, 262), new[] { new Vector3(14, 0, 280), new Vector3(-14, 0, 280), new Vector3(0, 0, 264) }),
                ("the Skyline Tower's roof (the whole city)", new Vector3(-70, Tomorrowland.SkylineTop + .1f, 196), new[] { new Vector3(-66, Tomorrowland.SkylineTop, 200), new Vector3(-72, Tomorrowland.SkylineTop, 204) }),
            })
            {
                yield return TpSettle(from, 0);
                Application.targetFrameRate = -1;
                int frames = 0, wp = 0; float t0 = Time.realtimeSinceStartup, worstFrame = 0;
                while (Time.realtimeSinceStartup - t0 < 8f)
                {
                    var d = path[wp] - Player.transform.position; d.y = 0; if (d.magnitude < 2.5f) wp = (wp + 1) % path.Length;
                    Stick(StickFor(d.normalized) * .7f);
                    frames++; worstFrame = Mathf.Max(worstFrame, Time.unscaledDeltaTime);
                    if (TestRun.Headless) { yield return new WaitForEndOfFrame(); CameraRig.I.Cam.Render(); }
                    yield return null;
                }
                Stick(Vector2.zero);
                float fps = frames / (Time.realtimeSinceStartup - t0);
                Application.targetFrameRate = 144;
                Log($"  {name}: {fps:0} fps average, slowest frame {worstFrame * 1000:0} ms");
                if (fps < worstAvg) { worstAvg = fps; where = name; }
                yield return Shot("590_perf_" + name.Split('(')[0].Trim().Replace(' ', '_'));
            }
            float need = SteamDeck.On ? 40f : 60f;
            Ok(worstAvg > need, $"Tomorrowland: {worstAvg:0} fps at the slowest place ({where}); need {need:0} ({SystemInfo.graphicsDeviceName}, {Screen.width}x{Screen.height})");
        }

        IEnumerator TLDiag()
        {
            yield return EnsurePlaying(false);
            yield return TLEnter(Tomorrowland.Start);
            yield return Wait(1.5f);
            yield return Shot("d0_avenue_player_view");
            yield return CloseUpAt(new Vector3(0, 6, -6), new Vector3(0, 4, 60), "d1_avenue_from_south");
            yield return CloseUpAt(new Vector3(8, 3, 80), new Vector3(20, 4, 87), "d2_escalator");
            yield return CloseUpAt(new Vector3(0, 6, 150), new Vector3(0, 4, 195), "d3_square");
            yield return CloseUpAt(new Vector3(20, 4, 176), new Vector3(36, 3, 186), "d4_diner");
            yield return CloseUpAt(new Vector3(-30, 8, 170), new Vector3(-70, 30, 200), "d5_skyline_tower");
            yield return CloseUpAt(new Vector3(0, 4, 230), new Vector3(0, 6, 238), "d6_terminal_front");
            yield return CloseUpAt(new Vector3(0, 3, 266), new Vector3(0, 12, 298), "d7_hall_rocket");
            yield return CloseUpAt(new Vector3(-6, 2.5f, 44), new Vector3(-4, 1, 48), "d8_parked_car");
            yield return CloseUpAt(new Vector3(-2, 1.5f, 24), new Vector3(5, 1, 24), "d9_greeter");
        }
    }
}
