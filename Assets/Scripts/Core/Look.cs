using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ShardsOfTime
{
    // The graybox's light and sky: a warm sun, a bright sky colour, soft three-colour ambient, distance fog. Steam Deck on: shorter
    // shadows, one cascade, no MSAA (SteamDeck.cs).
    public static class Look
    {
        public static readonly Color Sky = new Color(.55f, .78f, .98f), FogColor = new Color(.72f, .86f, .98f);
        public static Light Sun { get; private set; }

        public static void Apply(Camera cam)
        {
            if (!Sun)
            {
                var go = new GameObject("Sun"); Sun = go.AddComponent<Light>(); Sun.type = LightType.Directional;
                Sun.color = new Color(1f, .95f, .86f); Sun.intensity = 1.25f; Sun.shadows = LightShadows.Soft;
                go.transform.rotation = Quaternion.Euler(48f, -35f, 0);
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.62f, .72f, .88f);
            RenderSettings.ambientEquatorColor = new Color(.58f, .6f, .62f);
            RenderSettings.ambientGroundColor = new Color(.32f, .3f, .28f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = FogColor;
            RenderSettings.fogStartDistance = 90f; RenderSettings.fogEndDistance = 420f;
            RenderSettings.skybox = null;
            if (cam) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Sky; }
            ApplyQuality();
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
}
