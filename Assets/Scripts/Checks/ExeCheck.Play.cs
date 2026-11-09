using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace ShardsOfTime
{
    // The play-through: from the title screen through Area A and B as a player would, with the virtual pad only.
    public sealed partial class ExeCheck
    {
        bool djAtWake = true;

        IEnumerator Play()
        {
            Head("Title screen (item 13)");
            yield return Wait(1.5f);
            Ok(Game.I.State == Game.Mode.Title && Menu.I.Open, "title screen shows the menu");
            Ok(!SaveGame.Exists && Menu.I.Current.sel == 1, "no save yet: Continue greyed out, New game selected");
            var menuCanvas = Menu.I.GetComponent<Canvas>(); menuCanvas.enabled = false;
            yield return Shot("00_key_art");   // menu-free: Tools/make_title_art.py makes the launcher cover and Steam art from it
            menuCanvas.enabled = true;
            yield return Shot("01_title");
            yield return Press(GamepadButton.South);
            yield return Wait(1f);
            Ok(Game.I.State == Game.Mode.Playing && SaveGame.Exists, "A on New game starts the game (save.json written)");
            yield return Wait(.6f);
            yield return Shot("02_start");
            var w = Game.World;

            Head("Play-through, Area A (items 4-11; followers watched all the way, item 7)");
            Log("  " + w.RequirementReport("A").detail);
            yield return Go(new Vector3(0, 0, -42f)); Ok(Has("A01"), "A01 start path");
            yield return Go(new Vector3(0, 0, -36f)); Ok(Has("A02"), "A02 start path");
            yield return Go(new Vector3(-6f, 0, -30f)); Ok(Has("A03"), "A03 by the slopes");
            yield return FollowersArrive("the start path");
            // the steps, as Dan
            yield return Go(new Vector3(-14.4f, 0, -30f), .3f, 10f, .6f);
            yield return JumpTo(new Vector3(-18f, 0, -30f), () => Player.transform.position.x <= -15.2f);
            Ok(Has("A04"), $"A04 step 1 (landed at {Fmt(Player.transform.position)})");
            yield return JumpTo(new Vector3(-22f, 0, -30f), () => Player.transform.position.x <= -18.7f);
            Ok(Has("A05"), $"A05 step 2 (at {Fmt(Player.transform.position)})");
            yield return JumpTo(new Vector3(-26f, 0, -30f), () => Player.transform.position.x <= -22.7f);
            Ok(Has("A06"), $"A06 step 3 (at {Fmt(Player.transform.position)})");
            yield return Shot("03_steps");
            yield return Go(new Vector3(-26f, 0, -35.5f));
            yield return Go(new Vector3(-14f, 0, -36f));
            yield return Go(new Vector3(0, 0, -22f));
            yield return Go(new Vector3(12f, 0, -10.3f), .5f); Ok(Has("A16"), "A16 behind the pillar");
            yield return FollowersArrive("the pillar");

            // the pool, as Kira (the best swimmer)
            yield return SwitchTo(2); Ok(Player.Stats.displayName == "Kira", "RB twice: Kira");
            yield return Go(new Vector3(21.2f, 0, -1f));
            yield return Go(new Vector3(30f, 0, 2f), .4f);
            Ok(M.Mode == MoveMode.Swim, $"in the pool: mode {M.Mode}");
            Btn(GamepadButton.East, true);   // dive
            yield return WaitFor(() => Has("A08"), 6f);
            Ok(Has("A08"), $"A08 on the pool bottom (dived to y {Player.transform.position.y:0.0})");
            yield return Go(new Vector3(34f, 0, -4f), .4f, 10f);
            yield return WaitFor(() => Has("A09"), 3f);
            Ok(Has("A09"), "A09 on the pool bottom");
            yield return Shot("04_underwater");
            Btn(GamepadButton.East, false); Btn(GamepadButton.South, true);   // swim up
            yield return WaitFor(() => Player.transform.position.y > -1f, 6f);
            Btn(GamepadButton.South, false);
            yield return Go(new Vector3(38.6f, 0, -1f), .4f, 8f);
            Stick(StickFor(Vector3.right)); yield return Press(GamepadButton.South);
            yield return WaitFor(() => M.Grounded && M.Mode != MoveMode.Swim, 4f); Stick(Vector2.zero);
            yield return Go(new Vector3(43f, 0, -1f));
            Ok(M.Mode != MoveMode.Swim && Player.transform.position.y > -.2f, $"out of the pool (leap at the surface), at {Fmt(Player.transform.position)}");
            yield return FollowersArrive("after the pool (they swim)");

            // the low-gravity tower, as Abi
            yield return SwitchTo(1); Ok(Player.Stats.displayName == "Abi", "Abi");
            yield return Go(new Vector3(43f, 0, 12f));
            yield return Go(new Vector3(32f, 0, 33.7f), .25f, 25f, .6f);
            djAtWake = SaveGame.Current.Has(Abilities.DoubleJump);
            Btn(GamepadButton.South, true); Stick(Vector2.zero);
            yield return WaitFor(() => Player.transform.position.y > 5.5f, 3f);
            Ok(M.Mode == MoveMode.LowGravity, $"low gravity on the way up (mode {M.Mode}, y {Player.transform.position.y:0.0})");
            Stick(StickFor(Vector3.forward)); Btn(GamepadButton.South, false);
            yield return WaitFor(() => M.Grounded || Game.InCutscene, 4f); Stick(Vector2.zero);
            Ok(Has("A10"), $"A10 on the low-gravity tower (jump {M.LastJumpHeight:0.0} m, {M.LastAirTime:0.0} s in the air)");
            Ok(!djAtWake, "the 10th shard was reached without the double jump (rule 2)");

            Head("The necklace wakes (items 9, 11)");
            yield return WaitFor(() => Cutscenes.Playing == "NecklaceWakes", 3f);
            Ok(Cutscenes.Playing == "NecklaceWakes", "10 shards in Area A start the cutscene NecklaceWakes");
            yield return Wait(1.0f); yield return Shot("05_cutscene_necklace_1");
            yield return Wait(1.7f); yield return Shot("06_cutscene_necklace_2");
            yield return Wait(2.2f); yield return Shot("07_cutscene_necklace_3");
            yield return WaitFor(() => !Game.InCutscene && !Game.I.Busy, 10f);
            Ok(!Cutscenes.LastSkipped && Cutscenes.LastDuration > 6f, $"played to the end ({Cutscenes.LastDuration:0.0} s), not skipped");
            Ok(Game.I.StoryShard.gameObject.activeSelf && SaveGame.Current.World(w.worldId).necklaceAwake, "the story shard appears; the save says the necklace is awake");
            var before = Player.transform.position;
            yield return Go(new Vector3(32f, 0, 30f), .6f, 6f);
            Ok(Vector3.Distance(before, Player.transform.position) > 1.5f && Game.Playing, "control comes back after the cutscene");
            yield return Wait(.3f);
            Ok(Necklace.Awake && Necklace.Target == Game.I.StoryShard.transform, $"the necklace leads to the story shard ({Necklace.Distance:0} m, beat every {Necklace.Period:0.00} s)");
            yield return Shot("08_necklace_awake_hud");

            // Kira's garden (after the wake: extra shards)
            yield return SwitchTo(2);
            yield return Go(new Vector3(0, 0, 21f));
            yield return Go(new Vector3(0, 0, 26.2f), .4f, 10f, .7f);
            yield return Go(new Vector3(-2f, 0, 27f), .5f); yield return Go(new Vector3(2f, 0, 27f), .5f);
            Ok(Has("A11") && Has("A12"), "A11, A12 in Kira's crawl garden");
            yield return Shot("09_kira_garden");
            yield return Go(new Vector3(0, 0, 25.8f), .4f); yield return Go(new Vector3(0, 0, 21f), .5f, 10f, .7f);
            yield return FollowersArrive("out of Kira's garden");

            Head("Story shard and the portal (items 9, 11)");
            yield return SwitchTo(0);
            yield return Go(new Vector3(9f, 0, 22f)); yield return Go(new Vector3(9f, 0, 40f)); yield return Go(new Vector3(2.5f, 0, 48f));
            yield return Go(new Vector3(0, 0, 51f), .25f, 15f);
            yield return WaitFor(() => Cutscenes.Playing == "PortalOpens", 3f);
            Ok(Cutscenes.Playing == "PortalOpens", "the story shard starts the cutscene PortalOpens");
            yield return Wait(1.2f); yield return Shot("10_cutscene_portal_1");
            yield return Wait(1.6f); yield return Shot("10b_cutscene_portal_2");
            yield return Hold(GamepadButton.South, 1.3f);
            yield return WaitFor(() => !Game.InCutscene && !Game.I.Busy, 4f);
            Ok(Cutscenes.LastSkipped && Cutscenes.LastDuration < 5.5f, $"holding A skips it (after {Cutscenes.LastDuration:0.0} s)");
            Ok(SaveGame.Current.portals.Contains("A-B") && Game.Playing, "the portal to Area B is open; control is back");
            yield return Wait(.4f);
            yield return Shot("11_portal_open");
            yield return Go(new Vector3(2.5f, 0, 54f), .5f); yield return Go(new Vector3(0, 0, 55f), .4f);
            yield return Go(new Vector3(0, 0, 59f), .5f, 10f, 1f, () => Player.transform.position.x > 200f);
            yield return WaitFor(() => Player.transform.position.x > 200f, 3f);
            Ok(Player.transform.position.x > 200f, "walking into the portal goes to Area B");
            yield return Wait(.6f);
            yield return Shot("12_area_b");
            yield return FollowersArrive("Area B");

            Head("Area B: the double jump (item 9)");
            yield return Go(new Vector3(400f, 0, -4f)); Ok(Has("B01"), "B01");
            yield return Go(new Vector3(404f, 0, 4f)); Ok(Has("B02"), "B02");
            yield return SwitchTo(1);
            yield return Go(new Vector3(405f, 0, 20f)); yield return Go(new Vector3(400f, 0, 25.6f), .4f);
            Ok(SaveGame.Current.Has(Abilities.DoubleJump) && Player.Motor.CanDoubleJump, "the pickup unlocks Abi's double jump (saved)");
            yield return Go(new Vector3(411f, 0, 18f)); yield return Go(new Vector3(411f, 0, 10.5f));
            yield return Go(new Vector3(408f, 0, 11.3f), .25f, 20f, .6f);
            yield return JumpTo(new Vector3(408f, 0, 14f), () => true, .3f, .35f, true);
            yield return Go(new Vector3(408f, 0, 14f), .3f, 3f, .4f);
            Ok(Has("B05"), $"B05 on the high pillar with a double jump (jumps this air {M.JumpsThisAir}, at {Fmt(Player.transform.position)})");
            yield return Shot("13_double_jump");
            yield return Go(new Vector3(400f, 0, -12f), .8f, 25f);
            yield return Go(new Vector3(400f, 0, -20f), .5f, 10f, 1f, () => Player.transform.position.x < 200f);
            yield return WaitFor(() => Player.transform.position.x < 200f, 3f);
            Ok(Player.transform.position.x < 200f, "the portal back to Area A");
            yield return Wait(.5f);

            Head("Area A again: the double-jump ledge (rule 6: the move stays useful)");
            yield return Go(new Vector3(9f, 0, 30f)); yield return Go(new Vector3(9f, 0, -14f)); yield return Go(new Vector3(10f, 0, -22f));
            yield return Go(new Vector3(16f, 0, -21.1f), .25f, 30f, .7f);
            yield return JumpTo(new Vector3(16f, 0, -18f), () => true, .25f, .35f, true);
            yield return Go(new Vector3(16f, 0, -18f), .3f, 3f, .4f);
            yield return Go(new Vector3(19f, 0, -18f), .4f); yield return Go(new Vector3(22f, 0, -18f), .4f);
            Ok(Has("A17") && Has("A18") && Has("A19"), $"A17-A19 on the high ledge (at {Fmt(Player.transform.position)})");
            yield return FollowersArrive("the high ledge (end of the run)");
            yield return Shot("14_high_ledge");
            var stuck = P.Heroes.Where(h => h != Player && h.Follower.FarFor > Follower.StuckSeconds).ToList();
            Ok(stuck.Count == 0, $"no follower stuck; warps over the whole run: {string.Join(", ", P.Heroes.Select(h => h.Stats.displayName + " " + h.Follower.Warps))}");
            Log($"  shards collected: {SaveGame.Current.Total} ({string.Join(" ", SaveGame.Current.World(w.worldId).shards)})");
        }
    }
}
