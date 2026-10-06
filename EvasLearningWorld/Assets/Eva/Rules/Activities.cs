using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public enum BuildingId { School, Playground, ZooFarm, ScienceLab, Workshop, ArtStudio, BrainGym, FriendsPark, Arcade }

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
            // Hidden for now (to be removed): Babies overlaps Mother, Classification repeats the two sort games, Geography is not about animals.
            // new Activity("animal_babies", BuildingId.ZooFarm, "AnimalBabies", "activities/animal_babies", "activity_animal_babies"),
            // new Activity("animal_classification", BuildingId.ZooFarm, "AnimalClassification", "activities/animal_classification", "activity_animal_classification"),
            // new Activity("geography", BuildingId.ZooFarm, "Geography", "activities/geography", "activity_geography"),
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
            // M4.6 Workshop, build order per the plan (tracker doc "8. Workshop"): the assembly presenter first
            // (Build a Car), the rest of the Build-a-X family, then Bridge Building and Simple Physics (its own
            // config of the same presenter - see Rules/Workshop.cs), then the three MATCH-presenter games last.
            new Activity("build_a_car", BuildingId.Workshop, "BuildACar", "activities/build_a_car", "activity_build_a_car"),
            new Activity("build_a_rocket", BuildingId.Workshop, "BuildARocket", "activities/build_a_rocket", "activity_build_a_rocket"),
            new Activity("build_a_house", BuildingId.Workshop, "BuildAHouse", "activities/build_a_house", "activity_build_a_house"),
            new Activity("build_a_boat", BuildingId.Workshop, "BuildABoat", "activities/build_a_boat", "activity_build_a_boat"),
            new Activity("build_a_robot", BuildingId.Workshop, "BuildARobot", "activities/build_a_robot", "activity_build_a_robot"),
            new Activity("bridge_building", BuildingId.Workshop, "BridgeBuilding", "activities/bridge_building", "activity_bridge_building"),
            new Activity("simple_physics", BuildingId.Workshop, "SimplePhysics", "activities/simple_physics", "activity_simple_physics"),
            new Activity("tool_selection", BuildingId.Workshop, "ToolSelection", "activities/tool_selection", "activity_tool_selection"),
            new Activity("balance", BuildingId.Workshop, "Balance", "activities/balance", "activity_balance"),
            new Activity("help_the_character", BuildingId.Workshop, "HelpTheCharacter", "activities/help_the_character", "activity_help_the_character"),
            // M4.7 Art Studio, build order per the plan (tracker doc "9. Art Studio"): Trace Shapes stands up
            // the new TRACE presenter first, then Trace Letters/Numbers reuse it; the four MATCH-presenter
            // games and two SEQUENCE-presenter games follow; Free Drawing (its own small mechanic) closes out
            // the building, same as the plan's own build order.
            new Activity("trace_shapes", BuildingId.ArtStudio, "TraceShapes", "activities/trace_shapes", "activity_trace_shapes"),
            new Activity("trace_letters", BuildingId.ArtStudio, "TraceLetters", "activities/trace_letters", "activity_trace_letters"),
            new Activity("trace_numbers", BuildingId.ArtStudio, "TraceNumbers", "activities/trace_numbers", "activity_trace_numbers"),
            new Activity("color_by_number", BuildingId.ArtStudio, "ColorByNumber", "activities/color_by_number", "activity_color_by_number"),
            new Activity("color_by_instruction", BuildingId.ArtStudio, "ColorByInstruction", "activities/color_by_instruction", "activity_color_by_instruction"),
            new Activity("finish_the_drawing", BuildingId.ArtStudio, "FinishTheDrawing", "activities/finish_the_drawing", "activity_finish_the_drawing"),
            new Activity("draw_what_you_hear", BuildingId.ArtStudio, "DrawWhatYouHear", "activities/draw_what_you_hear", "activity_draw_what_you_hear"),
            new Activity("guided_drawing", BuildingId.ArtStudio, "GuidedDrawing", "activities/guided_drawing", "activity_guided_drawing"),
            new Activity("drawing_challenges", BuildingId.ArtStudio, "DrawingChallenges", "activities/drawing_challenges", "activity_drawing_challenges"),
            new Activity("free_drawing", BuildingId.ArtStudio, "FreeDrawing", "activities/free_drawing", "activity_free_drawing"),
            // M4.8 Brain Gym, build order per the plan (tracker doc "10. Brain Gym"): Classic Memory stands up
            // the new MemoryBoardScreen presenter first, Remember the Sequence stands up the new
            // SequenceRecallScreen next and Simon Says reuses it; the 17 MATCH-presenter games follow; Sequence
            // Ordering (reusing SequenceScreen directly, no new presenter) closes out the building.
            new Activity("classic_memory", BuildingId.BrainGym, "ClassicMemory", "activities/classic_memory", "activity_classic_memory"),
            new Activity("remember_the_sequence", BuildingId.BrainGym, "RememberTheSequence", "activities/remember_the_sequence", "activity_remember_the_sequence"),
            new Activity("simon_says", BuildingId.BrainGym, "SimonSays", "activities/simon_says", "activity_simon_says"),
            new Activity("whats_disappeared", BuildingId.BrainGym, "WhatsDisappeared", "activities/whats_disappeared", "activity_whats_disappeared"),
            new Activity("remember_the_location", BuildingId.BrainGym, "RememberTheLocation", "activities/remember_the_location", "activity_remember_the_location"),
            new Activity("same_or_different", BuildingId.BrainGym, "SameOrDifferent", "activities/same_or_different", "activity_same_or_different"),
            new Activity("match_rotation", BuildingId.BrainGym, "MatchRotation", "activities/match_rotation", "activity_match_rotation"),
            new Activity("which_is_bigger", BuildingId.BrainGym, "WhichIsBigger", "activities/which_is_bigger", "activity_which_is_bigger"),
            new Activity("complete_the_picture", BuildingId.BrainGym, "CompleteThePicture", "activities/complete_the_picture", "activity_complete_the_picture"),
            new Activity("find_the_differences", BuildingId.BrainGym, "FindTheDifferences", "activities/find_the_differences", "activity_find_the_differences"),
            new Activity("spot_the_object", BuildingId.BrainGym, "SpotTheObject", "activities/spot_the_object", "activity_spot_the_object"),
            new Activity("follow_the_path", BuildingId.BrainGym, "FollowThePath", "activities/follow_the_path", "activity_follow_the_path"),
            new Activity("whats_behind", BuildingId.BrainGym, "WhatsBehind", "activities/whats_behind", "activity_whats_behind"),
            new Activity("perspective", BuildingId.BrainGym, "Perspective", "activities/perspective", "activity_perspective"),
            new Activity("copy_the_construction", BuildingId.BrainGym, "CopyTheConstruction", "activities/copy_the_construction", "activity_copy_the_construction"),
            new Activity("find_the_missing_piece", BuildingId.BrainGym, "FindTheMissingPiece", "activities/find_the_missing_piece", "activity_find_the_missing_piece"),
            new Activity("sorting", BuildingId.BrainGym, "Sorting", "activities/sorting", "activity_sorting"),
            new Activity("recycling", BuildingId.BrainGym, "Recycling", "activities/recycling", "activity_recycling"),
            new Activity("match_item_to_category", BuildingId.BrainGym, "MatchItemToCategory", "activities/match_item_to_category", "activity_match_item_to_category"),
            new Activity("sort_laundry_chores", BuildingId.BrainGym, "SortLaundryChores", "activities/sort_laundry_chores", "activity_sort_laundry_chores"),
            new Activity("sequence_ordering_bg", BuildingId.BrainGym, "BrainGymSequenceOrdering", "activities/sequence_ordering_bg", "activity_sequence_ordering_bg"),
            // M4.9 Friends' Park, build order per the plan (tracker doc "11. Friends' Park"): the emotion pair
            // first (Emotion Matching stands up the shared item pool, Facial Expression Game reuses it
            // reversed), then the scenario->response cluster (What Would You Do/Empathy/Social Situations),
            // then the audio-led pair (Listen and Choose/Listen for Details), then Follow 1/2/3 Instructions
            // (reusing SequenceScreen directly - see Rules/FriendsPark.cs), then Road Safety and Safety
            // Scenarios close out the building.
            new Activity("emotion_matching", BuildingId.FriendsPark, "EmotionMatching", "activities/emotion_matching", "activity_emotion_matching"),
            new Activity("facial_expression", BuildingId.FriendsPark, "FacialExpressionGame", "activities/facial_expression", "activity_facial_expression"),
            new Activity("what_would_you_do", BuildingId.FriendsPark, "WhatWouldYouDo", "activities/what_would_you_do", "activity_what_would_you_do"),
            new Activity("empathy", BuildingId.FriendsPark, "Empathy", "activities/empathy", "activity_empathy"),
            new Activity("social_situations", BuildingId.FriendsPark, "SocialSituations", "activities/social_situations", "activity_social_situations"),
            new Activity("listen_and_choose", BuildingId.FriendsPark, "ListenAndChoose", "activities/listen_and_choose", "activity_listen_and_choose"),
            new Activity("listen_for_details", BuildingId.FriendsPark, "ListenForDetails", "activities/listen_for_details", "activity_listen_for_details"),
            new Activity("follow_1_instruction", BuildingId.FriendsPark, "Follow1Instruction", "activities/follow_1_instruction", "activity_follow_1_instruction"),
            new Activity("follow_2_instructions", BuildingId.FriendsPark, "Follow2Instructions", "activities/follow_2_instructions", "activity_follow_2_instructions"),
            new Activity("follow_3_instructions", BuildingId.FriendsPark, "Follow3Instructions", "activities/follow_3_instructions", "activity_follow_3_instructions"),
            new Activity("road_safety", BuildingId.FriendsPark, "RoadSafety", "activities/road_safety", "activity_road_safety"),
            new Activity("safety_scenarios", BuildingId.FriendsPark, "SafetyScenarios", "activities/safety_scenarios", "activity_safety_scenarios"),
            // M4's final building, Arcade (tracker doc "5. Arcade", built last since every game reskins a
            // mechanic built above - see Rules/Arcade.cs). Build order: the six MATCH-shaped games first
            // (Balloon Popping, Whack-a-Mole, Fishing, Space Shooter, Fruit Catcher, Treasure Hunt), then
            // Platformer, the one SEQUENCE-shaped game.
            new Activity("balloon_popping", BuildingId.Arcade, "BalloonPopping", "activities/balloon_popping", "activity_balloon_popping"),
            new Activity("whack_a_mole", BuildingId.Arcade, "WhackAMole", "activities/whack_a_mole", "activity_whack_a_mole"),
            new Activity("fishing", BuildingId.Arcade, "Fishing", "activities/fishing", "activity_fishing"),
            new Activity("space_shooter", BuildingId.Arcade, "SpaceShooter", "activities/space_shooter", "activity_space_shooter"),
            new Activity("fruit_catcher", BuildingId.Arcade, "FruitCatcher", "activities/fruit_catcher", "activity_fruit_catcher"),
            new Activity("treasure_hunt", BuildingId.Arcade, "TreasureHunt", "activities/treasure_hunt", "activity_treasure_hunt"),
            new Activity("platformer", BuildingId.Arcade, "Platformer", "activities/platformer", "activity_platformer"),
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
