using System.Collections.Generic;
using NUnit.Framework;
using MobileGamesFramework.Persistence;
using Game02_Sudoku;

namespace Game02_Sudoku.Tests
{
    public class SudokuAudioSettingsTests
    {
        private class FakeKeyValueStore : IKeyValueStore
        {
            private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

            public string GetString(string key, string defaultValue) =>
                _values.TryGetValue(key, out var value) ? value : defaultValue;

            public void SetString(string key, string value) => _values[key] = value;
        }

        [Test]
        public void MusicEnabled_Initially_ReturnsTrue()
        {
            var settings = new SudokuAudioSettings(new FakeKeyValueStore());

            Assert.IsTrue(settings.MusicEnabled);
        }

        [Test]
        public void SfxEnabled_Initially_ReturnsTrue()
        {
            var settings = new SudokuAudioSettings(new FakeKeyValueStore());

            Assert.IsTrue(settings.SfxEnabled);
        }

        [Test]
        public void SetMusicEnabled_False_PersistsAsFalse()
        {
            var settings = new SudokuAudioSettings(new FakeKeyValueStore());

            settings.SetMusicEnabled(false);

            Assert.IsFalse(settings.MusicEnabled);
        }

        [Test]
        public void SetSfxEnabled_False_PersistsAsFalse()
        {
            var settings = new SudokuAudioSettings(new FakeKeyValueStore());

            settings.SetSfxEnabled(false);

            Assert.IsFalse(settings.SfxEnabled);
        }

        [Test]
        public void MusicAndSfxToggles_AreIndependent()
        {
            var settings = new SudokuAudioSettings(new FakeKeyValueStore());

            settings.SetMusicEnabled(false);

            Assert.IsFalse(settings.MusicEnabled);
            Assert.IsTrue(settings.SfxEnabled);
        }
    }
}
