using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public sealed class MissingNumberRound
    {
        public int A;
        public int Sum;
        public CountObject Object;
        public bool ShowObjects;
        public int[] Choices;

        // The value the child must find: A + Missing = Sum.
        public int Missing => Sum - A;
    }

    // Missing Number (spec 4.2, Mathematics): Addition's own shape read backwards - `A + ? = Sum` instead of
    // `A + B = ?`. Reuses Addition's exact TAP-THE-TARGET/answer-tile mechanic and level tables; the only
    // difference is which value is hidden. Own difficulty ladder (PlayerProgress.MissingNumberLevel/Buffer).
    public static class MissingNumberRoundGenerator
    {
        public const int RoundsPerSession = 5;

        // Same tables as Addition, by design: this is the same arithmetic fact family the child just learned,
        // asked with a different piece missing.
        private static readonly int[] OperandMaxByLevel = { 3, 5, 7, 9, 9, 9 };
        private static readonly bool[] ShowObjectsByLevel = { true, true, true, false, false, false };
        private static readonly int[] TileCountByLevel = { 3, 4, 5, 5, 6, 6 };
        private const int OffByOneFromLevel = 3;

        public static MissingNumberRound Create(int level, Random rng, int? previousSum)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var index = level - DifficultyLadder.MinLevel;
            var operandMax = OperandMaxByLevel[index];
            var tileCount = TileCountByLevel[index];

            int a, missing, sum;
            do
            {
                a = rng.Next(1, operandMax + 1);
                missing = rng.Next(1, operandMax + 1);
                sum = a + missing;
            } while (previousSum.HasValue && sum == previousSum.Value && operandMax > 1);

            // The classic missing-addend slip - answering with the total instead of the hidden piece - is
            // guaranteed among the choices whenever it differs from the answer (it always does, since Missing
            // is at least 1). From OffByOneFromLevel on, an off-by-one of the answer is guaranteed too, the
            // same "guaranteed confusable" shape every other TAP-THE-TARGET game in this family uses.
            var choices = new List<int> { missing };
            if (sum != missing && !choices.Contains(sum)) choices.Add(sum);
            if (level >= OffByOneFromLevel)
            {
                var under = missing - 1;
                var over = missing + 1;
                if (under >= 0 && !choices.Contains(under)) choices.Add(under);
                else if (!choices.Contains(over)) choices.Add(over);
            }

            var maxChoice = operandMax * 2;
            var pool = new List<int>();
            for (var n = 0; n <= maxChoice; n++) if (!choices.Contains(n)) pool.Add(n);
            while (choices.Count < tileCount && pool.Count > 0)
            {
                var pick = rng.Next(0, pool.Count);
                choices.Add(pool[pick]);
                pool.RemoveAt(pick);
            }
            Shuffle(choices, rng);

            var obj = (CountObject)rng.Next(0, 4);
            return new MissingNumberRound
            {
                A = a,
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
