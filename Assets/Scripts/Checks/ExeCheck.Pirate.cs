using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace ShardsOfTime
{
    // Round 10: the Pirate Seas as a player plays it, with the virtual pad, from the vortex (a save with the first four worlds done to their
    // story shards, each one's shards counted that rule 2 counts on arrival here, Muscle Man along; no moves learned on the way): the arrival
    // (PirateArrival), otter Kira's belly-slide and the low cargo rack, the ship's shards (the decks, the plank, the rigging up to the main top
    // and the crow's nest, the fore top by Muscle Man's boost, his three jobs: the cannon, the crates, the anchor), the hold, the fiddler's
    // deep dive, the gangplank, the island and the cove, the necklace, the chest and ShardOverboard, the dive to the green shard (the music
    // under water, the air bubbles), SeaVortex and the vortex (Victorian Steampunk to be continued, Home 5 of 8); back in: the grotto behind
    // the gap (otter Kira), a pufferfish; then the Medieval moat's deep pit with the deep dive. Teleports go from place to place; what happens
    // at each place is done with the pad.
    public sealed partial class ExeCheck
    {
        int ShardsPS => SaveGame.Current.World(WorldData.Pirate).shards.Count;
        Vector3 PS(float x, float z) => Pirate.OnGround(x, z);
        Vector3 SpotPS(string id) => Pirate.ShardPos(WorldData.Load(WorldData.Pirate).shards.First(s => s.id == id).position);
        const GamepadButton DiveBtn = GamepadButton.East;

        IEnumerator PirateSetup()
        {
            yield return EnsurePlaying(false);
            var pv = WorldData.Load(WorldData.Pirate).areas[0].firstVisit;
            foreach (var wid in new[] { WorldData.IceAge, WorldData.Egypt, WorldData.Bethlehem, WorldData.Medieval })
            {
                var prog = SaveGame.Current.World(wid); prog.storyShard = true; prog.necklaceAwake = true;
                var wd = WorldData.Load(wid); var fv = wd.areas[0].firstVisit.Concat(pv).ToArray();
                foreach (var sp in wd.shards) if (sp.requires.All(fv.Contains) && !prog.shards.Contains(sp.id)) prog.shards.Add(sp.id);
                if (!SaveGame.Current.worldsReached.Contains(wid)) SaveGame.Current.worldsReached.Add(wid);
            }
            foreach (var f in new[] { (WorldData.Egypt, "EG-arrived"), (WorldData.Bethlehem, "BE-arrived"), (WorldData.Medieval, "ME-arrived") }) if (!SaveGame.Current.World(f.Item1).opened.Contains(f.Item2)) SaveGame.Current.World(f.Item1).opened.Add(f.Item2);
            foreach (var p in new[] { "IA-vortex", "EG-vortex-gate", "BE-vortex-gate", "ME-vortex-gate" }) if (!SaveGame.Current.portals.Contains(p)) SaveGame.Current.portals.Add(p);
            if (!SaveGame.Current.abilities.Contains(Abilities.MuscleMan)) SaveGame.Current.abilities.Add(Abilities.MuscleMan);
            Log($"  (set up: {SaveGame.Current.Total} shards in all from the first four worlds (what rule 2 counts on arrival here), Muscle Man along, no moves learned)");
            SaveGame.Current.checkpoint = Vortex.Checkpoint; Checkpoint.RespawnParty(); CameraRig.I.SnapBehind();
            if (!P.Companion) P.AddCompanion(Player.transform.position + Vector3.back * 3f, Quaternion.identity);
            yield return Wait(.6f);
        }

        IEnumerator PirateOnly()
        {
            yield return PirateSetup();
            Cutscenes.Started -= OnPlayed; Cutscenes.Started += OnPlayed;
            float t0 = Time.time;
            Head("Round 10, item 4: the vortex's Pirate Seas path, PirateArrival");
            var door = VortexDoor.All.FirstOrDefault(d => d.info.id == "PirateSeas"); var steam = VortexDoor.All.FirstOrDefault(d => d.info.id == "Steampunk");
            Ok(door && !door.Blocked, $"the Pirate Seas path is open after the purple shard ({(door ? door.label.text.Replace("\n", " ") : "none")})");
            Ok(steam && steam.Blocked, $"Victorian Steampunk is there and closed until the green shard ({(steam ? steam.label.text.Replace("\n", " ") : "none")})");
            float yaw = Yaw("PirateSeas");
            yield return Go(VPath(yaw, 4), .8f); yield return Go(VPath(yaw, 19), .6f);
            yield return Go(VPath(yaw, 24), .5f, 6f, 1f, () => Game.I.Busy);
            yield return WaitFor(() => Cutscenes.Playing == "PirateArrival", 8f);
            Ok(Game.InPirate && Cutscenes.Playing == "PirateArrival", $"the path loads the Pirate Seas and plays its arrival (world {Game.World.worldId}, cutscene {Cutscenes.Playing ?? "none"})");
            foreach (var (t, n) in new[] { (1.5f, "300a_tunnel"), (5f, "300b_ship_night"), (10.5f, "300c_green_vortex"), (14f, "300d_barrels"), (16.2f, "300e_pop_out"), (18.8f, "300f_standing"),
                                           (20.6f, "300g_through_the_sails"), (23f, "300h_crash"), (26.6f, "300i_flat"), (29.5f, "300j_grin"), (31.3f, "300k_abi_laughs"), (33.2f, "300l_green_shard") })
            {
                yield return WaitFor(() => Cutscenes.Director && Cutscenes.Director.time >= t || Cutscenes.Playing == null, 40f);
                yield return Shot(n);
            }
            yield return WaitFor(() => !Game.I.Busy && !Game.InCutscene, 30f);
            yield return Wait(1.5f);
            Ok(Game.Playing && Vector3.Distance(Player.transform.position, Pirate.ArriveAt) < 3f, $"play starts on the main deck at sunset ({Fmt(Player.transform.position)}); Look night off");
            Ok(Music.Track == "pirate", $"the ship's song plays ({Music.Track}; pirate.mp3 {(Music.HasClip ? "loaded" : "MISSING")})");
            var kira = P.Heroes.First(h => h.Stats.displayName == "Kira");
            Ok(kira.Form && kira.Form.id == KiraForm.Otter && kira.Model, $"Kira is an otter (model {(kira.Model ? kira.Model.name : "graybox")}); Dan wears {P.Heroes[0].Outfit}; Muscle Man {(P.Companion && P.Companion.Model ? P.Companion.Model.name : "none")}");
            yield return Shot("301_on_deck_player_view");
            Log($"  HUD: {Hud.TotalShown} / {Hud.UnderTotal}");

            Head("Round 10, item 6: otter Kira: the belly-slide on the wet deck, the low cargo rack");
            yield return SwitchTo(2);
            yield return TpSettle(new Vector3(1.3f, 3.6f, 6.4f), 0);   // (a lane clear of the foremast)
            Stick(StickFor(Vector3.forward)); yield return Wait(.35f); int s0 = CharacterMotor.SlidesAll;
            Log($"  (before the slide: player {Player.Stats.displayName}, form {(Player.Form ? Player.Form.id : "-")}, can slide {M.CanBellySlide}, grounded {M.Grounded}, mode {M.Mode})");
            yield return Press(DiveBtn);
            yield return Wait(.3f); yield return Shot("310_belly_slide");
            yield return WaitFor(() => !M.Sliding, 4f); Stick(Vector2.zero);
            Ok(CharacterMotor.SlidesAll > s0 && M.LastSlideDistance > 5f, $"the belly-slide across the wet deck: {M.LastSlideDistance:0.0} m (a run does {kira.Stats.runSpeed * CharacterMotor.SlideTime:0.0} m in the same time)");
            yield return TpSettle(new Vector3(-1.8f, .7f, 1f), 270);
            yield return PShard("K01", .4f, 10f);
            yield return Shot("311_the_cargo_nook");
            yield return TpSettle(new Vector3(0, 3.6f, -4f), 0); yield return SwitchTo(0);
            Ok(Player.Stats.displayName == "Dan", "back to Dan");

            Head("Round 10, items 2-3: the ship (decks, the plank, the rigging, the hold)");
            foreach (var (id, from, fy) in new[] { ("S01", new Vector3(0, 3.6f, -6.5f), 180f), ("S02", new Vector3(-2, 3.6f, 7), 0f), ("S03", new Vector3(2, 3.6f, 10.5f), 0f),
                                                   ("Q01", new Vector3(-1, 6.1f, -17.5f), 180f), ("Q02", new Vector3(1, 6.1f, -15f), 180f), ("F01", new Vector3(0, 6.1f, 16f), 0f), ("F02", new Vector3(0, 6.1f, 19.5f), 0f),
                                                   ("H01", new Vector3(0, .7f, 2.4f), 0f), ("H02", new Vector3(-1, .7f, 8f), 0f), ("H03", new Vector3(1.5f, .7f, 0.5f), 180f) })
            { yield return TpSettle(from, fy); yield return PShard(id, .5f, 12f); }
            yield return Shot("320_hold");
            // up the quarterdeck ramp from the main deck (walked)
            yield return TpSettle(new Vector3(-3.9f, 3.6f, -5.5f), 180); yield return Go(new Vector3(-3.9f, 6f, -13.5f), .6f, 8f);
            Ok(Player.transform.position.y > 5.8f, $"walked up the quarterdeck ramp ({Fmt(Player.transform.position)})");
            // the plank
            yield return TpSettle(new Vector3(-4.6f, 3.6f, 4f), 270);
            yield return Go(new Vector3(-6.4f, 3.6f, 4f), .3f, 4f, .4f);
            Ok(Player.transform.position.x < -6f && Player.transform.position.y > 3.4f, $"stepped out onto the plank through the rail's gap ({Fmt(Player.transform.position)})");
            yield return Go(SpotPS("P01"), .3f, 8f, .4f); yield return WaitFor(() => Has("P01"), 1.5f);
            Ok(Has("P01"), $"P01: walked the plank to its end ({Fmt(Player.transform.position)})"); yield return Shot("321_the_plank");
            // the rigging: the net up to the main top, on up to the crow's nest
            yield return TpSettle(new Vector3(-4.2f, 3.6f, -1f), 270); int c0 = RopeClimb.Climbs;
            yield return Go(new Vector3(-5.0f, 3.5f, -1f), .4f, 4f, .5f); yield return Press(JumpBtn);
            yield return WaitFor(() => !M.Climbing && RopeClimb.Climbs > c0, 6f); yield return Wait(.6f);
            Ok(RopeClimb.Climbs > c0 && Player.transform.position.y > 13f, $"up the rope net to the main top ({Fmt(Player.transform.position)})");
            yield return PShard("T01", .5f, 6f); yield return Shot("322_main_top");
            yield return Go(new Vector3(-1.1f, 13.4f, 1.2f), .35f, 5f, .5f); c0 = RopeClimb.Climbs; yield return Press(JumpBtn);
            yield return WaitFor(() => !M.Climbing && RopeClimb.Climbs > c0, 6f); yield return Wait(.6f);
            Ok(Player.transform.position.y > 20.8f, $"up the net to the crow's nest ({Fmt(Player.transform.position)})");
            yield return PShard("T02", .4f, 5f); yield return Shot("323_crows_nest_view");

            Head("Round 10, item 7: Muscle Man's jobs (the boost to the fore top, the cannon, the crates, the anchor)");
            yield return TpSettle(new Vector3(0, 3.6f, 6.6f), 0); yield return Go(new Vector3(0, 3.5f, 7.6f), .4f, 4f, .5f);
            yield return WaitFor(() => P.Companion && Vector3.Distance(P.Companion.transform.position, Player.transform.position) < 6f, 5f);
            yield return Press(AttackBtn); yield return WaitFor(() => Game.I.Busy, 3f); yield return WaitFor(() => !Game.I.Busy, 8f); yield return WaitFor(() => M.Grounded, 6f); yield return Wait(.5f);
            Ok(Player.transform.position.y > 11.4f, $"Muscle Man boosts Dan up onto the fore top ({Fmt(Player.transform.position)})");
            yield return PShard("T03", .5f, 6f); yield return Shot("330_fore_top");
            foreach (var (job, from, fy, shards) in new[] { ("PS-cannon-cabin", new Vector3(0, 3.6f, -9.6f), 180f, new[] { "C01", "C02" }), ("PS-crates-hold", new Vector3(0, .7f, -3.8f), 180f, new[] { "H04", "H05" }),
                                                            ("PS-anchor-locker", new Vector3(0, 3.6f, 12.0f), 0f, new[] { "L01" }) })
            {
                yield return TpSettle(from, fy); yield return Wait(1.2f);
                yield return Press(AttackBtn); yield return WaitFor(() => Game.I.Busy, 3f); yield return WaitFor(() => !Game.I.Busy, 8f);
                bool done = SaveGame.Current.World(WorldData.Pirate).opened.Contains(job);
                Ok(done, $"{job}: Muscle Man {(done ? "did it" : "did NOT do it")}");
                yield return Shot("331_" + job);
                if (job == "PS-crates-hold") yield return Go(new Vector3(0, .6f, -7.6f), .5f, 6f);
                if (job == "PS-cannon-cabin") yield return Go(new Vector3(0, 3.5f, -15f), .5f, 6f);
                foreach (var id in shards) yield return PShard(id, .5f, 12f);
            }

            Head("Round 10, item 8: the ship's fiddler teaches everyone the deep dive");
            yield return TpSettle(Pirate.ViolinistAt + new Vector3(-4.5f, .1f, 0), 90);
            yield return Go(Pirate.ViolinistAt + new Vector3(-1.6f, 0, 0), .4f, 6f, .6f);
            yield return WaitFor(() => played.Contains("ViolinistTeachesPirate"), 6f); yield return Wait(2f); yield return Shot("340_fiddler");
            yield return WaitFor(() => !Game.I.Busy && !Game.InCutscene, 15f);
            Ok(SaveGame.Current.Has(Abilities.DeepDive) && P.Heroes.All(h => h.Motor.CanDeepDive && (h.Stats.swimmer || h.Motor.BreathMax > 40f)), $"everyone has the deep dive (Dan's breath {P.Heroes[0].Motor.BreathMax:0} s)");

            Head("Round 10, item 3: down the gangplank to the jetty and the island; the cove");
            yield return TpSettle(new Vector3(4.6f, 3.6f, -2f), 90);
            yield return Go(new Vector3(13f, 1.3f, -2f), .5f, 8f);
            Ok(Player.transform.position.x > 12f && Player.transform.position.y > .9f, $"walked down the gangplank onto the jetty ({Fmt(Player.transform.position)})");
            yield return PShard("J01", .5f, 8f);
            foreach (var id in new[] { "I01", "I03", "I04", "I02", "I07", "I05", "I06" })
            { var sp = SpotPS(id); yield return TpSettle(PS(sp.x - 3, sp.z) + Vector3.up * .3f, 90); yield return PShard(id, .5f, 12f); }
            yield return Shot("350_island");
            foreach (var id in new[] { "V01", "V02" })
            { var sp = SpotPS(id); yield return TpSettle(new Vector3(sp.x, .2f, sp.z - 3), 0); yield return SwimTo(sp, 15f); Ok(Has(id), $"{id} ({Game.World.shards.First(s => s.id == id).note}): {ShardsPS} here, {SaveGame.Current.Total} in all"); }
            yield return Shot("351_cove");
            Log($"  {ShardsPS} from the Pirate Seas, {SaveGame.Current.Total} in all (threshold {Game.World.areas[0].requirement}, 20 from here)");

            Head("Round 10, items 5 and 10: the necklace, the chest, ShardOverboard");
            yield return TpSettle(new Vector3(0, 3.6f, -4f), 90); yield return Wait(1f);
            yield return WaitFor(() => played.Contains("NecklaceWakesPirate"), 12f);
            Ok(played.Contains("NecklaceWakesPirate") && SaveGame.Current.World(WorldData.Pirate).necklaceAwake, $"the necklace wakes at {SaveGame.Current.Total} in all, {ShardsPS} from here (NecklaceWakesPirate, green)");
            yield return WaitFor(() => !Game.I.Busy && !Game.InCutscene, 15f); yield return Wait(.5f);
            Ok(Pirate.ChestShard && Pirate.ChestShard.gameObject.activeSelf && Necklace.Target == Pirate.ChestShard.transform, $"it leads to the chest by the rail (target {(Necklace.Target ? Necklace.Target.name : "none")})");
            yield return Shot("360_necklace_to_chest");
            yield return Go(Pirate.ChestAt + new Vector3(-1.2f, 0, 0), .4f, 8f);
            yield return Go(Pirate.ChestAt + new Vector3(-.4f, 0, 0), .3f, 3f, .5f);
            yield return WaitFor(() => Cutscenes.Playing == "ShardOverboard", 6f);
            Ok(Cutscenes.Playing == "ShardOverboard", "the chest's shard plays ShardOverboard");
            foreach (var (t, n) in new[] { (1.5f, "361a_he_grabs_it"), (4f, "361b_held_up_laughing"), (5.8f, "361c_it_slips"), (8.2f, "361d_it_rolls"), (11f, "361e_over_the_side"), (14.8f, "361f_he_leaps_in"), (16.6f, "361g_otter_dives") })
            { yield return WaitFor(() => Cutscenes.Director && Cutscenes.Director.time >= t || Cutscenes.Playing == null, 25f); yield return Shot(n); }
            yield return WaitFor(() => !Game.I.Busy && !Game.InCutscene, 25f); yield return Wait(1f);
            Ok(Pirate.DeepOpen && SeaLock.All.Count == 0 && M.Mode == MoveMode.Swim, $"in the sea by the ship, the deep sea open (SeaLock {SeaLock.All.Count}, mode {M.Mode})");
            Ok(Game.I.StoryShard.gameObject.activeSelf && Necklace.Target == Game.I.StoryShard.transform, $"the necklace leads down to the green shard on the sea floor ({Fmt(Pirate.StoryAt)})");
            yield return Shot("362_in_the_sea");

            Head("Round 10, items 3, 11, 12: under the sea: the music, the look, air bubbles, the green shard, SeaVortex");
            var mid = Vector3.Lerp(Player.transform.position, Pirate.StoryAt, .4f); mid.y = -6f;
            yield return SwimTo(mid, 15f);
            Ok(Pirate.Underwater && Music.Track == "underwater" && ShardsOfTime.Look.Underwater, $"under the water: underwater.mp3 ({Music.Track}, {(Music.HasClip ? "loaded" : "MISSING")}), the bright blue look");
            yield return Shot("370_under_the_sea");
            float b0 = M.Breath; var vent = new Vector3(-45f, -8f, 18f);
            yield return SwimTo(vent, 12f); yield return Wait(1.5f);
            Ok(M.AirRefills > 0, $"air bubbles give the breath back ({b0:0.0} -> {M.Breath:0.0} s of {M.BreathMax:0} s)");
            yield return Shot("371_air_bubbles");
            yield return SwimTo(Pirate.StoryAt, 20f);
            yield return WaitFor(() => played.Contains("SeaVortex"), 6f);
            Ok(played.Contains("SeaVortex"), "the green shard plays SeaVortex");
            foreach (var (t, n) in new[] { (1.5f, "372a_vortex_opens"), (4.5f, "372b_pulled_in"), (7.2f, "372c_into_the_green") })
            { yield return WaitFor(() => Cutscenes.Director && Cutscenes.Director.time >= t || Cutscenes.Playing == null, 15f); yield return Shot(n); }
            yield return WaitFor(() => !Game.I.Busy && !Game.InCutscene, 20f); yield return Wait(1f);
            var home = VortexDoor.All.First(d => d.info.id == "Home"); steam = VortexDoor.All.First(d => d.info.id == "Steampunk");
            Ok(Vortex.Inside(Player.transform.position) && SaveGame.Current.World(WorldData.Pirate).storyShard && home.label.text.Contains("5 of 8"), $"into the vortex with the green shard; Home says \"{home.label.text.Replace("\n", " ")}\"");
            Ok(P.Companion && Vector3.Distance(P.Companion.transform.position, Player.transform.position) < 9f, "Muscle Man came too");
            yield return Go(VPath(Yaw("Steampunk"), 4), .8f); yield return Go(VPath(Yaw("Steampunk"), 19), .6f);
            yield return Go(VPath(Yaw("Steampunk"), 24), .5f, 6f, 1f, () => Game.I.Busy);
            yield return WaitFor(() => Game.I.ToBeContinuedShown > 0, 6f);
            Ok(Game.I.ToBeContinuedShown > 0 && !steam.Blocked, $"Victorian Steampunk: to be continued ({steam.label.text.Replace("\n", " ")})");
            yield return WaitFor(() => !Game.I.Busy, 10f);
            Log($"  the Pirate Seas from the arrival to the vortex took the bot {(Time.time - t0) / 60f:0.0} min");

            Head("Round 10, items 6 and 9: back under the sea: otter Kira through the gap into the grotto; a pufferfish");
            yield return Go(VPath(Yaw("PirateSeas"), 4), .8f); yield return Go(VPath(Yaw("PirateSeas"), 19), .6f);
            yield return Go(VPath(Yaw("PirateSeas"), 24), .5f, 6f, 1f, () => Game.I.Busy);
            yield return WaitFor(() => Game.InPirate && !Game.I.Busy, 12f);
            Ok(Game.InPirate && !played.Skip(1).Contains("PirateArrival") && Pirate.DeepOpen, $"back in the Pirate Seas at the vortex checkpoint, the deep sea still open ({Fmt(Player.transform.position)})");
            yield return SwitchTo(2);
            yield return TpSettle(new Vector3(-136.65f, -17.4f, 17.5f), 0);
            yield return SwimTo(new Vector3(-136.65f, -17.6f, 21.2f), 8f); yield return SwimTo(SpotPS("U22"), 10f);
            Ok(Has("U22"), $"U22: otter Kira squeezed through the gap into the grotto ({Fmt(Player.transform.position)})"); yield return Shot("380_grotto");
            yield return SwitchTo(0);
            var puff = Enemy.All.FirstOrDefault(e => e && e.kind == Enemy.Kind.Puffer && e.isActiveAndEnabled);
            if (puff) { yield return TpSettle(puff.transform.position + new Vector3(0, -.5f, -6f), 0); yield return SwimTo(puff.transform.position + Vector3.back * 2.5f, 6f); yield return Wait(.5f);
                Ok(puff.Puffs > 0, $"a pufferfish puffs up as Dan swims close ({puff.Puffs} puffs)"); yield return Shot("381_pufferfish"); }
            var crab = Enemy.All.Count(e => e && e.kind == Enemy.Kind.Crab); var deck = Enemy.All.Count(e => e && e.kind == Enemy.Kind.Deckhand);
            Ok(crab >= 2 && deck >= 2 && FishSchool.Count >= 50, $"{deck} deckhands, {crab} crabs, {Enemy.All.Count(e => e && e.kind == Enemy.Kind.Puffer)} pufferfish, {FishSchool.Count} friendly fish");

            yield return DeepGateMedieval();
        }

        // Round 10, item 8: the deep dive opens the earlier worlds' deep water: the Medieval moat's deep pit (DD1, DD2)
        IEnumerator DeepGateMedieval()
        {
            Head("Round 10, item 8: an earlier world's deep-water gate with the deep dive (the Medieval moat's deep pit)");
            if (!SaveGame.Current.Has(Abilities.DeepDive)) { SaveGame.Current.abilities.Add(Abilities.DeepDive); P.ApplyAbilities(); }
            SaveGame.Current.checkpoint = Medieval.VortexCp; yield return StartWorld(WorldData.Medieval); yield return WaitFor(() => !Game.I.Busy, 10f);
            yield return SwitchTo(0);
            yield return TpSettle(new Vector3(-42, 0, 95), 0); yield return Wait(.8f);
            var dd = Medieval.ShardPos(WorldData.Load(WorldData.Medieval).shards.First(s => s.id == "DD1").position);
            yield return SwimTo(dd, 15f);
            Ok(Has("DD1"), $"DD1 in the moat's deep pit with the deep dive ({Fmt(Player.transform.position)}, shard at {Fmt(dd)})");
            yield return Shot("390_moat_deep_pit");
        }

        IEnumerator PShard(string id, float tol = .5f, float timeout = 15f)
        {
            if (Has(id)) yield break;
            yield return GoF(SpotPS(id), tol, timeout);
            yield return WaitFor(() => Has(id), 1.5f);
            Ok(Has(id), $"{id} ({Game.World.shards.First(s => s.id == id).note}) at {Fmt(Player.transform.position)}; {ShardsPS} here, {SaveGame.Current.Total} in all");
        }

        // swim to a point in 3D: the stick toward it, Dive held to go down, Jump held to go up
        IEnumerator SwimTo(Vector3 to, float timeout)
        {
            float end = Time.time + timeout;
            while (Time.time < end && !Game.I.Busy)
            {
                var c = M.Center; var d = to - c; var flat = new Vector3(d.x, 0, d.z);
                if (d.magnitude < .5f) break;
                Stick(flat.magnitude > .3f ? StickFor(flat.normalized) * Mathf.Clamp(flat.magnitude / 1.5f, .3f, 1f) : Vector2.zero);
                Btn(DiveBtn, d.y < -.4f); Btn(JumpBtn, d.y > .6f && M.Mode == MoveMode.Swim);
                yield return null;
            }
            Btn(DiveBtn, false); Btn(JumpBtn, false); Stick(Vector2.zero);
            yield return Wait(.3f);
        }

        IEnumerator PirateEnter(string cp)
        {
            var pr = SaveGame.Current.World(WorldData.Pirate); if (!pr.opened.Contains("PS-arrived")) pr.opened.Add("PS-arrived");
            if (!SaveGame.Current.Has(Abilities.MuscleMan)) SaveGame.Current.abilities.Add(Abilities.MuscleMan);
            SaveGame.Current.checkpoint = cp; yield return StartWorld(WorldData.Pirate); yield return WaitFor(() => !Game.I.Busy, 10f);
            if (!P.Companion) P.AddCompanion(Player.transform.position + Vector3.back * 3f, Quaternion.identity);
        }

        IEnumerator PerfPirate()
        {
            Head($"Round 10, item 12: frame rate in the Pirate Seas, the full view drawn every frame ({(TestRun.Headless ? $"{TestRun.View.width} x {TestRun.View.height} off-screen" : "window")})");
            if (!Game.InPirate) { yield return EnsurePlaying(false); yield return PirateEnter(Pirate.Start); }
            Pirate.OpenDeepSea(true);
            yield return SwitchTo(0);
            float worstAvg = 9999; string where = "";
            foreach (var (name, from, path, swim) in new[]
            {
                ("the main deck", new Vector3(0, 3.6f, -6), new[] { new Vector3(-3, 3.5f, 8), new Vector3(3, 3.5f, 8), new Vector3(0, 3.5f, -8) }, false),
                ("the island toward the ship", PS(40, 0), new[] { PS(60, 20), PS(35, 15), PS(45, -20) }, false),
                ("the reef with its fish", new Vector3(-40, -6, 0), new[] { new Vector3(-60, -6, 20), new Vector3(-70, -6, -10), new Vector3(-40, -6, 0) }, true),
                ("the wreck's trench", new Vector3(-55, -12, 95), new[] { new Vector3(-75, -14, 115), new Vector3(-60, -12, 100) }, true),
            })
            {
                yield return TpSettle(from + Vector3.up * .1f, 0);
                Application.targetFrameRate = -1;
                int frames = 0, wp = 0; float t0 = Time.realtimeSinceStartup, worstFrame = 0;
                while (Time.realtimeSinceStartup - t0 < 8f)
                {
                    var d = path[wp] - Player.transform.position; var fd = d; fd.y = 0; if (fd.magnitude < 2.5f) wp = (wp + 1) % path.Length;
                    Stick(StickFor(fd.normalized)); if (swim) { Btn(DiveBtn, d.y < -.5f); Btn(JumpBtn, d.y > .8f); }
                    frames++; worstFrame = Mathf.Max(worstFrame, Time.unscaledDeltaTime);
                    if (TestRun.Headless) { yield return new WaitForEndOfFrame(); CameraRig.I.Cam.Render(); }
                    yield return null;
                }
                Stick(Vector2.zero); Btn(DiveBtn, false); Btn(JumpBtn, false);
                float fps = frames / (Time.realtimeSinceStartup - t0);
                Application.targetFrameRate = 144;
                Log($"  {name}: {fps:0} fps average, slowest frame {worstFrame * 1000:0} ms");
                if (fps < worstAvg) { worstAvg = fps; where = name; }
                yield return Shot("395_perf_" + name.Replace(' ', '_').Replace("'", ""));
            }
            float need = SteamDeck.On ? 40f : 60f;
            Ok(worstAvg > need, $"the Pirate Seas: {worstAvg:0} fps at the slowest place ({where}); need {need:0} ({SystemInfo.graphicsDeviceName}, {Screen.width}x{Screen.height})");
        }

        IEnumerator DeepGateOnly() { yield return EnsurePlaying(false); if (!SaveGame.Current.Has(Abilities.MuscleMan)) SaveGame.Current.abilities.Add(Abilities.MuscleMan); yield return DeepGateMedieval(); }

        // every shard under the sea, swum to from 4 m above it (otter Kira for the deep and narrow ones, Dan with the deep dive for the rest)
        IEnumerator SeaReach()
        {
            yield return EnsurePlaying(false); yield return PirateEnter(Pirate.Start);
            Pirate.OpenDeepSea(true);
            if (!SaveGame.Current.Has(Abilities.DeepDive)) { SaveGame.Current.abilities.Add(Abilities.DeepDive); P.ApplyAbilities(); }
            Head("Round 10, item 10: every shard under the sea, swum to (Fix 8.1's reach, in water)");
            int got = 0; var missed = new System.Collections.Generic.List<string>();
            foreach (var sp in Game.World.shards.Where(x => x.id.StartsWith("U")))
            {
                bool kira = sp.requires.Contains("Kira");
                yield return TpSettle(new Vector3(0, 3.6f, -4f), 0); yield return SwitchTo(kira ? 2 : 0);
                var at = Pirate.ShardPos(sp.position);
                var from = sp.id == "U22" || sp.id == "U23" ? new Vector3(-136.65f, -17.4f, 17.5f) : at + Vector3.up * 4f + Vector3.back * 1.5f;
                yield return TpSettle(from, 0);
                if (sp.id == "U22" || sp.id == "U23") yield return SwimTo(new Vector3(-136.65f, -17.6f, 21.2f), 8f);
                yield return SwimTo(at, 14f);
                if (Has(sp.id)) got++; else missed.Add($"{sp.id} at {Fmt(at)} (bot at {Fmt(Player.transform.position)})");
            }
            Ok(missed.Count == 0, $"under the sea: {got} shards swum to{(missed.Count > 0 ? "; NOT: " + string.Join(", ", missed) : "")}");
            yield return Shot("396_sea_reach_last");
        }

        IEnumerator StuckPirate() { yield return EnsurePlaying(false); yield return PirateEnter(Pirate.Start); yield return StuckWorld(WorldData.Pirate, null); }

        // quick looks (teleports, screenshots) for building the world
        IEnumerator PirateDiag()
        {
            yield return EnsurePlaying(false);
            yield return PirateEnter(Pirate.Start);
            yield return Wait(1.5f);
            yield return Shot("d0_spawn_player_view");
            yield return CloseUpAt(new Vector3(-30, 8, -28), new Vector3(0, 7, 2), "d1_ship_from_sea");
            yield return CloseUpAt(new Vector3(0, 6.5f, -14), new Vector3(0, 4, 12), "d2_deck_from_quarterdeck");
            yield return CloseUpAt(new Vector3(2, 2.6f, 11), new Vector3(0, 1, -6), "d3_hold");
            yield return CloseUpAt(new Vector3(30, 6, -25), new Vector3(70, 3, 20), "d4_island");
            yield return CloseUpAt(new Vector3(-3, 22.5f, -3), new Vector3(0, 3, 10), "d5_from_crows_nest");
            yield return CloseUpAt(new Vector3(-6, 5, -8), new Vector3(10, 1, -2), "d6_gangplank_jetty");
            Pirate.OpenDeepSea(true);
            yield return CloseUpAt(new Vector3(-30, -5, 0), new Vector3(-45, -10, 10), "d7_reef_clearing");
            yield return CloseUpAt(new Vector3(-55, -12, 95), new Vector3(-70, -18, 118), "d8_wreck");
            yield return CloseUpAt(new Vector3(-108, -12, 0), new Vector3(-130, -15, 0), "d9_cave_mouth");
            yield return CloseUpAt(new Vector3(-8, -6, -20), new Vector3(0, -2, 0), "d10_ship_from_below");
            yield return TpSettle(new Vector3(-40, -6, 0), 270); yield return Wait(1f); yield return Shot("d11_underwater_player_view");
            yield return CloseUpAt(new Vector3(70, 3, 50), new Vector3(82, -1, 66), "d12_cove");
        }
    }
}
