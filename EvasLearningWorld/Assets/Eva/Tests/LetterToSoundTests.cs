using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class LetterToSoundTests
    {
        private static readonly int[] ChoiceCountByLevel = { 3, 3, 4, 4, 4, 4 };
        private static readonly (char Letter, string Key)[] Catalogue =
        {
            ('a', "apple"), ('b', "ball"), ('c', "cat"), ('d', "dog"),
            ('f', "fish"), ('g', "goat"), ('h', "hat"), ('j', "jam"),
            ('k', "kite"), ('l', "lion"), ('m', "moon"), ('n', "nest"),
        };
        private static readonly int[] PoolSizeByLevel = { 4, 6, 8, 10, 12, 12 };
        private const int ConfusablesFromLevel = 5;

        private static char LetterOf(string key) => Catalogue.First(i => i.Key == key).Letter;

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => LetterToSoundRoundGenerator.Create(0, new Random(1), null));
            Assert.Throws<ArgumentOutOfRangeException>(() => LetterToSoundRoundGenerator.Create(7, new Random(1), null));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = LetterToSoundRoundGenerator.Create(4, new Random(7), null);
            var b = LetterToSoundRoundGenerator.Create(4, new Random(7), null);
            Assert.That(a.TargetLetter, Is.EqualTo(b.TargetLetter));
            Assert.That(a.Choices, Is.EqualTo(b.Choices));
        }

        [Test]
        public void RoundsPerSessionMatchesTheSharedFiveRoundWindow()
        {
            Assert.That(LetterToSoundRoundGenerator.RoundsPerSession, Is.EqualTo(5));
        }

        [Test]
        public void TargetStaysWithinTheLevelsPool()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = LetterToSoundRoundGenerator.Create(level, new Random(seed), null);
                var poolLetters = Catalogue.Take(PoolSizeByLevel[level - 1]).Select(i => i.Letter);
                Assert.IsTrue(poolLetters.Contains(round.TargetLetter), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void ChoiceCountMatchesTheLevelTable()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = LetterToSoundRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Choices.Length, Is.EqualTo(ChoiceCountByLevel[level - 1]), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void CorrectIndexAlwaysPointsAtAnItemStartingWithTheTargetLetterAndChoicesAreDistinct()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = LetterToSoundRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.CorrectIndex, Is.InRange(0, round.Choices.Length - 1), "level " + level + " seed " + seed);
                Assert.That(LetterOf(round.Choices[round.CorrectIndex]), Is.EqualTo(round.TargetLetter), "level " + level + " seed " + seed);
                Assert.That(round.Choices.Distinct().Count(), Is.EqualTo(round.Choices.Length), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void NeverRepeatsThePreviousTargetWhenTheRangeAllowsAChoice()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var first = LetterToSoundRoundGenerator.Create(level, new Random(seed), null);
                var second = LetterToSoundRoundGenerator.Create(level, new Random(seed + 1000), first.TargetLetter);
                Assert.AreNotEqual(first.TargetLetter, second.TargetLetter, "level " + level + " seed " + seed);
            }
        }

        // From level 5, a sound-alike partner (g/k or m/n) is guaranteed among the choices whenever it's
        // within the level's pool.
        [Test]
        public void ConfusablePartnerAppearsFromLevelFiveWhenInPool()
        {
            for (var level = ConfusablesFromLevel; level <= 6; level++)
            for (var seed = 0; seed < 300; seed++)
            {
                var round = LetterToSoundRoundGenerator.Create(level, new Random(seed), null);
                var partner = round.TargetLetter switch { 'g' => 'k', 'k' => 'g', 'm' => 'n', 'n' => 'm', _ => (char)0 };
                if (partner == (char)0) continue;
                var poolLetters = Catalogue.Take(PoolSizeByLevel[level - 1]).Select(i => i.Letter);
                if (!poolLetters.Contains(partner)) continue;
                Assert.IsTrue(round.Choices.Any(key => LetterOf(key) == partner), "level " + level + " seed " + seed + " target " + round.TargetLetter);
            }
        }
    }
}
