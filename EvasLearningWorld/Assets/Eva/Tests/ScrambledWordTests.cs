using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class ScrambledWordTests
    {
        private static readonly string[] Catalogue =
        {
            "cat", "hat", "dog", "fog", "sun", "fun", "cup", "cap", "box", "fox", "bed", "red",
        };
        private static readonly int[] PoolSizeByLevel = { 4, 6, 8, 10, 12, 12 };

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ScrambledWordRoundGenerator.Create(0, new Random(1), null));
            Assert.Throws<ArgumentOutOfRangeException>(() => ScrambledWordRoundGenerator.Create(7, new Random(1), null));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = ScrambledWordRoundGenerator.Create(4, new Random(7), null);
            var b = ScrambledWordRoundGenerator.Create(4, new Random(7), null);
            Assert.That(a.Word, Is.EqualTo(b.Word));
            for (var i = 0; i < a.Letters.Length; i++)
            {
                Assert.That(a.Letters[i].Letter, Is.EqualTo(b.Letters[i].Letter));
                Assert.That(a.Letters[i].TrayPosition.X, Is.EqualTo(b.Letters[i].TrayPosition.X));
            }
        }

        [Test]
        public void RoundsPerSessionMatchesTheShorterDragSessionWindow()
        {
            Assert.That(ScrambledWordRoundGenerator.RoundsPerSession, Is.EqualTo(3));
        }

        [Test]
        public void WordStaysWithinTheLevelsPool()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = ScrambledWordRoundGenerator.Create(level, new Random(seed), null);
                var pool = Catalogue.Take(PoolSizeByLevel[level - 1]);
                Assert.IsTrue(pool.Contains(round.Word), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void LettersSpellTheWordInHomeOrder()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = ScrambledWordRoundGenerator.Create(level, new Random(seed), null);
                var spelled = new string(round.Letters.Select(l => l.Letter).ToArray());
                Assert.That(spelled, Is.EqualTo(round.Word), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void TrayPositionsAreAPermutationOfHomePositionsNeverTheIdentity()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = ScrambledWordRoundGenerator.Create(level, new Random(seed), null);
                var homeXs = round.Letters.Select(l => l.HomePosition.X).OrderBy(x => x).ToArray();
                var trayXs = round.Letters.Select(l => l.TrayPosition.X).OrderBy(x => x).ToArray();
                Assert.That(trayXs, Is.EqualTo(homeXs), "level " + level + " seed " + seed);

                var scrambled = round.Letters.Any(l => l.TrayPosition.X != l.HomePosition.X);
                Assert.IsTrue(scrambled, "level " + level + " seed " + seed + " tray must differ from home");
            }
        }

        [Test]
        public void NeverRepeatsThePreviousWordWhenTheRangeAllowsAChoice()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var first = ScrambledWordRoundGenerator.Create(level, new Random(seed), null);
                var second = ScrambledWordRoundGenerator.Create(level, new Random(seed + 1000), first.Word);
                Assert.AreNotEqual(first.Word, second.Word, "level " + level + " seed " + seed);
            }
        }
    }
}
