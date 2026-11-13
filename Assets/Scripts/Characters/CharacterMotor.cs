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
        public bool divePressed;      // this frame (Round 4: Dan's ground pound in the air)
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
        public bool CanGroundPound;                      // set by the party from the save (Dan's, Round 4)
        public bool CanDeepDive;                         // Round 5: everyone, once the deep dive is learned (Pirate Seas)
        // Round 7, item 7: Kira's pounce. On the ground, Dive = a short aim (she crouches and turns to the stick), then a long low leap
        // (17 m/s, 1.8 m high: about 11.8 m roof to roof; Abi's double jump reaches about 9.8). It keeps its speed through the air.
        public bool CanPounce;
        public bool Aiming => aimT > 0;
        public bool Pouncing { get; private set; }
        public int Pounces { get; private set; }
        public float LastPounceDistance { get; private set; }
        public const float PounceSpeed = 17f, PounceHeight = 1.8f, PounceAim = .28f;
        float aimT; Vector3 pounceDir, pounceFrom;
        // Round 8, item 6: dragon Kira's glide. Holding Jump while falling: her little wings hold the fall to GlideFall m/s and she keeps
        // (and steers) her run speed: about 7 m forward per metre down.
        public bool CanGlide;
        public bool Gliding { get; private set; }
        public int Glides { get; private set; }
        public float GlideDistance { get; private set; }   // the last glide's ground covered (checks)
        public const float GlideFall = 1.1f, GlideSpeed = 7.8f;
        Vector3 glideFrom;
        // Round 10: otter Kira. Swims 1.6x faster (any depth: CanDeepDive). Belly-slide: Dive while on the ground = a long slide on her tummy
        // (SlideSpeed, slowing over SlideTime; on a wet deck (WetDeck) it keeps its speed); Jump out of a slide carries its speed through the air.
        public bool Otter, CanBellySlide;
        public bool Sliding { get; private set; }
        public int Slides { get; private set; }
        public static int SlidesAll { get; private set; }   // (checks)
        public float LastSlideDistance { get; private set; }
        public const float SlideSpeed = 10.5f, SlideTime = 1.1f, OtterSwimScale = 1.6f;
        float slideT; Vector3 slideDir, slideFrom;
        // Round 10: the deep dive (everyone, the ship's fiddler): breath x3 under water; air bubbles (AirVolume) give it back
        public bool DeepBreath;
        // Round 11, item 7: Abi's wall jump (the Violinist, Steampunk). In the air, touching a wall: Jump = off the wall, up and away from
        // it (a chimney: bounce from wall to wall). Then her double jump is there again.
        public bool CanWallJump;
        public int WallJumps { get; private set; }
        public static int WallJumpsAll { get; private set; }   // (checks)
        public bool TouchingWall => wallT > 0;
        public Vector3 WallNormal => wallN;
        public const float WallJumpPush = 5.2f, WallGrace = .22f;
        float wallT; Vector3 wallN;
        // Round 12, item 7: Abi's air dash (Tomorrowland's Violinist). In the air, Dive = one quick dash the way the stick points (or ahead):
        // AirDashSpeed for AirDashTime, the fall held level, then it carries on like a throw. Once per time in the air (a wall jump gives it back).
        public bool CanAirDash;
        public bool Dashing => dashT > 0;
        public int AirDashes { get; private set; }
        public static int AirDashesAll { get; private set; }   // (checks)
        public const float AirDashSpeed = 13.5f, AirDashTime = .24f;
        float dashT; bool dashUsed;
        // Round 12, item 6b: robot Kira's wheel dash: the belly-slide on wheels (faster, it keeps its speed on any floor while it lasts)
        public bool Wheels;
        public const float WheelSpeed = 13.5f, WheelTime = 1.5f;
        // Round 13, item 4: space Kira's float: hold Jump in the air (once the jump stops rising) = drift up at FloatRise for up to FloatTime
        public bool CanFloat;
        public bool Floating { get; private set; }
        public int Floats { get; private set; }
        public const float FloatRise = 2.0f, FloatTime = 2.8f;   // Round 13: about 5.6 m on top of her low-gravity jump (Abi's double jump reaches ~6.8 m there; the float pillars are 7.6 m)
        float floatLeft;
        // Round 11, item 5: a conveyor belt under the feet carries the character (Conveyor.Push, world m/s)
        public Conveyor Belt { get; private set; }
        public float BreathMax => Stats.breathSeconds * (DeepBreath ? 3f : 1f);
        public bool Pounding { get; private set; }       // the ground pound: a short hang, then straight down
        public int Pounds { get; private set; }
        public const float PoundSpeed = 26f, PoundHang = .14f;
        public UpdraftVolume Updraft { get; private set; }   // Round 4: warm steam from a volcano vent lifts the character
        public float Breath { get; private set; }        // seconds of air left (non-swimmers)
        public bool HeadUnderwater { get; private set; }
        public WaterVolume Water { get; private set; }
        public LowGravityVolume LowGravity { get; private set; }
        public float LastJumpHeight { get; private set; } // apex over take-off of the last finished jump (checks)
        public float LastAirTime { get; private set; }
        public int JumpsThisAir { get; private set; }
        public float LandSpeed { get; private set; }     // falling speed at the last landing (m/s): hard landings (Round 3)
        public float LastPush = -10f;                    // Time.time of the last push against a heavy block (the push clip)
        public event Action<string> Event;               // "jump", "doublejump", "land", "splash", "outofbreath", "leap", "poundstart", "pound"

        float coyote, buffer, leapTimer, takeoffY, apexY, airTime, poundHang;
        // Round 6, item 2: lifted out of an ice hole onto the ice (scripted: up until clear of the edge, then across onto it)
        public bool Climbing { get; private set; }
        public int ClimbOuts { get; private set; }
        Vector3 climbTo; float climbTime, swimDeepest, swimTime; bool cameFromUnderIce;
        bool rising; Vector3 groundNormal = Vector3.up; bool steep; bool sideHit;
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
            Velocity = Vector3.zero; platform = null; rising = false; buffer = 0; coyote = 0; Grounded = false; Pounding = false; Climbing = false; swimDeepest = 0; swimTime = 0;
            aimT = 0; Pouncing = false; Gliding = false; keepAir = 0; Sliding = false; dashT = 0; Floating = false;
            if (!Stats.swimmer) Breath = BreathMax;
        }

        public void Knockback(Vector3 v) { Velocity = v; Grounded = false; rising = false; platform = null; coyote = 0; Pounding = false; aimT = 0; Pouncing = false; }
        // Round 7, item 6: lamb Kira's springy bounce (straight up to `height`, keeping her run)
        // Round 8: a throw (Muscle Man's boost) keeps its sideways speed until it lands (no air drag), like the pounce
        float keepAir;
        public void Throw(Vector3 sideways, float height, float keepFor = 2f) { Velocity = new Vector3(sideways.x, 0, sideways.z); Launch(height); keepAir = keepFor; }
        public void Launch(float height)
        {
            var hv = new Vector3(Velocity.x, 0, Velocity.z);
            Knockback(hv + Vector3.up * Mathf.Sqrt(2f * Gravity * height));
            takeoffY = apexY = transform.position.y; airTime = 0; JumpsThisAir = 1;
        }

        public float LastStepTime { get; private set; } = -10f;   // Fix 13.2: a motor nobody steps (a frozen party, Muscle Man at a job) stands
        public void Step(MotorInput input, float dt)
        {
            if (dt <= 0) return;
            LastStepTime = Time.time;
            if (Climbing) { ClimbStep(dt); Breathe(dt); return; }
            CarryWithPlatform();
            Water = WaterVolume.At(transform.position + Vector3.up * Stats.height * .45f);
            LowGravity = LowGravityVolume.At(Center);
            Updraft = UpdraftVolume.At(Center);
            leapTimer -= dt;
            bool swim = Water && leapTimer <= 0 && Water.Top - transform.position.y > Stats.height * .55f;
            if (swim && Mode != MoveMode.Swim) { if (Velocity.y < -4f) Event?.Invoke("splash"); rising = false; JumpsThisAir = 0; Pounding = false; Pouncing = false; aimT = 0; swimDeepest = 0; swimTime = 0; cameFromUnderIce = false; }
            Mode = swim ? MoveMode.Swim : LowGravity ? MoveMode.LowGravity : Grounded ? MoveMode.Ground : MoveMode.Air;
            if (swim) SwimStep(input, dt); else WalkStep(input, dt);
            Breathe(dt);
        }

        void WalkStep(MotorInput input, float dt)
        {
            var s = Stats; float g = Gravity;
            float jumpScale = LowGravity ? LowGravity.jumpScale : 1f;
            // horizontal: analog run toward the stick's speed
            var hv = new Vector3(Velocity.x, 0, Velocity.z); var hv0 = hv;
            var target = Vector3.ClampMagnitude(input.move, 1f) * s.runSpeed * SpeedScale;
            float accel = Grounded ? s.acceleration * (target.sqrMagnitude < .01f ? 1.3f : 1f) : s.acceleration * s.airControl;
            var slick = Grounded ? SlickIce.At(transform.position + Vector3.up * .2f) : null;   // Round 5: the frozen lake: slide
            if (slick) { accel *= slick.grip; target *= 1.25f; }
            var lane = slick ? new Vector3(slick.push.x, 0, slick.push.z) : Vector3.zero;
            if (lane.sqrMagnitude > .01f)
            {
                // a skating lane: slide along it (little grip), steer across it with full grip
                var f = lane.normalized; float hf = Vector3.Dot(hv, f), tf = Vector3.Dot(target, f);
                var hl = hv - f * hf; var tl = target - f * tf;
                hl = Vector3.MoveTowards(hl, tl, s.acceleration * dt); hf = Mathf.MoveTowards(hf, tf, accel * dt);
                hv = hl + f * hf + lane * dt;
            }
            else hv = Vector3.MoveTowards(hv, target, accel * dt);
            if (keepAir > 0) { keepAir -= dt; if (!Grounded) hv = hv0; else keepAir = 0; }   // Round 8: a throw flies on
            float vy = Velocity.y;

            // Round 4: the ground pound (Dan): Dive in the air = a short hang, then straight down; landing makes the "pound" event
            if (!Pounding && input.divePressed && !Grounded && CanGroundPound && !Updraft)
            {
                Pounding = true; poundHang = PoundHang; rising = false; buffer = 0; Event?.Invoke("poundstart");
            }
            if (Pounding) { hv = Vector3.zero; input.jumpPressed = false; }
            // Round 12: Abi's air dash
            if (CanAirDash && !Grounded && !dashUsed && input.divePressed && !Pounding && coyote <= 0)
            {
                var dir = input.move.sqrMagnitude > .04f ? new Vector3(input.move.x, 0, input.move.z).normalized : new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
                hv = dir * AirDashSpeed; hv0 = hv; dashT = AirDashTime; dashUsed = true; keepAir = AirDashTime + .25f; rising = false;
                AirDashes++; AirDashesAll++; transform.rotation = Quaternion.LookRotation(dir);
                Event?.Invoke("airdash"); if (CompareTag("Player")) PlayLog.Line($"air dash from {transform.position}");
            }
            if (dashT > 0) { dashT -= dt; vy = Mathf.Max(vy, 0f); }
            // Round 7: the pounce: aim on the ground, then leap
            if (CanPounce && aimT <= 0 && !Pouncing && Grounded && input.divePressed)
            {
                aimT = PounceAim; pounceDir = input.move.sqrMagnitude > .04f ? new Vector3(input.move.x, 0, input.move.z).normalized : transform.forward; Event?.Invoke("aim");
            }
            if (aimT > 0)
            {
                if (input.move.sqrMagnitude > .04f) pounceDir = Vector3.RotateTowards(pounceDir, new Vector3(input.move.x, 0, input.move.z).normalized, 8f * dt, 0);
                hv = Vector3.zero; input.jumpPressed = false; aimT -= dt;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(pounceDir), 900f * dt);
                if (aimT <= 0 && Grounded)
                {
                    Pouncing = true; Pounces++; pounceFrom = transform.position;
                    hv = pounceDir * PounceSpeed; vy = Mathf.Sqrt(2f * s.gravity * PounceHeight);
                    Grounded = false; rising = false; coyote = 0; buffer = 0; platform = null; JumpsThisAir = 1; takeoffY = apexY = transform.position.y; airTime = 0;
                    Event?.Invoke("pounce");
                }
                else if (aimT <= 0) aimT = 0;
            }
            else if (Pouncing)
            {
                var keep = pounceDir * PounceSpeed;   // a little steering, no slowing down
                hv = Vector3.MoveTowards(hv0, keep + Vector3.ProjectOnPlane(input.move, pounceDir) * 3f, 6f * dt);   // from last frame's speed: the air drag above doesn't apply
            }
            // Round 10: otter Kira's belly-slide
            if (CanBellySlide && Grounded && !Sliding && aimT <= 0 && input.divePressed)
            {
                Sliding = true; Slides++; SlidesAll++; slideT = Wheels ? WheelTime : SlideTime; slideFrom = transform.position;
                slideDir = input.move.sqrMagnitude > .04f ? new Vector3(input.move.x, 0, input.move.z).normalized : new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
                Event?.Invoke("slide"); if (CompareTag("Player")) PlayLog.Line($"belly-slide from {transform.position}");
            }
            if (Sliding)
            {
                bool wet = WetDeck.At(transform.position + Vector3.up * .2f);
                slideT -= dt;
                if (input.move.sqrMagnitude > .04f) slideDir = Vector3.RotateTowards(slideDir, new Vector3(input.move.x, 0, input.move.z).normalized, 1.6f * dt, 0);
                float sp = Wheels ? (slideT > 0 ? WheelSpeed : Mathf.Lerp(Stats.runSpeed, WheelSpeed, Mathf.Clamp01(1f + slideT / .4f))) : wet ? SlideSpeed : Mathf.Lerp(Stats.runSpeed * .6f, SlideSpeed, Mathf.Clamp01(slideT / SlideTime));   // Round 12: wheels keep their speed
                if (Wheels && BoostRamp.At(transform.position + Vector3.up * .2f) is BoostRamp ramp && slideT > -.3f) { hv = slideDir * sp; Velocity = hv; keepAir = 1.4f; EndSlide(); Launch(ramp.height); ramp.Launched++; vy = Velocity.y; Event?.Invoke("boost"); if (CompareTag("Player")) PlayLog.Line($"boost ramp {ramp.name}: launched {ramp.height} m"); }
                hv = slideDir * sp;
                if (input.jumpPressed && (Grounded || coyote > 0)) { keepAir = 1.2f; EndSlide(); }
                else if (!Sliding) { }
                else if ((slideT <= (Wheels ? -.4f : 0) && !wet) || slideT < -2.5f ||   // (a wet slide keeps going, but not into a wall for ever)
                          (!Grounded && coyote <= 0 && airTime > .3f)) { if (!Grounded) keepAir = 1.2f; EndSlide(); }
            }
            if (input.jumpPressed) buffer = JumpBuffer; else buffer -= dt;
            if (Grounded) { coyote = CoyoteTime; floatLeft = FloatTime; } else coyote -= dt;
            if (buffer > 0 && (Grounded || coyote > 0))
            {
                vy = Mathf.Sqrt(2f * s.gravity * s.jumpHeight) * jumpScale;   // take-off speed as on normal ground: low gravity = higher, floatier
                buffer = 0; coyote = 0; Grounded = false; rising = true; JumpsThisAir = 1; platform = null;
                takeoffY = apexY = transform.position.y; airTime = 0;
                Event?.Invoke("jump");
            }
            else if (input.jumpPressed && !Grounded && coyote <= 0 && CanWallJump && wallT > 0 && !Pounding)
            {
                // Round 11: off the wall: up as a normal jump, pushed away from the wall (kept for a moment so air control doesn't eat it)
                vy = Mathf.Sqrt(2f * s.gravity * s.jumpHeight * 1.08f) * jumpScale;
                hv = wallN * WallJumpPush; keepAir = .3f; hv0 = hv;
                buffer = 0; rising = true; JumpsThisAir = 1; wallT = 0; WallJumps++; WallJumpsAll++; dashUsed = false;
                transform.rotation = Quaternion.LookRotation(new Vector3(wallN.x, 0, wallN.z));
                Event?.Invoke("walljump");
            }
            else if (input.jumpPressed && !Grounded && coyote <= 0 && CanDoubleJump && JumpsThisAir < 2)
            {
                vy = Mathf.Sqrt(2f * s.gravity * s.doubleJumpHeight) * jumpScale;
                buffer = 0; rising = true; JumpsThisAir = 2;
                Event?.Invoke("doublejump");
            }
            if (rising && !input.jumpHeld && vy > 0) { vy *= s.jumpCut; rising = false; }   // variable height: let go early, jump lower
            if (vy <= 0) rising = false;

            // Round 13: space Kira's float: Jump held once the jump stops rising: up slowly for a while
            bool fl = CanFloat && !Grounded && input.jumpHeld && (Floating || vy <= .5f) && floatLeft > 0 && !Pounding;   // (once floating it goes on while Jump is held)
            if (fl && !Floating) { Floating = true; Floats++; Event?.Invoke("float"); if (CompareTag("Player")) PlayLog.Line($"Kira floats up from {transform.position}"); }
            else if (!fl) Floating = false;
            if (Floating) { floatLeft -= dt; vy = FloatRise; hv = Vector3.MoveTowards(hv, Vector3.ClampMagnitude(input.move, 1f) * 2.4f, 6f * dt); }
            // Round 8: the glide (dragon Kira): Jump held while falling
            bool glide = CanGlide && !Grounded && input.jumpHeld && vy <= 0 && !Pounding && !Pouncing && !Updraft && coyote <= 0;
            if (glide && !Gliding) { Gliding = true; Glides++; glideFrom = transform.position; Event?.Invoke("glide"); }
            else if (!glide && Gliding) { Gliding = false; var gd = transform.position - glideFrom; gd.y = 0; GlideDistance = gd.magnitude; }
            if (Gliding)
            {
                var want = input.move.sqrMagnitude > .04f ? new Vector3(input.move.x, 0, input.move.z).normalized * GlideSpeed : hv.normalized * Mathf.Min(hv.magnitude, GlideSpeed);
                hv = Vector3.MoveTowards(hv, want, 14f * dt);
            }
            if (Floating) { }
            else if (Pounding) { if (poundHang > 0) { poundHang -= dt; vy = 0; } else vy = -PoundSpeed; }
            else if (Gliding) vy = Mathf.Max(vy - g * dt, -GlideFall);
            else if (dashT > 0) vy = Mathf.Max(0f, vy - g * dt);   // Round 12: the dash holds the fall level
            else if (Updraft) { vy = Mathf.MoveTowards(vy, Updraft.lift, Updraft.accel * dt); rising = false; }   // steam: up to its lift speed
            else vy -= g * dt;
            if (Updraft && Grounded) { vy = Mathf.Max(vy, 1f); Grounded = false; }   // steam lifts you off the ground
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
            Belt = wasGrounded ? Conveyor.At(transform.position + Vector3.up * .2f) : null;   // Round 11: the belt carries what stands on it
            var carry = Belt ? Belt.Push : Vector3.zero;
            wallT -= dt; sideHit = false;
            var flags = Cc.Move((Velocity + carry) * dt);
            if (sideHit && !Grounded) wallT = WallGrace;
            bool below = (flags & CollisionFlags.Below) != 0;
            steep = below && Vector3.Angle(groundNormal, Vector3.up) > SlopeLimit;
            Grounded = below && !steep;
            if ((flags & CollisionFlags.Above) != 0 && Velocity.y > 0) { Velocity.y = 0; rising = false; }
            if (Grounded && Velocity.y < 0) Velocity.y = -2f;

            if (!Grounded) { airTime += Time.deltaTime; apexY = Mathf.Max(apexY, transform.position.y); }
            if (Grounded && !wasGrounded)
            {
                if (Gliding) { Gliding = false; var gd = transform.position - glideFrom; gd.y = 0; GlideDistance = gd.magnitude; }
                LastJumpHeight = apexY - takeoffY; LastAirTime = airTime; JumpsThisAir = 0; LandSpeed = -vyBefore; dashUsed = false; dashT = 0; Floating = false;
                if (Pounding) { Pounding = false; Pounds++; Event?.Invoke("pound"); }
                if (Pouncing) { Pouncing = false; var d = transform.position - pounceFrom; d.y = 0; LastPounceDistance = d.magnitude; }
                Event?.Invoke("land");
            }
            if (!Grounded && wasGrounded && !rising) { takeoffY = apexY = transform.position.y; airTime = 0; JumpsThisAir = Mathf.Max(JumpsThisAir, 0); }
            Face(hv, dt);
            TrackPlatform();
        }

        void EndSlide() { Sliding = false; var d = transform.position - slideFrom; d.y = 0; LastSlideDistance = d.magnitude; }

        void SwimStep(MotorInput input, float dt)
        {
            if (Sliding) EndSlide();
            var s = Stats;
            float surfaceFeet = Water.Top - s.height * .75f;     // floating: the top quarter out of the water
            float y = transform.position.y;
            bool atSurface = y >= surfaceFeet - .2f;
            float swimSpeed = s.swimSpeed * (Otter ? OtterSwimScale : 1f) * (Struggling ? .55f : 1f);   // Round 10: the otter's fast swim; Muscle Man's struggle
            var target = Vector3.ClampMagnitude(input.move, 1f) * swimSpeed * SpeedScale;
            if (input.diveHeld) target.y = -swimSpeed * .8f;
            else if (input.jumpHeld && !atSurface) target.y = swimSpeed * .8f;
            else if (atSurface || y > surfaceFeet - 1.2f) target.y = Mathf.Clamp((surfaceFeet - y) * 4f, -2f, 2.5f);   // float up to and at the surface
            else target.y = .5f;                                                                          // gentle rise
            swimTime += dt; swimDeepest = Mathf.Max(swimDeepest, surfaceFeet - y);
            // Round 6, item 2: up into an ice hole from under the ice = lifted out onto the ice; Jump in a hole = out too (just dropped in)
            var hole = IceHole.At(transform.position);
            if (!hole && IceHole.UnderIce(this)) cameFromUnderIce = true;
            if (hole && !input.diveHeld && (y > surfaceFeet - .6f && cameFromUnderIce && Velocity.y > .1f || y > surfaceFeet - 1.5f && input.jumpPressed))
            {
                ClimbOut(hole.ExitFrom(transform.position, input.move)); return;
            }
            // Fix 7.2: a well: up the rope and over the rim (Jump, or swimming into the wall; not the wall once divers can explore the bottom)
            var well = WellExit.At(transform.position);
            if (well && !input.diveHeld && (input.jumpPressed || input.jumpHeld || !CanDeepDive && input.move.sqrMagnitude > .2f && well.AtWall(transform.position)))   // any depth: the rope reaches down
            {
                ClimbOut(well.ExitFrom(transform.position, input.move), true); return;
            }
            var seaLock = SeaLock.At(transform.position);                                                  // Round 10: the deep sea before the shard goes overboard
            if (seaLock) { target.y = Mathf.Max(target.y, 2.4f); if (CompareTag("Player")) seaLock.Hint(); }
            var deep = DeepWater.At(transform.position);                                                   // Round 5: too deep without the deep dive
            if (deep && !CanDeepDive) { target.y = Mathf.Max(target.y, 2.6f); if (CompareTag("Player")) deep.Hint(); }
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

        public int WellClimbs { get; private set; }
        bool climbStraight;   // Fix 7.2: a well: straight up the rope until clear of the wall, then over
        public void ClimbOut(Vector3 onIce, bool straightUp = false)
        {
            climbStraight = straightUp; if (straightUp) WellClimbs++;
            if (CompareTag("Player")) PlayLog.Line($"climb out ({(straightUp ? "well" : "ice hole")}) from {transform.position} to {onIce}");
            climbTo = onIce; climbTime = 0; Climbing = true; ClimbOuts++; swimDeepest = 0; swimTime = 0; cameFromUnderIce = false;
            Velocity = Vector3.zero; Pounding = false; rising = false; Event?.Invoke("leap");
        }

        void ClimbStep(float dt)
        {
            climbTime += dt;
            var p = transform.position; var flat = climbTo - p; flat.y = 0;
            bool clear = p.y > climbTo.y + .15f;
            float vy = clear ? 0f : 6f;
            var hv = flat.magnitude > .05f ? flat.normalized * Mathf.Min(4.5f, flat.magnitude / dt) : Vector3.zero;
            if (!clear && (flat.magnitude < 1.4f || climbStraight)) hv = Vector3.zero;   // still below the edge: straight up first
            Velocity = new Vector3(hv.x, vy, hv.z);
            Cc.Move(Velocity * dt);
            Mode = MoveMode.Air; Grounded = false; platform = null;
            Face(hv, dt);
            if ((clear && flat.magnitude < .25f) || climbTime > (climbStraight ? 3.5f : 2.5f))
            {
                if (CompareTag("Player")) PlayLog.Line($"climb ended at {transform.position} after {climbTime:0.00} s{(climbTime > 2.5f ? " (timed out)" : "")}");
                Climbing = false; Velocity = Vector3.zero; leapTimer = .3f;
                takeoffY = apexY = transform.position.y; airTime = 0;
            }
        }

        void Breathe(float dt)
        {
            HeadUnderwater = Water && Head.y < Water.Top - .05f;
            if (Stats.swimmer) { Breath = 0; return; }
            if (HeadUnderwater && AirVolume.At(Head)) { Breath = Mathf.MoveTowards(Breath, BreathMax, dt * BreathMax / 1.2f); AirRefills += dt; return; }   // Round 10: air bubbles
            if (HeadUnderwater)
            {
                Breath -= dt;
                if (Breath <= 0) { Breath = 3f; Event?.Invoke("outofbreath"); }
            }
            else Breath = Mathf.MoveTowards(Breath, BreathMax, dt * BreathMax / 1.5f);
        }
        public float AirRefills { get; private set; }
        public bool Struggling;   // Round 10: Muscle Man under water (slow; his struggling clip)
        public float Breath01 => Stats.swimmer || Stats.breathSeconds <= 0 ? 1f : Mathf.Clamp01(Breath / BreathMax);

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
            // Round 11: a wall beside the character (for the wall jump): near-vertical, met while moving sideways
            if (Mathf.Abs(hit.normal.y) < .3f && hit.point.y > transform.position.y + .15f) { sideHit = true; wallN = new Vector3(hit.normal.x, 0, hit.normal.z).normalized; }
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
