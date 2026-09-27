namespace MobileGamesFramework.Persistence
{
    public class HighScoreStore
    {
        private readonly IKeyValueStore _store;

        public HighScoreStore(IKeyValueStore store)
        {
            _store = store;
        }

        public int GetHighScore(string gameId)
        {
            var raw = _store.GetString(Key(gameId), "0");
            return int.TryParse(raw, out var value) ? value : 0;
        }

        public void ReportScore(string gameId, int score)
        {
            if (score > GetHighScore(gameId))
                _store.SetString(Key(gameId), score.ToString());
        }

        private static string Key(string gameId) => $"highscore.{gameId}";
    }
}
