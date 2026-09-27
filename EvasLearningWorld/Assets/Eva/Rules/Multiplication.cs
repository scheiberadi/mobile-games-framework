using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public sealed class MultiplicationRound
    {
        public int Rows;
        public int Cols;
        public CountObject Object;
        public bool ShowObjects;
        public int[] Choices;

        public int Product => Rows * Cols;
    }

    // Multiplication (spec 4.2, Mathematics): TAP-THE-TARGET, the last game of School's Mathematics cluster.
    // Reuses Addition's own answer-tile mechanic; what differs is the problem itself - a Rows x Cols grid of
    // objects the child counts as a group of groups (reusing Count's own CountObject sprites), rather than two
    // separate groups added together. Per the plan's own wording ("introduce x notation late"), the object grid
    // stays up through every level except the last, where it drops to a bare `Rows x Cols = ?` equation - later
    // than Addition/Subtraction/Missing Number, which drop objects from the halfway level. Own difficulty
    // ladder (PlayerProgress.MultiplicationLevel/Buffer).
    public static class MultiplicationRoundGenerator
    {
        public const int RoundsPerSession = 5;

        // Index i = level (i + 1). Grid size grows with level, capped low enough that even the biggest object
        // grid (3 rows x 4 cols = 12 icons) stays countable and fits the problem field.
        private static readonly int[] RowsMaxByLevel = { 2, 2, 3, 3, 3, 4 };
        private static readonly int[] ColsMaxByLevel = { 2, 3, 3, 4, 4, 5 };
        private static readonly bool[] ShowObjectsByLevel = { true, true, true, true, true, false };
        private static readonly int[] TileCountByLevel = { 3, 4, 5, 5, 6, 6 };
        private const int OffByOneFromLevel = 3;

        public static MultiplicationRound Create(int level, Random rng, int? previousProduct)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var index = level - DifficultyLadder.MinLevel;
            var rowsMax = RowsMaxByLevel[index];
            var colsMax = ColsMaxByLevel[index];
            var tileCount = TileCountByLevel[index];

            int rows, cols, product;
            do
            {
                rows = rng.Next(1, rowsMax + 1);
                cols = rng.Next(1, colsMax + 1);
                product = rows * cols;
            } while (previousProduct.HasValue && product == previousProduct.Value && (rowsMax > 1 || colsMax > 1));

            // The classic multiplication slip - off by one in the total - guaranteed among the choices from
            // OffByOneFromLevel on, same shape as every other game in this family.
            var choices = new List<int> { product };
            if (level >= OffByOneFromLevel)
            {
                var under = product - 1;
                var over = product + 1;
                if (under >= 0 && !choices.Contains(under)) choices.Add(under);
                else if (!choices.Contains(over)) choices.Add(over);
            }

            var maxProduct = rowsMax * colsMax;
            var pool = new List<int>();
            for (var n = 0; n <= maxProduct; n++) if (!choices.Contains(n)) pool.Add(n);
            while (choices.Count < tileCount && pool.Count > 0)
            {
                var pick = rng.Next(0, pool.Count);
                choices.Add(pool[pick]);
                pool.RemoveAt(pick);
            }
            Shuffle(choices, rng);

            var obj = (CountObject)rng.Next(0, 4);
            return new MultiplicationRound
            {
                Rows = rows,
                Cols = cols,
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
