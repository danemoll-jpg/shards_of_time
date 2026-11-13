using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShardsOfTime
{
    // Fix 13.2 (Dan's play-test of Tomorrowland and the Moon), the exe check "fix132":
    //  item 2: every world's text (signs, labels) uses the world-text material, and a pixel test: a sign seen from its front shows; from
    //          behind it, or from its front with a wall between, not one pixel of it is drawn (rendered with and without the sign);
    //  items 1, 3: every Muscle Man job in every world (the prompts sweep): he plays his job's own clip the whole time (never fall / swim /
    //          tread), and nothing is left behind where the object stood (the car's yellow hatch cover); before / after shots;
    //  item 5: in every world the party is held (frozen, followers still carrying speed) and nobody plays a walk / run clip; the run-in-place
    //          counter (any character on a walk / run clip while not moving for more than half a second) is 0 at the end of every check.
    public sealed partial class ExeCheck
    {
        IEnumerator Fix132()
        {
            yield return EnsurePlaying(false);
            jobLook = true;
            yield return PromptsAll();
            yield return SignsCheck();
        }

        bool jobLook;
        IEnumerator Fix132Jobs()
        {
            yield return EnsurePlaying(false); jobLook = true;
            if (!SaveGame.Current.Has(Abilities.MuscleMan)) SaveGame.Current.abilities.Add(Abilities.MuscleMan);
            if (!P.Companion) P.AddCompanion(Player.transform.position + Vector3.back * 3f, Quaternion.identity);
            yield return PromptWorld(WorldData.Pirate, Pirate.Start);
            yield return PromptWorld(WorldData.Steampunk, Steampunk.FactoryCp);
        }

        // ---------------------------------------------------------------- item 2
        // how many pixels the renderer adds to a view from `from` looking at `at` (drawn with and without it)
        int TextPixels(Renderer r, Vector3 from, Vector3 at)
        {
            var go = new GameObject("Sign test camera"); var cam = go.AddComponent<Camera>();
            var main = CameraRig.I.Cam; cam.CopyFrom(main); cam.enabled = false;
            var rt = new RenderTexture(480, 270, 24); cam.targetTexture = rt; cam.fieldOfView = 40f; cam.nearClipPlane = .1f;
            go.transform.SetPositionAndRotation(from, Quaternion.LookRotation(at - from));
            Color32[] Grab()
            {
                cam.Render(); var prev = RenderTexture.active; RenderTexture.active = rt;
                var t = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); t.Apply();
                RenderTexture.active = prev; var px = t.GetPixels32(); Object.Destroy(t); return px;
            }
            bool was = r.enabled;
            r.enabled = true; var a = Grab(); r.enabled = false; var b = Grab(); r.enabled = was;
            int n = 0; for (int i = 0; i < a.Length; i++) if (Mathf.Abs(a[i].r - b[i].r) + Mathf.Abs(a[i].g - b[i].g) + Mathf.Abs(a[i].b - b[i].b) > 24) n++;
            cam.targetTexture = null; rt.Release(); Object.Destroy(rt); Object.Destroy(go);
            return n;
        }

        IEnumerator SignView(string what, TextMesh tm, Vector3 front, Vector3 back, Vector3? frontWall)
        {
            var r = tm.GetComponent<Renderer>(); var c = r.bounds.center;
            int f = TextPixels(r, front, c), bk = TextPixels(r, back, c);
            Ok(f > 40, $"{what} \"{tm.text.Replace("\n", " ")}\": from its front it shows ({f} px)");
            Ok(bk == 0, $"{what}: from behind it, nothing of it is drawn ({bk} px)");
            if (frontWall.HasValue) { int w = TextPixels(r, frontWall.Value, c); Ok(w == 0, $"{what}: from its front side with a wall between, nothing of it is drawn ({w} px)"); }
            yield return null;
        }

        IEnumerator SignsCheck()
        {
            Head("Fix 13.2, item 2: world text is hidden by walls and shows only from its front");
            var shader = Resources.Load<Shader>("Shaders/WorldText");
            Ok(shader, "the world-text shader is in the build");
            foreach (var (world, cp) in new[] { (WorldData.IceAge, "IA-vortex"), (WorldData.Bethlehem, Bethlehem.Start), (WorldData.Tomorrowland, Tomorrowland.Start) })
            {
                if (cp != null) yield return ToWorld(world, cp);
                yield return Wait(.5f);
                var all = Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                var bad = all.Where(t => { var mr = t.GetComponent<MeshRenderer>(); return !mr || !mr.sharedMaterial || mr.sharedMaterial.shader != shader; }).ToList();
                Ok(bad.Count == 0, $"{Game.World.displayName}: all {all.Length} world texts use the world-text material{(bad.Count > 0 ? " (not: " + string.Join(", ", bad.Take(6).Select(t => t.name)) + ")" : "")}");
                if (world == WorldData.Bethlehem)
                {
                    var nr = all.FirstOrDefault(t => t.text == "NO ROOM");
                    if (!nr) { Fail("Bethlehem's NO ROOM sign is missing"); continue; }
                    var p = nr.transform.position; var fw = nr.transform.forward;   // a TextMesh reads from its -z side
                    yield return SignView("Bethlehem's inn sign", nr, p - fw * 6f + Vector3.up * .3f, p + fw * 6f + Vector3.up * .3f, null);
                    yield return TpSettle(Game.OnGround(p.x, p.z - 6f) + Vector3.up * .05f, 0); yield return Wait(.8f); yield return Shot("520_bethlehem_no_room_front");
                }
                if (world == WorldData.Tomorrowland)
                {
                    // an east shop's sign (x 13.8, facing west over the avenue): front from the avenue; behind from the alley past the shop
                    // (x 30: the shop between); its front side through a west shop (x -30: the west shop's solid block between)
                    var s = all.Where(t => t.name.StartsWith("Sign ") && t.transform.position.x > 10f && t.transform.position.z > 20f && t.transform.position.z < 36f).OrderBy(t => t.transform.position.z).FirstOrDefault();
                    if (!s) { Fail("Tomorrowland: no east shop sign at z 22..34"); continue; }
                    var p = s.transform.position;
                    yield return SignView($"Tomorrowland's shop sign at {Fmt(p)}", s, new Vector3(0, p.y, p.z), new Vector3(31f, p.y, p.z), new Vector3(-31f, p.y, p.z));
                    // the shot Dan saw: on the roofs behind a shop, looking at its back
                    yield return TpSettle(new Vector3(19f, 7.05f, p.z - 6f), 300f); yield return Wait(1f); yield return Shot("521_tl_sign_from_the_roof_behind");
                    yield return TpSettle(new Vector3(4f, .05f, p.z - 8f), 60f); yield return Wait(1f); yield return Shot("522_tl_sign_from_the_avenue");
                }
                if (world == WorldData.IceAge)
                {
                    // the vortex door labels (free-standing: only the front draws)
                    SaveGame.Current.checkpoint = Vortex.Checkpoint; Checkpoint.RespawnParty(); CameraRig.I.SnapBehind(); yield return Wait(1f);
                    var lab = Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None).FirstOrDefault(t => t.transform.parent && t.transform.parent.GetComponent<VortexDoor>());
                    if (!lab) { Fail("no vortex door label"); continue; }
                    var p = lab.transform.position; var fw = lab.transform.forward;
                    yield return SignView("a vortex door's label", lab, p - fw * 8f, p + fw * 8f, null);
                    yield return Shot("523_vortex_labels");
                }
            }
        }

        // ---------------------------------------------------------------- items 1, 3 (called from the prompts sweep, step 4)
        static readonly string[] SwimLike = { "fall", "swim", "tread", "mm_struggle", "walk", "run", "run_slow" };
        Bounds JobBounds(MuscleJob j)
        {
            var bs = j.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger).Select(c => c.bounds)
                .Concat(j.GetComponentsInChildren<Renderer>().Where(r => r.enabled && r.gameObject.activeInHierarchy).Select(r => r.bounds)).ToList();
            var b = bs.Count > 0 ? bs[0] : new Bounds(j.transform.position, Vector3.one); foreach (var x in bs) b.Encapsulate(x); return b;
        }
        // what sits with the object, inside its footprint, not part of it (pickups, people and big floors aside)
        List<Renderer> SitsWith(MuscleJob j, Bounds foot)
        {
            var list = new List<Renderer>();
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy || r.transform.IsChildOf(j.transform) || r is ParticleSystemRenderer) continue;
                if (r.GetComponentInParent<Pickup>() || r.GetComponentInParent<Hero>() || r.GetComponentInParent<Enemy>()) continue;
                var b = r.bounds; if (b.size.x > foot.size.x * 1.6f + .5f || b.size.z > foot.size.z * 1.6f + .5f) continue;   // floors, walls bigger than it
                if (b.max.y < foot.min.y - .3f || b.min.y > foot.max.y) continue;
                float ix = Mathf.Max(0, Mathf.Min(b.max.x, foot.max.x) - Mathf.Max(b.min.x, foot.min.x)), iz = Mathf.Max(0, Mathf.Min(b.max.z, foot.max.z) - Mathf.Max(b.min.z, foot.min.z));
                float area = Mathf.Max(.0001f, b.size.x * b.size.z);
                if (ix * iz / area > .7f) list.Add(r);
            }
            return list;
        }

        IEnumerator JobLook(MuscleJob j, string name, System.Func<IEnumerator> doIt)
        {
            var foot = JobBounds(j); var with = SitsWith(j, foot);
            Log($"  ({name}: footprint {Fmt(foot.min)}..{Fmt(foot.max)}; with it: {(with.Count == 0 ? "nothing" : string.Join(", ", with.Select(r => r.name)))})");
            yield return Shot($"510_{Game.World.worldId}_{j.id}_before");
            var states = new List<string>(); var mm = P.Companion; int seenShown = 0, seenAll = 0;
            var run = doIt(); bool going = true;
            Game.I.StartCoroutine(Sample());
            IEnumerator Sample() { while (going) { if (mm && mm.Anim && Game.I.Busy) { states.Add(mm.Anim.State); seenAll++; if (mm.Body.gameObject.activeInHierarchy) seenShown++; if (states.Count == 8) Log($"  ({name}: sampled {mm.Anim.name}, active {mm.Anim.isActiveAndEnabled}, at {Fmt(mm.transform.position)}, mode {mm.Motor.Mode})"); } yield return Wait(.1f); } }
            yield return run; going = false;
            var seen = states.Distinct().ToList();
            Ok(states.Count > 0 && !states.Any(s => SwimLike.Contains(s)) && MuscleJob.LastClip != "(none)" && states.Contains(MuscleJob.LastClip),
                $"{name}: Muscle Man plays his job's clip {MuscleJob.LastClip} (clips while working: {string.Join(", ", seen)})");
            // (only what moves can leave something behind: a bent cage stays where it is; what lies below the object's base is what it uncovers)
            Ok(seenAll > 0 && seenShown >= seenAll * .8f, $"{name}: Muscle Man is in view while he works ({seenShown} of {seenAll} samples shown; he stood {MuscleJob.LastStand})");
            var left = j is Boulder ? SitsWith(j, foot).Where(r => with.Contains(r) && r.bounds.center.y > foot.min.y - .05f).ToList() : new List<Renderer>();
            Ok(left.Count == 0, $"{name}: nothing left behind where it stood{(left.Count > 0 ? ": " + string.Join(", ", left.Select(r => r.name + " " + Fmt(r.bounds.center))) : "")}");
            yield return Wait(.6f);
            yield return Shot($"511_{Game.World.worldId}_{j.id}_after");
        }

        // ---------------------------------------------------------------- item 5: held still, nobody runs
        IEnumerator StandStill()
        {
            yield return WaitFor(() => !Game.I.Busy, 6f);
            var who = P.Everyone.Where(h => h && h.Anim).ToList();
            // walk a few metres so the followers carry speed, then hold the party (a cutscene / the panel freeze) with that speed in their motors
            var dir = Player.transform.forward; Stick(StickFor(dir)); yield return Wait(1.0f);
            P.Frozen = true; Stick(Vector2.zero);
            foreach (var h in who) if (h.Motor) h.Motor.Velocity = h.transform.forward * 5f;   // what a frozen motor kept before
            yield return Wait(1.0f);
            var moving = who.Where(h => h.Anim.State == "walk" || h.Anim.State == "run" || h.Anim.State == "run_slow").ToList();
            Ok(moving.Count == 0, $"{Game.World.displayName}: the party held still (frozen with speed left in their motors): nobody runs in place ({string.Join(", ", who.Select(h => h.Stats.displayName + " " + h.Anim.State))})");
            P.Frozen = false;
            // and the busy hold (a message, a prompt): input off, the stick still pushed
            Game.I.SetBusy(true); P.InputEnabled = false; Stick(StickFor(dir)); yield return Wait(1.0f);
            moving = who.Where(h => h.Anim.State == "walk" || h.Anim.State == "run" || h.Anim.State == "run_slow").ToList();
            Ok(moving.Count == 0 || moving.All(h => h.Anim.RealSpeed > .3f), $"{Game.World.displayName}: input held off: nobody runs on the spot ({string.Join(", ", who.Select(h => $"{h.Stats.displayName} {h.Anim.State} {h.Anim.RealSpeed:0.0} m/s"))})");
            Stick(Vector2.zero); P.InputEnabled = true; Game.I.SetBusy(false); yield return Wait(.3f);
        }
    }
}
