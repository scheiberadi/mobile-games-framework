using GoogleMobileAds.Api;
using MobileGamesFramework.Monetization;

namespace Game02_Sudoku
{
    public class AdMobAdProvider : IAdProvider
    {
        // Google's public test ad unit IDs. Replace with real ad unit IDs before publishing.
        private const string InterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
        private const string RewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";

        private InterstitialAd _interstitialAd;
        private RewardedAd _rewardedAd;

        public AdMobAdProvider()
        {
            MobileAds.Initialize(_ =>
            {
                LoadInterstitial();
                LoadRewarded();
            });
        }

        public bool IsInterstitialReady => _interstitialAd != null && _interstitialAd.CanShowAd();

        public void ShowInterstitial()
        {
            if (!IsInterstitialReady) return;
            _interstitialAd.Show();
        }

        public bool IsRewardedReady => _rewardedAd != null && _rewardedAd.CanShowAd();

        public void ShowRewarded(System.Action<bool> onComplete)
        {
            if (!IsRewardedReady)
            {
                onComplete?.Invoke(false);
                return;
            }

            var adToShow = _rewardedAd;
            _rewardedAd = null;
            var earnedReward = false;

            void HandleClosed()
            {
                adToShow.OnAdFullScreenContentClosed -= HandleClosed;
                onComplete?.Invoke(earnedReward);
                LoadRewarded();
            }

            adToShow.OnAdFullScreenContentClosed += HandleClosed;
            adToShow.Show(_ => earnedReward = true);
        }

        private void LoadInterstitial()
        {
            InterstitialAd.Load(InterstitialAdUnitId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null) return;

                _interstitialAd = ad;
                _interstitialAd.OnAdFullScreenContentClosed += LoadInterstitial;
            });
        }

        private void LoadRewarded()
        {
            RewardedAd.Load(RewardedAdUnitId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null) return;
                _rewardedAd = ad;
            });
        }
    }
}
