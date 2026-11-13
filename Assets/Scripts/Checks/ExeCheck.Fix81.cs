using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace ShardsOfTime
{
    // Fix 8.1: Medieval fixes + shard thresholds. Modes: `fix81` (everything, every world), `floors` (items 1 and 3's static probes:
    // feet vs the visible floor, invisible colliders), `platforms` (item 2), `reach` (item 3: the bot walks to every shard), `rule81`
    // (item 4). The probes make a collider from every visible mesh (layer 31, collides with nothing, off while the bot moves) and
    // compare it with the colliders the heroes stand on and bump into.
    public sealed partial class ExeCheck
    {
        const int VisLayer = 31;
        static int VisMask => 1 << VisLayer;
        static int SolidMask => ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << Hero.EnemyLayer) | (1 << 4) | (1 << 2) | (1 << VisLayer));
        readonly List<GameObject> vis = new List<GameObject>();

        static readonly (string world, string cp)[] Fix81Worlds = { (WorldData.IceAge, IceAge.Start), (WorldData.Egypt, Egypt.Start), (WorldData.Bethlehem, Bethlehem.Start), (WorldData.Medieval, Medieval.Start), (WorldData.Pirate, Pirate.Start), (WorldData.Steampunk, Steampunk.FactoryCp), (WorldData.Tomorrowland, Tomorrowland.Start) };   // Round 12   // Round 10; Round 11

        IEnumerator Fix81() => Fix81Run(true, true, true, true);
        IEnumerator Floors81() => Fix81Run(true, false, false, false);
        IEnumerator Platforms81() => Fix81Run(false, true, false, false);
        IEnumerator Reach81() => Fix81Run(false, false, true, false);
        IEnumerator Rule81Only() => Fix81Run(false, false, false, true);
        IEnumerator Fix81Medieval() => Fix81Run(true, true, true, false, WorldData.Medieval);
        IEnumerator Fix81Pirate() => Fix81Run(true, true, true, true, WorldData.Pirate);   // Round 10
        IEnumerator Fix81Steam() => Fix81Run(true, true, true, true, WorldData.Steampunk);   // Round 11
        IEnumerator Fix81TL() => Fix81Run(true, true, true, true, WorldData.Tomorrowland);   // Round 12

        IEnumerator Fix81Run(bool floors, bool platforms, bool reach, bool rule, string only = null)
        {
            yield return EnsurePlaying(false);
            if (floors || platforms || reach)
                foreach (var (world, cp) in Fix81Worlds)
                {
                    if (only != null && world != only) continue;
                    yield return ToWorld(world, cp);
                    yield return Wait(1f);
                    var foes = Enemy.All.Where(e => e && e.gameObject.activeSelf).ToList(); foreach (var e in foes) e.gameObject.SetActive(false);   // (arrows come and go: not during the probes)
                    BuildVis();
                    if (floors) { yield return FloorProbe(); yield return InvisibleScan(); if (world == WorldData.Medieval) yield return FeetSpots(); }
                    if (platforms) yield return PlatformCheck();
                    if (reach) yield return ReachCheck();
                    ClearVis();
                    foreach (var e in foes) if (e) e.gameObject.SetActive(true);
                }
            if (rule) yield return RuleCheck();
        }

        // ---------------------------------------------------------------- the visible surfaces as probe colliders
        void BuildVis()
        {
            ClearVis(); int made = 0, skipped = 0;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!r.enabled || r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly) continue;
                int L = r.gameObject.layer;
                if (L == Hero.PlayerLayer || L == Hero.FollowerLayer || L == Hero.EnemyLayer || L == 4 || L == 2 || L == VisLayer) continue;
                if (r.GetComponentInParent<Hero>() || r.GetComponentInParent<Pickup>() || r.GetComponentInParent<Enemy>()) continue;
                if (r.name.StartsWith("FarHills") || r.name.StartsWith("Story beam") || r.name.StartsWith("Sky")) continue;
                var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue;
                if (!mf.sharedMesh.isReadable) { skipped++; continue; }
                var go = new GameObject("vis"); go.layer = VisLayer; go.transform.SetParent(r.transform, false);
                go.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh; vis.Add(go); made++;
            }
            for (int i = 0; i < 32; i++) Physics.IgnoreLayerCollision(VisLayer, i, true);
            Physics.SyncTransforms();
            Log($"  (probe: {made} visible meshes as probe colliders in {Game.World.displayName}; {skipped} not readable, skipped)");
        }
        void SetVis(bool on) { foreach (var v in vis) if (v) v.SetActive(on); Physics.SyncTransforms(); }
        void ClearVis() { foreach (var v in vis) if (v) Destroy(v); vis.Clear(); }
        static string Owner(Collider c) => c.gameObject.layer == VisLayer && c.transform.parent ? c.transform.parent.name : c.name;

        List<(string name, Bounds box, float step)> ProbeBoxes()
        {
            var w = Game.World; var l = new List<(string, Bounds, float)>();
            if (w.worldId == WorldData.Medieval)
            {
                l.Add(("courtyard", B(-30, 30, -1, 40, 0, 63), .5f)); l.Add(("walls and towers", B(-34, 34, 5, 40, -7, 64), 1f)); l.Add(("great hall", B(-15, 15, -1, 40, 63, 87), .5f));
                l.Add(("moat and banks", B(-60, 60, -20, 30, -30, 114), 1f)); l.Add(("tournament field", B(35, 135, -5, 30, -130, -35), 1f));
                l.Add(("village", B(-145, -25, -5, 30, -140, -35), 1f)); l.Add(("dungeon", B(386, 414, -2, 8, -2, 60), .5f));
            }
            else foreach (var a in w.areas) if (a.box.size != Vector3.zero) l.Add((a.name, a.box, 2f));
            return l;
            Bounds B(float x0, float x1, float y0, float y1, float z0, float z1) { var b = new Bounds(); b.SetMinMax(new Vector3(x0, y0, z0), new Vector3(x1, y1, z1)); return b; }
        }

        // ---------------------------------------------------------------- item 1: feet vs the visible floor, per area
        // the visible surface over the feet is a floor (the plinth in the hall was one), not a prop the hero stands beside
        static bool FloorLike(string n) => new[] { "Floor", "Plinth", "Path", "Gateway", "Step", "Circle", "Cobble", "Deck", "Terrain", "Ground", "Road", "Hearth", "Flagstone", "Snow", "Sand", "Grass" }.Any(k => n.Contains(k));
        sealed class Cell { public int n, bad; public float sum, worst; public string col, vis; public Vector3 at; }
        IEnumerator FloorProbe()
        {
            var w = Game.World;
            Head($"Fix 8.1, item 1: feet vs the visible floor in {w.displayName} (every walkable collider top compared with the visible surface at it)");
            int k = 0, defects = 0;
            foreach (var (name, b, step) in ProbeBoxes())
            {
                int n = 0, sunk = 0, flo = 0, none = 0, propOver = 0; float worstSunk = 0, worstFloat = 0;
                var cells = new Dictionary<(int, int, int), Cell>(); var flCells = new Dictionary<(int, int, int), Cell>();
                for (float x = b.min.x; x <= b.max.x; x += step)
                    for (float z = b.min.z; z <= b.max.z; z += step)
                    {
                        if (++k % 3000 == 0) yield return null;
                        foreach (var h in Physics.RaycastAll(new Vector3(x, b.max.y, z), Vector3.down, b.size.y, SolidMask, QueryTriggerInteraction.Ignore))
                        {
                            if (h.normal.y < .7f || h.point.y < b.min.y) continue;
                            if (h.collider.GetComponentInParent<Hero>() || h.collider.GetComponentInParent<Pickup>() || h.collider.GetComponentInParent<Enemy>()) continue;
                            if (Physics.Raycast(h.point + Vector3.up * .05f, Vector3.up, 1.0f, SolidMask, QueryTriggerInteraction.Ignore)) continue;   // no head room: nobody stands here
                            if (Physics.CheckSphere(h.point + Vector3.up * .22f, .15f, SolidMask, QueryTriggerInteraction.Ignore)) continue;   // under another solid (a step on the ground)
                            if (h.collider.name.Contains("boundary")) continue;   // the world's edge walls
                            if (WaterVolume.At(h.point + Vector3.up * .3f)) continue;   // under water: swimming, not standing
                            n++;
                            float d; string vname;
                            if (Physics.Raycast(h.point + Vector3.up * 1.5f, Vector3.down, out var v, 2.5f, VisMask, QueryTriggerInteraction.Collide)) { d = v.point.y - h.point.y; vname = Owner(v.collider); }
                            else { d = -9f; vname = "(nothing visible within 1 m below)"; }
                            bool bad = d > .1f || d < -.1f;
                            bool floorLike = FloorLike(vname);
                            if (d > .1f && !floorLike) { propOver++; bad = false; }
                            else if (d > .1f) { sunk++; worstSunk = Mathf.Max(worstSunk, d); }
                            else if (d < -8f) none++;
                            else if (d < -.1f) { flo++; worstFloat = Mathf.Min(worstFloat, d); }
                            var key = (Mathf.FloorToInt(x / 6), Mathf.FloorToInt(z / 6), Mathf.RoundToInt(h.point.y));
                            if (!cells.TryGetValue(key, out var c)) cells[key] = c = new Cell { at = h.point };
                            c.n++;
                            if (bad && d < -.1f) { if (!flCells.TryGetValue(key, out var fc)) flCells[key] = fc = new Cell { at = h.point, col = h.collider.name, vis = vname }; fc.bad++; fc.sum += Mathf.Max(d, -1f); }
                            if (bad && d > .1f) { c.bad++; c.sum += d; if (Mathf.Abs(d) > Mathf.Abs(c.worst)) { c.worst = d; c.col = h.collider.name; c.vis = vname; c.at = h.point; } }
                        }
                    }
                // a defect: a 6 x 6 m patch with at least 4 points where the feet sink more than 10 cm under what is seen (single points at a
                // mesh's edge are noise)
                var badCells = cells.Values.Where(c => c.bad >= 4).OrderByDescending(c => c.bad).ToList();
                defects += badCells.Count;
                Ok(badCells.Count == 0, $"{w.displayName} / {name}: {n} floor points, feet sunk > 10 cm at {sunk} (worst {worstSunk * 100:0} cm; patches of 4+: {badCells.Count}); floating > 10 cm at {flo} (worst {-worstFloat * 100:0} cm), nothing visible under {none} (listed, not failed); a prop's look over its collider at {propOver} (not a floor: not failed)");
                foreach (var c in badCells.Take(25))
                    Log($"      sunk {Fmt(c.at)}: {c.bad} of {c.n} points, average {c.sum / c.bad * 100:0} cm, worst {c.worst * 100:0} cm (collider '{c.col}', visible '{c.vis}')");
                // floating (the collider's top over what is seen: roofs, wall tops, ledges): listed for a look, not failed
                foreach (var c in flCells.Values.Where(c => c.bad >= 4).OrderByDescending(c => c.bad).Take(15))
                    Log($"      (floating {Fmt(c.at)}: {c.bad} points, average {c.sum / c.bad * 100:0} cm; collider '{c.col}', visible '{c.vis}')");
            }
            Log($"  ({w.displayName}: {defects} patch(es) in all)");
        }

        // feet in the player's view at the places Dan named (the courtyard where Muscle Man shows off, the keep's steps, the hall)
        IEnumerator FeetSpots()
        {
            Head("Fix 8.1, item 1: the heroes' feet where Muscle Man is first met (built exe, player's view)");
            SetVis(false);
            var spots = new[] { ("the courtyard by Muscle Man (first meeting)", new Vector3(-11f, 0, 14f), 300f), ("the keep's steps", new Vector3(0, .5f, 61.2f), 0f),
                                ("the great hall by Muscle Man", new Vector3(5.5f, 0, 70f), 60f), ("the walled yard's gate", new Vector3(-16f, 0, 8f), 270f) };
            for (int i = 0; i < P.Heroes.Count; i++)
            {
                yield return SwitchTo(i); string who = Player.Stats.displayName;
                foreach (var (name, at, yaw) in spots)
                {
                    yield return TpSettle(Medieval.OnGround(at.x, at.z, 3f) + Vector3.up * .1f, yaw); yield return Wait(.6f);
                    SetVis(true);
                    var p = Player.transform.position;
                    float vy = Physics.Raycast(p + Vector3.up * .6f, Vector3.down, out var v, 2f, VisMask, QueryTriggerInteraction.Collide) ? v.point.y : float.NaN;
                    SetVis(false);
                    float off = p.y - vy;   // + = feet above the visible floor, - = sunk into it
                    Ok(M.Grounded && Mathf.Abs(off) < .1f, $"{who} at {name}: feet {p.y:0.00}, visible floor {vy:0.00} ({off * 100:+0;-0} cm), grounded {M.Grounded}");
                    if (i == 0) yield return Shot($"81_feet_{name.Split(' ')[1]}_{who}");
                }
            }
        }

        // ---------------------------------------------------------------- item 3: invisible colliders (solid, with nothing seen on them)
        IEnumerator InvisibleScan()
        {
            var w = Game.World;
            Head($"Fix 8.1, item 3: invisible walls in {w.displayName} (every solid collider's open sides: is anything visible there?)");
            var terrain = Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(c => c.name.StartsWith("Terrain") && c.gameObject.layer != VisLayer).ToArray();
            float TerrainY(Vector3 p)
            {
                float y = -999; var ray = new Ray(new Vector3(p.x, 400, p.z), Vector3.down);
                foreach (var t in terrain) if (t.Raycast(ray, out var h, 800)) y = Mathf.Max(y, h.point.y);
                return y;
            }
            var found = new List<string>(); int scanned = 0, k = 0; var edges = new List<string>();
            foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!c || c.isTrigger || !c.enabled || c is CharacterController || c is MeshCollider mc && !mc.convex) continue;   // (ClosestPoint needs a convex shape)
                int L = c.gameObject.layer; if (L == Hero.PlayerLayer || L == Hero.FollowerLayer || L == Hero.EnemyLayer || L == 4 || L == 2 || L == VisLayer) continue;
                if (c.GetComponentInParent<Hero>() || c.GetComponentInParent<Pickup>() || c.GetComponentInParent<Enemy>()) continue;
                if (c.name.StartsWith("Terrain")) continue;
                var r = c.GetComponent<Renderer>(); if (r && r.enabled) continue;   // drawn itself
                var b = c.bounds; if (b.size.x > 300 || b.size.z > 300) continue;
                scanned++;
                int pts = 0, seen = 0;
                foreach (var (n, u) in new[] { (Vector3.right, Vector3.forward), (Vector3.left, Vector3.forward), (Vector3.forward, Vector3.right), (Vector3.back, Vector3.right) })
                {
                    float len = Vector3.Scale(b.size, u).magnitude, half = Vector3.Scale(b.extents, n).magnitude;
                    int nu = Mathf.Clamp(Mathf.FloorToInt(len), 1, 12);
                    for (int i = 0; i < nu; i++)
                        for (float y = b.min.y + .4f; y <= Mathf.Min(b.max.y, b.min.y + 4f); y += 1f)
                        {
                            var onBox = b.center + n * half + u * ((i + .5f) / nu - .5f) * len; onBox.y = y;
                            var face = c.ClosestPoint(onBox + n * 2f);   // the collider's own surface (a sphere, a turned box)
                            if ((face - onBox).magnitude > 1.5f) continue;
                            var outside = face + n * .35f;
                            if (outside.y < TerrainY(outside) + .1f) continue;                                        // under the ground
                            if (Physics.CheckSphere(outside, .2f, SolidMask, QueryTriggerInteraction.Ignore)) continue;   // against another solid: nobody gets here
                            if (WaterVolume.At(outside) && outside.y < WaterVolume.At(outside).Top - 1.5f) continue;
                            pts++;
                            if (Physics.CheckSphere(face, .45f, VisMask, QueryTriggerInteraction.Collide)) seen++;
                        }
                }
                if (++k % 40 == 0) yield return null;
                if (pts >= 4 && seen < pts * .5f)
                {
                    string line = $"'{c.name}' at {Fmt(b.center)} size {Fmt(b.size)}: {seen} of {pts} open side points have anything visible within 45 cm";
                    if (c.name.Contains("oundary") || c.name.Contains("edge")) edges.Add(line);
                    else if (c.name.StartsWith("Sphinx") || c.name == "Gate arch") edges.Add(line + " [a block inside a big sculpture / over the gate passage: out of reach]");
                    else found.Add(line);
                }
            }
            Ok(found.Count == 0, $"{w.displayName}: {scanned} hidden solid colliders scanned, {found.Count} with nothing visible on them (world-edge walls listed apart: {edges.Count})");
            foreach (var f in found.Take(40)) Log("      " + f);
            foreach (var f in edges) Log("      (world edge) " + f);
        }

        // ---------------------------------------------------------------- item 2: every moving platform (and the drawbridge), every hero
        IEnumerator PlatformCheck()
        {
            var w = Game.World;
            var list = Object.FindObjectsByType<MovingPlatform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Select(m => (m.name, m.transform)).ToList();
            if (w.worldId == WorldData.Medieval && Medieval.Drawbridge) list.Insert(0, ("Drawbridge", Medieval.Drawbridge));
            Head($"Fix 8.1, item 2: walk across every moving platform in {w.displayName} ({list.Count}), every hero");
            SetVis(false);
            int idx = 0;
            foreach (var (name, t) in list)
            {
                idx++;
                if (name.Contains("Camel") || t.GetComponentInParent<Camel>()) { Log($"  ({name}: the camel is ridden, not stood on: its riding is checked by `camel`)"); continue; }
                Collider[] Cols() => t.GetComponentsInChildren<Collider>().Where(c => !c.isTrigger && c.enabled && c.gameObject.layer != VisLayer).ToArray();
                var cols = Cols();
                Ok(cols.Length > 0, $"{name} at {Fmt(t.position)} has a solid collider ({cols.Length})");
                if (cols.Length == 0) continue;
                Bounds Box() { var cs = Cols(); var bb = cs[0].bounds; foreach (var c in cs) bb.Encapsulate(c.bounds); return bb; }
                var b0 = Box();
                bool alongX = b0.size.x >= b0.size.z;
                float ext = Mathf.Max(.3f, (alongX ? b0.extents.x : b0.extents.z) - .45f);
                var axis = alongX ? Vector3.right : Vector3.forward;
                Vector3 la = t.InverseTransformPoint(b0.center - axis * ext), lb = t.InverseTransformPoint(b0.center + axis * ext);
                bool shot = false;
                for (int i = 0; i < P.Heroes.Count; i++)
                {
                    yield return SwitchTo(i); Health.Refill(); string who = Player.Stats.displayName;
                    var from = t.TransformPoint(la); from.y = Box().max.y + .15f;
                    var to0 = t.TransformPoint(lb) - t.TransformPoint(la); to0.y = 0;
                    // a sliding block can be inside its wall when we arrive: try again a little later (up to 4 times)
                    var mpf = t.GetComponent<MovingPlatform>();
                    for (int tries = 0; tries < 4; tries++)
                    {
                        // a sliding platform: placed at the ends of its slide in turn (one end is out of its wall)
                        if (mpf && mpf.a != mpf.b && mpf.path == null) { var end0 = tries % 2 == 0 ? mpf.a : mpf.b; yield return WaitFor(() => (t.position - end0).magnitude < .25f, mpf.period + 1f); }
                        from = t.TransformPoint(la); from.y = Box().max.y + .15f;
                        Tp(from, Mathf.Atan2(to0.x, to0.z) * Mathf.Rad2Deg);
                        yield return Wait(.5f);
                        var bq = Box(); var pq = Player.transform.position;
                        if (M.Grounded && pq.y > bq.max.y - .3f && pq.x > bq.min.x - .2f && pq.x < bq.max.x + .2f && pq.z > bq.min.z - .2f && pq.z < bq.max.z + .2f) break;
                        yield return Wait(1.7f);
                    }
                    float end = Time.time + Mathf.Max(3f, ext * 2f / 3f + 2.5f), lowest = 99f; bool fell = false, there = false, onTop = M.Grounded;
                    while (Time.time < end)
                    {
                        var bb = Box(); var p = Player.transform.position; var target = t.TransformPoint(lb);
                        var d = target - p; d.y = 0;
                        bool over = p.x > bb.min.x - .2f && p.x < bb.max.x + .2f && p.z > bb.min.z - .2f && p.z < bb.max.z + .2f;
                        if (over) lowest = Mathf.Min(lowest, p.y - bb.max.y);
                        if (over && p.y < bb.max.y - .6f || M.Mode == MoveMode.Swim) { fell = true; break; }
                        if (d.magnitude < .45f) { there = true; break; }
                        Stick(StickFor(d.normalized) * .55f);
                        if (!shot && i == 0 && Time.time > end - 2f && name == "Drawbridge") { shot = true; yield return Shot("81_drawbridge_walk"); }
                        yield return null;
                    }
                    Stick(Vector2.zero);
                    Ok(onTop && !fell, $"{name}: {who} {(fell ? "FELL THROUGH" : there ? "walked across" : "stood on it, didn't reach the far end (it slid away / into its wall)")} (feet at most {Mathf.Max(0, -lowest) * 100:0} cm under its top; started on it {onTop}; at {Fmt(Player.transform.position)})");
                    if (fell || !onTop) yield return Shot($"81_platform_{idx}_{who}_fail");
                }
                if (name == "Drawbridge")
                {
                    yield return SwitchTo(0);
                    yield return TpSettle(Medieval.OnGround(1.5f, -26f, 5f) + Vector3.up * .1f, 0); yield return Wait(.6f);
                    yield return Shot("81_drawbridge_view");
                    SetVis(true);
                    float seenTop = Physics.Raycast(new Vector3(0, 3, -12), Vector3.down, out var dv, 6f, VisMask, QueryTriggerInteraction.Collide) ? dv.point.y : float.NaN;
                    SetVis(false);
                    Ok(Mathf.Abs(seenTop - Box().max.y) < .1f && Mathf.Abs(Box().max.y) < .1f, $"the drawbridge's planks are seen at {seenTop:0.00} over the moat, its collider's top at {Box().max.y:0.00} (the gate and the bank at 0)");
                    // the deck moves with the drawbridge: raised (turned up about its hinge) the collider stands up too; then lowered again
                    var rot = t.rotation; float top0 = Box().max.y;
                    t.rotation = Quaternion.Euler(80, 0, 0) * rot; Physics.SyncTransforms();
                    float topUp = Box().max.y;
                    t.rotation = rot; Physics.SyncTransforms();
                    Ok(topUp > top0 + 8f && Mathf.Abs(Box().max.y - top0) < .01f, $"the drawbridge's deck collider moves with it: raised its top is at {topUp:0.0} (lowered {top0:0.0}), lowered again {Box().max.y:0.0}");
                }
            }
            if (list.Count == 0) Log($"  ({w.displayName} has no moving platforms)");
            SetVis(true);
        }

        // ---------------------------------------------------------------- item 3: every shard reachable by the bot from its open sides
        IEnumerator ReachCheck()
        {
            var w = Game.World; var first = w.areas[0].firstVisit;
            Head($"Fix 8.1, item 3: the bot walks to every shard in {w.displayName} from its open sides (with the moves taught so far; gates opened as their move would)");
            var had = new List<string>(SaveGame.Current.abilities);
            foreach (var a in TaughtSoFar.Concat(first)) if (a != "Dan" && a != "Abi" && a != "Kira" && !SaveGame.Current.abilities.Contains(a)) SaveGame.Current.abilities.Add(a);
            Hero ByName(string n) => P.Heroes.First(h => h.Stats.displayName == n);
            foreach (var f in FireTarget.All.ToList()) if (f && !f.Lit) f.Burn(ByName("Kira"));
            foreach (var d in Object.FindObjectsByType<CellDoor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) if (!d.Open) d.Unlock();
            foreach (var r in RockWall.All.ToList()) if (r) r.Punch(ByName("Dan"), true);
            foreach (var j in MuscleJob.All.ToList()) if (j) j.OpenNow();
            foreach (var c in CrackedFloor.All.ToList()) if (c) c.Pound(ByName("Dan"));
            foreach (var iw in Object.FindObjectsByType<IceWall>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) if (!iw.Broken) iw.Claw(ByName("Kira"));
            yield return Wait(1.5f);
            var enemies = Enemy.All.Where(e => e && e.gameObject.activeSelf).ToList(); foreach (var e in enemies) e.gameObject.SetActive(false);   // (already off from Fix81Run: an empty list)
            Physics.SyncTransforms();
            // approach points first (with the visible surfaces on: an approach must look open)
            var plan = new List<(ShardSpot s, Pickup pk, List<Vector3> from, string skip)>();
            foreach (var s in w.shards)
            {
                var pk = Pickup.All.FirstOrDefault(p => p && p.kind == Pickup.Kind.Shard && p.id == s.id);
                if (!pk) { plan.Add((s, null, null, "already collected in this save")); continue; }
                var later = s.requires.Where(r => r != "Dan" && r != "Abi" && r != "Kira" && !TaughtSoFar.Contains(r) && !first.Contains(r)).ToList();
                if (later.Any()) { plan.Add((s, pk, null, $"needs {string.Join("+", later)}, a later move")); continue; }
                var at = pk.transform.position;
                if (WaterVolume.At(at)) { plan.Add((s, pk, null, "in water (swum to: the never-stuck water checks)")); continue; }
                var from = new List<Vector3>();
                foreach (float r in new[] { 4f, 2.6f })
                {
                    foreach (int a in new[] { 0, 6, 3, 9, 1, 7, 4, 10, 2, 8, 5, 11 })
                    {
                        if (from.Count >= 3) break;
                        var dir = Quaternion.Euler(0, a * 30f, 0) * Vector3.forward; var p = at + dir * r;
                        if (from.Any(q => Vector3.Angle(q - at, dir) < 50f)) continue;
                        if (!Physics.Raycast(p + Vector3.up * .6f, Vector3.down, out var g, 2.4f, SolidMask, QueryTriggerInteraction.Ignore) || g.normal.y < .7f) continue;
                        if (Mathf.Abs(g.point.y - (at.y - 1f)) > .55f) continue;   // the shard floats 1 m over its floor: the same level
                        if (Physics.Raycast(g.point + Vector3.up * .05f, Vector3.up, 1.7f, SolidMask, QueryTriggerInteraction.Ignore) || WaterVolume.At(g.point + Vector3.up * .3f)) continue;
                        if (Physics.CheckSphere(g.point + Vector3.up * .9f, .45f, SolidMask, QueryTriggerInteraction.Ignore)) continue;
                        if (Physics.Linecast(g.point + Vector3.up * .9f, at + Vector3.down * .1f, VisMask, QueryTriggerInteraction.Collide)) continue;   // something seen in the way
                        if (Physics.Linecast(g.point + Vector3.up * .35f, at + Vector3.down * .65f, VisMask, QueryTriggerInteraction.Collide)) continue;
                        from.Add(g.point);
                    }
                    if (from.Count > 0) break;
                }
                plan.Add((s, pk, from, from.Count == 0 ? "no open level ground within 4 m (a ledge, a pillar top, a narrow spot)" : null));
            }
            SetVis(false);
            int walked = 0, unreached = 0, partial = 0; var skips = new List<string>();
            foreach (var (s, pk, from, skip) in plan)
            {
                if (skip != null) { skips.Add($"{s.id} ({s.note}): {skip}"); continue; }
                pk.enabled = false;
                string hero = s.requires.Contains("Kira") ? "Kira" : s.requires.Contains(Abilities.DoubleJump) ? "Abi" : "Dan";
                yield return SwitchTo(P.Heroes.FindIndex(h => h.Stats.displayName == hero)); Health.Refill();
                int ok = 0; var failedFrom = new List<string>();
                foreach (var f in from)
                {
                    // never during a cutscene (a brazier group or the necklace can start one): skip it as a player would
                    for (int g = 0; g < 4 && (Game.InCutscene || Game.I.Busy); g++) { if (Game.InCutscene) yield return Hold(GamepadButton.South, 1.4f); yield return WaitFor(() => !Game.InCutscene && !Game.I.Busy, 6f); }
                    var at = pk.transform.position; var to = at - f; to.y = 0;
                    Tp(f + Vector3.up * .05f, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg);
                    yield return Wait(.35f);
                    float end = Time.time + 3.6f, blocked = 0, best = 99f, jumpT = 0; bool got = false;
                    while (Time.time < end)
                    {
                        var p = Player.transform.position; float reach = pk.radius + Player.Stats.radius;
                        float dist = Mathf.Min((M.Center - at).magnitude, (p - at).magnitude); best = Mathf.Min(best, dist);
                        if (dist < reach) { got = true; break; }
                        var d = at - p; d.y = 0;
                        Stick(StickFor(d.normalized) * Mathf.Clamp(d.magnitude / 1.2f, .4f, 1f));
                        // a gap ahead (a pillar's edge, the water before a floe): jump it, as a player would
                        if (jumpT > 0) { jumpT -= Time.deltaTime; if (jumpT <= 0) Btn(GamepadButton.South, false); }
                        else if (M.Grounded && d.magnitude > 1f && !Physics.Raycast(p + d.normalized * .8f + Vector3.up * .5f, Vector3.down, 1.6f, SolidMask, QueryTriggerInteraction.Ignore)) { Btn(GamepadButton.South, true); jumpT = .4f; }
                        var hv = M.Velocity; hv.y = 0;
                        blocked = M.Grounded && hv.magnitude < .5f ? blocked + Time.deltaTime : 0;
                        if (blocked > .4f) { blocked = 0; yield return Press(GamepadButton.South); }
                        yield return null;
                    }
                    Stick(Vector2.zero); Btn(GamepadButton.South, false);
                    if (got) ok++; else failedFrom.Add($"from {Fmt(f)} got within {best:0.0} m, stopped at {Fmt(Player.transform.position)}");
                }
                pk.enabled = true; walked++;
                if (ok == 0) { unreached++; Fail($"{w.displayName} {s.id} ({s.note}) at {Fmt(pk.transform.position)}: NOT reached by {hero} from any of {from.Count} open sides: {string.Join("; ", failedFrom)}"); yield return Shot($"81_reach_{w.worldId}_{s.id}"); }
                else if (failedFrom.Count > 0) { partial++; Log($"  note  {w.displayName} {s.id} ({s.note}): reached by {hero} from {ok} of {from.Count} sides; blocked {string.Join("; ", failedFrom)}"); }
            }
            Ok(unreached == 0, $"{w.displayName}: {walked} shards walked to, {unreached} not reached, {partial} reached from some sides only; {skips.Count} not walked (listed)");
            foreach (var sk in skips) Log("      not walked: " + sk);
            foreach (var e in enemies) if (e) e.gameObject.SetActive(true);
            SaveGame.Current.abilities.Clear(); SaveGame.Current.abilities.AddRange(had);
            SetVis(true);
        }

        // ---------------------------------------------------------------- item 4: the necklace's new rule, rule 2 with the new numbers
        IEnumerator RuleCheck()
        {
            Head("Fix 8.1, item 4: a world's necklace wakes at its total threshold AND 20 shards from the world itself");
            foreach (var id in WorldData.StoryOrder)
            {
                var wd = WorldData.Load(id);
                foreach (var a in wd.areas) { var r = wd.RequirementReport(a.id); Ok(r.ok, "rule 2: " + r.detail.Split(new[] { "; gates:" }, System.StringSplitOptions.None)[0]); }
            }
            yield return ToWorld(WorldData.Medieval, Medieval.Start);
            yield return SwitchTo(0);
            yield return TpSettle(Medieval.OnGround(0, 14, 3f), 0);
            var s = SaveGame.Current; var area = Game.World.areas[0]; var prog = s.World(WorldData.Medieval);
            prog.necklaceAwake = false; prog.storyShard = false;
            var ids = Game.World.shards.Select(x => x.id).ToList();
            void SetHere(int n) { prog.shards.Clear(); prog.shards.AddRange(ids.Take(n)); }
            void SetTotal(int total) { var ia = s.World(WorldData.IceAge); ia.shards.RemoveAll(x => x.StartsWith("fix81-")); int i = 0; while (s.Total < total) ia.shards.Add("fix81-" + i++); }
            // a: plenty from here, the total short
            SetHere(25); var ia0 = s.World(WorldData.IceAge); ia0.shards.Clear(); SetTotal(area.requirement - 10);
            yield return Wait(1.2f);
            Ok(!prog.necklaceAwake && Hud.UnderTotal == $"Next: {area.requirement} (this world: {area.fromHere} / {area.fromHere})", $"{s.Total} in all ({Game.I.AreaShards(area.id)} from here): asleep; the HUD under the total: \"{Hud.UnderTotal}\"");
            // b: the total reached, only 12 from here (the "I only needed like 3" case)
            SetHere(12); SetTotal(area.requirement + 30);
            yield return Wait(1.2f);
            Ok(!prog.necklaceAwake && Hud.UnderTotal == $"Next: {area.requirement} (this world: 12 / {area.fromHere})", $"{s.Total} in all, {Game.I.AreaShards(area.id)} from here: still asleep; HUD \"{Hud.UnderTotal}\"");
            yield return Shot("81_hud_next_this_world");
            // c: the 20th shard from here wakes it
            SetHere(20); SetTotal(area.requirement + 30);
            yield return WaitFor(() => prog.necklaceAwake, 3f);
            Ok(prog.necklaceAwake, $"{s.Total} in all, {Game.I.AreaShards(area.id)} from here: the necklace wakes");
            yield return WaitFor(() => !Game.I.Busy, 30f); yield return Wait(.5f);
            Ok(Hud.UnderTotal == "Follow the necklace", $"after waking: HUD \"{Hud.UnderTotal}\"");
            // d: an awake necklace (an older save) never relocks, even with fewer than 20 from here
            SetHere(5); SetTotal(area.requirement - 40);
            yield return Wait(1.2f);
            Ok(prog.necklaceAwake && Hud.UnderTotal == "Follow the necklace", $"an awake world with {Game.I.AreaShards(area.id)} from here and {s.Total} in all stays awake (HUD \"{Hud.UnderTotal}\")");
            s.World(WorldData.IceAge).shards.RemoveAll(x => x.StartsWith("fix81-"));
        }
    }
}
