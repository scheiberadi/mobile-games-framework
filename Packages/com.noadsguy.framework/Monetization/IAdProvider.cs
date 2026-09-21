using System;

namespace MobileGamesFramework.Monetization
{
    public interface IAdProvider
    {
        bool IsInterstitialReady { get; }
        void ShowInterstitial();

        bool IsRewardedReady { get; }
        void ShowRewarded(Action<bool> onComplete);
    }
}
