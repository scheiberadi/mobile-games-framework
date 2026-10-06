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
        // For each bin (same order as BinCategories): things of its category that are already there when the round starts (the
        // residents scene stands them in the pasture; the same kind as one on the belt may be among them, a cow can join cows).
        public string[][] Residents;

        // Puts the bins in the given order (a residents scene has a fixed zone for each category); Residents follow their bins.
        public DropSortRound WithBinOrder(params string[] order)
        {
            var bins = BinCategories.OrderBy(c => Array.IndexOf(order, c)).ToArray();
            var residents = bins.Select(c => Residents[Array.IndexOf(BinCategories, c)]).ToArray();
            BinCategories = bins;
            Residents = residents;
            return this;
        }

        // The index in BinCategories of the bin item `itemIndex` belongs in.
        public int BinIndexOf(int itemIndex) => Array.IndexOf(BinCategories, ItemCategories[itemIndex]);
    }

    // Builds a multi-item sort from a catalogue (Sorting's by default). Match rounds (MatchRoundBuilder) have one target per
    // round and so cannot drive this; the old Sorting tap game is unchanged and keeps its own six-item list.
    public static class DropSortRoundBuilder
    {
        // A round is 3-6 drags, so a session is shorter than the tap game's five rounds.
        public const int RoundsPerSession = 3;
        public const int MaxItems = 6;
        public const int MaxBins = 3;
        public const int ResidentsPerBin = 5;
        public const int FewResidentsPerBin = 3; // Land/Sea/Air: too many animals standing about looks crowded

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

        // The other Brain Gym sorting games on the same presenter. They hold fewer things than Sorting (one per
        // category for some), so a round may have fewer items than the level table asks for; Create never shows
        // an empty bin and never repeats an item.
        public static readonly IReadOnlyList<(string Id, string Category)> RecyclingCatalogue = new[]
        {
            ("bottle", "plastic"), ("can", "plastic"), ("newspaper", "paper"), ("cardboard", "paper"),
            ("jar", "glass"), ("bananapeel", "organic"),
        };

        public static readonly IReadOnlyList<(string Id, string Category)> ItemToCategoryCatalogue = new[]
        {
            ("guitar", "music"), ("drum", "music"), ("ball", "sports"), ("bat", "sports"),
            ("book", "reading"), ("paintbrush", "art"),
        };

        public static readonly IReadOnlyList<(string Id, string Category)> ChoresCatalogue = new[]
        {
            ("shirt", "hamper"), ("sock", "hamper"), ("toy", "bedroom"), ("book", "bedroom"),
            ("dish", "kitchen"), ("towel", "bathroom"),
        };

        public static IReadOnlyList<string> Categories { get; } = Catalogue.Select(c => c.Category).Distinct().ToArray();

        public static DropSortRound Create(int level, Random rng) => Create(Catalogue, level, rng);

        // minBins forces at least that many bins (a three-zone scene always shows all three), as far as the catalogue has categories.
        public static DropSortRound Create(IReadOnlyList<(string Id, string Category)> catalogue, int level, Random rng, int minBins = 0, int residentsPerBin = ResidentsPerBin)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var itemCount = ItemCountByLevel[level - DifficultyLadder.MinLevel];
            var categories = catalogue.Select(c => c.Category).Distinct().ToArray();
            // A two-bucket game (Domestic vs Wild, Living vs Non-living, Day and Night) never has a third bin.
            var binCount = Math.Min(Math.Max(BinCountByLevel[level - DifficultyLadder.MinLevel], minBins), categories.Length);
            int Supply(string category) => catalogue.Count(c => c.Category == category);

            // Pick the bins; try a few shuffles for a set that holds enough items for this level.
            string[] bins = null;
            var bestSupply = -1;
            for (var attempt = 0; attempt < 20 && bestSupply < itemCount; attempt++)
            {
                var candidate = categories.OrderBy(_ => rng.Next()).Take(binCount).ToArray();
                var supply = candidate.Sum(Supply);
                if (supply > bestSupply) { bins = candidate; bestSupply = supply; }
            }

            // Every bin gets one item, then the rest are dealt round-robin in a random bin order, so the counts
            // differ by at most one and never exceed what the catalogue holds for a category.
            var counts = new int[binCount];
            for (var b = 0; b < binCount; b++) counts[b] = 1;
            var dealOrder = Enumerable.Range(0, binCount).OrderBy(_ => rng.Next()).ToArray();
            var dealt = binCount;
            for (var turn = 0; dealt < itemCount && turn < itemCount * binCount; turn++)
            {
                var b = dealOrder[turn % binCount];
                if (counts[b] >= Supply(bins[b])) continue;
                counts[b]++;
                dealt++;
            }

            var items = new List<(string Id, string Category)>();
            for (var b = 0; b < binCount; b++)
            {
                var category = bins[b];
                items.AddRange(catalogue.Where(c => c.Category == category).OrderBy(_ => rng.Next()).Take(counts[b]));
            }
            var belt = items.OrderBy(_ => rng.Next()).ToArray();
            // When a category has fewer kinds than residentsPerBin (only three animals fly) the kinds repeat.
            var residents = bins.Select(category =>
            {
                var kinds = catalogue.Where(c => c.Category == category).Select(c => c.Id).OrderBy(_ => rng.Next()).ToArray();
                return Enumerable.Range(0, residentsPerBin).Select(i => kinds[i % kinds.Length]).ToArray();
            }).ToArray();

            return new DropSortRound
            {
                ItemIds = belt.Select(i => i.Id).ToArray(),
                ItemCategories = belt.Select(i => i.Category).ToArray(),
                BinCategories = bins,
                Residents = residents,
            };
        }
    }
}
