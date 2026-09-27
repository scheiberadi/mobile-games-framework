using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public sealed class UppercaseToLowercaseRound
    {
        // The letter itself (lowercase value); the target tile shows its uppercase form, the choice tiles show
        // lowercase forms - see the screen for the "letters/upper_<letter>" vs "letters/<letter>" sprite split.
        public char Target;
        public char[] Choices;
        public int CorrectIndex;
    }

    // Uppercase to Lowercase (spec 4.2, Literacy): the first Literacy game beyond Letter Hunt, and the one that
    // defines the MATCH presenter every other Literacy game in this cluster reuses. An uppercase letter is
    // shown, the child taps its lowercase match among choices - reuses Item to Shadow's own MATCH shell
    // (Playground) almost unchanged: one decorative target tile above a row of up to 4 tappable choice tiles.
    // Reuses Letter Hunt's own alphabet-pool/confusable-partner tables (Rules/LetterHunt.cs) rather than
    // inventing new ones, since it's the same alphabet and the same shape-confusion pairs apply to matching as
    // much as to finding. Own difficulty ladder (PlayerProgress.UppercaseToLowercaseLevel/Buffer).
    public static class UppercaseToLowercaseRoundGenerator
    {
        public const int RoundsPerSession = 5;

        // Index i = level (i + 1). Same alphabet-pool growth as Letter Hunt; choice count follows Item to
        // Shadow's own MATCH shape (max 4 tiles) rather than Letter Hunt's up-to-6 TAP-THE-TARGET grid.
        private static readonly int[] PoolSizeByLevel = { 5, 10, 15, 20, 26, 26 };
        private static readonly int[] ChoiceCountByLevel = { 3, 3, 4, 4, 4, 4 };
        private const int ConfusablesFromLevel = 5;

        // Same shape-confusion pairs as Letter Hunt (Rules/LetterHunt.cs) - matching an uppercase letter to its
        // lowercase form hits the same b/d, p/q, m/w, n/u confusions as finding one does.
        private static readonly Dictionary<char, char> ConfusablePartner = new Dictionary<char, char>
        {
            { 'b', 'd' }, { 'd', 'b' }, { 'p', 'q' }, { 'q', 'p' }, { 'm', 'w' }, { 'w', 'm' }, { 'n', 'u' }, { 'u', 'n' },
        };

        public static UppercaseToLowercaseRound Create(int level, Random rng, char? previousTarget)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var index = level - DifficultyLadder.MinLevel;
            var poolSize = PoolSizeByLevel[index];
            var choiceCount = ChoiceCountByLevel[index];

            char target;
            do { target = (char)('a' + rng.Next(poolSize)); } while (previousTarget.HasValue && target == previousTarget.Value && poolSize > 1);

            var choices = new List<char> { target };
            if (level >= ConfusablesFromLevel && ConfusablePartner.TryGetValue(target, out var partner) && partner - 'a' < poolSize)
                choices.Add(partner);

            var pool = new List<char>(poolSize);
            for (var i = 0; i < poolSize; i++)
            {
                var c = (char)('a' + i);
                if (!choices.Contains(c)) pool.Add(c);
            }
            while (choices.Count < choiceCount)
            {
                var pick = rng.Next(0, pool.Count);
                choices.Add(pool[pick]);
                pool.RemoveAt(pick);
            }
            Shuffle(choices, rng);

            return new UppercaseToLowercaseRound { Target = target, Choices = choices.ToArray(), CorrectIndex = choices.IndexOf(target) };
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
