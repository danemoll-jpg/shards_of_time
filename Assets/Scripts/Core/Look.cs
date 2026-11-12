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
            if (w != world) night = false;   // Round 11: a world's night (the factory, the ship's arrival) never follows into the next world
            world = w; vortex = false;
            if (world == WorldData.Playground)
            {
                Sky = new Color(.55f, .78f, .98f); FogColor = new Color(.72f, .86f, .98f); FogStart = 90f; FogEnd = 420f; FarClip = 600f;
                Sun.color = new Color(1f, .95f, .86f); sunIntensity = 1.25f; Sun.transform.rotation = Quaternion.Euler(48f, -35f, 0);
                ambSky = new Color(.62f, .72f, .88f); ambEq = new Color(.58f, .6f, .62f); ambGround = new Color(.32f, .3f, .28f);
            }
            else if (world == WorldData.Egypt)   // Round 6, item 12: golden dunes under a deep blue sky, a warm high sun, a light golden haze far off
            {
                Sky = new Color(.28f, .55f, .92f); FogColor = new Color(.86f, .8f, .68f); FogStart = 120f; FogEnd = 1500f; FarClip = 2000f;
                Sun.color = new Color(1f, .93f, .78f); sunIntensity = 1.3f; Sun.transform.rotation = Quaternion.Euler(52f, -40f, 0);
                ambSky = new Color(.6f, .72f, .92f); ambEq = new Color(.78f, .7f, .58f); ambGround = new Color(.62f, .52f, .38f);
            }
            else if (world == WorldData.Bethlehem)   // Round 7, item 11: a starry night: deep blue sky, soft blue moonlight, warm lamps do the rest
            {
                Sky = new Color(.035f, .07f, .2f); FogColor = new Color(.05f, .08f, .19f); FogStart = 70f; FogEnd = 900f; FarClip = 1500f;
                Sun.color = new Color(.62f, .7f, 1f); sunIntensity = .42f; Sun.transform.rotation = Quaternion.Euler(42f, 150f, 0);
                ambSky = new Color(.2f, .26f, .48f); ambEq = new Color(.16f, .17f, .3f); ambGround = new Color(.1f, .09f, .1f);
            }
            else if (world == WorldData.Medieval)   // Round 8, item 12: a torch-lit night: deep blue sky, cool moonlight, the torches' warm light does the rest
            {
                Sky = new Color(.05f, .09f, .26f); FogColor = new Color(.07f, .1f, .24f); FogStart = 80f; FogEnd = 700f; FarClip = 1300f;
                Sun.color = new Color(.6f, .68f, 1f); sunIntensity = .5f; Sun.transform.rotation = Quaternion.Euler(40f, 160f, 0);
                ambSky = new Color(.24f, .28f, .5f); ambEq = new Color(.22f, .19f, .26f); ambGround = new Color(.16f, .12f, .1f);
            }
            else if (world == WorldData.Pirate)   // Round 10, item 12: a pink-violet sunset over a deep blue sea, a low warm sun
            {
                Sky = new Color(.86f, .56f, .72f); FogColor = new Color(.9f, .62f, .7f); FogStart = 90f; FogEnd = 900f; FarClip = 1300f;
                Sun.color = new Color(1f, .74f, .55f); sunIntensity = 1.15f; Sun.transform.rotation = Quaternion.Euler(16f, -70f, 0);
                ambSky = new Color(.7f, .55f, .8f); ambEq = new Color(.72f, .55f, .55f); ambGround = new Color(.25f, .3f, .45f);
                if (night)   // the arrival (Dan's clip): the ship at night with its lanterns
                {
                    Sky = new Color(.04f, .07f, .2f); FogColor = new Color(.05f, .09f, .22f); FogStart = 60f; FogEnd = 600f;
                    Sun.color = new Color(.55f, .65f, 1f); sunIntensity = .45f; Sun.transform.rotation = Quaternion.Euler(40f, 150f, 0);
                    ambSky = new Color(.2f, .26f, .5f); ambEq = new Color(.16f, .18f, .3f); ambGround = new Color(.08f, .1f, .16f);
                }
            }
            else if (world == WorldData.Steampunk)   // Round 11, item 12: the streets on a warm golden morning; the factory at night (blue-violet haze)
            {
                Sky = new Color(.98f, .8f, .58f); FogColor = new Color(.95f, .8f, .64f); FogStart = 70f; FogEnd = 520f; FarClip = 900f;
                Sun.color = new Color(1f, .85f, .62f); sunIntensity = 1.25f; Sun.transform.rotation = Quaternion.Euler(28f, -50f, 0);
                ambSky = new Color(.78f, .7f, .62f); ambEq = new Color(.72f, .6f, .5f); ambGround = new Color(.4f, .32f, .28f);
                if (night)   // inside the factory: night through the skylights, warm brass lamps do the rest
                {
                    Sky = new Color(.12f, .1f, .3f); FogColor = new Color(.2f, .16f, .38f); FogStart = 25f; FogEnd = 190f;
                    Sun.color = new Color(.55f, .55f, 1f); sunIntensity = .35f; Sun.transform.rotation = Quaternion.Euler(55f, 150f, 0);
                    ambSky = new Color(.3f, .26f, .55f); ambEq = new Color(.28f, .22f, .38f); ambGround = new Color(.16f, .12f, .14f);
                }
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

        // Round 10: the arrival's night on the ship (Pirate.SetNight), and under the sea: bright blue water, short blue fog, sun shafts
        static bool night;
        public static void SetNight(bool on) { night = on; ApplyWorld(world); }
        public static bool Underwater { get; private set; }
        public static readonly Color SeaFog = new Color(.1f, .5f, .78f);
        static float under;
        public static void SetUnderwater(bool on)
        {
            float to = on ? 1f : 0f;
            if (Mathf.Approximately(under, to)) { Underwater = on; return; }
            under = Mathf.MoveTowards(under, to, Time.deltaTime * 4f); Underwater = under > .5f;
            Refresh();
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
            if ((amount > .5f) != (backlit > .5f)) PlayLog.Line($"backlight {(amount > .5f ? "on" : "off")}");
            backlit = Mathf.Clamp01(amount); Refresh();
            if (CameraRig.I) CameraRig.I.Cam.backgroundColor = Color.Lerp(Sky, new Color(1f, .98f, .92f), backlit);
        }

        static void Refresh()
        {
            float k = Mathf.Lerp(1f, .06f, darkness) * Mathf.Lerp(1f, .08f, backlit), a = Mathf.Lerp(1f, .12f, darkness) * Mathf.Lerp(1f, .22f, backlit);
            if (Sun) Sun.intensity = sunIntensity * k;
            RenderSettings.ambientSkyColor = ambSky * a; RenderSettings.ambientEquatorColor = ambEq * a; RenderSettings.ambientGroundColor = ambGround * a;
            RenderSettings.fogColor = Color.Lerp(FogColor, new Color(1f, .98f, .92f), backlit);
            // Round 10: under the sea
            var deep = SeaFog;   // (the fog itself: CameraRig.LateUpdate)
            if (under > 0)
            {
                RenderSettings.ambientSkyColor = Color.Lerp(RenderSettings.ambientSkyColor, new Color(.45f, .8f, 1f), under * .8f);
                RenderSettings.ambientEquatorColor = Color.Lerp(RenderSettings.ambientEquatorColor, new Color(.3f, .62f, .85f), under * .8f);
                RenderSettings.ambientGroundColor = Color.Lerp(RenderSettings.ambientGroundColor, new Color(.25f, .45f, .55f), under * .8f);
                if (Sun) { Sun.intensity *= Mathf.Lerp(1f, .8f, under); }
            }
            if (CameraRig.I && !vortex && backlit <= 0) CameraRig.I.Cam.backgroundColor = Color.Lerp(Sky, deep, under);
        }

        // the vortex space: a deep violet sky and fog behind the swirl
        static bool vortex;
        public static void InVortex(bool on)
        {
            if (on == vortex) return; vortex = on;
            if (CameraRig.I) CameraRig.I.Cam.backgroundColor = on ? new Color(.08f, .06f, .2f) : Sky;
            FogColor = on ? new Color(.2f, .15f, .4f) : (world == WorldData.Playground ? new Color(.72f, .86f, .98f) : world == WorldData.Egypt ? new Color(.86f, .8f, .68f) : world == WorldData.Bethlehem ? new Color(.05f, .08f, .19f) : new Color(.8f, .87f, .95f));
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
}
