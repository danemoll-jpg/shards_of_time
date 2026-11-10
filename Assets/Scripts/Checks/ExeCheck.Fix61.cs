using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace ShardsOfTime
{
    // Fix 6.1: "fix61" = the Violinist's lyre (idle and playing for Abi: front, side and the player's view; hands on the lyre, no arm
    // through his body), the HUD's universal total and the pause menu's per-world counts, and saves from 0.6.0 loaded into 0.6.1
    // (`-oldSave <save.json>`: a real 0.6.0 save after Egypt; a mid-Egypt one is made from it).
    public sealed partial class ExeCheck
    {
        IEnumerator Fix61()
        {
            Cutscenes.Started -= OnPlayed; Cutscenes.Started += OnPlayed;
            yield return EnsurePlaying(false);
            yield return LyreCheck();
            yield return OldSaves();
        }

        IEnumerator LyreCheck()
        {
            Head("Fix 6.1, item 2: the Violinist plays the lyre (his own clip, hands on the lyre)");
            var eg = SaveGame.Current.World(WorldData.Egypt); if (!eg.opened.Contains("EG-arrived")) eg.opened.Add("EG-arrived");
            SaveGame.Current.checkpoint = Egypt.Start; yield return StartWorld(WorldData.Egypt); yield return WaitFor(() => !Game.I.Busy, 10f);
            var v = Violinist.I;
            Ok(v && v.HasModel && v.playClip == "violinist_lyre" && v.Instrument && v.Instrument.name == "Lyre", $"the Violinist in Egypt: model {(v ? v.ModelName : "-")}, instrument {(v && v.Instrument ? v.Instrument.name : "none")}, playing clip {(v ? v.playClip : "-")}");
            yield return SwitchTo(0);
            yield return TpSettle(Egypt.OnGround(Egypt.ViolinistAt.x + 3.2f, Egypt.ViolinistAt.z + 3.2f) + Vector3.up * .1f, 225f);
            yield return Wait(1f);
            Ok(v.ClipNow == "idle" && v.InstrumentPose == "hold", $"idle: the lyre in his hand (clip {v.ClipNow}, pose {v.InstrumentPose})");
            yield return Shot("01_lyre_idle_player_view");
            yield return ViolinistViews(v, "02_lyre_idle");
            // Abi walks up: he plays for her (ViolinistTeachesEgypt)
            yield return Go(Egypt.ViolinistAt + new Vector3(4f, 0, 4f), .6f, 6f);
            yield return SwitchTo(1);
            yield return Go(Egypt.ViolinistAt + new Vector3(1.1f, 0, 1.1f), .3f, 15f, .6f, () => Cutscenes.Playing != null);
            yield return WaitFor(() => Cutscenes.Playing == "ViolinistTeachesEgypt", 10f);
            yield return Wait(1.0f);
            Ok(v.ClipNow == "play" && v.InstrumentPose == "play", $"he plays for Abi (cutscene {Cutscenes.Playing ?? "none"}, clip {v.ClipNow}, pose {v.InstrumentPose})");
            float ml = 0, mr = 0, tl = 0, tr = 0, clear = 9; int n = 0;
            for (int i = 0; i < 4 && Cutscenes.Playing == "ViolinistTeachesEgypt"; i++)
            {
                yield return Shot($"03_lyre_plays_cutscene_{i}");
                Log($"    ({v.ArmReport})"); ml = Mathf.Max(ml, v.MissL); mr = Mathf.Max(mr, v.MissR); tl = Mathf.Max(tl, v.TipL); tr = Mathf.Max(tr, v.TipR); clear = Mathf.Min(clear, v.ForearmClear); n++;
                if (i == 1) yield return ViolinistViews(v, "04_lyre_plays");
                yield return Wait(.45f);
            }
            Ok(n >= 2 && ml < .03f && mr < .03f, $"hands on the lyre over {n} moments: each hand within {Mathf.Max(ml, mr) * 100:0.0} cm of its spot, fingertips within {tl * 100:0} (left, behind) / {tr * 100:0} (right, in front) cm of the strings' plane (look at the close-ups)");
            Ok(n >= 2 && clear > .1f, $"no forearm through his body: they stay at least {clear * 100:0} cm from his spine");
            yield return WaitFor(() => !Game.I.Busy, 15f);
            Ok(SaveGame.Current.Has(Abilities.DoubleJump), "Abi learned the double jump");
            // and outside the cutscene, playing (as in his teaching loop), from the player's camera
            yield return TpSettle(Egypt.OnGround(Egypt.ViolinistAt.x + 3.2f, Egypt.ViolinistAt.z + 3.2f) + Vector3.up * .1f, 225f);
            yield return Wait(1.6f);
            Log($"  after: clip {v.ClipNow}, pose {v.InstrumentPose}");
            yield return Shot("05_lyre_after_player_view");
        }

        // front, side and three-quarter close-ups of him (the instrument at chest height)
        IEnumerator ViolinistViews(Violinist v, string name)
        {
            var t = v.transform; var at = t.position + Vector3.up * 1.25f; var f = t.forward; f.y = 0; f.Normalize(); var r = Vector3.Cross(Vector3.up, f);
            yield return CloseUpAt(at + f * 2.2f + Vector3.up * .15f, at, name + "_front");
            yield return CloseUpAt(at - r * 2.2f + f * .2f, at, name + "_side_left");
            yield return CloseUpAt(at + r * 2.2f + f * .2f, at, name + "_side_right");
            yield return CloseUpAt(at + (f - r).normalized * 1.6f + Vector3.up * .3f, at, name + "_34");
        }

        // Fix 6.1, item 1: 0.6.0 saves in 0.6.1
        IEnumerator OldSaves()
        {
            Head("Fix 6.1, item 1: saves from 0.6.0 (the total is their sum; nothing relocks)");
            var args = Environment.GetCommandLineArgs(); int ai = Array.IndexOf(args, "-oldSave");
            string src = ai >= 0 && ai + 1 < args.Length ? args[ai + 1] : null;
            if (src == null || !File.Exists(src)) { Fail($"no 0.6.0 save to load (-oldSave {src ?? "missing"})"); yield break; }
            string json = File.ReadAllText(src);

            // a: the real 0.6.0 save, after Egypt (both story shards)
            var old = JsonUtility.FromJson<SaveGame>(json);
            yield return LoadSave(json);
            var s = SaveGame.Current; int ia = s.World(WorldData.IceAge).shards.Count, eg = s.World(WorldData.Egypt).shards.Count;
            Ok(s.Total == ia + eg && s.Total == old.totalShards, $"0.6.0 save after Egypt: total {s.Total} = Ice Age {ia} + Egypt {eg} (the save said {old.totalShards})");
            Ok(s.World(WorldData.IceAge).storyShard && s.World(WorldData.Egypt).storyShard && s.StoryShards == 2 && s.portals.Contains("EG-vortex-gate"), $"both story shards kept, the vortex gates open ({string.Join(", ", s.portals)})");
            yield return Wait(.6f);
            Ok(Hud.TotalShown == s.Total.ToString() && Hud.UnderTotal == "", $"HUD: {Hud.TotalShown}, nothing under it (Egypt's story shard is found): \"{Hud.UnderTotal}\"");
            yield return Shot("10_oldsave_after_egypt_hud");
            Game.I.Pause(); yield return Wait(.4f);
            string body = Game.I.ShardTracker();
            Ok(body.Contains($"The Ice Age: {ia} / 60") && body.Contains($"Ancient Egypt: {eg} / 60") && body.Contains($"Shards in all: {s.Total}"), $"the pause menu's tracker: {body.Replace("\n", " | ")}");
            yield return Shot("11_pause_tracker");
            Game.I.Resume(); yield return Wait(.3f);

            // b: mid-Egypt in 0.6.0 (Ice Age done, 30 of Egypt's shards, its necklace still asleep): in 0.6.1 the total wakes it
            var mid = JsonUtility.FromJson<SaveGame>(json);
            var mw = mid.worlds.First(w => w.world == WorldData.Egypt);
            mw.shards = mw.shards.Take(30).ToList(); mw.storyShard = false; mw.necklaceAwake = false;
            mid.portals.RemoveAll(p => p == "EG-vortex-gate"); mid.worldsReached.Remove("Bethlehem"); mid.checkpoint = Egypt.Start; mid.currentWorld = WorldData.Egypt;
            int midIa = mid.worlds.First(w => w.world == WorldData.IceAge).shards.Count;
            Log($"  mid-Egypt save: Ice Age {midIa} + Egypt 30 = {midIa + 30} (0.6.0 needed 35 of Egypt's own)");
            played.Clear();
            yield return LoadSave(JsonUtility.ToJson(mid));
            yield return WaitFor(() => played.Contains("NecklaceWakesEgypt"), 12f);
            Ok(played.Contains("NecklaceWakesEgypt"), $"the total ({SaveGame.Current.Total} >= {NeedEG}) wakes Egypt's necklace on Continue (NecklaceWakesEgypt)");
            yield return Wait(2f); yield return Shot("12_oldsave_mid_egypt_wakes");
            yield return WaitFor(() => !Game.I.Busy && !Game.InCutscene, 15f);
            Ok(SaveGame.Current.World(WorldData.Egypt).necklaceAwake && Game.I.StoryShard && Game.I.StoryShard.gameObject.activeSelf && Hud.UnderTotal == "Follow the necklace",
               $"awake, the red story shard waits; HUD {Hud.TotalShown}, \"{Hud.UnderTotal}\"");

            // c: under the threshold (Ice Age 40 + Egypt 20 = 60): asleep, "Next: 75"; then collect one Egypt shard on the pad
            var low = JsonUtility.FromJson<SaveGame>(JsonUtility.ToJson(mid));
            low.worlds.First(w => w.world == WorldData.IceAge).shards = low.worlds.First(w => w.world == WorldData.IceAge).shards.Take(40).ToList();
            var lw = low.worlds.First(w => w.world == WorldData.Egypt); lw.shards = lw.shards.Take(20).ToList();
            played.Clear();
            yield return LoadSave(JsonUtility.ToJson(low));
            yield return Wait(1.5f);
            Ok(!SaveGame.Current.World(WorldData.Egypt).necklaceAwake && !played.Contains("NecklaceWakesEgypt") && Hud.TotalShown == "60" && Hud.UnderTotal == $"Next: {NeedEG}",
               $"Ice Age 40 + Egypt 20: the necklace sleeps; HUD {Hud.TotalShown}, \"{Hud.UnderTotal}\"");
            yield return Shot("13_hud_next_75");
            // the nearest open-ground shard not yet collected: set down 3 m from it, then walk onto it with the pad
            var got = SaveGame.Current.World(WorldData.Egypt).shards;
            var next = Game.World.shards.Where(sp => sp.requires.Length == 0 && !got.Contains(sp.id) && sp.position.x < 200 && Mathf.Abs(sp.position.y) < .5f)
                .OrderBy(sp => new Vector2(sp.position.x, sp.position.z).magnitude).FirstOrDefault();
            if (next == null) Fail("no open-ground Egypt shard left to pick up");
            else
            {
                yield return TpSettle(EG(next.position.x, next.position.z - 3f) + Vector3.up * .1f, 0);
                yield return EShard(next.id, EG(next.position.x, next.position.z));
                Ok(SaveGame.Current.Total == 61 && Hud.TotalShown == "61" && Hud.UnderTotal == $"Next: {NeedEG}", $"one more Egypt shard ({next.id}): the total {SaveGame.Current.Total}, HUD {Hud.TotalShown}, \"{Hud.UnderTotal}\"");
                yield return Shot("14_hud_61");
            }
            yield return Wait(.5f);
            Log("  " + TestRun.Report());
        }

        IEnumerator LoadSave(string json)
        {
            if (Game.I.State != Game.Mode.Title) { Game.I.ToTitle(); yield return Wait(.3f); }
            Directory.CreateDirectory(Path.GetDirectoryName(SaveGame.FilePath));
            File.WriteAllText(SaveGame.FilePath, json);
            Game.I.StartGame(false); yield return Wait(.6f);
            yield return WaitFor(() => !Game.InCutscene || Cutscenes.Playing == "NecklaceWakesEgypt", 5f);
            Log($"  loaded: world {Game.World.worldId}, checkpoint {SaveGame.Current.checkpoint}, total {SaveGame.Current.Total}");
        }
    }
}
