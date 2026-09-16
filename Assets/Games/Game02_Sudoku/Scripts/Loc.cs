using MobileGamesFramework.Localization;
using MobileGamesFramework.Persistence;

namespace Game02_Sudoku
{
    public static class Loc
    {
        private const string ResourcesFolder = "Localization/Sudoku";

        private static LocalizationTable _table;
        private static Language _currentLanguage;

        private static SudokuLocalizationSettings Settings => new SudokuLocalizationSettings(new PlayerPrefsStore());

        private static void EnsureLoaded()
        {
            if (_table != null) return;

            var settings = Settings;
            var language = settings.StoredLanguage;
            if (!language.HasValue)
            {
                // First launch: detect once, then persist immediately so every
                // later launch (and every later Loc call this session) behaves
                // exactly like an explicit user pick from here on.
                language = LocalizationLoader.DetectSystemLanguage();
                settings.SetLanguage(language.Value);
            }

            LoadLanguage(language.Value);
        }

        private static void LoadLanguage(Language language)
        {
            var englishTable = new LocalizationTable(LocalizationLoader.LoadValues(Language.English, ResourcesFolder));

            _table = language == Language.English
                ? englishTable
                : new LocalizationTable(LocalizationLoader.LoadValues(language, ResourcesFolder), englishTable);

            _currentLanguage = language;
        }

        public static Language CurrentLanguage
        {
            get { EnsureLoaded(); return _currentLanguage; }
        }

        // Called by the Settings screen's language picker. Persists the choice and
        // reloads this static cache; the caller is still responsible for reloading
        // the active scene so on-screen text actually refreshes (see SudokuSettingsController).
        public static void SetLanguage(Language language)
        {
            Settings.SetLanguage(language);
            LoadLanguage(language);
        }

        public static string Get(string key)
        {
            EnsureLoaded();
            return _table.Get(key);
        }

        public static string Get(string key, params object[] args)
        {
            EnsureLoaded();
            return _table.Get(key, args);
        }

        // Difficulty enum names (Easy/Medium/Hard/Expert) map 1:1 to
        // difficulty.easy/medium/hard/expert keys by construction - see
        // Resources/Localization/Sudoku/en.json for the canonical key list.
        public static string Difficulty(Difficulty difficulty) =>
            Get($"difficulty.{difficulty.ToString().ToLowerInvariant()}");
    }
}
