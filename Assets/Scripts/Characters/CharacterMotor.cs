using System;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 1, item 4: one character motor with movement modes (Ground, Air, Swim, LowGravity), so Pirate Seas and the Moon plug in
    // without a rewrite (CLAUDE.md rule 7). The player and the followers drive the same motor; only where the input comes from differs.
    // Ground / Air: analog run, variable jump height, coyote time, jump buffering, slopes (steeper than the limit slides), riding
    // moving platforms. Swim: in a WaterVolume, surface swimming and diving (Jump = up, Dive = down), breath only for non-swimmers.
    // LowGravity: inside a LowGravityVolume gravity and jump scale (Ground / Air rules otherwise).
    public enum MoveMode { Ground, Air, Swim, LowGravity }

    public struct MotorInput
    {
        public Vector3 move;          // world space, length 0..1 (analog)
        public bool jumpPressed;      // this frame
        public bool jumpHeld;
        public bool diveHeld;
    }

    [RequireComponent(typeof(CharacterController))]
    public sealed class CharacterMotor : MonoBehaviour
    {
        public const float CoyoteTime = .12f, JumpBuffer = .15f, SlopeLimit = 46f, TerminalFall = 35f;

        public CharacterStats Stats { get; private set; }
        public CharacterController Cc { get; private set; }
        public MoveMode Mode { get; private set; } = MoveMode.Ground;
        public Vector3 Velocity;
        public bool Grounded { get; private set; }
        public float SpeedScale = 1f;                    // followers catching up run a bit faster
        public bool CanDoubleJump;                       // set by the party from the save (Abi's ability)
        public float Breath { get; private set; }        // seconds of air left (non-swimmers)
        public bool HeadUnderwater { get; private set; }
        public WaterVolume Water { get; private set; }
        public LowGravityVolume LowGravity { get; private set; }
        public float LastJumpHeight { get; private set; } // apex over take-off of the last finished jump (checks)
        public float LastAirTime { get; private set; }
        public int JumpsThisAir { get; private set; }
        public float LandSpeed { get; private set; }     // falling speed at the last landing (m/s): hard landings (Round 3)
        public float LastPush = -10f;                    // Time.time of the last push against a heavy block (the push clip)
        public event Action<string> Event;               // "jump", "doublejump", "land", "splash", "outofbreath", "leap"

        float coyote, buffer, leapTimer, takeoffY, apexY, airTime;
        bool rising; Vector3 groundNormal = Vector3.up; bool steep;
        Transform platform; Vector3 platformLocal; Quaternion platformRot;
        public Collider GroundCollider { get; private set; }

        public void Configure(CharacterStats s)
        {
            Stats = s; Cc = GetComponent<CharacterController>();
            Cc.height = s.height; Cc.radius = s.radius; Cc.center = new Vector3(0, s.height / 2f, 0);
            Cc.stepOffset = Mathf.Min(.35f, s.height * .4f); Cc.slopeLimit = SlopeLimit; Cc.skinWidth = Mathf.Max(.02f, s.radius * .1f); Cc.minMoveDistance = 0;
            Breath = s.breathSeconds;
        }

        public float Gravity => Stats.gravity * (LowGravity ? LowGravity.gravityScale : 1f);
        public Vector3 Center => transform.position + Vector3.up * Stats.height * .5f;
        public Vector3 Head => transform.position + Vector3.up * Stats.height * .9f;

        public void Teleport(Vector3 pos, Quaternion rot)
        {
            Cc.enabled = false; transform.SetPositionAndRotation(pos, rot); Cc.enabled = true;
            Velocity = Vector3.zero; platform = null; rising = false; buffer = 0; coyote = 0; Grounded = false;
            if (!Stats.swimmer) Breath = Stats.breathSeconds;
        }

        public void Knockback(Vector3 v) { Velocity = v; Grounded = false; rising = false; platform = null; coyote = 0; }

        public void Step(MotorInput input, float dt)
        {
            if (dt <= 0) return;
            CarryWithPlatform();
            Water = WaterVolume.At(transform.position + Vector3.up * Stats.height * .45f);
            LowGravity = LowGravityVolume.At(Center);
            leapTimer -= dt;
            bool swim = Water && leapTimer <= 0 && Water.Top - transform.position.y > Stats.height * .55f;
            if (swim && Mode != MoveMode.Swim) { if (Velocity.y < -4f) Event?.Invoke("splash"); rising = false; JumpsThisAir = 0; }
            Mode = swim ? MoveMode.Swim : LowGravity ? MoveMode.LowGravity : Grounded ? MoveMode.Ground : MoveMode.Air;
            if (swim) SwimStep(input, dt); else WalkStep(input, dt);
            Breathe(dt);
        }

        void WalkStep(MotorInput input, float dt)
        {
            var s = Stats; float g = Gravity;
            float jumpScale = LowGravity ? LowGravity.jumpScale : 1f;
            // horizontal: analog run toward the stick's speed
            var hv = new Vector3(Velocity.x, 0, Velocity.z);
            var target = Vector3.ClampMagnitude(input.move, 1f) * s.runSpeed * SpeedScale;
            float accel = Grounded ? s.acceleration * (target.sqrMagnitude < .01f ? 1.3f : 1f) : s.acceleration * s.airControl;
            hv = Vector3.MoveTowards(hv, target, accel * dt);
            float vy = Velocity.y;

            if (input.jumpPressed) buffer = JumpBuffer; else buffer -= dt;
            if (Grounded) coyote = CoyoteTime; else coyote -= dt;
            if (buffer > 0 && (Grounded || coyote > 0))
            {
                vy = Mathf.Sqrt(2f * s.gravity * s.jumpHeight) * jumpScale;   // take-off speed as on normal ground: low gravity = higher, floatier
                buffer = 0; coyote = 0; Grounded = false; rising = true; JumpsThisAir = 1; platform = null;
                takeoffY = apexY = transform.position.y; airTime = 0;
                Event?.Invoke("jump");
            }
            else if (input.jumpPressed && !Grounded && coyote <= 0 && CanDoubleJump && JumpsThisAir < 2)
            {
                vy = Mathf.Sqrt(2f * s.gravity * s.doubleJumpHeight) * jumpScale;
                buffer = 0; rising = true; JumpsThisAir = 2;
                Event?.Invoke("doublejump");
            }
            if (rising && !input.jumpHeld && vy > 0) { vy *= s.jumpCut; rising = false; }   // variable height: let go early, jump lower
            if (vy <= 0) rising = false;

            vy -= g * dt;
            if (Grounded && vy < 0 && !steep) vy = -2f;          // stay on the ground over slopes and steps
            vy = Mathf.Max(vy, -TerminalFall);
            if (steep)                                           // steeper than the slope limit: slide down it
            {
                var down = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
                hv += new Vector3(down.x, 0, down.z) * g * .6f * dt;
            }

            Velocity = new Vector3(hv.x, vy, hv.z);
            bool wasGrounded = Grounded; float vyBefore = Velocity.y;
            groundNormal = Vector3.up; GroundCollider = null;
            var flags = Cc.Move(Velocity * dt);
            bool below = (flags & CollisionFlags.Below) != 0;
            steep = below && Vector3.Angle(groundNormal, Vector3.up) > SlopeLimit;
            Grounded = below && !steep;
            if ((flags & CollisionFlags.Above) != 0 && Velocity.y > 0) { Velocity.y = 0; rising = false; }
            if (Grounded && Velocity.y < 0) Velocity.y = -2f;

            if (!Grounded) { airTime += Time.deltaTime; apexY = Mathf.Max(apexY, transform.position.y); }
            if (Grounded && !wasGrounded)
            {
                LastJumpHeight = apexY - takeoffY; LastAirTime = airTime; JumpsThisAir = 0; LandSpeed = -vyBefore;
                Event?.Invoke("land");
            }
            if (!Grounded && wasGrounded && !rising) { takeoffY = apexY = transform.position.y; airTime = 0; JumpsThisAir = Mathf.Max(JumpsThisAir, 0); }
            Face(hv, dt);
            TrackPlatform();
        }

        void SwimStep(MotorInput input, float dt)
        {
            var s = Stats;
            float surfaceFeet = Water.Top - s.height * .75f;     // floating: the top quarter out of the water
            float y = transform.position.y;
            bool atSurface = y >= surfaceFeet - .2f;
            var target = Vector3.ClampMagnitude(input.move, 1f) * s.swimSpeed * SpeedScale;
            if (input.diveHeld) target.y = -s.swimSpeed * .8f;
            else if (input.jumpHeld && !atSurface) target.y = s.swimSpeed * .8f;
            else if (atSurface || y > surfaceFeet - 1.2f) target.y = Mathf.Clamp((surfaceFeet - y) * 4f, -2f, 2.5f);   // float up to and at the surface
            else target.y = .5f;                                                                          // gentle rise
            if (atSurface && input.jumpPressed && !input.diveHeld)
            {
                // leap out (onto a low edge)
                Velocity = new Vector3(target.x, Mathf.Sqrt(2f * Stats.gravity * 2.0f), target.z);
                leapTimer = .5f; Mode = MoveMode.Air; rising = false; takeoffY = apexY = y; airTime = 0;
                Event?.Invoke("leap");
                Cc.Move(Velocity * dt); return;
            }
            Velocity = Vector3.MoveTowards(Velocity, target, 14f * dt);
            groundNormal = Vector3.up; GroundCollider = null;
            var flags = Cc.Move(Velocity * dt);
            Grounded = false; platform = null; steep = false;
            if (transform.position.y > surfaceFeet + .05f && !input.jumpPressed) { var p = transform.position; p.y = Mathf.Max(surfaceFeet, p.y - 3f * dt); Cc.enabled = false; transform.position = p; Cc.enabled = true; }
            Face(new Vector3(Velocity.x, 0, Velocity.z), dt);
            _ = flags;
        }

        void Breathe(float dt)
        {
            HeadUnderwater = Water && Head.y < Water.Top - .05f;
            if (Stats.swimmer) { Breath = 0; return; }
            if (HeadUnderwater)
            {
                Breath -= dt;
                if (Breath <= 0) { Breath = 3f; Event?.Invoke("outofbreath"); }
            }
            else Breath = Mathf.MoveTowards(Breath, Stats.breathSeconds, dt * Stats.breathSeconds / 1.5f);
        }
        public float Breath01 => Stats.swimmer || Stats.breathSeconds <= 0 ? 1f : Mathf.Clamp01(Breath / Stats.breathSeconds);

        void Face(Vector3 hv, float dt)
        {
            if (hv.sqrMagnitude < .04f) return;
            var want = Quaternion.LookRotation(new Vector3(hv.x, 0, hv.z));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, 900f * dt);
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // the ground: the hit below the capsule's lower sphere centre with the flattest normal
            if (hit.point.y < transform.position.y + Stats.radius && hit.normal.y > .05f)
            {
                if (GroundCollider == null || hit.normal.y > groundNormal.y) { groundNormal = hit.normal; GroundCollider = hit.collider; }
            }
            if (Mathf.Abs(hit.moveDirection.y) < .5f)
            {
                var push = hit.collider.GetComponentInParent<PushBlock>();
                if (push) push.Pushed(this, hit.moveDirection);
                var boulder = hit.collider.GetComponentInParent<Boulder>();
                if (boulder) boulder.Touched(this);
            }
        }

        // ---- moving platforms: ride along (position and turn) with whatever the character stands on
        void TrackPlatform()
        {
            platform = null;
            if (!Grounded || GroundCollider == null) return;
            var mp = GroundCollider.GetComponentInParent<MovingPlatform>();
            if (!mp) return;
            platform = mp.transform; platformLocal = platform.InverseTransformPoint(transform.position); platformRot = platform.rotation;
        }
        void CarryWithPlatform()
        {
            if (!platform) return;
            var to = platform.TransformPoint(platformLocal);
            var delta = to - transform.position;
            var turn = platform.rotation * Quaternion.Inverse(platformRot);
            float yaw = turn.eulerAngles.y;
            if (delta.sqrMagnitude > 0) Cc.Move(delta);
            if (Mathf.Abs(Mathf.DeltaAngle(0, yaw)) > .001f) transform.rotation = Quaternion.Euler(0, yaw, 0) * transform.rotation;
        }
        public bool OnPlatform => platform;
    }
}
