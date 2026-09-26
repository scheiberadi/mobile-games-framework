using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public sealed class FollowNumbersInOrderRound
    {
        // The decorative corridor (reused as-is from Finger Maze - see the class comment below).
        public WorldPoint[] Path;

        // One checkpoint per non-start waypoint of Path (CheckpointPositions[i] == Path[i + 1]), each showing
        // CheckpointNumbers[i]. The two arrays are parallel; a checkpoint's position on the corridor has no
        // relation to its number, so the child must read the numbers, not just follow the path in order.
        public WorldPoint[] CheckpointPositions;
        public int[] CheckpointNumbers;

        // CheckpointNumbers' values, sorted ascending - the order the child must tap them in.
        public int[] SortedNumbers;
    }

    // Follow Numbers in Order (Playground, spec 4.1): Mathematics/Logic-spatial, the second NAVIGATION game.
    // Reuses Finger Maze's grid/path generator for its corridor and checkpoint count (FingerMazeRoundGenerator's
    // own CellCountByLevel table becomes "checkpoint count" here, one fewer than the path's waypoint count since
    // the start itself isn't a checkpoint) rather than a second grid-walk implementation; only what sits on the
    // path - numbered checkpoints instead of a dragged character - is specific to this game. Progression:
    // checkpoint count (via the reused table) and numeral range (how far apart the numbers can be, so higher
    // levels need real magnitude comparison instead of just "next integer").
    public static class FollowNumbersInOrderRoundGenerator
    {
        public const int RoundsPerSession = 5;

        // Index i = level (i + 1). Matches num_1..num_20 (Resources/Voice/voice-lines.txt), already authored.
        private static readonly int[] NumeralRangeMaxByLevel = { 9, 9, 14, 14, 20, 20 };

        public static FollowNumbersInOrderRound Create(int level, Random rng)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var maze = FingerMazeRoundGenerator.Create(level, rng);
            var checkpointCount = maze.Path.Length - 1;
            var max = NumeralRangeMaxByLevel[level - DifficultyLadder.MinLevel];

            var sorted = DrawDistinctAscending(checkpointCount, max, rng);
            var positions = new WorldPoint[checkpointCount];
            for (var i = 0; i < checkpointCount; i++) positions[i] = maze.Path[i + 1];

            var shuffled = (int[])sorted.Clone();
            Shuffle(shuffled, rng);

            return new FollowNumbersInOrderRound
            {
                Path = maze.Path,
                CheckpointPositions = positions,
                CheckpointNumbers = shuffled,
                SortedNumbers = sorted,
            };
        }

        // `count` distinct integers from 1..max, ascending - always possible since every level's max comfortably
        // exceeds its own checkpoint count (see the level tables above).
        private static int[] DrawDistinctAscending(int count, int max, Random rng)
        {
            var pool = new List<int>(max);
            for (var n = 1; n <= max; n++) pool.Add(n);
            var chosen = new List<int>(count);
            for (var i = 0; i < count; i++)
            {
                var index = rng.Next(pool.Count);
                chosen.Add(pool[index]);
                pool.RemoveAt(index);
            }
            chosen.Sort();
            return chosen.ToArray();
        }

        private static void Shuffle(int[] values, Random rng)
        {
            for (var i = values.Length - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
        }
    }
}
