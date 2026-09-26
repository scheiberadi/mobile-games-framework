using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class LetterHuntTests
    {
        private static readonly int[] PoolSizeByLevel = { 5, 10, 15, 20, 26, 26 };
        private static readonly int[] TileCountByLevel = { 3, 4, 5, 5, 6, 6 };

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => LetterHuntRoundGenerator.Create(0, new Random(1), null));
            Assert.Throws<ArgumentOutOfRangeException>(() => LetterHuntRoundGenerator.Create(7, new Random(1), null));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = LetterHuntRoundGenerator.Create(4, new Random(7), null);
            var b = LetterHuntRoundGenerator.Create(4, new Random(7), null);
            Assert.That(a.Target, Is.EqualTo(b.Target));
            Assert.That(a.Choices, Is.EqualTo(b.Choices));
        }

        [Test]
        public void RoundsPerSessionMatchesTheSharedFiveRoundWindow()
        {
            Assert.That(LetterHuntRoundGenerator.RoundsPerSession, Is.EqualTo(5));
        }

        [Test]
        public void TileCountMatchesTheLevelTable()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = LetterHuntRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Choices.Length, Is.EqualTo(TileCountByLevel[level - 1]), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void TargetAndEveryChoiceStayWithinTheLevelsAlphabetPool()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = LetterHuntRoundGenerator.Create(level, new Random(seed), null);
                var maxLetter = (char)('a' + PoolSizeByLevel[level - 1] - 1);
                Assert.That(round.Target, Is.InRange('a', maxLetter), "level " + level + " seed " + seed);
                foreach (var choice in round.Choices) Assert.That(choice, Is.InRange('a', maxLetter), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void ChoicesAlwaysIncludeTheTargetExactlyOnceAndAreDistinct()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = LetterHuntRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Choices.Count(c => c == round.Target), Is.EqualTo(1), "level " + level + " seed " + seed);
                Assert.That(round.Choices.Distinct().Count(), Is.EqualTo(round.Choices.Length), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void NeverRepeatsThePreviousTargetWhenThePoolAllowsAChoice()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var first = LetterHuntRoundGenerator.Create(level, new Random(seed), null);
                var second = LetterHuntRoundGenerator.Create(level, new Random(seed + 1000), first.Target);
                Assert.AreNotEqual(first.Target, second.Target, "level " + level + " seed " + seed);
            }
        }

        // From level 5, a confusable partner (b/d, p/q, m/w, n/u) must appear alongside the target whenever one
        // exists within the level's own alphabet pool - same guarantee as Number Hunt's numeral confusables.
        [Test]
        public void ConfusablePartnerAppearsFromLevelFiveWhenInRange()
        {
            var confusable = new System.Collections.Generic.Dictionary<char, char>
            {
                { 'b', 'd' }, { 'd', 'b' }, { 'p', 'q' }, { 'q', 'p' }, { 'm', 'w' }, { 'w', 'm' }, { 'n', 'u' }, { 'u', 'n' },
            };
            for (var level = 5; level <= 6; level++)
            for (var seed = 0; seed < 300; seed++)
            {
                var round = LetterHuntRoundGenerator.Create(level, new Random(seed), null);
                if (confusable.TryGetValue(round.Target, out var partner))
                    Assert.Contains(partner, round.Choices, "level " + level + " seed " + seed + " target " + round.Target);
            }
        }
    }
}
