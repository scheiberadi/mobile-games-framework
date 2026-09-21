using UnityEngine;

namespace MobileGamesFramework.UI
{
    // Synthesizes short sine-wave SFX at runtime, the same way RoundedRectSprite draws
    // shapes into a Texture2D - no imported audio assets, no art/audio pipeline needed.
    public static class ProceduralAudio
    {
        private const int SampleRate = 44100;

        public static AudioClip GenerateTone(float frequency, float duration, float volume = 0.3f)
        {
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[sampleCount];

            const float fadeSeconds = 0.01f;
            for (var i = 0; i < sampleCount; i++)
            {
                var t = (float)i / SampleRate;
                var envelope = Mathf.Min(Mathf.Clamp01(t / fadeSeconds), Mathf.Clamp01((duration - t) / fadeSeconds));
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * volume * envelope;
            }

            var clip = AudioClip.Create("ProceduralTone", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
