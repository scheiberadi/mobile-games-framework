using System;
using System.Collections;
using UnityEngine;

namespace EvasLearningWorld.App
{
    // Eva's voice. Plays Resources/Voice/en/<key> when the clip exists; a missing clip is silent but still "speaks" for
    // Duration(key), so animation and the speech bubble behave the same before the audio is generated.
    public sealed class Voice : MonoBehaviour
    {
        private AudioSource _source;
        private Coroutine _speaking;

        // False silences the voice (the Voice switch in Settings); a line still "speaks" for its duration so animation,
        // the speech bubble and the flow that waits on it behave the same.
        public bool Enabled { get; set; } = true;

        public event Action<bool> SpeakingChanged;
        // Raised at the start of every Say with its key (in order), so tests can see which line came first.
        public event Action<string> Said;
        public bool IsSpeaking { get; private set; }
        public string LastKey { get; private set; }

        private AudioSource Source
        {
            get
            {
                if (_source == null)
                {
                    var child = new GameObject("VoiceSource");
                    child.transform.SetParent(transform, false);
                    _source = child.AddComponent<AudioSource>();
                    _source.playOnAwake = false;
                }
                return _source;
            }
        }

        public void Say(string key)
        {
            if (_speaking != null) StopCoroutine(_speaking);
            Source.Stop();
            LastKey = key;
            Said?.Invoke(key);
            var clip = LoadClip(key);
            if (clip != null && Enabled)
            {
                Source.clip = clip;
                Source.Play();
            }
            _speaking = StartCoroutine(SpeakFor(Duration(key)));
        }

        // A short pause after every spoken line, so back-to-back lines and the actions that follow them
        // don't run together (a child needs a beat to take each one in).
        public const float BreathSeconds = 0.35f;

        public IEnumerator SayAndWait(string key)
        {
            Say(key);
            yield return new WaitForSeconds(Duration(key) + BreathSeconds);
        }

        public float Duration(string key)
        {
            var clip = LoadClip(key);
            if (clip != null) return clip.length;
            return Mathf.Max(1f, 0.06f * VoiceLines.TextFor(key).Length);
        }

        private static AudioClip LoadClip(string key) => string.IsNullOrEmpty(key) ? null : Resources.Load<AudioClip>("Voice/en/" + key);

        private IEnumerator SpeakFor(float seconds)
        {
            SetSpeaking(true);
            yield return new WaitForSeconds(seconds);
            _speaking = null;
            SetSpeaking(false);
        }

        private void SetSpeaking(bool speaking)
        {
            if (IsSpeaking == speaking) return;
            IsSpeaking = speaking;
            SpeakingChanged?.Invoke(speaking);
        }
    }
}
