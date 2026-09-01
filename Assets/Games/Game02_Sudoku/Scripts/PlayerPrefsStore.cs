using UnityEngine;
using MobileGamesFramework.Persistence;

namespace Game02_Sudoku
{
    public class PlayerPrefsStore : IKeyValueStore
    {
        public string GetString(string key, string defaultValue) => PlayerPrefs.GetString(key, defaultValue);

        public void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }
    }
}
