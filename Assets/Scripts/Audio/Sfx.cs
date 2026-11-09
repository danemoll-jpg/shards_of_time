using System.Collections.Generic;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 1: placeholder sound effects made in code (short tones and noise), so jumps, shards and hits have feedback before the
    // ElevenLabs sounds arrive. Real files in Resources/Sounds/<name> win when they exist. Volume: master x effects (Settings).
    // Test runs are muted from the first frame (Core/TestRun.cs).
    public static class Sfx
    {
        static AudioSource src;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        public static int Played { get; private set; }

        public static void Init(GameObject host)
        {
            src = host.AddComponent<AudioSource>(); src.playOnAwake = false; src.spatialBlend = 0;
            ApplyVolume();
        }

        public static void ApplyVolume()
        {
            if (!TestRun.Muted) AudioListener.volume = GameSettings.S.volume / 100f;
            if (src) src.volume = GameSettings.S.effectsVolume / 100f;
        }

        public static void Play(string name, float vol = 1f, float pitch = 1f)
        {
            if (!src) return;
            var c = Clip(name); if (!c) return;
            src.pitch = pitch; src.PlayOneShot(c, vol); Played++;
        }

        static AudioClip Clip(string name)
        {
            if (clips.TryGetValue(name, out var c)) return c;
            c = Resources.Load<AudioClip>("Sounds/" + name);
            if (!c)
                switch (name)
                {
                    case "jump": c = Tone(name, .12f, 330, 520, .6f, 0); break;
                    case "shard": c = Tone(name, .25f, 1320, 1760, .5f, 0, 2); break;
                    case "story": c = Tone(name, .9f, 660, 990, .5f, 0, 3); break;
                    case "hurt": c = Tone(name, .25f, 300, 140, .6f, .3f); break;
                    case "heal": c = Tone(name, .3f, 600, 900, .5f, 0, 2); break;
                    case "attack": c = Tone(name, .1f, 200, 120, .3f, .7f); break;
                    case "hit": c = Tone(name, .12f, 160, 90, .6f, .5f); break;
                    case "switch": c = Tone(name, .1f, 700, 880, .4f, 0); break;
                    case "splash": c = Tone(name, .35f, 400, 200, .4f, .9f); break;
                    case "checkpoint": c = Tone(name, .4f, 520, 780, .5f, 0, 2); break;
                    case "portal": c = Tone(name, .8f, 200, 800, .5f, .2f); break;
                    case "necklace": c = Tone(name, 1.2f, 440, 1320, .5f, 0, 3); break;
                    case "menu": c = Tone(name, .05f, 900, 900, .3f, 0); break;
                    default: c = Tone(name, .1f, 500, 500, .3f, 0); break;
                }
            clips[name] = c; return c;
        }

        // a sweep from f0 to f1 Hz with some noise; `steps` > 1 = an arpeggio
        static AudioClip Tone(string name, float seconds, float f0, float f1, float amp, float noise, int steps = 1)
        {
            int rate = 44100, n = (int)(seconds * rate);
            var data = new float[n]; float phase = 0; var rnd = new System.Random(name.GetHashCode());
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float f = steps > 1 ? Mathf.Lerp(f0, f1, Mathf.Floor(t * steps) / (steps - 1)) : Mathf.Lerp(f0, f1, t);
                phase += f / rate;
                float env = Mathf.Min(1f, t * 40f) * (1f - t) * (1f - t);
                float s = Mathf.Sin(phase * 2f * Mathf.PI) * (1f - noise) + ((float)rnd.NextDouble() * 2f - 1f) * noise;
                data[i] = s * env * amp;
            }
            var clip = AudioClip.Create(name, n, 1, rate, false); clip.SetData(data, 0);
            return clip;
        }
    }
}
