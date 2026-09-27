using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class WordToImageTests
    {
        private static readonly int[] ChoiceCountByLevel = { 3, 3, 4, 4, 4, 4 };
        private static readonly string[] Catalogue =
        {
            "cat", "hat", "dog", "fog", "sun", "fun", "cup", "cap", "box", "fox", "bed", "red",
        };
        private static readonly int[] PoolSizeByLevel = { 4, 6, 8, 10, 12, 12 };
        private const int ConfusablesFromLevel = 5;

        private static readonly System.Collections.Generic.Dictionary<string, string> ConfusablePartner =
            new System.Collections.Generic.Dictionary<string, string>
            {
                { "cat", "hat" }, { "hat", "cat" }, { "dog", "fog" }, { "fog", "dog" },
                { "sun", "fun" }, { "fun", "sun" }, { "cup", "cap" }, { "cap", "cup" },
                { "box", "fox" }, { "fox", "box" }, { "bed", "red" }, { "red", "bed" },
            };

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => WordToImageRoundGenerator.Create(0, new Random(1), null));
            Assert.Throws<ArgumentOutOfRangeException>(() => WordToImageRoundGenerator.Create(7, new Random(1), null));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = WordToImageRoundGenerator.Create(4, new Random(7), null);
            var b = WordToImageRoundGenerator.Create(4, new Random(7), null);
            Assert.That(a.TargetWord, Is.EqualTo(b.TargetWord));
            Assert.That(a.Choices, Is.EqualTo(b.Choices));
        }

        [Test]
        public void RoundsPerSessionMatchesTheSharedFiveRoundWindow()
        {
            Assert.That(WordToImageRoundGenerator.RoundsPerSession, Is.EqualTo(5));
        }

        [Test]
        public void TargetStaysWithinTheLevelsPool()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = WordToImageRoundGenerator.Create(level, new Random(seed), null);
                var pool = Catalogue.Take(PoolSizeByLevel[level - 1]);
                Assert.IsTrue(pool.Contains(round.TargetWord), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void ChoiceCountMatchesTheLevelTable()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = WordToImageRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Choices.Length, Is.EqualTo(ChoiceCountByLevel[level - 1]), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void CorrectIndexAlwaysPointsAtTheTargetWordAndChoicesAreDistinct()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = WordToImageRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.CorrectIndex, Is.InRange(0, round.Choices.Length - 1), "level " + level + " seed " + seed);
                Assert.That(round.Choices[round.CorrectIndex], Is.EqualTo(round.TargetWord), "level " + level + " seed " + seed);
                Assert.That(round.Choices.Distinct().Count(), Is.EqualTo(round.Choices.Length), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void NeverRepeatsThePreviousTargetWhenTheRangeAllowsAChoice()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var first = WordToImageRoundGenerator.Create(level, new Random(seed), null);
                var second = WordToImageRoundGenerator.Create(level, new Random(seed + 1000), first.TargetWord);
                Assert.AreNotEqual(first.TargetWord, second.TargetWord, "level " + level + " seed " + seed);
            }
        }

        // From level 5, a visually similar printed-word partner (e.g. cat/hat) is guaranteed among the choices
        // whenever it's within the level's pool.
        [Test]
        public void ConfusablePartnerAppearsFromLevelFiveWhenInPool()
        {
            for (var level = ConfusablesFromLevel; level <= 6; level++)
            for (var seed = 0; seed < 300; seed++)
            {
                var round = WordToImageRoundGenerator.Create(level, new Random(seed), null);
                if (!ConfusablePartner.TryGetValue(round.TargetWord, out var partner)) continue;
                var pool = Catalogue.Take(PoolSizeByLevel[level - 1]);
                if (!pool.Contains(partner)) continue;
                Assert.IsTrue(round.Choices.Contains(partner), "level " + level + " seed " + seed + " target " + round.TargetWord);
            }
        }
    }
}
