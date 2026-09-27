using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public sealed class NumberHuntRound
    {
        public int Target;
        public int[] Choices;
    }

    // Number hunt (spec 4.8): Mathematics, number recognition, about 4-6. Eva speaks a number and the child taps
    // the matching numeral tile among several on the board. Reuses the shared DifficultyLadder (Rules/Counting.cs,
    // levels 1-6, same rolling-window rule) for its own, independent level and buffer (see PlayerProgress).
    // Level/tile-count data lives in one place here: numbers grow from 1-5 to 1-20, tiles from 3 to 6, and from
    // level 5 a guaranteed visually-confusable distractor (6/9, 2/5, 1/7) appears alongside the target when one
    // exists in range.
    public static class NumberHuntRoundGenerator
    {
        public const int RoundsPerSession = 5;

        // Index i = level (i + 1).
        private static readonly int[] MaxNumberByLevel = { 5, 10, 10, 20, 20, 20 };
        private static readonly int[] TileCountByLevel = { 3, 4, 5, 5, 6, 6 };
        private const int ConfusablesFromLevel = 5;

        // Single-digit numerals that read as each other when flipped or mirrored. Two-digit numbers have no
        // guaranteed partner (kept simple: the child still meets bigger numbers, just without this guarantee).
        private static readonly Dictionary<int, int> ConfusablePartner = new Dictionary<int, int>
        {
            { 6, 9 }, { 9, 6 }, { 2, 5 }, { 5, 2 }, { 1, 7 }, { 7, 1 },
        };

        public static NumberHuntRound Create(int level, Random rng, int? previousTarget)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var index = level - DifficultyLadder.MinLevel;
            var max = MaxNumberByLevel[index];
            var tileCount = TileCountByLevel[index];

            int target;
            do { target = rng.Next(1, max + 1); } while (previousTarget.HasValue && target == previousTarget.Value && max > 1);

            // Guarantee the target once, its confusable partner (from this level up) when one is in range, then
            // fill the rest from the remaining numbers without replacement, and shuffle - never sorted, so the
            // target's tile position and its partner's are never predictable.
            var choices = new List<int> { target };
            if (level >= ConfusablesFromLevel && ConfusablePartner.TryGetValue(target, out var partner) && partner <= max)
                choices.Add(partner);

            var pool = new List<int>(max);
            for (var n = 1; n <= max; n++) if (!choices.Contains(n)) pool.Add(n);
            while (choices.Count < tileCount)
            {
                var pick = rng.Next(0, pool.Count);
                choices.Add(pool[pick]);
                pool.RemoveAt(pick);
            }
            Shuffle(choices, rng);

            return new NumberHuntRound { Target = target, Choices = choices.ToArray() };
        }

        private static void Shuffle(List<int> list, Random rng)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
