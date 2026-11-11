using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace ShardsOfTime
{
    // Round 8, items 1-14: the Medieval Kingdom as a player plays it, with the virtual pad, from the vortex (a save with the Ice Age, Egypt
    // and Bethlehem done to their story shards, each one's first-visit shards counted; no moves learned: a player may skip the Violinists):
    // the arrival (MedievalArrival), the rival Muscle Man in the courtyard, dragon Kira's glide off the wall and her fire (the dungeon's
    // braziers, a rat, the hay heap, the brambles, the stands' braziers), the dungeon's keys and cells, a knight's shield, the bard's lesson
    // (the charged punch: the cracked wall, a shield knocked away), the archers, the great hall (the rival, the chandeliers), the test of
    // strength, the necklace, the stone (TheStone) and the vortex with Muscle Man; then a boulder and a cracked wall in Bethlehem, and Muscle
    // Man's jobs in the Medieval Kingdom (the yard's boulder, the armory's portcullis, a boost to the stands' flag platform).
    // Teleports go from place to place; what happens at each place is done with the pad.
    public sealed partial class ExeCheck
    {
        int ShardsME => SaveGame.Current.World(WorldData.Medieval).shards.Count;
        int NeedME => WorldData.Load(WorldData.Medieval).areas[0].requirement;
        Vector3 ME(float x, float z) => Medieval.OnGround(x, z, x > 300 ? 3f : 30f);
        Vector3 SpotME(string id) => Medieval.ShardPos(WorldData.Load(WorldData.Medieval).shards.First(s => s.id == id).position);
        const GamepadButton AttackBtn = GamepadButton.West, JumpBtn = GamepadButton.South;

        IEnumerator MedievalOnly()
        {
            yield return EnsurePlaying(false);
            foreach (var wid in new[] { WorldData.IceAge, WorldData.Egypt, WorldData.Bethlehem })
            {
                var prog = SaveGame.Current.World(wid); prog.storyShard = true; prog.necklaceAwake = true;
                var wd = WorldData.Load(wid); var fv = wd.areas[0].firstVisit;
                foreach (var sp in wd.shards) if (sp.requires.All(fv.Contains) && !prog.shards.Contains(sp.id)) prog.shards.Add(sp.id);
                if (!SaveGame.Current.worldsReached.Contains(wid)) SaveGame.Current.worldsReached.Add(wid);
            }
            SaveGame.Current.World(WorldData.Egypt).opened.Add("EG-arrived"); SaveGame.Current.World(WorldData.Bethlehem).opened.Add("BE-arrived");
            foreach (var p in new[] { "IA-vortex", "EG-vortex-gate", "BE-vortex-gate" }) if (!SaveGame.Current.portals.Contains(p)) SaveGame.Current.portals.Add(p);
            Log($"  (set up: the Ice Age {SaveGame.Current.World(WorldData.IceAge).shards.Count} + Egypt {SaveGame.Current.World(WorldData.Egypt).shards.Count} + Bethlehem {SaveGame.Current.World(WorldData.Bethlehem).shards.Count} = {SaveGame.Current.Total} shards, three story shards, no moves learned)");
            SaveGame.Current.checkpoint = Vortex.Checkpoint; Checkpoint.RespawnParty(); CameraRig.I.SnapBehind();
            yield return Wait(.6f);
            yield return MedievalRoute();
        }

        IEnumerator MShard(string id, float tol = .6f, float timeout = 25f)
        {
            if (Has(id)) yield break;
            yield return GoF(SpotME(id), tol, timeout);
            yield return WaitFor(() => Has(id), 1.5f);
            Ok(Has(id), $"{id} ({Game.World.shards.First(s => s.id == id).note}) at {Fmt(Player.transform.position)}; {ShardsME} here, {SaveGame.Current.Total} in all");
            yield return NecklaceIfAwakeME();
        }

        bool necklaceSeenME;
        IEnumerator NecklaceIfAwakeME()
        {
            if (necklaceSeenME || SaveGame.Current.Total < NeedME) yield break;
            yield return WaitFor(() => played.Contains("NecklaceWakesMedieval"), 10f);
            if (!played.Contains("NecklaceWakesMedieval")) yield break;
            necklaceSeenME = true;
            Ok(SaveGame.Current.World(WorldData.Medieval).necklaceAwake, $"the necklace wakes at {SaveGame.Current.Total} shards in all ({ShardsME} from the Medieval Kingdom; threshold {NeedME}) (NecklaceWakesMedieval, purple)");
            yield return Wait(2f); yield return Shot("240_necklace_wakes");
            yield return WaitFor(() => !Game.I.Busy && !Game.InCutscene, 15f);
        }

        // a short tap (the attack) facing `dir`
        IEnumerator AttackToward(Vector3 at, float hold = 0)
        {
            var d = at - Player.transform.position; d.y = 0;
            if (d.sqrMagnitude > .01f) { Player.Motor.Teleport(Player.transform.position, Quaternion.LookRotation(d)); yield return null; }
            if (hold > 0) yield return Hold(AttackBtn, hold); else yield return Press(AttackBtn);
            yield return Wait(.5f);
        }

        IEnumerator MedievalRoute()
        {
            Cutscenes.Started -= OnPlayed; Cutscenes.Started += OnPlayed;
            float t0 = Time.time;
            Head("Round 8, item 3: the vortex's Medieval path, the arrival");
            var door = VortexDoor.All.FirstOrDefault(d => d.info.id == "Medieval"); var pirate = VortexDoor.All.FirstOrDefault(d => d.info.id == "PirateSeas");
            Ok(door && !door.Blocked, $"the vortex's Medieval Kingdom path is open after Bethlehem's golden shard ({(door ? door.label.text.Replace("\n", " ") : "none")})");
            Ok(pirate && pirate.Blocked, $"the Pirate Seas path is there and closed until the purple shard ({(pirate ? pirate.label.text.Replace("\n", " ") : "none")})");
            float yaw = door ? door.info.yaw : 12f;
            yield return Go(VPath(yaw, 4), .8f); yield return Go(VPath(yaw, 19), .6f);
            yield return Go(VPath(yaw, 24), .5f, 6f, 1f, () => Game.I.Busy);
            yield return WaitFor(() => Cutscenes.Playing == "MedievalArrival", 8f);
            Ok(Game.InMedieval && Cutscenes.Playing == "MedievalArrival", $"the path loads the Medieval Kingdom and plays its arrival (world {Game.World.worldId}, cutscene {Cutscenes.Playing ?? "none"})");
            foreach (var (t, n) in new[] { (1.0f, "200a_vortex_over_courtyard"), (2.6f, "200b_into_the_hay"), (4.6f, "200c_pop_up"), (6.4f, "200d_achoo"), (8.2f, "200e_fire_jump"), (10.8f, "200f_runaway_wagon"), (12.3f, "200g_kaboom"), (13.6f, "200h_wreck"), (15.6f, "200i_slink_away") })
            {
                yield return WaitFor(() => Cutscenes.Director && Cutscenes.Director.time >= t || Cutscenes.Playing == null, 20f);
                yield return Shot(n);
            }
            yield return WaitFor(() => !Game.InCutscene && !Game.I.Busy, 15f);
            yield return Wait(.6f);
            var kira = P.Heroes[2];
            Ok(P.Heroes[0].Outfit == Outfits.Medieval && P.Heroes[1].Outfit == Outfits.Medieval && kira.Form && kira.Form.id == KiraForm.Dragon,
               $"dressed for the Medieval Kingdom (Dan {(P.Heroes[0].Model ? P.Heroes[0].Model.name : "graybox")}, Abi {(P.Heroes[1].Model ? P.Heroes[1].Model.name : "graybox")}); Kira is {(kira.Form ? kira.Form.displayName : "-")} ({(kira.Model ? kira.Model.name : "graybox")})");
            Ok(NecklaceModel.Tint.b > .8f && NecklaceModel.Tint.r > .5f && NecklaceModel.Tint.g < .5f, $"the necklace's shard is purple here (tint {NecklaceModel.Tint})");
            MusicCheck("the Medieval Kingdom", "medieval");
            Ok(Medieval.WreckSite && Medieval.WreckSite.activeSelf, "after the arrival the wagon's wreck, its singed driver and the pony are in the courtyard");
            Ok(Mathf.Abs(Player.transform.position.z - 8f) < 2f && Mathf.Abs(Player.transform.position.x) < 2f, $"play starts in the courtyard ({Fmt(Player.transform.position)})");
            yield return Shot("201_courtyard_start");
            var rr = Game.World.RequirementReport("ME"); Log("  " + rr.detail);
            Ok(rr.ok && Game.World.shards.Count == 60, $"item 9 / rule 2 (universal total): {rr.reachable} available on arrival (three worlds + the Medieval first visit), threshold {rr.requirement} (240 shards exist up to here: {100f * rr.requirement / 240f:0}%)");
            Ok(Hud.UnderTotal == $"Next: {NeedME}", $"HUD on arrival: {Hud.TotalShown}, under it \"{Hud.UnderTotal}\"");

            yield return RivalCourtyard();
            yield return Glide();
            yield return Dungeon();
            yield return NecklaceIfAwakeME(); yield return WaitFor(() => !Game.I.Busy && !Game.InCutscene, 15f);
            yield return KnightShield(false);
            yield return BardAndPunch();
            yield return KnightShield(true);
            yield return Archers();
            yield return GreatHall();
            yield return FieldAndStriker();
            yield return VillageFire();
            yield return CourtyardShards();
            Log($"  the Medieval Kingdom took the bot {(Time.time - t0) / 60f:0.0} min so far; {ShardsME} shards here, {SaveGame.Current.Total} in all; hits {Health.HitsTaken}, respawns {Health.Respawns}");
            yield return TheStoneFinale();
            yield return MuscleManEarlierWorld();
            yield return MuscleManJobsHere();
            Log("  " + TestRun.Report());
        }

        IEnumerator RivalCourtyard()
        {
            Head("Round 8, item 5: the rival Muscle Man in the courtyard (shows off, laughs at Dan's try at the boulder)");
            yield return SwitchTo(0);
            int shows = RivalMuscleMan.TotalShows;
            yield return GoF(XZ(-5, 11), .8f, 10f);
            yield return WaitFor(() => RivalMuscleMan.TotalShows > shows, 6f);
            Ok(RivalMuscleMan.TotalShows > shows, $"he shows off as the party comes near ({RivalMuscleMan.All.Count} rival spots; shows {RivalMuscleMan.TotalShows})");
            yield return Wait(1.6f); yield return TurnView(XZ(-12.5f, 11.5f)); yield return Shot("202_rival_lifts");
            yield return WaitFor(() => Medieval.CourtRival && !Medieval.CourtRival.Busy, 8f);
            int mocks = RivalMuscleMan.TotalMocks; var b = Object.FindObjectsByType<Boulder>().First(x => x.id == "ME-boulder-yard");
            yield return GoF(XZ(-15.8f, 7.5f), .6f, 8f); Stick(StickFor(Vector3.left)); yield return Wait(1.2f); Stick(Vector2.zero);
            yield return WaitFor(() => RivalMuscleMan.TotalMocks > mocks, 3f);
            Ok(b.Touches > 0 && RivalMuscleMan.TotalMocks > mocks, $"Dan pushes the yard's boulder (it won't move: {b.Touches} touches) and the rival laughs at him (mocks {RivalMuscleMan.TotalMocks})");
            yield return TurnView(XZ(-12.5f, 11.5f)); yield return Shot("203_rival_laughs");
            Ok(!Medieval.CourtRival.GetComponentInChildren<Collider>(), "the rival has no collider (he blocks nothing)");
        }

        IEnumerator Glide()
        {
            Head("Round 8, item 6: dragon Kira glides (hold Jump in the air) off the west wall walk");
            yield return SwitchTo(2);
            Ok(M.CanGlide, $"dragon Kira can glide (form {Player.Form?.displayName})");
            yield return TpSettle(new Vector3(-31.2f, 10.1f, 30), 90);
            yield return Shot("204_kira_on_the_wall");
            int g0 = M.Glides;
            Stick(StickFor(Vector3.right)); Btn(JumpBtn, true);
            yield return WaitFor(() => M.Gliding, 2f);
            yield return Wait(.8f); yield return Shot("205_kira_gliding");
            yield return WaitFor(() => M.Grounded, 12f);
            Btn(JumpBtn, false); Stick(Vector2.zero);
            Ok(M.Glides > g0 && M.GlideDistance > 20f, $"she glides {M.GlideDistance:0.0} m from 10 m up (glides {M.Glides - g0}; lands at {Fmt(Player.transform.position)})");
            // without holding Jump: an ordinary fall (for comparison)
            yield return TpSettle(new Vector3(-31.2f, 10.1f, 40), 90);
            Stick(StickFor(Vector3.right)); yield return Press(JumpBtn); yield return WaitFor(() => M.Grounded, 4f); Stick(Vector2.zero);
            var fx = Player.transform.position.x + 31.2f;
            Ok(fx < 12f, $"a plain jump off the wall lands {fx:0.0} m out (the glide carries much further)");
            yield return MShard("C02");
        }

        IEnumerator Dungeon()
        {
            Head("Round 8, items 4, 6, 8: the dungeon (dark; dragon Kira's fire lights the braziers; keys open the cells; rats run from the fire)");
            Health.Refill();
            yield return SwitchTo(2);
            yield return TpSettle(ME(-25.5f, 46), -90);
            yield return Go(ME(-29.6f, 46), .3f, 6f, 1f, () => Player.transform.position.x > 300);
            yield return WaitFor(() => Player.transform.position.x > 300, 4f);
            Ok(Player.transform.position.x > 300, $"the stair door takes the party down into the dungeon ({Fmt(Player.transform.position)})");
            yield return Wait(.8f);
            Ok(DarkVolume.At(M.Center), "it's dark down here (the necklace's light)");
            yield return Shot("210_dungeon");
            yield return MShard("D01");
            // a rat: puff at it
            var rat = Enemy.All.Where(e => e.kind == Enemy.Kind.Rat && !e.Dead).OrderBy(e => (e.transform.position - Player.transform.position).sqrMagnitude).FirstOrDefault();
            if (rat)
            {
                yield return TpSettle(rat.transform.position + (rat.transform.position - Medieval.Dungeon).normalized * 0f + new Vector3(0, .05f, -2.2f), 0);
                yield return AttackToward(rat.transform.position);
                Ok(rat.Scares > 0, $"dragon Kira's fire puff sends a rat squeaking away (scared {rat.Scares}, hits taken {rat.HitsTaken}: it doesn't hurt it)");
            }
            // the braziers
            var group = Object.FindObjectsByType<FireGroup>().First(g => g.name == "Dungeon braziers");
            foreach (var z in new[] { 8f, 22f, 36f })
            {
                yield return TpSettle(new Vector3(Medieval.Dungeon.x + 1.1f, .1f, z), 90);
                yield return AttackToward(new Vector3(Medieval.Dungeon.x + 3f, 0, z));
            }
            Health.Refill();
            Ok(group.Done, $"Kira lights the dungeon's three braziers ({group.targets.Count(t => t.Lit)} lit); the bars at the end open ({FireTarget.Burns} things burned so far)");
            yield return TurnView(Medieval.Dungeon + new Vector3(0, 1, 52)); yield return Shot("211_braziers_lit");
            yield return GoF(Medieval.Dungeon + new Vector3(0, 0, 44f), .7f, 10f); yield return GoF(Medieval.Dungeon + new Vector3(0, 0, 51.5f), .6f, 8f);   // up the corridor, through the opened bars
            yield return MShard("D09"); yield return MShard("D10");
            yield return GoF(Medieval.Dungeon + new Vector3(0, 0, 55.2f), .5f, 6f); yield return Wait(.5f);
            Ok(SaveGame.Current.heartUpgrades.Contains("ME-H1"), $"the brazier room's heart (max hearts {SaveGame.Current.maxHearts})");
            // the keys and the cells (anyone)
            yield return SwitchTo(1);
            for (int i = 0; i < 3; i++)
            {
                var doorC = Object.FindObjectsByType<CellDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(d => d.id == "ME-cell-" + (i + 1));
                float[] cz = { 9, 23, 37 };
                var keyAt = Medieval.KeyAt(i); keyAt.y = 0;
                yield return TpSettle(new Vector3(Medieval.Dungeon.x, .1f, keyAt.z), 90);
                yield return GoF(keyAt, .5f, 8f);
                yield return WaitFor(() => doorC.Open, 2f);
                Ok(doorC.Open, $"the key in the east alcove opens cell {i + 1}");
                if (i == 0) yield return Shot("212_key_cell_open");
                yield return GoF(new Vector3(Medieval.Dungeon.x - 1, 0, cz[i] + 4f), .6f, 8f);
                yield return MShard("D0" + (3 + i * 2)); yield return MShard("D0" + (4 + i * 2));
                yield return GoF(new Vector3(Medieval.Dungeon.x - 2, 0, cz[i] + 4f), .6f, 8f); yield return GoF(new Vector3(Medieval.Dungeon.x, 0, cz[i] + 4f), .6f, 6f);   // out by the cell's door
            }
            yield return MShard("D02");
            yield return GoF(Medieval.Dungeon + new Vector3(0, 0, 2.5f), .6f, 15f);
            yield return Go(Medieval.Dungeon + new Vector3(0, 0, -.2f), .3f, 5f, 1f, () => Player.transform.position.x < 300);
            yield return WaitFor(() => Player.transform.position.x < 300, 4f);
            Ok(Player.transform.position.x < 0, $"the stair door at the corridor's south end leads back up to the courtyard ({Fmt(Player.transform.position)})");
        }

        Enemy KnightNear(Vector3 at) => Enemy.All.Where(e => e.kind == Enemy.Kind.Knight && !e.Dead && e.ShieldUp).OrderBy(e => (e.transform.position - at).sqrMagnitude).FirstOrDefault();

        IEnumerator KnightShield(bool charged)
        {
            Head(charged ? "Round 8, items 7-8: the charged punch knocks a knight's shield away" : "Round 8, item 8: a clumsy knight's shield blocks a punch from the front; from behind it lands");
            yield return SwitchTo(0);
            var k = KnightNear(XZ(0, 30)); if (!k) { Fail("no knight with a shield"); yield break; }
            var f = k.transform.forward; f.y = 0; f.Normalize();
            yield return TpSettle(k.transform.position + f * 1.7f, 0);
            int blocks = k.Blocks_, hits = k.HitsTaken;
            k.Stun(3f);
            yield return AttackToward(k.transform.position, charged ? .9f : 0);
            if (!charged)
            {
                Ok(k.Blocks_ > blocks && k.HitsTaken == hits, $"a punch from the front clangs off the shield (blocks {k.Blocks_}, hits {k.HitsTaken})");
                yield return Shot("215_knight_blocks");
                k.Stun(3f);
                yield return TpSettle(k.transform.position - f * 1.4f, 0);
                yield return AttackToward(k.transform.position);
                Ok(k.HitsTaken > hits, $"from behind the punch lands (hits {k.HitsTaken})");
            }
            else
            {
                yield return WaitFor(() => !k.ShieldUp, 1.5f);
                Ok(!k.ShieldUp, $"the charged punch knocks the shield away (shield up {k.ShieldUp}, big punches {P.Heroes[0].BigPunches})");
                yield return Shot("232_shield_flies");
            }
            Health.Refill();
        }

        IEnumerator BardAndPunch()
        {
            Head("Round 8, item 7: the bard (the Violinist with his lute) teaches Dan the charged punch");
            yield return SwitchTo(0);
            var v = Violinist.I;
            Ok(v && v.instrument == "Lute", $"the Violinist plays a lute here ({(v ? v.instrument + ", model " + v.ModelName : "none")})");
            yield return TpSettle(ME(Medieval.ViolinistAt.x + 5f, Medieval.ViolinistAt.z), -90);
            yield return TurnView(Medieval.ViolinistAt); yield return Shot("220_bard");
            for (int tries = 0; tries < 3 && Cutscenes.Playing != "ViolinistTeachesMedieval" && !SaveGame.Current.Has(Abilities.ChargedPunch); tries++)
            {
                yield return WaitFor(() => !Game.I.Busy && !Game.InCutscene, 15f);
                yield return Go(ME(Medieval.ViolinistAt.x + 1.6f, Medieval.ViolinistAt.z), .5f, 8f, 1f, () => Game.I.Busy);
                yield return WaitFor(() => Cutscenes.Playing == "ViolinistTeachesMedieval" || Game.InCutscene, 5f);
            }
            Ok(Cutscenes.Playing == "ViolinistTeachesMedieval", "he plays for Dan (ViolinistTeachesMedieval)");
            yield return Wait(1.2f); Log($"  the lute: pose {v.InstrumentPose}; {v.ArmReport}"); yield return Shot("221_bard_plays_lute");
            yield return Wait(2f); yield return Shot("222_dan_learns");
            yield return WaitFor(() => !Game.I.Busy && !Game.InCutscene, 15f);
            Ok(SaveGame.Current.Has(Abilities.ChargedPunch) && P.Heroes[0].HasAbility(Abilities.ChargedPunch), "Dan learned the charged punch (saved)");
            // the store room's cracked wall
            Head("Round 8, item 7: the charged punch breaks the store room's cracked wall (a plain punch doesn't)");
            yield return TpSettle(ME(-20.4f, 56), -90);
            yield return AttackToward(XZ(-22.5f, 56));
            Ok(!SaveGame.Current.World(WorldData.Medieval).opened.Contains("ME-crack-store"), "a plain punch only cracks it a little more");
            yield return Wait(.5f);
            yield return AttackToward(XZ(-22.5f, 56), .9f);
            yield return WaitFor(() => SaveGame.Current.World(WorldData.Medieval).opened.Contains("ME-crack-store"), 2f);
            Ok(SaveGame.Current.World(WorldData.Medieval).opened.Contains("ME-crack-store"), $"held and let go, the charged punch breaks it (charged punches {P.ChargedPunches})");
            yield return Shot("223_wall_broken");
            yield return MShard("K01"); yield return MShard("K02");
            yield return Go(ME(-19, 56), .6f, 6f);
        }

        IEnumerator Archers()
        {
            Head("Round 8, item 8: nervous archers on the walls loose slow, dodgeable arrows");
            int shots0 = Enemy.All.Where(e => e.kind == Enemy.Kind.Archer).Sum(e => e.Shots);
            yield return SwitchTo(1);
            yield return TpSettle(ME(-2, 14), 180);
            yield return WaitFor(() => Enemy.All.Where(e => e.kind == Enemy.Kind.Archer).Sum(e => e.Shots) > shots0, 10f);
            int shots = Enemy.All.Where(e => e.kind == Enemy.Kind.Archer).Sum(e => e.Shots) - shots0;
            Ok(shots > 0, $"an archer on the south wall draws and shoots at Abi in the courtyard ({shots} arrows in the wait; {Arrow.Hits} hit so far)");
            yield return TurnView(new Vector3(0, 10, -1.5f)); yield return Shot("225_archers");
            // dodge: run sideways while they shoot
            int hits0 = Arrow.Hits; yield return GoF(XZ(8, 12), .8f, 6f); yield return GoF(XZ(-8, 12), .8f, 6f); Stick(Vector2.zero);
            Log($"  arrows that hit Abi while she ran about: {Arrow.Hits - hits0}");
            Health.Refill();
        }

        IEnumerator GreatHall()
        {
            Head("Round 8, item 4: the great hall (the rival at the feast, the gallery, the chandeliers to the east ledge)");
            yield return SwitchTo(1);
            yield return TpSettle(ME(0, 58), 0);
            int shows = RivalMuscleMan.TotalShows;
            yield return GoF(XZ(0, 67), .7f, 10f);
            yield return WaitFor(() => RivalMuscleMan.TotalShows > shows, 5f);
            Ok(RivalMuscleMan.TotalShows > shows, $"the rival shows off at the feast (lifts a barrel; shows {RivalMuscleMan.TotalShows})");
            yield return Wait(1.4f); yield return TurnView(XZ(8, 70)); yield return Shot("226_hall_rival");
            yield return MShard("G02"); yield return MShard("G03");
            // up the gallery stair
            yield return GoF(XZ(-1.5f, 67f), .5f, 8f); yield return GoF(new Vector3(-12.5f, 6f, 67.2f), .7f, 15f);
            Ok(Player.transform.position.y > 5.5f, $"up the stair onto the west gallery ({Fmt(Player.transform.position)})");
            yield return MShard("G04"); yield return MShard("G05", .6f, 30f);
            // ride a chandelier: stand on it at its west end, see it carry her
            var ch = Object.FindObjectsByType<MovingPlatform>().Where(m => m.name.StartsWith("Chandelier")).OrderBy(m => m.transform.position.x).First();
            yield return WaitFor(() => Vector3.Distance(ch.transform.position, ch.a) < .4f, 7f);
            Tp(ch.transform.position + Vector3.up * .5f, 90); var x0 = ch.transform.position.x;
            yield return Wait(2.2f);
            Ok(Player.transform.position.y > 5.4f && Player.transform.position.x - x0 > 3f, $"a chandelier carries Abi across the hall ({Fmt(Player.transform.position)}, {Player.transform.position.x - x0:0.0} m)");
            yield return Shot("227_chandelier");
            yield return TpSettle(new Vector3(12.5f, 6.1f, 71), 0);
            yield return MShard("G06"); yield return MShard("G07");
            yield return GoF(new Vector3(13, 6, 75), .5f, 5f); yield return Wait(.4f);
            Ok(SaveGame.Current.heartUpgrades.Contains("ME-H2"), "the east ledge's heart");
            yield return MShard("G01", .6f, 10f);
        }

        IEnumerator FieldAndStriker()
        {
            Head("Round 8, items 4-6: the tournament field: the rival rings the bell, Dan can't; Kira lights the stands' braziers and burns the hay");
            yield return SwitchTo(0);
            yield return TpSettle(ME(0, -24), 180);
            yield return MShard("M03");
            yield return GoF(XZ(0, -50), 1f, 20f); yield return GoF(XZ(40, -80), 1.2f, 30f); yield return MShard("T01");
            int shows = RivalMuscleMan.TotalShows;
            yield return GoF(XZ(54, -76), 1f, 25f); yield return GoF(XZ(56, -92), 1f, 15f); yield return GoF(XZ(66, -95), 1f, 15f);   // round the west end of the lists' rail
            yield return WaitFor(() => RivalMuscleMan.TotalShows > shows, 5f);
            Ok(RivalMuscleMan.TotalShows > shows, "the rival strikes the pad: the puck flies, the bell rings, he flexes");
            yield return Wait(.6f); yield return TurnView(new Vector3(70, 3, -100)); yield return Shot("230_rival_rings_the_bell");
            yield return WaitFor(() => RivalMuscleMan.All.All(r => !r.Busy), 8f);
            int tries = Medieval.Striker.Tries, mocks = RivalMuscleMan.TotalMocks;
            yield return TpSettle(ME(70, -101.8f), 0);
            yield return AttackToward(new Vector3(70, 0, -100));
            yield return WaitFor(() => RivalMuscleMan.TotalMocks > mocks, 4f);
            Ok(Medieval.Striker.Tries > tries && Medieval.Striker.LastHeight < 1f && RivalMuscleMan.TotalMocks > mocks, $"Dan hits the pad: the puck gets {Medieval.Striker.LastHeight * 100:0}% of the way, no bell, and the rival laughs (mocks {RivalMuscleMan.TotalMocks})");
            yield return Shot("231_dan_fails_rival_laughs");
            // the charged punch at the pad: so close
            yield return Wait(2f); tries = Medieval.Striker.Tries;
            yield return AttackToward(new Vector3(70, 0, -100), .9f); yield return Wait(.6f);
            Ok(Medieval.Striker.Tries > tries && Medieval.Striker.LastHeight > .8f && Medieval.Striker.LastHeight < 1f, $"even the charged punch only gets it {Medieval.Striker.LastHeight * 100:0}% up (so close)");
            // Kira: the stands' braziers -> the prize chest
            yield return SwitchTo(2);
            var group = Object.FindObjectsByType<FireGroup>().First(g => g.name == "Stands braziers");
            foreach (var x in new[] { 77f, 85f, 93f })
            {
                yield return TpSettle(new Vector3(x - 1.3f, 3.1f, -60.6f), 90);
                yield return AttackToward(new Vector3(x, 3f, -60.2f));
            }
            Ok(group.Done, $"Kira lights the three braziers on the stands; the prize chest opens ({group.targets.Count(t => t.Lit)} lit)");
            yield return TurnView(new Vector3(85, 3, -60)); yield return Shot("233_stands_braziers");
            yield return MShard("T05"); yield return MShard("T04");
            yield return TpSettle(ME(95, -112), 180); yield return MShard("T07");
            // the hay heap
            yield return TpSettle(ME(122, -105.5f), 180);
            yield return AttackToward(XZ(122, -110));
            yield return WaitFor(() => SaveGame.Current.World(WorldData.Medieval).opened.Contains("ME-hay-field"), 3f);
            yield return Wait(1.5f);
            Ok(SaveGame.Current.World(WorldData.Medieval).opened.Contains("ME-hay-field"), "Kira's fire burns the hay heap away");
            yield return MShard("T06");
            yield return MShard("T03", .6f, 30f); yield return GoF(XZ(116, -84), 1f, 15f); yield return GoF(XZ(112, -76), 1f, 15f); yield return MShard("T02", .6f, 30f);
        }

        IEnumerator VillageFire()
        {
            Head("Round 8, item 6: the village: Kira burns the brambles; up the crates onto a stall's awning");
            yield return SwitchTo(2);
            yield return TpSettle(ME(-60, -113.5f), 180);
            yield return AttackToward(XZ(-60, -118.5f));
            yield return Wait(1.6f);
            yield return TpSettle(ME(-64.5f, -113.5f), 180); yield return AttackToward(XZ(-64.5f, -118.5f)); yield return Wait(.3f);
            yield return TpSettle(ME(-55.5f, -113.5f), 180); yield return AttackToward(XZ(-55.5f, -118.5f)); yield return Wait(1.6f);
            Ok(FireTarget.All.Count(f => f.kind == FireTarget.Kind.Bramble && f.Lit) + (3 - FireTarget.All.Count(f => f.kind == FireTarget.Kind.Bramble)) >= 1, "the brambles burn away");
            yield return Shot("235_brambles_burned");
            yield return MShard("V06");
            yield return SwitchTo(1);
            yield return TpSettle(ME(-70, -70), 270);
            yield return MShard("V01", .6f, 30f); yield return MShard("V02"); yield return MShard("V03");
            yield return GoF(XZ(-96.5f, -94.5f), .5f, 10f); yield return JumpTo(new Vector3(-96.5f, 1f, -92.4f), () => true, .4f, .4f);
            yield return Wait(.4f); yield return JumpTo(new Vector3(-96.5f, 2f, -92.4f), () => true, .3f, .4f); yield return Wait(.4f);
            yield return JumpTo(SpotME("V04"), () => true, .3f, .5f); yield return WaitFor(() => Has("V04"), 2f);
            Ok(Has("V04"), $"V04 on a stall's awning, up the crates ({Fmt(Player.transform.position)})");
            yield return MShard("V05", .6f, 30f); yield return MShard("V07", .6f, 30f);
        }

        IEnumerator CourtyardShards()
        {
            Head("Round 8, item 9: the courtyard, the walls, the arcade's roof (until the necklace wakes)");
            yield return SwitchTo(1);
            yield return TpSettle(ME(0, 9), 0);
            foreach (var id in new[] { "C01", "C05", "C03", "C04", "C06" }) yield return MShard(id, .6f, 30f);
            // up the west wall's stair
            yield return GoF(XZ(-28.8f, 12.4f), .6f, 20f); yield return GoF(new Vector3(-28.8f, 10f, 30.5f), .7f, 25f);
            Ok(Player.transform.position.y > 9.5f, $"up the stair onto the west wall walk ({Fmt(Player.transform.position)})");
            yield return GoF(new Vector3(-31.6f, 10f, 31.5f), .6f, 8f);   // off the stair's top onto the walk
            foreach (var id in new[] { "W02", "W03" }) yield return MShard(id, .6f, 25f);
            yield return GoF(new Vector3(-30.8f, 10f, 45.6f), .6f, 10f); yield return GoF(new Vector3(-30.8f, 16f, 57.5f), .8f, 20f); yield return MShard("W04", .8f, 15f);
            yield return Shot("238_nw_tower_top");
            yield return GoF(new Vector3(-30.8f, 16f, 56.5f), .7f, 8f); yield return GoF(new Vector3(-30.8f, 10f, 45.5f), .8f, 20f); yield return GoF(new Vector3(-31.6f, 10f, 40f), .7f, 10f);
            yield return MShard("W01", .6f, 30f);
            // the arcade's stair, its roof, the steps on to the east walk and up the north-east tower
            yield return TpSettle(ME(26.5f, 61), 180);
            yield return GoF(new Vector3(26.5f, 6.6f, 51f), .8f, 20f);
            foreach (var id in new[] { "C08", "C07" }) yield return MShard(id, .6f, 25f);
            yield return GoF(new Vector3(28.9f, 6.6f, 8.4f), .6f, 15f); yield return GoF(new Vector3(28.9f, 10f, 17.6f), .7f, 10f); yield return GoF(new Vector3(31.6f, 10f, 18f), .6f, 6f);
            Ok(Player.transform.position.y > 9.5f, $"from the arcade's roof up the steps onto the east wall walk ({Fmt(Player.transform.position)})");
            foreach (var id in new[] { "W05", "W06" }) yield return MShard(id, .6f, 25f);
            yield return GoF(new Vector3(30.8f, 10f, 45.6f), .6f, 10f); yield return GoF(new Vector3(30.8f, 16f, 57.5f), .8f, 20f); yield return MShard("W07", .8f, 15f);
            yield return NecklaceIfAwakeME();
        }

        IEnumerator TheStoneFinale()
        {
            Head("Round 8, item 10: the stone: TheStone, Muscle Man swept along");
            yield return NecklaceIfAwakeME();
            var prog = SaveGame.Current.World(WorldData.Medieval);
            if (!prog.necklaceAwake)
            {
                // more easy shards until it wakes (a player gathers more first)
                foreach (var s in Game.World.shards.Where(s => s.requires.Length == 0 && !Has(s.id)).ToList())
                {
                    if (SaveGame.Current.Total >= NeedME) break;
                    var at = SpotME(s.id); yield return TpSettle(at + new Vector3(0, .2f, -2.5f), 0); yield return MShard(s.id, .6f, 12f);
                }
                yield return NecklaceIfAwakeME();
            }
            Ok(prog.necklaceAwake, $"the necklace is awake ({SaveGame.Current.Total} of {NeedME})");
            yield return TpSettle(ME(0, 28), 0);
            yield return SwitchTo(0);   // (after the move: there's room for Dan here)
            yield return Shot("245_the_stone_awake");
            yield return Go(ME(0, 32.8f), .3f, 6f, .6f, () => Game.I.Busy);
            yield return WaitFor(() => Cutscenes.Playing == "TheStone", 6f);
            Ok(Cutscenes.Playing == "TheStone", "Dan reaches the stone: TheStone plays");
            foreach (var (t, n) in new[] { (1.0f, "250a_muscleman_brags"), (4.4f, "250b_the_heave"), (7.6f, "250c_flat_on_his_back"), (9.6f, "250d_dan_steps_up"), (12.6f, "250e_it_slides_out"), (14.8f, "250f_master"), (17.0f, "250g_the_vortex") })
            {
                yield return WaitFor(() => Cutscenes.Director && Cutscenes.Director.time >= t || Cutscenes.Playing == null, 22f);
                yield return Shot(n);
            }
            yield return WaitFor(() => !Game.I.Busy && !Game.InCutscene, 20f);
            yield return Wait(1f);
            Ok(Vortex.Inside(Player.transform.position) && prog.storyShard, $"into the vortex with the purple shard ({SaveGame.Current.StoryShards} of {Vortex.StoryShardsTotal})");
            Ok(P.Companion && SaveGame.Current.Has(Abilities.MuscleMan), $"Muscle Man came too: he follows the party (model {(P.Companion && P.Companion.Model ? P.Companion.Model.name : "graybox")})");
            var home = VortexDoor.All.First(d => d.info.id == "Home"); var pirate = VortexDoor.All.First(d => d.info.id == "PirateSeas");
            Ok(!pirate.Blocked && home.label.text.Contains("4 of 8"), $"the Pirate Seas path is open; Home says \"{home.label.text.Replace("\n", " ")}\"");
            yield return Go(VPath(0, 6), .8f, 6f); yield return Wait(1.5f);
            Ok(P.Companion && Vector3.Distance(P.Companion.transform.position, Player.transform.position) < 8f, $"Muscle Man keeps up in the vortex ({Vector3.Distance(P.Companion.transform.position, Player.transform.position):0.0} m behind; warps {P.Companion.Follower.Warps})");
            yield return TurnView(Player.transform.position + Vector3.back * 3); yield return Shot("251_vortex_with_muscleman");
        }

        IEnumerator MuscleManEarlierWorld()
        {
            Head("Round 8, items 7 and 11: in an earlier world (Bethlehem): Muscle Man rolls the cave's boulder away; Dan's charged punch breaks the courtyard's cracked wall");
            yield return Go(VPath(-12, 4), .8f); yield return Go(VPath(-12, 19), .6f);
            yield return Go(VPath(-12, 24), .5f, 6f, 1f, () => Game.I.Busy);
            yield return WaitFor(() => Game.InBethlehem && !Game.I.Busy, 12f);
            Ok(Game.InBethlehem && P.Companion, $"Bethlehem, with Muscle Man along ({(P.Companion && P.Companion.Model ? P.Companion.Model.name : "-")})");
            yield return SwitchTo(0);
            float cg = Bethlehem.Terrain(180, 60);
            yield return TpSettle(new Vector3(180.2f, cg + .1f, 60), 90); yield return Wait(1.5f);
            var b = Object.FindObjectsByType<Boulder>().First(x => x.id == "BE-boulder-cave");
            yield return Press(AttackBtn);
            yield return WaitFor(() => b.IsDone && !Game.I.Busy, 8f);
            Ok(b.IsDone, $"X by the cave's boulder: Muscle Man rolls it away (jobs {MuscleJob.Jobs})");
            yield return TurnView(new Vector3(186, cg + 1, 60)); yield return Shot("260_bethlehem_boulder_moved");
            yield return GoF(new Vector3(188, cg, 62), .7f, 10f); yield return WaitFor(() => Has("M01"), 2f);
            Ok(Has("M01"), "M01 in the shepherds' cave behind it");
            yield return TpSettle(Bethlehem.OnGround(-160, 128.6f), 0);
            yield return AttackToward(new Vector3(-160, 0, 131.5f), .9f);
            yield return WaitFor(() => SaveGame.Current.World(WorldData.Bethlehem).opened.Contains("BE-crack-courtyard"), 2f);
            Ok(SaveGame.Current.World(WorldData.Bethlehem).opened.Contains("BE-crack-courtyard"), "Dan's charged punch breaks Bethlehem's cracked courtyard wall");
            yield return Shot("261_bethlehem_wall_broken");
            yield return GoF(new Vector3(-160, 0, 136), .7f, 10f); yield return WaitFor(() => Has("C01"), 2f);
            Ok(Has("C01"), "C01 behind it");
        }

        IEnumerator MuscleManJobsHere()
        {
            Head("Round 8, item 11: Muscle Man's jobs in the Medieval Kingdom (the yard's boulder, the armory's portcullis, a boost)");
            SaveGame.Current.checkpoint = Vortex.Checkpoint; Checkpoint.RespawnParty(); CameraRig.I.SnapBehind(); yield return Wait(.6f);
            yield return Go(VPath(12, 4), .8f); yield return Go(VPath(12, 19), .6f);
            yield return Go(VPath(12, 24), .5f, 6f, 1f, () => Game.I.Busy);
            yield return WaitFor(() => Game.InMedieval && !Game.I.Busy, 12f);
            Ok(Game.InMedieval && played.Count(p => p == "MedievalArrival") <= 1 && P.Companion, "back in the Medieval Kingdom (no second arrival), Muscle Man along");
            Ok(RivalMuscleMan.All.All(r => !r.gameObject.activeInHierarchy), "the rival is gone from his spots (he's with the party now)");
            yield return SwitchTo(0);
            yield return TpSettle(ME(-15.6f, 7.5f), -90); yield return Wait(1.5f);
            var b = Object.FindObjectsByType<Boulder>().First(x => x.id == "ME-boulder-yard");
            yield return Press(AttackBtn); yield return WaitFor(() => b.IsDone && !Game.I.Busy, 8f);
            Ok(b.IsDone, "Muscle Man rolls the yard's boulder away");
            yield return Shot("265_yard_boulder");
            yield return MShard("B01"); yield return MShard("B02");
            var pc = Object.FindObjectsByType<Portcullis>().First(x => x.id == "ME-portcullis-armory");
            yield return TpSettle(ME(22, 10.5f), 180); yield return Wait(1.2f);
            yield return Press(AttackBtn); yield return WaitFor(() => pc.IsDone && !Game.I.Busy, 8f);
            Ok(pc.IsDone, "Muscle Man heaves the armory's portcullis up");
            yield return Shot("266_portcullis");
            yield return MShard("A01"); yield return MShard("A02");
            var boost = Object.FindObjectsByType<BoostSpot>().First();
            yield return TpSettle(new Vector3(83.6f, .1f, -54f), 180); yield return Wait(1.2f);
            yield return Press(AttackBtn); yield return WaitFor(() => boost.Boosts > 0, 4f);
            yield return WaitFor(() => M.Grounded, 4f);
            Ok(boost.Boosts > 0 && Player.transform.position.y > 5f, $"Muscle Man boosts Dan up onto the stands' flag platform ({Fmt(Player.transform.position)})");
            yield return Shot("267_boost");
            yield return GoF(new Vector3(84.4f, 5.6f, -57.75f), .3f, 4f); yield return GoF(new Vector3(86.1f, 5.6f, -57.75f), .3f, 4f);   // round the flag post
            yield return MShard("X01", .5f, 6f);
        }

        IEnumerator PerfMedieval()
        {
            Head($"Round 8, item 12: frame rate in the Medieval Kingdom, the full view drawn every frame ({(TestRun.Headless ? $"{TestRun.View.width} x {TestRun.View.height} off-screen" : "window")}, Steam Deck setting {(SteamDeck.On ? "on" : "off")})");
            if (!Game.InMedieval)
            {
                yield return EnsurePlaying(false);
                var me = SaveGame.Current.World(WorldData.Medieval); if (!me.opened.Contains("ME-arrived")) me.opened.Add("ME-arrived");
                SaveGame.Current.checkpoint = Medieval.VortexCp; yield return StartWorld(WorldData.Medieval);
                yield return WaitFor(() => !Game.I.Busy, 10f);
            }
            yield return SwitchTo(0);
            float worstAvg = 9999; string where = "";
            foreach (var (name, from, path) in new[]
            {
                ("the courtyard toward the keep", XZ(0, 6), new[] { XZ(-10, 30), XZ(10, 40), XZ(0, 10) }),
                ("the great hall", XZ(0, 67), new[] { XZ(-8, 78), XZ(8, 78), XZ(0, 68) }),
                ("the tournament field", XZ(55, -60), new[] { XZ(90, -95), XZ(110, -70), XZ(60, -55) }),
                ("the village market", XZ(-45, -70), new[] { XZ(-85, -85), XZ(-110, -70), XZ(-50, -70) }),
            })
            {
                yield return TpSettle(ME(from.x, from.z) + Vector3.up * .1f, 0);
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
                yield return Shot("290_perf_" + name.Replace(' ', '_'));
            }
            float need = SteamDeck.On ? 40f : 60f;
            Ok(worstAvg > need, $"the Medieval Kingdom: {worstAvg:0} fps at the slowest place ({where}); need {need:0} ({SystemInfo.graphicsDeviceName}, {Screen.width}x{Screen.height})");
        }

        // quick looks (teleports, screenshots) for building the world
        IEnumerator MedDiag()
        {
            yield return EnsurePlaying(false);
            var me = SaveGame.Current.World(WorldData.Medieval); if (!me.opened.Contains("ME-arrived")) me.opened.Add("ME-arrived");
            SaveGame.Current.checkpoint = Medieval.Start; yield return StartWorld(WorldData.Medieval); yield return WaitFor(() => !Game.I.Busy, 10f);
            yield return Wait(1.5f);
            yield return Shot("d0_spawn_player_view");
            yield return CloseUpAt(new Vector3(0, 4, -28), new Vector3(0, 8, 40), "d1_gate_and_keep");
            yield return CloseUpAt(new Vector3(-10, 3, 12), new Vector3(2, 1.5f, 34), "d2_courtyard_stone");
            yield return CloseUpAt(new Vector3(20, 14, 20), new Vector3(-20, 2, 40), "d3_courtyard_high");
            yield return CloseUpAt(new Vector3(0, 4, 67), new Vector3(0, 3, 80), "d4_hall");
            yield return CloseUpAt(Medieval.Dungeon + new Vector3(0, 2.4f, 3), Medieval.Dungeon + new Vector3(0, 1.5f, 30), "d5_dungeon");
            yield return CloseUpAt(new Vector3(60, 8, -40), new Vector3(90, 2, -85), "d6_field");
            yield return CloseUpAt(new Vector3(-40, 8, -60), new Vector3(-90, 2, -90), "d7_village");
            yield return CloseUpAt(new Vector3(-60, 30, -50), new Vector3(0, 4, 40), "d8_castle_from_sw");
            yield return CloseUpAt(new Vector3(-20, 3, 30), new Vector3(-26, 1.2f, 24), "d9_wreck");
            yield return CloseUpAt(new Vector3(-8, 2.2f, 36), new Vector3(-14, 1.3f, 38), "d10_bard");
            yield return CloseUpAt(new Vector3(-6, 2.2f, 14), new Vector3(-12.5f, 1.3f, 11.5f), "d11_rival");
        }
    }
}
