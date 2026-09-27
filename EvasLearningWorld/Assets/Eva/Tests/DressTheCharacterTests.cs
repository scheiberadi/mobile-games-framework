using System;
using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class DressTheCharacterTests
    {
        private static readonly int[] PoolSizeByLevel = { 1, 2, 2, 3, 3, 4 };

        private static readonly Dictionary<(WardrobeSlot, Gender), string[]> Pools = new Dictionary<(WardrobeSlot, Gender), string[]>
        {
            { (WardrobeSlot.Top, Gender.Boy), new[] { "top_boy_0", "top_boy_1", "top_boy_2", "top_boy_3" } },
            { (WardrobeSlot.Top, Gender.Girl), new[] { "top_girl_0", "top_girl_1", "top_girl_2", "top_girl_3" } },
            { (WardrobeSlot.Bottom, Gender.Boy), new[] { "bottom_boy_0", "bottom_boy_1", "bottom_boy_2" } },
            { (WardrobeSlot.Bottom, Gender.Girl), new[] { "bottom_girl_0", "bottom_girl_1", "bottom_girl_2" } },
            { (WardrobeSlot.Dress, Gender.Girl), new[] { "dress_girl_0", "dress_girl_1", "dress_girl_2" } },
            { (WardrobeSlot.Shoes, Gender.Boy), new[] { "shoes_boy_0", "shoes_boy_1", "shoes_boy_2" } },
            { (WardrobeSlot.Shoes, Gender.Girl), new[] { "shoes_girl_0", "shoes_girl_1", "shoes_girl_2" } },
            { (WardrobeSlot.Glasses, Gender.Boy), new[] { "glasses_0", "glasses_1", "glasses_2" } },
            { (WardrobeSlot.Glasses, Gender.Girl), new[] { "glasses_0", "glasses_1", "glasses_2" } },
        };

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DressTheCharacterRoundGenerator.Create(0, new Random(1), Gender.Boy));
            Assert.Throws<ArgumentOutOfRangeException>(() => DressTheCharacterRoundGenerator.Create(7, new Random(1), Gender.Boy));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = DressTheCharacterRoundGenerator.Create(4, new Random(7), Gender.Girl);
            var b = DressTheCharacterRoundGenerator.Create(4, new Random(7), Gender.Girl);
            Assert.That(a.Pieces.Length, Is.EqualTo(b.Pieces.Length));
            for (var i = 0; i < a.Pieces.Length; i++)
            {
                Assert.That(a.Pieces[i].Slot, Is.EqualTo(b.Pieces[i].Slot));
                Assert.That(a.Pieces[i].ItemId, Is.EqualTo(b.Pieces[i].ItemId));
                Assert.That(a.Pieces[i].TrayPosition.X, Is.EqualTo(b.Pieces[i].TrayPosition.X));
            }
        }

        [Test]
        public void RoundsPerSessionMatchesTheShorterThreeRoundWindow()
        {
            Assert.That(DressTheCharacterRoundGenerator.RoundsPerSession, Is.EqualTo(3));
        }

        // Dress is girls-only (CharacterLook.Normalize's own rule) - a boy's session must never roll it, so
        // every boy round is the full 4-piece Top+Bottom+Shoes+Glasses shape.
        [Test]
        public void BoysNeverRollADressRoundAndAlwaysGetFourPieces()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = DressTheCharacterRoundGenerator.Create(level, new Random(seed), Gender.Boy);
                Assert.That(round.Pieces.Length, Is.EqualTo(4), "level " + level + " seed " + seed);
                CollectionAssert.AreEquivalent(
                    new[] { WardrobeSlot.Top, WardrobeSlot.Bottom, WardrobeSlot.Shoes, WardrobeSlot.Glasses },
                    round.Pieces.Select(p => p.Slot).ToArray(), "level " + level + " seed " + seed);
            }
        }

        // Girls roll between the two round shapes for outfit variety (see the class comment) - across many
        // seeds, both shapes must actually occur, not just one of them.
        [Test]
        public void GirlsRollBothRoundShapesAcrossManySeeds()
        {
            var sawDress = false;
            var sawTopAndBottom = false;
            for (var seed = 0; seed < 200; seed++)
            {
                var round = DressTheCharacterRoundGenerator.Create(3, new Random(seed), Gender.Girl);
                if (round.Pieces.Any(p => p.Slot == WardrobeSlot.Dress)) sawDress = true;
                else sawTopAndBottom = true;
            }
            Assert.IsTrue(sawDress, "expected at least one Dress round across 200 seeds");
            Assert.IsTrue(sawTopAndBottom, "expected at least one Top+Bottom round across 200 seeds");
        }

        // Dress replaces Top+Bottom at once (CharacterLook.SetDress) - a Dress round must never also include
        // a Top or a Bottom piece, and is exactly 3 pieces, not 4.
        [Test]
        public void ADressRoundHasExactlyThreePiecesAndNeverATopOrBottom()
        {
            for (var seed = 0; seed < 200; seed++)
            {
                var round = DressTheCharacterRoundGenerator.Create(3, new Random(seed), Gender.Girl);
                if (!round.Pieces.Any(p => p.Slot == WardrobeSlot.Dress)) continue;
                Assert.That(round.Pieces.Length, Is.EqualTo(3), "seed " + seed);
                CollectionAssert.AreEquivalent(
                    new[] { WardrobeSlot.Dress, WardrobeSlot.Shoes, WardrobeSlot.Glasses },
                    round.Pieces.Select(p => p.Slot).ToArray(), "seed " + seed);
            }
        }

        [Test]
        public void EveryPieceHasADistinctHomePositionAndTrayPosition()
        {
            foreach (var gender in new[] { Gender.Boy, Gender.Girl })
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 50; seed++)
            {
                var round = DressTheCharacterRoundGenerator.Create(level, new Random(seed), gender);
                var homes = round.Pieces.Select(p => (p.HomePosition.X, p.HomePosition.Y)).Distinct().Count();
                Assert.That(homes, Is.EqualTo(round.Pieces.Length), gender + " level " + level + " seed " + seed);
                var trays = round.Pieces.Select(p => (p.TrayPosition.X, p.TrayPosition.Y)).Distinct().Count();
                Assert.That(trays, Is.EqualTo(round.Pieces.Length), gender + " level " + level + " seed " + seed);
            }
        }

        // A slot's fixed body position never changes round to round - only which item variant sits there
        // does (see the class comment on why there is no placement puzzle here). Top and Dress deliberately
        // share the same column (they're mutually exclusive, never in the same round), so every other slot
        // must still resolve to exactly one fixed position, and Top/Dress's shared column is asserted
        // explicitly rather than assumed.
        [Test]
        public void EverySlotsHomePositionIsFixedAcrossRounds()
        {
            var knownHomeBySlot = new Dictionary<WardrobeSlot, (float X, float Y)>();
            foreach (var gender in new[] { Gender.Boy, Gender.Girl })
            for (var seed = 0; seed < 100; seed++)
            {
                var round = DressTheCharacterRoundGenerator.Create(3, new Random(seed), gender);
                foreach (var piece in round.Pieces)
                {
                    var home = (piece.HomePosition.X, piece.HomePosition.Y);
                    if (knownHomeBySlot.TryGetValue(piece.Slot, out var expected))
                        Assert.That(home, Is.EqualTo(expected), gender + " seed " + seed + " slot " + piece.Slot);
                    else
                        knownHomeBySlot[piece.Slot] = home;
                }
            }
            Assert.That(knownHomeBySlot.Keys, Is.EquivalentTo(new[] { WardrobeSlot.Top, WardrobeSlot.Bottom, WardrobeSlot.Dress, WardrobeSlot.Shoes, WardrobeSlot.Glasses }));
            Assert.That(knownHomeBySlot[WardrobeSlot.Top], Is.EqualTo(knownHomeBySlot[WardrobeSlot.Dress]), "Top and Dress share a column - they never appear together");
        }

        [Test]
        public void ItemIdStaysWithinTheLevelsPoolSizeAndRespectsGender()
        {
            foreach (var gender in new[] { Gender.Boy, Gender.Girl })
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = DressTheCharacterRoundGenerator.Create(level, new Random(seed), gender);
                foreach (var piece in round.Pieces)
                {
                    var fullPool = Pools[(piece.Slot, gender)];
                    var poolSize = Math.Min(PoolSizeByLevel[level - DifficultyLadder.MinLevel], fullPool.Length);
                    var pool = fullPool.Take(poolSize);
                    Assert.IsTrue(pool.Contains(piece.ItemId), gender + " level " + level + " seed " + seed + " slot " + piece.Slot);
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
