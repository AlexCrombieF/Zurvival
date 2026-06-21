using UnityEngine;

namespace ZombieSurvival.Audio
{
    /// <summary>
    /// Synthesizes all the game's sound effects in code at runtime, so the
    /// project needs zero audio asset files. They're crude (guttural noise +
    /// tones), but they give real directional/threat feedback — which is the
    /// point. Swap these for recorded clips later without touching gameplay.
    /// </summary>
    public static class ProceduralSfx
    {
        private const int SampleRate = 44100;

        /// <summary>Low, wobbling guttural groan. Slightly different each call.</summary>
        public static AudioClip Groan()
        {
            float dur = Random.Range(0.9f, 1.6f);
            int n = Mathf.RoundToInt(dur * SampleRate);
            float baseFreq = Random.Range(70f, 130f);
            var data = new float[n];

            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float p = (float)i / n;
                float vibrato = 1f + 0.12f * Mathf.Sin(2f * Mathf.PI * 5.5f * t);
                float freq = baseFreq * vibrato;
                float saw = 2f * (t * freq - Mathf.Floor(0.5f + t * freq)); // sawtooth
                float noise = (Random.value * 2f - 1f) * 0.3f;
                float env = Mathf.Sin(Mathf.PI * p);                        // soft attack+decay
                data[i] = (saw * 0.5f + noise * 0.5f) * env * 0.5f;
            }
            return Make("groan", data);
        }

        /// <summary>Short scuffing footstep — a fast-decaying filtered noise burst.</summary>
        public static AudioClip Footstep()
        {
            int n = Mathf.RoundToInt(0.16f * SampleRate);
            var data = new float[n];
            float last = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float noise = Random.value * 2f - 1f;
                last = Mathf.Lerp(last, noise, 0.25f);   // crude low-pass for a duller "thud"
                float env = Mathf.Exp(-t * 28f);
                data[i] = last * env * 0.6f;
            }
            return Make("footstep", data);
        }

        /// <summary>A single "lub-dub" heartbeat.</summary>
        public static AudioClip Heartbeat()
        {
            int n = Mathf.RoundToInt(0.5f * SampleRate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                data[i] = Thump(t, 0.00f, 55f) + Thump(t, 0.16f, 48f);
            }
            return Make("heartbeat", data);
        }

        /// <summary>Dull impact for a connecting melee hit.</summary>
        public static AudioClip Thud()
        {
            int n = Mathf.RoundToInt(0.2f * SampleRate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 22f);
                float tone = Mathf.Sin(2f * Mathf.PI * 90f * t);
                float noise = (Random.value * 2f - 1f) * 0.5f;
                data[i] = (tone * 0.6f + noise * 0.4f) * env * 0.7f;
            }
            return Make("thud", data);
        }

        /// <summary>Airy "whoosh" of a weapon swung through the air.</summary>
        public static AudioClip Whoosh()
        {
            int n = Mathf.RoundToInt(0.28f * SampleRate);
            var data = new float[n];
            float last = 0f;
            for (int i = 0; i < n; i++)
            {
                float p = (float)i / n;
                float env = Mathf.Sin(p * Mathf.PI);            // swell then fade
                float noise = Random.value * 2f - 1f;
                last = Mathf.Lerp(last, noise, 0.5f);           // soften to a breathy hiss
                data[i] = last * env * 0.3f;
            }
            return Make("whoosh", data);
        }

        private static float Thump(float t, float start, float freq)
        {
            if (t < start) return 0f;
            float lt = t - start;
            float env = Mathf.Exp(-lt * 18f);
            return Mathf.Sin(2f * Mathf.PI * freq * lt) * env * 0.8f;
        }

        private static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
