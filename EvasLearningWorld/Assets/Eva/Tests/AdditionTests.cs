using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class AdditionTests
    {
        private static readonly int[] OperandMaxByLevel = { 3, 5, 7, 9, 9, 9 };
        private static readonly bool[] ShowObjectsByLevel = { true, true, true, false, false, false };
        private static readonly int[] TileCountByLevel = { 3, 4, 5, 5, 6, 6 };

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => AdditionRoundGenerator.Create(0, new Random(1), null));
            Assert.Throws<ArgumentOutOfRangeException>(() => AdditionRoundGenerator.Create(7, new Random(1), null));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = AdditionRoundGenerator.Create(4, new Random(7), null);
            var b = AdditionRoundGenerator.Create(4, new Random(7), null);
            Assert.That(a.A, Is.EqualTo(b.A));
            Assert.That(a.B, Is.EqualTo(b.B));
            Assert.That(a.Choices, Is.EqualTo(b.Choices));
        }

        [Test]
        public void RoundsPerSessionMatchesTheSharedFiveRoundWindow()
        {
            Assert.That(AdditionRoundGenerator.RoundsPerSession, Is.EqualTo(5));
        }

        [Test]
        public void SumIsAlwaysAPlusB()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = AdditionRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Sum, Is.EqualTo(round.A + round.B), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void OperandsStayWithinTheLevelsRange()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = AdditionRoundGenerator.Create(level, new Random(seed), null);
                var max = OperandMaxByLevel[level - 1];
                Assert.That(round.A, Is.InRange(1, max), "level " + level + " seed " + seed);
                Assert.That(round.B, Is.InRange(1, max), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void ShowObjectsMatchesTheLevelTable()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            {
                var round = AdditionRoundGenerator.Create(level, new Random(1), null);
                Assert.That(round.ShowObjects, Is.EqualTo(ShowObjectsByLevel[level - 1]), "level " + level);
            }
        }

        [Test]
        public void TileCountMatchesTheLevelTable()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = AdditionRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Choices.Length, Is.EqualTo(TileCountByLevel[level - 1]), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void ChoicesAlwaysIncludeTheSumExactlyOnceAndAreDistinct()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = AdditionRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Choices.Count(c => c == round.Sum), Is.EqualTo(1), "level " + level + " seed " + seed);
                Assert.That(round.Choices.Distinct().Count(), Is.EqualTo(round.Choices.Length), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void ChoicesAreNeverNegative()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = AdditionRoundGenerator.Create(level, new Random(seed), null);
                foreach (var choice in round.Choices) Assert.That(choice, Is.GreaterThanOrEqualTo(0), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void NeverRepeatsThePreviousSumWhenTheRangeAllowsAChoice()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var first = AdditionRoundGenerator.Create(level, new Random(seed), null);
                var second = AdditionRoundGenerator.Create(level, new Random(seed + 1000), first.Sum);
                Assert.AreNotEqual(first.Sum, second.Sum, "level " + level + " seed " + seed);
            }
        }

        // From level 3, an off-by-one distractor (the classic addition slip) must appear alongside the sum
        // whenever one is in range - same guaranteed-confusable shape as Number Hunt/Letter Hunt.
        [Test]
        public void OffByOneDistractorAppearsFromLevelThree()
        {
            for (var level = 3; level <= 6; level++)
            for (var seed = 0; seed < 300; seed++)
            {
                var round = AdditionRoundGenerator.Create(level, new Random(seed), null);
                var hasNeighbor = round.Choices.Contains(round.Sum - 1) || round.Choices.Contains(round.Sum + 1);
                Assert.IsTrue(hasNeighbor, "level " + level + " seed " + seed + " sum " + round.Sum);
            }
        }
    }
}
