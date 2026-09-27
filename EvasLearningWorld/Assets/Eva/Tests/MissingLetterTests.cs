using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class MissingLetterTests
    {
        private static readonly string[] Catalogue =
        {
            "cat", "hat", "dog", "fog", "sun", "fun", "cup", "cap", "box", "fox", "bed", "red",
        };
        private static readonly int[] PoolSizeByLevel = { 4, 6, 8, 10, 12, 12 };
        private static readonly int[] TileCountByLevel = { 3, 4, 5, 5, 6, 6 };
        private const int ConfusablesFromLevel = 3;

        private static readonly System.Collections.Generic.Dictionary<char, char> ConfusablePartner =
            new System.Collections.Generic.Dictionary<char, char>
            {
                { 'b', 'd' }, { 'd', 'b' }, { 'p', 'q' }, { 'q', 'p' }, { 'm', 'w' }, { 'w', 'm' }, { 'n', 'u' }, { 'u', 'n' },
            };

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MissingLetterRoundGenerator.Create(0, new Random(1), null));
            Assert.Throws<ArgumentOutOfRangeException>(() => MissingLetterRoundGenerator.Create(7, new Random(1), null));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = MissingLetterRoundGenerator.Create(4, new Random(7), null);
            var b = MissingLetterRoundGenerator.Create(4, new Random(7), null);
            Assert.That(a.Word, Is.EqualTo(b.Word));
            Assert.That(a.Choices, Is.EqualTo(b.Choices));
        }

        [Test]
        public void RoundsPerSessionMatchesTheSharedFiveRoundWindow()
        {
            Assert.That(MissingLetterRoundGenerator.RoundsPerSession, Is.EqualTo(5));
        }

        [Test]
        public void MissingIsAlwaysTheMiddleLetter()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = MissingLetterRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Word.Length, Is.EqualTo(3), "level " + level + " seed " + seed);
                Assert.That(round.Missing, Is.EqualTo(round.Word[1]), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void WordStaysWithinTheLevelsPool()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = MissingLetterRoundGenerator.Create(level, new Random(seed), null);
                var pool = Catalogue.Take(PoolSizeByLevel[level - 1]);
                Assert.IsTrue(pool.Contains(round.Word), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void TileCountMatchesTheLevelTable()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = MissingLetterRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Choices.Length, Is.EqualTo(TileCountByLevel[level - 1]), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void ChoicesAlwaysIncludeTheMissingLetterExactlyOnceAndAreDistinct()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = MissingLetterRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Choices.Count(c => c == round.Missing), Is.EqualTo(1), "level " + level + " seed " + seed);
                Assert.That(round.Choices.Distinct().Count(), Is.EqualTo(round.Choices.Length), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void NeverRepeatsThePreviousWordWhenTheRangeAllowsAChoice()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var first = MissingLetterRoundGenerator.Create(level, new Random(seed), null);
                var second = MissingLetterRoundGenerator.Create(level, new Random(seed + 1000), first.Word);
                Assert.AreNotEqual(first.Word, second.Word, "level " + level + " seed " + seed);
            }
        }

        // From level 3, a shape-confusable partner (b/d, p/q, m/w, n/u) is guaranteed among the choices whenever
        // it's within the level's own word pool - the same guaranteed-confusable shape as Letter Hunt/Uppercase
        // to Lowercase.
        [Test]
        public void ConfusablePartnerAppearsFromLevelThreeWhenInPool()
        {
            for (var level = ConfusablesFromLevel; level <= 6; level++)
            for (var seed = 0; seed < 300; seed++)
            {
                var round = MissingLetterRoundGenerator.Create(level, new Random(seed), null);
                if (!ConfusablePartner.TryGetValue(round.Missing, out var partner)) continue;
                var poolLetters = Catalogue.Take(PoolSizeByLevel[level - 1]).SelectMany(w => w).Distinct();
                if (!poolLetters.Contains(partner)) continue;
                Assert.IsTrue(round.Choices.Contains(partner), "level " + level + " seed " + seed + " missing " + round.Missing);
            }
        }
    }
}
