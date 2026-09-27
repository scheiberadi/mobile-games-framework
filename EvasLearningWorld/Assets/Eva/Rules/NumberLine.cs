using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public sealed class NumberLineRound
    {
        // The line always runs 0..LineMax; Eva's character starts at Start and hops Hops spaces forward
        // (Forward true) or back (Forward false).
        public int LineMax;
        public int Start;
        public int Hops;
        public bool Forward;
        public int[] Choices;

        public int Landing => Forward ? Start + Hops : Start - Hops;
    }

    // Number Line (spec 4.2, Mathematics): a new small mechanic - Eva's character hops along a number line,
    // child taps the landing number. Reuses the same answer-tile TAP-THE-TARGET shape every other Mathematics
    // game in this family uses (Addition/Subtraction/Missing Number's own numeral-tile grid) rather than making
    // every position on the line itself a tap target: a line long enough for level 6 (0..20) would need far
    // more 240-unit tap targets than the screen can hold side by side, the same ceiling Jigsaw/Tangram's
    // cramped piece grids hit. The line itself (NumberLineScreen) is a non-interactive visual aid - digit
    // labels under a few dots, which the no-reading audit allows since digits are the one text form it permits
    // anywhere - while the actual answer is picked from a proper-sized tile grid below it, same shape as
    // Addition. Own difficulty ladder (PlayerProgress.NumberLineLevel/Buffer).
    public static class NumberLineRoundGenerator
    {
        public const int RoundsPerSession = 5;

        // Index i = level (i + 1). The line and hop distance both grow with level; backward hops (a harder,
        // less intuitive case than always-forward) are introduced only from BackwardFromLevel.
        private static readonly int[] LineMaxByLevel = { 5, 10, 10, 15, 20, 20 };
        private static readonly int[] HopsMaxByLevel = { 1, 2, 3, 3, 4, 5 };
        private static readonly int[] TileCountByLevel = { 3, 4, 5, 5, 6, 6 };
        private const int BackwardFromLevel = 4;
        private const int OffByOneFromLevel = 3;

        public static NumberLineRound Create(int level, Random rng, int? previousLanding)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var index = level - DifficultyLadder.MinLevel;
            var lineMax = LineMaxByLevel[index];
            var hopsMax = HopsMaxByLevel[index];
            var tileCount = TileCountByLevel[index];
            var allowBackward = level >= BackwardFromLevel;

            int start, hops, landing;
            bool forward;
            do
            {
                hops = rng.Next(1, hopsMax + 1);
                forward = !allowBackward || rng.Next(0, 2) == 0;
                start = forward ? rng.Next(0, lineMax - hops + 1) : rng.Next(hops, lineMax + 1);
                landing = forward ? start + hops : start - hops;
            } while (previousLanding.HasValue && landing == previousLanding.Value && lineMax > hopsMax);

            // The classic off-by-one slip - landing one space short or long - guaranteed among the choices
            // from OffByOneFromLevel on, same shape as every other game in this family.
            var choices = new List<int> { landing };
            if (level >= OffByOneFromLevel)
            {
                var under = landing - 1;
                var over = landing + 1;
                if (under >= 0 && !choices.Contains(under)) choices.Add(under);
                else if (over <= lineMax && !choices.Contains(over)) choices.Add(over);
            }

            var pool = new List<int>();
            for (var n = 0; n <= lineMax; n++) if (!choices.Contains(n)) pool.Add(n);
            while (choices.Count < tileCount && pool.Count > 0)
            {
                var pick = rng.Next(0, pool.Count);
                choices.Add(pool[pick]);
                pool.RemoveAt(pick);
            }
            Shuffle(choices, rng);

            return new NumberLineRound
            {
                LineMax = lineMax,
                Start = start,
                Hops = hops,
                Forward = forward,
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
