using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class MissingNumberTests
    {
        private static readonly int[] OperandMaxByLevel = { 3, 5, 7, 9, 9, 9 };
        private static readonly bool[] ShowObjectsByLevel = { true, true, true, false, false, false };
        private static readonly int[] TileCountByLevel = { 3, 4, 5, 5, 6, 6 };

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MissingNumberRoundGenerator.Create(0, new Random(1), null));
            Assert.Throws<ArgumentOutOfRangeException>(() => MissingNumberRoundGenerator.Create(7, new Random(1), null));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = MissingNumberRoundGenerator.Create(4, new Random(7), null);
            var b = MissingNumberRoundGenerator.Create(4, new Random(7), null);
            Assert.That(a.A, Is.EqualTo(b.A));
            Assert.That(a.Sum, Is.EqualTo(b.Sum));
            Assert.That(a.Choices, Is.EqualTo(b.Choices));
        }

        [Test]
        public void RoundsPerSessionMatchesTheSharedFiveRoundWindow()
        {
            Assert.That(MissingNumberRoundGenerator.RoundsPerSession, Is.EqualTo(5));
        }

        [Test]
        public void MissingIsAlwaysSumMinusA()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = MissingNumberRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Missing, Is.EqualTo(round.Sum - round.A), "level " + level + " seed " + seed);
                Assert.That(round.Missing, Is.GreaterThanOrEqualTo(1), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void OperandsStayWithinTheLevelsRange()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = MissingNumberRoundGenerator.Create(level, new Random(seed), null);
                var max = OperandMaxByLevel[level - 1];
                Assert.That(round.A, Is.InRange(1, max), "level " + level + " seed " + seed);
                Assert.That(round.Missing, Is.InRange(1, max), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void ShowObjectsMatchesTheLevelTable()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            {
                var round = MissingNumberRoundGenerator.Create(level, new Random(1), null);
                Assert.That(round.ShowObjects, Is.EqualTo(ShowObjectsByLevel[level - 1]), "level " + level);
            }
        }

        [Test]
        public void TileCountMatchesTheLevelTable()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = MissingNumberRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Choices.Length, Is.EqualTo(TileCountByLevel[level - 1]), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void ChoicesAlwaysIncludeTheMissingValueExactlyOnceAndAreDistinct()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = MissingNumberRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Choices.Count(c => c == round.Missing), Is.EqualTo(1), "level " + level + " seed " + seed);
                Assert.That(round.Choices.Distinct().Count(), Is.EqualTo(round.Choices.Length), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void ChoicesAreNeverNegative()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = MissingNumberRoundGenerator.Create(level, new Random(seed), null);
                foreach (var choice in round.Choices) Assert.That(choice, Is.GreaterThanOrEqualTo(0), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void NeverRepeatsThePreviousSumWhenTheRangeAllowsAChoice()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var first = MissingNumberRoundGenerator.Create(level, new Random(seed), null);
                var second = MissingNumberRoundGenerator.Create(level, new Random(seed + 1000), first.Sum);
                Assert.AreNotEqual(first.Sum, second.Sum, "level " + level + " seed " + seed);
            }
        }

        // The classic missing-addend slip - answering with the total instead of the hidden piece - must appear
        // among the choices whenever it differs from the answer (it always does here, since Missing >= 1).
        [Test]
        public void TheSumItselfIsGuaranteedAsADistractor()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = MissingNumberRoundGenerator.Create(level, new Random(seed), null);
                Assert.IsTrue(round.Choices.Contains(round.Sum), "level " + level + " seed " + seed);
            }
        }

        // From level 3, an off-by-one distractor of the answer must appear whenever one is in range - same
        // guaranteed-confusable shape as Addition/Number Hunt/Letter Hunt.
        [Test]
        public void OffByOneDistractorAppearsFromLevelThree()
        {
            for (var level = 3; level <= 6; level++)
            for (var seed = 0; seed < 300; seed++)
            {
                var round = MissingNumberRoundGenerator.Create(level, new Random(seed), null);
                var hasNeighbor = round.Choices.Contains(round.Missing - 1) || round.Choices.Contains(round.Missing + 1);
                Assert.IsTrue(hasNeighbor, "level " + level + " seed " + seed + " missing " + round.Missing);
            }
        }
    }
}
