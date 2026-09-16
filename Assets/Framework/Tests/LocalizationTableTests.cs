using System.Collections.Generic;
using NUnit.Framework;
using MobileGamesFramework.Localization;

namespace MobileGamesFramework.Tests
{
    public class LocalizationTableTests
    {
        [Test]
        public void Get_KeyPresent_ReturnsValue()
        {
            var table = new LocalizationTable(new Dictionary<string, string> { { "hello", "Hello" } });

            Assert.AreEqual("Hello", table.Get("hello"));
        }

        [Test]
        public void Get_KeyMissingNoFallback_ReturnsKeyItself()
        {
            var table = new LocalizationTable(new Dictionary<string, string>());

            Assert.AreEqual("missing.key", table.Get("missing.key"));
        }

        [Test]
        public void Get_KeyMissingFromPrimary_FallsBackToFallbackTable()
        {
            var fallback = new LocalizationTable(new Dictionary<string, string> { { "hello", "Hello" } });
            var table = new LocalizationTable(new Dictionary<string, string>(), fallback);

            Assert.AreEqual("Hello", table.Get("hello"));
        }

        [Test]
        public void Get_KeyPresentInBoth_PrefersPrimaryOverFallback()
        {
            var fallback = new LocalizationTable(new Dictionary<string, string> { { "hello", "Hello" } });
            var table = new LocalizationTable(new Dictionary<string, string> { { "hello", "Bonjour" } }, fallback);

            Assert.AreEqual("Bonjour", table.Get("hello"));
        }

        [Test]
        public void Get_KeyMissingFromBoth_ReturnsKeyItself()
        {
            var fallback = new LocalizationTable(new Dictionary<string, string>());
            var table = new LocalizationTable(new Dictionary<string, string>(), fallback);

            Assert.AreEqual("missing.key", table.Get("missing.key"));
        }

        [Test]
        public void GetWithArgs_FormatsTemplate()
        {
            var table = new LocalizationTable(new Dictionary<string, string> { { "hintsLeft", "Hints left: {0}" } });

            Assert.AreEqual("Hints left: 3", table.Get("hintsLeft", 3));
        }

        [Test]
        public void GetWithArgs_MultiplePlaceholders_FormatsAllOfThem()
        {
            var table = new LocalizationTable(new Dictionary<string, string> { { "time", "Time: {0}   Best: {1}" } });

            Assert.AreEqual("Time: 01:23   Best: 00:45", table.Get("time", "01:23", "00:45"));
        }
    }
}
