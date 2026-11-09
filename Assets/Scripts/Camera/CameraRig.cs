using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShardsOfTime
{
    // Round 1, item 5: the third-person camera. A Cinemachine orbit camera (CinemachineCamera + OrbitalFollow + RotationComposer +
    // Deoccluder) around a focus point that eases after the player (so switching heroes or Kira's small size never makes it jump).
    // Right stick / mouse orbits (Settings: invert X / Y, camera speed); a gentle recenter behind the player while running; walls pull
    // the camera in (never through them); Recenter (right stick click, R, middle mouse) swings it behind the player; underwater it
    // keeps working and tints the view.
    public sealed class CameraRig : MonoBehaviour
    {
        public static CameraRig I { get; private set; }
        public Camera Cam { get; private set; }
        public CinemachineBrain Brain { get; private set; }
        public CinemachineCamera Gameplay { get; private set; }
        public CinemachineOrbitalFollow Orbit { get; private set; }
        public CinemachineDeoccluder Deoccluder { get; private set; }
        public Transform Focus { get; private set; }
        public float Yaw => Cam.transform.eulerAngles.y;
        public bool Underwater { get; private set; }
        public int Recenters { get; private set; }

        public const float StickDegPerSec = 160f, MouseDegPerUnit = .11f, Radius = 6.5f;
        float idleLook = 10f, recenterUntil = -1f; Vector3 focusVel;

        public static CameraRig Create()
        {
            var go = new GameObject("Camera Rig"); var r = go.AddComponent<CameraRig>(); I = r;
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            r.Cam = camGo.AddComponent<Camera>(); r.Cam.nearClipPlane = .08f; r.Cam.farClipPlane = 600f; r.Cam.fieldOfView = 55f;
            camGo.AddComponent<AudioListener>(); TestRun.Attach(camGo);
            r.Brain = camGo.AddComponent<CinemachineBrain>();
            r.Brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, .6f);
            r.Focus = new GameObject("Camera Focus").transform;

            var cmGo = new GameObject("Gameplay Camera");
            r.Gameplay = cmGo.AddComponent<CinemachineCamera>();
            r.Gameplay.Target.TrackingTarget = r.Focus;
            r.Gameplay.Lens.FieldOfView = 55f; r.Gameplay.Lens.NearClipPlane = .08f; r.Gameplay.Lens.FarClipPlane = 600f;
            r.Gameplay.Priority = 10;
            r.Orbit = cmGo.AddComponent<CinemachineOrbitalFollow>();
            r.Orbit.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere; r.Orbit.Radius = Radius;
            r.Orbit.TrackerSettings.BindingMode = Unity.Cinemachine.TargetTracking.BindingMode.WorldSpace;
            r.Orbit.TrackerSettings.PositionDamping = new Vector3(.15f, .3f, .15f);
            r.Orbit.HorizontalAxis.Recentering.Enabled = false; r.Orbit.VerticalAxis.Recentering.Enabled = false;
            r.Orbit.VerticalAxis.Range = new Vector2(-35f, 70f); r.Orbit.VerticalAxis.Value = 16f; r.Orbit.VerticalAxis.Center = 16f;
            var comp = cmGo.AddComponent<CinemachineRotationComposer>(); comp.Damping = new Vector2(.15f, .15f);
            comp.Composition.ScreenPosition = new Vector2(0, .08f);
            r.Deoccluder = cmGo.AddComponent<CinemachineDeoccluder>();
            r.Deoccluder.CollideAgainst = ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << Hero.EnemyLayer) | (1 << 4) | (1 << 2) | (1 << 5));
            r.Deoccluder.MinimumDistanceFromTarget = .4f;
            var av = r.Deoccluder.AvoidObstacles; av.Enabled = true; av.CameraRadius = .25f; av.Strategy = CinemachineDeoccluder.ObstacleAvoidance.ResolutionStrategy.PullCameraForward;
            av.Damping = .3f; av.DampingWhenOccluded = 0f; av.SmoothingTime = .1f; av.DistanceLimit = 0; av.MaximumEffort = 4;
            r.Deoccluder.AvoidObstacles = av;
            TestRun.SetupHeadless(r.Cam);
            return r;
        }

        Vector3 FocusPoint(Hero h) => h.transform.position + Vector3.up * (h.Stats.height * .75f + .35f);

        public void SnapBehind()
        {
            if (!Party.I) return;
            var h = Party.I.Player;
            var old = Focus.position;
            Focus.position = FocusPoint(h); focusVel = Vector3.zero;
            Orbit.HorizontalAxis.Value = h.transform.eulerAngles.y;
            Gameplay.OnTargetObjectWarped(Focus, Focus.position - old);
            Gameplay.PreviousStateIsValid = false;
        }

        void Update()
        {
            if (!Party.I || Time.unscaledDeltaTime <= 0) return;
            var h = Party.I.Player;
            // the focus eases after the player: quick sideways, softer up and down (jumps don't bob the view)
            var want = FocusPoint(h);
            var f = Focus.position;
            f.x = Mathf.SmoothDamp(f.x, want.x, ref focusVel.x, .05f);
            f.z = Mathf.SmoothDamp(f.z, want.z, ref focusVel.z, .05f);
            f.y = Mathf.SmoothDamp(f.y, want.y, ref focusVel.y, h.Motor.Grounded || h.Motor.Mode == MoveMode.Swim ? .12f : .3f);
            Focus.position = f;
            if (!Game.Playing) return;

            float dt = Time.deltaTime;
            var s = GameSettings.S; float speed = s.cameraSpeed / 100f;
            var look = Controls.Look.ReadValue<Vector2>();
            bool mouse = Controls.Look.activeControl != null && Controls.Look.activeControl.device is Mouse;
            Vector2 deg = mouse ? look * MouseDegPerUnit * speed : (look.magnitude > .15f ? look * StickDegPerSec * speed * dt : Vector2.zero);
            if (s.invertX) deg.x = -deg.x;
            if (!s.invertY) deg.y = -deg.y;    // stick up = look up (camera goes down)
            if (deg.sqrMagnitude > 0) { idleLook = 0; recenterUntil = -1; }
            else idleLook += dt;
            Orbit.HorizontalAxis.Value = Mathf.Repeat(Orbit.HorizontalAxis.Value + deg.x + 180f, 360f) - 180f;
            Orbit.VerticalAxis.Value = Mathf.Clamp(Orbit.VerticalAxis.Value + deg.y, Orbit.VerticalAxis.Range.x, Orbit.VerticalAxis.Range.y);

            // Recenter button: swing behind the player
            if (Controls.Recenter.WasPressedThisFrame()) { recenterUntil = Time.time + .35f; Recenters++; }
            float heading = h.transform.eulerAngles.y;
            if (recenterUntil > Time.time)
            {
                Orbit.HorizontalAxis.Value = Mathf.MoveTowardsAngle(Orbit.HorizontalAxis.Value, heading, 720f * dt);
                Orbit.VerticalAxis.Value = Mathf.MoveTowards(Orbit.VerticalAxis.Value, 16f, 120f * dt);
            }
            else
            {
                // gentle auto-recenter while running (not when the player has just turned the camera)
                var hv = h.Motor.Velocity; hv.y = 0;
                if (idleLook > 1.2f && hv.magnitude > 2.5f && h.Motor.Mode != MoveMode.Swim)
                {
                    float rate = 35f * Mathf.Clamp01(hv.magnitude / h.Stats.runSpeed);
                    float moveYaw = Mathf.Atan2(hv.x, hv.z) * Mathf.Rad2Deg;
                    if (Mathf.Abs(Mathf.DeltaAngle(Orbit.HorizontalAxis.Value, moveYaw)) < 135f)   // never swing round when running at the camera
                        Orbit.HorizontalAxis.Value = Mathf.MoveTowardsAngle(Orbit.HorizontalAxis.Value, moveYaw, rate * dt);
                }
            }
        }

        void LateUpdate()
        {
            var w = WaterVolume.At(Cam.transform.position);
            Underwater = w;
            Hud.SetUnderwater(Underwater);
            RenderSettings.fogColor = Underwater ? new Color(.12f, .35f, .5f) : Look.FogColor;
            RenderSettings.fogStartDistance = Underwater ? 0f : 90f;
            RenderSettings.fogEndDistance = Underwater ? 28f : 420f;
        }

        // the camera's distance from the focus (checks: pulled in by walls)
        public float Distance => Vector3.Distance(Cam.transform.position, Focus.position);
    }
}
