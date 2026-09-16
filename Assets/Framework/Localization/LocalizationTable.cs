using System.Collections.Generic;

namespace MobileGamesFramework.Localization
{
    public class LocalizationTable
    {
        private readonly Dictionary<string, string> _values;
        private readonly LocalizationTable _fallback;

        public LocalizationTable(Dictionary<string, string> values, LocalizationTable fallback = null)
        {
            _values = values;
            _fallback = fallback;
        }

        // Falls back to the fallback table on a miss, and to the raw key itself if
        // even the fallback doesn't have it - a visibly broken label beats a blank
        // one or a crash, and is easy to spot in a screenshot during verification.
        public string Get(string key)
        {
            if (_values.TryGetValue(key, out var value)) return value;
            if (_fallback != null) return _fallback.Get(key);
            return key;
        }

        public string Get(string key, params object[] args) => string.Format(Get(key), args);
    }
}
