using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    public sealed class MissingLetterRound
    {
        // A 3-letter word from the shared spelling catalogue (same words as Word to Image/Image to Word); the
        // middle letter is always the missing one - "C_T" shown means Word = "cat", Missing = 'a'.
        public string Word;
        public char[] Choices;

        public char Missing => Word[1];
    }

    // Missing Letter (spec 4.2, Literacy): TAP-THE-TARGET, the first of the cluster's spelling-composition
    // games. Reuses Missing Number's own answer-tile mechanic and problem-field shape (a hidden slot between
    // two known values) read as letters instead of digits: "C_T" is Word[0], a mystery box, Word[2] - the
    // child taps the missing middle letter among choices. Reuses the same 3-letter catalogue as Word to
    // Image/Image to Word (Rules/WordToImage.cs) so every word's middle letter is always the one hidden,
    // matching the spec's own "C_T" example exactly, and Letter Hunt/Uppercase to Lowercase's own
    // shape-confusable pairs (b/d, p/q, m/w, n/u) for the guaranteed distractor - the same shape confusions
    // apply to guessing a missing letter as to finding or matching one. Own difficulty ladder
    // (PlayerProgress.MissingLetterLevel/Buffer).
    public static class MissingLetterRoundGenerator
    {
        public const int RoundsPerSession = 5;

        private static readonly string[] Catalogue =
        {
            "cat", "hat", "dog", "fog", "sun", "fun", "cup", "cap", "box", "fox", "bed", "red",
        };

        // Same growth shape as the rest of the Literacy cluster's own pool tables; tile count follows Missing
        // Number's own TAP-THE-TARGET shape (max 6 tiles) rather than the MATCH family's max 4.
        private static readonly int[] PoolSizeByLevel = { 4, 6, 8, 10, 12, 12 };
        private static readonly int[] TileCountByLevel = { 3, 4, 5, 5, 6, 6 };
        private const int ConfusablesFromLevel = 3;

        private static readonly Dictionary<char, char> ConfusablePartner = new Dictionary<char, char>
        {
            { 'b', 'd' }, { 'd', 'b' }, { 'p', 'q' }, { 'q', 'p' }, { 'm', 'w' }, { 'w', 'm' }, { 'n', 'u' }, { 'u', 'n' },
        };

        public static MissingLetterRound Create(int level, Random rng, string previousWord)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var index = level - DifficultyLadder.MinLevel;
            var poolSize = PoolSizeByLevel[index];
            var tileCount = TileCountByLevel[index];
            var pool = Catalogue.Take(poolSize).ToList();

            string word;
            do { word = pool[rng.Next(pool.Count)]; } while (previousWord != null && word == previousWord && pool.Count > 1);
            var missing = word[1];

            var choices = new List<char> { missing };
            if (level >= ConfusablesFromLevel && ConfusablePartner.TryGetValue(missing, out var partner) && !choices.Contains(partner))
                choices.Add(partner);

            // Distractor letters come from this level's own word pool - never a letter the child hasn't seen
            // among these games' own catalogue.
            var letterPool = pool.SelectMany(w => w).Distinct().Where(c => !choices.Contains(c)).ToList();
            Shuffle(letterPool, rng);
            while (choices.Count < tileCount && letterPool.Count > 0)
            {
                choices.Add(letterPool[0]);
                letterPool.RemoveAt(0);
            }
            Shuffle(choices, rng);

            return new MissingLetterRound { Word = word, Choices = choices.ToArray() };
        }

        private static void Shuffle<T>(List<T> list, Random rng)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
