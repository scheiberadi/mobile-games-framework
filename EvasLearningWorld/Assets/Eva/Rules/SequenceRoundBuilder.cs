using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    // A shared SEQUENCE round shape (M4.5 Science Lab's Plant Growth, docs/kids-games/full-catalogue-plan.md
    // "7. Science Lab" - first game to need the SEQUENCE mechanic the plan's reuse table lists but nothing had
    // built yet). Generalizes Rules/NumberOrdering.cs's own shape (numeral tiles shown scrambled, tapped back in
    // order) to an arbitrary sprite-backed item list instead of numerals, the same way MatchRoundBuilder
    // generalized WordToImageScreen's tap-a-picture shape for Zoo & Farm. `stages` is already in its correct
    // growth/sequence order; a round takes its first `count` stages (a prefix keeps that order for free) and
    // shuffles them for display.
    public sealed class SequenceRound
    {
        // Shuffled tile ids (what's shown on the board, in this order).
        public string[] Choices;
        // Choices' ids in the order the child must tap them. Choices may hold extra tiles that are never targets (decoys).
        public string[] TargetOrder;
        // Optional voice keys spoken after the prompt, one per target in order (Follow Instructions says what to do).
        public string[] SpokenKeys;
    }

    public static class SequenceRoundBuilder
    {
        public static SequenceRound Build(IReadOnlyList<string> stages, int level, Random rng, int[] stageCountByLevel)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var index = level - DifficultyLadder.MinLevel;
            var count = Math.Min(stageCountByLevel[index], stages.Count);

            var targetOrder = stages.Take(count).ToArray();
            var choices = (string[])targetOrder.Clone();
            Shuffle(choices, rng);

            return new SequenceRound { Choices = choices, TargetOrder = targetOrder };
        }

        private static void Shuffle(string[] values, Random rng)
        {
            for (var i = values.Length - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
        }
    }
}
