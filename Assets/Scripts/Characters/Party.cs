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
        public const float KillY = -25f;
        public readonly List<Hero> Heroes = new List<Hero>();
        public Hero Player { get; private set; }
        public int Switches { get; private set; }
        public bool InputEnabled = true;
        public bool Frozen;                          // cutscenes: nobody moves
        public readonly List<Vector3> Trail = new List<Vector3>();   // the player's path, newest first, a point every 0.3 m

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
            return p;
        }

        public void ApplyAbilities()
        {
            foreach (var h in Heroes)
            {
                h.Motor.CanDoubleJump = h.Stats.lockedAbility == Abilities.DoubleJump && SaveGame.Current.Has(Abilities.DoubleJump);
                h.Motor.CanGroundPound = h.Stats.lockedAbility == Abilities.GroundPound && SaveGame.Current.Has(Abilities.GroundPound);   // Round 4
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
                if (Controls.Attack.WasPressedThisFrame()) Player.TryAttack();
                if (Controls.SwitchNext.WasPressedThisFrame()) Switch((Index + 1) % 3);
                else if (Controls.SwitchPrev.WasPressedThisFrame()) Switch((Index + 2) % 3);
                else if (Controls.PickDan.WasPressedThisFrame()) Switch(0);
                else if (Controls.PickAbi.WasPressedThisFrame()) Switch(1);
                else if (Controls.PickKira.WasPressedThisFrame()) Switch(2);
            }
            Player.Motor.SpeedScale = 1f;
            Player.Motor.Step(input, Time.deltaTime);
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
        }

        public void SetVisible(bool v) { foreach (var h in Heroes) h.Body.gameObject.SetActive(v); }
    }
}
