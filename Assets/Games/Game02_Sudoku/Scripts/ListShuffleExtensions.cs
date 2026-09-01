using System;
using System.Collections.Generic;

namespace Game02_Sudoku
{
    internal static class ListShuffleExtensions
    {
        public static void Shuffle<T>(this IList<T> list, Random random)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
