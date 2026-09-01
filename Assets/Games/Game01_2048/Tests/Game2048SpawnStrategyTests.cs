using System;
using System.Linq;
using NUnit.Framework;
using MobileGamesFramework.Grid;
using Game01_2048;

namespace Game01_2048.Tests
{
    public class Game2048SpawnStrategyTests
    {
        [Test]
        public void Spawn_FillsExactlyOneEmptyCell()
        {
            var grid = new GridCore<int>(4, 4);
            var strategy = new Game2048SpawnStrategy(new Random(1));

            strategy.Spawn(grid);

            Assert.AreEqual(15, grid.GetEmptyPositions().Count());
        }

        [Test]
        public void Spawn_OnlyProducesTwoOrFour()
        {
            var strategy = new Game2048SpawnStrategy(new Random(1));

            for (var trial = 0; trial < 200; trial++)
            {
                var grid = new GridCore<int>(4, 4);
                strategy.Spawn(grid);
                var value = grid.AllPositions().Select(grid.Get).First(v => v.HasValue).Value;
                Assert.That(value, Is.EqualTo(2).Or.EqualTo(4));
            }
        }

        [Test]
        public void Spawn_RoughlyMatchesNinetyTenOdds()
        {
            var strategy = new Game2048SpawnStrategy(new Random(42));
            var twoCount = 0;
            const int trials = 1000;

            for (var trial = 0; trial < trials; trial++)
            {
                var grid = new GridCore<int>(4, 4);
                strategy.Spawn(grid);
                var value = grid.AllPositions().Select(grid.Get).First(v => v.HasValue).Value;
                if (value == 2) twoCount++;
            }

            var ratio = twoCount / (double)trials;
            Assert.That(ratio, Is.InRange(0.85, 0.95));
        }

        [Test]
        public void Spawn_OnFullGrid_DoesNothing()
        {
            var grid = new GridCore<int>(1, 1);
            grid.Set(new GridPosition(0, 0), 2);
            var strategy = new Game2048SpawnStrategy(new Random(1));

            strategy.Spawn(grid);

            Assert.AreEqual(2, grid.Get(new GridPosition(0, 0)));
        }
    }
}
