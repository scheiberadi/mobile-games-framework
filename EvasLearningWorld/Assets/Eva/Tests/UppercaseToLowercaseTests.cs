using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class UppercaseToLowercaseTests
    {
        private static readonly int[] PoolSizeByLevel = { 5, 10, 15, 20, 26, 26 };
        private static readonly int[] ChoiceCountByLevel = { 3, 3, 4, 4, 4, 4 };
        private const int ConfusablesFromLevel = 5;

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => UppercaseToLowercaseRoundGenerator.Create(0, new Random(1), null));
            Assert.Throws<ArgumentOutOfRangeException>(() => UppercaseToLowercaseRoundGenerator.Create(7, new Random(1), null));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = UppercaseToLowercaseRoundGenerator.Create(4, new Random(7), null);
            var b = UppercaseToLowercaseRoundGenerator.Create(4, new Random(7), null);
            Assert.That(a.Target, Is.EqualTo(b.Target));
            Assert.That(a.Choices, Is.EqualTo(b.Choices));
        }

        [Test]
        public void RoundsPerSessionMatchesTheSharedFiveRoundWindow()
        {
            Assert.That(UppercaseToLowercaseRoundGenerator.RoundsPerSession, Is.EqualTo(5));
        }

        [Test]
        public void TargetStaysWithinTheLevelsAlphabetPool()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = UppercaseToLowercaseRoundGenerator.Create(level, new Random(seed), null);
                Assert.That((int)round.Target, Is.InRange((int)'a', 'a' + PoolSizeByLevel[level - 1] - 1), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void ChoiceCountMatchesTheLevelTable()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = UppercaseToLowercaseRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Choices.Length, Is.EqualTo(ChoiceCountByLevel[level - 1]), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void CorrectIndexAlwaysPointsAtTheTargetAndChoicesAreDistinct()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = UppercaseToLowercaseRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.CorrectIndex, Is.InRange(0, round.Choices.Length - 1), "level " + level + " seed " + seed);
                Assert.That(round.Choices[round.CorrectIndex], Is.EqualTo(round.Target), "level " + level + " seed " + seed);
                Assert.That(round.Choices.Distinct().Count(), Is.EqualTo(round.Choices.Length), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void NeverRepeatsThePreviousTargetWhenTheRangeAllowsAChoice()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var first = UppercaseToLowercaseRoundGenerator.Create(level, new Random(seed), null);
                var second = UppercaseToLowercaseRoundGenerator.Create(level, new Random(seed + 1000), first.Target);
                Assert.AreNotEqual(first.Target, second.Target, "level " + level + " seed " + seed);
            }
        }

        // From level 5, a shape-confusable partner (b/d, p/q, m/w, n/u - same table as Letter Hunt) is
        // guaranteed among the choices whenever it's within the level's alphabet pool.
        [Test]
        public void ConfusablePartnerAppearsFromLevelFiveWhenInPool()
        {
            var confusable = new[] { 'b', 'd', 'p', 'q', 'm', 'w', 'n', 'u' };
            for (var level = ConfusablesFromLevel; level <= 6; level++)
            for (var seed = 0; seed < 300; seed++)
            {
                var round = UppercaseToLowercaseRoundGenerator.Create(level, new Random(seed), null);
                if (Array.IndexOf(confusable, round.Target) < 0) continue;
                var partner = round.Target switch
                {
                    'b' => 'd', 'd' => 'b', 'p' => 'q', 'q' => 'p', 'm' => 'w', 'w' => 'm', 'n' => 'u', 'u' => 'n',
                    _ => round.Target,
                };
                Assert.IsTrue(round.Choices.Contains(partner), "level " + level + " seed " + seed + " target " + round.Target);
            }
        }
    }
}
