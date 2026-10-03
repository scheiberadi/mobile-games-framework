using UnityEngine;

namespace EvasLearningWorld.App
{
    // Plays one looping background track at a time. A new track fades the old one out first, a screen with no track fades to silence,
    // and the music dips while Eva speaks so she is always heard. The AudioSource is created lazily so the class also works in edit
    // mode tests, where Update never runs and Play only records what is wanted.
    public sealed class MusicPlayer : MonoBehaviour
    {
        public const float FullVolume = 0.35f;
        public const float DuckedVolume = 0.12f;
        private const float FadeSeconds = 0.8f;

        private AudioSource _source;
        private string _playing;
        private float _volume;

        // False silences the music (the Music switch in Settings).
        public bool Enabled { get; set; } = true;

        // Eva's voice, for the dip; null leaves the music at full volume.
        public Voice Voice { get; set; }

        // The track the current screen asks for (null = none).
        public string Wanted { get; private set; }

        public void Play(string track) => Wanted = track;

        private void Update()
        {
            var wanted = Enabled ? Wanted : null;
            if (_playing != wanted)
            {
                _volume = Mathf.MoveTowards(_volume, 0f, Time.unscaledDeltaTime / FadeSeconds);
                if (_volume > 0.001f && _source != null && _source.isPlaying) { _source.volume = _volume; return; }
                Switch(wanted);
            }
            var target = _playing == null ? 0f : Voice != null && Voice.IsSpeaking ? DuckedVolume : FullVolume;
            _volume = Mathf.MoveTowards(_volume, target, Time.unscaledDeltaTime / FadeSeconds * FullVolume);
            if (_source != null) _source.volume = _volume;
        }

        private void Switch(string track)
        {
            _volume = 0f;
            _playing = null;
            if (_source != null) _source.Stop();
            if (track == null) return;
            var clip = Resources.Load<AudioClip>("Music/" + track);
            if (clip == null)
            {
                Debug.LogWarning("Music: no clip for track " + track);
                return;
            }
            if (_source == null)
            {
                _source = gameObject.AddComponent<AudioSource>();
                _source.playOnAwake = false;
                _source.loop = true;
            }
            _source.clip = clip;
            _source.volume = 0f;
            _source.Play();
            _playing = track;
        }
    }
}
