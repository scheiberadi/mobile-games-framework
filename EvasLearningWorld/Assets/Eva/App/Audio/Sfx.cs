using System;
using System.Collections.Generic;
using MobileGamesFramework.UI;
using UnityEngine;

namespace EvasLearningWorld.App
{
    // Sound effects. The clips are synthesized by tools/sfx/generate.js into Resources/Sfx; when a clip is missing (edit-mode tests
    // before an import, a deleted file) a plain procedural tone plays instead so a tap is never silent. The AudioSource is created
    // lazily so the class also works in edit mode tests.
    public sealed class Sfx : MonoBehaviour
    {
        // Every effect that has a file in Resources/Sfx (checked by SfxTests).
        public static readonly string[] Names = { "tap", "pick", "drop", "place", "right", "retry", "coin", "buy", "win", "hint", "pop", "levelup", "reelout", "reelin", "detbeep", "dethot", "dig", "splash", "flop", "shiver", "jetpack_on", "jetpack", "clink" };

        private AudioSource _source;
        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();

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

        public void Tap() => Play("tap", () => ProceduralAudio.GenerateTone(660f, 0.05f));
        public void Pick() => Play("pick", () => ProceduralAudio.GenerateTone(520f, 0.06f));
        public void Drop() => Play("drop", () => ProceduralAudio.GenerateTone(200f, 0.08f));
        public void Place() => Play("place", () => ProceduralAudio.GenerateTone(440f, 0.08f));
        public void Right() => Play("right", () => Sequence(0.3f, (523f, 0.10f), (784f, 0.16f)));
        public void Retry() => Play("retry", () => ProceduralAudio.GenerateTone(220f, 0.15f, 0.2f));
        public void Coin() => Play("coin", () => ProceduralAudio.GenerateTone(1300f, 0.06f));
        public void Buy() => Play("buy", () => Sequence(0.3f, (523f, 0.09f), (659f, 0.09f), (784f, 0.16f)));
        public void Win() => Play("win", () => Sequence(0.3f, (523f, 0.1f), (659f, 0.1f), (784f, 0.1f), (1046f, 0.25f)));
        public void Hint() => Play("hint", () => Sequence(0.25f, (659f, 0.1f), (880f, 0.15f)));
        public void LevelUp() => Play("levelup", () => Sequence(0.25f, (523f, 0.05f), (587f, 0.05f), (659f, 0.05f), (784f, 0.05f), (1046f, 0.2f)));
        // Habitat: what an animal sounds like in the wrong place (splash in water, flopping out of it, chattering teeth elsewhere).
        public void Splash() => Play("splash", () => ProceduralAudio.GenerateTone(250f, 0.2f, 0.3f));
        public void Flop() => Play("flop", () => ProceduralAudio.GenerateTone(150f, 0.15f, 0.3f));
        public void Shiver() => Play("shiver", () => ProceduralAudio.GenerateTone(1000f, 0.1f, 0.2f));
        // Feeding: an animal chewing up a food (a real recording, Resources/Sfx/chomp, once imported).
        public void Chomp() => Play("chomp", () => ProceduralAudio.GenerateTone(320f, 0.08f, 0.3f));
        public void Pop() => Play("pop", () => ProceduralAudio.GenerateTone(700f, 0.05f));
        // Magnet: a thing jumps up and sticks to the magnet's tips.
        public void Clink() => Play("clink", () => ProceduralAudio.GenerateTone(1800f, 0.08f, 0.3f));

        // The real recording of an animal (Resources/Animals/<id>, from tools/animals/import.js). Plays it and returns its length in
        // seconds; 0 when effects are off or the animal has no clip.
        public float PlayAnimal(string animalId)
        {
            if (!Enabled) return 0f;
            var key = "animal:" + animalId;
            if (!_clips.TryGetValue(key, out var clip))
            {
                clip = Resources.Load<AudioClip>("Animals/" + animalId);
                _clips[key] = clip;
            }
            if (clip == null) return 0f;
            Source.PlayOneShot(clip);
            return clip.length;
        }

        // The fishing reel runs as a loop on its own source while the hook is out (line running off) or coming back (winding in).
        private AudioSource _reel;

        public void ReelOut() => StartReel("reelout", 380f);
        public void ReelIn() => StartReel("reelin", 170f);

        // The metal detector: a beep (the same over junk, so it stays a surprise), the continuous tone right over something (a loop on the reel's source, the two never
        // play together), and a shovel scoop.
        public void DetectorBeep() => Play("detbeep", () => ProceduralAudio.GenerateTone(1175f, 0.1f));
        public void DetectorHot() => StartReel("dethot", 1200f);
        public void Dig() => Play("dig", () => ProceduralAudio.GenerateTone(150f, 0.15f, 0.3f));

        // Jetpack Cat: a soft whoosh when the jetpack lights, then a quiet hum on its own looping source for as long as a finger is down.
        private AudioSource _thrust;

        public void ThrustStart()
        {
            Play("jetpack_on", () => ProceduralAudio.GenerateTone(120f, 0.2f, 0.2f));
            if (!Enabled) return;
            if (!_clips.TryGetValue("jetpack", out var clip) || clip == null)
            {
                clip = Resources.Load<AudioClip>("Sfx/jetpack") ?? ProceduralAudio.GenerateTone(110f, 0.3f, 0.1f);
                _clips["jetpack"] = clip;
            }
            if (_thrust == null)
            {
                _thrust = gameObject.AddComponent<AudioSource>();
                _thrust.playOnAwake = false;
                _thrust.loop = true;
            }
            _thrust.clip = clip;
            _thrust.Play();
        }

        public void ThrustStop()
        {
            if (_thrust != null) _thrust.Stop();
        }

        // Cuts every effect that is playing (one-shots and the reel/detector/jetpack loops), when a screen is left.
        public void StopAll()
        {
            if (_source != null) _source.Stop();
            if (_reel != null) _reel.Stop();
            if (_thrust != null) _thrust.Stop();
        }

        public void ReelStop()
        {
            if (_reel != null) _reel.Stop();
        }

        private void StartReel(string name, float fallbackHz)
        {
            if (!Enabled) return;
            if (!_clips.TryGetValue(name, out var clip) || clip == null)
            {
                clip = Resources.Load<AudioClip>("Sfx/" + name) ?? ProceduralAudio.GenerateTone(fallbackHz, 0.3f, 0.1f);
                _clips[name] = clip;
            }
            if (_reel == null)
            {
                _reel = gameObject.AddComponent<AudioSource>();
                _reel.playOnAwake = false;
                _reel.loop = true;
            }
            _reel.clip = clip;
            _reel.Play();
        }

        // False silences every effect (the Sound effects switch in Settings).
        public bool Enabled { get; set; } = true;

        private void Play(string name, Func<AudioClip> fallback)
        {
            if (!Enabled) return;
            if (!_clips.TryGetValue(name, out var clip) || clip == null)
            {
                clip = Resources.Load<AudioClip>("Sfx/" + name) ?? fallback();
                _clips[name] = clip;
            }
            Source.PlayOneShot(clip);
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
