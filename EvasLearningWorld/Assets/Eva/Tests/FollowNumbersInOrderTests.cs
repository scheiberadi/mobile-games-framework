using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class FollowNumbersInOrderTests
    {
        [Test]
        public void CheckpointCountMatchesFingerMazesOwnPathLengthMinusOne()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var mazeSeed = new Random(seed);
                var round = FollowNumbersInOrderRoundGenerator.Create(level, new Random(seed));
                var maze = FingerMazeRoundGenerator.Create(level, mazeSeed);
                Assert.That(round.CheckpointPositions.Length, Is.EqualTo(maze.Path.Length - 1), "level " + level + " seed " + seed);
                Assert.That(round.CheckpointNumbers.Length, Is.EqualTo(round.CheckpointPositions.Length));
                Assert.That(round.SortedNumbers.Length, Is.EqualTo(round.CheckpointPositions.Length));
            }
        }

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => FollowNumbersInOrderRoundGenerator.Create(0, new Random(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => FollowNumbersInOrderRoundGenerator.Create(7, new Random(1)));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = FollowNumbersInOrderRoundGenerator.Create(4, new Random(7));
            var b = FollowNumbersInOrderRoundGenerator.Create(4, new Random(7));
            Assert.That(a.CheckpointNumbers, Is.EqualTo(b.CheckpointNumbers));
            Assert.That(a.SortedNumbers, Is.EqualTo(b.SortedNumbers));
        }

        [Test]
        public void RoundsPerSessionMatchesTheSharedFiveRoundWindow()
        {
            Assert.That(FollowNumbersInOrderRoundGenerator.RoundsPerSession, Is.EqualTo(5));
        }

        [Test]
        public void CheckpointNumbersAreDistinctAndSortedNumbersIsThemInAscendingOrder()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = FollowNumbersInOrderRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.CheckpointNumbers.Distinct().Count(), Is.EqualTo(round.CheckpointNumbers.Length), "level " + level + " seed " + seed);

                var expectedSorted = (int[])round.CheckpointNumbers.Clone();
                Array.Sort(expectedSorted);
                Assert.That(round.SortedNumbers, Is.EqualTo(expectedSorted), "level " + level + " seed " + seed);
                for (var i = 1; i < round.SortedNumbers.Length; i++)
                    Assert.Less(round.SortedNumbers[i - 1], round.SortedNumbers[i]);
            }
        }

        // Levels 1-2 draw from 1-9, 3-4 from 1-14, 5-6 from 1-20 (matching the num_1..num_20 voice lines already
        // authored) - the "numeral range" half of this game's progression.
        [Test]
        public void NumeralRangeWidensWithLevel()
        {
            var maxByLevel = new[] { 9, 9, 14, 14, 20, 20 };
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = FollowNumbersInOrderRoundGenerator.Create(level, new Random(seed));
                foreach (var number in round.CheckpointNumbers)
                    Assert.That(number, Is.InRange(1, maxByLevel[level - 1]), "level " + level + " seed " + seed);
            }
        }
    }
}
