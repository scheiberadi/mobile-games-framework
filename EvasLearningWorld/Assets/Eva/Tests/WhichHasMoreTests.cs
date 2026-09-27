using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class WhichHasMoreTests
    {
        private static readonly int[] MaxByLevel = { 3, 5, 7, 9, 9, 9 };
        private static readonly bool[] AskDifferenceByLevel = { false, false, false, true, true, true };
        private static readonly int[] TileCountByLevel = { 3, 4, 5, 5, 6, 6 };

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => WhichHasMoreRoundGenerator.Create(0, new Random(1), null));
            Assert.Throws<ArgumentOutOfRangeException>(() => WhichHasMoreRoundGenerator.Create(7, new Random(1), null));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = WhichHasMoreRoundGenerator.Create(4, new Random(7), null);
            var b = WhichHasMoreRoundGenerator.Create(4, new Random(7), null);
            Assert.That(a.A, Is.EqualTo(b.A));
            Assert.That(a.B, Is.EqualTo(b.B));
        }

        [Test]
        public void RoundsPerSessionMatchesTheSharedFiveRoundWindow()
        {
            Assert.That(WhichHasMoreRoundGenerator.RoundsPerSession, Is.EqualTo(5));
        }

        [Test]
        public void ABAndDifferenceStayWithinTheLevelsRangeAndAreNeverEqual()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = WhichHasMoreRoundGenerator.Create(level, new Random(seed), null);
                var max = MaxByLevel[level - 1];
                Assert.That(round.A, Is.InRange(1, max), "level " + level + " seed " + seed);
                Assert.That(round.B, Is.InRange(1, max), "level " + level + " seed " + seed);
                Assert.AreNotEqual(round.A, round.B, "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void AskDifferenceMatchesTheLevelTable()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            {
                var round = WhichHasMoreRoundGenerator.Create(level, new Random(1), null);
                Assert.That(round.AskDifference, Is.EqualTo(AskDifferenceByLevel[level - 1]), "level " + level);
            }
        }

        [Test]
        public void ChoicesAreNullWhenNotAskingDifference()
        {
            for (var level = 1; level <= 3; level++)
            for (var seed = 0; seed < 50; seed++)
            {
                var round = WhichHasMoreRoundGenerator.Create(level, new Random(seed), null);
                Assert.IsNull(round.Choices, "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void TileCountMatchesTheLevelTableWhenAskingDifference()
        {
            for (var level = 4; level <= 6; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = WhichHasMoreRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Choices.Length, Is.EqualTo(TileCountByLevel[level - 1]), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void ChoicesAlwaysIncludeTheDifferenceExactlyOnceAndAreDistinctWhenAskingDifference()
        {
            for (var level = 4; level <= 6; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = WhichHasMoreRoundGenerator.Create(level, new Random(seed), null);
                var difference = Math.Abs(round.A - round.B);
                Assert.That(round.Choices.Count(c => c == difference), Is.EqualTo(1), "level " + level + " seed " + seed);
                Assert.That(round.Choices.Distinct().Count(), Is.EqualTo(round.Choices.Length), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void ChoicesAreAlwaysAtLeastOneWhenAskingDifference()
        {
            for (var level = 4; level <= 6; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = WhichHasMoreRoundGenerator.Create(level, new Random(seed), null);
                foreach (var choice in round.Choices) Assert.That(choice, Is.GreaterThanOrEqualTo(1), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void NeverRepeatsThePreviousDifferenceWhenAskingDifferenceAndTheRangeAllowsAChoice()
        {
            for (var level = 4; level <= 6; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var first = WhichHasMoreRoundGenerator.Create(level, new Random(seed), null);
                var firstDifference = Math.Abs(first.A - first.B);
                var second = WhichHasMoreRoundGenerator.Create(level, new Random(seed + 1000), firstDifference);
                var secondDifference = Math.Abs(second.A - second.B);
                Assert.AreNotEqual(firstDifference, secondDifference, "level " + level + " seed " + seed);
            }
        }

        // From level 5, an off-by-one distractor must appear alongside the difference whenever one is in range -
        // same guaranteed-confusable shape as Addition/Subtraction.
        [Test]
        public void OffByOneDistractorAppearsFromLevelFive()
        {
            for (var level = 5; level <= 6; level++)
            for (var seed = 0; seed < 300; seed++)
            {
                var round = WhichHasMoreRoundGenerator.Create(level, new Random(seed), null);
                var difference = Math.Abs(round.A - round.B);
                var hasNeighbor = round.Choices.Contains(difference - 1) || round.Choices.Contains(difference + 1);
                Assert.IsTrue(hasNeighbor, "level " + level + " seed " + seed + " difference " + difference);
            }
        }
    }
}
