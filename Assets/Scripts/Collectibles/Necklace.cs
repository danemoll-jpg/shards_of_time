using UnityEngine;
using UnityEngine.InputSystem;

namespace ShardsOfTime
{
    // Round 1, item 9 (rule 3): Dan's necklace, the in-fiction shard detector. It pulses faster the nearer the closest uncollected shard
    // of the current area (HUD icon + the gem on Dan, plus a light rumble per pulse: Settings > Rumble, on by default). Once the area's
    // requirement is reached it is awake: it leads to the story shard instead (an arrow on the HUD icon and a beam at the shard).
    public static class Necklace
    {
        public const float FastPeriod = .22f, SlowPeriod = 1.8f, Range = 45f;
        public static Transform Target { get; private set; }
        public static float Distance { get; private set; } = float.MaxValue;
        public static float Period => Mathf.Lerp(FastPeriod, SlowPeriod, Mathf.Clamp01(Distance / Range));
        public static float Pulse { get; private set; }     // 0..1, 1 at the beat
        public static bool Awake { get; private set; }
        public static int Beats { get; private set; }
        static float phase, rumbleUntil;

        public static void Tick(string area, bool awake)
        {
            Awake = awake;
            Target = null; Distance = float.MaxValue;
            if (!Party.I) return;
            var from = Party.I.Player.Motor.Center;
            foreach (var p in Pickup.All)
            {
                if (!p.isActiveAndEnabled || p.area != area) continue;
                if (awake ? p.kind != Pickup.Kind.StoryShard : p.kind != Pickup.Kind.Shard) continue;
                float d = Vector3.Distance(from, p.transform.position);
                if (d < Distance) { Distance = d; Target = p.transform; }
            }
            if (!Target) { Pulse = Mathf.MoveTowards(Pulse, 0, Time.deltaTime * 3); StopRumble(); return; }
            phase += Time.deltaTime / Period;
            if (phase >= 1f)
            {
                phase -= Mathf.Floor(phase); Beats++; Pulse = 1f;
                if (GameSettings.S.rumble && Gamepad.current != null && Game.Playing && !CheckRun.Active)
                {
                    float k = Mathf.Lerp(.25f, .08f, Mathf.Clamp01(Distance / Range));
                    Gamepad.current.SetMotorSpeeds(k * .5f, k); rumbleUntil = Time.unscaledTime + .06f;
                }
            }
            else Pulse = Mathf.MoveTowards(Pulse, 0, Time.deltaTime * 5f);
            if (Time.unscaledTime > rumbleUntil) StopRumble();
        }

        public static void StopRumble()
        {
            if (rumbleUntil > 0 && Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0, 0);
            rumbleUntil = 0;
        }

        // the direction to the target on screen (for the awake arrow), in degrees, 0 = straight ahead
        public static float ScreenAngle()
        {
            if (!Target || !CameraRig.I || !Party.I) return 0;
            var d = Target.position - Party.I.Player.transform.position; d.y = 0;
            return -Mathf.DeltaAngle(CameraRig.I.Yaw, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg);
        }
    }
}
