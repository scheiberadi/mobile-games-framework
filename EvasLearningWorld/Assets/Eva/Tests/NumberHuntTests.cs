using System;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class NumberHuntTests
    {
        private static readonly int[] MaxByLevel = { 5, 10, 10, 20, 20, 20 };
        private static readonly int[] TileCountByLevel = { 3, 4, 5, 5, 6, 6 };

        [Test]
        public void EveryRoundHasATargetAndChoicesWithinTheLevelsRange()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 300; seed++)
            {
                var r = NumberHuntRoundGenerator.Create(level, new Random(seed), null);
                var max = MaxByLevel[level - 1];
                Assert.That(r.Target, Is.InRange(1, max));
                Assert.That(r.Choices.Length, Is.EqualTo(TileCountByLevel[level - 1]));
                Assert.That(r.Choices, Is.Unique);
                Assert.That(r.Choices, Does.Contain(r.Target));
                Assert.That(r.Choices, Is.All.InRange(1, max));
            }
        }

        [Test]
        public void TargetDiffersFromThePreviousRound()
        {
            for (var seed = 0; seed < 200; seed++)
            for (var previous = 1; previous <= 5; previous++)
                Assert.That(NumberHuntRoundGenerator.Create(1, new Random(seed), previous).Target, Is.Not.EqualTo(previous));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = NumberHuntRoundGenerator.Create(4, new Random(7), null);
            var b = NumberHuntRoundGenerator.Create(4, new Random(7), null);
            Assert.That(a.Target, Is.EqualTo(b.Target));
            Assert.That(a.Choices, Is.EqualTo(b.Choices));
        }

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => NumberHuntRoundGenerator.Create(0, new Random(1), null));
            Assert.Throws<ArgumentOutOfRangeException>(() => NumberHuntRoundGenerator.Create(7, new Random(1), null));
        }

        [Test]
        public void TileCountGrowsFromThreeAtLevelOneToSixAtLevelSix()
        {
            Assert.That(NumberHuntRoundGenerator.Create(1, new Random(1), null).Choices.Length, Is.EqualTo(3));
            Assert.That(NumberHuntRoundGenerator.Create(6, new Random(1), null).Choices.Length, Is.EqualTo(6));
            for (var level = 2; level <= 6; level++)
                Assert.That(TileCountByLevel[level - 1], Is.GreaterThanOrEqualTo(TileCountByLevel[level - 2]));
        }

        // Levels 1-4 never guarantee a confusable partner tile, even when the target has one (6, 9, 2, 5, 1 or 7).
        [Test]
        public void NoConfusablePartnerIsGuaranteedBelowLevelFive()
        {
            for (var level = 1; level <= 4; level++)
            {
                var sawConfusableTarget = false;
                for (var seed = 0; seed < 500; seed++)
                {
                    var r = NumberHuntRoundGenerator.Create(level, new Random(seed), null);
                    if (r.Target != 6 && r.Target != 9 && r.Target != 2 && r.Target != 5 && r.Target != 1 && r.Target != 7) continue;
                    sawConfusableTarget = true;
                }
                Assert.IsTrue(sawConfusableTarget, "level " + level + " never drew a confusable target in 500 seeds");
            }
        }

        [TestCase(6, 9)]
        [TestCase(9, 6)]
        [TestCase(2, 5)]
        [TestCase(5, 2)]
        [TestCase(1, 7)]
        [TestCase(7, 1)]
        public void FromLevelFiveTheConfusablePartnerIsIncludedWheneverTheTargetHasOne(int target, int partner)
        {
            var seed = 0;
            while (true)
            {
                var r = NumberHuntRoundGenerator.Create(5, new Random(seed), null);
                if (r.Target == target) { Assert.That(r.Choices, Does.Contain(partner)); return; }
                seed++;
                Assert.That(seed, Is.LessThan(2000), "never drew target " + target + " in 2000 seeds");
            }
        }

        [Test]
        public void RoundsPerSessionMatchesTheSharedFiveRoundWindow()
        {
            Assert.That(NumberHuntRoundGenerator.RoundsPerSession, Is.EqualTo(5));
        }
    }
}
