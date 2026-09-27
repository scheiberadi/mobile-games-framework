using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    public sealed class BuildAWordRound
    {
        // A 3-letter word from the shared spelling catalogue (same words as Word to Image/Missing Letter); the
        // last letter is always the missing one - "CA_" shown means Word = "cat", Missing = 't'. Eva speaks the
        // whole target word aloud before the tiles go interactive (word_<Word>, reusing Rhyming's per-word
        // voice-line convention), confirming which word is being built without revealing which letter completes it.
        public string Word;
        public char[] Choices;

        public char Missing => Word[2];
    }

    // Build a Word (spec 4.2, Literacy): TAP-THE-TARGET, the second spelling-composition game - "CA_" shown
    // (the word's first two letters), the child taps the letter that completes it into a real, spoken word.
    // Reuses Missing Letter's own answer-tile mechanic (Rules/MissingLetter.cs) almost unchanged - only the
    // hidden position moves from the middle letter to the last one, and the target word is spoken aloud so the
    // child's ear confirms what they're building, not just their eye. Same 3-letter catalogue and shape-
    // confusable pairs as Missing Letter. Own difficulty ladder (PlayerProgress.BuildAWordLevel/Buffer).
    public static class BuildAWordRoundGenerator
    {
        public const int RoundsPerSession = 5;

        private static readonly string[] Catalogue =
        {
            "cat", "hat", "dog", "fog", "sun", "fun", "cup", "cap", "box", "fox", "bed", "red",
        };

        private static readonly int[] PoolSizeByLevel = { 4, 6, 8, 10, 12, 12 };
        private static readonly int[] TileCountByLevel = { 3, 4, 5, 5, 6, 6 };
        private const int ConfusablesFromLevel = 3;

        private static readonly Dictionary<char, char> ConfusablePartner = new Dictionary<char, char>
        {
            { 'b', 'd' }, { 'd', 'b' }, { 'p', 'q' }, { 'q', 'p' }, { 'm', 'w' }, { 'w', 'm' }, { 'n', 'u' }, { 'u', 'n' },
        };

        public static BuildAWordRound Create(int level, Random rng, string previousWord)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var index = level - DifficultyLadder.MinLevel;
            var poolSize = PoolSizeByLevel[index];
            var tileCount = TileCountByLevel[index];
            var pool = Catalogue.Take(poolSize).ToList();

            string word;
            do { word = pool[rng.Next(pool.Count)]; } while (previousWord != null && word == previousWord && pool.Count > 1);
            var missing = word[2];

            var choices = new List<char> { missing };
            if (level >= ConfusablesFromLevel && ConfusablePartner.TryGetValue(missing, out var partner) && !choices.Contains(partner))
                choices.Add(partner);

            var letterPool = pool.SelectMany(w => w).Distinct().Where(c => !choices.Contains(c)).ToList();
            Shuffle(letterPool, rng);
            while (choices.Count < tileCount && letterPool.Count > 0)
            {
                choices.Add(letterPool[0]);
                letterPool.RemoveAt(0);
            }
            Shuffle(choices, rng);

            return new BuildAWordRound { Word = word, Choices = choices.ToArray() };
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
