using MobileGamesFramework.Persistence;

namespace MobileGamesFramework.Monetization
{
    public class InterstitialCadenceTracker
    {
        private readonly IKeyValueStore _store;

        public InterstitialCadenceTracker(IKeyValueStore store)
        {
            _store = store;
        }

        public bool ShouldShowInterstitial(string gameId, int cadence)
        {
            var key = $"completedGames.{gameId}";
            var count = int.TryParse(_store.GetString(key, "0"), out var value) ? value : 0;
            count++;
            _store.SetString(key, count.ToString());
            return count % cadence == 0;
        }
    }
}
