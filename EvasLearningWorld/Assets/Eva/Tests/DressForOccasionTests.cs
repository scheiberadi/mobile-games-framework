using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class DressForOccasionTests
    {
        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DressForOccasionRoundGenerator.Create(0, new Random(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => DressForOccasionRoundGenerator.Create(7, new Random(1)));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = DressForOccasionRoundGenerator.Create(4, new Random(7));
            var b = DressForOccasionRoundGenerator.Create(4, new Random(7));
            Assert.That(a.Occasion, Is.EqualTo(b.Occasion));
            for (var i = 0; i < a.ShelfItems.Length; i++)
            {
                Assert.That(a.ShelfItems[i].ItemId, Is.EqualTo(b.ShelfItems[i].ItemId));
                Assert.That(a.ShelfItems[i].TrayPosition.X, Is.EqualTo(b.ShelfItems[i].TrayPosition.X));
            }
        }

        [Test]
        public void RoundsPerSessionMatchesTheShorterThreeRoundWindow()
        {
            Assert.That(DressForOccasionRoundGenerator.RoundsPerSession, Is.EqualTo(3));
        }

        // Level doubles as which occasion is being dressed for, in the brief's own listed order - the same
        // shape as ShoppingRoundGenerator's level-is-mode design.
        [Test]
        public void EachLevelProducesItsOwnOccasion()
        {
            var expected = new[] { Occasion.School, Occasion.Beach, Occasion.Winter, Occasion.Birthday, Occasion.Sports, Occasion.Camping };
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 20; seed++)
            {
                var round = DressForOccasionRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.Occasion, Is.EqualTo(expected[level - 1]), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void ShelfHasExactlyThreeCorrectItemsOnePerSlotAndTwoDistractors()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = DressForOccasionRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.ShelfItems.Length, Is.EqualTo(5), "level " + level + " seed " + seed);

                var correct = round.ShelfItems.Where(i => i.Correct).ToArray();
                Assert.That(correct.Length, Is.EqualTo(3), "level " + level + " seed " + seed);
                Assert.That(correct.Select(i => i.Slot).Distinct().Count(), Is.EqualTo(3), "level " + level + " seed " + seed);

                var distractors = round.ShelfItems.Where(i => !i.Correct).ToArray();
                Assert.That(distractors.Length, Is.EqualTo(2), "level " + level + " seed " + seed);
            }
        }

        // A distractor must never be one of this occasion's own 3 correct items (it must come from another
        // occasion entirely), so the child can never accidentally "cheat" by matching a duplicate id.
        [Test]
        public void DistractorsAreNeverThisOccasionsOwnItems()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = DressForOccasionRoundGenerator.Create(level, new Random(seed));
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
                var round = DressForOccasionRoundGenerator.Create(level, new Random(seed));
                var distinct = round.ShelfItems.Select(i => (i.TrayPosition.X, i.TrayPosition.Y)).Distinct().Count();
                Assert.That(distinct, Is.EqualTo(round.ShelfItems.Length), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void SlotHomePositionCoversExactlyTopBottomFeetAndIsFixedAcrossRounds()
        {
            var first = DressForOccasionRoundGenerator.Create(1, new Random(1));
            Assert.That(first.SlotHomePosition.Keys, Is.EquivalentTo(new[] { ClothingSlot.Top, ClothingSlot.Bottom, ClothingSlot.Feet }));

            for (var seed = 2; seed < 50; seed++)
            {
                var round = DressForOccasionRoundGenerator.Create(3, new Random(seed));
                foreach (var slot in new[] { ClothingSlot.Top, ClothingSlot.Bottom, ClothingSlot.Feet })
                {
                    Assert.That(round.SlotHomePosition[slot].X, Is.EqualTo(first.SlotHomePosition[slot].X), "seed " + seed + " slot " + slot);
                    Assert.That(round.SlotHomePosition[slot].Y, Is.EqualTo(first.SlotHomePosition[slot].Y), "seed " + seed + " slot " + slot);
                }
            }
        }

        [Test]
        public void SnapRadiusIsPositiveAndUnderHalfTheSlotColumnSpacing()
        {
            Assert.Greater(DressForOccasionRoundGenerator.SnapRadius, 0f);
            // Slot columns are 260 apart; half of that is 130.
            Assert.That(DressForOccasionRoundGenerator.SnapRadius, Is.LessThan(130f));
        }
    }
}
