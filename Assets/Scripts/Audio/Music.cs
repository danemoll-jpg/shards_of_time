using UnityEngine;

namespace ShardsOfTime
{
    // Round 4, item 10 (Round 5: Dan's Suno tracks, chosen by Game.MusicFor): the music and the ambience. A world's theme loops seamlessly (two sources: near the end the next pass fades in
    // while this one fades out, `Crossfade` seconds), with its ambience loop under it (the Ice Age: wind). Clips come from
    // Resources/Music/<track> (the theme) and Resources/Sounds/<name> (ambience: Tools/make_sounds.py). Changing track crossfades.
    // Volume: master x music (Settings > Music volume). Test runs are muted from the first frame (Core/TestRun.cs): AudioListener 0.
    public sealed class Music : MonoBehaviour
    {
        public static Music I { get; private set; }
        public const float Crossfade = 3f;
        public static string Track { get; private set; }
        public static bool HasClip => I && I.clip;
        AudioSource[] src = new AudioSource[2]; AudioSource amb; int cur; AudioClip clip; float fadeT = -1, trackGain;
        // a world track's ambience (none under the title, Act I or the vortex)
        static readonly System.Collections.Generic.Dictionary<string, string> Ambience = new System.Collections.Generic.Dictionary<string, string> { ["iceage"] = "wind_loop", ["pirate"] = "waves_loop" };   // Round 10: the sea lapping at the ship
        float fadeLen = Crossfade;

        public static void Init(GameObject host)
        {
            I = host.AddComponent<Music>();
            for (int i = 0; i < 2; i++) { var a = host.AddComponent<AudioSource>(); a.playOnAwake = false; a.loop = false; a.spatialBlend = 0; a.priority = 0; I.src[i] = a; }
            I.amb = host.AddComponent<AudioSource>(); I.amb.playOnAwake = false; I.amb.loop = true; I.amb.spatialBlend = 0;
        }

        static float Vol => GameSettings.S.musicVolume / 100f;
        public static void ApplyVolume() { }   // read every frame

        // Round 5: Dan's tracks (Docs/MUSIC.md): "title", "act1", "iceage", "vortex"; a missing file is silence. null = silence.
        // `fade`: seconds of the crossfade (the old track out, the new one in).
        public static void Play(string track, float fade = Crossfade)
        {
            if (!I || track == Track) return;
            Track = track;
            var c = string.IsNullOrEmpty(track) ? null : Resources.Load<AudioClip>("Music/" + track);
            I.fadeLen = Mathf.Max(.05f, fade);
            I.Switch(c);
            string ambName = track != null && Ambience.TryGetValue(track, out var an) ? an : null;
            var wind = ambName == null ? null : Resources.Load<AudioClip>("Sounds/" + ambName);
            if (I.amb.clip != wind) { I.amb.clip = wind; if (wind) I.amb.Play(); else I.amb.Stop(); }
            PlayLog.Line($"music: {(c ? track + $" ({c.length:0.0} s)" : (string.IsNullOrEmpty(track) ? "silence" : track + " missing: silence"))}{(wind ? ", ambience " + ambName : "")}");
        }
        public static AudioSource Current => I ? I.src[I.cur] : null;

        void Switch(AudioClip c)
        {
            if (c == clip) return;
            // fade the old one out over Crossfade, start the new one fresh
            if (fadeOld && fadeOld != src[cur]) fadeOld.Stop();   // a track still fading out from the last change: this change reuses its source
            var old = src[cur]; cur = 1 - cur; clip = c;
            if (c) { src[cur].clip = c; src[cur].time = 0; src[cur].volume = 0; src[cur].Play(); }
            fadeOld = old; fadeT = 0;
        }
        AudioSource fadeOld;

        void Update()
        {
            float v = Vol;
            if (fadeT >= 0)
            {
                fadeT += (Time.timeScale > 0 ? Time.deltaTime : Time.unscaledDeltaTime) / fadeLen; float k = Mathf.Clamp01(fadeT);   // game time, in step with the cutscenes (real time while paused)
                if (fadeOld) fadeOld.volume = v * Mathf.Cos(k * Mathf.PI / 2);   // equal power
                src[cur].volume = v * Mathf.Sin(k * Mathf.PI / 2);
                if (k >= 1) { if (fadeOld && fadeOld != src[cur]) fadeOld.Stop(); fadeT = -1; fadeOld = null; }
            }
            else if (clip) src[cur].volume = v;
            // seamless loop: Crossfade seconds before the end, the other source starts the clip again
            if (clip && fadeT < 0 && src[cur].isPlaying && src[cur].time > clip.length - Crossfade)
            {
                var old = src[cur]; cur = 1 - cur; src[cur].clip = clip; src[cur].time = 0; src[cur].volume = 0; src[cur].Play();
                fadeOld = old; fadeT = 0; fadeLen = Crossfade;
            }
            if (amb) amb.volume = v * .55f;
        }
    }
}
