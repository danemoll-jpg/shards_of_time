using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace ShardsOfTime
{
    // Round 9, item 9: the hand-offs of the eight redone film scenes, as the game plays them (exe check mode "r9", ~5 min):
    // New game from the title -> Act I to its end (Dan's 60 s act1 song, then the Ice Age theme on the landing) -> the Ice Age arrival,
    // skipped with a held A; then each world's arrival through the game's own vortex path (Game.GoToWorld), the Sphinx gag, the Wise Men's
    // finale and the stone's finale (Game's own coroutines), each skipped with a held A. For each: it plays, its music is right, the hold
    // skips it, control comes back (the party visible, the stick moves the player), and the music after it is right.
    public sealed partial class ExeCheck
    {
        IEnumerator R9()
        {
            Cutscenes.Started -= OnPlayed; Cutscenes.Started += OnPlayed;
            Head("Round 9: Act I from New game (played to the end)");
            yield return Wait(1.5f);
            MusicCheck("the title screen", "title");
            yield return Press(GamepadButton.South);
            yield return WaitFor(() => Cutscenes.Playing == "ActOne", 6f);
            Ok(Cutscenes.Playing == "ActOne", "New game starts Act I");
            yield return WaitFor(() => Cutscenes.Director && Cutscenes.Director.time >= 5f, 20f);
            MusicCheck("Act I", "act1");
            yield return WaitFor(() => Cutscenes.Director && Cutscenes.Director.time >= 45f, 60f);
            yield return Shot("r9_act1_45s");
            Ok(Music.Track == "act1", $"Act I's song still plays at 45 s (track {Music.Track ?? "none"})");
            yield return WaitFor(() => Cutscenes.Playing == "IceAgeArrival", 30f);
            Ok(!Cutscenes.LastSkipped && Cutscenes.LastDuration > 59f, $"Act I played to the end ({Cutscenes.LastDuration:0.0} s of 60)");
            yield return Wait(1.5f);
            MusicCheck("the Ice Age arrival (the landing)", "iceage");
            yield return Skip("IceAgeArrival", "iceage");

            Head("Round 9: the worlds' arrivals through the vortex (Game.GoToWorld), each skipped");
            yield return Arrive(WorldData.Egypt, Egypt.Start, "EgyptArrival", "egypt");
            SaveGame.Current.World(WorldData.Egypt).opened.Add("EG-sphinx-gag");   // as SphinxGagZone does (it plays once)
            Game.I.StartCoroutine(Game.I.SphinxGag(Egypt.GagSpot));
            yield return Played("SphinxGag", "egypt");
            yield return Arrive(WorldData.Bethlehem, Bethlehem.Start, "BethlehemArrival", "bethlehem");
            Run("WiseMenFinale");
            yield return Played("WiseMenArrive", null, true);
            yield return Arrive(WorldData.Medieval, Medieval.Start, "MedievalArrival", "medieval");
            Run("StoneFinale");
            yield return Played("TheStone", null, true);
            Ok(Party.I.Companion && Party.I.Companion.gameObject.activeSelf, "after TheStone Muscle Man follows the party");
        }

        void Run(string coroutine)
        {
            var m = typeof(Game).GetMethod(coroutine, BindingFlags.NonPublic | BindingFlags.Instance);
            Game.I.StartCoroutine((IEnumerator)m.Invoke(Game.I, null));
        }

        IEnumerator Arrive(string world, string cp, string scene, string music)
        {
            var m = typeof(Game).GetMethod("GoToWorld", BindingFlags.NonPublic | BindingFlags.Instance);
            Game.I.StartCoroutine((IEnumerator)m.Invoke(Game.I, new object[] { world, cp, true }));
            yield return Played(scene, music);
        }

        // it starts, its music, a shot, the hold skips it, control and music after
        IEnumerator Played(string scene, string music, bool toVortex = false)
        {
            yield return WaitFor(() => Cutscenes.Playing == scene, 15f);
            Ok(Cutscenes.Playing == scene, $"{scene} plays");
            yield return WaitFor(() => Cutscenes.Director && Cutscenes.Director.time >= 3f, 8f);
            if (music != null) MusicCheck(scene, music);
            else Ok(Music.Track == null, $"{scene}: no world theme under it (its own sounds) (track {Music.Track ?? "none"})");
            yield return Shot("r9_" + scene);
            yield return Skip(scene, toVortex ? "vortex" : music);
            if (toVortex) Ok(Vortex.Inside(Player.transform.position), $"after {scene}: in the vortex space");
        }

        IEnumerator Skip(string scene, string musicAfter)
        {
            yield return Hold(GamepadButton.South, 1.3f);
            yield return WaitFor(() => !Game.InCutscene && !Game.I.Busy, 10f);
            Ok(Cutscenes.LastSkipped && Cutscenes.LastDuration < 6f, $"holding A skips {scene} (after {Cutscenes.LastDuration:0.0} s)");
            yield return Wait(.6f);
            Ok(Game.Playing && !Party.I.Frozen && Player.Body.gameObject.activeSelf, $"after {scene}: control is back (party visible, not frozen)");
            var from = Player.transform.position;
            yield return Go(from + Player.transform.forward * 2.5f, .5f, 3f);
            var moved = Vector3.Distance(from, Player.transform.position);
            Ok(moved > .8f, $"after {scene}: the stick moves {Player.Stats.displayName} ({moved:0.0} m)");
            yield return Wait(1.5f);
            if (musicAfter != null) MusicCheck($"play after {scene}", musicAfter);
        }
    }
}
