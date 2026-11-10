using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace ShardsOfTime
{
    // Round 4: the Ice Age as Dan plays it (title -> New game -> Act I -> the arrival -> the valley -> the necklace -> the Violinist
    // -> the story shard -> the vortex -> Egypt "to be continued" -> back), with the virtual pad only; then the see-through check of
    // items 1-2 (Kira's forms, Abi's outfits) and the frame rate in the valley.
    public sealed partial class ExeCheck
    {
        int ShardsHere => SaveGame.Current.World(Game.World.worldId).shards.Count;

        // fight whatever is near (attack when in reach), until nothing alive is within `radius` or the time is up
        IEnumerator Fight(float radius, float timeout = 25f)
        {
            float end = Time.time + timeout; float swing = 0;
            while (Time.time < end)
            {
                var me = Player.transform.position;
                var e = Enemy.All.Where(x => x && !x.Dead && x.isActiveAndEnabled && Vector3.Distance(x.transform.position, me) < radius).OrderBy(x => Vector3.Distance(x.transform.position, me)).FirstOrDefault();
                if (!e) break;
                var d = e.transform.position - me; d.y = 0;
                if (d.magnitude > 1.6f) Stick(StickFor(d.normalized));
                else { Stick(StickFor(d.normalized) * .2f); if (Time.time > swing) { swing = Time.time + .35f; yield return Press(GamepadButton.West); continue; } }
                yield return null;
            }
            Stick(Vector2.zero);
        }

        // go somewhere, fighting anything that comes close on the way
        IEnumerator GoF(Vector3 target, float tol = .7f, float timeout = 30f, float speed = 1f)
        {
            float end = Time.time + timeout;
            while (Time.time < end)
            {
                var near = Enemy.All.Any(x => x && !x.Dead && x.isActiveAndEnabled && x.Chasing && Vector3.Distance(x.transform.position, Player.transform.position) < 5f);
                if (near) yield return Fight(7f, 12f);
                yield return OutOfRiver();
                yield return Go(target, tol, Mathf.Min(4f, end - Time.time), speed);
                if (reached) yield break;
            }
        }

        IEnumerator Shard(string id, Vector3 at, float tol = .5f)
        {
            yield return GoF(at, tol);
            yield return WaitFor(() => Has(id), 1.5f);
            Ok(Has(id), $"{id} ({Game.World.shards.First(s => s.id == id).note}) at {Fmt(Player.transform.position)}; {ShardsHere} shards");
        }

        static Vector3 XZ(float x, float z) => new Vector3(x, 0, z);

        // fallen into the gorge's river: swim east to the ramp and walk up it (as a player would)
        IEnumerator OutOfRiver()
        {
            if (Player.transform.position.y > -4f || Mathf.Abs(Player.transform.position.z - 252) > 14) yield break;
            Log($"  (in the river at {Fmt(Player.transform.position)}: out by the ramp)");
            yield return Go(XZ(49, 252), .8f, 30f); yield return Go(XZ(49, 245), .6f, 8f); yield return Go(XZ(49, 226), .6f, 10f);
        }

        readonly System.Collections.Generic.List<string> played = new System.Collections.Generic.List<string>();
        IEnumerator IceAgePlay()
        {
            Cutscenes.Started += n => played.Add(n);
            Head("Title screen");
            yield return Wait(1.5f);
            Ok(Game.I.State == Game.Mode.Title && Menu.I.Open && !Game.TestArea, "title screen over the Ice Age valley");
            Ok(!SaveGame.Exists && Menu.I.Current.sel == 1, "no save yet: Continue greyed out, New game selected");
            var menuCanvas = Menu.I.GetComponent<Canvas>(); menuCanvas.enabled = false;
            yield return Shot("00_key_art"); menuCanvas.enabled = true;
            yield return Shot("01_title");
            MusicCheck("the title screen", "title");
            yield return Press(GamepadButton.South);

            Head("Item 3: Act I (the living room) and the Ice Age arrival");
            yield return WaitFor(() => Cutscenes.Playing == "ActOne", 4f);
            Ok(Cutscenes.Playing == "ActOne" && SaveGame.Exists, "New game starts Act I (save written)");
            foreach (var (t, n) in new[] { (2f, "02a_act1_evening"), (4.8f, "02b_act1_kira"), (7.1f, "02c_act1_swipe"), (9.1f, "02d_act1_shatter"), (12.5f, "02e_act1_vortex"), (16.2f, "02f_act1_pulled_in") })
            { yield return WaitFor(() => Cutscenes.Director && Cutscenes.Director.time >= t, 25f); yield return Shot(n); if (t == 2f) MusicCheck("Act I", "act1"); }
            // Act I's music fades over the white-out: the fade starts at Game.ActOneMusicFadeAt (2.2 s), the track is gone after it
            yield return WaitFor(() => Music.Track == null || !Cutscenes.Director, 25f);
            double fadeAt = Cutscenes.Director ? Cutscenes.Director.time : -1;
            yield return new WaitForSeconds(2.6f);   // game time: the fade runs in game time (a busy frame-capped run lags real time)
            var act1 = Game.I.GetComponents<AudioSource>().Where(x => x.isPlaying && x.clip && x.clip.name == "act1").ToList();
            Ok(fadeAt >= Game.ActOneMusicFadeAt - .1f && fadeAt < 18.3f && act1.All(x => x.volume < .02f),
               $"Act I's music fades out over the white-out (fade from {fadeAt:0.00} s of 19; act1 {(act1.Count > 0 ? $"volume {act1.Max(x => x.volume):0.00}" : "stopped")} 2.6 s later)");
            yield return WaitFor(() => Cutscenes.Playing == "IceAgeArrival", 8f);
            Ok(!Cutscenes.LastSkipped && Cutscenes.LastDuration > 18f, $"Act I played to the end ({Cutscenes.LastDuration:0.0} s)");
            Ok(Cutscenes.Playing == "IceAgeArrival", "then the Ice Age arrival");
            foreach (var (t, n) in new[] { (.6f, "03a_arrival_landing"), (3.2f, "03b_arrival_dan_gets_up"), (4.9f, "03c_arrival_silhouette"), (6.9f, "03d_arrival_fall"),
                                            (9.2f, "03e_arrival_its_kira"), (12.0f, "03f_arrival_necklace"), (13.4f, "03g_arrival_abi_laughs") })
            { yield return WaitFor(() => Cutscenes.Director && Cutscenes.Director.time >= t, 15f); yield return Shot(n); if (n.StartsWith("03a")) MusicCheck("the arrival", "iceage"); }
            yield return Hold(GamepadButton.South, 1.3f);
            yield return WaitFor(() => !Game.InCutscene && !Game.I.Busy, 5f);
            Ok(Cutscenes.LastSkipped, $"holding A skips the arrival (after {Cutscenes.LastDuration:0.0} s)");
            Ok(Game.Playing && P.Heroes[0].Outfit == Outfits.IceAge && P.Heroes[2].Form && P.Heroes[2].Form.id == KiraForm.Sabertooth,
               $"play starts in the snow: caveman outfits, sabertooth Kira; at {Fmt(Player.transform.position)}");
            yield return Wait(.5f); yield return Shot("04_start_view");
            var w = Game.World;
            Log("  " + w.RequirementReport("V").detail);
            var rep = w.RequirementReport("V");
            Ok(rep.ok && rep.reachable >= 25, $"rule 2 by data: {rep.reachable} of {w.InArea("V").Count()} shards reachable on the first visit, requirement {rep.requirement}");

            Head("Item 4: the landing snowfield and the pond");
            yield return Shard("S01", XZ(0, 24)); yield return Shard("S02", XZ(-6, 36)); yield return Shard("S03", XZ(6, 46));
            yield return FollowersArrive("the snowfield");
            yield return SwitchTo(2); Ok(Player.Stats.displayName == "Kira", "Kira for the pond (she never runs out of air)");
            yield return Go(XZ(9.6f, 53), .4f, 20f, .8f);
            yield return JumpTo(XZ(12.5f, 53), () => true, .6f); yield return JumpTo(XZ(17, 52), () => true, .6f);
            yield return WaitFor(() => Has("S09"), 1.5f); Ok(Has("S09"), $"S09 on an ice floe (at {Fmt(Player.transform.position)})");
            yield return Go(XZ(21.5f, 55), .6f, 10f);
            Btn(GamepadButton.East, true);
            yield return Go(XZ(22, 55), .5f, 6f); yield return WaitFor(() => Has("S07"), 5f);
            Ok(Has("S07") && M.Mode == MoveMode.Swim, $"S07 on the pond bottom (dived to y {Player.transform.position.y:0.0})");
            yield return Shot("05_pond_underwater");
            yield return Go(XZ(27, 60), .5f, 8f); yield return WaitFor(() => Has("S08"), 3f); Ok(Has("S08"), "S08 on the pond bottom");
            Btn(GamepadButton.East, false);
            yield return Go(XZ(37, 62), .8f, 15f);
            Ok(M.Mode != MoveMode.Swim, $"out of the pond at {Fmt(Player.transform.position)}");
            yield return SwitchTo(0);
            yield return Shard("S04", XZ(-14, 62)); yield return Shard("S05", XZ(-4, 80)); yield return Shard("S06", XZ(10, 96));
            yield return FrozenLake();

            Head("Item 4: the mammoth ridge (ride a mammoth to the shelf)");
            yield return Shard("R01", XZ(30, 120));
            yield return GoF(XZ(50.75f, 106), .5f); yield return Go(XZ(50.75f, 108.2f), .3f, 6f, .5f);
            yield return JumpTo(XZ(50.75f, 110.6f), () => true, .5f); yield return JumpTo(XZ(50.75f, 113.8f), () => true, .5f);
            yield return JumpTo(XZ(50.75f, 117.5f), () => true, .5f);
            yield return Go(XZ(51, 120), .4f, 5f, .5f); yield return WaitFor(() => Has("R07"), 1f);
            Ok(Has("R07"), $"R07 on the loading rock (y {Player.transform.position.y:0.0})");
            yield return Go(XZ(49.2f, 119), .3f, 5f, .4f);
            Mammoth ride = null;
            yield return WaitFor(() => (ride = Mammoth.All.FirstOrDefault(m => m.transform.forward.z > .9f && Mathf.Abs(m.transform.position.x - 46) < .3f && Mathf.Abs(m.transform.position.z - 118.5f) < .8f)) != null, 130f);
            Ok(ride, "a mammoth comes past the loading rock");
            if (ride)
            {
                var mount = ride.transform;
                yield return JumpTo(mount.position + Vector3.forward * 1.2f, () => true, .55f, .3f);
                yield return Wait(.4f);
                bool onBack = M.Grounded && M.OnPlatform && Player.transform.position.y > 3.6f;
                Ok(onBack, $"jumped onto its back (y {Player.transform.position.y:0.0}, riding {M.OnPlatform})");
                yield return Shot("06_riding_mammoth");
                float z0 = Player.transform.position.z;
                yield return WaitFor(() => ride.transform.position.z > 157f, 30f);
                Ok(Player.transform.position.z > z0 + 25f && Player.transform.position.y > 3.6f, $"the mammoth carried the party {Player.transform.position.z - z0:0} m up the ridge");
                yield return JumpTo(new Vector3(49.6f, 0, ride.transform.position.z + 1.2f), () => true, 1f, .35f);
                Ok(Player.transform.position.y > 5f, $"jumped from its back onto the high shelf (y {Player.transform.position.y:0.0})");
                yield return Shard("R04", XZ(51, 158)); yield return Shard("R05", XZ(55, 180)); yield return Shard("R06", XZ(51, 202));
                yield return Shot("07_shelf");
                yield return Go(XZ(51, 214), .6f, 6f);
            }
            yield return Shard("R03", XZ(28, 190));
            yield return Fight(12f, 30f);
            Ok(true, $"the ridge wolves: {string.Join(", ", Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include).Where(e => e.kind == Enemy.Kind.Wolf && e.transform.position.z < 260).Select(e => $"{(e.Dead ? "defeated" : "hp " + e.hp)}, {e.Lunges} lunges"))}");
            yield return Shard("R02", XZ(20, 150));

            Head("Item 4: the ice pillars, the cracked bridge, the river");
            yield return GoF(XZ(20, 239.5f), .4f);
            Ok(Health.Hearts > 0, $"hearts {Health.Hearts}/{Health.Max}");
            foreach (var (z, id) in new[] { (243.5f, "V01"), (248f, "V02"), (252.5f, "V03"), (257f, "V04") })
            {
                float edge = z - 4.5f + 1.0f;   // run to the near edge of the last pillar (or the bank), then jump
                yield return JumpTo(XZ(20, z), () => Player.transform.position.z > edge, 1f);
                yield return WaitFor(() => Has(id), 1f); Ok(Has(id), $"{id} on an ice pillar (at {Fmt(Player.transform.position)})");
            }
            yield return Shot("08_pillars");
            yield return JumpTo(XZ(20, 261.5f), () => Player.transform.position.z > 258f, 1f);
            yield return JumpTo(XZ(20, 266), () => Player.transform.position.z > 262.5f, 1f);
            Ok(Player.transform.position.z > 262f && Player.transform.position.y > -1.5f, $"across the gorge on the pillars ({Fmt(Player.transform.position)})");
            yield return Go(XZ(-10, 272), .5f);
            yield return SwitchTo(2);
            var planks = Object.FindObjectsByType<CrumblePlatform>();
            yield return Go(XZ(-10, 260.5f), .4f, 8f, .7f);
            yield return Go(XZ(-10, 254f), .4f, 4f, .5f);
            Stick(Vector2.zero);
            yield return WaitFor(() => M.Mode == MoveMode.Swim, 4f);
            Ok(planks.Sum(p => p.Drops) > 0 && M.Mode == MoveMode.Swim, $"the cracked plank dropped under Kira ({planks.Sum(p => p.Drops)} drops): into the river");
            Btn(GamepadButton.East, true);
            yield return Go(XZ(-10, 250), .5f, 6f); yield return WaitFor(() => Has("V05"), 4f);
            Ok(Has("V05"), "V05 on the river bottom under the bridge");
            Btn(GamepadButton.East, false);
            yield return Go(XZ(49, 251), .8f, 25f);
            yield return Go(XZ(49, 246), .6f, 6f);
            yield return Shard("V06", XZ(49, 236));
            yield return Go(XZ(49, 226), .6f, 8f);
            Ok(Player.transform.position.y > -1f, $"up the ramp out of the gorge ({Fmt(Player.transform.position)})");
            yield return SwitchTo(0);
            yield return GoF(XZ(20, 239.5f), .4f);
            foreach (var z in new[] { 243.5f, 248f, 252.5f, 257f, 261.5f, 266f }) { float edge = z - 4.5f + 1.0f; yield return JumpTo(XZ(20, z), () => Player.transform.position.z > edge, 1f); }
            yield return OutOfRiver();
            yield return GlacierClimb();

            Head("Item 4: the ice caves (dark; Kira's crawl tunnel; the ice wall)");
            yield return GoF(XZ(-8, 333)); yield return Go(XZ(-19, 333), .5f);
            yield return Go(XZ(-27, 333), .5f);
            yield return Wait(.6f);
            var gem = P.Heroes[0].NecklaceGem; var neckLight = gem ? gem.Find("Necklace light") : null;
            Ok(ShardsOfTime.Look.Dark && neckLight && neckLight.gameObject.activeSelf, $"inside the cave it is dark and Dan's necklace gives a light (sun {ShardsOfTime.Look.Sun.intensity:0.00})");
            yield return Shot("09_cave_dark");
            yield return Shard("C01", XZ(-38, 316)); yield return Shard("C02", XZ(-38, 352));
            yield return SwitchTo(2);
            yield return Go(XZ(-38.6f, 320), .3f, 15f, .6f);
            yield return Go(XZ(-44, 320), .3f, 6f, .6f);
            Ok(Player.transform.position.x < -42.5f, $"Kira crawls through the gap (at {Fmt(Player.transform.position)})");
            yield return Shard("C03", XZ(-58, 316)); yield return Shard("C04", XZ(-58, 324));
            yield return Shot("10_crawl_chamber");
            yield return Go(XZ(-44, 320), .3f, 8f, .6f); yield return Go(XZ(-38, 320), .4f, 6f, .6f);
            yield return Go(XZ(-31, 355.95f), .3f, 15f, .6f);
            Stick(StickFor(Vector3.forward) * .3f); yield return Wait(.25f); Stick(Vector2.zero);
            var iw = IceWall.All.FirstOrDefault(x => x.id == "IA-ice-cave");
            Log($"  (at the ice wall: Kira at {Fmt(Player.transform.position)} facing {Fmt(Player.transform.forward)}, wall {(iw ? Fmt(iw.transform.position) : "missing")}, player {Player.Stats.displayName}, form {Player.Form?.id}, near: {string.Join(",", Physics.OverlapSphere(Player.transform.position + Vector3.up * .25f + Player.transform.forward * .7f, .6f).Select(c => c.name))})");
            for (int k = 0; k < 3 && !SaveGame.Current.World(w.worldId).opened.Contains("IA-ice-cave"); k++) { yield return Press(GamepadButton.West); yield return Wait(.5f); }
            Ok(SaveGame.Current.World(w.worldId).opened.Contains("IA-ice-cave"), "sabertooth Kira claws the ice wall open");
            yield return Shard("C05", XZ(-31, 362));
            int beforeWake = ShardsHere;
            bool gpBeforeWake = SaveGame.Current.Has(Abilities.GroundPound);
            Ok(beforeWake >= 35, $"{beforeWake} shards without the ground pound, the double jump or Muscle Man (rule 2, play-through; Round 5: 35 needed)");
            yield return Go(XZ(-31, 340), .8f); yield return Go(XZ(-27, 333), .8f); yield return Go(XZ(-16, 330), .8f);

            Head("Items 5, 11: the necklace wakes (35 shards, Round 5)");
            yield return WaitFor(() => played.Contains("NecklaceWakes"), 8f);
            Ok(played.Contains("NecklaceWakes") && !gpBeforeWake, "out of the cave, the necklace wakes (cutscene NecklaceWakes, Ice Age actors)");
            yield return Wait(1.0f); yield return Shot("11a_necklace_wakes_1"); yield return Wait(2.2f); yield return Shot("11b_necklace_wakes_2");
            yield return WaitFor(() => !Game.InCutscene && !Game.I.Busy, 10f);
            Ok(Game.I.StoryShard.gameObject.activeSelf && Necklace.Awake, $"the blue story shard appears; the necklace leads to it ({Necklace.Distance:0} m)");

            Head("Item 6: the Violinist teaches Dan the ground pound");
            yield return SwitchTo(2);
            yield return Go(IceAge.ViolinistAt + new Vector3(1.6f, 0, -1.2f), .5f);
            yield return Wait(.4f);
            Ok(!SaveGame.Current.Has(Abilities.GroundPound) && !Game.I.Busy && Hud.LastMessage.Contains("looking past you"), $"Kira reaches him: he looks past her, at Dan (\"{Hud.LastMessage}\")");
            yield return SwitchTo(0);
            yield return Go(IceAge.ViolinistAt + new Vector3(1.4f, 0, -1.4f), .5f, 8f, .6f);
            yield return WaitFor(() => Cutscenes.Playing == "ViolinistTeaches", 4f);
            Ok(Cutscenes.Playing == "ViolinistTeaches", "Dan reaches him: he plays (cutscene ViolinistTeaches)");
            yield return Wait(1.2f); yield return Shot("12a_violinist_plays"); yield return Wait(2f); yield return Shot("12b_dan_learns");
            yield return WaitFor(() => !Game.InCutscene && !Game.I.Busy, 10f);
            Ok(SaveGame.Current.Has(Abilities.GroundPound) && M.CanGroundPound, $"Dan learned the ground pound (saved): \"{Hud.LastMessage}\"");

            Head("Item 6: the ground pound (the cave's cracked ice)");
            yield return Go(XZ(-19, 333), .5f); yield return Go(XZ(-26, 342.5f), .5f); yield return Go(XZ(-30.8f, 342.5f), .3f, 6f, .5f);
            int pounds = M.Pounds;
            Btn(GamepadButton.South, true); yield return WaitFor(() => M.Velocity.y < .5f && !M.Grounded, 1.5f); Btn(GamepadButton.South, false);
            yield return Press(GamepadButton.East);
            yield return WaitFor(() => M.Pounds > pounds, 3f);
            Ok(M.Pounds > pounds, "jump, then B: Dan slams down");
            yield return Wait(1.2f);
            Ok(SaveGame.Current.World(w.worldId).opened.Contains("IA-crack-cave") && Player.transform.position.y < -2.5f, $"the cracked ice breaks: Dan drops into the pit (y {Player.transform.position.y:0.0})");
            yield return Shard("C06", XZ(-33, 338)); yield return Shard("C07", XZ(-26, 347));
            yield return Go(XZ(-31, 342.5f), .4f);
            yield return JumpTo(XZ(-29.5f, 342.5f), () => true, .4f); yield return JumpTo(XZ(-28.45f, 342.5f), () => true, .4f);
            yield return JumpTo(XZ(-27.45f, 342.5f), () => true, .4f); yield return JumpTo(XZ(-25.3f, 342.5f), () => true, .4f);
            Ok(Player.transform.position.y > -.3f, $"up the steps out of the pit ({Fmt(Player.transform.position)})");
            yield return Go(XZ(-27, 333), .8f); yield return Go(XZ(-16, 331), .8f);

            Head("Item 4: the volcano vents (steam lifts the party)");
            yield return GoF(XZ(10, 380)); yield return GoF(XZ(30, 398)); yield return Go(XZ(40, 418.5f), .4f);
            yield return Go(XZ(40, 420), .3f, 4f, .4f);
            foreach (var (vx, vz, topY, landX, id, shard) in new[] { (40f, 420f, 11f, 46.5f, "H01", XZ(47, 436)), (48f, 422f, 19f, 54f, "H02", XZ(58, 437)), (53.6f, 420f, 27f, 58.5f, "H03", XZ(60, 414)) })
            {
                yield return Go(XZ(vx, vz), .35f, 8f, .5f);
                yield return WaitFor(() => Player.transform.position.y > topY, 6f);
                Ok(Player.transform.position.y > topY, $"the steam lifts {Player.Stats.displayName} to y {Player.transform.position.y:0.0}");
                if (id == "H01") yield return Shot("13_steam_vent");
                yield return Go(XZ(landX, vz), .6f, 4f);
                yield return WaitFor(() => M.Grounded, 3f);
                yield return Shard(id, shard);
            }
            yield return Go(XZ(57.2f, 417), .4f, 5f); yield return Go(XZ(57.2f, 423.5f), .4f, 5f);   // round the ice shelf
            yield return Shard("H04", XZ(60, 426));
            yield return Shot("14_terrace_top");
            yield return Go(XZ(54, 430), .6f, 6f); yield return Go(XZ(36, 446), 1f, 10f);
            yield return HotSpringWall();

            Head("Item 4: the standing stones, the frozen well, the story shard");
            yield return GoF(XZ(0, 456)); yield return Fight(14f, 30f);
            yield return Shard("T01", XZ(-12, 476)); yield return Fight(10f, 15f);
            yield return Shard("T02", XZ(12, 500));
            yield return Go(XZ(18, 474), .5f); yield return Go(XZ(18, 477.5f), .3f, 4f, .4f);
            Btn(GamepadButton.South, true); yield return WaitFor(() => M.Velocity.y < .5f && !M.Grounded, 1.5f); Btn(GamepadButton.South, false);
            yield return Press(GamepadButton.East); yield return Wait(1.4f);
            Ok(SaveGame.Current.World(w.worldId).opened.Contains("IA-crack-well"), "the ground pound breaks the frozen well's ice");
            yield return Shard("T04", XZ(18, 478));
            yield return Fight(9f, 15f);
            for (int k = 0; k < 4 && Player.transform.position.y < -.4f; k++)
            {
                yield return Fight(7f, 10f);
                yield return Go(XZ(18.7f, 478), .3f, 6f, .5f);
                foreach (var x in new[] { 19.7f, 20.5f, 21.3f, 22.4f }) yield return JumpTo(XZ(x, 478), () => true, .6f);
                yield return Go(XZ(25, 478), .5f, 4f);
                if (Player.transform.position.y < -.4f) Log($"  (still in the well at {Fmt(Player.transform.position)}: again)");
            }
            yield return Shot("15_stones");
            var boulder = Object.FindObjectsByType<Boulder>().FirstOrDefault();
            yield return Go(XZ(25, 470), .8f); yield return Go(XZ(10, 469), .8f);   // round the open well
            yield return Go(XZ(-40.5f, 484), .5f); Stick(StickFor(Vector3.left)); yield return Wait(.6f); Stick(Vector2.zero);
            Ok(boulder && boulder.Touches > 0 && !Has("T03") && Hud.LastMessage.Contains("Muscle Man"), "the boulder needs Muscle Man (a future-ability gate, visible but closed)");
            yield return Go(XZ(-7, 494.5f), .4f); Btn(GamepadButton.South, true); yield return Wait(.5f); Btn(GamepadButton.South, false); yield return Wait(.6f);
            Ok(!Has("T05"), "T05 on the stone lintel is out of a single jump's reach (Abi's double jump, later)");
            Log($"  {ShardsHere} of {w.shards.Count} shards before the story shard; hearts {Health.Hearts}/{Health.Max}; respawns {Health.Respawns}");
            yield return Go(XZ(0, 487.5f), .4f);
            yield return JumpTo(XZ(0, 490), () => true, .4f);
            yield return WaitFor(() => played.Contains("VortexOpens"), 6f);
            Ok(played.Contains("VortexOpens"), "the blue story shard opens the vortex (cutscene VortexOpens)");
            yield return Wait(2.6f); yield return Shot("16_vortex_opens");
            yield return Hold(GamepadButton.South, 1.3f);
            yield return WaitFor(() => !Game.InCutscene && !Game.I.Busy, 5f);
            Ok(Cutscenes.LastSkipped && SaveGame.Current.portals.Contains("IA-vortex") && IceAge.Gate.Open, $"skipped after {Cutscenes.LastDuration:0.0} s; the vortex gate is open (saved)");
            yield return Wait(.4f); yield return Shot("17_vortex_gate");

            Head("Item 5: the vortex (paths: Ice Age, Egypt, Home)");
            yield return WaitFor(() => !Game.InCutscene && !Game.I.Busy, 8f);
            yield return Go(XZ(0, 499), .5f); yield return Go(XZ(0, 507), .5f, 8f, 1f, () => Vortex.Inside(Player.transform.position));
            yield return WaitFor(() => Vortex.Inside(Player.transform.position), 3f);
            Ok(Vortex.Inside(Player.transform.position), "stepping into the vortex takes the party into the vortex space");
            yield return Wait(.6f); yield return Shot("18_vortex_space"); MusicCheck("the vortex space", "vortex");
            var c = Vortex.Centre;
            Vector3 PathPt(float yaw, float d) => c + Quaternion.Euler(0, yaw, 0) * new Vector3(0, 0, d);
            // Round 6: four paths (the Ice Age -54, Egypt -18, Bethlehem 18, Home 54); Egypt is real now (EgyptRoute), so here Home and
            // Bethlehem bounce, then back to the valley
            yield return Go(PathPt(54, 8), .6f); yield return Go(PathPt(54, 19), .6f); yield return Go(PathPt(54, 24), .5f, 6f);
            yield return Wait(.5f);
            Ok(Vortex.Inside(Player.transform.position) && Hud.LastMessage.Contains("still missing"), $"Home is blocked: \"{Hud.LastMessage}\"");
            yield return Shot("19_home_blocked");
            yield return Go(PathPt(54, 12), .6f); yield return Go(PathPt(54, 6), .6f); yield return Go(PathPt(18, 6), .6f);
            yield return Go(PathPt(18, 19), .6f); yield return Go(PathPt(18, 24), .5f, 6f); yield return Wait(.5f);
            Ok(Vortex.Inside(Player.transform.position) && Hud.LastMessage.Contains("Bethlehem"), $"Bethlehem waits for Egypt's red shard: \"{Hud.LastMessage}\"");
            yield return Shot("20_bethlehem_closed");
            yield return Go(PathPt(18, 12), .6f); yield return Go(PathPt(0, 4), .8f);
            yield return Go(PathPt(-54, 8), .6f); yield return Go(PathPt(-54, 19), .6f); yield return Go(PathPt(-54, 24), .5f, 6f, 1f, () => !Vortex.Inside(Player.transform.position));
            yield return WaitFor(() => !Vortex.Inside(Player.transform.position), 3f);
            Ok(!Vortex.Inside(Player.transform.position) && Player.transform.position.z > 480f && Player.transform.position.z < 520f && Player.transform.position.y < 50f, $"the Ice Age path leads back to the valley ({Fmt(Player.transform.position)})");
            yield return Wait(.5f); yield return Shot("21_back_in_the_valley"); MusicCheck("back in the valley", "iceage");
            yield return FollowersArrive("back from the vortex");
            var stuck = P.Heroes.Where(h => h != Player && h.Follower.FarFor > Follower.StuckSeconds).ToList();
            Ok(stuck.Count == 0, $"no follower stuck; warps over the run: {string.Join(", ", P.Heroes.Select(h => h.Stats.displayName + " " + h.Follower.Warps))}");
            Log($"  shards: {ShardsHere} of {w.shards.Count} ({string.Join(" ", SaveGame.Current.World(w.worldId).shards)}); story shards {SaveGame.Current.StoryShards}; abilities [{string.Join(",", SaveGame.Current.abilities)}]; worlds reached [{string.Join(",", SaveGame.Current.worldsReached)}]");
            Log($"  enemies: {string.Join("; ", Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include).GroupBy(e => e.kind).Select(g => $"{g.Key} {g.Count(e => e.Dead)}/{g.Count()} defeated"))}; hits taken {Health.HitsTaken}, respawns {Health.Respawns}");
            var srcs = Game.I.GetComponents<AudioSource>();
            Ok(Music.HasClip && Music.Track == "iceage" && srcs.Any(x => x.isPlaying && x.clip && x.clip.name == "iceage") && srcs.Any(x => x.isPlaying && x.clip && x.clip.name == "wind_loop"),
               $"music: the Ice Age theme and the wind are playing ({string.Join(", ", srcs.Where(x => x.isPlaying && x.clip).Select(x => $"{x.clip.name} {x.clip.length:0} s"))}); {Sfx.Played} sound effects played over the run");
            foreach (var n in new[] { "footstep_snow", "crack", "shard", "necklace", "growl", "pound", "violinist_tune" })
                Ok(Resources.Load<AudioClip>("Sounds/" + n), $"sound {n} is in the build (ElevenLabs)");
            Log("  " + TestRun.Report());
        }

        // ---------------------------------------------------------------- items 1-2: see-through holes, in the game's own renderer
        // Each model alone in front of a magenta background, at 8 angles round it (and close-ups of the head): drawn as the game draws it,
        // then drawn two-sided; pixels that are model in the second and background in the first are see-through holes.
        IEnumerator Fixes()
        {
            yield return EnsurePlaying(false);
            Head("Items 1-2: see-through holes (Kira's forms, Abi's outfits), measured in the game's renderer");
            // in the vortex space nothing changes a form or an outfit (no world area)
            yield return TpSettle(Vortex.Centre + new Vector3(0, .1f, -1f), 0);
            var kira = P.Heroes[2]; var abi = P.Heroes[1];
            foreach (var (h, label, set) in new (Hero, string, System.Action)[]
            {
                (kira, "kira", () => kira.SetForm(KiraForm.Load(KiraForm.Home))), (kira, "kira_sabertooth", () => kira.SetForm(KiraForm.Load(KiraForm.Sabertooth))),
                (abi, "abi_home", () => abi.SetOutfit(Outfits.Home)), (abi, "abi_iceage", () => abi.SetOutfit(Outfits.IceAge)),
            })
            {
                set(); yield return Wait(.3f);
                yield return Turntable(h, label);
            }
            Head("Items 1-2: the player's view while running (vortex platform)");
            foreach (var (i, label, set) in new (int, string, System.Action)[]
            {
                (1, "abi_home", () => abi.SetOutfit(Outfits.Home)), (1, "abi_iceage", () => abi.SetOutfit(Outfits.IceAge)),
                (2, "kira_sabertooth", () => kira.SetForm(KiraForm.Load(KiraForm.Sabertooth))), (2, "kira", () => kira.SetForm(KiraForm.Load(KiraForm.Home))),
            })
            {
                yield return SwitchTo(i); set();
                yield return TpSettle(Vortex.Centre + new Vector3(0, .1f, -5f), 0);
                Stick(StickFor(Vector3.right)); yield return Wait(.5f); yield return Shot($"r4_run_{label}_side");
                Stick(StickFor(Vector3.back)); yield return Wait(.6f); yield return Shot($"r4_run_{label}_front");
                Stick(StickFor(Vector3.forward)); yield return Wait(.6f); yield return Shot($"r4_run_{label}_back");
                Stick(Vector2.zero); yield return Wait(.3f);
            }
        }

        IEnumerator Turntable(Hero h, string label)
        {
            if (!TestRun.Headless || !h.Model) { Log($"  (no turntable for {label}: {(h.Model ? "not headless" : "no model")})"); yield break; }
            yield return new WaitForEndOfFrame();
            const int L = 30, S = 480;
            var rends = h.Model.GetComponentsInChildren<Renderer>(true);
            var layers = rends.Select(r => r.gameObject.layer).ToArray();
            foreach (var r in rends) r.gameObject.layer = L;
            var go = new GameObject("Turntable camera"); var cam = go.AddComponent<Camera>();
            var rt = new RenderTexture(S, S, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 }; rt.Create();
            cam.targetTexture = rt; cam.cullingMask = 1 << L; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.magenta;
            cam.fieldOfView = 30f; cam.nearClipPlane = .02f;
            var b = new Bounds(h.Model.transform.position, Vector3.zero); foreach (var r in rends) if (r.enabled) b.Encapsulate(r.bounds);
            float dist = b.extents.magnitude / Mathf.Tan(15f * Mathf.Deg2Rad) * 1.05f;
            const int Cols = 6, Rows = 4;   // Round 5: 16 angles + close-ups (Dan: ears, tail, paws, fangs)
            var sheet = new Texture2D(S * Cols, S * Rows, TextureFormat.RGB24, false);
            int total = 0, worst = 0; string worstView = "";
            float hy = h.transform.eulerAngles.y;   // dir = from what it looks at toward the camera
            var views = Enumerable.Range(0, 16).Select(i => (n: $"yaw{i * 22.5f:000}", dir: Quaternion.Euler(i % 2 == 0 ? -12f : 8f, hy + i * 22.5f, 0) * Vector3.forward, aim: b.center, d: dist)).ToList();
            var headT = h.IsPerson ? (h.Anim ? h.Anim.GetComponentInChildren<Animator>() : null)?.GetBoneTransform(HumanBodyBones.Head) : h.Model.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Head");
            if (headT)
                foreach (var (n, yaw, pitch) in new[] { ("head_front", 0f, 8f), ("head_back", 180f, 15f), ("head_left", -90f, 10f), ("head_top", 200f, 65f) })
                    views.Add((n, Quaternion.Euler(-pitch, hy + yaw, 0) * Vector3.forward, headT.position, (h.IsPerson ? .55f : .3f) / Mathf.Tan(15f * Mathf.Deg2Rad)));
            if (!h.IsPerson)   // Round 5: Kira's tail, paws and fangs close up
            {
                Transform Bone(string n) => h.Model.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == n);
                float near = .16f / Mathf.Tan(15f * Mathf.Deg2Rad);
                foreach (var (n, bone, yaw, pitch) in new[] { ("tail", "Tail3", 150f, 10f), ("paws_front", "FL_Paw", 20f, 5f), ("paws_back", "BR_Paw", 200f, 5f), ("fangs", "Head", 0f, -12f) })
                {
                    var t = Bone(bone); if (!t) continue;
                    var aim = n == "fangs" ? t.position + h.transform.forward * .05f - Vector3.up * .05f : t.position;
                    views.Add((n, Quaternion.Euler(-pitch, hy + yaw, 0) * Vector3.forward, aim, near));
                }
            }
            var mats = rends.Select(r => r.sharedMaterials).ToArray();
            var two = rends.Select(r => r.sharedMaterials.Select(m => { var c2 = new Material(m); c2.SetFloat("_Cull", 0f); return c2; }).ToArray()).ToArray();
            for (int v = 0; v < views.Count; v++)
            {
                var (n, dir, aim, d) = views[v];
                go.transform.position = aim + dir.normalized * d; go.transform.LookAt(aim);
                var a = Grab(cam, rt);
                for (int i = 0; i < rends.Length; i++) rends[i].sharedMaterials = two[i];
                var bb = Grab(cam, rt);
                for (int i = 0; i < rends.Length; i++) rends[i].sharedMaterials = mats[i];
                int holes = 0;
                for (int p = 0; p < a.Length; p++)
                {
                    bool bgA = a[p].r > 240 && a[p].g < 15 && a[p].b > 240, bgB = bb[p].r > 240 && bb[p].g < 15 && bb[p].b > 240;
                    if (bgA && !bgB) { holes++; a[p] = new Color32(0, 255, 0, 255); }
                    else if (bgA) a[p] = new Color32(70, 70, 80, 255);
                }
                total += holes; if (holes > worst) { worst = holes; worstView = n; }
                if (v < Cols * Rows) sheet.SetPixels32((v % Cols) * S, (Rows - 1 - v / Cols) * S, S, S, a);
                Log($"    {label} {n}: {holes} see-through pixels");
            }
            sheet.Apply(); File.WriteAllBytes(Path.Combine(dir, $"r4_turntable_{label}.png"), sheet.EncodeToPNG());
            Destroy(sheet); foreach (var arr in two) foreach (var m in arr) Destroy(m);
            for (int i = 0; i < rends.Length; i++) rends[i].gameObject.layer = layers[i];
            cam.targetTexture = null; rt.Release(); Destroy(rt); Destroy(go);
            Ok(total == 0, $"{label}: {total} see-through pixels over {views.Count} views of {S}x{S} (worst {worst} in {worstView}; must be 0, Round 5); sheet r4_turntable_{label}.png (holes in green)");
        }

        static Color32[] Grab(Camera cam, RenderTexture rt)
        {
            cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var t = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false); t.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); t.Apply();
            RenderTexture.active = prev;
            var px = t.GetPixels32(); Destroy(t); return px;
        }

        // a quick look at single spots (teleports; not part of "all")
        IEnumerator Diag()
        {
            yield return EnsurePlaying(false);
            SaveGame.Current.abilities.Add(Abilities.GroundPound); P.ApplyAbilities();
            foreach (var u in UpdraftVolume.All) Log($"  updraft at {Fmt(u.transform.position)} size {Fmt(u.transform.lossyScale)} active {u.isActiveAndEnabled}");
            foreach (var (x, y, z) in new[] { (40f, 0f, 420f), (48f, 8f, 422f), (53.6f, 16f, 420f) })
            {
                yield return TpSettle(new Vector3(x, y + .2f, z), 0);
                Log($"  at {Fmt(Player.transform.position)} centre {Fmt(M.Center)}: updraft {(UpdraftVolume.At(M.Center) ? "yes" : "no")}, grounded {M.Grounded}, ground {(M.GroundCollider ? M.GroundCollider.name : "-")}");
                yield return Wait(1.5f);
                Log($"    after 1.5 s: y {Player.transform.position.y:0.0}, vy {M.Velocity.y:0.0}, updraft {(M.Updraft ? "yes" : "no")}");
            }
            var cap = CrackedFloor.All.FirstOrDefault(c => c.id == "IA-crack-well"); if (cap) cap.Pound(Player);
            yield return TpSettle(new Vector3(18f, -3.3f, 478f), 0);
            Log($"  in the well at {Fmt(Player.transform.position)}");
            yield return Go(XZ(18.7f, 478), .3f, 6f, .5f); Log($"   -> {Fmt(Player.transform.position)}");
            foreach (var x in new[] { 19.7f, 20.5f, 21.3f, 22.4f, 25f }) { yield return JumpTo(XZ(x, 478), () => true, .6f); Log($"   jump to {x}: at {Fmt(Player.transform.position)}"); }
        }

        // ---------------------------------------------------------------- item 8: frame rate in the valley
        IEnumerator PerfIceAge()
        {
            yield return EnsurePlaying(false);
            Head($"Item 8: frame rate in the Ice Age, the full view drawn every frame ({(TestRun.Headless ? $"{TestRun.View.width} x {TestRun.View.height} off-screen" : "window")}, Steam Deck setting {(SteamDeck.On ? "on" : "off")})");
            yield return SwitchTo(0);
            float worstAvg = 9999; string where = "";
            foreach (var (name, from, path) in new[]
            {
                ("the snowfield toward the volcanoes", XZ(0, 20), new[] { XZ(0, 60), XZ(14, 90), XZ(-10, 110), XZ(-4, 40) }),
                ("the mammoth ridge", XZ(28, 140), new[] { XZ(36, 180), XZ(24, 205), XZ(30, 150) }),
                ("the standing stones and the vents", XZ(10, 455), new[] { XZ(-8, 470), XZ(14, 470), XZ(30, 440), XZ(0, 456) }),
            })
            {
                yield return TpSettle(IceAge.OnGround(from.x, from.z) + Vector3.up * .1f, 0);
                Application.targetFrameRate = -1;
                int frames = 0, wp = 0; float t0 = Time.realtimeSinceStartup, worstFrame = 0;
                while (Time.realtimeSinceStartup - t0 < 8f)
                {
                    var d = path[wp] - Player.transform.position; d.y = 0; if (d.magnitude < 2f) wp = (wp + 1) % path.Length;
                    Stick(StickFor(d.normalized)); frames++; worstFrame = Mathf.Max(worstFrame, Time.unscaledDeltaTime);
                    if (TestRun.Headless) { yield return new WaitForEndOfFrame(); CameraRig.I.Cam.Render(); }   // batch mode draws only on request: draw every frame, as a window would
                    yield return null;
                }
                Stick(Vector2.zero);
                float fps = frames / (Time.realtimeSinceStartup - t0);
                Application.targetFrameRate = 144;
                Log($"  {name}: {fps:0} fps average, slowest frame {worstFrame * 1000:0} ms");
                if (fps < worstAvg) { worstAvg = fps; where = name; }
                yield return Shot("40_perf_" + name.Split(' ')[1]);
            }
            float need = SteamDeck.On ? 40f : 60f;
            Ok(worstAvg > need, $"{worstAvg:0} fps at the slowest place ({where}); need {need:0} ({SystemInfo.graphicsDeviceName}, {Screen.width}x{Screen.height})");
        }
    }
}
