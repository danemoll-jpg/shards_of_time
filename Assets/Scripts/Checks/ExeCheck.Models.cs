using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace ShardsOfTime
{
    // Round 3, item 5: Dan and Abi animated, checked as Dan sees them (pad input, the player's camera), with numbers for what a picture
    // can't show: feet on the ground, feet not sliding while running, facing, the necklace never inside the clothes, outfits per area.
    public sealed partial class ExeCheck
    {
        IEnumerator Models()
        {
            yield return EnsurePlaying();
            Health.Refill();
            Head("Round 3: the imported models and clips (item 1)");
            var rep = Resources.Load<TextAsset>("Data/Models");
            if (rep) { foreach (var l in rep.text.Split('\n')) if (l.Trim().Length > 0) Ok(!l.Contains("FAILED"), l.Trim()); }
            else Fail("no model report in the build (Resources/Data/Models.txt)");

            Head("Round 3: Dan and Abi in Area A, home clothes (items 2, 3, 5)");
            foreach (int i in new[] { 0, 1 }) yield return OneHero(i, "home");
            yield return Followers("home");

            Head("Round 3: Area B, Ice Age outfits (item 2)");
            yield return SwitchTo(0);
            yield return TpSettle(new Vector3(396.5f, .1f, -12f), 180);
            yield return Wait(.3f);
            Ok(P.Heroes[0].Outfit == Outfits.IceAge && P.Heroes[1].Outfit == Outfits.IceAge && P.Heroes[0].Model.name.EndsWith("dan_iceage") && P.Heroes[1].Model.name.EndsWith("abi_iceage"),
               $"arriving in Area B dresses them for the Ice Age (Dan {P.Heroes[0].Model.name}, Abi {P.Heroes[1].Model.name}); Kira {(P.Heroes[2].Form ? P.Heroes[2].Form.id : "-")}");
            foreach (int i in new[] { 0, 1 }) yield return OneHero(i, "iceage", full: false);
            yield return Followers("iceage");

            yield return SwitchTo(0);
            yield return TpSettle(new Vector3(3f, .1f, -40f), 0);
            yield return Wait(.3f);
            Ok(P.Heroes[0].Outfit == Outfits.Home && P.Heroes[1].Outfit == Outfits.Home && P.Heroes[0].NecklaceGem && P.Heroes[0].NecklaceGem.GetComponentInParent<Animator>(),
               "back in Area A: home clothes again, the necklace moved onto the home model");
        }

        IEnumerator OneHero(int i, string outfit, bool full = true)
        {
            yield return SwitchTo(i);
            var h = Player; string n = h.Stats.displayName.ToLowerInvariant(), tag = $"r3_{n}_{outfit}";
            bool areaB = outfit == "iceage";
            Vector3 start = areaB ? new Vector3(396.5f, .1f, -12f) : new Vector3(3f, .1f, -47f);
            Ok(h.Model && h.Model.name == $"Model {n}_{outfit}" && h.Anim && h.Anim.Humanoid && h.Anim.ClipCount == CreatureAnimator.PeopleClips.Length && !h.Graybox.gameObject.activeSelf,
               $"{h.Stats.displayName} is the {outfit} model ({(h.Model ? h.Model.name : "none")}, humanoid {(h.Anim && h.Anim.Humanoid)}, {(h.Anim ? h.Anim.ClipCount : 0)} of {CreatureAnimator.PeopleClips.Length} clips, hips {(h.Anim ? h.Anim.HipsHeight : 0):0.00} m), the capsule hidden");

            // facing the camera, standing: the camera is north (yaw 180: it sits behind him), he turns to it
            yield return TpSettle(start, 180);
            Stick(StickFor(Vector3.forward) * .25f); yield return Wait(.3f); Stick(Vector2.zero); yield return Wait(1.0f);
            float foot = FootHeight(h);
            Ok(h.Anim.State == "idle" && Mathf.Abs(foot) < .06f, $"{h.Stats.displayName} standing: clip '{h.Anim.State}', lowest toe {foot * 100:0.0} cm off the ground (sinking < 0, floating > 0)");
            Log("    (" + FootDiag(h) + ")");
            Ok(Facing(h) > .95f, $"{h.Stats.displayName}'s model faces the hero's way (model forward . hero forward = {Facing(h):0.00})");
            yield return Shot(tag + "_idle_front");
            if (h.Stats.displayName == "Dan") yield return NecklaceCloseUp(h, tag + "_necklace_closeup");

            // running at the camera (a front view): the gait blend, feet not sliding, the necklace never inside the clothes
            Stick(StickFor(Vector3.forward)); yield return Wait(.7f);
            yield return Shot(tag + "_run_front");
            var slide = new List<float>(); var along = new List<float>(); float neckDeep = -1; int samples = 0;
            var feet = new[] { h.Anim.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.LeftToes), h.Anim.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.RightToes) };
            Vector3[] last = feet.Select(f => f ? f.position : Vector3.zero).ToArray();
            float t0 = Time.time; string runState = h.Anim.State;
            while (Time.time - t0 < 1.0f)
            {
                yield return null;
                float dt = Time.deltaTime; if (dt <= 0) continue;
                float ground = h.transform.position.y - h.Motor.Cc.skinWidth;
                for (int k = 0; k < 2; k++)
                {
                    if (!feet[k]) continue;
                    var p = feet[k].position;
                    if (p.y - ground < .03f && last[k].y - ground < .03f) { var d = p - last[k]; d.y = 0; slide.Add(d.magnitude / dt); along.Add(Vector3.Dot(d, h.transform.forward) / dt); }   // a planted foot
                    last[k] = p;
                }
                if (h.Stats.displayName == "Dan" && samples++ % 6 == 0) neckDeep = Mathf.Max(neckDeep, NecklaceDepth(h));
            }
            float speed = new Vector3(M.Velocity.x, 0, M.Velocity.z).magnitude;
            slide.Sort(); float med = slide.Count > 0 ? slide[slide.Count / 2] : -1;
            along.Sort(); float fwdMed = along.Count > 0 ? along[along.Count / 2] : 0;
            Ok(runState == "run" && med >= 0 && med < .12f * speed,
               $"{h.Stats.displayName} running {speed:0.0} m/s: clip '{runState}' at x{h.Anim.GaitRate:0.00} (made for {h.Anim.GaitSpeed:0.0} m/s); planted foot slides {med:0.00} m/s (median of {slide.Count}; {fwdMed:+0.00;-0.00} m/s along his run: + = the clip is too slow; limit 12% of the speed)");
            if (h.Stats.displayName == "Dan")
                Ok(neckDeep < .005f, $"the necklace's shard stays outside the {outfit} clothes while running (deepest {neckDeep * 100:0.0} cm {(neckDeep > 0 ? "inside" : "in front")}; sampled every 6th frame)");
            Ok(Facing(h) > .95f, $"running: model forward . hero forward = {Facing(h):0.00}");
            Stick(Vector2.zero); yield return Wait(.6f);

            // a jump, from the side-ish (the camera behind, he has turned back to it: a back-3/4 view)
            Btn(GamepadButton.South, true); yield return Wait(.22f);
            string up = h.Anim.State; yield return Shot(tag + "_jump");
            Btn(GamepadButton.South, false);
            yield return WaitFor(() => h.Motor.Grounded, 3f); yield return null; yield return null;
            string landed = h.Anim.State;
            Ok(up == "jump" && landed == "land", $"{h.Stats.displayName} jumps: '{up}' going up, '{landed}' on landing ({h.Motor.LandSpeed:0.0} m/s)");
            yield return Wait(.8f);

            // the attack, standing then running (Dan's punch goes on the upper body while he runs)
            yield return Press(GamepadButton.West); yield return Wait(.12f);
            string atk = h.Anim.State; yield return Shot(tag + "_attack");
            Ok(atk == (h.Stats.displayName == "Dan" ? "punch" : "kick"), $"{h.Stats.attackName}: clip '{atk}'");
            yield return Wait(1.0f);
            if (!full) yield break;

            // across the screen: the camera turned 90 degrees with the right stick, then a run to the right of the view
            Look(new Vector2(1, 0)); yield return Wait(.56f); Look(Vector2.zero); yield return Wait(.1f);
            var right = Quaternion.Euler(0, CameraRig.I.Yaw + 90f, 0) * Vector3.forward;
            Stick(StickFor(right)); yield return Wait(.75f);
            yield return Shot(tag + "_run_side");
            if (h.Stats.displayName == "Dan") { yield return Press(GamepadButton.West); yield return Wait(.15f); yield return Shot(tag + "_punch_running"); Ok(h.Anim.State == "punch", $"punch while running: '{h.Anim.State}' on the upper body, legs still running"); }
            Stick(Vector2.zero); yield return Wait(.6f);

            // swimming: the pool (water top -0.3 m)
            yield return TpSettle(new Vector3(30f, -1.6f, 2f), 0);
            yield return WaitFor(() => M.Mode == MoveMode.Swim, 2f); yield return Wait(1.2f);
            string tread = h.Anim.State; yield return Shot(tag + "_tread");
            Stick(StickFor(Vector3.right)); yield return Wait(1.0f);
            string swim = h.Anim.State; yield return Shot(tag + "_swim");
            Stick(Vector2.zero);
            Ok(tread == "tread" && swim == "swim", $"{h.Stats.displayName} in the pool: '{tread}' still, '{swim}' moving (model raised {h.Model.transform.localPosition.y:0.00} m to the surface)");
            yield return TpSettle(start, 180); yield return Wait(.3f);

            if (h.Stats.displayName == "Dan")
            {
                yield return TpSettle(new Vector3(-14f, .1f, Object.FindObjectsByType<PushBlock>().First().transform.position.z - 2.4f), 0);
                Stick(StickFor(Vector3.forward)); yield return Wait(1.6f);
                string push = h.Anim.State; yield return Shot(tag + "_push");
                Stick(Vector2.zero);
                Ok(push == "push", $"Dan at the heavy block: clip '{push}'");
            }
            // a fall from 4 m lands hard
            yield return TpSettle(start + Vector3.up * 4.5f, 180);
            yield return WaitFor(() => h.Motor.Grounded, 3f); yield return null; yield return null;
            Ok(h.Anim.State == "land_hard", $"a {4.5f:0.0} m drop lands hard: '{h.Anim.State}' ({h.Motor.LandSpeed:0.0} m/s)");
            yield return Wait(.5f);
            // a shard pickup: the cheer
            h.Cheer(); yield return Wait(.6f);
            string cheer = h.Anim.State; yield return Shot(tag + "_cheer");
            Ok(cheer == "cheer", $"cheer: '{cheer}'");
            yield return Wait(1.5f);
        }

        IEnumerator Followers(string outfit)
        {
            yield return SwitchTo(2);
            var start = outfit == "iceage" ? new Vector3(396.5f, .1f, -12f) : new Vector3(3f, .1f, -47f);
            yield return TpSettle(start, 0);
            Stick(StickFor(Vector3.forward)); yield return Wait(1.6f); Stick(Vector2.zero);
            yield return Wait(.15f); yield return Shot($"r3_followers_{outfit}_running");
            yield return Wait(1.6f);
            // turn round to look at them
            Stick(StickFor(Vector3.back) * .25f); yield return Wait(.3f); Stick(Vector2.zero); yield return Wait(.6f);
            yield return Shot($"r3_followers_{outfit}");
            var people = P.Heroes.Where(x => x.IsPerson).ToList();
            Ok(people.All(x => x.Model && x.Model.activeInHierarchy && x.Anim.State == "idle" && Mathf.Abs(FootHeight(x)) < .06f && Facing(x) > .95f),
               $"followers ({outfit}): " + string.Join("; ", people.Select(x => $"{x.Stats.displayName} '{(x.Anim ? x.Anim.State : "-")}', toe {FootHeight(x) * 100:0.0} cm, facing {Facing(x):0.00}")));
            foreach (var x in people) Log($"    ({x.Stats.displayName}: {FootDiag(x)})");
            // switching: the new hero takes the spot, the camera keeps going
            yield return Press(GamepadButton.RightShoulder); yield return null; yield return null;
            yield return Shot($"r3_switch_{outfit}");
            Ok(Player.Stats.displayName == "Dan" && Player.Model.activeInHierarchy, $"RB from Kira: {Player.Stats.displayName} ({Player.Model.name})");
            yield return Wait(.5f);
        }

        // the lowest toe over the ground under the hero (m)
        static float FootHeight(Hero h)
        {
            var a = h.Model ? h.Model.GetComponentInChildren<Animator>() : null;
            if (!a) return 99f;
            float toe = Mathf.Min(a.GetBoneTransform(HumanBodyBones.LeftToes).position.y, a.GetBoneTransform(HumanBodyBones.RightToes).position.y);
            float ground = h.transform.position.y - h.Motor.Cc.skinWidth;
            if (Physics.Raycast(h.transform.position + Vector3.up * .5f, Vector3.down, out var hit, 2f, ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << 4) | (1 << 2)), QueryTriggerInteraction.Ignore)) ground = hit.point.y;
            return toe - ground - .02f;   // the toe bone sits about 2 cm above the sole
        }
        static string FootDiag(Hero h)
        {
            var a = h.Model.GetComponentInChildren<Animator>();
            return $"toes L {a.GetBoneTransform(HumanBodyBones.LeftToes).position.y:0.000} R {a.GetBoneTransform(HumanBodyBones.RightToes).position.y:0.000}, feet L {a.GetBoneTransform(HumanBodyBones.LeftFoot).position.y:0.000} R {a.GetBoneTransform(HumanBodyBones.RightFoot).position.y:0.000}, hips {a.GetBoneTransform(HumanBodyBones.Hips).position.y:0.000}, root {h.transform.position.y:0.000}, model {h.Model.transform.position.y:0.000}";
        }

        // the model's forward (from its legs: right leg on its right) against the hero's forward
        static float Facing(Hero h)
        {
            var a = h.Model ? h.Model.GetComponentInChildren<Animator>() : null;
            if (!a) return -1;
            var right = a.GetBoneTransform(HumanBodyBones.RightUpperLeg).position - a.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position; right.y = 0;
            var fwd = Vector3.Cross(right.normalized, Vector3.up);
            var hf = h.transform.forward; hf.y = 0;
            return Vector3.Dot(fwd.normalized, hf.normalized);
        }

        // how far the clothes reach in front of the shard (m): > 0 = the shard is inside them. The skinned mesh as it is this frame, in
        // the body's space, the skin points right behind / in front of the shard (within 1.5 cm across).
        static float NecklaceDepth(Hero dan)
        {
            var shard = dan.NecklaceGem; if (!shard || !dan.Model) return -1;
            var body = dan.Body; var s = body.InverseTransformPoint(shard.position);
            float deepest = -1; var baked = new Mesh();
            foreach (var smr in dan.Model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                // only the torso's skin (vertices mostly on a spine / neck bone): the arms swinging past in front don't count
                var bw = smr.sharedMesh.boneWeights; var bones = smr.bones;
                smr.BakeMesh(baked, true);
                var m = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
                var verts = baked.vertices;
                for (int i = 0; i < verts.Length && i < bw.Length; i++)
                {
                    var bn = bones[bw[i].boneIndex0] ? bones[bw[i].boneIndex0].name : "";
                    if (!bn.Contains("Spine") && !bn.Contains("Neck")) continue;
                    var p = body.InverseTransformPoint(m.MultiplyPoint3x4(verts[i]));
                    if (Mathf.Abs(p.x - s.x) < .04f && Mathf.Abs(p.y - s.y) < .04f && p.z > s.z - .2f) deepest = Mathf.Max(deepest, p.z - s.z);
                }
            }
            Object.Destroy(baked);
            return deepest;
        }

        // an inspection close-up (not the player's view): a camera dist m from the target along `along`, a little above (Fix 3.1)
        IEnumerator CloseUp(Transform target, Vector3 along, float dist, string name)
        {
            if (!TestRun.Headless) yield break;
            yield return new WaitForEndOfFrame();
            var go = new GameObject("Close-up camera"); var cam = go.AddComponent<Camera>();
            cam.targetTexture = TestRun.View; cam.fieldOfView = 20f; cam.nearClipPlane = .05f;
            go.transform.position = target.position + along.normalized * dist + Vector3.up * .1f;
            go.transform.LookAt(target.position);
            TestRun.SaveView(cam, Path.Combine(dir, name + ".png"));
            Object.Destroy(go);
            Log($"  screenshot {name}.png (inspection close-up, not the player's view)");
        }

        // an inspection close-up (not the player's view): a camera 0.8 m in front of Dan's chest
        IEnumerator NecklaceCloseUp(Hero dan, string name)
        {
            if (!TestRun.Headless || !dan.NecklaceGem) yield break;
            yield return new WaitForEndOfFrame();
            var go = new GameObject("Close-up camera"); var cam = go.AddComponent<Camera>();
            cam.targetTexture = TestRun.View; cam.fieldOfView = 30f; cam.nearClipPlane = .05f;
            var target = dan.NecklaceGem.position;
            go.transform.position = target + dan.transform.forward * .8f + Vector3.up * .05f;
            go.transform.LookAt(target);
            TestRun.SaveView(cam, Path.Combine(dir, name + ".png"));
            Object.Destroy(go);
            Log($"  screenshot {name}.png (inspection close-up, not the player's view)");
        }
    }
}
