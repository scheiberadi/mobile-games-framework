namespace MobileGamesFramework.Localization
{
    public enum Language
    {
        English,
        Spanish,
        Portuguese,
        German,
        French,
        Japanese,
        Korean,
        Russian,
        ChineseSimplified,
        Italian,
        Romanian
    }

    public static class LanguageInfo
    {
        // ISO-ish code used as the Resources filename for this language's content
        // (see LocalizationLoader) - kept separate from NativeName since a display
        // string and a filename-safe identifier are different concerns.
        public static string Code(Language language)
        {
            switch (language)
            {
                case Language.English: return "en";
                case Language.Spanish: return "es";
                case Language.Portuguese: return "pt";
                case Language.German: return "de";
                case Language.French: return "fr";
                case Language.Japanese: return "ja";
                case Language.Korean: return "ko";
                case Language.Russian: return "ru";
                case Language.ChineseSimplified: return "zh-Hans";
                case Language.Italian: return "it";
                case Language.Romanian: return "ro";
                default: return "en";
            }
        }

        // Always shown in the language itself (not translated into the currently
        // active language) so a player can find their own language in a picker
        // even if they can't read whatever language is currently active.
        public static string NativeName(Language language)
        {
            switch (language)
            {
                case Language.English: return "English";
                case Language.Spanish: return "Español";
                case Language.Portuguese: return "Português";
                case Language.German: return "Deutsch";
                case Language.French: return "Français";
                case Language.Japanese: return "日本語";
                case Language.Korean: return "한국어";
                case Language.Russian: return "Русский";
                case Language.ChineseSimplified: return "简体中文";
                case Language.Italian: return "Italiano";
                case Language.Romanian: return "Română";
                default: return language.ToString();
            }
        }
    }
}
