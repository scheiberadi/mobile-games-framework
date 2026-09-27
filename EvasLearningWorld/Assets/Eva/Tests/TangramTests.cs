using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class TangramTests
    {
        private static readonly int[] ShapeCountByLevel = { 3, 4, 5, 6, 7, 7 };

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TangramRoundGenerator.Create(0, new Random(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => TangramRoundGenerator.Create(7, new Random(1)));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = TangramRoundGenerator.Create(4, new Random(7));
            var b = TangramRoundGenerator.Create(4, new Random(7));
            for (var i = 0; i < a.Pieces.Length; i++)
            {
                Assert.That(a.Pieces[i].TrayPosition.X, Is.EqualTo(b.Pieces[i].TrayPosition.X));
                Assert.That(a.Pieces[i].SizeScale, Is.EqualTo(b.Pieces[i].SizeScale));
            }
        }

        [Test]
        public void RoundsPerSessionMatchesTheShorterThreeSilhouetteWindow()
        {
            Assert.That(TangramRoundGenerator.RoundsPerSession, Is.EqualTo(3));
        }

        [Test]
        public void ShapeCountMatchesTheClassicSevenPieceLadder()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 50; seed++)
            {
                var round = TangramRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.Pieces.Length, Is.EqualTo(ShapeCountByLevel[level - 1]), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void ShapeCountNeverExceedsTheClassicSevenPieceSet()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 50; seed++)
            {
                var round = TangramRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.Pieces.Length, Is.LessThanOrEqualTo(7), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void EveryPieceHasADistinctHomePositionAndSpriteKey()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 50; seed++)
            {
                var round = TangramRoundGenerator.Create(level, new Random(seed));
                var distinctHomes = round.Pieces.Select(p => (p.HomePosition.X, p.HomePosition.Y)).Distinct().Count();
                Assert.That(distinctHomes, Is.EqualTo(round.Pieces.Length), "level " + level + " seed " + seed);
                var distinctKeys = round.Pieces.Select(p => p.SpriteKey).Distinct().Count();
                Assert.That(distinctKeys, Is.EqualTo(round.Pieces.Length), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void TrayPositionsAreAFullPermutationWithNoDuplicates()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 50; seed++)
            {
                var round = TangramRoundGenerator.Create(level, new Random(seed));
                var distinctTrays = round.Pieces.Select(p => (p.TrayPosition.X, p.TrayPosition.Y)).Distinct().Count();
                Assert.That(distinctTrays, Is.EqualTo(round.Pieces.Length), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void EveryPieceHasAPositiveSizeScale()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 50; seed++)
            {
                var round = TangramRoundGenerator.Create(level, new Random(seed));
                foreach (var piece in round.Pieces) Assert.Greater(piece.SizeScale, 0f, "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void SnapRadiusIsPositiveAndNoLargerThanHalfTheSmallerCellDimension()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 50; seed++)
            {
                var round = TangramRoundGenerator.Create(level, new Random(seed));
                Assert.Greater(round.SnapRadius, 0f, "level " + level + " seed " + seed);
                Assert.That(round.SnapRadius, Is.LessThanOrEqualTo(Math.Min(round.BasePieceWidth, round.BasePieceHeight) / 2f), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void BoardAndTrayAreasNeverOverlap()
        {
            var boardTop = TangramRoundGenerator.BoardCenter.Y + TangramRoundGenerator.BoardHeight / 2f;
            var boardBottom = TangramRoundGenerator.BoardCenter.Y - TangramRoundGenerator.BoardHeight / 2f;
            var trayTop = TangramRoundGenerator.TrayCenter.Y + TangramRoundGenerator.TrayHeight / 2f;
            var trayBottom = TangramRoundGenerator.TrayCenter.Y - TangramRoundGenerator.TrayHeight / 2f;
            Assert.IsTrue(trayTop <= boardBottom || boardTop <= trayBottom);
        }
    }
}
