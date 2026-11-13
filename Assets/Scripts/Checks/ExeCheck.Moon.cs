using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace ShardsOfTime
{
    // Round 13: the Moon as a player plays it, with the virtual pad: from Tomorrowland's fuel socket (seven worlds done, the pink shard in hand)
    //  moon: RocketLaunch -> MoonLanding -> the landing site (helmets, low gravity, the Moon's path in the vortex) -> KiraFloats -> space Kira
    //    floats up a pillar (Abi can't) -> a bouncy rim -> the critters (a bump, a bop: rolls away) -> Muscle Man (the airlock, a boulder, the
    //    mesa) -> the base -> the Violinist's tune -> the necklace -> the rover: Dan puzzles ("Switch to Abi"), Abi solves the light puzzle
    //    (one wrong press: the pattern again) -> WhiteShard -> the vortex, Home open (to be continued).
    //  stuckmoon (rule 12 with low gravity), fix81moon, perfmoon, moondiag.
    public sealed partial class ExeCheck
    {
        int ShardsMO => SaveGame.Current.World(WorldData.Moon).shards.Count;
        Vector3 SpotMO(string id) => Moon.ShardPos(WorldData.Load(WorldData.Moon).shards.First(s => s.id == id).position);
        IEnumerator MShardMO(string id, float tol = .6f, float timeout = 15f)
        {
            if (Has(id)) yield break;
            yield return GoF(SpotMO(id), tol, timeout);
            yield return WaitFor(() => Has(id), 1.5f);
            Ok(Has(id), $"{id} ({Game.World.shards.First(s => s.id == id).note}) at {Fmt(Player.transform.position)}; {ShardsMO} here, {SaveGame.Current.Total} in all");
        }
        static readonly string[] SevenWorlds = { WorldData.IceAge, WorldData.Egypt, WorldData.Bethlehem, WorldData.Medieval, WorldData.Pirate, WorldData.Steampunk, WorldData.Tomorrowland };

        IEnumerator MoonEnter(string cp)
        {
            var pr = SaveGame.Current.World(WorldData.Moon); if (!pr.opened.Contains(Moon.ArrivedFlag)) pr.opened.Add(Moon.ArrivedFlag);
            foreach (var a in new[] { Abilities.MuscleMan, Abilities.Rocket }) if (!SaveGame.Current.Has(a)) SaveGame.Current.abilities.Add(a);
            var tl = SaveGame.Current.World(WorldData.Tomorrowland); foreach (var f in new[] { Tomorrowland.ArrivedFlag, Tomorrowland.LaunchFlag }) if (!tl.opened.Contains(f)) tl.opened.Add(f);
            SaveGame.Current.checkpoint = cp; yield return StartWorld(WorldData.Moon); yield return WaitFor(() => !Game.I.Busy, 10f);
            if (!P.Companion) P.AddCompanion(Player.transform.position + Vector3.back * 3f, Quaternion.identity);
            yield return Wait(.8f);
        }

        IEnumerator MoonOnly()
        {
            yield return WorldsDone(SevenWorlds, WorldData.Moon);
            Cutscenes.Started -= OnPlayed; Cutscenes.Started += OnPlayed;
            float t0 = Time.time;
            Head("Round 13, item 2: from Tomorrowland's fuel socket: RocketLaunch, then MoonLanding (no vortex between)");
            var tlp = SaveGame.Current.World(WorldData.Tomorrowland); tlp.opened.Remove(Tomorrowland.LaunchFlag); tlp.storyShard = true; tlp.necklaceAwake = true;
            SaveGame.Current.checkpoint = Tomorrowland.TerminalCp; yield return StartWorld(WorldData.Tomorrowland); yield return WaitFor(() => !Game.I.Busy, 10f);
            yield return TpSettle(Tomorrowland.SocketAt + new Vector3(0, .1f, -6f), 0); yield return Go(Tomorrowland.SocketAt + new Vector3(0, 0, -2f), .6f, 8f);
            yield return WaitFor(() => Cutscenes.Playing == "RocketLaunch", 6f);
            Ok(Cutscenes.Playing == "RocketLaunch", "the pink shard at the fuel socket: RocketLaunch");
            yield return WaitFor(() => Cutscenes.Playing == "MoonLanding", 40f);
            Ok(Game.InMoon && Cutscenes.Playing == "MoonLanding", $"the rocket flies straight to the Moon: MoonLanding (world {Game.World.worldId}; the vortex never visited)");
            foreach (var (t, n) in new[] { (2.6f, "600a_rocket_comes_down"), (4.7f, "600b_touchdown_dust"), (6.4f, "600c_helmets_on"), (8.8f, "600d_hatch_opens"), (11.0f, "600e_bounding_down"), (14.4f, "600f_earth_rises_mm_jumps") })
            { yield return WaitFor(() => Cutscenes.Director && Cutscenes.Director.time >= t || Cutscenes.Playing == null, 30f); yield return Shot(n); }
            yield return WaitFor(() => !Game.InCutscene && !Game.I.Busy, 30f); yield return Wait(1f);
            var kira = P.Heroes.First(h => h.Stats.displayName == "Kira");
            Ok(Game.Playing && SaveGame.Current.World(WorldData.Moon).opened.Contains(Moon.ArrivedFlag), $"play starts at the landing site ({Fmt(Player.transform.position)})");
            Ok(kira.Form && kira.Form.id == KiraForm.Space, $"Kira is in her spacesuit (model {(kira.Model ? kira.Model.name : "graybox")}, abilities [{string.Join(", ", kira.Form.abilities)}]); Dan wears {P.Heroes[0].Model?.name}");
            int helmets = P.Everyone.Count(h => h && h.Model && h.Model.GetComponentsInChildren<Transform>().Any(t => t.name == "Bubble helmet"));
            Ok(helmets == P.Everyone.Count(h => h && h.Model), $"every hero wears a bubble helmet ({helmets} of {P.Everyone.Count(h => h && h.Model)}; {Helmet.Made} made)");
            Ok(Music.Track == "moon", $"Dan's moon song plays ({Music.Track}; {(Music.HasClip ? "loaded" : "MISSING")})");
            yield return Shot("601_landing_site_player_view");
            yield return SwitchTo(0); yield return TpSettle(Moon.OnGround(10, 0) + Vector3.up * .1f, 0); Btn(JumpBtn, true); yield return WaitFor(() => !M.Grounded, 1f); yield return Wait(1.2f); Btn(JumpBtn, false); yield return WaitFor(() => M.Grounded, 6f);   // (held: a tap cuts the jump short)
            Ok(M.LastJumpHeight > 2.4f && M.LastAirTime > 1.2f, $"low gravity: Dan's jump rises {M.LastJumpHeight:0.0} m and lasts {M.LastAirTime:0.0} s (on Earth ~1.6 m, 0.6 s)");
            Log($"  HUD: {Hud.TotalShown} / {Hud.UnderTotal}");

            Head("Round 13, item 4: KiraFloats on the way to the rover; space Kira floats up a pillar (Abi can't)");
            yield return TpSettle(Moon.OnGround(Moon.FloatsAt.x + 12, Moon.FloatsAt.z + 12) + Vector3.up * .1f, 225); yield return Go(Moon.OnGround(Moon.FloatsAt.x, Moon.FloatsAt.z), 3f, 12f);
            yield return WaitFor(() => Cutscenes.Playing == "KiraFloats", 6f);
            foreach (var (t, n) in new[] { (2.6f, "610a_up_and_up"), (5.6f, "610b_abi_catches_her"), (7.8f, "610c_back_down") })
            { yield return WaitFor(() => Cutscenes.Director && Cutscenes.Director.time >= t || Cutscenes.Playing == null, 15f); yield return Shot(n); }
            yield return WaitFor(() => !Game.InCutscene && !Game.I.Busy, 15f);
            Ok(played.Contains("KiraFloats"), "KiraFloats played on the way to the rover");
            var pillar = new Vector3(110, Moon.Height(110, 70), 70);   // (the ground, not the pillar's top)
            yield return SwitchTo(1); yield return TpSettle(pillar + new Vector3(0, .1f, -2.6f), 0);
            float abiTop = 0; Btn(JumpBtn, true); Stick(StickFor(Vector3.forward) * .5f); yield return WaitFor(() => M.Velocity.y < .5f && !M.Grounded, 2f); Btn(JumpBtn, false); yield return null; Btn(JumpBtn, true);
            for (float t = 0; t < 4f && (t < .3f || !M.Grounded); t += Time.deltaTime) { abiTop = Mathf.Max(abiTop, Player.transform.position.y); yield return null; }
            Btn(JumpBtn, false); Stick(Vector2.zero); yield return Wait(.5f);
            Ok(abiTop < pillar.y + Moon.PillarY - .3f, $"Abi's double jump tops out at {abiTop - pillar.y:0.0} m: short of the {Moon.PillarY} m pillar");
            yield return SwitchTo(2); yield return TpSettle(pillar + new Vector3(0, .1f, -2.4f), 0);
            int f0 = M.Floats; Btn(JumpBtn, true); Stick(StickFor(Vector3.forward) * .35f);
            yield return WaitFor(() => Player.transform.position.y > pillar.y + Moon.PillarY + .3f, 6f);
            for (float t = 0; t < 4f && !(M.Grounded && t > .3f); t += Time.deltaTime)   // over the top, steer to its middle until she lands
            { var to = pillar + new Vector3(0, 0, 0) - Player.transform.position; to.y = 0; Stick(to.magnitude > .3f ? StickFor(to.normalized) * .6f : Vector2.zero); if (t > .6f) Btn(JumpBtn, false); yield return null; }
            Btn(JumpBtn, false); Stick(Vector2.zero); yield return Wait(.3f);
            Ok(M.Floats > f0 && Player.transform.position.y > pillar.y + Moon.PillarY - .4f, $"space Kira floats up onto the pillar ({Player.transform.position.y - pillar.y:0.0} m)");
            yield return Shot("611_kira_on_pillar");
            yield return MShardMO("F01", .4f, 4f); yield return MShardMO("F02", .4f, 4f);

            Head("Round 13, items 5, 8: a bouncy crater rim; the moon critters (a bump; a bop: they roll away)");
            yield return SwitchTo(0); int b0 = MoonBounce.Bounces;
            var rim = Object.FindObjectsByType<MoonBounce>(FindObjectsSortMode.None).First();
            yield return TpSettle(rim.transform.position + Vector3.up * 3f, 0); yield return WaitFor(() => MoonBounce.Bounces > b0, 4f); yield return Wait(.6f);
            Ok(MoonBounce.Bounces > b0, $"landed on a glowing crater rim: a big floaty bounce ({Fmt(Player.transform.position)})");
            yield return Shot("620_rim_bounce");
            var crit = Enemy.All.First(e => e && e.kind == Enemy.Kind.MoonCritter && e.isActiveAndEnabled && !e.Rolled);
            int cb = Enemy.CritterBumps, hearts = Health.Hearts;
            for (int i = 0; i < 6 && Enemy.CritterBumps == cb; i++) { yield return TpSettle(crit.transform.position + crit.transform.forward * 1.5f + Vector3.up * .1f, crit.transform.eulerAngles.y + 180); yield return WaitFor(() => Enemy.CritterBumps > cb, 2f); }
            Ok(Enemy.CritterBumps > cb && Health.Hearts == hearts, $"a moon critter bounces into Dan: knocked back, no heart lost ({Enemy.CritterBumps - cb} bumps, {Health.Hearts} hearts)");
            int cr = Enemy.CritterRolls;
            for (int i = 0; i < 10 && Enemy.CritterRolls == cr; i++) { yield return WaitFor(() => crit.transform.position.y < Moon.Ground(crit.transform.position.x, crit.transform.position.z) + .2f, 2f); yield return TpSettle(crit.transform.position - crit.transform.forward * .9f + Vector3.up * .1f, crit.transform.eulerAngles.y); yield return Press(AttackBtn); yield return Wait(.4f); }   // (between its hops)
            Ok(Enemy.CritterRolls > cr, "bopped, it curls up and rolls away like a little moon");
            yield return Wait(.6f); yield return Shot("621_critter_rolls");

            Head("Round 13, item 7: Muscle Man (the airlock door, a moon boulder, the mesa boost); the base; the Violinist");
            yield return MMJob(Moon.OnGround(-90, 55.2f) + Vector3.up * .1f, 0, "MO-airlock"); yield return Shot("630_airlock_open");
            yield return Go(new Vector3(-90, .3f, 72), .8f, 12f);
            Ok(Player.transform.position.z > 66f, $"through the airlock into the base ({Fmt(Player.transform.position)})");
            yield return Shot("631_base_hub");
            foreach (var id in new[] { "K03", "K01", "K02", "K04" }) yield return MShardMO(id, .6f, 12f);
            foreach (var p in new[] { new Vector3(-90, .15f, 84), new Vector3(-90, .15f, 92.5f), new Vector3(-90, .15f, 104) }) yield return Go(p, .6f, 8f);   // through the hub's north door to the garden
            foreach (var id in new[] { "K05", "K06", "K07", "K08" }) yield return MShardMO(id, .6f, 12f);
            foreach (var p in new[] { new Vector3(-90, .15f, 104), new Vector3(-90, .15f, 92.5f), new Vector3(-90, .15f, 80), new Vector3(-77.5f, .15f, 80), new Vector3(-69, .15f, 80) }) yield return Go(p, .6f, 8f);   // back, and east to the lab
            foreach (var id in new[] { "K12", "K11", "K09", "K10" }) yield return MShardMO(id, .6f, 12f);
            yield return Go(new Vector3(-86f, .3f, 118f), .6f, 8f);
            yield return MMJob(Moon.OnGround(141.2f, -60) + Vector3.up * .1f, 90, "MO-boulder-a"); yield return MShardMO("B01", .6f, 10f); yield return MShardMO("B02", .6f, 8f);
            yield return SwitchTo(0); yield return TpSettle(Moon.OnGround(30, -76.5f) + Vector3.up * .1f, 180); yield return Go(Moon.OnGround(30, -78f), .4f, 4f, .5f);
            yield return WaitFor(() => P.Companion && Vector3.Distance(P.Companion.transform.position, Player.transform.position) < 6f, 5f);
            yield return Press(AttackBtn); yield return WaitFor(() => Game.I.Busy, 3f); yield return WaitFor(() => !Game.I.Busy, 8f); yield return WaitFor(() => M.Grounded, 8f); yield return Wait(.5f);
            Ok(Player.transform.position.y > Moon.MesaY - .4f, $"Muscle Man boosts Dan up onto the mesa ({Fmt(Player.transform.position)})");
            foreach (var id in new[] { "M01", "M02", "M03" }) yield return MShardMO(id, .6f, 8f);
            yield return SwitchTo(1); yield return TpSettle(Moon.OnGround(Moon.ViolinistAt.x + 3, Moon.ViolinistAt.z - 3) + Vector3.up * .1f, 315);
            yield return Go(Moon.OnGround(Moon.ViolinistAt.x + 1.2f, Moon.ViolinistAt.z - 1.4f), .5f, 6f, .6f);
            yield return WaitFor(() => played.Contains("ViolinistPlaysMoon"), 6f); yield return Wait(2f); yield return Shot("640_violinist_moon");
            yield return WaitFor(() => !Game.I.Busy && !Game.InCutscene, 15f);
            Ok(SaveGame.Current.Has(Abilities.MoonTune), $"the Violinist plays his floaty tune ({Hud.LastMessage})");

            Head("Round 13, items 3, 5: the rest of the Moon (the landing site, the craters, the deep crater, the ridge)");
            foreach (var id in new[] { "L01", "L02", "L03", "L04", "L07", "L08", "L09", "C01", "C02", "C03", "C05", "D01", "D02", "D04", "R01", "R02" })
            { var sp = SpotMO(id); yield return TpSettle(sp + new Vector3(0, -.9f, -2.5f), 0); yield return MShardMO(id, .6f, 10f); }
            Log($"  {ShardsMO} from the Moon, {SaveGame.Current.Total} in all (threshold {Game.World.areas[0].requirement}, {Game.World.areas[0].fromHere} from here)");

            Head("Round 13, item 10: the necklace, the rover; Dan puzzles; Abi solves the light puzzle; WhiteShard; Home open");
            yield return SwitchTo(0); yield return TpSettle(Moon.OnGround(-10, -10) + Vector3.up * .1f, 225);
            yield return WaitFor(() => played.Contains("NecklaceWakesMoon"), 12f);
            Ok(played.Contains("NecklaceWakesMoon") && SaveGame.Current.World(WorldData.Moon).necklaceAwake, $"the necklace wakes at {SaveGame.Current.Total} in all, {ShardsMO} from here (NecklaceWakesMoon, white)");
            yield return WaitFor(() => !Game.I.Busy && !Game.InCutscene, 15f); yield return Wait(.5f);
            Ok(Necklace.Target == Game.I.StoryShard.transform, $"it leads to the rover (target {(Necklace.Target ? Necklace.Target.name : "none")})");
            yield return Shot("650_necklace_to_rover");
            var pa = new Vector3(Moon.PanelAt.x, Moon.PlateauY, Moon.PanelAt.z);
            yield return TpSettle(pa + new Vector3(0, .1f, 3f), 180); yield return Go(pa + new Vector3(0, 0, 1.4f), .4f, 4f, .5f); yield return Wait(1.5f);
            Ok(!Moon.Panel.Active && Hud.LastMessage.Contains("Switch to Abi"), $"Dan at the panel: \"{Hud.LastMessage}\"");
            yield return Shot("651_dan_puzzles");
            yield return SwitchTo(1); yield return TpSettle(pa + new Vector3(0, .1f, 2.6f), 180); yield return Go(pa + new Vector3(0, 0, 1.4f), .4f, 4f, .5f);
            yield return WaitFor(() => Moon.Panel.Active, 4f);
            Ok(Moon.Panel.Active, "Abi at the panel: the light puzzle starts");
            yield return Shot("652_abi_at_the_panel");
            bool wrongDone = false;
            for (int round = 0; round < 3; round++)
            {
                yield return WaitFor(() => !Moon.Panel.Showing && Moon.Panel.Round == round, 12f); yield return Wait(.3f);
                var pat = Moon.Panel.Pattern.ToArray();
                if (!wrongDone && round == 1)
                {
                    int w0 = Moon.Panel.Wrongs; yield return Flick((pat[0] + 1) % 4); yield return WaitFor(() => Moon.Panel.Wrongs > w0, 1f);
                    Ok(Moon.Panel.Wrongs > w0, "a wrong press: the panel buzzes and plays the pattern again"); wrongDone = true;
                    yield return WaitFor(() => Moon.Panel.Showing, 2f); yield return WaitFor(() => !Moon.Panel.Showing, 12f); yield return Wait(.3f);
                }
                foreach (var kk in pat) yield return Flick(kk);
                Log($"  round {round + 1}: {pat.Length} lights repeated ({string.Join(" ", pat)})");
                if (round == 2) break;
            }
            yield return WaitFor(() => Cutscenes.Playing == "WhiteShard", 6f);
            Ok(RoverPanel.Solved > 0 && Cutscenes.Playing == "WhiteShard", $"three rounds (4, 6, 8 lights) solved: WhiteShard ({Moon.Panel.Wrongs} wrong presses)");
            foreach (var (t, n) in new[] { (1.6f, "653a_abi_at_the_panel"), (4.4f, "653b_the_shard_answers"), (7.2f, "653c_every_shard_lights_up"), (10.6f, "653d_light_fills_the_moon") })
            { yield return WaitFor(() => Cutscenes.Director && Cutscenes.Director.time >= t || Cutscenes.Playing == null, 20f); yield return Shot(n); }
            yield return WaitFor(() => !Game.I.Busy && !Game.InCutscene, 25f); yield return Wait(1f);
            var home = VortexDoor.All.First(d => d.info.id == "Home"); var moon = VortexDoor.All.First(d => d.info.id == "Moon");
            Ok(Vortex.Inside(Player.transform.position) && SaveGame.Current.StoryShards == 8 && !home.Blocked, $"into the vortex with all eight story shards; Home is open (\"{home.label.text.Replace("\n", " ")}\")");
            Ok(!moon.Blocked, "the Moon's path stays open");
            yield return Shot("654_vortex_home_open");
            yield return Go(VPath(Yaw("Home"), 4), .8f); yield return Go(VPath(Yaw("Home"), 19), .6f);
            int tb = Game.I.ToBeContinuedShown; yield return Go(VPath(Yaw("Home"), 24), .5f, 6f, 1f, () => Game.I.Busy);
            yield return WaitFor(() => Game.I.ToBeContinuedShown > tb, 6f);
            Ok(Game.I.ToBeContinuedShown > tb, "Home: to be continued (Acts X and XI and the credits are the next round)");
            yield return WaitFor(() => !Game.I.Busy, 10f);
            Log($"  the Moon from the launch to the vortex took the bot {(Time.time - t0) / 60f:0.0} min");
        }

        // one stick flick at the rover's panel: 0 up, 1 right, 2 down, 3 left
        IEnumerator Flick(int k)
        {
            var v = k == 0 ? new Vector2(0, 1) : k == 1 ? new Vector2(1, 0) : k == 2 ? new Vector2(0, -1) : new Vector2(-1, 0);
            Stick(v); yield return null; yield return null; yield return null; Stick(Vector2.zero); yield return null; yield return null; yield return Wait(.12f);
        }

        IEnumerator MinOnMoon()
        {
            yield return WaitFor(() => Game.InMoon && !Game.I.Busy && !Game.InCutscene, 60f); yield return UntilFree(40f);
            Ok(Game.InMoon && Game.Playing, "RocketLaunch and MoonLanding (skipped): on the Moon");
            yield return MinimumHere("the Moon");
            Ok(SaveGame.Current.Total == 200, $"the minimum player wakes the Moon's necklace at {SaveGame.Current.Total} shards in all (8 x 25)");
            yield return SwitchTo(1); Health.Refill();
            var pa = new Vector3(Moon.PanelAt.x, Moon.PlateauY, Moon.PanelAt.z);
            yield return TpSettle(pa + new Vector3(0, .1f, 2.6f), 180); yield return Go(pa + new Vector3(0, 0, 1.4f), .4f, 4f, .5f);
            yield return WaitFor(() => Moon.Panel.Active, 4f);
            for (int round = 0; round < 3; round++)
            {
                yield return WaitFor(() => !Moon.Panel.Showing && Moon.Panel.Round == round, 12f); yield return Wait(.3f);
                foreach (var kk in Moon.Panel.Pattern.ToArray()) yield return Flick(kk);
            }
            yield return WaitFor(() => Game.I.Busy || Game.InCutscene, 4f); yield return UntilFree(60f);
            yield return WaitFor(() => Vortex.Inside(Player.transform.position), 8f);
            Ok(SaveGame.Current.World(WorldData.Moon).storyShard && Vortex.Inside(Player.transform.position) && SaveGame.Current.StoryShards == 8, $"the white shard (the light puzzle, WhiteShard), in the vortex with all {SaveGame.Current.StoryShards} story shards");
            yield return Shot("min_done_moon");
        }

        IEnumerator StuckMoon() { yield return EnsurePlaying(false); yield return MoonEnter(Moon.Start); yield return StuckWorld(WorldData.Moon, null); }

        IEnumerator PerfMoon()
        {
            Head($"Round 13, item 11: frame rate on the Moon, the full view drawn every frame ({(TestRun.Headless ? $"{TestRun.View.width} x {TestRun.View.height} off-screen" : "window")})");
            if (!Game.InMoon) { yield return EnsurePlaying(false); yield return MoonEnter(Moon.Start); }
            yield return SwitchTo(0);
            float worstAvg = 9999; string where = "";
            foreach (var (name, from, path) in new[]
            {
                ("the landing site (the rocket, Earth, the stars)", new Vector3(0, 0, 0), new[] { new Vector3(20, 0, 10), new Vector3(-20, 0, 10), new Vector3(0, 0, -10) }),
                ("the crater field", new Vector3(50, 0, 0), new[] { new Vector3(100, 0, 0), new Vector3(120, 0, 40), new Vector3(60, 0, 30) }),
                ("the moon base (inside the hub)", new Vector3(-90, .4f, 74), new[] { new Vector3(-84, .3f, 84), new Vector3(-96, .3f, 84), new Vector3(-90, .3f, 72) }),
                ("the rover's plateau (the whole Moon)", new Vector3(-56, 16.2f, -84), new[] { new Vector3(-66, 16, -88), new Vector3(-56, 16, -94) }),
            })
            {
                yield return TpSettle(new Vector3(from.x, Mathf.Max(from.y, Moon.Ground(from.x, from.z)) + .1f, from.z), 0);
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
                yield return Shot("690_perf_" + name.Split('(')[0].Trim().Replace(' ', '_'));
            }
            float need = SteamDeck.On ? 40f : 60f;
            Ok(worstAvg > need, $"the Moon: {worstAvg:0} fps at the slowest place ({where}); need {need:0} ({SystemInfo.graphicsDeviceName}, {Screen.width}x{Screen.height})");
        }

        IEnumerator MoonDiag()
        {
            yield return EnsurePlaying(false);
            yield return MoonEnter(Moon.Start);
            yield return Wait(1.5f);
            yield return Shot("d0_landing_player_view");
            // (what is drawn in the air over the rocket: the black disc in the first look)
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                if (r.enabled && r.gameObject.activeInHierarchy && r.bounds.max.y > 22f && r.bounds.min.y < 45f && Mathf.Abs(r.bounds.center.x) < 30f && Mathf.Abs(r.bounds.center.z - 24f) < 30f)
                    Log($"  over the rocket: {r.name} ({r.GetType().Name}) under {(r.transform.parent ? r.transform.parent.name : "-")}, bounds {Fmt(r.bounds.min)}..{Fmt(r.bounds.max)}");
            yield return CloseUpAt(new Vector3(-12, 3, -8), new Vector3(0, 10, 24), "d1_rocket_on_the_moon");
            yield return CloseUpAt(new Vector3(0, 2, -10), new Vector3(260, 620, 1700), "d2_earth");
            yield return CloseUpAt(new Vector3(70, 6, -6), new Vector3(100, -2, 20), "d3_deep_crater");
            yield return CloseUpAt(new Vector3(-90, 4, 40), new Vector3(-90, 3, 80), "d4_moon_base");
            yield return CloseUpAt(new Vector3(-90, 2, 72), new Vector3(-90, 2, 112), "d5_base_inside");
            yield return CloseUpAt(new Vector3(-30, 6, -50), new Vector3(-60, 16, -90), "d6_ridge_and_rover");
            yield return CloseUpAt(new Vector3(-60, 18, -80), new Vector3(-60, 17, -86), "d7_rover_panel");
            yield return CloseUpAt(new Vector3(100, 3, 60), new Vector3(110, 6, 70), "d8_float_pillar");
            yield return CloseUpAt(new Vector3(20, 4, -70), new Vector3(30, 9, -90), "d9_mesa");
        }
    }
}
