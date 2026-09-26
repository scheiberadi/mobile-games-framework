using MobileGamesFramework.Localization;

namespace EvasLearningWorld.App
{
    // Adult-facing text (Settings and the Parent gate) in the languages the framework ships. Follows the phone language
    // like Sudoku does; English fills any missing key. The child-facing screens use no text, so nothing else needs this.
    public static class Loc
    {
        public const string ResourcesFolder = "Localization/Eva";

        private static LocalizationTable _table;

        // Forces a language (tests); null goes back to following the phone.
        public static void Use(Language? language)
        {
            _table = language.HasValue ? Load(language.Value) : null;
        }

        public static string Get(string key)
        {
            if (_table == null) _table = Load(LocalizationLoader.DetectSystemLanguage());
            return _table.Get(key);
        }

        private static LocalizationTable Load(Language language)
        {
            var english = new LocalizationTable(LocalizationLoader.LoadValues(Language.English, ResourcesFolder));
            return language == Language.English
                ? english
                : new LocalizationTable(LocalizationLoader.LoadValues(language, ResourcesFolder), english);
        }
    }
}
