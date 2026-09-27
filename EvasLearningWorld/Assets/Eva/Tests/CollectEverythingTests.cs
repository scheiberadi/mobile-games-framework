using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class CollectEverythingTests
    {
        private static readonly int[] RequestedByLevel = { 1, 2, 2, 3, 3, 4 };

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CollectEverythingRoundGenerator.Create(0, new Random(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => CollectEverythingRoundGenerator.Create(7, new Random(1)));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = CollectEverythingRoundGenerator.Create(4, new Random(7));
            var b = CollectEverythingRoundGenerator.Create(4, new Random(7));
            Assert.That(a.Pickups, Is.EqualTo(b.Pickups));
            Assert.That(a.PickupFractions, Is.EqualTo(b.PickupFractions));
        }

        [Test]
        public void RoundsPerSessionMatchesTheSharedFiveRoundWindow()
        {
            Assert.That(CollectEverythingRoundGenerator.RoundsPerSession, Is.EqualTo(5));
        }

        [Test]
        public void PathIsExactlyFingerMazesOwnPathForTheSameLevelAndSeed()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = CollectEverythingRoundGenerator.Create(level, new Random(seed));
                var maze = FingerMazeRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.Path, Is.EqualTo(maze.Path), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void PickupCountNeverExceedsTheLevelsRequestedCount()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = CollectEverythingRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.Pickups.Length, Is.LessThanOrEqualTo(RequestedByLevel[level - 1]), "level " + level + " seed " + seed);
                Assert.That(round.PickupFractions.Length, Is.EqualTo(round.Pickups.Length));
            }
        }

        // Every pickup must be one of the path's own points (never off the corridor) and never the start or
        // finish - those two are always clear so the child always has somewhere safe to begin and end.
        [Test]
        public void EveryPickupIsAnInteriorPathPointNeverTheStartOrFinish()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = CollectEverythingRoundGenerator.Create(level, new Random(seed));
                var start = round.Path[0];
                var finish = round.Path[round.Path.Length - 1];
                foreach (var pickup in round.Pickups)
                {
                    Assert.That(round.Path.Any(p => Distance(p, pickup) < 0.01f), Is.True, "level " + level + " seed " + seed);
                    Assert.Greater(Distance(pickup, start), 0.01f, "level " + level + " seed " + seed);
                    Assert.Greater(Distance(pickup, finish), 0.01f, "level " + level + " seed " + seed);
                }
            }
        }

        [Test]
        public void PickupsAreNeverDuplicated()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = CollectEverythingRoundGenerator.Create(level, new Random(seed));
                var distinctCount = round.Pickups.Select(p => (p.X, p.Y)).Distinct().Count();
                Assert.That(distinctCount, Is.EqualTo(round.Pickups.Length), "level " + level + " seed " + seed);
            }
        }

        // Fractions are strictly ascending (pickups are reported in the order they occur along the path) and
        // every one is strictly between 0 (the start) and 1 (the finish).
        [Test]
        public void PickupFractionsAreAscendingAndStrictlyBetweenZeroAndOne()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = CollectEverythingRoundGenerator.Create(level, new Random(seed));
                for (var i = 0; i < round.PickupFractions.Length; i++)
                {
                    Assert.That(round.PickupFractions[i], Is.InRange(0.001f, 0.999f), "level " + level + " seed " + seed);
                    if (i > 0) Assert.Greater(round.PickupFractions[i], round.PickupFractions[i - 1], "level " + level + " seed " + seed);
                }
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
