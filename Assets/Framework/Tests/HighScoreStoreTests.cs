using System.Collections.Generic;
using NUnit.Framework;
using MobileGamesFramework.Persistence;

namespace MobileGamesFramework.Tests
{
    public class HighScoreStoreTests
    {
        private class FakeKeyValueStore : IKeyValueStore
        {
            private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

            public string GetString(string key, string defaultValue) =>
                _values.TryGetValue(key, out var value) ? value : defaultValue;

            public void SetString(string key, string value) => _values[key] = value;
        }

        [Test]
        public void GetHighScore_NoScoreYet_ReturnsZero()
        {
            var store = new HighScoreStore(new FakeKeyValueStore());

            Assert.AreEqual(0, store.GetHighScore("2048"));
        }

        [Test]
        public void ReportScore_HigherThanCurrent_UpdatesHighScore()
        {
            var store = new HighScoreStore(new FakeKeyValueStore());

            store.ReportScore("2048", 500);

            Assert.AreEqual(500, store.GetHighScore("2048"));
        }

        [Test]
        public void ReportScore_LowerThanCurrent_DoesNotOverwrite()
        {
            var store = new HighScoreStore(new FakeKeyValueStore());
            store.ReportScore("2048", 500);

            store.ReportScore("2048", 100);

            Assert.AreEqual(500, store.GetHighScore("2048"));
        }

        [Test]
        public void ReportScore_DifferentGameIds_AreIndependent()
        {
            var store = new HighScoreStore(new FakeKeyValueStore());

            store.ReportScore("2048", 500);
            store.ReportScore("match3", 200);

            Assert.AreEqual(500, store.GetHighScore("2048"));
            Assert.AreEqual(200, store.GetHighScore("match3"));
        }
    }
}
