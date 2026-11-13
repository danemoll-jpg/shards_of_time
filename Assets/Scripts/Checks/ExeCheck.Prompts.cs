using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 12 item 0b: every Muscle Man job prompt, in every world. Dan saw "Muscle Man rolls the boulder away" in Egypt's walled quarry with
    // no boulder in sight (it measured from the 12 m block's centre and never looked at the screen). For each job in each world:
    //  1. from in front of it (a spot found around its surface) the prompt appears, and the object is on screen and unhidden,
    //  2. turned away (the camera behind, the object at the player's back) the prompt goes within a second and stays gone,
    //  3. from 6+ m off its surface, or behind a wall, no prompt,
    //  4. the job done with the attack button: the prompt goes and never comes back there,
    //  5. after a reload (the world rebuilt from the save) the job is still done and offers nothing.
    // Plus Dan's spot: inside Egypt's quarry, facing east, the block behind him: no prompt.
    public sealed partial class ExeCheck
    {

        IEnumerator PromptsAll()
        {
            yield return EnsurePlaying(false);
            if (!SaveGame.Current.Has(Abilities.MuscleMan)) SaveGame.Current.abilities.Add(Abilities.MuscleMan);
            if (!P.Companion) P.AddCompanion(Player.transform.position + Vector3.back * 3f, Quaternion.identity);
            yield return PromptWorld(WorldData.IceAge, null);
            yield return PromptWorld(WorldData.Egypt, Egypt.Start);
            yield return PromptWorld(WorldData.Bethlehem, Bethlehem.Start);
            yield return PromptWorld(WorldData.Medieval, Medieval.Start);
            yield return PromptWorld(WorldData.Pirate, Pirate.Start);
            yield return PromptWorld(WorldData.Steampunk, Steampunk.FactoryCp);
            foreach (var (w, cp) in ExtraPromptWorlds) yield return PromptWorld(w, cp);
        }
        // Round 12 / 13 add their worlds here
        static readonly List<(string, string)> ExtraPromptWorlds = new List<(string, string)> { (WorldData.Tomorrowland, Tomorrowland.Start), (WorldData.Moon, Moon.Start) };

        bool PromptUp(MuscleJob j) => Hud.LastMessage == j.Prompt && MuscleJob.Prompting == j;

        // a spot about 1.8 m from the job's surface, on the ground, free, from which it's offered (facing it)
        bool FrontSpot(MuscleJob j, out Vector3 at, out float yaw)
        {
            at = default; yaw = 0;
            var centre = j.transform.position; float baseY = j.transform.position.y;
            var r = j.GetComponentsInChildren<Renderer>().Where(x => x.enabled).Select(x => x.bounds).ToList();
            if (r.Count > 0) { var b = r[0]; foreach (var x in r) b.Encapsulate(x); centre = b.center; baseY = b.min.y; }
            if (j is BoostSpot) { centre = j.transform.position; baseY = j.transform.position.y; }
            float ext = Mathf.Max(.6f, (j.ClosestPoint(centre + Vector3.right * 30f) - centre).magnitude);
            for (float d = 1.6f; d <= 3.2f; d += .8f)
                for (int i = 0; i < 16; i++)
                {
                    var dir = Quaternion.Euler(0, i * 22.5f, 0) * Vector3.forward;
                    var p = centre + dir * 30f; var sp = j is BoostSpot ? centre : j.ClosestPoint(new Vector3(p.x, baseY + .9f, p.z)); sp.y = baseY;
                    var q = sp + dir * (j is BoostSpot ? d * .25f : d);
                    if (!Physics.Raycast(q + Vector3.up * 1.5f, Vector3.down, out var hit, 3f, SolidMask, QueryTriggerInteraction.Ignore)) continue;
                    if (hit.collider.transform.IsChildOf(j.transform) || Mathf.Abs(hit.point.y - baseY) > 1.0f) continue;
                    var foot = hit.point + Vector3.up * .05f;
                    if (Physics.CheckCapsule(foot + Vector3.up * .5f, foot + Vector3.up * 1.4f, .35f, SolidMask, QueryTriggerInteraction.Ignore)) continue;
                    var near = j is BoostSpot ? centre + j.transform.forward : j.ClosestPoint(foot + Vector3.up * .9f);
                    var look = near - foot; look.y = 0; if (look.sqrMagnitude < .01f) continue;
                    at = foot; yaw = Quaternion.LookRotation(look).eulerAngles.y; return true;
                }
            return false;
        }

        IEnumerator PromptWorld(string world, string cp)
        {
            if (cp != null) yield return ToWorld(world, cp);
            if (world == WorldData.Pirate) Pirate.OpenDeepSea(true);
            if (!P.Companion) P.AddCompanion(Player.transform.position + Vector3.back * 3f, Quaternion.identity);
            yield return Wait(.5f);
            var jobs = MuscleJob.All.Where(j => j && j.isActiveAndEnabled && !string.IsNullOrEmpty(j.id)).ToList();
            Head($"Round 12, item 0b: Muscle Man's prompts in {Game.World.displayName} ({jobs.Count} jobs that stay done; boost spots can be used again and are checked for steps 1-3 only)");
            var all = MuscleJob.All.Where(j => j && j.isActiveAndEnabled).ToList();
            foreach (var j in all)
            {
                string name = $"{j.GetType().Name} {j.id ?? j.name}";
                if (j.IsDone) { Log($"  ({name}: already done in this save)"); continue; }
                yield return SwitchTo(0);
                if (!FrontSpot(j, out var at, out var yaw)) { Fail($"{name}: no free spot in front of it to stand on"); continue; }
                // 1. in front
                yield return TpSettle(at, yaw); Stick(Vector2.zero);
                yield return WaitFor(() => PromptUp(j), 4f);
                if (!PromptUp(j))
                {
                    // nudge forward a little (a ledge or a lip in the way of the camera)
                    yield return Go(j.ClosestPoint(Player.transform.position), 1.1f, 2.5f, .4f); yield return WaitFor(() => PromptUp(j), 3f);
                }
                bool up = PromptUp(j);
                Ok(up, $"{name}: in front of it the prompt shows ({(up ? j.Prompt : j.WhyNot(Player) ?? "offered but not shown")}; at {Fmt(Player.transform.position)})");
                var vp = CameraRig.I.Cam.WorldToViewportPoint(j.ClosestPoint(Player.transform.position + Vector3.up * .9f));
                Ok(!up || (vp.z > 0 && vp.x > 0 && vp.x < 1 && vp.y > 0 && vp.y < 1), $"{name}: the object is on screen while it's offered (viewport {vp.x:0.00}, {vp.y:0.00})");
                if (up && jobs.Count > 0 && j == all.First()) yield return Shot($"500_{Game.World.worldId}_{j.id ?? "boost"}_prompt");
                // 2. turned away
                yield return TpSettle(Player.transform.position, yaw + 180f);
                yield return Wait(1.0f);
                bool gone = !PromptUp(j) && Hud.LastMessage != j.Prompt;
                yield return WaitFor(() => PromptUp(j), 3f);
                Ok(gone && !PromptUp(j), $"{name}: turned away from it, no prompt ({j.WhyNot(Player) ?? "OFFERED"})");
                // 3. far off
                var away = Player.transform.position + Quaternion.Euler(0, yaw + 180f, 0) * Vector3.forward * 6f;
                if (Physics.Raycast(away + Vector3.up * 4f, Vector3.down, out var gh, 12f, SolidMask, QueryTriggerInteraction.Ignore))
                {
                    yield return TpSettle(gh.point + Vector3.up * .05f, yaw); yield return WaitFor(() => PromptUp(j), 2.5f);
                    Ok(!PromptUp(j), $"{name}: 6 m further off, facing it, no prompt ({j.WhyNot(Player) ?? "OFFERED"})");
                }
                if (string.IsNullOrEmpty(j.id)) continue;   // a boost spot: used any number of times
                // 4. done
                yield return TpSettle(at, yaw); yield return WaitFor(() => PromptUp(j), 4f);
                yield return Press(AttackBtn); yield return WaitFor(() => Game.I.Busy, 3f); yield return WaitFor(() => !Game.I.Busy, 10f);
                Ok(j.IsDone, $"{name}: Muscle Man does it with the attack button");
                yield return TpSettle(at, yaw); yield return Wait(3.5f);
                Ok(!PromptUp(j) && Hud.LastMessage != j.Prompt, $"{name}: once done, no prompt where it was ({Hud.LastMessage})");
            }
            // 5. reload: save, rebuild the world from the save, nothing offered again
            SaveGame.Save("prompt check");
            var done = jobs.Where(j => j && j.IsDone).Select(j => j.id).ToList();
            var cpNow = SaveGame.Current.checkpoint;
            SaveGame.Load(); Game.I.BuildWorld(); yield return null; yield return null; yield return WaitFor(() => !Game.I.Busy, 10f); yield return Wait(1f);   // a real reload: the save from disk, the world rebuilt
            if (!P.Companion) P.AddCompanion(Player.transform.position + Vector3.back * 3f, Quaternion.identity);
            int shown0 = MuscleJob.PromptsShown;
            foreach (var j in MuscleJob.All.Where(j => j && done.Contains(j.id)).ToList())
            {
                Ok(j.IsDone, $"{j.id}: still done after the reload");
                if (FrontSpot(j, out var at, out var yaw)) { yield return TpSettle(at, yaw); yield return Wait(2f); Ok(!PromptUp(j), $"{j.id}: after the reload, no prompt in front of where it was"); }
            }
            Ok(MuscleJob.PromptsShown == shown0 || MuscleJob.All.Any(x => x && string.IsNullOrEmpty(x.id) && MuscleJob.Prompting == x), $"after the reload no done job prompted ({MuscleJob.PromptsShown - shown0} prompts)");
            _ = cpNow;
            if (world == WorldData.Egypt) yield return QuarrySpot();
        }

        // Dan's screenshot (2026-11-12): inside the quarry, the block behind him, the camera on the far wall
        IEnumerator QuarrySpot()
        {
            var b = Object.FindObjectsByType<Boulder>().FirstOrDefault(x => x.id == "EG-block-quarry");
            if (!b) { Fail("Egypt's quarry block is missing"); yield break; }
            var pr = SaveGame.Current.World(WorldData.Egypt); bool was = pr.opened.Remove("EG-block-quarry");
            if (was) { SaveGame.Save("prompt check: the block back"); SaveGame.Load(); Game.I.BuildWorld(); yield return null; yield return null; yield return WaitFor(() => !Game.I.Busy, 10f);
                if (!P.Companion) P.AddCompanion(Player.transform.position + Vector3.back * 3f, Quaternion.identity); b = Object.FindObjectsByType<Boulder>().First(x => x && x.isActiveAndEnabled && x.id == "EG-block-quarry"); }
            yield return SwitchTo(1);
            float gy = Game.OnGround(91f, 61f).y;
            yield return TpSettle(new Vector3(91f, gy + .05f, 61f), 90f); yield return Wait(4f);
            Ok(!PromptUp(b), $"Dan's spot: inside the quarry facing the far wall (the block 3.5 m behind), no prompt ({b.WhyNot(Player) ?? "OFFERED"})");
            yield return Shot("501_egypt_quarry_facing_away");
            yield return TpSettle(new Vector3(82f, Game.OnGround(82f, 61f).y + .05f, 61f), 90f); yield return WaitFor(() => PromptUp(b), 4f);
            Ok(PromptUp(b), $"outside the quarry facing the granite block: \"{Hud.LastMessage}\"");
            yield return Shot("502_egypt_quarry_block_prompt");
            if (was) SaveGame.Current.World(WorldData.Egypt).opened.Add("EG-block-quarry");
        }
    }
}
