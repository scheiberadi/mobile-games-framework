using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    public sealed class WordToImageRound
    {
        // The word shown in the target tile - drawn as a "words/<word>" sprite (a picture of the printed word,
        // never TMP_Text), per spec 4.9 the print is decorative/optional support: Eva also speaks the word, and
        // gameplay never depends on reading it. Choice tiles show pictures ("wordtoimage/<word>"); the one whose
        // picture matches TargetWord is correct, at CorrectIndex.
        public string TargetWord;
        public string[] Choices;
        public int CorrectIndex;
    }

    // Word to Image (spec 4.2, Literacy): MATCH. Reuses Uppercase to Lowercase's own MATCH shell and, unlike
    // Beginning Sound, keeps the shown target tile - but the tile is a picture of the printed word rather than
    // a photo of an object. Distractor guarantee reuses a "visually similar printed word" confusable pair
    // (same length, one letter apart - cat/hat, dog/fog, sun/fun, cup/cap, box/fox, bed/red) instead of a shape
    // or sound confusion: the same guaranteed-mistake idea, scoped to how two short printed words can look
    // alike to a pre-reader. Own difficulty ladder (PlayerProgress.WordToImageLevel/Buffer).
    public static class WordToImageRoundGenerator
    {
        public const int RoundsPerSession = 5;

        // 6 pairs of visually similar printed words, ordered so PoolSizeByLevel can gate a growing prefix.
        private static readonly string[] Catalogue =
        {
            "cat", "hat", "dog", "fog", "sun", "fun", "cup", "cap", "box", "fox", "bed", "red",
        };

        // Same growth shape as Beginning Sound/Uppercase to Lowercase's own pool tables.
        private static readonly int[] PoolSizeByLevel = { 4, 6, 8, 10, 12, 12 };
        private static readonly int[] ChoiceCountByLevel = { 3, 3, 4, 4, 4, 4 };
        private const int ConfusablesFromLevel = 5;

        private static readonly Dictionary<string, string> ConfusablePartner = new Dictionary<string, string>
        {
            { "cat", "hat" }, { "hat", "cat" }, { "dog", "fog" }, { "fog", "dog" },
            { "sun", "fun" }, { "fun", "sun" }, { "cup", "cap" }, { "cap", "cup" },
            { "box", "fox" }, { "fox", "box" }, { "bed", "red" }, { "red", "bed" },
        };

        public static WordToImageRound Create(int level, Random rng, string previousTarget)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var index = level - DifficultyLadder.MinLevel;
            var poolSize = PoolSizeByLevel[index];
            var choiceCount = ChoiceCountByLevel[index];
            var pool = Catalogue.Take(poolSize).ToList();

            string target;
            do { target = pool[rng.Next(pool.Count)]; } while (previousTarget != null && target == previousTarget && pool.Count > 1);

            var choices = new List<string> { target };
            if (level >= ConfusablesFromLevel && ConfusablePartner.TryGetValue(target, out var partner) && pool.Contains(partner))
                choices.Add(partner);

            var remaining = pool.Where(w => !choices.Contains(w)).ToList();
            Shuffle(remaining, rng);
            while (choices.Count < choiceCount && remaining.Count > 0)
            {
                choices.Add(remaining[0]);
                remaining.RemoveAt(0);
            }
            Shuffle(choices, rng);

            return new WordToImageRound { TargetWord = target, Choices = choices.ToArray(), CorrectIndex = choices.IndexOf(target) };
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
