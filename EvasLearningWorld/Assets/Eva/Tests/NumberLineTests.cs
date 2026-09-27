using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class NumberLineTests
    {
        private static readonly int[] LineMaxByLevel = { 5, 10, 10, 15, 20, 20 };
        private static readonly int[] HopsMaxByLevel = { 1, 2, 3, 3, 4, 5 };
        private static readonly int[] TileCountByLevel = { 3, 4, 5, 5, 6, 6 };
        private const int BackwardFromLevel = 4;

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => NumberLineRoundGenerator.Create(0, new Random(1), null));
            Assert.Throws<ArgumentOutOfRangeException>(() => NumberLineRoundGenerator.Create(7, new Random(1), null));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = NumberLineRoundGenerator.Create(4, new Random(7), null);
            var b = NumberLineRoundGenerator.Create(4, new Random(7), null);
            Assert.That(a.Start, Is.EqualTo(b.Start));
            Assert.That(a.Hops, Is.EqualTo(b.Hops));
            Assert.That(a.Forward, Is.EqualTo(b.Forward));
            Assert.That(a.Choices, Is.EqualTo(b.Choices));
        }

        [Test]
        public void RoundsPerSessionMatchesTheSharedFiveRoundWindow()
        {
            Assert.That(NumberLineRoundGenerator.RoundsPerSession, Is.EqualTo(5));
        }

        [Test]
        public void LandingAndStartAlwaysStayOnTheLine()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = NumberLineRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Start, Is.InRange(0, round.LineMax), "level " + level + " seed " + seed);
                Assert.That(round.Landing, Is.InRange(0, round.LineMax), "level " + level + " seed " + seed);
                Assert.That(round.LineMax, Is.EqualTo(LineMaxByLevel[level - 1]), "level " + level);
                Assert.That(round.Hops, Is.InRange(1, HopsMaxByLevel[level - 1]), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void BackwardHopsOnlyAppearFromLevelFour()
        {
            for (var level = DifficultyLadder.MinLevel; level < BackwardFromLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = NumberLineRoundGenerator.Create(level, new Random(seed), null);
                Assert.IsTrue(round.Forward, "level " + level + " seed " + seed + " should never hop backward");
            }
        }

        [Test]
        public void TileCountMatchesTheLevelTable()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = NumberLineRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Choices.Length, Is.EqualTo(TileCountByLevel[level - 1]), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void ChoicesAlwaysIncludeTheLandingExactlyOnceAreDistinctAndOnTheLine()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = NumberLineRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Choices.Count(c => c == round.Landing), Is.EqualTo(1), "level " + level + " seed " + seed);
                Assert.That(round.Choices.Distinct().Count(), Is.EqualTo(round.Choices.Length), "level " + level + " seed " + seed);
                foreach (var choice in round.Choices) Assert.That(choice, Is.InRange(0, round.LineMax), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void NeverRepeatsThePreviousLandingWhenTheRangeAllowsAChoice()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var first = NumberLineRoundGenerator.Create(level, new Random(seed), null);
                var second = NumberLineRoundGenerator.Create(level, new Random(seed + 1000), first.Landing);
                Assert.AreNotEqual(first.Landing, second.Landing, "level " + level + " seed " + seed);
            }
        }

        // From level 3, an off-by-one distractor (landing one space short or long) must appear whenever one is
        // in range - same guaranteed-confusable shape as Addition/Missing Number.
        [Test]
        public void OffByOneDistractorAppearsFromLevelThree()
        {
            for (var level = 3; level <= 6; level++)
            for (var seed = 0; seed < 300; seed++)
            {
                var round = NumberLineRoundGenerator.Create(level, new Random(seed), null);
                var hasNeighbor = round.Choices.Contains(round.Landing - 1) || round.Choices.Contains(round.Landing + 1);
                Assert.IsTrue(hasNeighbor, "level " + level + " seed " + seed + " landing " + round.Landing);
            }
        }
    }
}
