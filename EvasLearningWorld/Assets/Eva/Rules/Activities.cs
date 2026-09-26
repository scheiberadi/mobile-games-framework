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
            // M4.1 Playground, first of its 14 games (build order in the M4 plan): defines the sequence/blank
            // TAP-THE-TARGET shape the rest of the building's cheaper games reuse.
            new Activity("pattern_completion", BuildingId.Playground, "PatternCompletion", "activities/pattern_completion", "activity_pattern_completion"),
            new Activity("odd_one_out", BuildingId.Playground, "OddOneOut", "activities/odd_one_out", "activity_odd_one_out"),
            new Activity("whats_missing", BuildingId.Playground, "WhatsMissing", "activities/whats_missing", "activity_whats_missing"),
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
