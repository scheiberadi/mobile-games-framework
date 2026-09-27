using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public sealed class OneMoreOneLessRound
    {
        public int StartCount;
        // true = "one more" (the child adds one), false = "one less" (the child takes one away).
        public bool IsMore;
        public int TargetCount => IsMore ? StartCount + 1 : StartCount - 1;
        // Always Duck: the plan's own theming for this game is specifically "a duck into/out of a pond", not a
        // random object like Addition/Subtraction/Which Has More? use.
        public CountObject Object;
        // Levels 1-3: DRAG & DROP (drag a duck into/out of the pond). Levels 4-6: numeric tap answer among
        // tiles (Choices is set only then), the plan's own progression for this game.
        public bool UseDrag;
        public int[] Choices;
    }

    // One More / One Less (spec 4.2, Mathematics): a pond holding `StartCount` objects; the child either drags
    // one more object into the pond or drags one out, matching whichever of "one more"/"one less" this round
    // asks for. From level 4 the objects drop for a numeric question ("what is one more/less than N?") answered
    // among tiles, Addition/Subtraction's own answer-tile shape, with the classic mistake of answering with the
    // original number itself (StartCount) guaranteed among the choices. Reuses the shared DifficultyLadder (own
    // OneMoreOneLessLevel/OneMoreOneLessBuffer) and HelpLadder exactly as every other game.
    public static class OneMoreOneLessRoundGenerator
    {
        public const int RoundsPerSession = 5;

        // Index i = level (i + 1). StartCount starts at 2 (so "one less" never asks for a negative target) and
        // grows to 8; the drag mechanic is only for the first half of levels, per the plan.
        private static readonly int[] StartCountByLevel = { 2, 3, 5, 6, 7, 8 };
        private static readonly bool[] UseDragByLevel = { true, true, true, false, false, false };
        private static readonly int[] TileCountByLevel = { 3, 4, 5, 5, 6, 6 };

        public static OneMoreOneLessRound Create(int level, Random rng, bool? previousIsMore)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var index = level - DifficultyLadder.MinLevel;
            var startCount = StartCountByLevel[index];
            var useDrag = UseDragByLevel[index];

            bool isMore;
            do { isMore = rng.Next(0, 2) == 0; } while (previousIsMore.HasValue && isMore == previousIsMore.Value);

            var round = new OneMoreOneLessRound { StartCount = startCount, IsMore = isMore, Object = CountObject.Duck, UseDrag = useDrag };

            if (!useDrag)
            {
                var target = round.TargetCount;
                var tileCount = TileCountByLevel[index];

                // The classic slip for this game - answering with the original count instead of one more/less -
                // guaranteed among the choices, the same "guaranteed confusable" shape every tap-answer game
                // this session uses, just specific to this game's own mistake rather than a numeral shape.
                var choices = new List<int> { target };
                if (startCount != target && !choices.Contains(startCount)) choices.Add(startCount);

                var maxChoice = startCount + 2;
                var pool = new List<int>();
                for (var n = 0; n <= maxChoice; n++) if (!choices.Contains(n)) pool.Add(n);
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
