using NUnit.Framework;
using UnityEngine;
using MobileGamesFramework.Localization;

namespace MobileGamesFramework.Tests
{
    public class LocalizationLoaderTests
    {
        [TestCase(SystemLanguage.English, Language.English)]
        [TestCase(SystemLanguage.Spanish, Language.Spanish)]
        [TestCase(SystemLanguage.Portuguese, Language.Portuguese)]
        [TestCase(SystemLanguage.German, Language.German)]
        [TestCase(SystemLanguage.French, Language.French)]
        [TestCase(SystemLanguage.Japanese, Language.Japanese)]
        [TestCase(SystemLanguage.Korean, Language.Korean)]
        [TestCase(SystemLanguage.Russian, Language.Russian)]
        [TestCase(SystemLanguage.Italian, Language.Italian)]
        [TestCase(SystemLanguage.Romanian, Language.Romanian)]
        public void MapSystemLanguage_DirectlySupportedLanguage_MapsExactly(SystemLanguage systemLanguage, Language expected)
        {
            Assert.AreEqual(expected, LocalizationLoader.MapSystemLanguage(systemLanguage));
        }

        [TestCase(SystemLanguage.Chinese)]
        [TestCase(SystemLanguage.ChineseSimplified)]
        [TestCase(SystemLanguage.ChineseTraditional)]
        public void MapSystemLanguage_AnyChineseVariant_MapsToChineseSimplified(SystemLanguage systemLanguage)
        {
            Assert.AreEqual(Language.ChineseSimplified, LocalizationLoader.MapSystemLanguage(systemLanguage));
        }

        [TestCase(SystemLanguage.Thai)]
        [TestCase(SystemLanguage.Arabic)]
        [TestCase(SystemLanguage.Hindi)]
        [TestCase(SystemLanguage.Dutch)]
        [TestCase(SystemLanguage.Unknown)]
        public void MapSystemLanguage_UnsupportedLanguage_FallsBackToEnglish(SystemLanguage systemLanguage)
        {
            Assert.AreEqual(Language.English, LocalizationLoader.MapSystemLanguage(systemLanguage));
        }
    }
}
