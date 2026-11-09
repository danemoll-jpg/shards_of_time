using UnityEngine;

namespace ShardsOfTime
{
    // Round 1, item 7 (rule 4): an inactive hero follows the player's trail a short way back, with the same motor (it swims in water).
    // No pathfinding: a follower more than ~12 m away, fallen well below, or not caught up within ~3 s (a jump or climb it can't make)
    // warps in just behind the player, out of the camera's view. Followers never block the player (their layer doesn't collide with
    // the player's: GameShell sets the physics matrix).
    public sealed class Follower : MonoBehaviour
    {
        public const float WarpDistance = 12f, StuckSeconds = 3f, NearEnough = 4f;
        public Hero Hero;
        public int Slot;                         // 0 = first behind the player, 1 = second
        public int Warps { get; private set; }
        public string LastWarpReason { get; private set; } = "";
        public float FarFor { get; private set; }
        float belowFor, blockedFor;

        float Gap => 1.6f + 1.4f * Slot;

        public void ResetTimers() { FarFor = belowFor = blockedFor = 0; }

        void Update()
        {
            var party = Party.I;
            if (!party || Hero.IsPlayer || Time.deltaTime <= 0 || party.Frozen) return;
            var me = Hero.Motor; var player = party.Player;
            var pos = transform.position; var ppos = player.transform.position;
            // a trail point a little way back, stepped to the side (left / right per slot) so followers flank the player instead of
            // standing between the camera and the player
            var target = party.TrailPoint(Gap);
            var along = party.TrailPoint(Gap - .5f) - target; along.y = 0;
            if (along.sqrMagnitude > .0001f) target += Vector3.Cross(Vector3.up, along.normalized) * (Slot == 0 ? -.9f : .9f);
            var to = target - pos; var flat = new Vector3(to.x, 0, to.z);
            float dist = flat.magnitude, toPlayer = Vector3.Distance(pos, ppos);

            var input = new MotorInput();
            if (dist > .5f) input.move = flat.normalized * Mathf.Clamp(dist / 1.5f, .35f, 1f);
            me.SpeedScale = toPlayer > 3.5f ? 1.35f : 1f;
            bool wantsUp = to.y > .5f && dist < 3.5f;
            if (me.Mode == MoveMode.Swim)
            {
                input.diveHeld = to.y < -.8f;
                input.jumpHeld = to.y > .8f || (wantsUp && dist < 1.5f);
                input.jumpPressed = wantsUp && dist < 1.8f && player.Motor.Mode != MoveMode.Swim;   // climb out where the player did
            }
            else
            {
                bool blocked = input.move.sqrMagnitude > .1f && new Vector3(me.Velocity.x, 0, me.Velocity.z).magnitude < .6f;
                blockedFor = blocked && me.Grounded ? blockedFor + Time.deltaTime : 0;
                bool gap = me.Grounded && input.move.sqrMagnitude > .1f && !Physics.Raycast(pos + Vector3.up * .3f + flat.normalized * .9f, Vector3.down, 2.5f, ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer)), QueryTriggerInteraction.Ignore);
                if (me.Grounded && (wantsUp || blockedFor > .3f || gap && (to.y > -1f))) { input.jumpPressed = true; blockedFor = 0; }
                input.jumpHeld = !me.Grounded && me.Velocity.y > 0;
            }
            me.Step(input, Time.deltaTime);

            // warps
            FarFor = toPlayer > NearEnough ? FarFor + Time.deltaTime : 0;
            belowFor = ppos.y - pos.y > 4f ? belowFor + Time.deltaTime : 0;
            if (pos.y < Party.KillY || pos.y < Party.I.Player.transform.position.y - 25f) WarpNearPlayer("fell out of the world");
            else if (toPlayer > WarpDistance) WarpNearPlayer($"{toPlayer:0} m behind");
            else if (belowFor > 1.5f) WarpNearPlayer("fell below");
            else if (FarFor > StuckSeconds) WarpNearPlayer($"not caught up in {StuckSeconds:0} s");
        }

        // Just behind the player, out of the camera's view if any such spot is free; else the trail point behind the player.
        public void WarpNearPlayer(string reason, Hero playerOverride = null)
        {
            var party = Party.I; var player = playerOverride ? playerOverride : party.Player;
            var ppos = player.transform.position; var cam = CameraRig.I ? CameraRig.I.Cam : Camera.main;
            Vector3 best = Vector3.zero; float bestScore = float.MaxValue; bool found = false;
            var back = cam ? Vector3.ProjectOnPlane(cam.transform.position - ppos, Vector3.up).normalized : -player.transform.forward;
            if (back.sqrMagnitude < .01f) back = -player.transform.forward;
            for (int ring = 0; ring < 3; ring++)
                for (int k = 0; k < 16; k++)
                {
                    float r = 2.2f + ring * 1.2f, ang = k * 22.5f;
                    var dir = Quaternion.Euler(0, ang, 0) * back;
                    var p = ppos + dir * r;
                    if (!Ground(ref p, ppos.y)) continue;
                    if (!Party.Fits(Hero, p)) continue;
                    if (Physics.Linecast(ppos + Vector3.up * .4f, p + Vector3.up * .4f, ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << 4)), QueryTriggerInteraction.Ignore)) continue;   // same side of the wall as the player
                    bool hidden = cam && !Visible(cam, p);
                    float score = (hidden ? 0 : 1000) + Vector3.Angle(dir, back) * .05f + r;
                    if (score < bestScore) { bestScore = score; best = p; found = true; }
                }
            if (!found) best = player == party.Player ? party.TrailPoint(1.5f) : ppos;
            Hero.Motor.Teleport(best, player.transform.rotation);
            ResetTimers();
            Warps++; LastWarpReason = reason;
            PlayLog.Line($"follower {Hero.Stats.displayName} warped ({reason}){(found && bestScore < 1000 ? ", out of view" : found ? ", in view (no hidden spot)" : ", trail point")}");
        }

        static bool Visible(Camera cam, Vector3 p)
        {
            var v = cam.WorldToViewportPoint(p + Vector3.up * .5f);
            if (v.z <= 0 || v.x < -.02f || v.x > 1.02f || v.y < -.02f || v.y > 1.02f) return false;
            // in the frame but behind a wall counts as hidden
            return !Physics.Linecast(cam.transform.position, p + Vector3.up * .5f, ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << 4) | (1 << 2)), QueryTriggerInteraction.Ignore);
        }

        static bool Ground(ref Vector3 p, float nearY)
        {
            if (WaterVolume.At(p + Vector3.up * .5f)) return true;
            if (Physics.Raycast(new Vector3(p.x, nearY + 2.5f, p.z), Vector3.down, out var hit, 5f, ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << 4) | (1 << 2)), QueryTriggerInteraction.Ignore) && hit.normal.y > .7f)
            { p = hit.point + Vector3.up * .05f; return true; }
            return false;
        }
    }
}
