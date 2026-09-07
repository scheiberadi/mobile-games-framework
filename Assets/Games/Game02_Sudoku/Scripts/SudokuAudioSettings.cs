using MobileGamesFramework.Persistence;

namespace Game02_Sudoku
{
    public class SudokuAudioSettings
    {
        private const string MusicKey = "settings.musicEnabled";
        private const string SfxKey = "settings.sfxEnabled";

        private readonly IKeyValueStore _store;

        public SudokuAudioSettings(IKeyValueStore store)
        {
            _store = store;
        }

        public bool MusicEnabled => _store.GetString(MusicKey, "1") == "1";
        public bool SfxEnabled => _store.GetString(SfxKey, "1") == "1";

        public void SetMusicEnabled(bool enabled) => _store.SetString(MusicKey, enabled ? "1" : "0");
        public void SetSfxEnabled(bool enabled) => _store.SetString(SfxKey, enabled ? "1" : "0");
    }
}
