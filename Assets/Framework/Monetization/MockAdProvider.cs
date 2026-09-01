using System;

namespace MobileGamesFramework.Monetization
{
    public class MockAdProvider : IAdProvider
    {
        public bool IsInterstitialReady => true;
        public void ShowInterstitial() { }

        public bool IsRewardedReady => true;
        public void ShowRewarded(Action<bool> onComplete) => onComplete?.Invoke(true);
    }
}
