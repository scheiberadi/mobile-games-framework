using System;
using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class FingerMazeTests
    {
        private static readonly int[] CellCountByLevel = { 4, 5, 6, 7, 8, 9 };

        [Test]
        public void EveryRoundsPathHasTheLevelsCellCount()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = FingerMazeRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.Path.Length, Is.EqualTo(CellCountByLevel[level - 1]), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => FingerMazeRoundGenerator.Create(0, new Random(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => FingerMazeRoundGenerator.Create(7, new Random(1)));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = FingerMazeRoundGenerator.Create(4, new Random(7));
            var b = FingerMazeRoundGenerator.Create(4, new Random(7));
            Assert.That(a.Path, Is.EqualTo(b.Path));
        }

        [Test]
        public void RoundsPerSessionMatchesTheSharedFiveRoundWindow()
        {
            Assert.That(FingerMazeRoundGenerator.RoundsPerSession, Is.EqualTo(5));
        }

        // Every waypoint is a distinct grid cell (the walk never crosses or revisits itself), and every
        // consecutive pair is exactly one grid step apart (a single up/down/left/right move, never a diagonal
        // or a skip) - the two properties that make the corridor drawable as a simple connected line.
        [Test]
        public void PathIsSelfAvoidingAndEveryStepIsOneGridCellApart()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = FingerMazeRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.Path.Distinct().Count(), Is.EqualTo(round.Path.Length), "level " + level + " seed " + seed + " repeats a cell");

                for (var i = 1; i < round.Path.Length; i++)
                {
                    var dx = round.Path[i].X - round.Path[i - 1].X;
                    var dy = round.Path[i].Y - round.Path[i - 1].Y;
                    var isHorizontalStep = Math.Abs(Math.Abs(dx) - FingerMazeRoundGenerator.CellSize) < 0.01f && dy == 0f;
                    var isVerticalStep = Math.Abs(Math.Abs(dy) - FingerMazeRoundGenerator.CellSize) < 0.01f && dx == 0f;
                    Assert.IsTrue(isHorizontalStep || isVerticalStep, "level " + level + " seed " + seed + " step " + i + " is not one grid cell");
                }
            }
        }

        [Test]
        public void EveryWaypointStaysInsideTheGrid()
        {
            var minX = FingerMazeRoundGenerator.OriginX;
            var maxX = FingerMazeRoundGenerator.OriginX + (FingerMazeRoundGenerator.Columns - 1) * FingerMazeRoundGenerator.CellSize;
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = FingerMazeRoundGenerator.Create(level, new Random(seed));
                foreach (var point in round.Path)
                {
                    Assert.That(point.X, Is.InRange(minX - 0.01f, maxX + 0.01f));
                }
            }
        }

        [Test]
        public void NearestFractionIsZeroAtTheStartAndOneAtTheFinish()
        {
            var path = new[] { new WorldPoint(0f, 0f), new WorldPoint(100f, 0f), new WorldPoint(100f, 100f) };
            Assert.That(FingerMazePath.NearestFraction(path, path[0]), Is.EqualTo(0f).Within(0.001f));
            Assert.That(FingerMazePath.NearestFraction(path, path[2]), Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void NearestFractionIsHalfwayAtTheMidpointOfAStraightPath()
        {
            var path = new[] { new WorldPoint(0f, 0f), new WorldPoint(200f, 0f) };
            Assert.That(FingerMazePath.NearestFraction(path, new WorldPoint(100f, 0f)), Is.EqualTo(0.5f).Within(0.001f));
        }

        // A finger position off to the side of the corridor still projects onto the nearest point of the
        // centreline rather than being rejected - the whole point of "wide corridors, no timer" forgiveness.
        [Test]
        public void NearestFractionProjectsAPointBesideTheCorridorOntoTheCentreline()
        {
            var path = new[] { new WorldPoint(0f, 0f), new WorldPoint(200f, 0f) };
            Assert.That(FingerMazePath.NearestFraction(path, new WorldPoint(100f, 60f)), Is.EqualTo(0.5f).Within(0.001f));
        }
    }
}
