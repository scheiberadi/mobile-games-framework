using System;
using System.Collections.Generic;
using MobileGamesFramework.UI;
using UnityEngine;

namespace EvasLearningWorld.App
{
    // Short procedural sound effects, no audio assets. The AudioSource is created lazily so the class also works in edit mode tests.
    public sealed class Sfx : MonoBehaviour
    {
        private AudioSource _source;
        private AudioClip _tap, _right, _retry, _coin, _place, _buy;

        private AudioSource Source
        {
            get
            {
                if (_source == null)
                {
                    _source = gameObject.AddComponent<AudioSource>();
                    _source.playOnAwake = false;
                }
                return _source;
            }
        }

        public void Tap() => Play(ref _tap, () => ProceduralAudio.GenerateTone(660f, 0.05f));
        public void Right() => Play(ref _right, () => Sequence(0.3f, (523f, 0.10f), (784f, 0.16f)));
        public void Retry() => Play(ref _retry, () => ProceduralAudio.GenerateTone(220f, 0.15f, 0.2f));
        public void Coin() => Play(ref _coin, () => ProceduralAudio.GenerateTone(1300f, 0.06f));
        public void Place() => Play(ref _place, () => ProceduralAudio.GenerateTone(440f, 0.08f));
        public void Buy() => Play(ref _buy, () => Sequence(0.3f, (523f, 0.09f), (659f, 0.09f), (784f, 0.16f)));

        // False silences every effect (the Sound effects switch in Settings).
        public bool Enabled { get; set; } = true;

        private void Play(ref AudioClip cache, Func<AudioClip> make)
        {
            if (!Enabled) return;
            if (cache == null) cache = make();
            Source.PlayOneShot(cache);
        }

        // Joins several tones into one clip so a rising phrase needs no timing code.
        private static AudioClip Sequence(float volume, params (float frequency, float duration)[] tones)
        {
            var samples = new List<float>();
            foreach (var (frequency, duration) in tones)
            {
                var tone = ProceduralAudio.GenerateTone(frequency, duration, volume);
                var data = new float[tone.samples];
                tone.GetData(data, 0);
                samples.AddRange(data);
                // Destroy() is play-mode-only; edit-mode callers (tests reaching a correct-answer tap) need
                // DestroyImmediate, same pattern as CountScreen.ClearChildren.
                if (Application.isPlaying) Destroy(tone);
                else DestroyImmediate(tone);
            }
            var clip = AudioClip.Create("EvaSfxSequence", samples.Count, 1, 44100, false);
            clip.SetData(samples.ToArray(), 0);
            return clip;
        }
    }
}
