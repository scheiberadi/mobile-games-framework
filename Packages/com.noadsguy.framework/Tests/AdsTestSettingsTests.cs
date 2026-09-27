using System.Collections.Generic;
using NUnit.Framework;
using MobileGamesFramework.Persistence;
using MobileGamesFramework.Monetization;

namespace MobileGamesFramework.Tests
{
    public class AdsTestSettingsTests
    {
        private class FakeKeyValueStore : IKeyValueStore
        {
            private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

            public string GetString(string key, string defaultValue) =>
                _values.TryGetValue(key, out var value) ? value : defaultValue;

            public void SetString(string key, string value) => _values[key] = value;
        }

        [Test]
        public void AdsDisabledForTesting_Initially_ReturnsFalse()
        {
            var settings = new AdsTestSettings(new FakeKeyValueStore());

            Assert.IsFalse(settings.AdsDisabledForTesting);
        }

        [Test]
        public void SetAdsDisabledForTesting_True_PersistsAsTrue()
        {
            var settings = new AdsTestSettings(new FakeKeyValueStore());

            settings.SetAdsDisabledForTesting(true);

            Assert.IsTrue(settings.AdsDisabledForTesting);
        }

        [Test]
        public void SetAdsDisabledForTesting_BackToFalse_PersistsAsFalse()
        {
            var settings = new AdsTestSettings(new FakeKeyValueStore());
            settings.SetAdsDisabledForTesting(true);

            settings.SetAdsDisabledForTesting(false);

            Assert.IsFalse(settings.AdsDisabledForTesting);
        }
    }
}
