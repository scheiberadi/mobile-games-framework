using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public enum BuildingId { School }

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
