using System.Collections;
using System.Linq;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 14, the exe check "ending": from the vortex with all eight story shards (the Moon done), the player walks the Home path:
    // Farewell (Muscle Man through the purple portal) -> TheKing in the Medieval courtyard -> Act I backwards (its music reversed) ->
    // HomeAgain (the Violinist outside the window; "No, Kira, no!") -> the credits (stills from each world, Dan's title song) -> free roam:
    // the vortex, every path open, Muscle Man in the party again, the ending saved; one world visited with him; the Theater lists the four.
    public sealed partial class ExeCheck
    {
        IEnumerator EndingOnly()
        {
            yield return WorldsDone(SevenWorlds, WorldData.Moon);
            Cutscenes.Started -= OnPlayed; Cutscenes.Started += OnPlayed;
            var mp = SaveGame.Current.World(WorldData.Moon); mp.storyShard = true; mp.necklaceAwake = true;
            foreach (var f in new[] { Moon.ArrivedFlag, Moon.FloatsFlag }) if (!mp.opened.Contains(f)) mp.opened.Add(f);
            if (!SaveGame.Current.worldsReached.Contains(WorldData.Moon)) SaveGame.Current.worldsReached.Add(WorldData.Moon);
            foreach (var a in new[] { Abilities.MuscleMan, Abilities.Rocket }) if (!SaveGame.Current.Has(a)) SaveGame.Current.abilities.Add(a);
            SaveGame.Current.checkpoint = Vortex.Checkpoint; yield return StartWorld(WorldData.Moon); yield return WaitFor(() => !Game.I.Busy, 10f); yield return Wait(1f);
            if (!P.Companion) P.AddCompanion(Player.transform.position + Vector3.back * 3f, Quaternion.identity);
            yield return EndingWalk();
        }

        // from the vortex with all eight story shards: the Home path to free roam (also the end of minplayer)
        IEnumerator EndingWalk()
        {
            Cutscenes.Started -= OnPlayed; Cutscenes.Started += OnPlayed;
            float t0 = Time.time;
            Head("Round 14: the vortex with all eight story shards: the Home path");
            var home = VortexDoor.All.First(d => d.info.id == "Home");
            Ok(Vortex.Inside(Player.transform.position) && SaveGame.Current.StoryShards == 8 && !home.Blocked, $"in the vortex with {SaveGame.Current.StoryShards} story shards; Home is open; Muscle Man in the party ({(P.Companion ? "yes" : "no")})");
            yield return Shot("700_vortex_home_open");
            yield return Go(VPath(Yaw("Home"), 4), .8f); yield return Go(VPath(Yaw("Home"), 19), .6f);
            yield return Go(VPath(Yaw("Home"), 24), .5f, 6f, 1f, () => Game.I.Busy);

            Head("Round 14, item 1: Farewell");
            yield return WaitFor(() => Cutscenes.Playing == "Farewell", 8f);
            Ok(Cutscenes.Playing == "Farewell" && Game.I.EndingsPlayed == 1, "the Home path plays Farewell");
            foreach (var (t, n) in new[] { (2.6f, "701a_the_portals_open"), (5.6f, "701b_he_spots_the_purple_one"), (9.2f, "701c_the_hug"), (14.0f, "701d_to_the_purple_portal"), (17.4f, "701e_goodbye") })
            { yield return WaitFor(() => Cutscenes.Director && Cutscenes.Director.time >= t || Cutscenes.Playing != "Farewell", 30f); yield return Shot(n); }
            Ok(Music.Track == "vortex", $"the vortex song under Farewell ({Music.Track})");

            Head("Round 14, item 2: TheKing in the Medieval courtyard");
            yield return WaitFor(() => Cutscenes.Playing == "TheKing", 20f);
            Ok(Cutscenes.Playing == "TheKing" && Game.World.worldId == WorldData.Medieval, $"TheKing plays in {Game.World.displayName}");
            Ok(Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length == 0, "the courtyard's enemies are out of the shot");
            yield return Wait(.3f);
            Ok(Music.Track == "medieval", $"Dan's medieval song ({Music.Track})");
            foreach (var (t, n) in new[] { (1.3f, "702a_out_of_the_portal"), (3.4f, "702b_flat_on_the_cobbles"), (7.2f, "702c_he_gets_up"), (10.6f, "702d_he_takes_the_sword"), (13.2f, "702e_the_crowd_roars"), (17.2f, "702f_king_king_king") })
            { yield return WaitFor(() => Cutscenes.Director && Cutscenes.Director.time >= t || Cutscenes.Playing != "TheKing", 30f); yield return Shot(n); }
            var held = Cutscenes.Director ? Cutscenes.Director.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name == "Sword in hand") : null;
            Ok(held && held.gameObject.activeInHierarchy, $"the sword is in Muscle Man's hand ({(held ? Fmt(held.position) : "missing")})");

            Head("Round 14, item 3: HomeAgain: Act I backwards, music too; then the living room");
            yield return WaitFor(() => Cutscenes.Playing == "HomeAgain", 20f);
            Ok(Cutscenes.Playing == "HomeAgain" && Cutscenes.Reversed, "Act I plays backwards (as HomeAgain)");
            double a0 = Cutscenes.Director ? Cutscenes.Director.time : 0; yield return Wait(1.5f); double a1 = Cutscenes.Director ? Cutscenes.Director.time : 0;
            Ok(a1 < a0 - 1.0, $"its time runs backwards ({a0:0.0} s -> {a1:0.0} s)");
            Ok(Music.Track == "act1_reversed" && Music.HasClip, $"Dan's Act I song reversed ({Music.Track}, {(Music.HasClip ? "loaded" : "MISSING")})");
            foreach (var (t, n) in new[] { (45f, "703a_back_up_the_tunnel"), (39.5f, "703b_back_into_the_spiral"), (30f, "703c_the_shards_fly_back"), (20f, "703d_the_bauble_on_the_floor"), (12.5f, "703e_it_flies_back_up") })
            { yield return WaitFor(() => Cutscenes.Director && Cutscenes.Director.time <= t || !Cutscenes.Reversed, 40f); yield return Shot(n); }
            yield return WaitFor(() => Cutscenes.Playing == "HomeAgain" && !Cutscenes.Reversed, 20f);
            Ok(!Cutscenes.Reversed && Cutscenes.Playing == "HomeAgain", "then the living room (HomeAgain)");
            var v = Game.I.HomeViolinist;
            Ok(v && v.HasModel && v.serenade, $"the Violinist outside the window ({(v ? v.ModelName : "missing")}, playing)");
            foreach (var (t, n) in new[] { (1.6f, "704a_asleep_on_the_couch"), (4.2f, "704b_awake"), (6.6f, "704c_the_ornament_whole"), (8.8f, "704d_a_dream"), (14.5f, "704e_the_violinist_outside"), (17.8f, "704f_it_wasnt_a_dream"), (20.2f, "704g_they_turn"), (21.3f, "704h_kira_leaps"), (22.4f, "704i_black") })
            { yield return WaitFor(() => Cutscenes.Director && Cutscenes.Director.time >= t || Cutscenes.Playing != "HomeAgain", 30f); yield return Shot(n); }

            Head("Round 14, item 4: the credits");
            yield return WaitFor(() => Game.I.CreditsRolling, 10f);
            Ok(Game.I.CreditsRolling, "the credits roll after HomeAgain");
            yield return Wait(1.5f);
            Ok(Music.Track == Game.CreditsTrack, $"Dan's {Game.CreditsTrack} song ({Music.Track})");
            Ok(Game.CreditsText.Contains("Based on the film") && Game.CreditsText.Contains("Dan Moll") && Game.CreditsText.Contains("Claude Code"), "the credits' text (Resources/Data/Credits.txt)");
            var stills = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < 6; i++) { yield return Wait(i == 0 ? 1f : 9f); stills.Add(Game.I.CreditsStill); yield return Shot($"705{(char)('a' + i)}_credits"); if (!Game.I.CreditsRolling) break; }
            Ok(stills.Count >= 5, $"stills from each world ({string.Join(", ", stills)})");
            yield return WaitFor(() => !Game.I.CreditsRolling, 20f);
            Ok(!Game.I.CreditsSkipped && Game.I.CreditsSeconds > Game.CreditsLength - 1f, $"the credits ran to the end ({Game.I.CreditsSeconds:0} s)");

            Head("Round 14, item 5: free roam");
            yield return WaitFor(() => !Game.I.Busy, 15f); yield return Wait(1f);
            Ok(SaveGame.Current.endingSeen, "the save remembers the ending is seen");
            Ok(Vortex.Inside(Player.transform.position), $"back in the vortex ({Game.World.displayName}'s)");
            Ok(P.Companion && P.Everyone.Contains(P.Companion), "Muscle Man is in the party again");
            var blocked = VortexDoor.All.Where(d => d.Blocked).Select(d => d.info.id).ToList();
            Ok(blocked.Count == 0, $"every path is open ({VortexDoor.All.Count} paths{(blocked.Count > 0 ? "; blocked: " + string.Join(", ", blocked) : "")})");
            foreach (var n in new[] { "Farewell", "TheKing", "HomeAgain", "Credits" }) Ok(SaveGame.Current.seen.Contains(n), $"the Theater has {n}");
            yield return Shot("706_free_roam_vortex");
            // with him into a world (the Medieval Kingdom)
            yield return Go(VPath(Yaw("Medieval"), 4), .8f); yield return Go(VPath(Yaw("Medieval"), 19), .6f);
            yield return Go(VPath(Yaw("Medieval"), 24), .5f, 6f, 1f, () => Game.I.Busy);
            yield return WaitFor(() => Game.InMedieval && !Game.I.Busy, 20f); yield return Wait(1.5f);
            Ok(Game.InMedieval && P.Companion, $"into the Medieval Kingdom with Muscle Man ({Fmt(Player.transform.position)})");
            yield return Shot("707_medieval_with_muscle_man");
            // a reload keeps it
            SaveGame.Save("ending check"); SaveGame.Load();
            Ok(SaveGame.Current.endingSeen, "after a reload the ending is still seen");
            Log($"  the ending from the Home path to free roam took {(Time.time - t0) / 60f:0.0} min");
        }
    }
}
