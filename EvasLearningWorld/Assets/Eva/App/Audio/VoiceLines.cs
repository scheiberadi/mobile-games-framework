using System.Collections.Generic;
using UnityEngine;

namespace EvasLearningWorld.App
{
    // The text of Eva's spoken lines, shown only in the optional speech bubble.
    public static class VoiceLines
    {
        private const string ResourcePath = "Voice/voice-lines";
        private static Dictionary<string, string> _loaded;

        // One "key<TAB>English text" per line. Comments (#) and blank lines are ignored; the split is on the first tab only.
        public static Dictionary<string, string> Parse(string source)
        {
            var lines = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(source)) return lines;
            foreach (var raw in source.Split('\n'))
            {
                var line = raw.TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;
                var tab = line.IndexOf('\t');
                if (tab <= 0) continue;
                var key = line.Substring(0, tab).Trim();
                if (key.Length == 0) continue;
                lines[key] = line.Substring(tab + 1).Trim();
            }
            return lines;
        }

        public static string TextFor(string key)
        {
            if (_loaded == null)
            {
                var asset = Resources.Load<TextAsset>(ResourcePath);
                _loaded = Parse(asset != null ? asset.text : string.Empty);
            }
            return TextFor(key, _loaded);
        }

        public static string TextFor(string key, IReadOnlyDictionary<string, string> lines)
        {
            if (key == null) return string.Empty;
            return lines != null && lines.TryGetValue(key, out var text) ? text : key;
        }
    }
}
