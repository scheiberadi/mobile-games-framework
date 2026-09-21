using System.Collections.Generic;
using NUnit.Framework;
using MobileGamesFramework.Persistence;
using MobileGamesFramework.Monetization;

namespace MobileGamesFramework.Tests
{
    public class InterstitialCadenceTrackerTests
    {
        private class FakeKeyValueStore : IKeyValueStore
        {
            private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

            public string GetString(string key, string defaultValue) =>
                _values.TryGetValue(key, out var value) ? value : defaultValue;

            public void SetString(string key, string value) => _values[key] = value;
        }

        [Test]
        public void BelowCadence_ReturnsFalse()
        {
            var tracker = new InterstitialCadenceTracker(new FakeKeyValueStore());

            Assert.IsFalse(tracker.ShouldShowInterstitial("2048", 3));
            Assert.IsFalse(tracker.ShouldShowInterstitial("2048", 3));
        }

        [Test]
        public void OnCadenceCount_ReturnsTrue()
        {
            var tracker = new InterstitialCadenceTracker(new FakeKeyValueStore());

            tracker.ShouldShowInterstitial("2048", 3);
            tracker.ShouldShowInterstitial("2048", 3);
            var result = tracker.ShouldShowInterstitial("2048", 3);

            Assert.IsTrue(result);
        }

        [Test]
        public void AfterCadenceCount_ResetsUntilNextCadence()
        {
            var tracker = new InterstitialCadenceTracker(new FakeKeyValueStore());
            for (var i = 0; i < 3; i++) tracker.ShouldShowInterstitial("2048", 3);

            Assert.IsFalse(tracker.ShouldShowInterstitial("2048", 3));
            Assert.IsFalse(tracker.ShouldShowInterstitial("2048", 3));
            Assert.IsTrue(tracker.ShouldShowInterstitial("2048", 3));
        }

        [Test]
        public void DifferentGameIds_AreIndependent()
        {
            var store = new FakeKeyValueStore();
            var tracker = new InterstitialCadenceTracker(store);
            tracker.ShouldShowInterstitial("2048", 3);
            tracker.ShouldShowInterstitial("2048", 3);
            tracker.ShouldShowInterstitial("2048", 3);

            Assert.IsFalse(tracker.ShouldShowInterstitial("match3", 3));
        }
    }
}
