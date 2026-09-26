using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public enum BuildingId { School, Playground }

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
            // School's 8th and last entry that fits inside TileLayout.MaxTiles=8 - same ceiling Playground hit at
            // its own 8th game. Any further School game must NOT be added here until that ceiling is raised or
            // the building list screen is made scrollable/paged (see full-catalogue-plan.md's open flag).
            new Activity("number_ordering", BuildingId.School, "NumberOrdering", "activities/number_ordering", "activity_number_ordering"),
            // M4.1 Playground, first of its 14 games (build order in the M4 plan): defines the sequence/blank
            // TAP-THE-TARGET shape the rest of the building's cheaper games reuse.
            new Activity("pattern_completion", BuildingId.Playground, "PatternCompletion", "activities/pattern_completion", "activity_pattern_completion"),
            new Activity("odd_one_out", BuildingId.Playground, "OddOneOut", "activities/odd_one_out", "activity_odd_one_out"),
            new Activity("whats_missing", BuildingId.Playground, "WhatsMissing", "activities/whats_missing", "activity_whats_missing"),
            new Activity("which_doesnt_make_sense", BuildingId.Playground, "WhichDoesntMakeSense", "activities/which_doesnt_make_sense", "activity_which_doesnt_make_sense"),
            new Activity("item_to_shadow", BuildingId.Playground, "ItemToShadow", "activities/item_to_shadow", "activity_item_to_shadow"),
            new Activity("finger_maze", BuildingId.Playground, "FingerMaze", "activities/finger_maze", "activity_finger_maze"),
            new Activity("follow_numbers", BuildingId.Playground, "FollowNumbersInOrder", "activities/follow_numbers", "activity_follow_numbers"),
            // Playground's 8th and last entry that fits inside TileLayout.MaxTiles=8 - see full-catalogue-plan.md's
            // "Immediate next step" note. Any further Playground game must NOT be added here until that ceiling
            // is raised or the building list screen is made scrollable/paged.
            new Activity("follow_letters", BuildingId.Playground, "FollowLettersInOrder", "activities/follow_letters", "activity_follow_letters"),
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
