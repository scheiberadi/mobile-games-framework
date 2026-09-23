using System.Collections.Generic;
using MobileGamesFramework.Persistence;

namespace EvasLearningWorld.Tests
{
    // In-memory IKeyValueStore so tests never touch PlayerPrefs.
    public sealed class FakeKeyValueStore : IKeyValueStore
    {
        public readonly Dictionary<string, string> Values = new Dictionary<string, string>();

        public string GetString(string key, string defaultValue) => Values.TryGetValue(key, out var v) ? v : defaultValue;
        public void SetString(string key, string value) => Values[key] = value;
    }
}
