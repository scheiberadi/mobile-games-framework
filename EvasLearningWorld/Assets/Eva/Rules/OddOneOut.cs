using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    public sealed class OddOneOutRound
    {
        // In final (shuffled) display order; sprite key suffix is "oddoneout/<item>" (see OddOneOutScreen).
        public string[] Items;
        public int OddIndex;
    }

    // Odd One Out (Playground, spec 4.1): Logic/spatial, classification. 4-5 objects are shown, one doesn't
    // belong; the child taps it. Reuses the shared DifficultyLadder (Rules/Counting.cs, levels 1-6) for its own,
    // independent level and buffer (see PlayerProgress.OddOneOutLevel/Buffer). Progression per the plan: obvious
    // category (levels 1-2: the odd item is from a wholly different category) -> a closer distinction within the
    // same category (levels 3-6: same top-level category, different subgroup - the plan's "habitat" example;
    // levels 5-6 reuse the same mechanic as a placeholder for a subtler "abstract" tier until a richer content
    // pass authors one, same open item the M4 plan already flags for every game's dataset).
    public static class OddOneOutRoundGenerator
    {
        public const int RoundsPerSession = 5;

        private sealed class Item
        {
            public readonly string Key, Category, Subgroup;
            public Item(string key, string category, string subgroup) { Key = key; Category = category; Subgroup = subgroup; }
        }

        // A small placeholder content pool (sprite art and any richer categorisation are a later authoring pass,
        // per the plan's own "content datasets are authored content, not generated" note). Animal is the only
        // category split into subgroups today - enough to drive levels 3-6 - so every subgroup-mode round draws
        // from it; more categories/subgroups can be folded in later without changing the generator.
        private static readonly Item[] Catalogue =
        {
            new Item("cow", "Animal", "Farm"), new Item("pig", "Animal", "Farm"), new Item("sheep", "Animal", "Farm"),
            new Item("horse", "Animal", "Farm"), new Item("goat", "Animal", "Farm"),
            new Item("lion", "Animal", "Wild"), new Item("tiger", "Animal", "Wild"), new Item("bear", "Animal", "Wild"),
            new Item("elephant", "Animal", "Wild"), new Item("zebra", "Animal", "Wild"),
            new Item("car", "Vehicle", "Vehicle"), new Item("bus", "Vehicle", "Vehicle"), new Item("bike", "Vehicle", "Vehicle"),
            new Item("truck", "Vehicle", "Vehicle"), new Item("train", "Vehicle", "Vehicle"),
            new Item("apple", "Fruit", "Fruit"), new Item("banana", "Fruit", "Fruit"), new Item("orange", "Fruit", "Fruit"),
            new Item("grape", "Fruit", "Fruit"), new Item("pear", "Fruit", "Fruit"),
        };

        private static readonly string[] Categories = Catalogue.Select(i => i.Category).Distinct().ToArray();

        // Index i = level (i + 1).
        private static readonly int[] ChoiceCountByLevel = { 4, 5, 4, 5, 4, 5 };
        private const int SubgroupModeFromLevel = 3;

        public static OddOneOutRound Create(int level, Random rng)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var count = ChoiceCountByLevel[level - DifficultyLadder.MinLevel];

            var items = level < SubgroupModeFromLevel ? CreateObviousRound(count, rng) : CreateSubgroupRound(count, rng);
            Shuffle(items, rng);

            var oddIndex = items.FindIndex(i => i.Item2);
            return new OddOneOutRound { Items = items.Select(i => i.Item1).ToArray(), OddIndex = oddIndex };
        }

        // N-1 items from one category, the odd one from a different category entirely.
        private static List<(string, bool)> CreateObviousRound(int count, Random rng)
        {
            var baseCategory = Categories[rng.Next(0, Categories.Length)];
            var basePool = Catalogue.Where(i => i.Category == baseCategory).ToList();
            var oddPool = Catalogue.Where(i => i.Category != baseCategory).ToList();

            var picked = new List<(string, bool)>();
            foreach (var item in TakeDistinct(basePool, count - 1, rng)) picked.Add((item.Key, false));
            picked.Add((TakeDistinct(oddPool, 1, rng)[0].Key, true));
            return picked;
        }

        // N-1 items from one subgroup, the odd one from another subgroup of the same category.
        private static List<(string, bool)> CreateSubgroupRound(int count, Random rng)
        {
            // Only a category with more than one subgroup can drive this mode (Animal, per the catalogue above).
            var category = Catalogue.GroupBy(i => i.Category).First(g => g.Select(i => i.Subgroup).Distinct().Count() > 1).Key;
            var groupNames = Catalogue.Where(i => i.Category == category).Select(i => i.Subgroup).Distinct().ToArray();
            var baseSubgroup = groupNames[rng.Next(0, groupNames.Length)];
            var oddSubgroup = groupNames.First(g => g != baseSubgroup);

            var basePool = Catalogue.Where(i => i.Category == category && i.Subgroup == baseSubgroup).ToList();
            var oddPool = Catalogue.Where(i => i.Category == category && i.Subgroup == oddSubgroup).ToList();

            var picked = new List<(string, bool)>();
            foreach (var item in TakeDistinct(basePool, count - 1, rng)) picked.Add((item.Key, false));
            picked.Add((TakeDistinct(oddPool, 1, rng)[0].Key, true));
            return picked;
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
