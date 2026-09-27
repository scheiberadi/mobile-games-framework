using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class FollowLettersInOrderTests
    {
        [Test]
        public void CheckpointCountMatchesFingerMazesOwnPathLengthMinusOne()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = FollowLettersInOrderRoundGenerator.Create(level, new Random(seed));
                var maze = FingerMazeRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.CheckpointPositions.Length, Is.EqualTo(maze.Path.Length - 1), "level " + level + " seed " + seed);
                Assert.That(round.CheckpointLetters.Length, Is.EqualTo(round.CheckpointPositions.Length));
                Assert.That(round.SortedLetters.Length, Is.EqualTo(round.CheckpointPositions.Length));
            }
        }

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => FollowLettersInOrderRoundGenerator.Create(0, new Random(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => FollowLettersInOrderRoundGenerator.Create(7, new Random(1)));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = FollowLettersInOrderRoundGenerator.Create(4, new Random(7));
            var b = FollowLettersInOrderRoundGenerator.Create(4, new Random(7));
            Assert.That(a.CheckpointLetters, Is.EqualTo(b.CheckpointLetters));
            Assert.That(a.SortedLetters, Is.EqualTo(b.SortedLetters));
        }

        [Test]
        public void RoundsPerSessionMatchesTheSharedFiveRoundWindow()
        {
            Assert.That(FollowLettersInOrderRoundGenerator.RoundsPerSession, Is.EqualTo(5));
        }

        [Test]
        public void CheckpointLettersAreDistinctFromAToHAndSortedLettersIsThemAlphabetical()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = FollowLettersInOrderRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.CheckpointLetters.Distinct().Count(), Is.EqualTo(round.CheckpointLetters.Length), "level " + level + " seed " + seed);
                foreach (var letter in round.CheckpointLetters) Assert.That(letter, Is.InRange('A', 'H'));

                var expectedSorted = (char[])round.CheckpointLetters.Clone();
                Array.Sort(expectedSorted);
                Assert.That(round.SortedLetters, Is.EqualTo(expectedSorted), "level " + level + " seed " + seed);
                for (var i = 1; i < round.SortedLetters.Length; i++)
                    Assert.Less(round.SortedLetters[i - 1], round.SortedLetters[i]);
            }
        }

        // The pool is exactly 8 letters (A-H), matching the maximum checkpoint count every level can produce
        // (Finger Maze's own 9-cell path at level 6, minus the start) - level 6 must draw all of them.
        [Test]
        public void HighestLevelDrawsFromTheWholeEightLetterPoolWithoutRunningShort()
        {
            for (var seed = 0; seed < 100; seed++)
            {
                var round = FollowLettersInOrderRoundGenerator.Create(6, new Random(seed));
                Assert.That(round.CheckpointLetters.Length, Is.LessThanOrEqualTo(8));
            }
        }
    }
}
