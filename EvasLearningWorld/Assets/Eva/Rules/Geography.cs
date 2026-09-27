using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public sealed class Country
    {
        public Country(string id, string continent)
        {
            Id = id;
            Continent = continent;
        }

        public string Id { get; }
        public string Continent { get; }
    }

    // Placeholder content (11 countries, 6 continents) - same caveat as ZooFarmAnimals, pending a real
    // content pass. One landmark per country is assumed (GeographyRoundGenerator's Landmark mode reuses Id).
    public static class GeographyCountries
    {
        public static readonly Country[] All =
        {
            new Country("romania", "europe"),
            new Country("france", "europe"),
            new Country("spain", "europe"),
            new Country("usa", "north_america"),
            new Country("brazil", "south_america"),
            new Country("egypt", "africa"),
            new Country("kenya", "africa"),
            new Country("china", "asia"),
            new Country("japan", "asia"),
            new Country("india", "asia"),
            new Country("australia", "oceania"),
        };
    }

    // Geography (Zoo & Farm's eleventh game, assigned 2026-09-26 per the plan - "closest existing
    // world-knowledge building"): MATCH, own dataset, reuses ZooFarm's MatchRoundBuilder shell. Eva always
    // names the country/landmark (TargetSprite stays null, same as Animal -> Sound), so the picture guessing
    // never depends on reading. Level gates which mode plays: Flag first, then Continent, then Landmark -
    // three separate "world-knowledge" skills over the same 11-country roster.
    public enum GeographyMode { Flag, Continent, Landmark }

    public static class GeographyRoundGenerator
    {
        public const int RoundsPerSession = 5;
        private static readonly int[] PoolSizeByLevel = { 4, 6, 8, 9, 10, 11 };
        private static readonly int[] ChoiceCountByLevel = { 3, 3, 4, 4, 4, 4 };

        public static MatchRound Create(int level, Random rng, string previousTargetId)
        {
            var mode = ModeFor(level);
            var (items, choicePrefix, promptKey) = mode switch
            {
                GeographyMode.Flag => (Items(c => c.Id), "geo/flag_", "geo_prompt_flag"),
                GeographyMode.Continent => (Items(c => c.Continent), "geo/continent_", "geo_prompt_continent"),
                _ => (Items(c => c.Id), "geo/landmark_", "geo_prompt_landmark"),
            };
            return MatchRoundBuilder.Build(items, level, rng, previousTargetId, PoolSizeByLevel, ChoiceCountByLevel,
                choicePrefix, null, promptKey, "geo_country_");
        }

        private static GeographyMode ModeFor(int level) => level <= 2 ? GeographyMode.Flag : level <= 4 ? GeographyMode.Continent : GeographyMode.Landmark;

        private static IReadOnlyList<(string id, string value)> Items(Func<Country, string> selector)
        {
            var list = new List<(string, string)>();
            foreach (var c in GeographyCountries.All) list.Add((c.Id, selector(c)));
            return list;
        }
    }
}
