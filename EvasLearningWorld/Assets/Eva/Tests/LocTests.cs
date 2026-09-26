using System.Collections.Generic;
using EvasLearningWorld.App;
using MobileGamesFramework.Localization;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class LocTests
    {
        private static readonly string[] Keys =
        {
            "gate.prompt", "settings.title", "settings.music", "settings.sfx", "settings.voice",
            "settings.reset", "settings.resetConfirm", "common.yes", "common.no",
        };

        [TearDown]
        public void TearDown() => Loc.Use(null);

        private static IEnumerable<Language> Languages() => (Language[])System.Enum.GetValues(typeof(Language));

        [TestCaseSource(nameof(Languages))]
        public void EveryLanguageTranslatesEveryEvaKey(Language language)
        {
            var values = LocalizationLoader.LoadValues(language, Loc.ResourcesFolder);
            foreach (var key in Keys)
            {
                Assert.IsTrue(values.TryGetValue(key, out var value), language + " lacks " + key);
                Assert.IsNotEmpty(value, language + " " + key);
            }
            Assert.That(values.Count, Is.EqualTo(Keys.Length), language + " has keys the code does not use");
        }

        [Test]
        public void NonEnglishLanguagesReallyDifferFromEnglishAndFollowTheChosenLanguage()
        {
            Loc.Use(Language.English);
            Assert.That(Loc.Get("settings.title"), Is.EqualTo("Settings"));
            Loc.Use(Language.German);
            Assert.That(Loc.Get("settings.title"), Is.EqualTo("Einstellungen"));
        }

        [Test]
        public void AnUnknownKeyFallsBackToTheKeyItself()
        {
            Loc.Use(Language.French);
            Assert.That(Loc.Get("no.such.key"), Is.EqualTo("no.such.key"));
        }
    }
}
