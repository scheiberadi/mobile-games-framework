using System.Collections.Generic;
using UnityEngine;

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
        // Only the final miss (no fallback left to try) logs a warning - each hop
        // up the fallback chain re-enters this same method, so the deepest call is
        // the only one that can see nothing further is available.
        public string Get(string key)
        {
            if (_values.TryGetValue(key, out var value)) return value;
            if (_fallback != null) return _fallback.Get(key);
            if (Application.isEditor || Debug.isDebugBuild)
                Debug.LogWarning($"Localization key not found in any table: {key}");
            return key;
        }

        // string.Format throws FormatException if the resolved value references a
        // {N} index beyond what args supplies (e.g. a translation edit adds an extra
        // placeholder). The design spec promises this can never crash, so fall back
        // to the raw (unformatted) string instead of propagating the exception.
        public string Get(string key, params object[] args)
        {
            var format = Get(key);
            try
            {
                return string.Format(format, args);
            }
            catch (System.FormatException)
            {
                return format;
            }
        }
    }
}
