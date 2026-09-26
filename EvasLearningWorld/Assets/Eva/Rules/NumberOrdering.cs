using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public sealed class NumberOrderingRound
    {
        // Shuffled tile values (what's shown on the board, in this order).
        public int[] Choices;
        // Choices' values sorted the direction the child must tap them in this round - ascending at low levels,
        // descending from level 5 (spec: "ascending -> descending").
        public int[] TargetOrder;
        public bool Descending;
    }

    // Number Ordering (spec 4.2, Mathematics): N numeral tiles shown scrambled (Number Hunt/Addition's own
    // answer-tile shape and positions, read NumberHuntScreen first), the child taps them in order - ascending at
    // low levels, descending from level 5. Reuses Follow Numbers in Order's own numeral range table (Playground,
    // Rules/FollowNumbersInOrder.cs) since it already has voice lines num_1..num_20 authored; unlike that game
    // there is no maze corridor here, just a static tile grid, so a wrong tap never eliminates a tile (it may
    // still be due later in the sequence) - only wobbles it, the same "mistake never removes a still-valid
    // choice" rule Follow Numbers/Letters in Order already established. Reuses the shared DifficultyLadder (own
    // NumberOrderingLevel/NumberOrderingBuffer) and HelpLadder exactly as every other game.
    public static class NumberOrderingRoundGenerator
    {
        public const int RoundsPerSession = 5;

        // Index i = level (i + 1). Matches num_1..num_20 (Resources/Voice/voice-lines.txt), already authored by
        // Follow Numbers in Order.
        private static readonly int[] TileCountByLevel = { 3, 4, 5, 5, 6, 6 };
        private static readonly int[] NumeralRangeMaxByLevel = { 9, 9, 14, 14, 20, 20 };
        private const int DescendingFromLevel = 5;

        public static NumberOrderingRound Create(int level, Random rng)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var index = level - DifficultyLadder.MinLevel;
            var tileCount = TileCountByLevel[index];
            var max = NumeralRangeMaxByLevel[index];
            var descending = level >= DescendingFromLevel;

            var sorted = DrawDistinctAscending(tileCount, max, rng);
            var choices = (int[])sorted.Clone();
            Shuffle(choices, rng);

            var targetOrder = (int[])sorted.Clone();
            if (descending) Array.Reverse(targetOrder);

            return new NumberOrderingRound { Choices = choices, TargetOrder = targetOrder, Descending = descending };
        }

        // `count` distinct integers from 1..max, ascending - always possible since every level's max comfortably
        // exceeds its own tile count (see the level tables above).
        private static int[] DrawDistinctAscending(int count, int max, Random rng)
        {
            var pool = new List<int>(max);
            for (var n = 1; n <= max; n++) pool.Add(n);
            var chosen = new List<int>(count);
            for (var i = 0; i < count; i++)
            {
                var pick = rng.Next(pool.Count);
                chosen.Add(pool[pick]);
                pool.RemoveAt(pick);
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
