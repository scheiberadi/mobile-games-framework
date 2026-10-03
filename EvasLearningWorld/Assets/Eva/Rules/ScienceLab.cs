using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // Science Lab (M4.5, docs/kids-games/full-catalogue-plan.md "7. Science Lab"): twelve of its thirteen games
    // reduce to the same MATCH/SORT shape Zoo & Farm's presenter already generalizes (see Rules/ZooFarm.cs's class
    // comment and Rules/MatchRoundBuilder.cs) - a target item and 2-4 choice values, one correct. Unlike Zoo & Farm,
    // these twelve don't share one entity table (an animal has many attributes; a sink/float object has exactly
    // one), so each game gets its own small (id, value) item list here instead of one shared dataset class. Plant
    // Growth, the thirteenth, is a SEQUENCE game instead - see Rules/PlantGrowth.cs and Rules/SequenceRoundBuilder.cs.
    //
    // Implementation decision (2026-09-27, same pattern as Zoo & Farm's SORT-via-MATCH-presenter call, flagged for
    // Adrian's review): Sink or Float and Magnet are built as tap-the-bucket MATCH/SORT rounds (predict which
    // bucket an object belongs in) rather than their own drop-and-observe physics mini-simulation, and Cause and
    // Effect is built as "pick the matching effect picture" MATCH rather than Workshop's own BUILD->TEST->OBSERVE
    // shape (which doesn't exist yet either). All three reuse the presenter fully, at the cost of the interactive
    // simulation flavor the plan's mechanic column describes - lower risk, ships now, and still teaches the same
    // prediction skill. Content across every dataset below is placeholder, pending a real art/content pass.
    public enum ScienceLabGameKind
    {
        SinkOrFloat, Magnet, LivingVsNonLiving, HumanSenses, HealthyVsUnhealthy, Weather, DressForWeather,
        CauseAndEffect, CookingMeasures, Seasons, DayNight, Space
    }

    public static class ScienceLabRoundGenerator
    {
        public const int RoundsPerSession = 5;

        public static MatchRound Create(ScienceLabGameKind kind, int level, Random rng, string previousTargetId)
        {
            var config = Config(kind);
            return MatchRoundBuilder.Build(config.items, level, rng, previousTargetId, config.poolSizeByLevel, config.choiceCountByLevel,
                config.choicePrefix, config.targetPrefix, config.promptKey, config.targetVoicePrefix);
        }

        // The same things and buckets as the tap versions, for the drop-sort presenter (Living vs Non-living, Seasons, Day and Night).
        public static IReadOnlyList<(string Id, string Category)> DropSortCatalogue(ScienceLabGameKind kind)
        {
            var items = Config(kind).items;
            var list = new List<(string, string)>();
            foreach (var item in items) list.Add((item.id, item.value));
            return list;
        }

        private static readonly int[] SixItemPool = { 4, 5, 6, 6, 6, 6 };
        private static readonly int[] EightItemPool = { 4, 5, 6, 7, 8, 8 };
        private static readonly int[] TenItemPool = { 5, 6, 7, 8, 10, 10 };
        private static readonly int[] TwelveItemPool = { 6, 8, 10, 12, 12, 12 };
        private static readonly int[] FourteenItemPool = { 6, 8, 10, 12, 14, 14 };
        private static readonly int[] ChoiceCountByLevel = { 3, 3, 4, 4, 4, 4 };

        private static (IReadOnlyList<(string id, string value)> items, int[] poolSizeByLevel, int[] choiceCountByLevel,
            string choicePrefix, string targetPrefix, string promptKey, string targetVoicePrefix) Config(ScienceLabGameKind kind)
        {
            switch (kind)
            {
                case ScienceLabGameKind.SinkOrFloat:
                    return (SinkOrFloatItems, TwelveItemPool, ChoiceCountByLevel, "sciencelab/bucket_", "sciencelab/object_",
                        "sciencelab_prompt_sinkorfloat", "sciencelab_object_");
                case ScienceLabGameKind.Magnet:
                    return (MagnetItems, TwelveItemPool, ChoiceCountByLevel, "sciencelab/bucket_", "sciencelab/object_",
                        "sciencelab_prompt_magnet", "sciencelab_object_");
                case ScienceLabGameKind.LivingVsNonLiving:
                    return (LivingVsNonLivingItems, FourteenItemPool, ChoiceCountByLevel, "sciencelab/bucket_", "sciencelab/object_",
                        "sciencelab_prompt_livingvsnonliving", "sciencelab_object_");
                case ScienceLabGameKind.HumanSenses:
                    return (HumanSensesItems, SixItemPool, ChoiceCountByLevel, "sciencelab/sense_", "sciencelab/organ_",
                        "sciencelab_prompt_humansenses", "sciencelab_organ_");
                case ScienceLabGameKind.HealthyVsUnhealthy:
                    return (HealthyVsUnhealthyItems, FourteenItemPool, ChoiceCountByLevel, "sciencelab/bucket_", "sciencelab/food_",
                        "sciencelab_prompt_healthyvsunhealthy", "sciencelab_food_");
                case ScienceLabGameKind.Weather:
                    return (WeatherItems, SixItemPool, ChoiceCountByLevel, "sciencelab/weather_", null,
                        "sciencelab_prompt_weather_identify", "sciencelab_weather_");
                case ScienceLabGameKind.DressForWeather:
                    return (DressForWeatherItems, SixItemPool, ChoiceCountByLevel, "sciencelab/clothing_", "sciencelab/weather_",
                        "sciencelab_prompt_dressforweather", "sciencelab_weather_");
                case ScienceLabGameKind.CauseAndEffect:
                    return (CauseAndEffectItems, EightItemPool, ChoiceCountByLevel, "sciencelab/effect_", "sciencelab/cause_",
                        "sciencelab_prompt_causeandeffect", "sciencelab_cause_");
                case ScienceLabGameKind.CookingMeasures:
                    return (CookingMeasuresItems, TenItemPool, ChoiceCountByLevel, "sciencelab/measure_", "sciencelab/cup_",
                        "sciencelab_prompt_cookingmeasures", "sciencelab_cup_");
                case ScienceLabGameKind.Seasons:
                    return (SeasonsItems, TwelveItemPool, ChoiceCountByLevel, "sciencelab/season_", "sciencelab/activity_",
                        "sciencelab_prompt_seasons", "sciencelab_activity_");
                case ScienceLabGameKind.DayNight:
                    return (DayNightItems, TenItemPool, ChoiceCountByLevel, "sciencelab/daynight_", "sciencelab/activity_",
                        "sciencelab_prompt_daynight", "sciencelab_activity_");
                case ScienceLabGameKind.Space:
                    return (SpaceItems, EightItemPool, ChoiceCountByLevel, "sciencelab/space_", null,
                        "sciencelab_prompt_space_identify", "sciencelab_space_");
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        // Six sink, six float - ordered so the first four already span both buckets (same reasoning as Zoo & Farm's
        // animal ordering: PoolSizeByLevel's early levels must show both buckets, not a run of one).
        private static readonly (string, string)[] SinkOrFloatItems =
        {
            ("rock", "sink"), ("leaf", "float"), ("key", "sink"), ("balloon", "float"),
            ("coin", "sink"), ("cork", "float"), ("spoon", "sink"), ("sponge", "float"),
            ("marble", "sink"), ("rubber_duck", "float"), ("hammer", "sink"), ("apple", "float"),
        };

        private static readonly (string, string)[] MagnetItems =
        {
            ("nail", "magnetic"), ("pencil", "nonmagnetic"), ("paperclip", "magnetic"), ("leaf2", "nonmagnetic"),
            ("scissors", "magnetic"), ("button", "nonmagnetic"), ("fork", "magnetic"), ("plastic_cup", "nonmagnetic"),
            ("bottle_cap", "magnetic"), ("wooden_block", "nonmagnetic"), ("screw", "magnetic"), ("cotton_ball", "nonmagnetic"),
        };

        private static readonly (string, string)[] LivingVsNonLivingItems =
        {
            ("dog", "living"), ("stone", "nonliving"), ("tree", "living"), ("car", "nonliving"),
            ("flower", "living"), ("chair", "nonliving"), ("goldfish", "living"), ("cloud", "nonliving"),
            ("bird", "living"), ("ball", "nonliving"), ("ant", "living"), ("book", "nonliving"),
            ("cat", "living"), ("table", "nonliving"),
        };

        private static readonly (string, string)[] HealthyVsUnhealthyItems =
        {
            ("apple", "healthy"), ("candy", "unhealthy"), ("broccoli", "healthy"), ("soda", "unhealthy"),
            ("carrot", "healthy"), ("chips", "unhealthy"), ("banana", "healthy"), ("cake", "unhealthy"),
            ("yogurt", "healthy"), ("donut", "unhealthy"), ("grilled_fish", "healthy"), ("fries", "unhealthy"),
            ("salad", "healthy"), ("pizza", "unhealthy"),
        };

        // Sense organ -> the sense it's for. Only five in the human body, so every level plays the same five (the
        // pool table clamps to 6, the builder clamps further to Count = 5).
        private static readonly (string, string)[] HumanSensesItems =
        {
            ("eye", "sight"), ("ear", "hearing"), ("nose", "smell"), ("tongue", "taste"), ("hand", "touch"),
        };

        // Audio-led, same shape as Zoo & Farm's Animal -> Sound (Items(a => a.Id): the value is the item's own id,
        // so distractors are simply the other five weather conditions' own choice sprites).
        private static readonly (string, string)[] WeatherItems =
        {
            ("sunny", "sunny"), ("rainy", "rainy"), ("cloudy", "cloudy"),
            ("snowy", "snowy"), ("windy", "windy"), ("stormy", "stormy"),
        };

        // Weather -> the one clothing item that best fits it.
        private static readonly (string, string)[] DressForWeatherItems =
        {
            ("sunny", "sunhat"), ("rainy", "raincoat"), ("snowy", "mittens"),
            ("windy", "jacket"), ("hot", "shorts"), ("cold", "scarf"),
        };

        // Cause -> its effect.
        private static readonly (string, string)[] CauseAndEffectItems =
        {
            ("rain", "wet_ground"), ("drop_glass", "broken_glass"), ("water_plant", "grown_plant"), ("wind", "flying_kite"),
            ("sun_on_icecream", "melted_icecream"), ("kick_ball", "rolling_ball"), ("press_switch", "light_on"), ("pin_balloon", "popped_balloon"),
        };

        // Three cups per bucket (full/half/empty) - a different cup sprite per row so the same bucket isn't always
        // the same picture, same reasoning as Zoo & Farm's bucket games needing several animals per bucket.
        private static readonly (string, string)[] CookingMeasuresItems =
        {
            ("a", "full"), ("d", "half"), ("g", "empty"),
            ("b", "full"), ("e", "half"), ("h", "empty"),
            ("c", "full"), ("f", "half"), ("i", "empty"),
        };

        // Three activities/scenes per season.
        private static readonly (string, string)[] SeasonsItems =
        {
            ("blooming_flowers", "spring"), ("swimming", "summer"), ("falling_leaves", "fall"), ("building_snowman", "winter"),
            ("planting_seeds", "spring"), ("sandcastle", "summer"), ("picking_apples", "fall"), ("sledding", "winter"),
            ("rainbow", "spring"), ("sunbathing", "summer"), ("raking_leaves", "fall"), ("wearing_coat", "winter"),
        };

        private static readonly (string, string)[] DayNightItems =
        {
            ("sun", "day"), ("moon", "night"), ("breakfast", "day"), ("stars", "night"),
            ("school_bus", "day"), ("sleeping", "night"), ("playing_outside", "day"), ("pajamas", "night"),
            ("daytime_walk", "day"), ("owl", "night"),
        };

        // Audio-led, same shape as Weather.
        private static readonly (string, string)[] SpaceItems =
        {
            ("sun", "sun"), ("earth", "earth"), ("moon", "moon"), ("mars", "mars"),
            ("star", "star"), ("rocket", "rocket"), ("astronaut", "astronaut"), ("saturn", "saturn"),
        };
    }
}
