using System;
using NUnit.Framework;
using MobileGamesFramework.Localization;

namespace MobileGamesFramework.Tests
{
    public class LanguageInfoTests
    {
        [Test]
        public void Code_EveryLanguage_ReturnsNonEmptyUniqueCode()
        {
            var codes = new System.Collections.Generic.HashSet<string>();
            foreach (Language language in Enum.GetValues(typeof(Language)))
            {
                var code = LanguageInfo.Code(language);
                Assert.IsNotEmpty(code, $"{language} has an empty code");
                Assert.IsTrue(codes.Add(code), $"{language}'s code '{code}' collides with another language");
            }
        }

        [Test]
        public void NativeName_EveryLanguage_ReturnsNonEmptyName()
        {
            foreach (Language language in Enum.GetValues(typeof(Language)))
            {
                Assert.IsNotEmpty(LanguageInfo.NativeName(language), $"{language} has an empty native name");
            }
        }

        [Test]
        public void Code_English_ReturnsEn()
        {
            Assert.AreEqual("en", LanguageInfo.Code(Language.English));
        }

        [Test]
        public void Code_Romanian_ReturnsRo()
        {
            Assert.AreEqual("ro", LanguageInfo.Code(Language.Romanian));
        }

        [Test]
        public void NativeName_Romanian_ReturnsRomana()
        {
            Assert.AreEqual("Română", LanguageInfo.NativeName(Language.Romanian));
        }
    }
}
