using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MobileGamesFramework.Persistence;

namespace Game02_Sudoku
{
    public readonly struct LeaderboardEntry
    {
        public readonly float Seconds;
        public readonly DateTime CompletedAt;

        public LeaderboardEntry(float seconds, DateTime completedAt)
        {
            Seconds = seconds;
            CompletedAt = completedAt;
        }
    }

    public class SudokuLeaderboardStore
    {
        private const int MaxEntries = 20;

        private readonly IKeyValueStore _store;

        public SudokuLeaderboardStore(IKeyValueStore store)
        {
            _store = store;
        }

        public List<float> GetTimes(Difficulty difficulty) =>
            GetEntries(difficulty).Select(e => e.Seconds).ToList();

        public List<LeaderboardEntry> GetEntries(Difficulty difficulty)
        {
            var times = ParseTimes(_store.GetString(TimesKey(difficulty), ""));
            var dates = ParseDates(_store.GetString(DatesKey(difficulty), ""));

            // Times recorded before completion dates existed have no matching date entry -
            // fall back to DateTime.MinValue for those rather than dropping the time itself.
            var entries = new List<LeaderboardEntry>(times.Count);
            for (var i = 0; i < times.Count; i++)
                entries.Add(new LeaderboardEntry(times[i], i < dates.Count ? dates[i] : DateTime.MinValue));
            return entries;
        }

        public int GetCompletedCount(Difficulty difficulty)
        {
            var raw = _store.GetString(CountKey(difficulty), "0");
            return int.TryParse(raw, out var value) ? value : 0;
        }

        public void ReportCompletion(Difficulty difficulty, float elapsedSeconds, DateTime? completedAt = null)
        {
            var entries = GetEntries(difficulty);
            entries.Add(new LeaderboardEntry(elapsedSeconds, completedAt ?? DateTime.Now));
            entries = entries.OrderBy(e => e.Seconds).ToList();
            if (entries.Count > MaxEntries) entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);

            _store.SetString(TimesKey(difficulty), string.Join(",", entries.Select(e => e.Seconds.ToString(CultureInfo.InvariantCulture))));
            _store.SetString(DatesKey(difficulty), string.Join(",", entries.Select(e => e.CompletedAt.ToString("o", CultureInfo.InvariantCulture))));
            _store.SetString(CountKey(difficulty), (GetCompletedCount(difficulty) + 1).ToString());
        }

        public void ClearTimes(Difficulty difficulty)
        {
            _store.SetString(TimesKey(difficulty), "");
            _store.SetString(DatesKey(difficulty), "");
            _store.SetString(CountKey(difficulty), "0");
        }

        private static List<float> ParseTimes(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return new List<float>();
            return raw.Split(',')
                .Select(s => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : (float?)null)
                .Where(v => v.HasValue)
                .Select(v => v.Value)
                .ToList();
        }

        private static List<DateTime> ParseDates(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return new List<DateTime>();
            return raw.Split(',')
                .Select(s => DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var v) ? v : (DateTime?)null)
                .Where(v => v.HasValue)
                .Select(v => v.Value)
                .ToList();
        }

        private static string TimesKey(Difficulty difficulty) => $"sudoku.leaderboard.{difficulty}";
        private static string DatesKey(Difficulty difficulty) => $"sudoku.leaderboard.dates.{difficulty}";
        private static string CountKey(Difficulty difficulty) => $"sudoku.leaderboard.count.{difficulty}";
    }
}
