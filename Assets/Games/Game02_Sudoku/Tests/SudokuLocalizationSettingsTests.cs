using System.Collections.Generic;
using NUnit.Framework;
using MobileGamesFramework.Persistence;
using MobileGamesFramework.Localization;
using Game02_Sudoku;

namespace Game02_Sudoku.Tests
{
    public class SudokuLocalizationSettingsTests
    {
        private class FakeKeyValueStore : IKeyValueStore
        {
            private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

            public string GetString(string key, string defaultValue) =>
                _values.TryGetValue(key, out var value) ? value : defaultValue;

            public void SetString(string key, string value) => _values[key] = value;
        }

        [Test]
        public void StoredLanguage_Initially_ReturnsNull()
        {
            var settings = new SudokuLocalizationSettings(new FakeKeyValueStore());

            Assert.IsNull(settings.StoredLanguage);
        }

        [Test]
        public void SetLanguage_ThenStoredLanguage_ReturnsThatLanguage()
        {
            var settings = new SudokuLocalizationSettings(new FakeKeyValueStore());

            settings.SetLanguage(Language.Romanian);

            Assert.AreEqual(Language.Romanian, settings.StoredLanguage);
        }

        [Test]
        public void SetLanguage_Twice_PersistsTheLatestValue()
        {
            var settings = new SudokuLocalizationSettings(new FakeKeyValueStore());

            settings.SetLanguage(Language.Romanian);
            settings.SetLanguage(Language.German);

            Assert.AreEqual(Language.German, settings.StoredLanguage);
        }

        [Test]
        public void StoredLanguage_UnparseableStoredValue_ReturnsNull()
        {
            var store = new FakeKeyValueStore();
            store.SetString("settings.language", "not-a-real-language");
            var settings = new SudokuLocalizationSettings(store);

            Assert.IsNull(settings.StoredLanguage);
        }
    }
}
