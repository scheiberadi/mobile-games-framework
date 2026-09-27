using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class DressTheCharacterTests
    {
        private static readonly int[] PoolSizeByLevel = { 1, 2, 2, 3, 4, 4 };

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DressTheCharacterRoundGenerator.Create(0, new Random(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => DressTheCharacterRoundGenerator.Create(7, new Random(1)));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = DressTheCharacterRoundGenerator.Create(4, new Random(7));
            var b = DressTheCharacterRoundGenerator.Create(4, new Random(7));
            for (var i = 0; i < a.Pieces.Length; i++)
            {
                Assert.That(a.Pieces[i].ItemId, Is.EqualTo(b.Pieces[i].ItemId));
                Assert.That(a.Pieces[i].TrayPosition.X, Is.EqualTo(b.Pieces[i].TrayPosition.X));
            }
        }

        [Test]
        public void RoundsPerSessionMatchesTheShorterThreeRoundWindow()
        {
            Assert.That(DressTheCharacterRoundGenerator.RoundsPerSession, Is.EqualTo(3));
        }

        [Test]
        public void EveryRoundHasExactlyOnePiecePerSlot()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = DressTheCharacterRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.Pieces.Length, Is.EqualTo(4), "level " + level + " seed " + seed);
                var slots = round.Pieces.Select(p => p.Slot).Distinct().Count();
                Assert.That(slots, Is.EqualTo(4), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void EveryPieceHasADistinctHomePositionAndTrayPosition()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = DressTheCharacterRoundGenerator.Create(level, new Random(seed));
                var homes = round.Pieces.Select(p => (p.HomePosition.X, p.HomePosition.Y)).Distinct().Count();
                Assert.That(homes, Is.EqualTo(4), "level " + level + " seed " + seed);
                var trays = round.Pieces.Select(p => (p.TrayPosition.X, p.TrayPosition.Y)).Distinct().Count();
                Assert.That(trays, Is.EqualTo(4), "level " + level + " seed " + seed);
            }
        }

        // A slot's fixed body position never changes round to round - only which item variant sits there does
        // (see DressTheCharacterRoundGenerator's own class comment on why there is no single "correct" item).
        [Test]
        public void EverySlotsHomePositionIsFixedAcrossRounds()
        {
            var first = DressTheCharacterRoundGenerator.Create(3, new Random(1));
            for (var seed = 2; seed < 50; seed++)
            {
                var round = DressTheCharacterRoundGenerator.Create(3, new Random(seed));
                foreach (var piece in round.Pieces)
                {
                    var expected = first.Pieces.First(p => p.Slot == piece.Slot).HomePosition;
                    Assert.That(piece.HomePosition.X, Is.EqualTo(expected.X), "seed " + seed + " slot " + piece.Slot);
                    Assert.That(piece.HomePosition.Y, Is.EqualTo(expected.Y), "seed " + seed + " slot " + piece.Slot);
                }
            }
        }

        [Test]
        public void ItemIdStaysWithinTheLevelsPoolSize()
        {
            var catalogue = new System.Collections.Generic.Dictionary<ClothingSlot, string[]>
            {
                { ClothingSlot.Head, new[] { "cap", "hat", "beanie", "sunhat" } },
                { ClothingSlot.Top, new[] { "shirt", "jacket", "sweater", "tshirt" } },
                { ClothingSlot.Bottom, new[] { "pants", "shorts", "skirt", "jeans" } },
                { ClothingSlot.Feet, new[] { "shoes", "sandals", "boots", "sneakers" } },
            };
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = DressTheCharacterRoundGenerator.Create(level, new Random(seed));
                foreach (var piece in round.Pieces)
                {
                    var pool = catalogue[piece.Slot].Take(PoolSizeByLevel[level - 1]);
                    Assert.IsTrue(pool.Contains(piece.ItemId), "level " + level + " seed " + seed + " slot " + piece.Slot);
                }
            }
        }

        [Test]
        public void SnapRadiusIsPositiveAndUnderHalfTheColumnSpacing()
        {
            Assert.Greater(DressTheCharacterRoundGenerator.SnapRadius, 0f);
            // Columns are 260 apart (240-unit slots + 20 clearance); half of that is 130.
            Assert.That(DressTheCharacterRoundGenerator.SnapRadius, Is.LessThan(130f));
        }
    }
}
