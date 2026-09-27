using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public sealed class WhichHasMoreRound
    {
        public int A;
        public int B;
        public CountObject Object;
        // Levels 1-3: TAP-THE-TARGET on the bigger group directly (Choices is null). Levels 4-6: the plan's own
        // "later, 'how many more' asks for the numeric difference" extension - both groups are shown but the
        // child taps the difference among numeral tiles instead (Choices holds those tile values).
        public bool AskDifference;
        public int[] Choices;
    }

    // Which Has More? (spec 4.2, Mathematics): two visual object groups, the child taps the bigger one at low
    // levels; from level 4 the question becomes "how many more" and the child taps the numeric difference among
    // tiles instead (Addition/Subtraction's own answer-tile shape). Reuses the shared DifficultyLadder (own
    // WhichHasMoreLevel/WhichHasMoreBuffer) and HelpLadder exactly as every other game.
    public static class WhichHasMoreRoundGenerator
    {
        public const int RoundsPerSession = 5;

        // Index i = level (i + 1). Group sizes grow 3 -> 9; the "how many more" numeric-difference question
        // only starts once the child has already learned to compare groups by sight.
        private static readonly int[] MaxByLevel = { 3, 5, 7, 9, 9, 9 };
        private static readonly bool[] AskDifferenceByLevel = { false, false, false, true, true, true };
        private static readonly int[] TileCountByLevel = { 3, 4, 5, 5, 6, 6 };
        private const int OffByOneFromLevel = 5;

        public static WhichHasMoreRound Create(int level, Random rng, int? previousDifference)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var index = level - DifficultyLadder.MinLevel;
            var max = MaxByLevel[index];
            var askDifference = AskDifferenceByLevel[index];

            int a, b, difference;
            do
            {
                a = rng.Next(1, max + 1);
                do { b = rng.Next(1, max + 1); } while (b == a);
                difference = Math.Abs(a - b);
            } while (askDifference && previousDifference.HasValue && difference == previousDifference.Value && max > 2);

            var obj = (CountObject)rng.Next(0, 4);
            var round = new WhichHasMoreRound { A = a, B = b, Object = obj, AskDifference = askDifference };

            if (askDifference)
            {
                var tileCount = TileCountByLevel[index];
                var choices = new List<int> { difference };
                if (level >= OffByOneFromLevel)
                {
                    var under = difference - 1;
                    var over = difference + 1;
                    if (under >= 1 && !choices.Contains(under)) choices.Add(under);
                    else if (!choices.Contains(over)) choices.Add(over);
                }

                var pool = new List<int>();
                for (var n = 1; n <= max; n++) if (!choices.Contains(n)) pool.Add(n);
                while (choices.Count < tileCount && pool.Count > 0)
                {
                    var pick = rng.Next(0, pool.Count);
                    choices.Add(pool[pick]);
                    pool.RemoveAt(pick);
                }
                Shuffle(choices, rng);
                round.Choices = choices.ToArray();
            }

            return round;
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
