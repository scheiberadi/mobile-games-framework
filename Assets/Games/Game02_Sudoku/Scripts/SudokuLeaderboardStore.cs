using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MobileGamesFramework.Persistence;

namespace Game02_Sudoku
{
    public class SudokuLeaderboardStore
    {
        private const int MaxEntries = 20;

        private readonly IKeyValueStore _store;

        public SudokuLeaderboardStore(IKeyValueStore store)
        {
            _store = store;
        }

        public List<float> GetTimes(Difficulty difficulty)
        {
            var raw = _store.GetString(TimesKey(difficulty), "");
            if (string.IsNullOrEmpty(raw)) return new List<float>();

            return raw.Split(',')
                .Select(s => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : (float?)null)
                .Where(v => v.HasValue)
                .Select(v => v.Value)
                .ToList();
        }

        public int GetCompletedCount(Difficulty difficulty)
        {
            var raw = _store.GetString(CountKey(difficulty), "0");
            return int.TryParse(raw, out var value) ? value : 0;
        }

        public void ReportCompletion(Difficulty difficulty, float elapsedSeconds)
        {
            var times = GetTimes(difficulty);
            times.Add(elapsedSeconds);
            times.Sort();
            if (times.Count > MaxEntries) times.RemoveRange(MaxEntries, times.Count - MaxEntries);

            _store.SetString(TimesKey(difficulty), string.Join(",", times.Select(t => t.ToString(CultureInfo.InvariantCulture))));
            _store.SetString(CountKey(difficulty), (GetCompletedCount(difficulty) + 1).ToString());
        }

        public void ClearTimes(Difficulty difficulty)
        {
            _store.SetString(TimesKey(difficulty), "");
            _store.SetString(CountKey(difficulty), "0");
        }

        private static string TimesKey(Difficulty difficulty) => $"sudoku.leaderboard.{difficulty}";
        private static string CountKey(Difficulty difficulty) => $"sudoku.leaderboard.count.{difficulty}";
    }
}
