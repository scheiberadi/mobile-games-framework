using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class ItemToShadowTests
    {
        private static readonly int[] ChoiceCountByLevel = { 3, 3, 4, 4, 4, 4 };

        // Same group membership as ItemToShadowRoundGenerator's own catalogue (Rules/ItemToShadow.cs); mirrored
        // here only to check cross-group vs same-group distractor placement, not to duplicate the generator.
        private static readonly (string Key, string Group)[] Catalogue =
        {
            ("apple", "Round"), ("orange", "Round"), ("ball", "Round"), ("balloon", "Round"),
            ("banana", "Narrow"), ("carrot", "Narrow"), ("pencil", "Narrow"), ("candle", "Narrow"),
            ("cat", "Animal"), ("dog", "Animal"), ("fox", "Animal"), ("rabbit", "Animal"),
            ("car", "Vehicle"), ("truck", "Vehicle"), ("bus", "Vehicle"), ("bike", "Vehicle"),
        };

        private static string GroupOf(string key) => Catalogue.First(i => i.Key == key).Group;

        [Test]
        public void EveryRoundHasTheTargetAmongTheLevelsChoiceCount()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 300; seed++)
            {
                var round = ItemToShadowRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.Choices.Length, Is.EqualTo(ChoiceCountByLevel[level - 1]));
                Assert.That(round.CorrectIndex, Is.InRange(0, round.Choices.Length - 1));
                Assert.That(round.Choices[round.CorrectIndex], Is.EqualTo(round.TargetKey));
                Assert.That(round.Choices.Distinct().Count(), Is.EqualTo(round.Choices.Length), "level " + level + " repeats a choice");
            }
        }

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ItemToShadowRoundGenerator.Create(0, new Random(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => ItemToShadowRoundGenerator.Create(7, new Random(1)));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = ItemToShadowRoundGenerator.Create(4, new Random(7));
            var b = ItemToShadowRoundGenerator.Create(4, new Random(7));
            Assert.That(a.Choices, Is.EqualTo(b.Choices));
            Assert.That(a.TargetKey, Is.EqualTo(b.TargetKey));
        }

        [Test]
        public void RoundsPerSessionMatchesTheSharedFiveRoundWindow()
        {
            Assert.That(ItemToShadowRoundGenerator.RoundsPerSession, Is.EqualTo(5));
        }

        // Levels 1-2: every distractor comes from a group other than the target's own (obvious, dissimilar shapes).
        [Test]
        public void LowLevelsDrawDistractorsFromADifferentGroup()
        {
            for (var seed = 0; seed < 200; seed++)
            for (var level = 1; level <= 2; level++)
            {
                var round = ItemToShadowRoundGenerator.Create(level, new Random(seed));
                var targetGroup = GroupOf(round.TargetKey);
                foreach (var key in round.Choices)
                    if (key != round.TargetKey) Assert.That(GroupOf(key), Is.Not.EqualTo(targetGroup));
            }
        }

        // Levels 3-6: distractors are drawn from the target's own group (near-identical outlines), never the
        // target itself twice over.
        [Test]
        public void HighLevelsDrawDistractorsFromTheSameGroup()
        {
            for (var seed = 0; seed < 200; seed++)
            for (var level = 3; level <= 6; level++)
            {
                var round = ItemToShadowRoundGenerator.Create(level, new Random(seed));
                var targetGroup = GroupOf(round.TargetKey);
                foreach (var key in round.Choices)
                    if (key != round.TargetKey) Assert.That(GroupOf(key), Is.EqualTo(targetGroup));
            }
        }
    }
}
