using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace ShardsOfTime
{
    // Round 6, items 5-14: Egypt as a player plays it, with the virtual pad from the vortex: the arrival, the town and the guards, the
    // Violinist (Abi's double jump) and the obelisk, the cat temple (Kira), the quarry and the quicksand, the camel across the dunes, the
    // Sphinx gag and climb, the scaffolding, the tomb (traps, mummy Kira past the mummies, Dan's counterweight), the necklace, the red
    // story shard, the vortex; then the Ice Age again for a double-jump ledge there. Mode "egypt" starts at the vortex (an Ice Age
    // save set up with the blue shard); "full" = the Ice Age play-through first (item 14).
    public sealed partial class ExeCheck
    {
        int ShardsEG => SaveGame.Current.World(WorldData.Egypt).shards.Count;
        Vector3 EG(float x, float z) => Egypt.OnGround(x, z);
        Vector3 T(float x, float z) => new Vector3(Egypt.Tomb.x + x, Egypt.Tomb.y, z);   // a spot in the tomb (x from its middle)

        IEnumerator EgyptOnly()
        {
            yield return EnsurePlaying(false);
            // the Ice Age done as far as its story shard (the vortex gate open), then into the vortex
            var ia = SaveGame.Current.World(WorldData.IceAge); ia.storyShard = true; ia.necklaceAwake = true;
            if (!SaveGame.Current.portals.Contains("IA-vortex")) SaveGame.Current.portals.Add("IA-vortex");
            SaveGame.Current.checkpoint = Vortex.Checkpoint; Checkpoint.RespawnParty(); CameraRig.I.SnapBehind();
            yield return Wait(.6f);
            yield return EgyptRoute(true);
        }

        IEnumerator Full()
        {
            yield return IceAgePlay();
            yield return EgyptRoute(false);
            yield return PerfEgypt();
        }

        Vector3 VPath(float yaw, float d) => Vortex.Centre + Quaternion.Euler(0, yaw, 0) * new Vector3(0, 0, d);

        IEnumerator EgyptRoute(bool inVortex)
        {
            Cutscenes.Started -= OnPlayed; Cutscenes.Started += OnPlayed;
            float t0 = Time.time;
            Head("Round 6, item 6: the vortex's Egypt path");
            if (!inVortex)
            {
                yield return GoF(XZ(0, 499), .5f); yield return Go(XZ(0, 507), .5f, 8f, 1f, () => Vortex.Inside(Player.transform.position));
                yield return WaitFor(() => Vortex.Inside(Player.transform.position), 3f);
            }
            yield return Go(VPath(0, 4), .8f); yield return Go(VPath(-18, 8), .6f); yield return Go(VPath(-18, 19), .6f);
            yield return Go(VPath(-18, 24), .5f, 6f, 1f, () => Game.I.Busy);
            yield return WaitFor(() => Cutscenes.Playing == "EgyptArrival", 8f);
            Ok(Game.InEgypt && Cutscenes.Playing == "EgyptArrival", $"the Egypt path loads Egypt and plays its arrival (world {Game.World.worldId}, cutscene {Cutscenes.Playing ?? "none"})");
            foreach (var (t, n) in new[] { (1.5f, "80a_arrival_land"), (3.8f, "80b_arrival_pyramids"), (6.0f, "80c_arrival_clothes"), (8.0f, "80d_arrival_mummy_kira"), (10.2f, "80e_arrival_react"), (12.6f, "80f_arrival_red_shard") })
            {
                yield return WaitFor(() => Cutscenes.Director && Cutscenes.Director.time >= t || Cutscenes.Playing == null, 16f);
                yield return Shot(n);
            }
            yield return WaitFor(() => !Game.InCutscene && !Game.I.Busy, 15f);
            yield return Wait(.5f);
            var kira = P.Heroes[2];
            Ok(P.Heroes[0].Outfit == Outfits.Egypt && P.Heroes[1].Outfit == Outfits.Egypt && kira.Form && kira.Form.id == KiraForm.Mummy,
               $"in Egyptian clothes (Dan {(P.Heroes[0].Model ? P.Heroes[0].Model.name : "graybox")}, Abi {(P.Heroes[1].Model ? P.Heroes[1].Model.name : "graybox")}); Kira is {(kira.Form ? kira.Form.displayName : "-")} ({(kira.Model ? kira.Model.name : "graybox")})");
            var gem = P.Heroes[0].NecklaceGem; var gc = gem && gem.GetComponent<Renderer>() ? gem.GetComponent<Renderer>().sharedMaterial.color : Color.black;
            Ok(NecklaceModel.Tint.r > .8f && NecklaceModel.Tint.b < .3f && gc.r > gc.b, $"the necklace's shard is red here (tint {NecklaceModel.Tint}, the gem now {gc})");
            MusicCheck("Egypt", "egypt");
            yield return Shot("81_plaza_pyramids");
            Log("  " + Game.World.RequirementReport("E").detail);
            Ok(Game.World.RequirementReport("E").ok && Game.World.shards.Count == 60 && Game.World.areas[0].requirement == 35, $"rule 2: {Game.World.RequirementReport("E").reachable} of 60 reachable on the first visit, 35 needed");

            yield return TownAndMarket();
            yield return ViolinistEgypt();
            yield return CatTemple();
            yield return QuarryRoad();
            yield return CamelAndSphinx();
            yield return ScaffoldClimb();
            yield return TombRun();
            Log($"  Egypt took the bot {(Time.time - t0) / 60f:0.0} min; {ShardsEG} shards ({string.Join(" ", SaveGame.Current.World(WorldData.Egypt).shards)}); hits {Health.HitsTaken}, respawns {Health.Respawns}");
            yield return DoubleJumpInIceAge();
            Log("  " + TestRun.Report());
        }

        void OnPlayed(string n) => played.Add(n);

        IEnumerator CamelOnly()
        {
            Cutscenes.Started -= OnPlayed; Cutscenes.Started += OnPlayed;
            yield return EnsurePlaying(false);
            var eg = SaveGame.Current.World(WorldData.Egypt); if (!eg.opened.Contains("EG-arrived")) eg.opened.Add("EG-arrived");
            SaveGame.Current.checkpoint = "EG-gate"; yield return StartWorld(WorldData.Egypt); yield return WaitFor(() => !Game.I.Busy, 10f);
            yield return TpSettle(Egypt.OnGround(30, 140) + Vector3.up * .1f, 180);
            yield return CamelAndSphinx();
        }

        // the tomb alone (quick iteration): Egypt loaded, the party set down at the doorway on the pyramid's top
        IEnumerator TombOnly()
        {
            Cutscenes.Started -= OnPlayed; Cutscenes.Started += OnPlayed;
            yield return EnsurePlaying(false);
            var eg = SaveGame.Current.World(WorldData.Egypt); if (!eg.opened.Contains("EG-arrived")) eg.opened.Add("EG-arrived");
            SaveGame.Current.checkpoint = Egypt.TopCp; yield return StartWorld(WorldData.Egypt); yield return WaitFor(() => !Game.I.Busy, 10f);
            yield return TombRun();
        }

        // quick looks from above at places that need checking (teleports only; screenshots)
        IEnumerator EgyptDiag()
        {
            yield return EnsurePlaying(false);
            var eg = SaveGame.Current.World(WorldData.Egypt); if (!eg.opened.Contains("EG-arrived")) eg.opened.Add("EG-arrived");
            SaveGame.Current.checkpoint = Egypt.Start; yield return StartWorld(WorldData.Egypt); yield return WaitFor(() => !Game.I.Busy, 10f);
            var c = Egypt.TheCamel.transform.position; var (mb, ms) = Egypt.Mount(0);
            Log($"  camel at {Fmt(c)} facing {Egypt.TheCamel.transform.eulerAngles.y:0}; mount block {Fmt(mb)}, step {Fmt(ms)}");
            yield return CloseUpAt(c + new Vector3(0, 22, -.1f), c, "d1_camel_from_above");
            yield return CloseUpAt(c + (ms - c).normalized * 9f + Vector3.up * 4f, c + Vector3.up, "d2_camel_mount_side");
            yield return CloseUpAt(new Vector3(-12, 12, 215), new Vector3(-30, 1, 215), "d3_plinth_ramp");
            yield return CloseUpAt(new Vector3(500, 72, 52), new Vector3(500, 59, 52.1f), "d4_sand_hall");
            yield return CloseUpAt(new Vector3(-40, 25, 205), new Vector3(-60, 8, 215), "d5_sphinx_blocks");
        }

        // a close look from a given spot (its own camera, off-screen)
        IEnumerator CloseUpAt(Vector3 from, Vector3 at, string name)
        {
            yield return new WaitForEndOfFrame();
            var cam = CameraRig.I.Cam; var pos = cam.transform.position; var rot = cam.transform.rotation; var brain = cam.GetComponent<Unity.Cinemachine.CinemachineBrain>();
            if (brain) brain.enabled = false;
            cam.transform.position = from; cam.transform.LookAt(at);
            TestRun.SaveView(cam, System.IO.Path.Combine(dir, name + ".png")); Log($"  screenshot {name}.png");
            cam.transform.SetPositionAndRotation(pos, rot); if (brain) brain.enabled = true;
        }

        IEnumerator EShard(string id, Vector3 at, float tol = .5f)
        {
            yield return GoF(at, tol, 30f);
            yield return WaitFor(() => Has(id), 1.5f);
            Ok(Has(id), $"{id} ({Game.World.shards.First(s => s.id == id).note}) at {Fmt(Player.transform.position)}; {ShardsEG} shards");
            yield return NecklaceIfAwake();
        }

        // the 35th shard wakes the necklace (once out under the sky): wait out its cutscene, as a player does
        bool necklaceSeen;
        IEnumerator NecklaceIfAwake()
        {
            if (necklaceSeen || ShardsEG < 35) yield break;
            yield return WaitFor(() => played.Contains("NecklaceWakesEgypt"), ShardsEG == 35 ? 8f : .1f);
            if (!played.Contains("NecklaceWakesEgypt")) yield break;
            necklaceSeen = true;
            Ok(SaveGame.Current.World(WorldData.Egypt).necklaceAwake, $"the 35th shard wakes the necklace (NecklaceWakesEgypt, red) at {Fmt(Player.transform.position)}");
            yield return Wait(2f); yield return Shot("105_necklace_wakes");
            yield return WaitFor(() => !Game.I.Busy && !Game.InCutscene, 15f);
        }

        // ---- the town: the main street, the market, the rooftops; a guard sends you back
        // up an outside stair (0.78 m steps: a jump each), from the bottom step `from` to the top step `top`
        IEnumerator Stair(Vector3 from, Vector3 top, int steps = 6)
        {
            yield return Go(from, .4f, 12f);
            for (int i = 1; i <= steps; i++)
            {
                var p = Vector3.Lerp(from, top, i / (float)steps);
                for (int k = 0; k < 3 && Player.transform.position.y < i * .78f - .2f; k++) yield return JumpTo(p, () => true, .35f, .3f);
            }
        }

        // wait until no guard could see the spot (far, or walking away), then go
        IEnumerator PastGuards(Vector3 at, float timeout = 30f)
        {
            yield return WaitFor(() => Guard.All.All(g => Vector3.Distance(g.transform.position, at) > 14f || Vector3.Dot(g.transform.forward, (at - g.transform.position).normalized) < -.2f), timeout);
        }

        IEnumerator TownAndMarket()
        {
            Head("Round 6, item 7: the town and the market");
            yield return EShard("E01", EG(0, 18)); yield return EShard("E02", EG(0, 32)); yield return EShard("E03", EG(-4, 44));
            // the west roofs: up the stair by the north-west house, the plank across to the south one
            yield return Stair(EG(-23.1f, 50.6f), EG(-23.1f, 44.5f));
            yield return Go(XZ(-25.5f, 42f), .5f, 4f);
            Ok(Player.transform.position.y > 4f, $"up the outside stair onto the west roofs (y {Player.transform.position.y:0.0})");
            yield return Go(XZ(-27, 40), .5f, 6f); yield return Go(XZ(-27, 33), .5f, 6f); yield return Go(XZ(-27, 26), .5f, 8f);
            yield return WaitFor(() => Has("E10"), 1f);
            Ok(Has("E10") && Has("E11"), $"E10, E11 on the west rooftops ({ShardsEG} shards)");
            yield return TurnView(XZ(-52, 33)); yield return Shot("82_rooftops_far_roof");
            yield return Go(XZ(-29.5f, 33), .5f, 4f); yield return Hold(GamepadButton.South, .4f); yield return Wait(1f);
            Ok(!Has("E14"), "the far roof stays out of reach (Kira's pounce, later)");
            yield return WaitFor(() => M.Grounded, 4f);
            yield return Go(EG(-20, 40), .6f, 10f);
            yield return EShard("E06", EG(-20, 58));
            yield return Go(EG(0, 60), .8f, 10f); yield return TurnView(XZ(0, 120)); yield return Shot("83_market");
            // the guard: step into the east alley in front of its guard; he sends the party back to the last checkpoint
            var g = Guard.All.FirstOrDefault(x => x.name == "Guard east");
            if (g)
            {
                int spotted = g.Spotted; var cp = SaveGame.Current.checkpoint;
                float until = Time.time + 20f;
                while (g.Spotted == spotted && Time.time < until)
                {
                    var front = g.transform.position + g.transform.forward * 4f;
                    yield return Go(new Vector3(front.x, 0, front.z), .8f, 1f, 1f, () => g.Spotted > spotted);
                }
                Ok(g.Spotted > spotted, $"a guard spots the player and sends the party back to checkpoint {cp}: \"{Hud.LastMessage}\"");
                yield return Wait(.4f); yield return Shot("84_guard_halt");
                yield return WaitFor(() => !Game.I.Busy, 3f);
            }
            // the shards behind the stalls: slip past when the alley's guard is far or walking away
            yield return Go(EG(-16, 82), .6f, 25f); yield return PastGuards(EG(-26, 82)); yield return EShard("E08", EG(-26, 82)); yield return Go(EG(-16, 82), .6f, 6f);
            yield return EShard("E07", EG(20, 80));
            yield return Go(EG(16, 54), .6f, 15f); yield return PastGuards(EG(26, 54)); yield return EShard("E09", EG(26, 54)); yield return Go(EG(16, 56), .6f, 6f);
            Log($"  guards: {string.Join(", ", Guard.All.Select(x => $"{x.name} spotted the player {x.Spotted} time(s)"))}");
            yield return EShard("E04", EG(4, 96));
            // the east roof: the outside stair between the north houses
            yield return Stair(EG(20.6f, 105.6f), EG(20.6f, 99.5f));
            yield return Go(XZ(17, 100), .5f, 4f); yield return WaitFor(() => Has("E12"), 1f);
            Ok(Has("E12"), $"E12 on the east roof (up the outside stair, y {Player.transform.position.y:0.0})");
            yield return Go(XZ(14.6f, 100), .5f, 4f); yield return WaitFor(() => M.Grounded && Player.transform.position.y < 1f, 4f);
            yield return Stair(EG(-20.6f, 97.4f), EG(-20.6f, 103.5f));
            yield return Go(XZ(-17, 104), .5f, 4f); yield return WaitFor(() => Has("E13"), 1f);
            Ok(Has("E13"), $"E13 on the north-west roof (y {Player.transform.position.y:0.0})");
            yield return Go(XZ(-14.6f, 104), .5f, 4f); yield return WaitFor(() => M.Grounded && Player.transform.position.y < 1f, 4f);
            yield return EShard("E05", EG(0, 114));
            yield return Go(EG(10, 90), .6f, 10f);
        }

        // ---- item 9: the Violinist plays his lyre for Abi: the double jump; the obelisk's ledge
        IEnumerator ViolinistEgypt()
        {
            Head("Round 6, items 4 and 9: the Violinist's lyre, Abi's double jump");
            var v = Violinist.I;
            Ok(v && v.instrument == "Lyre" && v.Instrument && v.Instrument.name == "Lyre", $"the Violinist in the market with his lyre (model {(v ? v.ModelName : "-")}, instrument {(v && v.Instrument ? v.Instrument.name : "none")})");
            yield return SwitchTo(0);
            yield return Go(Egypt.ViolinistAt + new Vector3(3.5f, 0, 3.5f), .6f, 25f); yield return Wait(1f);
            yield return Go(Egypt.ViolinistAt + new Vector3(1.6f, 0, 1.6f), .5f, 6f, .5f); yield return Wait(.6f);
            Ok(!Game.InCutscene && Hud.LastMessage.Contains("looking past you"), $"for Dan he only smiles: \"{Hud.LastMessage}\"");
            yield return CloseUp(v.transform, "85_lyre_idle");
            yield return Go(Egypt.ViolinistAt + new Vector3(4f, 0, 4f), .6f, 6f);
            yield return SwitchTo(1);
            yield return Go(Egypt.ViolinistAt + new Vector3(1.5f, 0, 1.5f), .5f, 15f, .6f, () => Cutscenes.Playing != null);
            yield return WaitFor(() => Cutscenes.Playing == "ViolinistTeachesEgypt", 10f);
            yield return Wait(1.2f);
            Ok(v.ClipNow == "play" && v.InstrumentPose == "play", $"he plays the lyre for Abi (clip {v.ClipNow}, pose {v.InstrumentPose})");
            yield return Shot("86_lyre_plays"); yield return CloseUp(v.transform, "86b_lyre_closeup");
            yield return WaitFor(() => !Game.I.Busy, 15f);
            Ok(SaveGame.Current.Has(Abilities.DoubleJump) && Player.Motor.CanDoubleJump, $"Abi learned the double jump: \"{Hud.LastMessage}\"");
            // the obelisk: up the crates onto the pedestal, double jump to the golden ledge
            yield return Go(EG(4.6f, 67f), .5f, 15f);
            yield return JumpTo(XZ(3.1f, 67f), () => true, .4f, .35f); yield return JumpTo(XZ(3.3f, 68.3f), () => true, .4f, .35f);
            yield return JumpTo(XZ(1.2f, 66.4f), () => true, .4f, .4f);
            for (int k = 0; k < 3 && !Has("E16") && Player.transform.position.y > 2f; k++)   // up to three tries, as a player would
            {
                yield return Go(XZ(0, 65.4f), .3f, 3f, .4f);
                yield return JumpTo(XZ(0, 64f), () => true, .25f, .35f, doubleJump: true); yield return WaitFor(() => Has("E16"), 1f);
                if (!Has("E16") && Player.transform.position.y < 2f) { yield return JumpTo(XZ(3.1f, 67f), () => true, .4f, .35f); yield return JumpTo(XZ(3.3f, 68.3f), () => true, .4f, .35f); yield return JumpTo(XZ(1.2f, 66.4f), () => true, .4f, .4f); }
            }
            Ok(Has("E16"), $"E16 on the obelisk's ledge with Abi's double jump (y {Player.transform.position.y:0.0})");
            yield return Shot("87_obelisk_ledge");
            yield return Go(XZ(0, 61), .5f, 5f); yield return WaitFor(() => M.Grounded && Player.transform.position.y < 1f, 5f);
        }

        // ---- item 8: the cat temple opens for Kira
        IEnumerator CatTemple()
        {
            Head("Round 6, item 8: the cat temple (its door opens only for Kira)");
            var plate = CatPlate.All.First(c => c.id == "EG-cat-temple");
            yield return SwitchTo(0);
            yield return Go(EG(-18, 72), .6f, 20f); yield return PastGuards(EG(-31, 72)); yield return Go(EG(-37, 72), .6f, 8f);   // across the guard's alley behind his back
            yield return Go(EG(-40, 72), .4f, 5f, .5f); yield return Wait(1.2f);
            Ok(!plate.Open && Hud.LastMessage.Contains("cat"), $"Dan on the plate: nothing (\"{Hud.LastMessage}\")");
            yield return Go(EG(-36, 72), .6f, 5f);
            yield return SwitchTo(2);
            yield return Go(EG(-40, 72), .3f, 8f, .5f); yield return Wait(1.2f);
            Ok(plate.Open, $"mummy Kira on the plate: the temple door sinks (\"{Hud.LastMessage}\")");
            yield return Wait(2.5f); yield return Shot("88_cat_temple_open");
            yield return Go(EG(-47, 72), .5f, 6f);
            yield return EShard("E17", EG(-54, 66)); yield return EShard("E18", EG(-54, 78));
            yield return Go(EG(-47, 72), .5f, 8f); yield return Go(EG(-37, 72), .6f, 10f);
            yield return PastGuards(EG(-31, 72)); yield return Go(EG(-18, 72), .6f, 10f);
            yield return SwitchTo(0);
        }

        IEnumerator QuarryRoad()
        {
            Head("Round 6, item 7: the quarry road (stones on rollers, quicksand, the huge block)");
            yield return EShard("E19", EG(60, 60)); yield return EShard("E20", EG(64, 90)); yield return EShard("E21", EG(58, 120));
            yield return Go(EG(86, 54), .6f, 15f); yield return Go(EG(86, 59.4f), .4f, 4f, .5f); Stick(StickFor(Vector3.forward)); yield return Wait(.6f); Stick(Vector2.zero);
            Ok(Hud.LastMessage.Contains("Muscle Man") && !Has("E24"), "the quarry's huge block needs Muscle Man (visible, closed)");
            // the quicksand: ride the shuttle stone across
            yield return Go(EG(60, 132.6f), .5f, 25f);
            var shuttle = GameObject.Find("Shuttle stone")?.transform;
            yield return WaitFor(() => shuttle && Mathf.Abs(shuttle.position.z - 134.5f) < .6f, 16f);
            yield return JumpTo(new Vector3(60, 0, 135.4f), () => true, .5f, .35f);
            int sunk = Quicksand.All.Sum(q => q.Sunk);
            float until = Time.time + 16f;
            while (Time.time < until && shuttle && shuttle.position.z < 165f) { var d = shuttle.position - Player.transform.position; d.y = 0; Stick(d.magnitude > .5f ? StickFor(d.normalized) * .35f : Vector2.zero); yield return null; }
            Stick(Vector2.zero);
            yield return Shot("89_quicksand_ride");
            yield return JumpTo(XZ(60, 168.5f), () => true, .6f, .35f);
            Ok(Player.transform.position.z > 166f && Quicksand.All.Sum(q => q.Sunk) == sunk, $"across the quicksand on the shuttle stone (z {Player.transform.position.z:0.0})");
            yield return EShard("E22", EG(60, 172)); yield return EShard("E23", EG(74, 176));
        }

        IEnumerator CamelAndSphinx()
        {
            Head("Round 6, item 7: the judgmental camel, the ride across the dunes, the Sphinx");
            var camel = Egypt.TheCamel;
            yield return EShard("E29", EG(40, 192));
            yield return Go(EG(30, 140), .8f, 20f);
            yield return WaitFor(() => camel && !camel.Moving && Vector3.Distance(camel.transform.position, EG(Egypt.CamelStop.x, Egypt.CamelStop.z)) < 2f, 60f);
            int snorts = camel.Snorts;
            var (mb, ms) = Egypt.Mount(0);
            Log($"    (camel at {Fmt(camel.transform.position)}, mount step {Fmt(ms)}, block {Fmt(mb)}, player {Fmt(Player.transform.position)})");
            yield return Go(ms + (ms - mb).normalized * 1.6f, .5f, 15f); yield return WaitFor(() => camel.Snorts > snorts, 6f);
            Log($"    (at the mount: player {Fmt(Player.transform.position)}, hearts {Health.Hearts}, checkpoint {SaveGame.Current.checkpoint})");
            Ok(camel.Snorts > snorts, $"the camel stares at {Player.Stats.displayName} and snorts (\"{Hud.LastMessage}\")");
            yield return CloseUpAt(camel.transform.position + camel.transform.forward * 4.2f + camel.transform.right * 1.6f + Vector3.up * 2.3f, camel.transform.position + camel.transform.forward * 1.2f + Vector3.up * 2.0f, "90_camel_stare");
            // up: the step, the block, the saddle
            var steps = Egypt.MountSteps(0);
            for (int i = steps.Length - 1; i >= 0; i--) { var sp = steps[i].at; yield return JumpTo(sp, () => Flat(sp) < 1.9f, .4f, .35f); }
            yield return JumpTo(camel.transform.position, () => Flat(camel.transform.position) < 2.2f, .4f, .4f);
            Log($"    (after mounting: player {Fmt(Player.transform.position)}, riding {camel.Riding(Player)})");
            yield return WaitFor(() => camel.Riding(Player), 2f);
            Ok(camel.Riding(Player), $"on the camel's saddle (y {Player.transform.position.y:0.0})");
            yield return WaitFor(() => camel.Moving, 6f); yield return Wait(4f); yield return Shot("91_camel_ride");
            float ride = Time.time; bool stayed = true;
            float nextLog = 0;
            while (Time.time < ride + 40f && camel.Moving)
            {
                if (camel.Seated == 0) stayed = false; Stick(Vector2.zero);   // a passenger: stick still
                if (Time.time > nextLog) { nextLog = Time.time + 2f; Log($"      (riding: camel {Fmt(camel.transform.position)} seated {camel.Seated}, player {Fmt(Player.transform.position)} ground {(M.GroundCollider ? M.GroundCollider.name : "none")})"); }
                yield return null;
            }
            Stick(Vector2.zero);
            var end = Egypt.CamelRoute[Egypt.CamelRoute.Length - 1];
            Ok(stayed && Flat(end) < 4f && camel.Trips > 0 && P.Heroes.All(h => Vector3.Distance(h.transform.position, Player.transform.position) < 3f), $"the camel carried the whole party across the dunes to the Sphinx in {Time.time - ride:0} s (at {Fmt(Player.transform.position)}; {Followers()})");
            yield return Shot("92_sphinx_from_the_camel");
            var down = Egypt.MountSteps(1);
            foreach (var st in down) { var sp = st.at; yield return JumpTo(sp, () => true, .4f, .3f); }
            yield return Go(down[2].at + (down[2].at - down[0].at).normalized * 2f, .6f, 6f);
            yield return EShard("E28", EG(-14, 178)); yield return EShard("E27", EG(2, 164)); yield return EShard("E26", EG(20, 146)); yield return EShard("E30", EG(-12, 140));
            // the gag: walk up to the Sphinx's front
            yield return GoF(EG(-20, 212), .8f);
            yield return Go(EG(-25, 215), .5f, 8f, 1f, () => played.Contains("SphinxGag"));
            yield return WaitFor(() => played.Contains("SphinxGag"), 5f);
            Ok(played.Contains("SphinxGag"), "reaching the Sphinx plays SphinxGag (the first time)");
            foreach (var (t, n) in new[] { (2.4f, "93a_gag_walk_up"), (4.6f, "93b_gag_reveal"), (5.6f, "93c_gag_its_face"), (6.4f, "93d_gag_double_take"), (8.1f, "93e_gag_same_pose"), (9.6f, "93f_gag_abi") })
            {
                yield return WaitFor(() => Cutscenes.Director && Cutscenes.Director.time >= t || Cutscenes.Playing == null, 12f);
                yield return Shot(n);
            }
            yield return WaitFor(() => !Game.I.Busy, 10f);
            // the plinth, the paws, the climb up the restoration blocks to the back, the scaffold to the head
            yield return Go(EG(-19, 215), .6f, 8f); yield return Go(XZ(-30, 215), .6f, 8f);   // up the plinth's front ramp
            yield return EShard("E36", XZ(-28.8f, 201));
            yield return Go(XZ(-29.4f, 208f), .4f, 8f); yield return JumpTo(XZ(-30.7f, 208f), () => true, .4f, .35f); yield return JumpTo(XZ(-34f, 208f), () => true, .4f, .4f); yield return Go(XZ(-35f, 208f), .3f, 3f, .5f);
            yield return WaitFor(() => Has("E31"), 1.5f);
            Ok(Has("E31"), $"E31 on the Sphinx's paw (y {Player.transform.position.y:0.0})");
            yield return Go(XZ(-34, 207), .5f, 4f); yield return Go(XZ(-30.5f, 205f), .5f, 5f);
            yield return Go(XZ(-31, 201), .6f, 6f); yield return Go(XZ(-80, 201.5f), .6f, 25f);
            for (int i = 0; i < 9; i++)
            {
                float cx = -76.8f + i * 3f; float top = 2f + 1.25f * (i + 1);
                for (int k = 0; k < 3 && Player.transform.position.y < top - .2f; k++) yield return JumpTo(XZ(cx, 205.1f), () => true, .45f, .35f);
                if (Player.transform.position.y < top - .2f) { Fail($"Sphinx block {i + 1}: stuck at {Fmt(Player.transform.position)}"); break; }
            }
            yield return JumpTo(XZ(-56, 210), () => true, .5f, .35f);
            yield return Go(XZ(-62, 215), .5f, 6f); yield return Go(XZ(-72, 215), .5f, 8f);
            Ok(Has("E33") && Has("E34"), $"E33, E34 on the Sphinx's back (y {Player.transform.position.y:0.0})");
            yield return TurnView(XZ(60, 230)); yield return Shot("94_from_the_sphinx");
            yield return Go(XZ(-57.5f, 222f), .5f, 8f);
            for (int i = 0; i < 4; i++) yield return JumpTo(XZ(-56.2f + i * 1.6f, 224f), () => true, .4f, .35f);
            yield return JumpTo(XZ(-50.5f, 222.5f), () => true, .4f, .35f); yield return JumpTo(XZ(-47.5f, 221.5f), () => true, .4f, .35f);
            yield return JumpTo(XZ(-46f, 220.6f), () => true, .4f, .35f); yield return JumpTo(XZ(-46f, 217f), () => true, .4f, .35f); yield return Go(XZ(-46f, 215f), .3f, 3f, .5f);
            yield return WaitFor(() => Has("E35"), 1.5f);
            Ok(Has("E35"), $"E35 on the Sphinx's head (y {Player.transform.position.y:0.0})");
            yield return Shot("95_on_the_sphinx_head");
            yield return Go(XZ(-36, 215), .6f, 8f); yield return WaitFor(() => M.Grounded && Player.transform.position.y < 5f, 8f);
            yield return Go(XZ(-30, 214), .6f, 6f); yield return Go(EG(-22, 200), .6f, 8f);
        }


        IEnumerator ScaffoldClimb()
        {
            Head("Round 6, item 7: the pyramid under construction (the scaffolding up its south face)");
            yield return GoF(EG(84, 190), .8f, 40f);
            yield return Shot("96_scaffold");
            // ramps zigzag: level 1 from the ground at the east end; odd levels' ramps go up westward, even levels' eastward
            for (int lv = 1; lv <= 9; lv++)
            {
                bool west = lv % 2 == 0; float y = lv * 4f;
                float lowX = west ? 57.6f : 82.4f, highX = west ? 68.4f : 71.6f;
                yield return Go(XZ(lowX + (west ? -.6f : .6f), 197f), .6f, 15f);
                yield return Go(XZ(lowX, 194.4f), .5f, 5f);
                yield return Go(XZ(highX, 194.4f), .5f, 8f);
                yield return Go(XZ(highX, 197.5f), .5f, 4f);
                if (Player.transform.position.y < y - .5f) { Fail($"scaffold level {lv}: stuck at {Fmt(Player.transform.position)}"); break; }
                if (lv == 2) yield return EShard("E37", XZ(60, 200)); if (lv == 4) yield return EShard("E38", XZ(80, 200));
                if (lv == 6) yield return EShard("E39", XZ(60, 200)); if (lv == 8) yield return EShard("E40", XZ(80, 200));
            }
            Ok(Player.transform.position.y > 35f, $"up the scaffolding to level 9 (y {Player.transform.position.y:0.0})");
            yield return Go(XZ(70, 205.5f), .5f, 8f); yield return Go(XZ(70, 228f), .5f, 10f);
            yield return EShard("E41", XZ(74, 236));
            yield return TurnView(XZ(-55, 215)); yield return Shot("97_pyramid_top_view");
        }

        IEnumerator TombRun()
        {
            Head("Round 6, item 7: the tomb (inside the pyramid)");
            yield return Go(XZ(70, 236), .5f, 6f); yield return Go(XZ(70, 239f), .4f, 6f, .6f, () => Player.transform.position.x > 300f);
            yield return WaitFor(() => Player.transform.position.x > 300f, 3f);
            yield return Wait(1.4f);
            Ok(Player.transform.position.x > 300f && ShardsOfTime.Look.Dark, $"through the doorway into the tomb: dark, the necklace lights it ({Fmt(Player.transform.position)})");
            yield return Shot("98_tomb_entry");
            int hits = Health.HitsTaken;
            yield return EShard("E50", T(0, 15));
            yield return TombForward();
            Log($"  blades, darts, sand and blocks: {Health.HitsTaken - hits} hit(s) on the way; {ShardsEG} shards ({string.Join(" ", new[] { "E50", "E51", "E52", "E53" }.Where(Has))})");
            Ok(Player.transform.position.z > 86f && Player.transform.position.y > Egypt.Tomb.y - .5f, $"through the blade corridor, the sand-pit hall and the sliding blocks to the turning room ({Fmt(Player.transform.position)})");
            yield return Shot("100_turning_room");
            // east: the mummies' gallery; mummy Kira walks past quietly and opens the cat door
            yield return Bridge("E");
            Ok(Player.transform.position.x > Egypt.Tomb.x + 7.5f && Player.transform.position.y > Egypt.Tomb.y - .5f, $"across the turning bridge to the east ledge ({Fmt(Player.transform.position)}); E54 {(Has("E54") ? "got" : "missed")}");
            yield return Go(T(14, 96), .5f, 5f);
            var mummies = Enemy.All.Where(e => e.kind == Enemy.Kind.Mummy && e.transform.position.x > 450).ToList();
            yield return SwitchTo(2);
            int wakes = mummies.Sum(m => m.Wakes);
            yield return Go(T(22, 96), .4f, 8f); yield return Wait(.3f); yield return Shot("101_kira_past_the_mummies");
            yield return Go(T(34, 96), .4f, 8f); yield return Go(T(41.5f, 96), .4f, 8f);
            Ok(mummies.Sum(m => m.Wakes) == wakes, $"mummy Kira walks the gallery and no mummy wakes ({mummies.Count} asleep)");
            Ok(Has("E55") && Has("E56"), "E55, E56 in the gallery");
            yield return Go(T(42.2f, 96), .3f, 4f, .5f); yield return Wait(1.2f);
            Ok(CatPlate.All.First(c => c.id == "EG-cat-tomb").Open, "the hidden chamber's cat door opens for Kira");
            yield return Wait(2.5f); yield return Go(T(48, 94), .5f, 6f); yield return EShard("E57", T(47, 92)); yield return Go(T(49, 100), .5f, 5f);
            Ok(SaveGame.Current.heartUpgrades.Contains("EG-H1"), $"the hidden chamber's heart (max hearts {Health.Max})");
            yield return Go(T(40, 96), .5f, 8f);
            yield return SwitchTo(0);
            yield return Go(T(30, 93.5f), .5f, 6f); yield return WaitFor(() => mummies.Any(m => m.Awake), 3f);
            Ok(mummies.Any(m => m.Awake), $"as Dan, a mummy wakes ({mummies.Count(m => m.Awake)} awake)");
            yield return Shot("102_mummy_wakes");
            yield return Fight(8f, 20f);
            yield return Go(T(10, 96), .5f, 15f);
            // west: Dan pushes the weight onto the plate, the lift rises to the high ledge
            yield return Bridge("W");
            var cw = Object.FindObjectsByType<Counterweight>(FindObjectsSortMode.None).First();
            var wt = cw.weight;
            yield return Fight(7f, 12f);   // mummies that followed Dan in
            yield return Go(T(-26f, 96), .4f, 10f); yield return Go(new Vector3(wt.position.x + 2.2f, 0, 96f), .2f, 6f, .5f);   // lined up behind it
            Log($"    (before the push: player {Fmt(Player.transform.position)}, weight {Fmt(wt.position)})");
            float push = Time.time;
            for (int i = 0; i < 20 && !cw.Up; i++) { var d = wt.position + Vector3.right * .2f - Player.transform.position; d.y = 0; Stick(StickFor(new Vector3(-1f, 0, Mathf.Clamp(-d.z, -.3f, .3f)).normalized)); yield return WaitFor(() => cw.Up, 1f); }
            Stick(Vector2.zero);
            var pbk = wt.GetComponent<PushBlock>(); Log($"    (after the push: weight {Fmt(wt.position)}, pushed {(pbk ? pbk.PushedDistance : 0):0.0} m, last refusal: {(pbk ? pbk.Refused : "-")})");
            Ok(cw.Up, $"Dan pushes the weight onto the golden plate ({Time.time - push:0.0} s): the lift goes up");
            yield return Go(T(-20, 96), .4f, 8f);
            yield return WaitFor(() => Player.transform.position.y > Egypt.Tomb.y + 5.5f, 8f);
            yield return JumpTo(T(-26f, 96f), () => true, .6f, .4f); yield return Go(T(-26f, 96f), .3f, 3f, .5f); yield return WaitFor(() => Has("E58"), 1.5f);
            Ok(Has("E58"), $"E58 on the high ledge from the lift (y {Player.transform.position.y:0.0})");
            yield return Shot("103_counterweight_lift");
            yield return Go(T(-14, 96), .5f, 6f); yield return WaitFor(() => M.Grounded && Player.transform.position.y < Egypt.Tomb.y + .5f, 6f);
            yield return Go(T(-10, 96), .5f, 6f);
            // north: the burial chamber
            yield return Bridge("N");
            yield return Go(T(0, 112), .5f, 6f);
            yield return Wait(.4f); yield return Shot("104_burial_chamber");
            yield return Go(T(10.8f, 122), .4f, 6f); yield return Press(GamepadButton.West); yield return Wait(.4f);
            Ok(!Has("E59") && Hud.LastMessage.Contains("punch"), $"the cracked wall holds (charged punch, later): \"{Hud.LastMessage}\"");
            yield return Go(T(-11, 118), .4f, 6f); yield return Go(T(-14, 118), .4f, 4f, .5f); yield return Hold(GamepadButton.South, .4f); yield return Wait(.8f);
            Ok(!Has("E60"), $"the narrow shaft's shard is out of reach (wall jump, later): \"{Hud.LastMessage}\"");
            yield return Go(T(-6, 116), .5f, 5f);
            int n = ShardsEG;
            Log($"  shards in Egypt: {n} ({string.Join(" ", SaveGame.Current.World(WorldData.Egypt).shards)})");
            Ok(n >= 35, $"{n} shards in Egypt (35 wake the necklace)");
            if (!SaveGame.Current.World(WorldData.Egypt).necklaceAwake)
            {
                // the necklace needs open sky: out through the tomb, and back in
                yield return Bridge("S"); yield return TombBack();
                yield return Go(T(0, .8f), .4f, 5f, .6f, () => Player.transform.position.x < 300f);
                yield return WaitFor(() => Player.transform.position.x < 300f, 3f);
                yield return Go(XZ(70, 226), .6f, 6f);
                yield return NecklaceIfAwake();
                yield return Go(XZ(70, 236), .5f, 6f); yield return Go(XZ(70, 239f), .4f, 6f, .6f, () => Player.transform.position.x > 300f);
                yield return WaitFor(() => Player.transform.position.x > 300f, 3f);
                yield return TombForward(); yield return Bridge("N"); yield return Go(T(0, 112), .5f, 6f);
            }
            Ok(SaveGame.Current.World(WorldData.Egypt).necklaceAwake && Game.I.StoryShard && Game.I.StoryShard.gameObject.activeSelf, "the necklace is awake and the red story shard waits in the burial chamber");
            yield return Go(T(0, 126.6f), .4f, 6f); yield return WaitFor(() => played.Contains("VortexOpensEgypt"), 6f);
            Ok(SaveGame.Current.World(WorldData.Egypt).storyShard && played.Contains("VortexOpensEgypt"), "the red story shard in the burial chamber opens the vortex (VortexOpensEgypt)");
            yield return Wait(2.5f); yield return Shot("106_vortex_opens_red");
            yield return WaitFor(() => !Game.I.Busy && !Game.InCutscene, 10f);
            Ok(SaveGame.Current.StoryShards >= 2, $"{SaveGame.Current.StoryShards} of 8 story shards");
            yield return Go(T(0, 131.5f), .5f, 5f); yield return Go(T(0, 135f), .4f, 5f, 1f, () => Vortex.Inside(Player.transform.position));
            yield return WaitFor(() => Vortex.Inside(Player.transform.position), 4f);
            Ok(Vortex.Inside(Player.transform.position), "the red vortex takes the party into the vortex space");
            yield return Wait(.6f); yield return Shot("107_vortex_after_egypt");
            yield return Go(VPath(0, 4), .8f); yield return Go(VPath(18, 8), .6f); yield return Go(VPath(18, 19), .6f); yield return Go(VPath(18, 24), .5f, 6f, 1f, () => Game.I.Busy);
            yield return WaitFor(() => Hud.TitleShown != null, 4f);
            Ok(Hud.TitleShown == "Bethlehem", $"after Egypt the Bethlehem path opens: \"{Hud.TitleShown}, to be continued...\" (saved)");
            yield return WaitFor(() => !Game.I.Busy, 8f);
        }

        // the entry -> the turning room's south ledge: the blades, the dart wall, the stepping stones, the sliding blocks
        IEnumerator TombForward()
        {
            var blades = Object.FindObjectsByType<SwingBlade>(FindObjectsSortMode.None).OrderBy(b => b.transform.position.z).ToList();
            foreach (var b in blades)
            {
                yield return Go(T(0, b.transform.position.z - 2.4f), .4f, 8f, .6f);
                yield return WaitFor(() => Mathf.Abs(Mathf.DeltaAngle(0, b.transform.localEulerAngles.z)) > 55f, 4f);
                yield return Go(T(0, b.transform.position.z + 2.4f), .4f, 3f);
            }
            yield return Go(T(0, 43), .4f, 8f);
            foreach (var (x, z) in Egypt.SandStones.Select(z => (0f, z)).Concat(new[] { (0f, 60.5f) }))
            {
                var nx = T(x, z);
                yield return JumpTo(nx, () => Flat(nx) < 2.6f, .45f, .35f);   // walk to the edge, then jump
            }
            yield return Fight(6f, 10f);
            yield return Go(T(0, 60.8f), .4f, 6f); yield return Go(T(0, 63), .4f, 6f);
            var blocks = GameObject.FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None).Where(m => m.name == "Sliding block").OrderBy(m => m.transform.position.z).ToList();
            foreach (var blk in blocks)
            {
                yield return Go(T(0, blk.transform.position.z - 2.6f), .4f, 6f, .6f);
                yield return WaitFor(() => Mathf.Abs(blk.transform.position.x - Egypt.Tomb.x) > 3.4f, 6f);
                yield return Go(T(0, blk.transform.position.z + 2.4f), .4f, 3f);
            }
            yield return Go(T(0, 87.2f), .4f, 6f);
        }

        // the turning room's south ledge -> the entry (the same traps, the other way)
        IEnumerator TombBack()
        {
            var blocks = GameObject.FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None).Where(m => m.name == "Sliding block").OrderByDescending(m => m.transform.position.z).ToList();
            foreach (var blk in blocks)
            {
                yield return Go(T(0, blk.transform.position.z + 2.6f), .4f, 6f, .6f);
                yield return WaitFor(() => Mathf.Abs(blk.transform.position.x - Egypt.Tomb.x) > 3.4f, 6f);
                yield return Go(T(0, blk.transform.position.z - 2.4f), .4f, 3f);
            }
            yield return Go(T(0, 61), .4f, 5f);
            foreach (var (x, z) in Egypt.SandStones.Reverse().Select(z => (0f, z)).Concat(new[] { (0f, 43f) })) { var nx = T(x, z); yield return JumpTo(nx, () => Flat(nx) < 2.6f, .45f, .35f); }
            var bl = Object.FindObjectsByType<SwingBlade>(FindObjectsSortMode.None).OrderByDescending(b => b.transform.position.z).ToList();
            foreach (var b in bl)
            {
                yield return Go(T(0, b.transform.position.z + 2.4f), .4f, 8f, .6f);
                yield return WaitFor(() => Mathf.Abs(Mathf.DeltaAngle(0, b.transform.localEulerAngles.z)) > 55f, 4f);
                yield return Go(T(0, b.transform.position.z - 2.4f), .4f, 3f);
            }
            yield return Go(T(0, 4), .5f, 8f);
        }

        // across the turning bridge: from where the player stands (a ledge or the middle) to a ledge ("N", "S", "E", "W")
        IEnumerator Bridge(string to)
        {
            var bridge = GameObject.Find("Turning bridge").transform;
            float BridgeYaw() => Mathf.Repeat(bridge.eulerAngles.y, 180f);
            float Along(string side) => side == "N" || side == "S" ? 90f : 0f;
            var turner = bridge.GetComponent<StepTurner>();
            bool Ready(string side) => turner.Paused && Mathf.Abs(Mathf.Sin((bridge.eulerAngles.y - Along(side)) * Mathf.Deg2Rad)) < .035f;   // lined up either way round
            var p = Player.transform.position - Egypt.Tomb; float dx = p.x - Egypt.Tomb.x + Egypt.Tomb.x, dz = p.z - 96f;
            string from = Mathf.Abs(dz) < 2f && Mathf.Abs(p.x) < 2f ? "M" : Mathf.Abs(dz) > Mathf.Abs(p.x) ? (dz > 0 ? "N" : "S") : (p.x > 0 ? "E" : "W");
            if (from != "M")
            {
                yield return WaitFor(() => Ready(from), 16f); yield return Wait(.2f);
                Log($"    (bridge on from {from}: yaw {bridge.eulerAngles.y:0.0} paused {turner.Paused}; player {Fmt(Player.transform.position)})");
                yield return Go(T(0, 96), .4f, 4f);
            }
            for (int i = 0; i < 30 && !Ready(to); i++) { if (i % 3 == 0) Log($"      (waiting for the bridge toward {to}: yaw {bridge.eulerAngles.y:0.0} / {BridgeYaw():0.0}, paused {turner.Paused}, player {Fmt(Player.transform.position)})"); yield return WaitFor(() => Ready(to), 1f); }
            yield return Wait(.2f);
            Log($"    (bridge {from} -> {to}: yaw {bridge.eulerAngles.y:0.0} paused {turner.Paused}; player {Fmt(Player.transform.position)})");
            var dest = to == "N" ? T(0, 105) : to == "S" ? T(0, 87) : to == "E" ? T(10, 96) : T(-10, 96);
            yield return Go(dest, .5f, 4f);
            Log($"    (the turning bridge {from} -> {to}: at {Fmt(Player.transform.position)})");
        }

        // item 9: once learned, the double jump works everywhere: the Ice Age's ridge ledge (R08)
        IEnumerator DoubleJumpInIceAge()
        {
            Head("Round 6, item 9: the double jump in the Ice Age");
            if (!Vortex.Inside(Player.transform.position)) yield break;
            yield return Go(VPath(0, 4), .8f); yield return Go(VPath(-54, 8), .6f); yield return Go(VPath(-54, 19), .6f);
            yield return Go(VPath(-54, 24), .5f, 6f, 1f, () => Game.I.Busy);
            yield return WaitFor(() => !Game.I.Busy && Game.World.worldId == WorldData.IceAge, 12f);
            Ok(Game.World.worldId == WorldData.IceAge && !Game.InCutscene, $"the Ice Age path leads back to the valley (world {Game.World.worldId}, at {Fmt(Player.transform.position)})");
            MusicCheck("the Ice Age again", "iceage");
            Ok(NecklaceModel.Tint.b > .8f, $"the necklace's shard is blue again in the Ice Age (tint {NecklaceModel.Tint})");
            yield return SwitchTo(1);
            // R08: the ridge's high ledge (8, 3.2, 206): from the ground, a double jump
            yield return TpSettle(IceAge.OnGround(8, 201) + Vector3.up * .1f, 0);
            yield return JumpTo(XZ(8, 205.5f), () => true, .3f, .35f, doubleJump: true); yield return WaitFor(() => Has("R08"), 1.5f);
            Ok(Has("R08"), $"Abi double-jumps to the Ice Age's ridge ledge (R08, y {Player.transform.position.y:0.0})");
            yield return Shot("108_iceage_double_jump");
        }

        // item 12: the frame rate in Egypt, the view drawn every frame (Dan's PC; the Steam Deck setting with --extra "-steamDeck 1")
        IEnumerator PerfEgypt()
        {
            Head($"Round 6, item 12: frame rate in Egypt, the full view drawn every frame ({(TestRun.Headless ? $"{TestRun.View.width} x {TestRun.View.height} off-screen" : "window")}, Steam Deck setting {(SteamDeck.On ? "on" : "off")})");
            if (!Game.InEgypt)
            {
                yield return EnsurePlaying(false);
                var eg = SaveGame.Current.World(WorldData.Egypt); if (!eg.opened.Contains("EG-arrived")) eg.opened.Add("EG-arrived");
                SaveGame.Current.checkpoint = Egypt.Start; yield return StartWorld(WorldData.Egypt);
                yield return WaitFor(() => !Game.I.Busy, 10f);
            }
            yield return SwitchTo(0);
            float worstAvg = 9999; string where = "";
            foreach (var (name, from, path) in new[]
            {
                ("the plaza toward the pyramids", XZ(0, 8), new[] { XZ(0, 30), XZ(-4, 46), XZ(4, 20) }),
                ("the market", XZ(0, 52), new[] { XZ(-14, 70), XZ(12, 78), XZ(0, 56) }),
                ("the Sphinx's front", XZ(-14, 206), new[] { XZ(-24, 214), XZ(-18, 224), XZ(-12, 208) }),
                ("the scaffolding's foot", XZ(70, 186), new[] { XZ(56, 190), XZ(84, 188), XZ(70, 180) }),
            })
            {
                yield return TpSettle(Egypt.OnGround(from.x, from.z) + Vector3.up * .1f, 0);
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
                yield return Shot("110_perf_" + name.Replace(' ', '_').Replace("'", ""));
            }
            float need = SteamDeck.On ? 40f : 60f;
            Ok(worstAvg > need, $"Egypt: {worstAvg:0} fps at the slowest place ({where}); need {need:0} ({SystemInfo.graphicsDeviceName}, {Screen.width}x{Screen.height})");
        }

        IEnumerator StartWorld(string world)
        {
            var w = typeof(Game).GetMethod("GoToWorld", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            yield return Game.I.StartCoroutine((IEnumerator)w.Invoke(Game.I, new object[] { world, SaveGame.Current.checkpoint, false }));
        }
    }
}
