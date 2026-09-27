using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class NumberOrderingTests
    {
        private static readonly int[] TileCountByLevel = { 3, 4, 5, 5, 6, 6 };
        private static readonly int[] NumeralRangeMaxByLevel = { 9, 9, 14, 14, 20, 20 };

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => NumberOrderingRoundGenerator.Create(0, new Random(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => NumberOrderingRoundGenerator.Create(7, new Random(1)));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = NumberOrderingRoundGenerator.Create(4, new Random(7));
            var b = NumberOrderingRoundGenerator.Create(4, new Random(7));
            Assert.That(a.Choices, Is.EqualTo(b.Choices));
            Assert.That(a.TargetOrder, Is.EqualTo(b.TargetOrder));
        }

        [Test]
        public void RoundsPerSessionMatchesTheSharedFiveRoundWindow()
        {
            Assert.That(NumberOrderingRoundGenerator.RoundsPerSession, Is.EqualTo(5));
        }

        [Test]
        public void TileCountMatchesTheLevelTable()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = NumberOrderingRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.Choices.Length, Is.EqualTo(TileCountByLevel[level - 1]), "level " + level + " seed " + seed);
                Assert.That(round.TargetOrder.Length, Is.EqualTo(TileCountByLevel[level - 1]), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void ChoicesStayWithinTheLevelsNumeralRangeAndAreDistinct()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = NumberOrderingRoundGenerator.Create(level, new Random(seed));
                var max = NumeralRangeMaxByLevel[level - 1];
                foreach (var c in round.Choices) Assert.That(c, Is.InRange(1, max), "level " + level + " seed " + seed);
                Assert.That(round.Choices.Distinct().Count(), Is.EqualTo(round.Choices.Length), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void TargetOrderIsAscendingBelowLevelFiveAndDescendingFromLevelFive()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = NumberOrderingRoundGenerator.Create(level, new Random(seed));
                var sortedAscending = round.TargetOrder.OrderBy(n => n).ToArray();
                if (level >= 5)
                {
                    Assert.IsTrue(round.Descending, "level " + level + " seed " + seed);
                    Assert.That(round.TargetOrder, Is.EqualTo(sortedAscending.Reverse()), "level " + level + " seed " + seed);
                }
                else
                {
                    Assert.IsFalse(round.Descending, "level " + level + " seed " + seed);
                    Assert.That(round.TargetOrder, Is.EqualTo(sortedAscending), "level " + level + " seed " + seed);
                }
            }
        }

        [Test]
        public void TargetOrderIsAPermutationOfChoices()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = NumberOrderingRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.TargetOrder.OrderBy(n => n), Is.EqualTo(round.Choices.OrderBy(n => n)), "level " + level + " seed " + seed);
            }
        }
    }
}
