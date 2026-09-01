using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MobileGamesFramework.Grid;
using MobileGamesFramework.Persistence;
using Game01_2048;

namespace Game01_2048.Tests
{
    public class Game2048SaveServiceTests
    {
        private class FakeKeyValueStore : IKeyValueStore
        {
            private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

            public string GetString(string key, string defaultValue) =>
                _values.TryGetValue(key, out var value) ? value : defaultValue;

            public void SetString(string key, string value) => _values[key] = value;
        }

        private class NoSpawn : ITileSpawnStrategy
        {
            public void Spawn(GridCore<int> grid) { }
        }

        [Test]
        public void HasSave_Initially_ReturnsFalse()
        {
            var saveService = new Game2048SaveService(new FakeKeyValueStore());

            Assert.IsFalse(saveService.HasSave());
        }

        [Test]
        public void HasSave_AfterSave_ReturnsTrue()
        {
            var saveService = new Game2048SaveService(new FakeKeyValueStore());
            var game = new Game2048Game(new NoSpawn(), 2, 1);

            saveService.Save(game);

            Assert.IsTrue(saveService.HasSave());
        }

        [Test]
        public void ClearSave_RemovesSave()
        {
            var saveService = new Game2048SaveService(new FakeKeyValueStore());
            var game = new Game2048Game(new NoSpawn(), 2, 1);
            saveService.Save(game);

            saveService.ClearSave();

            Assert.IsFalse(saveService.HasSave());
        }

        [Test]
        public void TryLoad_WithNoSave_ReturnsFalse()
        {
            var saveService = new Game2048SaveService(new FakeKeyValueStore());

            var loaded = saveService.TryLoad(new NoSpawn(), out var game);

            Assert.IsFalse(loaded);
            Assert.IsNull(game);
        }

        [Test]
        public void Save_ThenTryLoad_RestoresGridValuesAndScore()
        {
            var original = new Game2048Game(new NoSpawn(), 2, 1);
            var grid = new GridCore<int>(2, 1);
            grid.Set(new GridPosition(0, 0), 4);
            grid.Set(new GridPosition(0, 1), 8);
            original.RestoreState(grid, 120);
            var saveService = new Game2048SaveService(new FakeKeyValueStore());

            saveService.Save(original);
            var loaded = saveService.TryLoad(new NoSpawn(), out var restored);

            Assert.IsTrue(loaded);
            Assert.AreEqual(120, restored.Score);
            Assert.AreEqual(4, restored.Grid.Get(new GridPosition(0, 0)));
            Assert.AreEqual(8, restored.Grid.Get(new GridPosition(0, 1)));
            Assert.AreEqual(GameState.Playing, restored.State);
        }
    }
}
