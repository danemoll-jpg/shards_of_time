using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace ShardsOfTime
{
    // Round 5: Dan's music in the right places, the props upright, the bigger valley.
    public sealed partial class ExeCheck
    {
        // the track this place should play is the one playing (Dan's file, loaded, its source running; the check run is muted)
        void MusicCheck(string where, string track)
        {
            var srcs = Game.I.GetComponents<AudioSource>();
            var playing = srcs.FirstOrDefault(x => x.isPlaying && x.clip && x.clip.name == track);
            Ok(Music.Track == track && playing, $"music in {where}: {track}" + (playing ? $" ({playing.clip.length:0.0} s, at {playing.time:0.0} s)" : $" NOT playing (track {Music.Track ?? "none"}; sources: {string.Join(", ", srcs.Where(x => x.isPlaying && x.clip).Select(x => x.clip.name))})"));
        }

        // ---------------------------------------------------------------- item 3: a look round the whole valley (player's view) + the props audit
        static readonly (string n, float x, float z, float yaw)[] TourStops =
        {
            ("start", -5, 6, 0), ("pond", 6, 38, 60), ("snowfield_west", -30, 60, 80), ("ridge_foot", 30, 100, 10), ("ridge_shelf", 57, 180, 200),
            ("valley_middle", 0, 180, 0), ("bridge_south", -10, 228, 0), ("pillars", 20, 236, 0), ("north_bank", 0, 275, 180),
            ("cave_mouth", -12, 330, 270), ("cave_hall", -30, 330, 0), ("west_side", -45, 420, 45), ("vents", 34, 400, 30),
            ("terrace_top", 60, 420, 250), ("stones", 0, 456, 0), ("stones_back", 0, 500, 180), ("grotto", -36, 484, 270), ("east_wall", 50, 300, 300),
            ("lake_pass", -70, 115, 270), ("lake", -100, 120, 250), ("lake_island", -133, 186, 180), ("glacier_gap", 70, 295, 90), ("glacier", 104, 290, 60),
            ("hot_spring_wall", 50, 464, 90),
        };

        IEnumerator Tour()
        {
            yield return EnsurePlaying(false);
            Head("Item 3: a look round the valley from the player's view");
            foreach (var (n, x, z, yaw) in TourStops)
            {
                yield return TpSettle(IceAge.OnGround(x, z, n.StartsWith("cave") ? 5f : 80f) + Vector3.up * .1f, yaw);
                yield return Wait(.8f);
                yield return Shot($"t_{n}");
            }
            yield return PropAudit();
        }

        // every Blender prop in the valley: upright (its up = the world's up), its bottom on the ground (not floating, not sunk)
        IEnumerator PropAudit()
        {
            Head("Items 2-3: every Ice Age prop upright and on the ground");
            yield return null;
            var names = new[] { "Pine", "Stone", "Rock1", "Rock2", "Rock3", "Bones", "IceCrystal" };
            var props = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => names.Contains(t.name) && t.parent && t.parent.name == "World Ice Age").ToList();
            int bad = 0; var kinds = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var t in props)
            {
                kinds[t.name] = kinds.TryGetValue(t.name, out var c) ? c + 1 : 1;
                var rends = t.GetComponentsInChildren<Renderer>(); if (rends.Length == 0) continue;
                var b = rends[0].bounds; foreach (var r in rends) b.Encapsulate(r.bounds);
                var own = t.GetComponentsInChildren<Collider>();
                float G(float x, float z)
                {
                    var hits = Physics.RaycastAll(new Vector3(x, b.max.y + .5f, z), Vector3.down, 200f, ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << Hero.EnemyLayer) | (1 << 4) | (1 << 2)), QueryTriggerInteraction.Ignore);
                    var h = hits.Where(hh => !own.Contains(hh.collider)).OrderBy(hh => hh.distance).FirstOrDefault();
                    return h.collider ? h.point.y : float.NaN;
                }
                bool tall = t.name == "Pine" || t.name == "Stone";
                bool upright = !tall || b.size.y > Mathf.Max(b.size.x, b.size.z);
                float gc = G(b.center.x, b.center.z);
                float lift = b.min.y - gc;                       // > 0: floating
                float sunk = gc - b.min.y;                       // how deep its bottom is under the ground at its centre
                float maxSunk = t.name == "Bones" ? .9f : t.name == "IceCrystal" ? .3f * b.size.y : t.name.StartsWith("Rock") ? .6f * b.size.y : .5f;   // crystal clusters lean: their lowest tip goes in
                bool ok = upright && !float.IsNaN(gc) && lift < .12f && sunk < maxSunk;
                if (!ok) { bad++; Log($"    BAD {t.name} at {Fmt(t.position)}: size {Fmt(b.size)}, {(upright ? "upright" : "LYING DOWN")}, bottom {(lift >= 0 ? $"{lift:0.00} m over" : $"{sunk:0.00} m under")} the ground at its centre"); }
            }
            Ok(bad == 0 && props.Count > 100, $"{props.Count} props ({string.Join(", ", kinds.Select(k => $"{k.Value} {k.Key}"))}): {(bad == 0 ? "all upright and on the ground" : bad + " wrong (listed above)")}");
        }
    
        // ---------------------------------------------------------------- items 5-7: the new zones in the play-through (the pad only)
        float Flat(Vector3 a) { var d = a - Player.transform.position; d.y = 0; return d.magnitude; }
        float zoneStart;

        IEnumerator FrozenLake()
        {
            Head("Round 5, item 5: the frozen lake (the west basin)");
            zoneStart = Time.time;
            yield return GoF(XZ(-40, 115)); yield return GoF(XZ(-88, 115)); yield return Go(XZ(-100, 100), .8f, 12f);
            Ok(SlickIce.At(Player.transform.position + Vector3.up * .2f), $"on the lake's slick ice at {Fmt(Player.transform.position)}");
            yield return Shot("50_frozen_lake");
            yield return Go(XZ(-118, 68), .8f, 20f); yield return Go(XZ(-133, 66), .6f, 15f);   // the lane's start, lined up
            foreach (var z in new[] { 100f, 118f, 135f, 150f, 165f }) yield return Go(XZ(-133, z), .9f, 10f);   // steering at each shard, as a player would
            yield return Go(XZ(-133, 186), .8f, 25f);   // the skating lane carries the party north, up the ramp onto the island
            yield return WaitFor(() => Has("L03"), 2f);
            Ok(Has("L01") && Has("L02"), $"L01, L02 on the skating lane (speed {new Vector3(M.Velocity.x, 0, M.Velocity.z).magnitude:0.0} m/s at the end)");
            Ok(Has("L03"), $"L03 on the ice island at the lane's end (y {Player.transform.position.y:0.0})");
            yield return Shot("51_ice_island");
            yield return Go(XZ(-124, 184), .8f, 8f); yield return Shard("L04", XZ(-101, 172), .8f);
            // the boulder on the west shore
            var lb = Object.FindObjectsByType<Boulder>().OrderBy(b => (b.transform.position - new Vector3(-148, 0, 124)).sqrMagnitude).FirstOrDefault();
            int touches = lb ? lb.Touches : 0;
            yield return Go(XZ(-100, 202), .8f, 15f); yield return Go(XZ(-150, 202), .8f, 20f); yield return Go(XZ(-146, 128), .6f, 20f); yield return Go(XZ(-144, 124), .5f, 6f); Stick(StickFor(Vector3.left)); yield return Wait(.7f); Stick(Vector2.zero);
            Ok(lb && lb.Touches > touches && !Has("L08"), "the lake's boulder needs Muscle Man (visible, closed)");
            yield return Go(XZ(-146, 100), .8f, 15f); yield return Go(XZ(-112, 80), .8f, 20f);
            // under the ice as Kira (she never runs out of air): in at the near hole, along the bottom, back out
            yield return SwitchTo(2);
            yield return Go(XZ(-112, 85), .6f, 25f); yield return Go(XZ(-118, 85), .4f, 6f, .6f);
            yield return WaitFor(() => M.Mode == MoveMode.Swim, 4f);
            Btn(GamepadButton.East, true);
            yield return Go(XZ(-118, 96), .5f, 12f); yield return WaitFor(() => Has("L05"), 5f);
            Ok(Has("L05"), $"L05 under the ice (Kira dived in at the near hole, y {Player.transform.position.y:0.0})");
            yield return Shot("52_under_the_ice");
            yield return Go(XZ(-118, 85), .5f, 12f);
            Btn(GamepadButton.East, false); yield return HoleOut(XZ(-118, 85), Vector3.right);
            Ok(M.Mode != MoveMode.Swim && Player.transform.position.y > -.6f, $"out of the hole onto the ice ({Fmt(Player.transform.position)})");
            // the deep part: diving down the deep hole, the water pushes her back up (deep dive: later)
            yield return Go(XZ(-112, 133), .6f, 20f); yield return Go(XZ(-118, 133), .4f, 6f, .6f);
            yield return WaitFor(() => M.Mode == MoveMode.Swim, 4f);
            Btn(GamepadButton.East, true); float low = 0; float until = Time.time + 6f;
            while (Time.time < until) { low = Mathf.Min(low, Player.transform.position.y); yield return null; }
            Btn(GamepadButton.East, false);
            Ok(low > -11f && !Has("L06") && Hud.LastMessage.Contains("Too deep"), $"the deep part pushes divers back up (lowest y {low:0.0}; L06 at -27 stays for the deep dive): \"{Hud.LastMessage}\"");
            yield return HoleOut(XZ(-118, 133), Vector3.right);
            yield return SwitchTo(0);
            yield return Go(XZ(-120, 115), .8f, 20f); yield return Go(XZ(-90, 115), .8f, 15f); yield return GoF(XZ(-50, 115));
            Log($"  the lake took the bot {(Time.time - zoneStart) / 60f:0.0} min; {ShardsHere} shards");
        }

        IEnumerator GlacierClimb()
        {
            Head("Round 5, item 5: the glacier climb (east)");
            zoneStart = Time.time;
            yield return GoF(XZ(50, 295)); yield return GoF(XZ(96, 290)); yield return Go(XZ(114, 274), .5f, 15f);
            yield return Shot("53_glacier");
            // the chimney: too high without a wall jump
            yield return Go(XZ(108, 301), .5f, 10f); yield return Go(XZ(112.8f, 301), .3f, 5f, .5f);
            yield return Hold(GamepadButton.South, .4f); yield return Wait(.8f);
            Ok(!Has("G06") && Player.transform.position.y < 3f, "the chimney's shard is out of reach (Abi's wall jump, later)");
            yield return Go(XZ(107, 301), .5f, 8f); yield return Go(XZ(108, 274), .5f, 15f); yield return Go(XZ(114, 274), .5f, 8f);
            for (int i = 0; i < 19; i++)
            {
                int row = i / 6, j = i % 6;
                float lx0 = i < 18 ? 118 + row * 8 : 138, lz0 = i < 18 ? (row % 2 == 0 ? 272 + j * 4 : 292 - j * 4) : 296;
                float lx1 = i < 18 ? lx0 + 8 : 158, lz1 = i < 18 ? lz0 + 4 : 316;
                var next = new Vector3(Mathf.Clamp((lx0 + lx1) / 2, lx0 + 1, lx1 - 1), 0, i < 18 ? lz0 + 2 : 299);
                float top = IceAge.LedgeStep * (i + 1);
                // jump at the ledge's near edge (less than 0.9 m from it)
                float Edge() { var p = Player.transform.position; float dx = Mathf.Max(lx0 - p.x, 0, p.x - lx1), dz = Mathf.Max(lz0 - p.z, 0, p.z - lz1); return Mathf.Sqrt(dx * dx + dz * dz); }
                for (int k = 0; k < 3 && Player.transform.position.y < top - .2f; k++)
                    yield return JumpTo(next, () => Edge() < .9f, .7f, .4f);
                if (Player.transform.position.y < top - .2f) { Fail($"glacier ledge {i + 1}: stuck at {Fmt(Player.transform.position)}"); break; }
                if (i == 3) yield return Shard("G01", XZ(122, 286), .6f);
                if (i == 8) yield return Shard("G02", XZ(130, 286), .6f);
                if (i == 13) yield return Shard("G03", XZ(138, 278), .6f);
            }
            yield return Shard("G04", XZ(148, 306), .6f); yield return Shard("G05", XZ(155, 312), .6f);
            Ok(Player.transform.position.y > 24f, $"on the glacier's summit (y {Player.transform.position.y:0.0})");
            yield return TurnView(XZ(0, 300)); yield return Shot("54_glacier_summit_view");
            yield return Go(XZ(154, 316.5f), .4f, 5f, .5f); yield return Hold(GamepadButton.South, .4f); yield return Wait(.8f);
            Ok(!Has("G08"), "G08 over the summit is out of a single jump's reach (Abi's double jump, later)");
            yield return Go(XZ(140, 306), .6f, 8f); yield return Go(XZ(134, 306), .6f, 6f);   // off the west edge, down to the floor
            yield return WaitFor(() => M.Grounded && Player.transform.position.y < 2f, 6f);
            yield return Go(XZ(100, 296), .8f, 20f); yield return GoF(XZ(60, 296));
            Log($"  the glacier took the bot {(Time.time - zoneStart) / 60f:0.0} min; {ShardsHere} shards; hearts {Health.Hearts}/{Health.Max}");
        }

        // up to the surface of an ice hole, then swim at its edge and jump out onto the ice
        IEnumerator HoleOut(Vector3 hole, Vector3 dir)
        {
            for (int k = 0; k < 4 && M.Mode == MoveMode.Swim; k++)
            {
                yield return Go(hole, .4f, 4f, .5f);
                Btn(GamepadButton.South, true); yield return WaitFor(() => Player.transform.position.y > -1.3f, 8f); Btn(GamepadButton.South, false);
                Stick(StickFor(dir) * .6f); yield return Wait(.5f);
                yield return Press(GamepadButton.South); yield return Wait(.6f); Stick(StickFor(dir)); yield return Wait(.5f); Stick(Vector2.zero);
                yield return WaitFor(() => M.Grounded, 2f);
            }
            yield return Go(hole + dir * 6f, .6f, 6f);
        }

        // the new zones alone (quick iteration; teleports only to set up each start, the pad does the rest)
        IEnumerator Zones()
        {
            yield return EnsurePlaying(false);
            yield return TpSettle(IceAge.OnGround(-40, 115) + Vector3.up * .1f, 270); yield return FrozenLake();
            yield return TpSettle(IceAge.OnGround(40, 295) + Vector3.up * .1f, 90); yield return GlacierClimb();
            yield return TpSettle(IceAge.OnGround(40, 460) + Vector3.up * .1f, 90); yield return HotSpringWall();
            Log("  " + TestRun.Report());
        }

        // Round 5, item 8: the Violinist's model and clips (teleport only to set up; Dan walks up with the pad)
        IEnumerator ViolinistCheck()
        {
            yield return EnsurePlaying(false);
            Head("Round 5, item 8: the Violinist (Meshy model, Mixamo clips)");
            var v = Violinist.I;
            Log($"  (violinists: {string.Join(", ", Object.FindObjectsByType<Violinist>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(x => $"{x.GetHashCode()} active {x.isActiveAndEnabled} model {x.HasModel}"))}; I = {(v ? v.GetHashCode() : 0)})");
            Ok(v && v.HasModel, $"the Violinist is his model ({(v ? Violinist.Model : "missing")}), the placeholder hidden");
            yield return SwitchTo(0);
            yield return TpSettle(IceAge.OnGround(IceAge.ViolinistAt.x + 5, IceAge.ViolinistAt.z - 5) + Vector3.up * .1f, 315);
            yield return Wait(1f);
            Ok(v.ClipNow == "idle", $"idle by the campfire (clip {v.ClipNow})"); yield return Shot("60_violinist_idle");
            yield return Go(IceAge.ViolinistAt + new Vector3(1.4f, 0, -1.4f), .5f, 15f, .6f, () => Cutscenes.Playing != null);
            yield return WaitFor(() => Cutscenes.Playing == "ViolinistTeaches", 15f);
            yield return Wait(1.2f);
            Log($"  (playing {v.Playing}, cutscene {Cutscenes.Playing}, model {v.HasModel})");
            Ok(v.ClipNow == "play", $"he plays the fiddle (clip {v.ClipNow})"); yield return Shot("61_violinist_plays");
            yield return CloseUp(v.transform, "62_violinist_fiddle_closeup");
            yield return WaitFor(() => !Game.InCutscene, 10f); yield return Wait(.5f);
            Ok(v.ClipNow == "bow" || v.ClipNow == "nod", $"after the tune: a bow, then a nod (clip {v.ClipNow})"); yield return Shot("63_violinist_bows");
            yield return Wait(1.6f); Log($"  then: {v.ClipNow}"); yield return Shot("64_violinist_nods");
            yield return WaitFor(() => !Game.I.Busy, 6f);
            Ok(SaveGame.Current.Has(Abilities.GroundPound), "Dan learned the ground pound");
            Log("  " + TestRun.Report());
        }

        // a close look at a figure from in front (its own camera, off-screen)
        IEnumerator CloseUp(Transform t, string name)
        {
            yield return new WaitForEndOfFrame();
            var cam = CameraRig.I.Cam; var pos = cam.transform.position; var rot = cam.transform.rotation; var brain = cam.GetComponent<Unity.Cinemachine.CinemachineBrain>();
            if (brain) brain.enabled = false;
            cam.transform.position = t.position + t.forward * 1.6f + Vector3.up * 1.6f; cam.transform.LookAt(t.position + Vector3.up * 1.35f);
            TestRun.SaveView(cam, System.IO.Path.Combine(dir, name + ".png")); Log($"  screenshot {name}.png");
            cam.transform.SetPositionAndRotation(pos, rot); if (brain) brain.enabled = true;
        }

        IEnumerator TurnView(Vector3 at)
        {
            var d = at - Player.transform.position; d.y = 0;
            Stick(StickFor(d.normalized) * .2f); yield return Wait(.4f); Stick(Vector2.zero); yield return Wait(1.6f);
        }

        IEnumerator HotSpringWall()
        {
            Head("Round 5, item 6: the hot-spring grotto (cracked rock wall)");
            yield return GoF(XZ(50, 464)); yield return Go(XZ(57, 464), .4f, 8f, .5f);
            yield return Wait(.3f); yield return Shot("55_grotto_wall");
            yield return Press(GamepadButton.West); yield return Wait(.4f);
            Ok(!SaveGame.Current.World(Game.World.worldId).opened.Contains("IA-rock-hotspring") && Hud.LastMessage.Contains("punch"),
               $"the cracked rock wall holds (Dan's charged punch, later): \"{Hud.LastMessage}\"");
            var gates = Game.World.gates;
            Ok(gates.Count >= 10 && gates.Select(g => g.needs).Distinct().Count() >= 5, $"gates as data: {gates.Count} ({string.Join(", ", gates.GroupBy(g => g.kind).Select(g => $"{g.Count()} {g.Key}"))})");
            Ok(Game.World.shards.Count == 60 && Game.World.areas[0].requirement == 35, $"{Game.World.shards.Count} shards, {Game.World.areas[0].requirement} wake the necklace");
            yield return Go(XZ(40, 460), .8f, 10f);
        }
    }
}
