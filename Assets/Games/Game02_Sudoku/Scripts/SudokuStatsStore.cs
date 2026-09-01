using System.Globalization;
using MobileGamesFramework.Persistence;

namespace Game02_Sudoku
{
    public class SudokuStatsStore
    {
        private const string TotalCompletedKey = "sudoku.completed.count";

        private readonly IKeyValueStore _store;

        public SudokuStatsStore(IKeyValueStore store)
        {
            _store = store;
        }

        public float? GetBestTimeSeconds(Difficulty difficulty)
        {
            var raw = _store.GetString(BestTimeKey(difficulty), "");
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : (float?)null;
        }

        public int GetTotalCompleted()
        {
            var raw = _store.GetString(TotalCompletedKey, "0");
            return int.TryParse(raw, out var value) ? value : 0;
        }

        public void ReportCompletion(Difficulty difficulty, float elapsedSeconds)
        {
            var best = GetBestTimeSeconds(difficulty);
            if (best == null || elapsedSeconds < best.Value)
                _store.SetString(BestTimeKey(difficulty), elapsedSeconds.ToString(CultureInfo.InvariantCulture));

            _store.SetString(TotalCompletedKey, (GetTotalCompleted() + 1).ToString());
        }

        private static string BestTimeKey(Difficulty difficulty) => $"sudoku.besttime.{difficulty}";
    }
}
