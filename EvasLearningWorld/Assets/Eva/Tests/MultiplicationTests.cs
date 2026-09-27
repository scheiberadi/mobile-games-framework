using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class MultiplicationTests
    {
        private static readonly int[] RowsMaxByLevel = { 2, 2, 3, 3, 3, 4 };
        private static readonly int[] ColsMaxByLevel = { 2, 3, 3, 4, 4, 5 };
        private static readonly bool[] ShowObjectsByLevel = { true, true, true, true, true, false };
        private static readonly int[] TileCountByLevel = { 3, 4, 5, 5, 6, 6 };

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MultiplicationRoundGenerator.Create(0, new Random(1), null));
            Assert.Throws<ArgumentOutOfRangeException>(() => MultiplicationRoundGenerator.Create(7, new Random(1), null));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = MultiplicationRoundGenerator.Create(4, new Random(7), null);
            var b = MultiplicationRoundGenerator.Create(4, new Random(7), null);
            Assert.That(a.Rows, Is.EqualTo(b.Rows));
            Assert.That(a.Cols, Is.EqualTo(b.Cols));
            Assert.That(a.Choices, Is.EqualTo(b.Choices));
        }

        [Test]
        public void RoundsPerSessionMatchesTheSharedFiveRoundWindow()
        {
            Assert.That(MultiplicationRoundGenerator.RoundsPerSession, Is.EqualTo(5));
        }

        [Test]
        public void ProductIsAlwaysRowsTimesCols()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = MultiplicationRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Product, Is.EqualTo(round.Rows * round.Cols), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void RowsAndColsStayWithinTheLevelsRange()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = MultiplicationRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Rows, Is.InRange(1, RowsMaxByLevel[level - 1]), "level " + level + " seed " + seed);
                Assert.That(round.Cols, Is.InRange(1, ColsMaxByLevel[level - 1]), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void ShowObjectsMatchesTheLevelTableAndDropsOnlyOnTheLastLevel()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            {
                var round = MultiplicationRoundGenerator.Create(level, new Random(1), null);
                Assert.That(round.ShowObjects, Is.EqualTo(ShowObjectsByLevel[level - 1]), "level " + level);
            }
        }

        [Test]
        public void TileCountMatchesTheLevelTable()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = MultiplicationRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Choices.Length, Is.EqualTo(TileCountByLevel[level - 1]), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void ChoicesAlwaysIncludeTheProductExactlyOnceAndAreDistinct()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = MultiplicationRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Choices.Count(c => c == round.Product), Is.EqualTo(1), "level " + level + " seed " + seed);
                Assert.That(round.Choices.Distinct().Count(), Is.EqualTo(round.Choices.Length), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void ChoicesAreNeverNegative()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = MultiplicationRoundGenerator.Create(level, new Random(seed), null);
                foreach (var choice in round.Choices) Assert.That(choice, Is.GreaterThanOrEqualTo(0), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void NeverRepeatsThePreviousProductWhenTheRangeAllowsAChoice()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var first = MultiplicationRoundGenerator.Create(level, new Random(seed), null);
                var second = MultiplicationRoundGenerator.Create(level, new Random(seed + 1000), first.Product);
                Assert.AreNotEqual(first.Product, second.Product, "level " + level + " seed " + seed);
            }
        }

        // From level 3, an off-by-one distractor (the classic multiplication slip) must appear alongside the
        // product whenever one is in range - same guaranteed-confusable shape as Addition/Number Hunt.
        [Test]
        public void OffByOneDistractorAppearsFromLevelThree()
        {
            for (var level = 3; level <= 6; level++)
            for (var seed = 0; seed < 300; seed++)
            {
                var round = MultiplicationRoundGenerator.Create(level, new Random(seed), null);
                var hasNeighbor = round.Choices.Contains(round.Product - 1) || round.Choices.Contains(round.Product + 1);
                Assert.IsTrue(hasNeighbor, "level " + level + " seed " + seed + " product " + round.Product);
            }
        }
    }
}
