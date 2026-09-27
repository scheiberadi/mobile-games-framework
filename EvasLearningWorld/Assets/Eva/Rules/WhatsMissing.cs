using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    public sealed class WhatsMissingRound
    {
        // The full set, in display order, as shown during the exposure beat.
        public string[] Shown;
        // Which slot of Shown goes blank after the exposure beat.
        public int MissingIndex;
        public string Missing => Shown[MissingIndex];
        public string[] Choices;
    }

    // What's Missing? (Playground, spec 4.1): Logic/spatial, memory (sequence/set recall). A group of shapes is
    // shown, then one is taken away; the child taps which one, among choices, is gone. Reuses the same shape
    // symbols as Pattern Completion (Rules/PatternCompletion.cs) for its content, and the shared DifficultyLadder
    // for its own, independent level and buffer (see PlayerProgress.WhatsMissingLevel/Buffer). Progression per the
    // plan: set size and exposure time - both get harder together as level rises.
    public static class WhatsMissingRoundGenerator
    {
        public const int RoundsPerSession = 5;

        // Index i = level (i + 1).
        private static readonly int[] SetSizeByLevel = { 3, 3, 4, 4, 5, 5 };
        private static readonly float[] ExposureSecondsByLevel = { 3.0f, 2.5f, 2.25f, 2.0f, 1.75f, 1.5f };
        private static readonly int[] ChoiceCountByLevel = { 3, 3, 4, 4, 4, 4 };

        public static float ExposureSecondsFor(int level)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            return ExposureSecondsByLevel[level - DifficultyLadder.MinLevel];
        }

        public static WhatsMissingRound Create(int level, Random rng)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var index = level - DifficultyLadder.MinLevel;
            var setSize = SetSizeByLevel[index];
            var choiceCount = ChoiceCountByLevel[index];

            var shown = Shuffled(PatternCompletionRoundGenerator.Symbols, rng).GetRange(0, setSize).ToArray();
            var missingIndex = rng.Next(0, setSize);
            var missing = shown[missingIndex];

            // Distractors are drawn from every symbol but the missing one - including symbols still visibly
            // present in the round, which is deliberate: a still-present symbol is a legitimate wrong choice
            // (the child must recall which slot actually went blank, not just guess an unseen symbol).
            var choices = new List<string> { missing };
            var distractorPool = new List<string>(PatternCompletionRoundGenerator.Symbols);
            distractorPool.Remove(missing);
            Shuffle(distractorPool, rng);
            for (var i = 0; choices.Count < choiceCount && i < distractorPool.Count; i++) choices.Add(distractorPool[i]);
            Shuffle(choices, rng);

            return new WhatsMissingRound { Shown = shown, MissingIndex = missingIndex, Choices = choices.ToArray() };
        }

        private static List<string> Shuffled(string[] source, Random rng)
        {
            var list = new List<string>(source);
            Shuffle(list, rng);
            return list;
        }

        private static void Shuffle(List<string> list, Random rng)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
