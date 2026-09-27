using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    public sealed class ImageToWordRound
    {
        // The word named by the target tile's picture ("wordtoimage/<word>" sprite - the same picture set Word
        // to Image uses, just shown in the target tile here instead of a choice tile). Choice tiles show
        // printed-word pictures ("words/<word>", also shared with Word to Image); the one matching TargetWord
        // is correct, at CorrectIndex.
        public string TargetWord;
        public string[] Choices;
        public int CorrectIndex;
    }

    // Image to Word (spec 4.2, Literacy): MATCH, the reverse direction of Word to Image - a picture is shown,
    // the child taps its matching printed word. Same presenter, same catalogue, same pool/choice-count/
    // confusable tables as Word to Image (Rules/WordToImage.cs); only the Screen's tile assignment is mirrored
    // (picture in the target tile, printed words as the choices). Own difficulty ladder
    // (PlayerProgress.ImageToWordLevel/Buffer) - a separate progression from Word to Image's, even though the
    // content pool is identical, since matching picture-to-word and word-to-picture are not the same skill.
    public static class ImageToWordRoundGenerator
    {
        public const int RoundsPerSession = 5;

        // Identical catalogue to Word to Image - same 6 pairs of visually similar printed words.
        private static readonly string[] Catalogue =
        {
            "cat", "hat", "dog", "fog", "sun", "fun", "cup", "cap", "box", "fox", "bed", "red",
        };

        private static readonly int[] PoolSizeByLevel = { 4, 6, 8, 10, 12, 12 };
        private static readonly int[] ChoiceCountByLevel = { 3, 3, 4, 4, 4, 4 };
        private const int ConfusablesFromLevel = 5;

        private static readonly Dictionary<string, string> ConfusablePartner = new Dictionary<string, string>
        {
            { "cat", "hat" }, { "hat", "cat" }, { "dog", "fog" }, { "fog", "dog" },
            { "sun", "fun" }, { "fun", "sun" }, { "cup", "cap" }, { "cap", "cup" },
            { "box", "fox" }, { "fox", "box" }, { "bed", "red" }, { "red", "bed" },
        };

        public static ImageToWordRound Create(int level, Random rng, string previousTarget)
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

            return new ImageToWordRound { TargetWord = target, Choices = choices.ToArray(), CorrectIndex = choices.IndexOf(target) };
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
