using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class WhichDoesntMakeSenseTests
    {
        [Test]
        public void EveryRoundHasFourItemsWithOneOddOne()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = WhichDoesntMakeSenseRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Items.Length, Is.EqualTo(4));
                Assert.That(round.OddIndex, Is.InRange(0, 3));
                Assert.That(round.Items.Distinct().Count(), Is.EqualTo(4));
            }
        }

        [Test]
        public void PoolIndexDiffersFromThePreviousRound()
        {
            for (var seed = 0; seed < 200; seed++)
            {
                var first = WhichDoesntMakeSenseRoundGenerator.Create(1, new Random(seed), null);
                var second = WhichDoesntMakeSenseRoundGenerator.Create(1, new Random(seed + 1000), first.PoolIndex);
                Assert.That(second.PoolIndex, Is.Not.EqualTo(first.PoolIndex));
            }
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = WhichDoesntMakeSenseRoundGenerator.Create(3, new Random(7), null);
            var b = WhichDoesntMakeSenseRoundGenerator.Create(3, new Random(7), null);
            Assert.That(a.Items, Is.EqualTo(b.Items));
            Assert.That(a.OddIndex, Is.EqualTo(b.OddIndex));
            Assert.That(a.PoolIndex, Is.EqualTo(b.PoolIndex));
        }

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => WhichDoesntMakeSenseRoundGenerator.Create(0, new Random(1), null));
            Assert.Throws<ArgumentOutOfRangeException>(() => WhichDoesntMakeSenseRoundGenerator.Create(7, new Random(1), null));
        }

        [Test]
        public void RoundsPerSessionMatchesTheSharedFiveRoundWindow()
        {
            Assert.That(WhichDoesntMakeSenseRoundGenerator.RoundsPerSession, Is.EqualTo(5));
        }
    }
}
