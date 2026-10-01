using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    // One round of a "put each thing in the right bin" game (answer-variety Prototype B, Sorting). Items come one
    // at a time in ItemIds order; each belongs to exactly one of the bins (BinCategories, left to right).
    public sealed class DropSortRound
    {
        public string[] ItemIds;
        public string[] ItemCategories;
        public string[] BinCategories;

        // The index in BinCategories of the bin item `itemIndex` belongs in.
        public int BinIndexOf(int itemIndex) => Array.IndexOf(BinCategories, ItemCategories[itemIndex]);
    }

    // Builds a multi-item sort from the Sorting catalogue. Match rounds (MatchRoundBuilder) have one target per
    // round and so cannot drive this; the old Sorting tap game is unchanged and keeps its own six-item list.
    public static class DropSortRoundBuilder
    {
        // A round is 3-6 drags, so a session is shorter than the tap game's five rounds.
        public const int RoundsPerSession = 3;
        public const int MaxItems = 6;
        public const int MaxBins = 3;

        // Index i = level (i + 1): how many things to put away and into how many bins.
        public static readonly int[] ItemCountByLevel = { 3, 4, 4, 5, 6, 6 };
        public static readonly int[] BinCountByLevel = { 2, 2, 3, 3, 3, 3 };

        // Three items for each of four categories, so every bin that is shown can receive two or more of them.
        // The item id doubles as the picture key (braingym/sortitem_<id>) and the voice key (braingym_category_<id>).
        public static readonly IReadOnlyList<(string Id, string Category)> Catalogue = new[]
        {
            ("apple", "fruit"), ("banana", "fruit"), ("orange", "fruit"),
            ("carrot", "vegetable"), ("tomato", "vegetable"), ("corn", "vegetable"),
            ("shirt", "clothes"), ("pants", "clothes"), ("sock", "clothes"),
            ("truck", "vehicle"), ("bus", "vehicle"), ("bike", "vehicle"),
        };

        public static IReadOnlyList<string> Categories { get; } = Catalogue.Select(c => c.Category).Distinct().ToArray();

        public static DropSortRound Create(int level, Random rng)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var itemCount = ItemCountByLevel[level - DifficultyLadder.MinLevel];
            var binCount = BinCountByLevel[level - DifficultyLadder.MinLevel];

            var bins = Categories.OrderBy(_ => rng.Next()).Take(binCount).ToArray();

            // Every bin gets one item, then the rest are dealt round-robin to a random bin order, so the counts
            // differ by at most one and never exceed what the catalogue holds for a category.
            var dealt = new int[binCount];
            for (var i = 0; i < itemCount; i++) dealt[i % binCount]++;
            var dealOrder = Enumerable.Range(0, binCount).OrderBy(_ => rng.Next()).ToArray();
            var counts = new int[binCount];
            for (var i = 0; i < binCount; i++) counts[dealOrder[i]] = dealt[i];

            var items = new List<(string Id, string Category)>();
            for (var b = 0; b < binCount; b++)
            {
                var category = bins[b];
                items.AddRange(Catalogue.Where(c => c.Category == category).OrderBy(_ => rng.Next()).Take(counts[b]));
            }
            var belt = items.OrderBy(_ => rng.Next()).ToArray();

            return new DropSortRound
            {
                ItemIds = belt.Select(i => i.Id).ToArray(),
                ItemCategories = belt.Select(i => i.Category).ToArray(),
                BinCategories = bins,
            };
        }
    }
}
