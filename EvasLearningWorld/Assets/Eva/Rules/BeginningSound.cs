using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    public sealed class BeginningSoundRound
    {
        public char TargetLetter;
        // In final (shuffled) display order; sprite key is "beginningsound/<key>". One of these is the item
        // whose name starts with TargetLetter - that's the correct choice, at CorrectIndex.
        public string[] Choices;
        public int CorrectIndex;
    }

    // Beginning Sound (spec 4.2, Literacy): MATCH, audio-led. Reuses Uppercase to Lowercase's own MATCH shell,
    // minus its shown target tile - here the prompt is entirely spoken, never pictured, since the child listens
    // for a sound rather than looks for a shape. Per the plan's own "content datasets are authored content, not
    // generated" note, this session has no per-word audio to author, so the round's spoken prompt reuses the
    // alphabet's own single-letter lines ("letter_<x>", already authored for Letter Hunt/Follow Letters in
    // Order) as the sound cue, rather than a full spoken word - a documented simplification, not the eventual
    // "Eva says a word" version the plan describes, until real word audio exists. Own difficulty ladder
    // (PlayerProgress.BeginningSoundLevel/Buffer).
    public static class BeginningSoundRoundGenerator
    {
        public const int RoundsPerSession = 5;

        private sealed class Item
        {
            public readonly char Letter;
            public readonly string Key;
            public Item(char letter, string key) { Letter = letter; Key = key; }
        }

        // One representative picture per letter (a small placeholder content pool - real art/word choice is a
        // later authoring pass, same caveat as Item to Shadow's own catalogue). Ordered so PoolSizeByLevel can
        // gate a growing prefix of it.
        private static readonly Item[] Catalogue =
        {
            new Item('a', "apple"), new Item('b', "ball"), new Item('c', "cat"), new Item('d', "dog"),
            new Item('f', "fish"), new Item('g', "goat"), new Item('h', "hat"), new Item('j', "jam"),
            new Item('k', "kite"), new Item('l', "lion"), new Item('m', "moon"), new Item('n', "nest"),
        };

        // Index i = level (i + 1): how much of the catalogue prefix is in play, same growth shape as Letter
        // Hunt's own PoolSizeByLevel.
        private static readonly int[] PoolSizeByLevel = { 4, 6, 8, 10, 12, 12 };
        private static readonly int[] ChoiceCountByLevel = { 3, 3, 4, 4, 4, 4 };
        private const int ConfusablesFromLevel = 5;

        // Sound-alike letter pairs within this catalogue's own set (g/k, m/n both share a place of articulation
        // young children commonly confuse) - the guaranteed-distractor idea, scoped to sound rather than shape.
        private static readonly Dictionary<char, char> ConfusablePartner = new Dictionary<char, char>
        {
            { 'g', 'k' }, { 'k', 'g' }, { 'm', 'n' }, { 'n', 'm' },
        };

        public static BeginningSoundRound Create(int level, Random rng, char? previousTarget)
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

            return new BeginningSoundRound { TargetLetter = target, Choices = choices.ToArray(), CorrectIndex = choices.IndexOf(targetItem.Key) };
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
