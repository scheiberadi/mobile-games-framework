using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    public sealed class LetterToSoundRound
    {
        public char TargetLetter;
        // In final (shuffled) display order; sprite key is "beginningsound/<key>" - the same picture set
        // Beginning Sound uses, since it's the same catalogue read in the opposite direction. One of these is
        // the item whose name starts with TargetLetter - that's the correct choice, at CorrectIndex.
        public string[] Choices;
        public int CorrectIndex;
    }

    // Letter to Sound (spec 4.2, Literacy): MATCH, the reverse direction of Beginning Sound - a letter is shown
    // (visually, "letters/upper_<letter>", the same sprite Uppercase to Lowercase's target tile uses), the
    // child taps the picture whose name starts with that letter's sound. Reuses Beginning Sound's own
    // catalogue/pool/confusable tables (Rules/BeginningSound.cs) and its "beginningsound/<key>" picture sprites
    // as-is - no new content pool or art convention needed, since both games read the same
    // letter-sound-to-picture catalogue in opposite directions. The spec's higher-level variant ("later blends
    // letters, C+A+T=CAT") is a documented deferred enhancement, not built this pass - it needs its own spelling
    // presenter and content (real words spelled out), which is a separate authoring effort; flagged in the plan
    // doc for Adrian's read, same as every other scope note this session. Own difficulty ladder
    // (PlayerProgress.LetterToSoundLevel/Buffer) - a separate progression from Beginning Sound's, even though
    // the content pool is identical, since matching letter-to-sound and sound-to-letter aren't the same skill.
    public static class LetterToSoundRoundGenerator
    {
        public const int RoundsPerSession = 5;

        private sealed class Item
        {
            public readonly char Letter;
            public readonly string Key;
            public Item(char letter, string key) { Letter = letter; Key = key; }
        }

        // Identical catalogue to Beginning Sound - same letter-to-picture pairs, same order.
        private static readonly Item[] Catalogue =
        {
            new Item('a', "apple"), new Item('b', "ball"), new Item('c', "cat"), new Item('d', "dog"),
            new Item('f', "fish"), new Item('g', "goat"), new Item('h', "hat"), new Item('j', "jam"),
            new Item('k', "kite"), new Item('l', "lion"), new Item('m', "moon"), new Item('n', "nest"),
        };

        private static readonly int[] PoolSizeByLevel = { 4, 6, 8, 10, 12, 12 };
        private static readonly int[] ChoiceCountByLevel = { 3, 3, 4, 4, 4, 4 };
        private const int ConfusablesFromLevel = 5;

        // Same sound-alike letter pairs as Beginning Sound (g/k, m/n).
        private static readonly Dictionary<char, char> ConfusablePartner = new Dictionary<char, char>
        {
            { 'g', 'k' }, { 'k', 'g' }, { 'm', 'n' }, { 'n', 'm' },
        };

        public static LetterToSoundRound Create(int level, Random rng, char? previousTarget)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var index = level - DifficultyLadder.MinLevel;
            var poolSize = PoolSizeByLevel[index];
            var choiceCount = ChoiceCountByLevel[index];
            var pool = Catalogue.Take(poolSize).ToList();

            char target;
            do { target = pool[rng.Next(pool.Count)].Letter; } while (previousTarget.HasValue && target == previousTarget.Value && poolSize > 1);
            var targetItem = pool.First(i => i.Letter == target);

            var choices = new List<string> { targetItem.Key };
            if (level >= ConfusablesFromLevel && ConfusablePartner.TryGetValue(target, out var partner))
            {
                var partnerItem = pool.FirstOrDefault(i => i.Letter == partner);
                if (partnerItem != null) choices.Add(partnerItem.Key);
            }

            var remaining = new List<Item>(pool.Where(i => !choices.Contains(i.Key)));
            Shuffle(remaining, rng);
            while (choices.Count < choiceCount && remaining.Count > 0)
            {
                choices.Add(remaining[0].Key);
                remaining.RemoveAt(0);
            }
            Shuffle(choices, rng);

            return new LetterToSoundRound { TargetLetter = target, Choices = choices.ToArray(), CorrectIndex = choices.IndexOf(targetItem.Key) };
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
