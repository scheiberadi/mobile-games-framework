using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    // Friends' Park (M4.9, docs/kids-games/full-catalogue-plan.md "11. Friends' Park"): a new building for
    // social-emotional skills, listening comprehension and following spoken instructions. Nine of its twelve
    // games reduce to the existing MATCH presenter (Emotion Matching/Facial Expression Game's picture<->word
    // pair, the "scenario -> response" shape What Would You Do/Empathy/Social Situations/Safety Scenarios all
    // share, the audio-led Listen and Choose/Listen for Details pair, and Road Safety - see the design call
    // below). Follow 1/2/3 Instructions reuse SequenceScreen directly (Brain Gym's Sequence Ordering precedent):
    // a fixed-size prefix of one shared action pool, tapped in order while Eva speaks the instruction line -
    // no new presenter needed.
    //
    // Design calls (2026-09-27, same pattern as every earlier building's simplifications, flagged for Adrian's
    // review):
    // - Road Safety is built as a single-round binary MATCH (a traffic-light scene is shown, tap "cross" or
    //   "wait") rather than a timed reaction game where the light actually cycles - reuses the MatchScreen
    //   presenter fully, same shape as Brain Gym's Which Is Bigger.
    // - Follow 1/2/3 Instructions are built on SequenceRoundBuilder/SequenceScreen with a *fixed* instruction
    //   count per game (1, 2 or 3 - the count the game's own name promises) rather than a count that grows
    //   with the difficulty ladder like Brain Gym's Sequence Ordering; the ladder still governs mistakes/hints/
    //   coins as usual. Each round speaks one placeholder instruction line rather than assembling one
    //   dynamically from named actions.
    // - Facial Expression Game keeps Emotion Matching's own six-emotion pool, only with target/choice swapped
    //   (plan's own explicit call: "MATCH, not an open-ended expression-builder").
    // Content across every dataset below is placeholder, pending a real art/content pass, same caveat as every
    // catalogue built this session.

    // --- Follow 1/2/3 Instructions (reuses SequenceScreen directly, same as Brain Gym's Sequence Ordering) ------

    public static class FollowInstructionsRoundGenerator
    {
        public const int RoundsPerSession = 4;
        public const string TileSpritePrefix = "friendspark/action_";

        // A fixed pool of park actions; a round takes a fixed-size prefix (1/2/3), same "prefix of a fixed list"
        // shape as BrainGymSequenceOrderingRoundGenerator/PlantGrowth - see the design call above.
        private static readonly string[] Actions = { "wave_hello", "sit_on_bench", "pick_up_ball", "pet_the_dog", "go_on_swing", "slide_down" };

        public const string InstructionVoicePrefix = "follow_do_";
        private const int MinTiles = 3;
        private const int ExtraDecoys = 2;

        public static IReadOnlyList<string> AllActions => Actions;

        // Eva speaks instructionCount random actions; the board shows those tiles plus decoys (at least three tiles in all), so
        // the child has to listen to know which to tap, and in which order.
        public static SequenceRound Create(int instructionCount, int level, Random rng)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            if (instructionCount < 1 || instructionCount > 3) throw new ArgumentOutOfRangeException(nameof(instructionCount));
            var pool = new List<string>(Actions);
            Shuffle(pool, rng);
            var targets = pool.GetRange(0, instructionCount).ToArray();
            var tileCount = Math.Min(Actions.Length, Math.Max(MinTiles, instructionCount + ExtraDecoys));
            var choices = pool.GetRange(0, tileCount).ToArray();
            Shuffle(choices, rng);
            return new SequenceRound
            {
                Choices = choices,
                TargetOrder = targets,
                SpokenKeys = targets.Select(t => InstructionVoicePrefix + t).ToArray(),
            };
        }

        private static void Shuffle(IList<string> list, Random rng)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }

    // --- MATCH-shaped games (9 of Friends' Park's 12) -------------------------------------------------------------

    public enum FriendsParkMatchGameKind
    {
        EmotionMatching, FacialExpressionGame, WhatWouldYouDo, Empathy, SocialSituations,
        ListenAndChoose, ListenForDetails, RoadSafety, SafetyScenarios,
    }

    public static class FriendsParkMatchRoundGenerator
    {
        public const int RoundsPerSession = 5;

        private static readonly int[] DefaultPoolSizeByLevel = { 4, 5, 6, 6, 6, 6 };
        private static readonly int[] DefaultChoiceCountByLevel = { 2, 3, 3, 4, 4, 4 };
        // Road Safety is a binary judgement (cross/wait), so the choice count never grows past 2 - same shape
        // as Brain Gym's Which Is Bigger.
        private static readonly int[] BinaryPoolSizeByLevel = { 2, 3, 4, 5, 6, 6 };
        private static readonly int[] BinaryChoiceCountByLevel = { 2, 2, 2, 2, 2, 2 };

        public static MatchRound Create(FriendsParkMatchGameKind kind, int level, Random rng, string previousTargetId)
        {
            var config = Config(kind);
            var poolSizes = kind == FriendsParkMatchGameKind.RoadSafety ? BinaryPoolSizeByLevel : DefaultPoolSizeByLevel;
            var choiceCounts = kind == FriendsParkMatchGameKind.RoadSafety ? BinaryChoiceCountByLevel : DefaultChoiceCountByLevel;
            return MatchRoundBuilder.Build(config.items, level, rng, previousTargetId, poolSizes, choiceCounts,
                config.choicePrefix, config.targetPrefix, config.promptKey, config.targetVoicePrefix);
        }

        private static (IReadOnlyList<(string id, string value)> items, string choicePrefix, string targetPrefix,
            string promptKey, string targetVoicePrefix) Config(FriendsParkMatchGameKind kind)
        {
            switch (kind)
            {
                case FriendsParkMatchGameKind.EmotionMatching:
                    // Audio-led (no target picture): Eva names the emotion, child taps the matching face.
                    return (EmotionItems, "friendspark/face_", null,
                        "friendspark_prompt_emotionmatching", "friendspark_emotion_");
                case FriendsParkMatchGameKind.FacialExpressionGame:
                    // Reversed framing of Emotion Matching: a face is shown, child taps the matching emotion icon.
                    return (EmotionItems, "friendspark/emotionicon_", "friendspark/face_",
                        "friendspark_prompt_facialexpression", "friendspark_facial_");
                case FriendsParkMatchGameKind.WhatWouldYouDo:
                    return (WhatWouldYouDoItems, "friendspark/response_", "friendspark/scenario_",
                        "friendspark_prompt_whatwouldyoudo", "friendspark_scenario_");
                case FriendsParkMatchGameKind.Empathy:
                    return (EmpathyItems, "friendspark/response_", "friendspark/scenario_",
                        "friendspark_prompt_empathy", "friendspark_scenario_");
                case FriendsParkMatchGameKind.SocialSituations:
                    return (SocialSituationsItems, "friendspark/response_", "friendspark/scenario_",
                        "friendspark_prompt_socialsituations", "friendspark_scenario_");
                case FriendsParkMatchGameKind.ListenAndChoose:
                    // Audio-led: Eva speaks a sentence, child taps the matching picture.
                    return (ListenAndChooseItems, "friendspark/picture_", null,
                        "friendspark_prompt_listenandchoose", "friendspark_sentence_");
                case FriendsParkMatchGameKind.ListenForDetails:
                    // Same audio-led shape, denser sentences and more similar-looking distractor pictures.
                    return (ListenForDetailsItems, "friendspark/detailpicture_", null,
                        "friendspark_prompt_listenfordetails", "friendspark_detailsentence_");
                case FriendsParkMatchGameKind.RoadSafety:
                    return (RoadSafetyItems, "friendspark/action2_", "friendspark/light_",
                        "friendspark_prompt_roadsafety", "friendspark_light_");
                case FriendsParkMatchGameKind.SafetyScenarios:
                    return (SafetyScenariosItems, "friendspark/response_", "friendspark/scenario_",
                        "friendspark_prompt_safetyscenarios", "friendspark_scenario_");
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        // Self-referential (value == id): the emotion word/icon and the matching face name the same emotion.
        private static readonly (string, string)[] EmotionItems =
        {
            ("happy", "happy"), ("sad", "sad"), ("angry", "angry"), ("scared", "scared"), ("surprised", "surprised"), ("calm", "calm"),
        };

        // Scenario -> the helpful/appropriate response.
        private static readonly (string, string)[] WhatWouldYouDoItems =
        {
            ("friend_falls", "help_up"), ("someone_crying", "comfort"), ("dropped_toy", "pick_up_together"),
            ("cant_reach", "offer_help"), ("someone_excluded", "invite_in"), ("spilled_drink", "help_clean"),
        };

        private static readonly (string, string)[] EmpathyItems =
        {
            ("friend_sad", "ask_whats_wrong"), ("friend_scared", "stay_close"), ("friend_lost_toy", "help_look"),
            ("friend_left_out", "include_them"), ("friend_hurt", "get_grownup"), ("friend_happy", "celebrate_with"),
        };

        private static readonly (string, string)[] SocialSituationsItems =
        {
            ("new_kid", "say_hello"), ("someone_waiting_turn", "wait_patiently"), ("want_to_join", "ask_to_play"),
            ("someone_won", "say_congrats"), ("made_mistake", "say_sorry"), ("someone_shared", "say_thankyou"),
        };

        // Sentence (spoken) -> the matching picture.
        private static readonly (string, string)[] ListenAndChooseItems =
        {
            ("dog_runs", "dog_runs"), ("bird_flies", "bird_flies"), ("girl_jumps", "girl_jumps"),
            ("boy_swings", "boy_swings"), ("cat_sleeps", "cat_sleeps"), ("kids_play_ball", "kids_play_ball"),
        };

        // Denser sentence -> a picture among more similar-looking distractors.
        private static readonly (string, string)[] ListenForDetailsItems =
        {
            ("boy_red_shirt_slide", "boy_red_shirt_slide"), ("girl_blue_dress_swing", "girl_blue_dress_swing"),
            ("dog_brown_ball", "dog_brown_ball"), ("cat_white_bench", "cat_white_bench"),
            ("boy_yellow_hat_sandbox", "boy_yellow_hat_sandbox"), ("girl_green_shoes_seesaw", "girl_green_shoes_seesaw"),
        };

        // Traffic light state -> the correct action.
        private static readonly (string, string)[] RoadSafetyItems =
        {
            ("green_light", "cross"), ("red_light", "wait"),
        };

        // Gentle safety scenario -> the correct response (hot stove, stranger, lost - handled gently per the plan).
        private static readonly (string, string)[] SafetyScenariosItems =
        {
            ("hot_stove", "dont_touch"), ("stranger_offers_candy", "say_no_tell_grownup"), ("lost_in_store", "find_a_helper"),
            ("sharp_scissors", "ask_for_help"), ("busy_road", "hold_a_hand"), ("unknown_medicine", "leave_it_alone"),
        };
    }
}
