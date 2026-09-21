using MobileGamesFramework.Persistence;

namespace MobileGamesFramework.Monetization
{
    public class AdsTestSettings
    {
        private const string Key = "debug.adsDisabledForTesting";

        private readonly IKeyValueStore _store;

        public AdsTestSettings(IKeyValueStore store)
        {
            _store = store;
        }

        public bool AdsDisabledForTesting => _store.GetString(Key, "0") == "1";

        public void SetAdsDisabledForTesting(bool disabled) => _store.SetString(Key, disabled ? "1" : "0");
    }
}
