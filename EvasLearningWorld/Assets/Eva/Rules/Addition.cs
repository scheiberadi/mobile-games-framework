using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public sealed class AdditionRound
    {
        public int A;
        public int B;
        public int Sum;
        public CountObject Object;
        public bool ShowObjects;
        public int[] Choices;
    }

    // Addition (spec 4.2, Mathematics): TAP-THE-TARGET, reusing Number Hunt's own "guess the target numeral
    // among tiles" shape almost exactly - the target here is a computed sum rather than a spoken number. Low
    // levels also show two visual object groups (reusing Count's own CountObject/objects sprite set) so the
    // child can count the answer out rather than read the arithmetic; the objects are dropped at higher levels
    // for the bare `A + B = ?` the plan calls for, once the shape is familiar. Reuses the shared
    // DifficultyLadder (own level/buffer, see PlayerProgress.AdditionLevel/AdditionBuffer) and HelpLadder
    // exactly as every other game.
    public static class AdditionRoundGenerator
    {
        public const int RoundsPerSession = 5;

        // Index i = level (i + 1). Operand max grows 3 -> 9 across the ladder; objects are shown for the first
        // half of levels (kept small enough to lay out and count: at most 9 icons a group), then dropped for
        // bigger bare addition once the child has seen the objects' shape.
        private static readonly int[] OperandMaxByLevel = { 3, 5, 7, 9, 9, 9 };
        private static readonly bool[] ShowObjectsByLevel = { true, true, true, false, false, false };
        private static readonly int[] TileCountByLevel = { 3, 4, 5, 5, 6, 6 };
        private const int OffByOneFromLevel = 3;

        public static AdditionRound Create(int level, Random rng, int? previousSum)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var index = level - DifficultyLadder.MinLevel;
            var operandMax = OperandMaxByLevel[index];
            var tileCount = TileCountByLevel[index];

            int a, b, sum;
            do
            {
                a = rng.Next(1, operandMax + 1);
                b = rng.Next(1, operandMax + 1);
                sum = a + b;
            } while (previousSum.HasValue && sum == previousSum.Value && operandMax > 1);

            // The classic addition slip - off by one in either direction - guaranteed among the choices from
            // OffByOneFromLevel on, the same "guaranteed confusable" shape Number Hunt/Letter Hunt use for
            // their own distractor.
            var choices = new List<int> { sum };
            if (level >= OffByOneFromLevel)
            {
                var under = sum - 1;
                var over = sum + 1;
                if (under >= 0 && !choices.Contains(under)) choices.Add(under);
                else if (!choices.Contains(over)) choices.Add(over);
            }

            var maxSum = operandMax * 2;
            var pool = new List<int>();
            for (var n = 0; n <= maxSum; n++) if (!choices.Contains(n)) pool.Add(n);
            while (choices.Count < tileCount && pool.Count > 0)
            {
                var pick = rng.Next(0, pool.Count);
                choices.Add(pool[pick]);
                pool.RemoveAt(pick);
            }
            Shuffle(choices, rng);

            var obj = (CountObject)rng.Next(0, 4);
            return new AdditionRound
            {
                A = a,
                B = b,
                Sum = sum,
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
