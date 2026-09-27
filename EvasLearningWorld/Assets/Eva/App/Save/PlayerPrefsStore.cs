using MobileGamesFramework.Persistence;
using UnityEngine;

namespace EvasLearningWorld.App
{
    public sealed class PlayerPrefsStore : IKeyValueStore
    {
        public string GetString(string key, string defaultValue) => PlayerPrefs.GetString(key, defaultValue);

        public void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }
    }
}
