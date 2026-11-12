using System.Collections.Generic;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 1, items 6 and 7: the three heroes, the one the player controls, instant switching (LB / RB, Q / E, 1 / 2 / 3: the new hero
    // takes the current spot, the camera keeps going) and the trail the followers walk. All three are always in the world (rule 4).
    [DefaultExecutionOrder(-10)]
    public sealed class Party : MonoBehaviour
    {
        public static Party I { get; private set; }
        public static float KillY => Game.InPirate ? -90f : -25f;   // Round 10: the Pirate Seas' floor goes down to -27
        public readonly List<Hero> Heroes = new List<Hero>();
        public Hero Player { get; private set; }
        public int Switches { get; private set; }
        public bool InputEnabled = true;
        public bool Frozen;                          // cutscenes: nobody moves
        public readonly List<Vector3> Trail = new List<Vector3>();   // the player's path, newest first, a point every 0.3 m
        // Round 8, item 11: Muscle Man, the companion after the stone (a follower, never controlled, not switchable: not in Heroes)
        public Hero Companion { get; private set; }
        // Round 8, item 7: Dan's charged punch: hold the attack (a normal punch first), the fist glows after ChargeTime, release = the big one
        public const float ChargeTime = .55f;
        public float Charge { get; private set; }
        public bool Charged => Charge >= ChargeTime;
        public int ChargedPunches { get; private set; }
        bool charging, chargeSound;

        public static Party Create(Vector3 start, Quaternion facing)
        {
            var go = new GameObject("Party"); var p = go.AddComponent<Party>(); I = p;
            string[] names = { "Dan", "Abi", "Kira" };
            for (int i = 0; i < 3; i++)
            {
                var stats = Resources.Load<CharacterStats>("Data/Characters/" + names[i]) ?? CharacterStats.Defaults(names[i]);
                var h = Hero.Create(stats, start - facing * Vector3.forward * (1.5f * i));
                h.transform.rotation = facing;
                h.transform.SetParent(go.transform, true);
                h.Follower.Slot = i - 1;
                p.Heroes.Add(h);
            }
            p.SetPlayer(0);
            p.ApplyAbilities();
            if (SaveGame.Current.Has(Abilities.MuscleMan)) p.AddCompanion(start - facing * Vector3.forward * 4.5f, facing);
            return p;
        }

        // Round 8: Muscle Man joins (after the stone; and on every load after that)
        public Hero AddCompanion(Vector3 at, Quaternion facing)
        {
            if (Companion) return Companion;
            var stats = Resources.Load<CharacterStats>("Data/Characters/MuscleMan") ?? CharacterStats.Defaults("MuscleMan");
            var h = Hero.Create(stats, at); h.transform.rotation = facing; h.transform.SetParent(transform, true);
            h.SetPlayer(false); h.Follower.Slot = 2;
            Companion = h;
            PlayLog.Line($"Muscle Man follows the party (model {(h.Model ? h.Model.name : "graybox")})");
            return h;
        }
        public IEnumerable<Hero> Everyone { get { foreach (var h in Heroes) yield return h; if (Companion) yield return Companion; } }

        public void ApplyAbilities()
        {
            foreach (var h in Heroes)
            {
                h.Motor.CanDoubleJump = h.Stats.lockedAbility == Abilities.DoubleJump && SaveGame.Current.Has(Abilities.DoubleJump);
                h.Motor.CanGroundPound = h.Stats.lockedAbility == Abilities.GroundPound && SaveGame.Current.Has(Abilities.GroundPound);   // Round 4
                h.Motor.CanBellySlide = h.HasAbility(Abilities.BellySlide);   // Round 10 (otter Kira)
                h.Motor.Otter = h.HasAbility(Abilities.OtterSwim);
                h.Motor.CanDeepDive = SaveGame.Current.Has(Abilities.DeepDive) || h.Motor.Otter;   // Round 5 (everyone); Round 10: otter Kira any depth
                h.Motor.DeepBreath = SaveGame.Current.Has(Abilities.DeepDive);   // Round 10: the deep dive's longer breath
                h.Motor.CanPounce = h.Stats.displayName == "Kira" && SaveGame.Current.Has(Abilities.Pounce) && !h.Motor.CanBellySlide;   // Round 7 (Kira, Bethlehem's Violinist); the otter slides instead
                h.Motor.CanGlide = h.HasAbility(Abilities.Glide);   // Round 8 (dragon Kira's wings; set again when her form changes)
                h.Motor.CanWallJump = h.Stats.displayName == "Abi" && SaveGame.Current.Has(Abilities.WallJump);   // Round 11 (Abi, the Steampunk Violinist)
            }
        }

        void SetPlayer(int i)
        {
            Player = Heroes[i];
            int slot = 0;
            foreach (var h in Heroes) { h.SetPlayer(h == Player); if (h != Player) h.Follower.Slot = slot++; }
        }

        public int Index => Heroes.IndexOf(Player);

        // Switch to hero i: it takes the current hero's place (position, facing, momentum); the old one takes its place as a follower.
        public bool Switch(int i)
        {
            if (i == Index || i < 0 || i >= Heroes.Count) return false;
            var old = Player; var nu = Heroes[i];
            var pos = old.transform.position; var rot = old.transform.rotation; var vel = old.Motor.Velocity; var nuPos = nu.transform.position;
            if (!Fits(nu, pos)) { Hud.Message($"No room for {nu.Stats.displayName} here."); return false; }
            nu.Motor.Teleport(pos, rot); nu.Motor.Velocity = vel;
            if (Fits(old, nuPos) && (nuPos - pos).sqrMagnitude < 15 * 15) old.Motor.Teleport(nuPos, rot); else old.Follower.WarpNearPlayer("switched out", nu);
            SetPlayer(i);
            Switches++;
            Sfx.Play("switch", .4f);
            PlayLog.Line($"switch to {nu.Stats.displayName}");
            return true;
        }

        public static bool Fits(Hero h, Vector3 feet)
        {
            float r = h.Stats.radius * .95f;
            var a = feet + Vector3.up * (r + .08f); var b = feet + Vector3.up * (h.Stats.height - r);
            return !Physics.CheckCapsule(a, b, r, ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << 4) | (1 << 2)), QueryTriggerInteraction.Ignore);
        }

        void Update()
        {
            if (Time.deltaTime <= 0 || Frozen) return;
            var input = new MotorInput();
            if (InputEnabled && Game.Playing)
            {
                var stick = Controls.Move.ReadValue<Vector2>();   // the Input System's stick deadzone already applies (0.125 - 0.925)
                float yaw = CameraRig.I ? CameraRig.I.Yaw : 0;
                input.move = Quaternion.Euler(0, yaw, 0) * new Vector3(stick.x, 0, stick.y);
                input.jumpPressed = Controls.Jump.WasPressedThisFrame();
                input.jumpHeld = Controls.Jump.IsPressed();
                input.diveHeld = Controls.Dive.IsPressed();
                input.divePressed = Controls.Dive.WasPressedThisFrame();
                if (Controls.Attack.WasPressedThisFrame())
                {
                    // Round 8: a Muscle Man job in reach (a boulder, a portcullis, a boost) takes the button; else the attack (Dan: and charging)
                    if (!MuscleJob.TryStart(Player)) { Player.TryAttack(); charging = Player.HasAbility(Abilities.ChargedPunch); Charge = 0; chargeSound = false; }
                }
                if (charging)
                {
                    if (Controls.Attack.IsPressed())
                    {
                        Charge += Time.deltaTime;
                        if (Charged && !chargeSound) { chargeSound = true; Sfx.Play("punch_charge", .6f); Player.ChargeGlow(true); }
                    }
                    else
                    {
                        if (Charged) { Player.ChargedPunch(); ChargedPunches++; }
                        charging = false; Charge = 0; Player.ChargeGlow(false);
                    }
                }
                if (Controls.SwitchNext.WasPressedThisFrame()) Switch((Index + 1) % 3);
                else if (Controls.SwitchPrev.WasPressedThisFrame()) Switch((Index + 2) % 3);
                if (charging && Player.Stats.displayName != "Dan") { charging = false; Charge = 0; }
                else if (Controls.PickDan.WasPressedThisFrame()) Switch(0);
                else if (Controls.PickAbi.WasPressedThisFrame()) Switch(1);
                else if (Controls.PickKira.WasPressedThisFrame()) Switch(2);
            }
            Player.Motor.SpeedScale = 1f;
            if (Game.Playing && RopeClimb.Try(Player, input.jumpPressed)) input.jumpPressed = false;   // Round 10: the ship's rigging
            if (Game.Playing && MagnetPillar.Try(Player, input.jumpPressed)) input.jumpPressed = false;   // Round 11: mechanical Kira's magnet paws
            Player.Motor.Step(input, Time.deltaTime);
            if (Game.Playing) IceHole.Watch(Player);   // Round 6, item 2: nobody stays stuck under the ice
            RecordTrail();
            var pp = Player.transform.position; var vc = Vortex.Centre;
            bool offVortex = Mathf.Abs(pp.x - vc.x) < 300 && Mathf.Abs(pp.z - vc.z) < 300 && pp.y < vc.y - 25f;   // Round 4: off the vortex platform
            if (pp.y < KillY || offVortex) Health.FellOut();
        }

        void RecordTrail()
        {
            var p = Player.transform.position;
            if (Trail.Count == 0 || (Trail[0] - p).sqrMagnitude > .3f * .3f)
            {
                Trail.Insert(0, p);
                if (Trail.Count > 240) Trail.RemoveAt(Trail.Count - 1);
            }
        }

        // The point `distance` metres back along the player's trail.
        public Vector3 TrailPoint(float distance)
        {
            var prev = Player.transform.position; float d = 0;
            foreach (var t in Trail)
            {
                float seg = Vector3.Distance(prev, t);
                if (d + seg >= distance) return Vector3.Lerp(prev, t, (distance - d) / Mathf.Max(.0001f, seg));
                d += seg; prev = t;
            }
            return prev;
        }

        // Put everyone at a spot (respawn, portals, load): the player there, the followers just behind.
        public void PlaceAll(Vector3 pos, Quaternion rot)
        {
            Player.Motor.Teleport(pos, rot);
            Trail.Clear();
            int k = 1;
            foreach (var h in Heroes) if (h != Player) { var p = pos + rot * new Vector3(k == 1 ? -1.3f : 1.3f, 0, -1.2f); k++; h.Motor.Teleport(Fits(h, p) ? p : pos, rot); h.Follower.ResetTimers(); }
            if (Companion) { var p = pos + rot * new Vector3(0, 0, -2.6f); Companion.Motor.Teleport(Fits(Companion, p) ? p : pos, rot); Companion.Follower.ResetTimers(); }
        }

        public void SetVisible(bool v) { foreach (var h in Everyone) h.Body.gameObject.SetActive(v); }
    }
}
