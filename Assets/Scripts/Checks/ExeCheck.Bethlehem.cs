using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace ShardsOfTime
{
    // Round 7, items 1-13: Bethlehem as a player plays it, with the virtual pad from the vortex: the roof-crash arrival, the farmyard
    // (the loft, the bale tower, lamb Kira's bounces up the hay stack, onto the barn's roof and up through the hole), the hills (the
    // Violinist teaches Kira the pounce on his harp; the pounce across the rock pillars; a goat's charge), the road (the Wise Men), the
    // village (a dog barks for a watchman, the inn's "no room", the rooftops, the pounce to the far roof, the watchmen's yard: Dan is
    // caught, lamb Kira walks in with the flock), the necklace, the golden shard in the manger, WiseMenArrive, the vortex (the Medieval
    // door closed); then Egypt's far roof with the pounce. Mode "bethlehem" starts in the vortex (a save with the Ice Age and Egypt
    // done to their story shards, their first-visit shards counted); "full" plays the Ice Age and Egypt first.
    public sealed partial class ExeCheck
    {
        int ShardsBE => SaveGame.Current.World(WorldData.Bethlehem).shards.Count;
        int NeedBE => WorldData.Load(WorldData.Bethlehem).areas[0].requirement;
        Vector3 BE(float x, float z) => Bethlehem.OnGround(x, z);
        Vector3 SpotBE(string id) => Bethlehem.ShardPos(WorldData.Load(WorldData.Bethlehem).shards.First(s => s.id == id).position);

        IEnumerator BethlehemOnly()
        {
            yield return EnsurePlaying(false);
            // the Ice Age and Egypt done to their story shards, with each one's first-visit shards (as a save would have them)
            foreach (var wid in new[] { WorldData.IceAge, WorldData.Egypt })
            {
                var prog = SaveGame.Current.World(wid); prog.storyShard = true; prog.necklaceAwake = true;
                var wd = WorldData.Load(wid); var fv = wd.areas[0].firstVisit;
                foreach (var sp in wd.shards) if (sp.requires.All(fv.Contains) && !prog.shards.Contains(sp.id)) prog.shards.Add(sp.id);
                if (!SaveGame.Current.worldsReached.Contains(wid)) SaveGame.Current.worldsReached.Add(wid);
            }
            SaveGame.Current.World(WorldData.Egypt).opened.Add("EG-arrived");
            foreach (var p in new[] { "IA-vortex", "EG-vortex-gate" }) if (!SaveGame.Current.portals.Contains(p)) SaveGame.Current.portals.Add(p);
            Log($"  (set up: the Ice Age {SaveGame.Current.World(WorldData.IceAge).shards.Count} + Egypt {SaveGame.Current.World(WorldData.Egypt).shards.Count} = {SaveGame.Current.Total} shards, both story shards)");
            SaveGame.Current.checkpoint = Vortex.Checkpoint; Checkpoint.RespawnParty(); CameraRig.I.SnapBehind();
            yield return Wait(.6f);
            yield return BethlehemRoute();
        }

        IEnumerator FullBethlehem()
        {
            yield return IceAgePlay();
            yield return EgyptRoute(false);
            // Egypt's route ends in the Ice Age (its double-jump ledge): back into the vortex the way a player goes
            SaveGame.Current.checkpoint = Vortex.Checkpoint; Checkpoint.RespawnParty(); CameraRig.I.SnapBehind();
            yield return Wait(.6f);
            yield return BethlehemRoute();
            yield return PerfBethlehem();
        }

        IEnumerator BethlehemRoute()
        {
            Cutscenes.Started -= OnPlayed; Cutscenes.Started += OnPlayed;
            float t0 = Time.time; int spottedBefore = 0;
            Head("Round 7, item 3: the vortex's Bethlehem path, the arrival");
            var door = VortexDoor.All.FirstOrDefault(d => d.info.id == "Medieval");
            Ok(door && door.Blocked, $"the vortex has a Medieval Kingdom path, closed until Bethlehem's golden shard ({(door ? door.label.text.Replace("\n", " ") : "none")})");
            float by = Yaw("Bethlehem");   // Round 8: the paths moved (six now)
            yield return Go(VPath(0, 4), .8f); yield return Go(VPath(by, 8), .6f); yield return Go(VPath(by, 19), .6f);
            yield return Go(VPath(by, 24), .5f, 6f, 1f, () => Game.I.Busy);
            yield return WaitFor(() => Cutscenes.Playing == "BethlehemArrival", 8f);
            Ok(Game.InBethlehem && Cutscenes.Playing == "BethlehemArrival", $"the Bethlehem path loads Bethlehem and plays its arrival (world {Game.World.worldId}, cutscene {Cutscenes.Playing ?? "none"})");
            foreach (var (t, n) in new[] { (1.0f, "120a_the_star"), (4.5f, "120b_golden_spiral"), (10f, "120c_ring_over_barn"), (15f, "120d_crash_top_down"), (16.6f, "120e_wise_men_puzzled"), (19.5f, "120f_inside") })
            {
                yield return WaitFor(() => Cutscenes.Director && Cutscenes.Director.time >= t || Cutscenes.Playing == null, 30f);
                yield return Shot(n);
            }
            yield return WaitFor(() => !Game.InCutscene && !Game.I.Busy, 15f);
            yield return Wait(.5f);
            var kira = P.Heroes[2];
            Ok(P.Heroes[0].Outfit == Outfits.Bethlehem && P.Heroes[1].Outfit == Outfits.Bethlehem && kira.Form && kira.Form.id == KiraForm.Lamb,
               $"dressed for Bethlehem (Dan {(P.Heroes[0].Model ? P.Heroes[0].Model.name : "graybox")}, Abi {(P.Heroes[1].Model ? P.Heroes[1].Model.name : "graybox")}); Kira is {(kira.Form ? kira.Form.displayName : "-")} ({(kira.Model ? kira.Model.name : "graybox")})");
            Ok(NecklaceModel.Tint.r > .9f && NecklaceModel.Tint.g > .6f && NecklaceModel.Tint.b < .4f, $"the necklace's shard is gold here (tint {NecklaceModel.Tint})");
            MusicCheck("Bethlehem", "bethlehem");
            Ok(Player.transform.position.y < 1f && Mathf.Abs(Player.transform.position.z - 29.5f) < 2f, $"play starts inside the barn, on its floor ({Fmt(Player.transform.position)})");
            yield return Shot("121_in_the_barn");
            var rr = Game.World.RequirementReport("BE"); Log("  " + rr.detail);
            Ok(rr.ok && Game.World.shards.Count == 60, $"item 9 / rule 2 (universal total): {rr.reachable} available on arrival (Ice Age + Egypt + Bethlehem first visit), threshold {rr.requirement} (180 shards exist up to here: {100f * rr.requirement / 180f:0}%)");
            Ok(Hud.UnderTotal == $"Next: {NeedBE}", $"HUD on arrival: {Hud.TotalShown}, under it \"{Hud.UnderTotal}\"");

            yield return Farmyard();
            yield return LambBounces();
            yield return HillsAndPounce();
            yield return GoatCharge();
            yield return RoadAndWiseMen();
            yield return Village();
            Log($"  Bethlehem took the bot {(Time.time - t0) / 60f:0.0} min so far; {ShardsBE} shards here, {SaveGame.Current.Total} in all ({string.Join(" ", SaveGame.Current.World(WorldData.Bethlehem).shards)}); hits {Health.HitsTaken}, respawns {Health.Respawns}");
            yield return GoldenShard();
            yield return PounceInEgypt();
            Log("  " + TestRun.Report());
            _ = spottedBefore;
        }

        // through waypoints (fence gates, alleys between the houses), as a player walks round things
        IEnumerator Via(params Vector3[] pts) { foreach (var p in pts) yield return GoF(XZ(p.x, p.z), 1.2f, 45f); }

        IEnumerator BShard(string id, params Vector3[] via)
        {
            yield return Via(via);
            var at = SpotBE(id); float tol = .5f;
            yield return GoF(at, tol, 40f);
            yield return WaitFor(() => Has(id), 1.5f);
            Ok(Has(id), $"{id} ({Game.World.shards.First(s => s.id == id).note}) at {Fmt(Player.transform.position)}; {ShardsBE} here, {SaveGame.Current.Total} in all");
            yield return NecklaceIfAwakeBE();
        }

        bool necklaceSeenBE;
        IEnumerator NecklaceIfAwakeBE()
        {
            if (necklaceSeenBE || SaveGame.Current.Total < NeedBE) yield break;
            yield return WaitFor(() => played.Contains("NecklaceWakesBethlehem"), 10f);
            if (!played.Contains("NecklaceWakesBethlehem")) yield break;
            necklaceSeenBE = true;
            Ok(SaveGame.Current.World(WorldData.Bethlehem).necklaceAwake, $"the necklace wakes at {SaveGame.Current.Total} shards in all ({ShardsBE} from Bethlehem; threshold {NeedBE}) (NecklaceWakesBethlehem, gold)");
            yield return Wait(2f); yield return Shot("140_necklace_wakes");
            yield return WaitFor(() => !Game.I.Busy && !Game.InCutscene, 15f);
        }

        // jump onto a springy pile (lamb Kira bounces) and steer to `dest` in the air; true if she went up at least `minRise`
        float lastApex;
        IEnumerator BounceTo(Vector3 pad, Vector3 dest, float minRise = 3f, float steerAbove = -99f)
        {
            int b0 = BouncePad.Bounces; float y0 = Player.transform.position.y; lastApex = y0;
            yield return Go(pad + (Player.transform.position - pad).normalized * 2.2f, .5f, 15f);
            yield return JumpTo(pad, () => true, .45f, .35f, false, 4f);
            yield return WaitFor(() => BouncePad.Bounces > b0, 1.5f);
            float end = Time.time + 4f;
            while (Time.time < end)
            {
                lastApex = Mathf.Max(lastApex, Player.transform.position.y);
                var d = dest - Player.transform.position; d.y = 0;
                // (a player steers once she's above what's in the way: the barn's eaves)
                Stick(Player.transform.position.y < steerAbove ? Vector2.zero : StickFor(d.normalized) * Mathf.Clamp(d.magnitude / 1.2f, 0f, 1f));
                if (M.Grounded && Time.time > end - 3.3f) break;
                yield return null;
            }
            Stick(Vector2.zero);
            Log($"  bounce off {Fmt(pad)}: {BouncePad.Bounces - b0} bounce(s), up {lastApex - y0:0.0} m, landed at {Fmt(Player.transform.position)}");
        }

        IEnumerator Farmyard()
        {
            Head("Round 7, items 2 and 4: the barn and the farmyard");
            var star = Object.FindObjectsByType<StarRing>(FindObjectsSortMode.None).FirstOrDefault();
            Ok(star && star.transform.position.y > 25f, $"the golden vortex (\"the star\") hovers over the barn at {(star ? Fmt(star.transform.position) : "-")}");
            yield return SwitchTo(0);
            // the loft: up the hay-bale steps by the west wall
            yield return Go(BE(-5f, 34.6f) + Vector3.zero, .5f, 10f);
            foreach (var z in new[] { 36.2f, 37.4f, 38.6f, 39.6f, 41f }) yield return JumpTo(new Vector3(-5f, 0, z), () => true, .4f, .4f);
            yield return Go(new Vector3(3, 0, 41), .5f, 8f);
            yield return WaitFor(() => Has("F06"), 1f);
            Ok(Has("F06"), $"F06 on the hay loft, up the bales (y {Player.transform.position.y:0.0})");
            yield return Shot("122_hay_loft");
            yield return Go(new Vector3(3, 0, 37.5f), .6f, 5f); yield return WaitFor(() => M.Grounded && Player.transform.position.y < .5f, 4f);
            // out through the door
            yield return Go(new Vector3(0, 0, 27.5f), .6f, 8f); yield return Go(new Vector3(0, 0, 22f), .6f, 8f);
            yield return Shot("123_barn_outside");
            yield return BShard("F01"); yield return BShard("F03"); yield return BShard("F02");
            // the bale tower (normal jumps)
            yield return Go(new Vector3(20, 0, 22.5f), .5f, 15f);
            foreach (var z in new[] { 24.5f, 25.5f, 26.5f, 27.5f, 28.5f, 30f }) yield return JumpTo(new Vector3(20, 0, z), () => true, .4f, .4f);
            yield return WaitFor(() => Has("F07"), 1f);
            Ok(Has("F07"), $"F07 on the bale tower (y {Player.transform.position.y:0.0})");
            yield return Go(new Vector3(20, 0, 34), .6f, 5f);
            // Dan on a wool pile: no bounce for him (a hint)
            int b0 = BouncePad.Bounces;
            yield return JumpTo(Bethlehem.StackBounce, () => Vector3.Distance(Player.transform.position, Bethlehem.StackBounce) < 2.2f, .5f, .35f);
            yield return Wait(1f);
            Ok(BouncePad.Bounces == b0, $"Dan lands on the wool pile and doesn't bounce (\"{Hud.LastMessage}\")");
            yield return BShard("F04"); yield return BShard("F05"); yield return BShard("F10");
            var pen = Flock.All.FirstOrDefault(f => f.name.Contains("the yard"));
            Ok(pen && pen.Sheep.Count == 4, $"a pen of sheep in the yard ({(pen ? pen.Sheep.Count : 0)} sheep, model {(CharacterModel.Exists("sheep") ? "sheep" : "placeholder")})");
            yield return CloseUpAt(pen.Middle + new Vector3(4, 2.2f, -4), pen.Middle + Vector3.up * .5f, "124_sheep");
        }

        IEnumerator LambBounces()
        {
            Head("Round 7, item 6: lamb Kira's springy bounce");
            yield return SwitchTo(2);
            Ok(Player.HasAbility(Abilities.WoolBounce) && Player.HasAbility(Abilities.FlockHide), $"lamb Kira has the wool bounce and the flock hide (form {Player.Form.displayName})");
            yield return CloseUpAt(Player.transform.position + Player.transform.forward * 1.3f + Vector3.up * .5f, Player.transform.position + Vector3.up * .3f, "125_lamb_kira_face");
            // up the big hay stack
            yield return BounceTo(Bethlehem.StackBounce, new Vector3(-22, 0, 20));
            yield return WaitFor(() => Has("F09"), 1.5f);
            Ok(Has("F09") && lastApex > 4.5f, $"F09 on the big hay stack: Kira bounced up {lastApex:0.0} m off the wool pile");
            yield return Shot("126_on_the_hay_stack");
            yield return Go(new Vector3(-16, 0, 20), .6f, 5f); yield return WaitFor(() => M.Grounded && Player.transform.position.y < .5f, 4f);
            yield return Via(XZ(-10, 18), XZ(13, 20), XZ(14, 36));
            // onto the barn's roof (east wall), up to the ridge
            yield return BounceTo(Bethlehem.BarnBounce, new Vector3(4.5f, 0, 36), 3f, 4.8f);
            float roofY = Player.transform.position.y;
            yield return Go(new Vector3(.6f, 0, 37), .4f, 6f, .6f);
            yield return WaitFor(() => Has("F08"), 1f);
            if (!Has("F08")) yield return JumpTo(new Vector3(0, 0, 37), () => true, .3f, .3f);   // a little hop up to it, as a player does
            yield return WaitFor(() => Has("F08"), 1.5f);
            Ok(Has("F08"), $"F08 on the barn's ridge (landed on the roof at y {roofY:0.0}, bounce {lastApex:0.0} m)");
            yield return Go(new Vector3(.3f, 0, 30.5f), .4f, 6f, .5f);
            yield return WaitFor(() => SaveGame.Current.heartUpgrades.Contains("BE-H1"), 1.5f);
            Ok(SaveGame.Current.heartUpgrades.Contains("BE-H1"), $"heart BE-H1 on the ridge ({Health.Max} hearts)");
            yield return Shot("127_on_the_roof");
            yield return Go(new Vector3(9, 0, 30.5f), .6f, 6f); yield return WaitFor(() => M.Grounded && Player.transform.position.y < .5f, 5f);
            yield return Via(XZ(13, 30), XZ(13, 21), XZ(2, 21));
            // inside: the hay pile under the hole bounces her up through the roof
            yield return Go(new Vector3(0, 0, 22), .6f, 10f); yield return Go(new Vector3(-1, 0, 30), .6f, 8f);
            yield return BounceTo(Bethlehem.HayPile, Bethlehem.HayPile + new Vector3(.3f, 0, .3f));
            Ok(lastApex > 6.2f, $"the hay under the roof hole bounces lamb Kira up through it (up to y {lastApex:0.0}; the hole's edge ~6.4)");
            yield return WaitFor(() => M.Grounded, 4f);
            if (Player.transform.position.y > 2f) { yield return Go(new Vector3(-8.5f, 0, 34), .6f, 6f); yield return WaitFor(() => M.Grounded && Player.transform.position.y < .5f, 5f); }
            yield return Via(XZ(-1, 28), XZ(0, 21));
        }

        IEnumerator HillsAndPounce()
        {
            Head("Round 7, items 4 and 7: the hills; the Violinist teaches Kira the pounce");
            yield return BShard("H01", XZ(10, 18), XZ(24, 22), XZ(36, 29)); yield return BShard("H03");
            var v = Violinist.I;
            Ok(v && v.instrument == "Harp" && v.Instrument && v.Instrument.name == "Harp", $"the Violinist by the shepherds' fire with his harp (model {(v ? v.ModelName : "-")}, playing clip {(v ? v.playClip : "-")})");
            yield return SwitchTo(0);
            var front = Bethlehem.ViolinistAt + v.transform.forward * 3.5f;
            yield return Go(BE(front.x, front.z), .6f, 25f); yield return Go(Bethlehem.ViolinistAt + v.transform.forward * 1.7f, .5f, 6f, .5f); yield return Wait(.6f);
            Ok(!Game.InCutscene && Hud.LastMessage.Contains("looking past you"), $"for Dan he only smiles: \"{Hud.LastMessage}\"");
            yield return CloseUp(v.transform, "128_harp_idle");
            yield return Go(BE(front.x, front.z), .6f, 6f);
            yield return SwitchTo(2);
            yield return Go(Bethlehem.ViolinistAt + v.transform.forward * 1.5f, .5f, 15f, .6f, () => Cutscenes.Playing != null);
            yield return WaitFor(() => Cutscenes.Playing == "ViolinistTeachesBethlehem", 10f);
            yield return Wait(1.2f);
            Ok(v.ClipNow == "play" && v.InstrumentPose == "play", $"he plays the harp for Kira (clip {v.ClipNow}, pose {v.InstrumentPose})");
            yield return Shot("129_harp_plays"); yield return CloseUp(v.transform, "129b_harp_closeup");
            yield return WaitFor(() => !Game.I.Busy, 15f);
            Ok(SaveGame.Current.Has(Abilities.Pounce) && M.CanPounce, $"Kira learned the pounce: \"{Hud.LastMessage}\"");
            // a pounce on the flat: Dive, aim, leap
            yield return Go(BE(62, 30), .6f, 10f);
            int p0 = M.Pounces; Stick(StickFor(Vector3.right)); yield return Press(GamepadButton.East);
            yield return WaitFor(() => M.Pounces > p0, 1f); yield return Wait(.25f); yield return Shot("130_pounce");
            yield return WaitFor(() => M.Grounded && !M.Pouncing, 3f); Stick(Vector2.zero);
            Ok(M.Pounces > p0 && M.LastPounceDistance > 9f, $"Kira's pounce on the hillside: {M.LastPounceDistance:0.0} m (a run-and-jump is ~4 m)");
            yield return BShard("H02");
            // the rock pillars: up the near one's steps, pounce to the far one
            yield return Go(BE(160, 118), .7f, 60f);
            for (int k = 0; k < 16 && Player.transform.position.y < Bethlehem.PillarTop - .3f; k++)
                yield return JumpTo(new Vector3(160, 0, Mathf.Min(139f, Player.transform.position.z + 1.6f)), () => true, .4f, .4f);
            Ok(Player.transform.position.y > Bethlehem.PillarTop - .3f, $"up the near pillar's steps (y {Player.transform.position.y:0.0}, top {Bethlehem.PillarTop:0.0})");
            yield return Go(new Vector3(163.6f, 0, 140), .25f, 5f, .4f);
            yield return Shot("131_pillar_gap");
            p0 = M.Pounces; Stick(StickFor(Vector3.right)); yield return Press(GamepadButton.East);
            yield return WaitFor(() => M.Pounces > p0, 1f); yield return WaitFor(() => M.Grounded && !M.Pouncing, 3f); Stick(Vector2.zero);
            yield return Go(new Vector3(177.5f, 0, 139.5f), .4f, 4f, .5f); yield return Go(new Vector3(177.5f, 0, 143f), .4f, 4f, .5f);
            Ok(Has("P01") && SaveGame.Current.heartUpgrades.Contains("BE-H2"), $"P01 and heart BE-H2 on the far pillar, reached with the pounce ({M.LastPounceDistance:0.0} m; y {Player.transform.position.y:0.0})");
            yield return Shot("132_far_pillar");
            yield return Go(new Vector3(184f, 0, 140), .6f, 5f); yield return WaitFor(() => M.Grounded && Player.transform.position.y < Bethlehem.PillarTop - 2f, 6f);
            yield return BShard("H16"); yield return BShard("H09");
        }

        IEnumerator GoatCharge()
        {
            Head("Round 7, item 8: a grumpy goat lowers its head and charges");
            yield return SwitchTo(0);
            var goat = Enemy.All.Where(e => e.kind == Enemy.Kind.Goat && !e.Dead).OrderBy(e => Vector3.Distance(e.transform.position, Player.transform.position)).FirstOrDefault();
            if (!goat) { Fail("no goat"); yield break; }
            int c0 = goat.Charges, h0 = Health.HitsTaken;
            // walk up in front of it and wait
            var at = goat.transform.position + goat.transform.forward * 6f;
            yield return Go(at, .8f, 30f);
            yield return WaitFor(() => goat.Charges > c0, 8f);
            yield return Wait(.3f); yield return Shot("133_goat_charge");
            yield return Wait(1.5f);
            Ok(goat.Charges > c0, $"the goat lowered its head and charged ({goat.Charges - c0} charge(s); {(Health.HitsTaken > h0 ? "it headbutted Dan" : "Dan got out of the way")})");
            yield return Fight(10f, 15f);
            Ok(goat.Dead || goat.HitsTaken > 0, $"Dan fights the goat off (hits {goat.HitsTaken}, {(goat.Dead ? "defeated" : "still standing")})");
            yield return BShard("H14");
        }

        IEnumerator RoadAndWiseMen()
        {
            Head("Round 7, item 4: the Wise Men's caravan on the road");
            var c = Caravan.I;
            float a0 = c ? c.Along : 0; yield return Wait(3f);
            Ok(c && Mathf.Abs(c.Along - a0) > .5f || c && c.Doing != "walk", $"the Wise Men ride the road toward the star (moved {(c ? Mathf.Abs(c.Along - a0) : 0):0.0} m in 3 s, now {(c ? c.Doing : "-")}, stops so far {(c ? c.Stops : 0)})");
            yield return BShard("H15"); yield return BShard("R03");
            int heard = c.LinesHeard; string line = null;
            float chase = Time.time + 90f;
            while (Time.time < chase && !(c.LinesHeard > heard && line != null))   // they keep riding: walk alongside them
            {
                if (Hud.LastMessage.StartsWith("The Wise Men")) line = Hud.LastMessage;
                if (Vector3.Distance(Player.transform.position, c.Lead) > 7f) yield return GoF(c.Lead + (Player.transform.position - c.Lead).normalized * 5f, 2f, 3f);
                else yield return null;
            }
            Ok(c.LinesHeard > heard, $"close to them you hear them bicker ({c.LinesHeard - heard} line(s)): \"{line ?? Hud.LastMessage}\"");
            yield return CloseUpAt(c.Lead + new Vector3(5, 3, 5), c.Lead + Vector3.up * 2.5f, "134_wise_men");
            yield return Shot("134b_wise_men_player_view");
            yield return BShard("R04"); yield return BShard("R05");
        }

        IEnumerator Village()
        {
            Head("Round 7, items 4 and 5: the village, the watchmen, a dog, the inn, lamb Kira in a flock");
            yield return BShard("V16");
            yield return Via(XZ(-62, 84), XZ(-62, 66)); yield return PastGuards(SpotBE("V01")); yield return BShard("V01");
            yield return Shot("135_village_street");
            // a dog barks for a watchman
            var dog = Enemy.All.Where(e => e.kind == Enemy.Kind.Dog && !e.Dead).OrderBy(e => Vector3.Distance(e.transform.position, BE(-95, 100))).First();
            int b0 = dog.Barks, al0 = Guard.All.Sum(g => g.Alerts);
            yield return Via(XZ(-62, 66), XZ(-62, 88)); yield return PastGuards(BE(-80, 89)); yield return Via(XZ(-95, 89.5f)); yield return Go(BE(-95, 96.5f), .6f, 8f);
            yield return WaitFor(() => dog.Barks > b0, 6f); yield return Shot("136_dog_barks");
            Ok(dog.Barks > b0, $"the courtyard's guard dog barks ({dog.Barks - b0}); watchmen called over: {Guard.All.Sum(g => g.Alerts) - al0}");
            yield return Fight(8f, 10f);
            yield return BShard("V06");
            // the inn: no room
            yield return Via(XZ(-95, 91)); yield return PastGuards(BE(-120, 90)); yield return Via(XZ(-120, 90.5f));
            // round the inn's back (the north lane), down its west side to the door (out of the back-street watchman's way)
            yield return Via(XZ(-95, 113), XZ(-152, 113), XZ(-157, 100), XZ(-157, 89.5f)); yield return Go(BE(-150.5f, 89.6f), .5f, 10f); yield return Wait(.6f);
            Ok(NoRoom.Told > 0, $"the inn: \"{Hud.LastMessage}\"");
            yield return Shot("137_no_room");
            var dozer = Guard.All.First(g => g.dozes);
            yield return WaitFor(() => dozer.Dozing, 30f);
            Ok(dozer.Dozing, $"the watchman at the inn door dozes off now and then ({dozer.name})");
            yield return BShard("V08"); yield return Go(BE(-157, 86), .6f, 6f);
            // the rooftops: up an outside stair, the near roof; Kira pounces to the far roof
            yield return SwitchTo(2);
            yield return Via(XZ(-158.1f, 88)); yield return Go(BE(-158.1f, 97.6f), .4f, 15f);
            for (int k = 0; k < 10 && Player.transform.position.y < 4f; k++) yield return JumpTo(new Vector3(-158.1f, 0, Mathf.Min(104f, Player.transform.position.z + 1f)), () => true, .4f, .35f);
            yield return Go(new Vector3(-160.5f, 0, 104), .4f, 4f, .5f); yield return Go(new Vector3(-164.6f, 0, 104), .25f, 6f, .4f);
            Ok(Player.transform.position.y > 4f, $"Kira up on the near roof (y {Player.transform.position.y:0.0})");
            yield return Shot("138_far_roof_gap");
            int p0 = M.Pounces; Stick(StickFor(Vector3.left)); yield return Press(GamepadButton.East);
            yield return WaitFor(() => M.Pounces > p0, 1f); yield return WaitFor(() => M.Grounded && !M.Pouncing, 3f); Stick(Vector2.zero);
            yield return Go(new Vector3(-178f, 0, 102f), .4f, 4f, .5f); yield return Go(new Vector3(-179f, 0, 106f), .4f, 4f, .5f);
            Ok(Has("P03") && Has("P04"), $"P03 and P04 on the far roof, reached with the pounce ({M.LastPounceDistance:0.0} m)");
            yield return Go(new Vector3(-190, 0, 104), .6f, 6f); yield return WaitFor(() => M.Grounded && Player.transform.position.y < .5f, 5f);
            // the watchmen's yard: Dan walks up to the gate and is caught; lamb Kira walks in with the street flock
            var keeper = Guard.All.First(g => g.name.Contains("yard gate"));
            yield return Via(XZ(-190, 98), XZ(-190, 66), XZ(-188, 32), XZ(-170, 30), XZ(-150, 26));
            for (int k = 0; k < 3 && P.Index != 0; k++) { yield return SwitchTo(0); if (P.Index != 0) yield return Go(Player.transform.position + Vector3.right * 2f, .5f, 2f); }
            int s0 = keeper.Spotted;
            yield return GoF(BE(-165, 12), .8f, 60f); yield return Go(BE(-176, 12), .6f, 10f, 1f, () => keeper.Spotted > s0);
            yield return WaitFor(() => keeper.Spotted > s0, 3f);
            Ok(keeper.Spotted > s0, $"Dan walks up to the watchmen's yard and is caught: \"{Hud.LastMessage}\"");
            yield return WaitFor(() => !Game.I.Busy, 3f);
            yield return SwitchTo(2);
            var flock = Flock.All.First(f => f.name.Contains("street"));
            yield return Via(XZ(-150, 26)); yield return GoF(flock.Middle, 1.5f, 80f);
            int k0 = keeper.Spotted; float tEnd = Time.time + 150f;
            while (Time.time < tEnd && !Has("V15"))
            {
                var mid = flock.Middle; var toShard = SpotBE("V15") - Player.transform.position; toShard.y = 0;
                // stay in the flock's middle; when the flock is in the yard, slip to the shard (2-5 m away, behind the watchman)
                var target = mid.x < -184f && toShard.magnitude < 7f ? SpotBE("V15") : mid;
                var d = target - Player.transform.position; d.y = 0;
                Stick(StickFor(d.normalized) * Mathf.Clamp(d.magnitude / 1.5f, .3f, 1f));
                if (mid.x < -175f && mid.x > -185f && !Has("V15") && Time.frameCount % 120 == 0) { Stick(Vector2.zero); yield return Shot("139_with_the_flock"); }
                yield return null;
            }
            Stick(Vector2.zero);
            Ok(Has("V15") && keeper.Spotted == k0 && Flock.HideCount > 0, $"lamb Kira walks into the watchmen's yard with the flock: V15 {(Has("V15") ? "got" : "missed")}, the watchman saw her {keeper.Spotted - k0} time(s) (hidden {Flock.HideCount} frames)");
            yield return Wait(4f);
            yield return SwitchTo(0);
            foreach (var id in new[] { "V13", "V07", "V03", "V04", "V05", "V02" }) { if (SaveGame.Current.Total >= NeedBE + 2 && necklaceSeenBE) break; yield return PastGuards(SpotBE(id), 20f); yield return BShard(id); }
            Log($"  watchmen: {string.Join(", ", Guard.All.Select(g => $"{g.name} spotted {g.Spotted}, alerts {g.Alerts}"))}");
        }

        IEnumerator GoldenShard()
        {
            Head("Round 7, items 9 and 10: the necklace, the golden shard in the manger, WiseMenArrive");
            // more shards until the total wakes the necklace (as a player would: the farmyard is on the way back)
            foreach (var id in new[] { "V02", "V03" })
            {
                if (necklaceSeenBE) break;
                yield return BShard(id);
            }
            Ok(SaveGame.Current.World(WorldData.Bethlehem).necklaceAwake, $"the necklace is awake ({SaveGame.Current.Total} in all, threshold {NeedBE})");
            yield return Via(XZ(-60, 34), XZ(-40, 33), XZ(-30, 33), XZ(-20, 30), XZ(-8, 20), XZ(0, 20)); yield return Go(new Vector3(0, 0, 27), .6f, 10f); yield return Go(new Vector3(0, 0, 34.5f), .5f, 10f);
            yield return Shot("141_manger_glows");
            yield return Go(new Vector3(0, 0, 36.6f), .3f, 5f, .5f, () => Game.I.Busy);
            yield return WaitFor(() => Cutscenes.Playing == "WiseMenArrive", 6f);
            Ok(Cutscenes.Playing == "WiseMenArrive", "the golden shard in the manger plays WiseMenArrive");
            foreach (var (t, n) in new[] { (2f, "142a_silhouettes"), (4.4f, "142b_doors"), (7f, "142c_at_the_manger"), (10.4f, "142d_disappointed"), (12.6f, "142e_present_tossed"), (13.8f, "142f_hug"), (15.5f, "142g_hay_tornado") })
            {
                yield return WaitFor(() => Cutscenes.Director && Cutscenes.Director.time >= t || Cutscenes.Playing == null, 30f);
                yield return Shot(n);
            }
            yield return WaitFor(() => !Game.I.Busy, 15f); yield return Wait(.6f);
            Ok(Vortex.Inside(Player.transform.position) && SaveGame.Current.World(WorldData.Bethlehem).storyShard && SaveGame.Current.StoryShards == 3,
               $"the hay's vortex takes them to the vortex space; story shards {SaveGame.Current.StoryShards} of 8: \"{Hud.LastMessage}\"");
            var med = VortexDoor.All.First(d => d.info.id == "Medieval"); var home = VortexDoor.All.First(d => d.info.id == "Home");
            Ok(!med.Blocked && home.label.text.Contains("3 of 8"), $"the Medieval path is open now; Home reads \"{home.label.text.Replace("\n", " ")}\"");
            yield return Shot("143_vortex_after_bethlehem");
        }

        // Round 7, item 7: the pounce works everywhere: Egypt's far roof (E14 / E15)
        IEnumerator PounceInEgypt()
        {
            Head("Round 7, item 7: Kira's pounce in Egypt (the far roof, E14 / E15)");
            yield return Go(VPath(0, 4), .8f); yield return Go(VPath(Yaw("Egypt"), 8), .6f); yield return Go(VPath(Yaw("Egypt"), 19), .6f);
            yield return Go(VPath(Yaw("Egypt"), 24), .5f, 6f, 1f, () => Game.I.Busy);
            yield return WaitFor(() => Game.InEgypt && !Game.I.Busy, 12f);
            Ok(Game.InEgypt, $"the Egypt path from the vortex ({Game.World.worldId})");
            Ok(P.Heroes[2].Form && P.Heroes[2].Form.id == KiraForm.Mummy, $"Kira is mummy Kira again ({P.Heroes[2].Form.displayName})");
            yield return SwitchTo(2);
            // up the west rooftop's outside stair (x -23.1, z 49 -> 44), across to the roof's west edge, pounce west to the far roof
            // the pounce is what changed here: Kira is set down on the west roof (Round 6's check climbs its stair with Dan)
            Log("  (set down on Egypt's west roof by the check; the stair up is Round 6's)");
            yield return TpSettle(Egypt.OnGround(-27, 40) + Vector3.up * .1f, 270);
            Ok(Player.transform.position.y > 4f, $"on Egypt's west roof (y {Player.transform.position.y:0.0})"); yield return Go(new Vector3(-29.6f, 0, 40f), .3f, 6f, .4f);
            yield return Shot("144_egypt_far_roof_gap");
            int p0 = M.Pounces; Stick(StickFor(Vector3.left)); yield return Press(GamepadButton.East);
            yield return WaitFor(() => M.Pounces > p0, 1f); yield return WaitFor(() => M.Grounded && !M.Pouncing, 3f); Stick(Vector2.zero);
            Log($"  pounce from the west roof: {M.LastPounceDistance:0.0} m, landed at {Fmt(Player.transform.position)}");
            var e14 = Egypt.ShardPos(WorldData.Load(WorldData.Egypt).shards.First(s => s.id == "E14").position);
            var e15 = Egypt.ShardPos(WorldData.Load(WorldData.Egypt).shards.First(s => s.id == "E15").position);
            yield return Go(e14, .4f, 5f, .5f); yield return Go(e15, .4f, 5f, .5f);
            Ok(Has("E14") && Has("E15"), $"E14 and E15 on Egypt's far roof with Kira's pounce (y {Player.transform.position.y:0.0})");
            yield return Shot("145_egypt_far_roof");
        }

        IEnumerator PerfBethlehem()
        {
            Head($"Round 7, item 11: frame rate in Bethlehem, the full view drawn every frame ({(TestRun.Headless ? $"{TestRun.View.width} x {TestRun.View.height} off-screen" : "window")}, Steam Deck setting {(SteamDeck.On ? "on" : "off")})");
            if (!Game.InBethlehem)
            {
                yield return EnsurePlaying(false);
                var be = SaveGame.Current.World(WorldData.Bethlehem); if (!be.opened.Contains("BE-arrived")) be.opened.Add("BE-arrived");
                SaveGame.Current.checkpoint = Bethlehem.VortexCp; yield return StartWorld(WorldData.Bethlehem);
                yield return WaitFor(() => !Game.I.Busy, 10f);
            }
            yield return SwitchTo(0);
            float worstAvg = 9999; string where = "";
            foreach (var (name, from, path) in new[]
            {
                ("the farmyard toward the barn", XZ(0, 4), new[] { XZ(-8, 16), XZ(8, 18), XZ(0, 6) }),
                ("the village's main street", XZ(-62, 60), new[] { XZ(-100, 60), XZ(-130, 62), XZ(-70, 60) }),
                ("the hills by the camps", XZ(60, 30), new[] { XZ(90, 50), XZ(110, 80), XZ(70, 40) }),
            })
            {
                yield return TpSettle(Bethlehem.OnGround(from.x, from.z) + Vector3.up * .1f, 0);
                Application.targetFrameRate = -1;
                int frames = 0, wp = 0; float t0 = Time.realtimeSinceStartup, worstFrame = 0;
                while (Time.realtimeSinceStartup - t0 < 8f)
                {
                    var d = path[wp] - Player.transform.position; d.y = 0; if (d.magnitude < 2f) wp = (wp + 1) % path.Length;
                    Stick(StickFor(d.normalized)); frames++; worstFrame = Mathf.Max(worstFrame, Time.unscaledDeltaTime);
                    if (TestRun.Headless) { yield return new WaitForEndOfFrame(); CameraRig.I.Cam.Render(); }
                    yield return null;
                }
                Stick(Vector2.zero);
                float fps = frames / (Time.realtimeSinceStartup - t0);
                Application.targetFrameRate = 144;
                Log($"  {name}: {fps:0} fps average, slowest frame {worstFrame * 1000:0} ms");
                if (fps < worstAvg) { worstAvg = fps; where = name; }
                yield return Shot("150_perf_" + name.Replace(' ', '_').Replace("'", ""));
            }
            float need = SteamDeck.On ? 40f : 60f;
            Ok(worstAvg > need, $"Bethlehem: {worstAvg:0} fps at the slowest place ({where}); need {need:0} ({SystemInfo.graphicsDeviceName}, {Screen.width}x{Screen.height})");
        }

        // quick looks (teleports, screenshots) for building the world
        IEnumerator BethDiag()
        {
            yield return EnsurePlaying(false);
            var be = SaveGame.Current.World(WorldData.Bethlehem); if (!be.opened.Contains("BE-arrived")) be.opened.Add("BE-arrived");
            SaveGame.Current.checkpoint = Bethlehem.Start; yield return StartWorld(WorldData.Bethlehem); yield return WaitFor(() => !Game.I.Busy, 10f);
            yield return Wait(1f);
            yield return Shot("d0_spawn_player_view");
            yield return CloseUpAt(new Vector3(18, 9, 6), new Vector3(0, 4, 34), "d1_barn_outside");
            yield return CloseUpAt(new Vector3(-14, 34, 10), new Vector3(0, 6, 34), "d2_barn_roof_hole");
            yield return CloseUpAt(new Vector3(4, 2.6f, 28), new Vector3(-2, 1.5f, 38), "d3_barn_inside");
            yield return CloseUpAt(new Vector3(-40, 14, 50), new Vector3(-120, 2, 60), "d4_village");
            yield return CloseUpAt(new Vector3(-150, 6, 70), new Vector3(-150, 4, 95), "d5_inn");
            yield return CloseUpAt(new Vector3(40, 18, 0), new Vector3(110, 4, 80), "d6_hills");
            yield return CloseUpAt(new Vector3(168, Bethlehem.PillarTop + 6, 118), Bethlehem.PillarA + Vector3.right * 8, "d7_pillars");
            yield return CloseUpAt(new Vector3(30, 8, 170), new Vector3(60, 2, 190), "d8_road");
            yield return CloseUpAt(new Vector3(0, 60, -60), new Vector3(0, 0, 80), "d9_overview");
        }
    }
}
