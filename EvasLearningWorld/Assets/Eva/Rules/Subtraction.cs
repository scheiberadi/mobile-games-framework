using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public sealed class SubtractionRound
    {
        public int A; // starting amount
        public int B; // amount taken away (always <= A, so the result is never negative)
        public int Difference;
        public CountObject Object;
        public bool ShowObjects;
        public int[] Choices;
    }

    // Subtraction (spec 4.2, Mathematics): Addition's closest sibling - same TAP-THE-TARGET shape, own
    // difficulty ladder (SubtractionLevel/SubtractionBuffer), same guaranteed off-by-one distractor from the
    // same level. The one difference from Addition is what the objects show: instead of two groups combining,
    // one group of A starts full and B of its own objects are visibly marked as taken away (AdditionScreen's
    // sibling, SubtractionScreen, dims them and overlays a small "taken away" mark), so the remaining, un-marked
    // objects are the answer to count - never negative, since B is always drawn no bigger than A.
    public static class SubtractionRoundGenerator
    {
        public const int RoundsPerSession = 5;

        // Index i = level (i + 1). A grows 3 -> 9 across the ladder; objects are shown for the first half of
        // levels, then dropped for the bare `A - B = ?` the plan calls for.
        private static readonly int[] StartMaxByLevel = { 3, 5, 7, 9, 9, 9 };
        private static readonly bool[] ShowObjectsByLevel = { true, true, true, false, false, false };
        private static readonly int[] TileCountByLevel = { 3, 4, 5, 5, 6, 6 };
        private const int OffByOneFromLevel = 3;

        public static SubtractionRound Create(int level, Random rng, int? previousDifference)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var index = level - DifficultyLadder.MinLevel;
            var startMax = StartMaxByLevel[index];
            var tileCount = TileCountByLevel[index];

            int a, b, difference;
            do
            {
                a = rng.Next(1, startMax + 1);
                b = rng.Next(0, a + 1);
                difference = a - b;
            } while (previousDifference.HasValue && difference == previousDifference.Value && startMax > 1);

            // The classic subtraction slip - off by one in either direction - guaranteed among the choices from
            // OffByOneFromLevel on, the same "guaranteed confusable" shape Addition/Number Hunt/Letter Hunt use.
            var choices = new List<int> { difference };
            if (level >= OffByOneFromLevel)
            {
                var under = difference - 1;
                var over = difference + 1;
                if (under >= 0 && !choices.Contains(under)) choices.Add(under);
                else if (!choices.Contains(over)) choices.Add(over);
            }

            var pool = new List<int>();
            for (var n = 0; n <= startMax; n++) if (!choices.Contains(n)) pool.Add(n);
            while (choices.Count < tileCount && pool.Count > 0)
            {
                var pick = rng.Next(0, pool.Count);
                choices.Add(pool[pick]);
                pool.RemoveAt(pick);
            }
            Shuffle(choices, rng);

            var obj = (CountObject)rng.Next(0, 4);
            return new SubtractionRound
            {
                A = a,
                B = b,
                Difference = difference,
                Object = obj,
                ShowObjects = ShowObjectsByLevel[index],
                Choices = choices.ToArray(),
            };
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
