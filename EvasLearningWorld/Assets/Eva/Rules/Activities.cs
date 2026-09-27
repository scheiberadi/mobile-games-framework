using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public enum BuildingId { School, Playground, ZooFarm, ScienceLab }

    // One thing a child can play in a building. ScreenKey is the name of the App layer's ScreenId (a string so this
    // assembly stays engine- and App-free).
    public sealed class Activity
    {
        public Activity(string id, BuildingId building, string screenKey, string iconSprite, string voiceKey)
        {
            Id = id;
            Building = building;
            ScreenKey = screenKey;
            IconSprite = iconSprite;
            VoiceKey = voiceKey;
        }

        public string Id { get; }
        public BuildingId Building { get; }
        public string ScreenKey { get; }
        public string IconSprite { get; }
        public string VoiceKey { get; }
    }

    // The activities of each building, in the fixed order they are shown. No locks and no progress: every entry that
    // exists is playable.
    public static class Activities
    {
        private static readonly Activity[] All =
        {
            new Activity("count", BuildingId.School, "Count", "activities/count", "activity_count"),
            new Activity("numhunt", BuildingId.School, "NumberHunt", "activities/numhunt", "activity_numhunt"),
            new Activity("letterhunt", BuildingId.School, "LetterHunt", "activities/letterhunt", "activity_letterhunt"),
            new Activity("addition", BuildingId.School, "Addition", "activities/addition", "activity_addition"),
            new Activity("subtraction", BuildingId.School, "Subtraction", "activities/subtraction", "activity_subtraction"),
            new Activity("which_has_more", BuildingId.School, "WhichHasMore", "activities/which_has_more", "activity_which_has_more"),
            new Activity("one_more_one_less", BuildingId.School, "OneMoreOneLess", "activities/one_more_one_less", "activity_one_more_one_less"),
            new Activity("number_ordering", BuildingId.School, "NumberOrdering", "activities/number_ordering", "activity_number_ordering"),
            new Activity("missing_number", BuildingId.School, "MissingNumber", "activities/missing_number", "activity_missing_number"),
            new Activity("number_line", BuildingId.School, "NumberLine", "activities/number_line", "activity_number_line"),
            new Activity("multiplication", BuildingId.School, "Multiplication", "activities/multiplication", "activity_multiplication"),
            new Activity("uppercase_to_lowercase", BuildingId.School, "UppercaseToLowercase", "activities/uppercase_to_lowercase", "activity_uppercase_to_lowercase"),
            new Activity("beginning_sound", BuildingId.School, "BeginningSound", "activities/beginning_sound", "activity_beginning_sound"),
            new Activity("rhyming", BuildingId.School, "Rhyming", "activities/rhyming", "activity_rhyming"),
            new Activity("word_to_image", BuildingId.School, "WordToImage", "activities/word_to_image", "activity_word_to_image"),
            new Activity("image_to_word", BuildingId.School, "ImageToWord", "activities/image_to_word", "activity_image_to_word"),
            new Activity("letter_to_sound", BuildingId.School, "LetterToSound", "activities/letter_to_sound", "activity_letter_to_sound"),
            new Activity("missing_letter", BuildingId.School, "MissingLetter", "activities/missing_letter", "activity_missing_letter"),
            new Activity("build_a_word", BuildingId.School, "BuildAWord", "activities/build_a_word", "activity_build_a_word"),
            new Activity("scrambled_word", BuildingId.School, "ScrambledWord", "activities/scrambled_word", "activity_scrambled_word"),
            new Activity("sentence_builder", BuildingId.School, "SentenceBuilder", "activities/sentence_builder", "activity_sentence_builder"),
            // M4.1 Playground, first of its 14 games (build order in the M4 plan): defines the sequence/blank
            // TAP-THE-TARGET shape the rest of the building's cheaper games reuse.
            new Activity("pattern_completion", BuildingId.Playground, "PatternCompletion", "activities/pattern_completion", "activity_pattern_completion"),
            new Activity("odd_one_out", BuildingId.Playground, "OddOneOut", "activities/odd_one_out", "activity_odd_one_out"),
            new Activity("whats_missing", BuildingId.Playground, "WhatsMissing", "activities/whats_missing", "activity_whats_missing"),
            new Activity("which_doesnt_make_sense", BuildingId.Playground, "WhichDoesntMakeSense", "activities/which_doesnt_make_sense", "activity_which_doesnt_make_sense"),
            new Activity("item_to_shadow", BuildingId.Playground, "ItemToShadow", "activities/item_to_shadow", "activity_item_to_shadow"),
            new Activity("finger_maze", BuildingId.Playground, "FingerMaze", "activities/finger_maze", "activity_finger_maze"),
            new Activity("follow_numbers", BuildingId.Playground, "FollowNumbersInOrder", "activities/follow_numbers", "activity_follow_numbers"),
            new Activity("follow_letters", BuildingId.Playground, "FollowLettersInOrder", "activities/follow_letters", "activity_follow_letters"),
            new Activity("shortest_path", BuildingId.Playground, "ShortestPath", "activities/shortest_path", "activity_shortest_path"),
            new Activity("avoid_obstacles", BuildingId.Playground, "AvoidObstacles", "activities/avoid_obstacles", "activity_avoid_obstacles"),
            new Activity("collect_everything", BuildingId.Playground, "CollectEverything", "activities/collect_everything", "activity_collect_everything"),
            new Activity("rotate_piece", BuildingId.Playground, "RotateThePiece", "activities/rotate_piece", "activity_rotate_piece"),
            new Activity("jigsaw", BuildingId.Playground, "Jigsaw", "activities/jigsaw", "activity_jigsaw"),
            new Activity("tangram", BuildingId.Playground, "Tangram", "activities/tangram", "activity_tangram"),
            // M4.4 Zoo & Farm, build order per the plan (Habitat first, defines the shared dataset schema;
            // Classification last, composing the other nine; Geography closes out the building).
            new Activity("zoofarm_habitat", BuildingId.ZooFarm, "ZooFarmHabitat", "activities/zoofarm_habitat", "activity_zoofarm_habitat"),
            new Activity("zoofarm_mother", BuildingId.ZooFarm, "ZooFarmMother", "activities/zoofarm_mother", "activity_zoofarm_mother"),
            new Activity("zoofarm_food", BuildingId.ZooFarm, "ZooFarmFood", "activities/zoofarm_food", "activity_zoofarm_food"),
            new Activity("zoofarm_footprint", BuildingId.ZooFarm, "ZooFarmFootprint", "activities/zoofarm_footprint", "activity_zoofarm_footprint"),
            new Activity("zoofarm_covering", BuildingId.ZooFarm, "ZooFarmCovering", "activities/zoofarm_covering", "activity_zoofarm_covering"),
            new Activity("zoofarm_sound", BuildingId.ZooFarm, "ZooFarmSound", "activities/zoofarm_sound", "activity_zoofarm_sound"),
            new Activity("domestic_vs_wild", BuildingId.ZooFarm, "DomesticVsWild", "activities/domestic_vs_wild", "activity_domestic_vs_wild"),
            new Activity("land_sea_air", BuildingId.ZooFarm, "LandSeaAir", "activities/land_sea_air", "activity_land_sea_air"),
            new Activity("animal_babies", BuildingId.ZooFarm, "AnimalBabies", "activities/animal_babies", "activity_animal_babies"),
            new Activity("animal_classification", BuildingId.ZooFarm, "AnimalClassification", "activities/animal_classification", "activity_animal_classification"),
            new Activity("geography", BuildingId.ZooFarm, "Geography", "activities/geography", "activity_geography"),
            // M4.5 Science Lab, build order per the plan (tracker doc "7. Science Lab").
            new Activity("sink_or_float", BuildingId.ScienceLab, "SinkOrFloat", "activities/sink_or_float", "activity_sink_or_float"),
            new Activity("magnet", BuildingId.ScienceLab, "Magnet", "activities/magnet", "activity_magnet"),
            new Activity("living_vs_nonliving", BuildingId.ScienceLab, "LivingVsNonLiving", "activities/living_vs_nonliving", "activity_living_vs_nonliving"),
            new Activity("plant_growth", BuildingId.ScienceLab, "PlantGrowth", "activities/plant_growth", "activity_plant_growth"),
            new Activity("human_senses", BuildingId.ScienceLab, "HumanSenses", "activities/human_senses", "activity_human_senses"),
            new Activity("healthy_vs_unhealthy", BuildingId.ScienceLab, "HealthyVsUnhealthy", "activities/healthy_vs_unhealthy", "activity_healthy_vs_unhealthy"),
            new Activity("weather", BuildingId.ScienceLab, "Weather", "activities/weather", "activity_weather"),
            new Activity("dress_for_weather", BuildingId.ScienceLab, "DressForWeather", "activities/dress_for_weather", "activity_dress_for_weather"),
            new Activity("cause_and_effect", BuildingId.ScienceLab, "CauseAndEffect", "activities/cause_and_effect", "activity_cause_and_effect"),
            new Activity("cooking_measures", BuildingId.ScienceLab, "CookingMeasures", "activities/cooking_measures", "activity_cooking_measures"),
            new Activity("seasons", BuildingId.ScienceLab, "Seasons", "activities/seasons", "activity_seasons"),
            new Activity("day_night", BuildingId.ScienceLab, "DayNight", "activities/day_night", "activity_day_night"),
            new Activity("space", BuildingId.ScienceLab, "Space", "activities/space", "activity_space"),
        };

        public static IReadOnlyList<Activity> For(BuildingId building) => Filter(building, All);

        public static IReadOnlyList<Activity> Filter(BuildingId building, IReadOnlyList<Activity> source)
        {
            var result = new List<Activity>();
            foreach (var activity in source)
                if (activity.Building == building) result.Add(activity);
            return result;
        }
    }
}
