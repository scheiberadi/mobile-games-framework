using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class AvoidObstaclesTests
    {
        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => AvoidObstaclesRoundGenerator.Create(0, new Random(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => AvoidObstaclesRoundGenerator.Create(7, new Random(1)));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = AvoidObstaclesRoundGenerator.Create(4, new Random(7));
            var b = AvoidObstaclesRoundGenerator.Create(4, new Random(7));
            Assert.That(a.Path, Is.EqualTo(b.Path));
            Assert.That(a.Hazards, Is.EqualTo(b.Hazards));
        }

        [Test]
        public void RoundsPerSessionMatchesTheSharedFiveRoundWindow()
        {
            Assert.That(AvoidObstaclesRoundGenerator.RoundsPerSession, Is.EqualTo(5));
        }

        // The Path is exactly Finger Maze's own generated corridor for the same level/seed - this game never
        // alters the walk itself, only what stands beside it.
        [Test]
        public void PathIsExactlyFingerMazesOwnPathForTheSameLevelAndSeed()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = AvoidObstaclesRoundGenerator.Create(level, new Random(seed));
                var maze = FingerMazeRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.Path, Is.EqualTo(maze.Path), "level " + level + " seed " + seed);
            }
        }

        // Every hazard must sit exactly one grid step from some point of the path (never zero, i.e. never on the
        // path itself, and never further than a single adjacent cell) - a temptation right beside the corridor,
        // not a wall blocking it.
        [Test]
        public void EveryHazardIsExactlyOneGridStepFromThePathAndNeverOnIt()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = AvoidObstaclesRoundGenerator.Create(level, new Random(seed));
                foreach (var hazard in round.Hazards)
                {
                    var onPath = round.Path.Any(p => Distance(p, hazard) < 0.01f);
                    Assert.IsFalse(onPath, "level " + level + " seed " + seed + " hazard sits on the path");

                    var isAdjacent = round.Path.Any(p => Math.Abs(Distance(p, hazard) - FingerMazeRoundGenerator.CellSize) < 0.5f);
                    Assert.IsTrue(isAdjacent, "level " + level + " seed " + seed + " hazard is not one grid step from the path");
                }
            }
        }

        [Test]
        public void HazardsAreNeverPlacedAtTheStartOrFinishCell()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = AvoidObstaclesRoundGenerator.Create(level, new Random(seed));
                var start = round.Path[0];
                var finish = round.Path[round.Path.Length - 1];
                foreach (var hazard in round.Hazards)
                {
                    Assert.Greater(Distance(hazard, start), 0.01f, "level " + level + " seed " + seed);
                    Assert.Greater(Distance(hazard, finish), 0.01f, "level " + level + " seed " + seed);
                }
            }
        }

        [Test]
        public void HazardsAreNeverDuplicated()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = AvoidObstaclesRoundGenerator.Create(level, new Random(seed));
                var distinctCount = round.Hazards.Select(h => (h.X, h.Y)).Distinct().Count();
                Assert.That(distinctCount, Is.EqualTo(round.Hazards.Length), "level " + level + " seed " + seed);
            }
        }

        // Higher levels ask for at least as many hazards as lower ones (the level table itself is monotonic);
        // PlaceHazards can return fewer than requested when a short path has too few free neighbours, but never
        // negative and never more than requested.
        [Test]
        public void HazardCountNeverExceedsTheLevelsRequestedCount()
        {
            var requestedByLevel = new[] { 1, 1, 2, 2, 3, 3 };
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = AvoidObstaclesRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.Hazards.Length, Is.LessThanOrEqualTo(requestedByLevel[level - 1]), "level " + level + " seed " + seed);
            }
        }

        private static float Distance(WorldPoint a, WorldPoint b)
        {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
