using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace ShardsOfTime
{
    // Fix 10.1's checks (built exe, batch mode, muted, the virtual pad):
    //  minplayer: the minimum player from New game to the Pirate Seas' green shard. In every world they take only what the stepped
    //    threshold needs (25), only shards reachable before that world's necklace wakes with the moves held then (each shard: the bot is
    //    put on it, its own pickup takes it: the reach of every shard is fix81 / seareach's job). Before the 25th the necklace must still
    //    sleep, at the 25th it must wake. Story shards, gates, vortex paths, arrivals (skipped with the pad as a player can) run for real.
    //    Mid-Pirate: Pause > Return to the vortex and back (the spot kept), and Pause > Theater (a scene plays, play comes back where it was).
    //  tpose: every animated character in every world, and every cutscene at five moments: none in its rest (bind) pose.
    //  sails: the Pirate Seas' deck and rigging from the player's view: what is between the camera and the player fades; masts no longer
    //    pull the camera in.
    //  theater: the title screen's Theater on a save that has seen scenes (Fix 10.1 fills an old save in).
    public sealed partial class ExeCheck
    {
        IEnumerator Fix101() { yield return MinPlayer(); yield return TPoseSweep(); yield return SailsView(); }

        // ---------------------------------------------------------------- the minimum player
        IEnumerator SkipCutscene()
        {
            yield return WaitFor(() => Game.InCutscene, 3f);
            if (Game.InCutscene) { string n = Cutscenes.Playing; yield return Wait(.6f); yield return Hold(GamepadButton.South, 1.3f); yield return WaitFor(() => !Game.InCutscene, 4f); Log($"  (skipped {n} with the pad)"); }
        }
        IEnumerator UntilFree(float s = 40f)
        {
            float end = Time.time + s;
            while (Time.time < end && (Game.I.Busy || Game.InCutscene))
            {
                if (Game.InCutscene && Cutscenes.SkipHeld01 == 0) { yield return Wait(.4f); if (Game.InCutscene) yield return Hold(GamepadButton.South, 1.3f); }
                yield return null;
            }
            yield return Wait(.4f);
        }

        // the bot is put on one shard; its own pickup takes it (2 s); false if it could not
        IEnumerator TakeShard(Pickup p, List<string> got)
        {
            if (!p) yield break;
            Health.Refill();
            string id = p.id; var at = p.transform.position;
            Tp(at - Vector3.up * .3f, Player.transform.eulerAngles.y);
            yield return WaitFor(() => !p || !p.gameObject.activeInHierarchy || Has(id), 2f);
            if (Has(id)) got.Add(id);
        }

        int ThisWorld => SaveGame.Current.World(Game.World.worldId).shards.Count;
        bool Awake => SaveGame.Current.World(Game.World.worldId).necklaceAwake;

        // in the open world: take only what the threshold needs, from shards reachable before the wake with the first-visit moves
        IEnumerator MinimumHere(string label, System.Func<IEnumerator> midway = null)
        {
            var w = Game.World; var a = w.areas[0]; var have = new HashSet<string>(a.firstVisit);
            int need = Mathf.Max(a.requirement - SaveGame.Current.Total, a.fromHere) ;
            var pre = new HashSet<string>(w.shards.Where(s => s.requires.All(have.Contains)).Select(s => s.id));
            Log($"  {label}: {SaveGame.Current.Total} in all on arrival, threshold {a.requirement} and {a.fromHere} from here: needs {need} here; {pre.Count} of {w.shards.Count} reachable before the wake");
            var got = new List<string>(); int tried = 0; bool sleptBefore = true;
            foreach (var pid in Pickup.All.Where(p => p && p.kind == Pickup.Kind.Shard && pre.Contains(p.id)).Select(p => p.id).OrderBy(x => x).ToList())
            {
                if (got.Count >= need) break;
                var p = Pickup.All.FirstOrDefault(q => q && q.kind == Pickup.Kind.Shard && q.id == pid);   // looked up again: the Theater rebuilds the world
                if (!p || Has(pid)) continue;
                if (got.Count == need - 1) sleptBefore = !Awake && !Game.I.Busy;
                tried++; yield return TakeShard(p, got);
                if (midway != null && got.Count == 10) { var m = midway; midway = null; yield return m(); }
            }
            Ok(got.Count >= need, $"{label}: took {got.Count} of the {need} needed ({tried} tried; {SaveGame.Current.Total} in all)");
            Ok(sleptBefore, $"{label}: one short ({SaveGame.Current.Total - 1} in all, {ThisWorld - 1} from here), the necklace still sleeps");
            yield return WaitFor(() => Game.InCutscene || Awake, 6f);
            yield return UntilFree();
            Ok(Awake, $"{label}: at {SaveGame.Current.Total} in all ({ThisWorld} from here) the necklace wakes");
            yield return Shot("min_" + label.Replace(" ", "_") + "_awake");
        }

        Pickup StoryPickup(string id = null) => Pickup.All.FirstOrDefault(p => p && p.kind == Pickup.Kind.StoryShard && p.gameObject.activeInHierarchy && (id == null || p.id == id));

        IEnumerator TakeStory(string label, string id = null)
        {
            yield return WaitFor(() => StoryPickup(id) != null, 3f);
            var sp = StoryPickup(id);
            Ok(sp != null, $"{label}: the story shard is out ({(sp ? Fmt(sp.transform.position) : "none")})");
            if (!sp) yield break;
            if (Game.InMedieval) yield return SwitchTo(0);
            Health.Refill();
            Tp(sp.transform.position - Vector3.up * .3f, 0);
            yield return WaitFor(() => Game.I.Busy || Game.InCutscene, 3f);
            yield return UntilFree(60f);
        }

        IEnumerator ThroughGate(string label)
        {
            if (Vortex.Inside(Player.transform.position)) yield break;
            var g = Game.Gate;
            Ok(g && g.Open, $"{label}: the vortex gate is open");
            Tp(g.transform.position + Vector3.up * .1f, g.transform.eulerAngles.y);
            yield return WaitFor(() => Vortex.Inside(Player.transform.position), 4f);
            yield return Wait(.5f);
        }

        IEnumerator TakePath(string path, string label)
        {
            Ok(Vortex.Inside(Player.transform.position), $"{label}: in the vortex");
            var door = VortexDoor.All.FirstOrDefault(d => d.info.id == path);
            Ok(door && !door.Blocked, $"{label}: the {path} path is open");
            float yaw = Yaw(path);
            yield return Go(VPath(yaw, 4), .8f); yield return Go(VPath(yaw, 19), .6f);
            yield return Go(VPath(yaw, 24), .5f, 6f, 1f, () => Game.I.Busy);
            yield return WaitFor(() => Game.World.worldId == Vortex.Paths.First(p => p.id == path).world && !Vortex.Inside(Player.transform.position), 8f);
            yield return UntilFree(60f);
            Ok(Game.World.worldId == Vortex.Paths.First(p => p.id == path).world && Game.Playing, $"{label}: in {Game.World.displayName}, playing");
        }

        IEnumerator MinPlayer()
        {
            Head("Fix 10.1, item 1: rule 2's report (the minimum player and the completionist, every world)");
            foreach (var wid in WorldData.StoryOrder)
            {
                var wd = WorldData.Load(wid);
                foreach (var a in wd.areas) { var r = wd.RequirementReport(a.id); Ok(r.ok, r.detail.Split(new[] { "; gates:" }, System.StringSplitOptions.None)[0]); }
            }
            Head("Fix 10.1, item 6 / Round 11: the minimum player from New game to Steampunk's teal shard");
            yield return WaitFor(() => Game.I.State == Game.Mode.Title, 10f);
            if (SaveGame.Exists) { Log("  (a save was there: starting over)"); }
            Game.I.StartGame(true);
            yield return SkipCutscene(); yield return SkipCutscene(); yield return UntilFree();
            Ok(Game.Playing && Game.World.worldId == WorldData.IceAge && SaveGame.Current.Total == 0, "New game: Act I and the arrival (skipped with the pad), playing in the Ice Age with 0 shards");
            Ok(SaveGame.Current.seen.Contains("ActOne") && SaveGame.Current.seen.Contains("IceAgeArrival"), $"the Theater has seen [{string.Join(", ", SaveGame.Current.seen)}]");
            yield return MinimumHere("Ice Age");
            yield return TakeStory("Ice Age"); yield return ThroughGate("Ice Age");
            yield return TakePath("Egypt", "to Egypt");
            yield return MinimumHere("Egypt");
            yield return TakeStory("Egypt"); yield return ThroughGate("Egypt");
            yield return TakePath("Bethlehem", "to Bethlehem");
            yield return MinimumHere("Bethlehem");
            yield return TakeStory("Bethlehem"); yield return ThroughGate("Bethlehem");
            yield return TakePath("Medieval", "to the Medieval Kingdom");
            yield return MinimumHere("Medieval");
            yield return TakeStory("Medieval"); yield return ThroughGate("Medieval");
            Ok(P.Companion, "Muscle Man came along from the stone");
            yield return TakePath("PirateSeas", "to the Pirate Seas");
            yield return MinimumHere("Pirate Seas", VortexAndBack);
            Ok(SaveGame.Current.Total == 125, $"the minimum player wakes the Pirate Seas' necklace at {SaveGame.Current.Total} shards in all (5 x 25)");
            yield return TakeStory("Pirate Seas (the chest)", Pirate.ChestShardId);
            Ok(SaveGame.Current.World(WorldData.Pirate).opened.Contains(Pirate.OverboardFlag), "ShardOverboard: the deep sea is open");
            yield return TakeStory("Pirate Seas (the sea floor)");
            yield return WaitFor(() => Vortex.Inside(Player.transform.position), 6f);
            Ok(SaveGame.Current.World(WorldData.Pirate).storyShard && Vortex.Inside(Player.transform.position), $"the green shard taken, SeaVortex, in the vortex ({SaveGame.Current.StoryShards} story shards)");
            var steam = VortexDoor.All.FirstOrDefault(d => d.info.id == "Steampunk");
            Ok(steam && !steam.Blocked, "the Victorian Steampunk path is open");
            yield return Shot("min_done_vortex");
            // Round 11: on into Victorian Steampunk: the arrival (skipped), the belt run (the bot rides it), the press (skipped), 25 more, the clock
            yield return TakePath("Steampunk", "to Victorian Steampunk");
            yield return WaitFor(() => Steampunk.BeltRunning, 10f);
            Ok(Steampunk.BeltRunning, "on the belt after the arrival");
            yield return BeltRunBot(false); yield return UntilFree(40f);
            Ok(Steampunk.PressGone && Game.Playing, "past the press (MuscleManHero skipped with the pad): the factory is free");
            yield return MinimumHere("Steampunk");
            Ok(SaveGame.Current.Total == 150, $"the minimum player wakes Steampunk's necklace at {SaveGame.Current.Total} shards in all (6 x 25)");
            Health.Refill(); Tp(Steampunk.ClockAt + new Vector3(0, .1f, -3.6f), 0);
            yield return WaitFor(() => Game.I.Busy || Game.InCutscene, 4f); yield return UntilFree(60f);
            yield return WaitFor(() => Vortex.Inside(Player.transform.position), 6f);
            Ok(SaveGame.Current.World(WorldData.Steampunk).storyShard && Vortex.Inside(Player.transform.position), $"the teal shard (ClockShard), in the vortex ({SaveGame.Current.StoryShards} story shards)");
            var tl = VortexDoor.All.FirstOrDefault(d => d.info.id == "Tomorrowland");
            Ok(tl && !tl.Blocked, "the Tomorrowland path is open");
            yield return Shot("min_done_steampunk");
            // Round 12: on into Tomorrowland: the arrival (skipped), 25 more, the necklace, the Skyline Tower's lift, the pink shard, the launch
            yield return TakePath("Tomorrowland", "to Tomorrowland");
            yield return MinimumHere("Tomorrowland");
            Ok(SaveGame.Current.Total == 175, $"the minimum player wakes Tomorrowland's necklace at {SaveGame.Current.Total} shards in all (7 x 25)");
            Health.Refill(); yield return TakeStory("Tomorrowland (the pink shard)");
            Ok(SaveGame.Current.World(WorldData.Tomorrowland).storyShard, "the pink shard taken from the Skyline Tower's roof");
            Tp(Tomorrowland.SocketAt + new Vector3(0, .1f, -2f), 0);
            yield return WaitFor(() => Game.I.Busy || Game.InCutscene, 4f); yield return UntilFree(80f);
            yield return MinAfterLaunch();
            Log($"  Theater after the run: [{string.Join(", ", SaveGame.Current.seen)}]");
        }
        IEnumerator MinAfterLaunch()
        {
            if (!Moon.Real)
            {
                yield return WaitFor(() => Vortex.Inside(Player.transform.position), 8f);
                Ok(SaveGame.Current.World(WorldData.Tomorrowland).opened.Contains(Tomorrowland.LaunchFlag) && Vortex.Inside(Player.transform.position), $"RocketLaunch, in the vortex, the Moon to be continued ({SaveGame.Current.StoryShards} story shards)");
                yield return Shot("min_done_tomorrowland");
                yield break;
            }
            yield return MinOnMoon();
            Log($"  Theater after the run: [{string.Join(", ", SaveGame.Current.seen)}]");
        }

        IEnumerator PauseRow(string label)
        {
            yield return Press(GamepadButton.Start); yield return Wait(.4f);
            var page = Menu.I.Current; int row = page != null ? page.rows.FindIndex(r => r.label == label) : -1;
            Ok(Game.I.State == Game.Mode.Paused && row >= 0 && (page.rows[row].enabled == null || page.rows[row].enabled()), $"the pause menu offers \"{label}\" ({(page != null ? string.Join(" / ", page.rows.Select(r => r.label)) : "no menu")})");
            if (row < 0) yield break;
            while (Menu.I.Current.sel != row) { yield return Press(GamepadButton.DpadDown); yield return Wait(.12f); }
            yield return Shot("min_pause_" + label.Replace(" ", "_"));
            yield return Press(GamepadButton.South); yield return Wait(.3f);
        }

        // mid-Pirate: Pause > Return to the vortex, back by the Pirate Seas path to the same spot; then Pause > Theater
        IEnumerator VortexAndBack()
        {
            Head("Fix 10.1, item 1: Pause > Return to the vortex from mid-Pirate, and back");
            yield return TpSettle(new Vector3(2.5f, 3.6f, 9f), 0); yield return Wait(.5f);
            var spot = Player.transform.position; int total = SaveGame.Current.Total, here = ThisWorld; int r0 = Game.I.VortexReturns, c0 = Game.I.VortexComebacks;
            yield return PauseRow("Return to the vortex");
            yield return WaitFor(() => Game.I.VortexReturns > r0 && !Game.I.Busy, 5f); yield return Wait(.5f);
            Ok(Vortex.Inside(Player.transform.position) && SaveGame.Current.Total == total, $"in the vortex ({SaveGame.Current.Total} shards kept, the spot {Fmt(SaveGame.Current.returnSpot)} saved)");
            yield return Shot("min_vortex_from_pirate");
            var ia = VortexDoor.All.FirstOrDefault(d => d.info.id == "IceAge");
            Ok(ia && !ia.Blocked, "earlier worlds stay open (the Ice Age path)");
            yield return TakePath("PirateSeas", "back to the Pirate Seas");
            float d = Vector3.Distance(Player.transform.position, spot);
            Ok(Game.I.VortexComebacks > c0 && d < 1.5f && ThisWorld == here, $"back where they left ({d:0.0} m from the spot), {ThisWorld} from here kept");
            yield return Shot("min_back_on_deck");

            Head("Fix 10.1, item 5: Pause > Theater");
            spot = Player.transform.position; int t0 = Game.I.TheaterPlays;
            yield return PauseRow("Theater");
            Ok(Menu.I.Current.title == "Theater", $"the Theater lists: {string.Join(" / ", Menu.I.Current.rows.Select(r => r.label))}");
            yield return Shot("min_theater_worlds");
            yield return Press(GamepadButton.South); yield return Wait(.3f);   // the Ice Age
            Ok(Menu.I.Current.title == "The Ice Age", $"its scenes: {string.Join(" / ", Menu.I.Current.rows.Select(r => r.label))}");
            yield return Shot("min_theater_iceage");
            yield return Press(GamepadButton.DpadDown); yield return Wait(.12f);   // the arrival
            yield return Press(GamepadButton.South);
            yield return WaitFor(() => Game.InCutscene, 8f);
            Ok(Game.I.TheaterPlays > t0 && Cutscenes.Playing == "IceAgeArrival", $"it plays {Cutscenes.Playing ?? "nothing"} (world {Game.World.worldId})");
            yield return Wait(3f); yield return Shot("min_theater_playing");
            Log($"  (music in the Theater: {Music.Track})");
            yield return Hold(GamepadButton.South, 1.3f);
            yield return WaitFor(() => !Game.InCutscene && Game.Playing && Game.InPirate, 15f); yield return Wait(1f);
            d = Vector3.Distance(Player.transform.position, spot);
            Ok(Game.InPirate && Game.Playing && d < 1.5f && ThisWorld == here, $"skipped; back in the Pirate Seas where they stood ({d:0.0} m), {SaveGame.Current.Total} shards");
        }

        // ---------------------------------------------------------------- the T-pose sweep
        // a skinned mesh in its bind pose: every bone where the mesh was bound (rotation within 2 deg)
        static float PoseOff(SkinnedMeshRenderer s)
        {
            var bones = s.bones; var bp = s.sharedMesh ? s.sharedMesh.bindposes : null; if (bp == null || bones == null || bones.Length != bp.Length) return 999f;
            float max = 0; var inv = s.transform.worldToLocalMatrix;
            for (int i = 0; i < bones.Length; i++)
            {
                if (!bones[i]) continue;
                var cur = (inv * bones[i].localToWorldMatrix).rotation; var bind = bp[i].inverse.rotation;
                max = Mathf.Max(max, Quaternion.Angle(cur, bind));
            }
            return max;
        }
        static string Who(Transform t) { var n = t.name; var p = t.parent; for (int i = 0; i < 4 && p; i++, p = p.parent) n = p.name + "/" + n; return n; }

        IEnumerator SweepNow(Transform under, string where, Dictionary<string, string> rest, Dictionary<string, int> seen)
        {
            var list = (under ? under.GetComponentsInChildren<SkinnedMeshRenderer>() : Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))
                .Where(s => s && s.enabled && s.gameObject.activeInHierarchy && s.bones.Length > 8 && s.transform.lossyScale.x > .05f).ToList();
            var first = list.ToDictionary(s => s, PoseOff);
            yield return Wait(.35f);
            foreach (var s in list)
            {
                if (!s) continue;
                string key = Who(s.transform.parent ? s.transform.parent : s.transform);
                float a = first[s], b = PoseOff(s);
                seen[key] = seen.TryGetValue(key, out int n) ? n + 1 : 1;
                if (a < 2f && b < 2f) rest[key] = $"{where} (bones within {Mathf.Max(a, b):0.0} deg of the bind pose)";
            }
        }

        IEnumerator TPoseSweep()
        {
            Head("Fix 10.1, item 2: the T-pose sweep, every world (play) and every cutscene");
            yield return EnsurePlaying(false);
            if (!P.Companion) P.AddCompanion(Player.transform.position + Vector3.back * 3f, Quaternion.identity);
            var rest = new Dictionary<string, string>(); var seen = new Dictionary<string, int>();
            foreach (var (wid, cp) in new[] { (WorldData.IceAge, "IA-vortex"), (WorldData.Egypt, Egypt.VortexCp), (WorldData.Bethlehem, Bethlehem.VortexCp), (WorldData.Medieval, Medieval.VortexCp), (WorldData.Pirate, Pirate.VortexCp), (WorldData.Steampunk, Steampunk.FactoryCp), (WorldData.Tomorrowland, Tomorrowland.Start), (WorldData.Moon, Moon.Start) })
            {
                SaveGame.Current.checkpoint = cp;
                if (Game.World.worldId != wid) yield return ToWorld(wid, cp);
                if (!P.Companion) P.AddCompanion(Player.transform.position + Vector3.back * 3f, Quaternion.identity);
                yield return Wait(2f);
                int before = seen.Count;
                yield return SweepNow(null, "play in " + wid, rest, seen);
                Log($"  {wid}: {seen.Count - before} animated characters sampled");
                if (wid == WorldData.Pirate) yield return DeckhandsAnimate();   // Round 11: while the Pirate Seas are built (Steampunk comes after)
            }
            yield return TPoseCutscenes(rest, seen);
        }
        IEnumerator DeckhandsAnimate()
        {
            // the deckhands by name: they were the T-pose (Dan, 0.10.0)
            var deck = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).Where(e => e.name.StartsWith("Deckhand")).ToList();
            Ok(deck.Count > 0 && deck.All(e => e.Anim && e.Anim.ClipCount > 10 && e.Anim.Humanoid), $"{deck.Count} deckhands animate (humanoid, {(deck.Count > 0 && deck[0].Anim ? deck[0].Anim.ClipCount : 0)} clips; state {(deck.Count > 0 && deck[0].Anim ? deck[0].Anim.State : "-")})");
            if (deck.Count > 0)
            {
                var e = deck[0];
                Tp(e.transform.position + e.transform.forward * 3.2f + Vector3.up * .2f, e.transform.eulerAngles.y + 180f); yield return Wait(.3f);
                CameraRig.I.SnapBehind(); yield return Wait(1.0f); yield return Shot("tpose_deckhand_" + (e.Anim ? e.Anim.State : "x"));
            }
        }
        IEnumerator TPoseCutscenes(Dictionary<string, string> rest, Dictionary<string, int> seen)
        {
            foreach (var name in Game.TheaterList.SelectMany(x => x.names))
            {
                if (!Cutscenes.Load(name)) { Log($"  (no cutscene {name})"); continue; }
                Game.HomeAgainNoBackwards = name == "HomeAgain";   // Round 14: the waking scene itself (Act I backwards is ActOne's frames)
                Game.I.StartCoroutine(Game.I.Film(name));
                yield return WaitFor(() => Cutscenes.Director != null && Cutscenes.Playing == name, 20f);
                var dir = Cutscenes.Director; if (!dir) { Fail($"{name} did not start"); continue; }
                foreach (var f in new[] { .12f, .33f, .55f, .75f, .93f })
                {
                    if (!Cutscenes.Director) break;
                    dir.time = f * dir.duration; dir.Evaluate(); yield return null; yield return null;
                    yield return SweepNow(dir.transform, $"{name} at {f * dir.duration:0.0} s", rest, seen);
                }
                if (Cutscenes.Director) dir.time = dir.duration - .01;
                yield return WaitFor(() => Cutscenes.Playing == null, 5f);
                Log($"  {name}: sampled");
            }
            SaveGame.Disabled = false; Cutscenes.AllowSkip = true; Game.I.SetBusy(false);
            Log($"  {seen.Count} characters sampled in all");
            foreach (var kv in rest) Log($"  REST POSE: {kv.Key}: {kv.Value}");
            Ok(rest.Count == 0, $"no character in its rest pose ({rest.Count} found)");
        }

        // ---------------------------------------------------------------- the sails
        IEnumerator SailsView()
        {
            Head("Fix 10.1, item 4: sails, nets, ropes and masts fade between the camera and the player");
            yield return EnsurePlaying(false);
            if (Game.World.worldId != WorldData.Pirate) { if (!SaveGame.Current.World(WorldData.Pirate).opened.Contains("PS-arrived")) SaveGame.Current.World(WorldData.Pirate).opened.Add("PS-arrived"); yield return ToWorld(WorldData.Pirate, Pirate.Start); }
            yield return SwitchTo(0);
            Ok(SeeThrough.Registered > 0, $"{SeeThrough.Registered} see-through parts on the ship");
            int f0 = SeeThrough.FadesEver; float minCam = 99f; var pullers = new Dictionary<string, int>(); int thinPulls = 0; int framesBlocked = 0, frames = 0;
            var walk = new[] { (new Vector3(0, 3.6f, -13f), new Vector3(0, 3.6f, 16f), "main deck aft to fore"), (new Vector3(-3f, 3.6f, 16f), new Vector3(-3f, 3.6f, -12f), "main deck fore to aft, port side"), (new Vector3(2.5f, 3.6f, -10f), new Vector3(2.5f, 3.6f, 14f), "starboard side") };
            int i = 0;
            foreach (var (a, b, label) in walk)
            {
                yield return TpSettle(a, Quaternion.LookRotation(b - a).eulerAngles.y);
                CameraRig.I.SnapBehind(); yield return Wait(.4f);
                float end = Time.time + 14f; bool shot = false;
                while (Time.time < end)
                {
                    var dd = b - Player.transform.position; dd.y = 0; if (dd.magnitude < .8f) break;
                    Stick(StickFor(dd.normalized) * .6f);
                    frames++; float cd = CameraRig.I.Distance; minCam = Mathf.Min(minCam, cd);
                    if (cd < 3f)   // pulled in: by what? (a wall is fine; a sail, rope or mast is not)
                    {
                        var c0 = Player.Motor.Center; var back = -CameraRig.I.Cam.transform.forward;
                        if (Physics.Raycast(c0, back, out var hit, 6.5f, ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << Hero.EnemyLayer) | (1 << 4) | (1 << 2) | (1 << 5)), QueryTriggerInteraction.Ignore))
                        {
                            string hn = hit.collider.name; pullers[hn] = pullers.TryGetValue(hn, out int pc) ? pc + 1 : 1;
                            string part = hn.Contains("__") ? hn.Substring(0, hn.IndexOf("__")) : hn;
                            if (hit.collider.gameObject.layer == SeeThrough.SolidLayer || SeeThrough.PirateParts.Contains(part)) thinPulls++;
                        }
                    }
                    if (SeeThrough.FadedNow > 0) { framesBlocked++; if (!shot) { shot = true; Stick(Vector2.zero); yield return Wait(.25f); yield return Shot($"sails_{i}_{label.Replace(" ", "_").Replace(",", "")}"); Log($"  ({label}: faded now: {SeeThrough.FadedNames})"); } }
                    yield return null;
                }
                Stick(Vector2.zero); i++;
            }
            // up in the rigging: the main top, the crow's nest, the fore top, looking along the ship
            foreach (var (at, yaw, n) in new[] { (new Vector3(-1.1f, 13.5f, 1.2f), 0f, "main_top"), (new Vector3(-.3f, 21.2f, .35f), 180f, "crows_nest"), (new Vector3(.5f, 11.95f, 12.9f), 180f, "fore_top"), (new Vector3(0, 6.1f, -16f), 0f, "quarterdeck") })
            {
                yield return TpSettle(at, yaw); CameraRig.I.SnapBehind(); yield return Wait(.8f);
                Log($"  {n}: camera {CameraRig.I.Distance:0.0} m back, faded: {(SeeThrough.FadedNow > 0 ? SeeThrough.FadedNames : "none")}");
                yield return Shot("sails_" + n);
            }
            Ok(SeeThrough.FadesEver > f0, $"parts faded on the walk ({SeeThrough.FadesEver - f0} fades; {framesBlocked} of {frames} walking frames had something faded)");
            Ok(thinPulls == 0, $"no sail, rope or mast pulled the camera in (nearest {minCam:0.0} m; what did: {(pullers.Count == 0 ? "nothing" : string.Join(", ", pullers.OrderByDescending(x => x.Value).Take(6).Select(x => $"{x.Key} x{x.Value}")))})");
        }

        // ---------------------------------------------------------------- the Theater on the title screen
        // Round 11, item 0: several scenes in a row. The save's world is made "ready but asleep" (the necklace could wake: what Dan's
        // save had when the Theater stopped answering after one scene); three scenes from the title, then Continue, then two from pause.
        IEnumerator TheaterPick(int worldRow, int sceneRow, string label)
        {
            var page = Menu.I.Current;
            while (Menu.I.Current.sel != worldRow) { yield return Press(GamepadButton.DpadDown); yield return Wait(.12f); }
            string wname = page.rows[worldRow].label;
            yield return Press(GamepadButton.South); yield return Wait(.3f);
            var sp = Menu.I.Current; int n = sp.rows.Count - 1;   // the last row is Back
            int row = Mathf.Min(sceneRow, n - 1);
            while (Menu.I.Current.sel != row) { yield return Press(GamepadButton.DpadDown); yield return Wait(.12f); }
            string want = sp.rows[row].label;
            int t0 = Game.I.TheaterPlays;
            yield return Press(GamepadButton.South);
            yield return WaitFor(() => Game.InCutscene, 10f);
            string playing = Cutscenes.Playing;
            Ok(Game.I.TheaterPlays > t0 && playing != null, $"{label}: {wname} > {want}: it plays ({playing ?? "nothing"}; busy before the pick: {(Game.I.TheaterPlays > t0 ? "no" : "?")})");
            yield return Wait(2.5f); yield return Shot("theater_" + label.Replace(" ", "_"));
            yield return Hold(GamepadButton.South, 1.3f);
        }

        IEnumerator TheaterTitle()
        {
            Head("Fix 10.1 / Round 11 item 0: the Theater, several scenes in a row");
            yield return WaitFor(() => Game.I.State == Game.Mode.Title, 10f); yield return Wait(1f);
            var args = System.Environment.GetCommandLineArgs(); int ai = System.Array.IndexOf(args, "-oldSave");
            if (ai >= 0 && ai + 1 < args.Length && System.IO.File.Exists(args[ai + 1]))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(SaveGame.FilePath));
                System.IO.File.Copy(args[ai + 1], SaveGame.FilePath, true); SaveGame.Load(); Game.I.ToTitle(); yield return Wait(.5f);
                var sv = SaveGame.Current;
                Ok(sv.seen.Contains("ActOne") && sv.seen.Contains("EgyptArrival") && sv.seen.Contains("VortexOpensEgypt") && !sv.seen.Contains("BethlehemArrival"), $"an old save (0.6.0, after Egypt) fills in what it has seen: [{string.Join(", ", sv.seen)}]");
                // the save's world: its necklace ready to wake but asleep (the state that stuck the Theater)
                var wp = sv.World(sv.currentWorld); wp.necklaceAwake = false; wp.storyShard = false; SaveGame.Save("check: ready but asleep");
                var wd = WorldData.Load(sv.currentWorld);
                Log($"  the save's world {sv.currentWorld}: necklace asleep with {sv.Total} in all and {wp.shards.Count} from here (threshold {wd.areas[0].requirement} / {wd.areas[0].fromHere})");
            }
            var page = Menu.I.Current; int row = page.rows.FindIndex(r => r.label == "Theater");
            Ok(row >= 0, $"the title offers: {string.Join(" / ", page.rows.Select(r => r.label + (r.enabled != null && !r.enabled() ? " (grey)" : "")))}; seen {SaveGame.Current.seen.Count}");
            if (row < 0 || SaveGame.Current.seen.Count == 0) yield break;
            while (Menu.I.Current.sel != row) { yield return Press(GamepadButton.DpadDown); yield return Wait(.12f); }
            yield return Press(GamepadButton.South); yield return Wait(.3f);
            Log($"  Theater: {string.Join(" / ", Menu.I.Current.rows.Select(r => r.label))}");
            yield return Shot("theater_title");
            int worlds = Menu.I.Current.rows.Count - 1;
            // three in a row from the title, each from a different place in the list
            var picks = new[] { (0, 1, "title 1"), (Mathf.Min(1, worlds - 1), 0, "title 2"), (0, 2, "title 3") };
            foreach (var (w, sc, label) in picks)
            {
                yield return TheaterPick(w, sc, label);
                yield return WaitFor(() => Game.I.State == Game.Mode.Title && !Game.InCutscene && Menu.I.Current != null && Menu.I.Current.title == "Theater", 20f); yield return Wait(.6f);
                Ok(Game.I.State == Game.Mode.Title && Menu.I.Current.title == "Theater" && !Game.I.Busy, $"{label}: skipped, back on the title's Theater page (busy {Game.I.Busy})");
            }
            // then into the game, and two in a row from the pause menu
            yield return Press(GamepadButton.East); yield return Wait(.3f);
            page = Menu.I.Current; row = page.rows.FindIndex(r => r.label == "Continue");
            while (Menu.I.Current.sel != row) { yield return Press(GamepadButton.DpadUp); yield return Wait(.12f); }
            yield return Press(GamepadButton.South);
            yield return WaitFor(() => Game.Playing, 15f); yield return Wait(1f);
            int wakes = 0; float end = Time.time + 40f;
            while (Time.time < end && (Game.I.Busy || Game.InCutscene)) { if (Game.InCutscene) { wakes++; Log($"  (on Continue: {Cutscenes.Playing})"); yield return Wait(.5f); yield return Hold(GamepadButton.South, 1.3f); yield return WaitFor(() => !Game.InCutscene, 4f); } yield return null; }
            yield return Wait(1f);
            Ok(wakes <= 1 && Game.Playing && !Game.I.Busy, $"Continue: in {Game.World.worldId}, {wakes} story scene(s) on the way in (the wake once, never twice)");
            for (int k = 0; k < 2; k++)
            {
                var spot = Player.transform.position;
                yield return PauseRow("Theater");
                Ok(Menu.I.Current.title == "Theater", $"pause {k + 1}: the Theater lists {string.Join(" / ", Menu.I.Current.rows.Select(r => r.label))}");
                yield return TheaterPick(0, k == 0 ? 1 : 3, "pause " + (k + 1));
                yield return WaitFor(() => !Game.InCutscene && Game.Playing && !Game.I.Busy, 20f); yield return Wait(1f);
                float d = Vector3.Distance(Player.transform.position, spot);
                Ok(Game.Playing && !Game.I.Busy && d < 1.5f, $"pause {k + 1}: back in play where they stood ({d:0.0} m)");
            }
            Ok(Game.I.TheaterPlays >= 5, $"{Game.I.TheaterPlays} Theater plays in one run");
        }
    }
}
