using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using Random = System.Random;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    public class JigsawTests
    {
        private static readonly int[] PieceCountByLevel = { 4, 6, 9, 16, 25, 25 };

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => JigsawRoundGenerator.Create(0, new Random(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => JigsawRoundGenerator.Create(7, new Random(1)));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = JigsawRoundGenerator.Create(4, new Random(7));
            var b = JigsawRoundGenerator.Create(4, new Random(7));
            for (var i = 0; i < a.Pieces.Length; i++)
            {
                Assert.That(a.Pieces[i].TrayPosition.X, Is.EqualTo(b.Pieces[i].TrayPosition.X));
                Assert.That(a.Pieces[i].TrayPosition.Y, Is.EqualTo(b.Pieces[i].TrayPosition.Y));
            }
        }

        [Test]
        public void RoundsPerSessionMatchesTheShorterThreePuzzleWindow()
        {
            Assert.That(JigsawRoundGenerator.RoundsPerSession, Is.EqualTo(3));
        }

        [Test]
        public void PieceCountMatchesTheLevelsGrid()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 50; seed++)
            {
                var round = JigsawRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.Pieces.Length, Is.EqualTo(PieceCountByLevel[level - 1]), "level " + level + " seed " + seed);
                Assert.That(round.Columns * round.Rows, Is.EqualTo(round.Pieces.Length));
            }
        }

        // Every piece's HomePosition must be a distinct point on the board grid, and every piece's own sprite
        // key must be distinct - no two pieces can claim the same slot or the same tile.
        [Test]
        public void EveryPieceHasADistinctHomePositionAndSpriteKey()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 50; seed++)
            {
                var round = JigsawRoundGenerator.Create(level, new Random(seed));
                var distinctHomes = round.Pieces.Select(p => (p.HomePosition.X, p.HomePosition.Y)).Distinct().Count();
                Assert.That(distinctHomes, Is.EqualTo(round.Pieces.Length), "level " + level + " seed " + seed);
                var distinctKeys = round.Pieces.Select(p => p.SpriteKey).Distinct().Count();
                Assert.That(distinctKeys, Is.EqualTo(round.Pieces.Length), "level " + level + " seed " + seed);
            }
        }

        // A piece whose picture is missing would silently show the placeholder, and a grid that reused another grid's
        // keys would show only a corner of the picture, so every level's keys must be real, imported sprites.
        [Test]
        public void EveryPieceOfEveryLevelHasItsOwnImportedPicture()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            {
                var round = JigsawRoundGenerator.Create(level, new Random(level));
                foreach (var piece in round.Pieces)
                {
                    Assert.IsNotNull(UnityEngine.Resources.Load<Sprite>("Art/" + piece.SpriteKey), piece.SpriteKey);
                    if (level < 6) Assert.IsTrue(seen.Add(piece.SpriteKey), "level " + level + " reuses " + piece.SpriteKey);
                }
            }
        }

        // Tray positions must also be a full permutation of the same grid shape - every tray slot is used
        // exactly once, same count as home slots.
        [Test]
        public void TrayPositionsAreAFullPermutationOfTheSameGridShape()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 50; seed++)
            {
                var round = JigsawRoundGenerator.Create(level, new Random(seed));
                var distinctTrays = round.Pieces.Select(p => (p.TrayPosition.X, p.TrayPosition.Y)).Distinct().Count();
                Assert.That(distinctTrays, Is.EqualTo(round.Pieces.Length), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void SnapRadiusIsPositiveAndNoLargerThanHalfTheSmallerCellDimension()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 50; seed++)
            {
                var round = JigsawRoundGenerator.Create(level, new Random(seed));
                Assert.Greater(round.SnapRadius, 0f, "level " + level + " seed " + seed);
                Assert.That(round.SnapRadius, Is.LessThanOrEqualTo(Math.Min(round.PieceWidth, round.PieceHeight) / 2f), "level " + level + " seed " + seed);
            }
        }

        // The board and tray areas must never overlap, so a scattered piece can never start already sitting on
        // top of the assembled board, and both stay inside the 1440 x 900 frame and clear of the Corner pair.
        [Test]
        public void BoardAndTrayAreasNeverOverlapAndStayInTheFrame()
        {
            var boardLeft = JigsawRoundGenerator.BoardCenter.X - JigsawRoundGenerator.BoardWidth / 2f;
            var boardRight = JigsawRoundGenerator.BoardCenter.X + JigsawRoundGenerator.BoardWidth / 2f;
            var trayLeft = JigsawRoundGenerator.TrayCenter.X - JigsawRoundGenerator.TrayWidth / 2f;
            var trayRight = JigsawRoundGenerator.TrayCenter.X + JigsawRoundGenerator.TrayWidth / 2f;
            var trayBottom = JigsawRoundGenerator.TrayCenter.Y - JigsawRoundGenerator.TrayHeight / 2f;
            var trayTop = JigsawRoundGenerator.TrayCenter.Y + JigsawRoundGenerator.TrayHeight / 2f;
            Assert.That(boardRight, Is.LessThanOrEqualTo(trayLeft));
            Assert.That(boardLeft, Is.GreaterThanOrEqualTo(-720f));
            Assert.That(trayRight, Is.LessThanOrEqualTo(720f));
            Assert.That(trayTop, Is.LessThanOrEqualTo(450f));
            Assert.That(trayBottom, Is.GreaterThanOrEqualTo(CompanionLayout.Corner.Footprint.YMax));
        }
    }
}
