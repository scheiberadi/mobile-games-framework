using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class RhymingTests
    {
        private static readonly int[] ChoiceCountByLevel = { 3, 3, 4, 4, 4, 4 };
        private const int NearFamilyFromLevel = 3;

        // Mirrors RhymingRoundGenerator's own catalogue (Rules/Rhyming.cs) only to check family membership,
        // not to duplicate the generator.
        private static readonly (string Key, string Family)[] Catalogue =
        {
            ("cat", "at"), ("hat", "at"), ("bat", "at"), ("mat", "at"),
            ("dog", "og"), ("frog", "og"), ("log", "og"), ("jog", "og"),
            ("pan", "an"), ("fan", "an"), ("van", "an"), ("man", "an"),
            ("bug", "ug"), ("rug", "ug"), ("mug", "ug"), ("jug", "ug"),
        };

        private static string FamilyOf(string key) => Catalogue.First(i => i.Key == key).Family;

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RhymingRoundGenerator.Create(0, new Random(1), null));
            Assert.Throws<ArgumentOutOfRangeException>(() => RhymingRoundGenerator.Create(7, new Random(1), null));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = RhymingRoundGenerator.Create(4, new Random(7), null);
            var b = RhymingRoundGenerator.Create(4, new Random(7), null);
            Assert.That(a.TargetKey, Is.EqualTo(b.TargetKey));
            Assert.That(a.Choices, Is.EqualTo(b.Choices));
        }

        [Test]
        public void RoundsPerSessionMatchesTheSharedFiveRoundWindow()
        {
            Assert.That(RhymingRoundGenerator.RoundsPerSession, Is.EqualTo(5));
        }

        [Test]
        public void ChoiceCountMatchesTheLevelTable()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = RhymingRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Choices.Length, Is.EqualTo(ChoiceCountByLevel[level - 1]), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void CorrectChoiceAlwaysRhymesWithTheTargetAndIsNeverTheTargetItself()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 300; seed++)
            {
                var round = RhymingRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.CorrectIndex, Is.InRange(0, round.Choices.Length - 1), "level " + level + " seed " + seed);
                var correctKey = round.Choices[round.CorrectIndex];
                Assert.That(FamilyOf(correctKey), Is.EqualTo(FamilyOf(round.TargetKey)), "level " + level + " seed " + seed);
                Assert.That(correctKey, Is.Not.EqualTo(round.TargetKey), "level " + level + " seed " + seed);
                Assert.That(round.Choices.Distinct().Count(), Is.EqualTo(round.Choices.Length), "level " + level + " seed " + seed);
            }
        }

        // Every distractor must come from a different family than the target - a same-family word would also
        // rhyme, making the round ambiguous.
        [Test]
        public void EveryDistractorComesFromADifferentFamilyThanTheTarget()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 300; seed++)
            {
                var round = RhymingRoundGenerator.Create(level, new Random(seed), null);
                var targetFamily = FamilyOf(round.TargetKey);
                for (var i = 0; i < round.Choices.Length; i++)
                {
                    if (i == round.CorrectIndex) continue;
                    Assert.That(FamilyOf(round.Choices[i]), Is.Not.EqualTo(targetFamily), "level " + level + " seed " + seed);
                }
            }
        }

        [Test]
        public void NeverRepeatsThePreviousTargetWhenTheRangeAllowsAChoice()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var first = RhymingRoundGenerator.Create(level, new Random(seed), null);
                var second = RhymingRoundGenerator.Create(level, new Random(seed + 1000), first.TargetKey);
                Assert.AreNotEqual(first.TargetKey, second.TargetKey, "level " + level + " seed " + seed);
            }
        }

        // From level 3, a near-family distractor (a phonetically closer false friend than a random other
        // family) is guaranteed among the choices.
        [Test]
        public void NearFamilyDistractorAppearsFromLevelThree()
        {
            var nearFamily = new System.Collections.Generic.Dictionary<string, string>
            {
                { "at", "an" }, { "an", "at" }, { "og", "ug" }, { "ug", "og" },
            };
            for (var level = NearFamilyFromLevel; level <= 6; level++)
            for (var seed = 0; seed < 300; seed++)
            {
                var round = RhymingRoundGenerator.Create(level, new Random(seed), null);
                var expectedNear = nearFamily[FamilyOf(round.TargetKey)];
                var hasNear = round.Choices.Any(key => FamilyOf(key) == expectedNear);
                Assert.IsTrue(hasNear, "level " + level + " seed " + seed + " target " + round.TargetKey);
            }
        }
    }
}
