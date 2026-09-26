using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    public sealed class ItemToShadowRound
    {
        public string TargetKey;
        // In final (shuffled) display order; sprite key suffix is "itemtoshadow/<key>_silhouette" (see the
        // screen). One of these equals TargetKey - that's the correct choice, at CorrectIndex.
        public string[] Choices;
        public int CorrectIndex;
    }

    // Item to Shadow (Playground, spec 4.1): Logic/spatial, classification/shape recognition. An object is shown;
    // the child taps its matching silhouette among choices. Reuses the shared DifficultyLadder (Rules/Counting.cs,
    // levels 1-6) for its own, independent level and buffer (see PlayerProgress.ItemToShadowLevel/Buffer).
    // Progression per the plan: silhouette similarity - distinct shapes at low levels (distractor silhouettes come
    // from a wholly different outline group) grow into near-identical outlines at high levels (distractors share
    // the target's own outline group, same "obvious -> same-subgroup" shape as OddOneOutRoundGenerator).
    public static class ItemToShadowRoundGenerator
    {
        public const int RoundsPerSession = 5;

        private sealed class Item
        {
            public readonly string Key, Group;
            public Item(string key, string group) { Key = key; Group = group; }
        }

        // A small placeholder content pool (real object/silhouette art is a later authoring pass, per the plan's
        // own "content datasets are authored content, not generated" note). Every group has 4 members so a
        // same-group distractor pool always has the 3 it needs at the hardest level.
        private static readonly Item[] Catalogue =
        {
            new Item("apple", "Round"), new Item("orange", "Round"), new Item("ball", "Round"), new Item("balloon", "Round"),
            new Item("banana", "Narrow"), new Item("carrot", "Narrow"), new Item("pencil", "Narrow"), new Item("candle", "Narrow"),
            new Item("cat", "Animal"), new Item("dog", "Animal"), new Item("fox", "Animal"), new Item("rabbit", "Animal"),
            new Item("car", "Vehicle"), new Item("truck", "Vehicle"), new Item("bus", "Vehicle"), new Item("bike", "Vehicle"),
        };

        // Index i = level (i + 1).
        private static readonly int[] ChoiceCountByLevel = { 3, 3, 4, 4, 4, 4 };
        private const int SameGroupModeFromLevel = 3;

        public static ItemToShadowRound Create(int level, Random rng)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var choiceCount = ChoiceCountByLevel[level - DifficultyLadder.MinLevel];

            var target = Catalogue[rng.Next(0, Catalogue.Length)];
            var pool = level < SameGroupModeFromLevel
                ? Catalogue.Where(i => i.Group != target.Group).ToList()
                : Catalogue.Where(i => i.Group == target.Group && i.Key != target.Key).ToList();

            var choices = new List<string> { target.Key };
            foreach (var item in TakeDistinct(pool, choiceCount - 1, rng)) choices.Add(item.Key);
            Shuffle(choices, rng);

            return new ItemToShadowRound { TargetKey = target.Key, Choices = choices.ToArray(), CorrectIndex = choices.IndexOf(target.Key) };
        }

        private static List<Item> TakeDistinct(List<Item> pool, int n, Random rng)
        {
            var copy = new List<Item>(pool);
            Shuffle(copy, rng);
            return copy.GetRange(0, n);
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
