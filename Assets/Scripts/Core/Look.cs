using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ShardsOfTime
{
    // The light and sky: a sun, a sky colour, soft three-colour ambient, distance fog. Steam Deck on: shorter shadows, one cascade, no
    // MSAA (SteamDeck.cs). Round 4: per world (ApplyWorld): the graybox's warm day, the Ice Age's soft blue-white light with fog for depth
    // and a far view (the volcanoes); SetDark dims it in dark places (the ice cave); a cutscene can bring its own mood (CutsceneMood).
    public static class Look
    {
        public static Color Sky = new Color(.55f, .78f, .98f), FogColor = new Color(.72f, .86f, .98f);
        public static float FogStart = 90f, FogEnd = 420f, FarClip = 600f;
        public static Light Sun { get; private set; }
        static float sunIntensity = 1.25f, darkness; static Color ambSky, ambEq, ambGround;
        public static bool Dark => darkness > .5f;

        public static void Apply(Camera cam)
        {
            if (!Sun)
            {
                var go = new GameObject("Sun"); Sun = go.AddComponent<Light>(); Sun.type = LightType.Directional;
                Sun.shadows = LightShadows.Soft;
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.skybox = null;
            if (cam) cam.clearFlags = CameraClearFlags.SolidColor;
            ApplyWorld(WorldData.Playground);
            ApplyQuality();
        }

        public static void ApplyWorld(string w)
        {
            world = w; vortex = false;
            if (world == WorldData.Playground)
            {
                Sky = new Color(.55f, .78f, .98f); FogColor = new Color(.72f, .86f, .98f); FogStart = 90f; FogEnd = 420f; FarClip = 600f;
                Sun.color = new Color(1f, .95f, .86f); sunIntensity = 1.25f; Sun.transform.rotation = Quaternion.Euler(48f, -35f, 0);
                ambSky = new Color(.62f, .72f, .88f); ambEq = new Color(.58f, .6f, .62f); ambGround = new Color(.32f, .3f, .28f);
            }
            else   // the Ice Age (Dan's reference: a wide snowy valley, soft blue-white light, warm rock, fog for depth)
            {
                Sky = new Color(.66f, .79f, .94f); FogColor = new Color(.8f, .87f, .95f); FogStart = 45f; FogEnd = 1700f; FarClip = 2200f;
                Sun.color = new Color(1f, .96f, .9f); sunIntensity = 1.15f; Sun.transform.rotation = Quaternion.Euler(38f, -28f, 0);
                ambSky = new Color(.7f, .79f, .94f); ambEq = new Color(.66f, .7f, .78f); ambGround = new Color(.52f, .52f, .58f);
            }
            darkness = 0; Refresh();
            if (CameraRig.I) { CameraRig.I.Cam.farClipPlane = FarClip; CameraRig.I.Gameplay.Lens.FarClipPlane = FarClip; CameraRig.I.Cam.backgroundColor = Sky; }
        }

        // the ice cave: day light down to 6%, ambient to 12% (the necklace's light and the fire do the rest)
        public static void SetDark(bool dark)
        {
            float to = dark ? 1f : 0f;
            if (Mathf.Approximately(darkness, to)) return;
            darkness = Mathf.MoveTowards(darkness, to, Time.deltaTime * 1.5f);
            Refresh();
        }

        // Round 5: a cutscene's silhouette shot (Backlight): light on the near side to 8%, ambient to 22%, the sky near white
        static float backlit;
        public static void Backlit(float amount)
        {
            backlit = Mathf.Clamp01(amount); Refresh();
            if (CameraRig.I) CameraRig.I.Cam.backgroundColor = Color.Lerp(Sky, new Color(1f, .98f, .92f), backlit);
        }

        static void Refresh()
        {
            float k = Mathf.Lerp(1f, .06f, darkness) * Mathf.Lerp(1f, .08f, backlit), a = Mathf.Lerp(1f, .12f, darkness) * Mathf.Lerp(1f, .22f, backlit);
            if (Sun) Sun.intensity = sunIntensity * k;
            RenderSettings.ambientSkyColor = ambSky * a; RenderSettings.ambientEquatorColor = ambEq * a; RenderSettings.ambientGroundColor = ambGround * a;
            RenderSettings.fogColor = Color.Lerp(FogColor, new Color(1f, .98f, .92f), backlit);
        }

        // the vortex space: a deep violet sky and fog behind the swirl
        static bool vortex;
        public static void InVortex(bool on)
        {
            if (on == vortex) return; vortex = on;
            if (CameraRig.I) CameraRig.I.Cam.backgroundColor = on ? new Color(.08f, .06f, .2f) : Sky;
            FogColor = on ? new Color(.2f, .15f, .4f) : (world == WorldData.Playground ? new Color(.72f, .86f, .98f) : new Color(.8f, .87f, .95f));
            SnowFall.Show(!on);
            Refresh();
        }
        static string world;

        // a cutscene's own light (CutsceneMood: the living room), and back
        public static void Mood(CutsceneMood m)
        {
            if (m == null) { Refresh(); if (CameraRig.I) CameraRig.I.Cam.backgroundColor = Sky; RenderSettings.fog = true; SnowFall.Show(true); return; }
            if (Sun) Sun.intensity = m.sunIntensity;
            RenderSettings.ambientSkyColor = m.ambientSky; RenderSettings.ambientEquatorColor = m.ambientEquator; RenderSettings.ambientGroundColor = m.ambientGround;
            RenderSettings.fog = m.fog;
            if (CameraRig.I) CameraRig.I.Cam.backgroundColor = m.background;
            SnowFall.Show(m.snow);
        }

        public static void ApplyQuality()
        {
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                bool deck = SteamDeck.On;
                urp.shadowDistance = deck ? 45f : 90f;
                urp.shadowCascadeCount = deck ? 1 : 2;
                urp.msaaSampleCount = deck ? 1 : 4;
                urp.renderScale = 1f;
            }
            // players: vsync. Checks: no vsync off-screen, so cap at 144 fps like a fast monitor (thousands of frames a second make
            // each step so small the character controller skips it); the frame-rate measurement lifts the cap itself.
            QualitySettings.vSyncCount = CheckRun.Active ? 0 : 1;
            Application.targetFrameRate = CheckRun.Active ? 144 : -1;
            // a hitch makes game time run slower instead of one huge step (characters jumping through floors, cut jumps):
            // players at most 0.1 s a frame, checks 1/30 s (a check on a busy PC stays the same game)
            Time.maximumDeltaTime = CheckRun.Active ? 1f / 30f : .1f;
        }
    }

    // Round 4: a cutscene's own light (on its prefab's root): the living room's warm interior, no fog, no snow falling indoors.
    public sealed class CutsceneMood : MonoBehaviour
    {
        public float sunIntensity = 0f;
        public Color ambientSky = new Color(.5f, .35f, .45f), ambientEquator = new Color(.45f, .3f, .35f), ambientGround = new Color(.25f, .16f, .14f);
        public Color background = new Color(.12f, .1f, .2f);
        public bool fog, snow;
    }
}
