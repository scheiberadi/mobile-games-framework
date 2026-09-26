using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public sealed class LetterHuntRound
    {
        public char Target;
        public char[] Choices;
    }

    // Letter Hunt (M4 School, Literacy, letter recognition): reuses Number Hunt's own shape almost exactly
    // (Rules/NumberHunt.cs) - Eva speaks a letter, the child taps the matching tile among several - except the
    // target is spoken, never shown as text (no on-screen "find B" caption), and every tile shows its letter as
    // a sprite ("letters/<lowercase>"), never TMP_Text: letters carry no order a preliterate child can infer by
    // sight the way OnlyDigitsAreShownExceptInTheSpeechBubble's exemption assumes digits do, the same reasoning
    // Follow Letters in Order's checkpoints already established (Rules/FollowLettersInOrder.cs). Reuses the
    // shared DifficultyLadder for its own, independent level and buffer (see PlayerProgress).
    public static class LetterHuntRoundGenerator
    {
        public const int RoundsPerSession = 5;

        // Index i = level (i + 1): the alphabet pool the target and distractors draw from widens with level,
        // same shape as Number Hunt's own MaxNumberByLevel/TileCountByLevel.
        private static readonly int[] PoolSizeByLevel = { 5, 10, 15, 20, 26, 26 };
        private static readonly int[] TileCountByLevel = { 3, 4, 5, 5, 6, 6 };
        private const int ConfusablesFromLevel = 5;

        // Lowercase letters that read as each other by shape (mirrored/rotated forms a preliterate child
        // commonly confuses) - the same guaranteed-distractor idea as Number Hunt's numeral confusables.
        private static readonly Dictionary<char, char> ConfusablePartner = new Dictionary<char, char>
        {
            { 'b', 'd' }, { 'd', 'b' }, { 'p', 'q' }, { 'q', 'p' }, { 'm', 'w' }, { 'w', 'm' }, { 'n', 'u' }, { 'u', 'n' },
        };

        public static LetterHuntRound Create(int level, Random rng, char? previousTarget)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var index = level - DifficultyLadder.MinLevel;
            var poolSize = PoolSizeByLevel[index];
            var tileCount = TileCountByLevel[index];

            char target;
            do { target = (char)('a' + rng.Next(poolSize)); } while (previousTarget.HasValue && target == previousTarget.Value && poolSize > 1);

            // Guarantee the target once, its confusable partner (from this level up) when one is in range, then
            // fill the rest from the remaining letters without replacement, and shuffle - same rule as Number
            // Hunt's own choice-building, just over letters instead of numbers.
            var choices = new List<char> { target };
            if (level >= ConfusablesFromLevel && ConfusablePartner.TryGetValue(target, out var partner) && partner - 'a' < poolSize)
                choices.Add(partner);

            var pool = new List<char>(poolSize);
            for (var i = 0; i < poolSize; i++)
            {
                var c = (char)('a' + i);
                if (!choices.Contains(c)) pool.Add(c);
            }
            while (choices.Count < tileCount)
            {
                var pick = rng.Next(0, pool.Count);
                choices.Add(pool[pick]);
                pool.RemoveAt(pick);
            }
            Shuffle(choices, rng);

            return new LetterHuntRound { Target = target, Choices = choices.ToArray() };
        }

        private static void Shuffle(List<char> list, Random rng)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
