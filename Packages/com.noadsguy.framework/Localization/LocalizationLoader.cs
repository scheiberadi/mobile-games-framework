using System;
using System.Collections.Generic;
using UnityEngine;

namespace MobileGamesFramework.Localization
{
    public static class LocalizationLoader
    {
        [Serializable]
        private class LocalizationEntry
        {
            public string key;
            public string value;
        }

        [Serializable]
        private class LocalizationFile
        {
            public LocalizationEntry[] entries;
        }

        public static Language DetectSystemLanguage() => MapSystemLanguage(Application.systemLanguage);

        // Exact matches only - no partial/fuzzy matching, per the design spec. Any
        // SystemLanguage value not listed here (including all three Chinese
        // variants, which map to the single ChineseSimplified content we ship)
        // falls back to English.
        public static Language MapSystemLanguage(SystemLanguage systemLanguage)
        {
            switch (systemLanguage)
            {
                case SystemLanguage.English: return Language.English;
                case SystemLanguage.Spanish: return Language.Spanish;
                case SystemLanguage.Portuguese: return Language.Portuguese;
                case SystemLanguage.German: return Language.German;
                case SystemLanguage.French: return Language.French;
                case SystemLanguage.Japanese: return Language.Japanese;
                case SystemLanguage.Korean: return Language.Korean;
                case SystemLanguage.Russian: return Language.Russian;
                case SystemLanguage.Chinese: return Language.ChineseSimplified;
                case SystemLanguage.ChineseSimplified: return Language.ChineseSimplified;
                case SystemLanguage.ChineseTraditional: return Language.ChineseSimplified;
                case SystemLanguage.Italian: return Language.Italian;
                case SystemLanguage.Romanian: return Language.Romanian;
                default: return Language.English;
            }
        }

        // Not unit-tested (needs Resources.Load, which needs a running Unity
        // instance) - covered by Task 14's compile-check build and Task 15's
        // on-device verification instead.
        public static Dictionary<string, string> LoadValues(Language language, string resourcesFolder)
        {
            var values = new Dictionary<string, string>();
            var path = $"{resourcesFolder}/{LanguageInfo.Code(language)}";
            var asset = Resources.Load<TextAsset>(path);
            if (asset == null)
            {
                if (Application.isEditor || Debug.isDebugBuild)
                    Debug.LogWarning($"Localization file not found at Resources/{path}");
                return values;
            }

            var file = JsonUtility.FromJson<LocalizationFile>(asset.text);
            if (file?.entries == null) return values;

            foreach (var entry in file.entries)
                values[entry.key] = entry.value;

            return values;
        }
    }
}
