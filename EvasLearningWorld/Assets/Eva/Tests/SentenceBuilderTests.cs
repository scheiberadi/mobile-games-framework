using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class SentenceBuilderTests
    {
        private static readonly (string Key, string Word1, string Word2)[] Catalogue =
        {
            ("cat_cup", "cat", "cup"), ("dog_box", "dog", "box"), ("sun_hat", "sun", "hat"),
            ("cap_bed", "cap", "bed"), ("fox_bed", "fox", "bed"), ("cup_box", "cup", "box"),
        };
        private static readonly int[] PoolSizeByLevel = { 2, 3, 4, 5, 6, 6 };
        private static readonly int[] WordSlotsByLevel = { 0, 0, 1, 1, 2, 2 };

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SentenceBuilderRoundGenerator.Create(0, new Random(1), null));
            Assert.Throws<ArgumentOutOfRangeException>(() => SentenceBuilderRoundGenerator.Create(7, new Random(1), null));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = SentenceBuilderRoundGenerator.Create(4, new Random(7), null);
            var b = SentenceBuilderRoundGenerator.Create(4, new Random(7), null);
            Assert.That(a.Key, Is.EqualTo(b.Key));
            for (var i = 0; i < a.Items.Length; i++)
            {
                Assert.That(a.Items[i].Word, Is.EqualTo(b.Items[i].Word));
                Assert.That(a.Items[i].AsWord, Is.EqualTo(b.Items[i].AsWord));
                Assert.That(a.Items[i].TrayPosition.X, Is.EqualTo(b.Items[i].TrayPosition.X));
            }
        }

        [Test]
        public void RoundsPerSessionMatchesTheShorterDragSessionWindow()
        {
            Assert.That(SentenceBuilderRoundGenerator.RoundsPerSession, Is.EqualTo(3));
        }

        [Test]
        public void SentenceStaysWithinTheLevelsPool()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = SentenceBuilderRoundGenerator.Create(level, new Random(seed), null);
                var pool = Catalogue.Take(PoolSizeByLevel[level - 1]).Select(s => s.Key);
                Assert.IsTrue(pool.Contains(round.Key), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void ItemsMatchTheSentencesWordsInOrder()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = SentenceBuilderRoundGenerator.Create(level, new Random(seed), null);
                var sentence = Catalogue.First(s => s.Key == round.Key);
                Assert.That(round.Items.Length, Is.EqualTo(2), "level " + level + " seed " + seed);
                Assert.That(round.Items[0].Word, Is.EqualTo(sentence.Word1), "level " + level + " seed " + seed);
                Assert.That(round.Items[1].Word, Is.EqualTo(sentence.Word2), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void WordSlotCountMatchesTheLevelTable()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = SentenceBuilderRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Items.Count(i => i.AsWord), Is.EqualTo(WordSlotsByLevel[level - 1]), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void TrayPositionsAreAPermutationOfHomePositionsNeverTheIdentity()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = SentenceBuilderRoundGenerator.Create(level, new Random(seed), null);
                var homeXs = round.Items.Select(i => i.HomePosition.X).OrderBy(x => x).ToArray();
                var trayXs = round.Items.Select(i => i.TrayPosition.X).OrderBy(x => x).ToArray();
                Assert.That(trayXs, Is.EqualTo(homeXs), "level " + level + " seed " + seed);

                var scrambled = round.Items.Any(i => i.TrayPosition.X != i.HomePosition.X);
                Assert.IsTrue(scrambled, "level " + level + " seed " + seed + " tray must differ from home");
            }
        }

        [Test]
        public void NeverRepeatsThePreviousSentenceWhenTheRangeAllowsAChoice()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var first = SentenceBuilderRoundGenerator.Create(level, new Random(seed), null);
                var second = SentenceBuilderRoundGenerator.Create(level, new Random(seed + 1000), first.Key);
                Assert.AreNotEqual(first.Key, second.Key, "level " + level + " seed " + seed);
            }
        }
    }
}
