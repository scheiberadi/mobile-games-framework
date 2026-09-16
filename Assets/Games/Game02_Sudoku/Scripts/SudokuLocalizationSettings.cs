using MobileGamesFramework.Persistence;
using MobileGamesFramework.Localization;

namespace Game02_Sudoku
{
    public class SudokuLocalizationSettings
    {
        private const string LanguageKey = "settings.language";

        private readonly IKeyValueStore _store;

        public SudokuLocalizationSettings(IKeyValueStore store)
        {
            _store = store;
        }

        // Null means "never set" (first launch) - distinct from any real Language
        // value, which is what tells Loc whether to auto-detect from the system.
        public Language? StoredLanguage
        {
            get
            {
                var raw = _store.GetString(LanguageKey, "");
                if (string.IsNullOrEmpty(raw)) return null;
                return System.Enum.TryParse<Language>(raw, out var language) ? language : (Language?)null;
            }
        }

        public void SetLanguage(Language language) => _store.SetString(LanguageKey, language.ToString());
    }
}
