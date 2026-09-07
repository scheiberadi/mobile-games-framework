using System.Collections.Generic;
using UnityEngine;
using MobileGamesFramework.Persistence;

namespace Game02_Sudoku
{
    // Calm background music shared across every Sudoku screen. Created once at app
    // launch (see SudokuAppBootstrap) and kept alive across scene loads via
    // DontDestroyOnLoad so navigating between menu/play/settings/high-scores doesn't
    // restart or overlap tracks. Reads whatever clips exist under
    // Assets/Resources/Audio - with none present yet it just stays silent instead
    // of throwing, so this ships ahead of the actual audio files landing.
    public class SudokuMusicPlayer : MonoBehaviour
    {
        private const string ResourcesFolder = "Audio";
        private const float Volume = 0.35f;

        private AudioSource _source;
        private List<AudioClip> _playlist;
        private int _nextIndex;

        public static SudokuMusicPlayer Instance { get; private set; }

        public static SudokuMusicPlayer CreateIfMissing()
        {
            if (Instance != null) return Instance;

            var host = new GameObject("SudokuMusicPlayer");
            Instance = host.AddComponent<SudokuMusicPlayer>();
            DontDestroyOnLoad(host);
            return Instance;
        }

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            _source.volume = Volume;

            _playlist = new List<AudioClip>(Resources.LoadAll<AudioClip>(ResourcesFolder));
            Shuffle(_playlist);

            var settings = new SudokuAudioSettings(new PlayerPrefsStore());
            SetMusicEnabled(settings.MusicEnabled);
        }

        private void Update()
        {
            if (!_source.mute && !_source.isPlaying && _playlist.Count > 0) PlayNext();
        }

        // mute (rather than Stop/Pause) so toggling back on resumes exactly where the
        // current track was, instead of restarting or skipping a track.
        public void SetMusicEnabled(bool enabled)
        {
            _source.mute = !enabled;
            if (enabled && !_source.isPlaying && _playlist.Count > 0) PlayNext();
        }

        private void PlayNext()
        {
            if (_nextIndex >= _playlist.Count)
            {
                _nextIndex = 0;
                Shuffle(_playlist);
            }

            _source.clip = _playlist[_nextIndex];
            _nextIndex++;
            _source.Play();
        }

        private static void Shuffle(List<AudioClip> clips)
        {
            var random = new System.Random();
            for (var i = clips.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (clips[i], clips[j]) = (clips[j], clips[i]);
            }
        }
    }
}
