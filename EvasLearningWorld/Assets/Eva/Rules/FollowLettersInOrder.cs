using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public sealed class FollowLettersInOrderRound
    {
        // The decorative corridor (reused as-is from Finger Maze - see the class comment below).
        public WorldPoint[] Path;

        // One checkpoint per non-start waypoint of Path (CheckpointPositions[i] == Path[i + 1]), each showing
        // CheckpointLetters[i]. The two arrays are parallel; a checkpoint's position on the corridor has no
        // relation to its letter.
        public WorldPoint[] CheckpointPositions;
        public char[] CheckpointLetters;

        // CheckpointLetters' values, sorted alphabetically - the order Eva names and the child must tap in.
        // Unlike numbers, a preliterate child cannot infer this order from the letters themselves, which is why
        // the screen has Eva speak it before every round (see FollowLettersInOrderScreen.RunRound).
        public char[] SortedLetters;
    }

    // Follow Letters in Order (Playground, spec 4.1): Literacy/Logic-spatial, the third NAVIGATION game. Same
    // shape as FollowNumbersInOrderRoundGenerator - reuses Finger Maze's grid/path generator for the corridor and
    // checkpoint count rather than a second grid-walk implementation - but drawn from a fixed 8-letter pool
    // (A-H, matching the 8-checkpoint ceiling every NAVIGATION game shares) instead of a widening numeral range:
    // letters carry no self-evident magnitude order the way numbers do, so there is no equivalent "range"
    // progression to widen, only checkpoint count.
    public static class FollowLettersInOrderRoundGenerator
    {
        public const int RoundsPerSession = 5;
        private const string LetterPool = "ABCDEFGH";

        public static FollowLettersInOrderRound Create(int level, Random rng)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var maze = FingerMazeRoundGenerator.Create(level, rng);
            var checkpointCount = maze.Path.Length - 1;

            var sorted = DrawDistinctAscending(checkpointCount, rng);
            var positions = new WorldPoint[checkpointCount];
            for (var i = 0; i < checkpointCount; i++) positions[i] = maze.Path[i + 1];

            var shuffled = (char[])sorted.Clone();
            Shuffle(shuffled, rng);

            return new FollowLettersInOrderRound
            {
                Path = maze.Path,
                CheckpointPositions = positions,
                CheckpointLetters = shuffled,
                SortedLetters = sorted,
            };
        }

        // `count` distinct letters from the pool, alphabetical - always possible since the pool (8) matches the
        // maximum checkpoint count every level can produce.
        private static char[] DrawDistinctAscending(int count, Random rng)
        {
            var pool = new List<char>(LetterPool.ToCharArray());
            var chosen = new List<char>(count);
            for (var i = 0; i < count; i++)
            {
                var index = rng.Next(pool.Count);
                chosen.Add(pool[index]);
                pool.RemoveAt(index);
            }
            chosen.Sort();
            return chosen.ToArray();
        }

        private static void Shuffle(char[] values, Random rng)
        {
            for (var i = values.Length - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
        }
    }
}
