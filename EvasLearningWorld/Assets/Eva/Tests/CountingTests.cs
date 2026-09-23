using System;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class CountingTests
    {
        [Test]
        public void EveryRoundHasValidQuantityAndThreeDistinctAscendingChoices()
        {
            for (var seed = 0; seed < 200; seed++)
            for (var round = 0; round < CountRoundGenerator.RoundsPerSession; round++)
            {
                var r = CountRoundGenerator.Create(round, new Random(seed), null);
                var max = CountRoundGenerator.MaxQuantityByRound[round];
                var pool = Math.Max(3, max);
                Assert.That(r.Quantity, Is.InRange(1, max));
                Assert.That(r.Choices.Length, Is.EqualTo(3));
                Assert.That(r.Choices, Is.Ordered.Ascending);
                Assert.That(r.Choices, Is.Unique);
                Assert.That(r.Choices, Does.Contain(r.Quantity));
                Assert.That(r.Choices, Is.All.InRange(1, pool));
            }
        }

        [Test]
        public void ObjectDiffersFromThePreviousRound()
        {
            for (var seed = 0; seed < 100; seed++)
                foreach (CountObject previous in Enum.GetValues(typeof(CountObject)))
                    Assert.That(CountRoundGenerator.Create(2, new Random(seed), previous).Object, Is.Not.EqualTo(previous));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = CountRoundGenerator.Create(3, new Random(7), null);
            var b = CountRoundGenerator.Create(3, new Random(7), null);
            Assert.That(a.Quantity, Is.EqualTo(b.Quantity));
            Assert.That(a.Object, Is.EqualTo(b.Object));
            Assert.That(a.Choices, Is.EqualTo(b.Choices));
        }

        [Test]
        public void RoundIndexOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CountRoundGenerator.Create(5, new Random(1), null));
            Assert.Throws<ArgumentOutOfRangeException>(() => CountRoundGenerator.Create(-1, new Random(1), null));
        }

        [Test]
        public void TallyCountsEachObjectOnceInTheOrderTheChildTapsThem()
        {
            var tally = new CountTally(3);
            Assert.That(tally.TryCount(2, out var n), Is.True);
            Assert.That(n, Is.EqualTo(1));
            Assert.That(tally.TryCount(0, out n), Is.True);
            Assert.That(n, Is.EqualTo(2));
            Assert.That(tally.IsComplete, Is.False);
            Assert.That(tally.TryCount(1, out n), Is.True);
            Assert.That(n, Is.EqualTo(3));
            Assert.That(tally.IsComplete, Is.True);
        }

        [Test]
        public void TappingAnAlreadyCountedObjectDoesNotAdvanceTheCount()
        {
            var tally = new CountTally(4);
            tally.TryCount(1, out _);
            tally.TryCount(3, out _);
            Assert.That(tally.TryCount(1, out var n), Is.False);
            Assert.That(n, Is.EqualTo(2));
            Assert.That(tally.Counted, Is.EqualTo(2));
            Assert.That(tally.IsCounted(1), Is.True);
            Assert.That(tally.IsCounted(0), Is.False);
        }

        [Test]
        public void OutOfRangeIndexIsIgnored()
        {
            var tally = new CountTally(2);
            Assert.That(tally.TryCount(-1, out _), Is.False);
            Assert.That(tally.TryCount(2, out _), Is.False);
            Assert.That(tally.Counted, Is.EqualTo(0));
        }
    }
}
