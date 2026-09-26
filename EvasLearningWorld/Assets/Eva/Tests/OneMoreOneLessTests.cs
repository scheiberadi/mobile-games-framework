using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class OneMoreOneLessTests
    {
        private static readonly int[] StartCountByLevel = { 2, 3, 5, 6, 7, 8 };
        private static readonly bool[] UseDragByLevel = { true, true, true, false, false, false };
        private static readonly int[] TileCountByLevel = { 3, 4, 5, 5, 6, 6 };

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => OneMoreOneLessRoundGenerator.Create(0, new Random(1), null));
            Assert.Throws<ArgumentOutOfRangeException>(() => OneMoreOneLessRoundGenerator.Create(7, new Random(1), null));
        }

        [Test]
        public void RoundsPerSessionMatchesTheSharedFiveRoundWindow()
        {
            Assert.That(OneMoreOneLessRoundGenerator.RoundsPerSession, Is.EqualTo(5));
        }

        [Test]
        public void StartCountAndUseDragMatchTheLevelTable()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            {
                var round = OneMoreOneLessRoundGenerator.Create(level, new Random(1), null);
                Assert.That(round.StartCount, Is.EqualTo(StartCountByLevel[level - 1]), "level " + level);
                Assert.That(round.UseDrag, Is.EqualTo(UseDragByLevel[level - 1]), "level " + level);
            }
        }

        [Test]
        public void TargetCountIsNeverNegative()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = OneMoreOneLessRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.TargetCount, Is.GreaterThanOrEqualTo(0), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void TargetCountIsStartCountPlusOrMinusOne()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = OneMoreOneLessRoundGenerator.Create(level, new Random(seed), null);
                var expected = round.IsMore ? round.StartCount + 1 : round.StartCount - 1;
                Assert.That(round.TargetCount, Is.EqualTo(expected), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void ChoicesAreNullWhenUsingDrag()
        {
            for (var level = 1; level <= 3; level++)
            for (var seed = 0; seed < 50; seed++)
            {
                var round = OneMoreOneLessRoundGenerator.Create(level, new Random(seed), null);
                Assert.IsNull(round.Choices, "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void TileCountMatchesTheLevelTableWhenNotUsingDrag()
        {
            for (var level = 4; level <= 6; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = OneMoreOneLessRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Choices.Length, Is.EqualTo(TileCountByLevel[level - 1]), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void ChoicesAlwaysIncludeTheTargetExactlyOnceAndAreDistinctWhenNotUsingDrag()
        {
            for (var level = 4; level <= 6; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = OneMoreOneLessRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Choices.Count(c => c == round.TargetCount), Is.EqualTo(1), "level " + level + " seed " + seed);
                Assert.That(round.Choices.Distinct().Count(), Is.EqualTo(round.Choices.Length), "level " + level + " seed " + seed);
            }
        }

        // The classic slip for this game - answering with the original count instead of one more/less - must be
        // guaranteed among the choices whenever it differs from the target (it always does, since the target is
        // always StartCount +/- 1).
        [Test]
        public void OriginalStartCountAppearsAsADistractorWhenNotUsingDrag()
        {
            for (var level = 4; level <= 6; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = OneMoreOneLessRoundGenerator.Create(level, new Random(seed), null);
                Assert.Contains(round.StartCount, round.Choices, "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void NeverRepeatsThePreviousIsMoreFlag()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var first = OneMoreOneLessRoundGenerator.Create(level, new Random(seed), null);
                var second = OneMoreOneLessRoundGenerator.Create(level, new Random(seed + 1000), first.IsMore);
                Assert.AreNotEqual(first.IsMore, second.IsMore, "level " + level + " seed " + seed);
            }
        }
    }
}
