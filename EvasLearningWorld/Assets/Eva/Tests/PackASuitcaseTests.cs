using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class PackASuitcaseTests
    {
        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PackASuitcaseRoundGenerator.Create(0, new Random(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => PackASuitcaseRoundGenerator.Create(7, new Random(1)));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = PackASuitcaseRoundGenerator.Create(4, new Random(7));
            var b = PackASuitcaseRoundGenerator.Create(4, new Random(7));
            Assert.That(a.Trip, Is.EqualTo(b.Trip));
            for (var i = 0; i < a.ShelfItems.Length; i++)
            {
                Assert.That(a.ShelfItems[i].ItemId, Is.EqualTo(b.ShelfItems[i].ItemId));
                Assert.That(a.ShelfItems[i].TrayPosition.X, Is.EqualTo(b.ShelfItems[i].TrayPosition.X));
            }
        }

        [Test]
        public void RoundsPerSessionMatchesTheShorterThreeRoundWindow()
        {
            Assert.That(PackASuitcaseRoundGenerator.RoundsPerSession, Is.EqualTo(3));
        }

        // Level doubles as which trip is being packed for, in the same order as Dress for the Occasion's own
        // Occasion-per-level mapping - reinforcing the same vocabulary rather than introducing a new one.
        [Test]
        public void EachLevelProducesItsOwnTrip()
        {
            var expected = new[] { Occasion.School, Occasion.Beach, Occasion.Winter, Occasion.Birthday, Occasion.Sports, Occasion.Camping };
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 20; seed++)
            {
                var round = PackASuitcaseRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.Trip, Is.EqualTo(expected[level - 1]), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void ShelfHasExactlyThreeCorrectItemsAndTwoDistractors()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = PackASuitcaseRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.ShelfItems.Length, Is.EqualTo(5), "level " + level + " seed " + seed);
                Assert.That(round.ShelfItems.Count(i => i.Correct), Is.EqualTo(3), "level " + level + " seed " + seed);
                Assert.That(round.ShelfItems.Count(i => !i.Correct), Is.EqualTo(2), "level " + level + " seed " + seed);
            }
        }

        // A distractor must never be one of this trip's own 3 correct items (it must come from another trip
        // entirely), so the child can never accidentally "cheat" by matching a duplicate id.
        [Test]
        public void DistractorsAreNeverThisTripsOwnItems()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = PackASuitcaseRoundGenerator.Create(level, new Random(seed));
                var correctIds = round.ShelfItems.Where(i => i.Correct).Select(i => i.ItemId).ToArray();
                foreach (var item in round.ShelfItems.Where(i => !i.Correct))
                    Assert.IsFalse(correctIds.Contains(item.ItemId), "level " + level + " seed " + seed + " item " + item.ItemId);
            }
        }

        [Test]
        public void EveryShelfItemHasADistinctTrayPosition()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = PackASuitcaseRoundGenerator.Create(level, new Random(seed));
                var distinct = round.ShelfItems.Select(i => (i.TrayPosition.X, i.TrayPosition.Y)).Distinct().Count();
                Assert.That(distinct, Is.EqualTo(round.ShelfItems.Length), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void SuitcaseHasExactlyThreeDistinctSlotPositionsFixedAcrossRounds()
        {
            var first = PackASuitcaseRoundGenerator.Create(1, new Random(1));
            Assert.That(first.SlotPositions.Length, Is.EqualTo(3));
            var distinct = first.SlotPositions.Select(p => (p.X, p.Y)).Distinct().Count();
            Assert.That(distinct, Is.EqualTo(3));

            for (var seed = 2; seed < 50; seed++)
            {
                var round = PackASuitcaseRoundGenerator.Create(3, new Random(seed));
                for (var i = 0; i < 3; i++)
                {
                    Assert.That(round.SlotPositions[i].X, Is.EqualTo(first.SlotPositions[i].X), "seed " + seed);
                    Assert.That(round.SlotPositions[i].Y, Is.EqualTo(first.SlotPositions[i].Y), "seed " + seed);
                }
            }
        }

        [Test]
        public void SnapRadiusIsPositiveAndUnderHalfTheSlotColumnSpacing()
        {
            Assert.Greater(PackASuitcaseRoundGenerator.SnapRadius, 0f);
            // Slot columns are 260 apart; half of that is 130.
            Assert.That(PackASuitcaseRoundGenerator.SnapRadius, Is.LessThan(130f));
        }
    }
}
