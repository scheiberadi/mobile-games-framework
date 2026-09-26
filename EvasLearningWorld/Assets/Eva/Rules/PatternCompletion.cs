using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public sealed class PatternCompletionRound
    {
        // The shown tiles, in order, ending right before the blank the child must fill.
        public string[] Sequence;
        public string Answer;
        public string[] Choices;
    }

    // Pattern Completion (Playground, spec 4.1): Logic/spatial, patterns. Eva shows a repeating sequence with one
    // blank at the end; the child taps the tile among several choices that continues it. Reuses the shared
    // DifficultyLadder (Rules/Counting.cs, levels 1-6, same rolling-window rule) for its own, independent level and
    // buffer (see PlayerProgress.PatternCompletionLevel/Buffer). Progression per the plan: AB -> ABB -> ABC ->
    // longer/less-obvious repeats. The repeat length (period) grows with level and always uses distinct symbols per
    // unit (a simplification of the plan's illustrative ABB, which repeats a symbol within the unit - same task
    // shape, one fewer authored special case); from level 2 the shown length no longer lands on an exact multiple
    // of the period, so the blank can't be guessed by just counting whole repeats. Shown length is capped at 8 tiles
    // (MaxShownLength) so the row always fits on screen (see PatternCompletionScreen).
    public static class PatternCompletionRoundGenerator
    {
        public const int RoundsPerSession = 5;
        public const int MaxShownLength = 8;

        // The tiles a pattern can be built from (sprite key suffix "pattern/shape_<lowercase>"; see
        // PatternCompletionScreen). Five is enough distinct shapes for every level's period plus spare distractors.
        public static readonly string[] Symbols = { "A", "B", "C", "D", "E" };

        // Index i = level (i + 1).
        private static readonly int[] PeriodByLevel = { 2, 2, 3, 3, 4, 4 };
        private static readonly int[] ExtraByLevel = { 0, 1, 0, 1, 0, 1 };
        private static readonly int[] ChoiceCountByLevel = { 3, 3, 4, 4, 4, 4 };

        public static PatternCompletionRound Create(int level, Random rng)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var index = level - DifficultyLadder.MinLevel;
            var period = PeriodByLevel[index];
            var shownLength = Math.Min(MaxShownLength, period * 2 + ExtraByLevel[index]);
            var choiceCount = ChoiceCountByLevel[index];

            var unit = Shuffled(Symbols, rng).GetRange(0, period);
            var sequence = new string[shownLength];
            for (var i = 0; i < shownLength; i++) sequence[i] = unit[i % period];
            var answer = unit[shownLength % period];

            var choices = new List<string> { answer };
            var distractorPool = new List<string>(Symbols);
            distractorPool.Remove(answer);
            Shuffle(distractorPool, rng);
            for (var i = 0; choices.Count < choiceCount && i < distractorPool.Count; i++) choices.Add(distractorPool[i]);
            Shuffle(choices, rng);

            return new PatternCompletionRound { Sequence = sequence, Answer = answer, Choices = choices.ToArray() };
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
