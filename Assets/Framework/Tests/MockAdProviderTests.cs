using NUnit.Framework;
using MobileGamesFramework.Monetization;

namespace MobileGamesFramework.Tests
{
    public class MockAdProviderTests
    {
        [Test]
        public void IsInterstitialReady_IsTrue()
        {
            var provider = new MockAdProvider();

            Assert.IsTrue(provider.IsInterstitialReady);
        }

        [Test]
        public void IsRewardedReady_IsTrue()
        {
            var provider = new MockAdProvider();

            Assert.IsTrue(provider.IsRewardedReady);
        }

        [Test]
        public void ShowRewarded_InvokesCallbackWithTrue()
        {
            var provider = new MockAdProvider();
            var granted = false;

            provider.ShowRewarded(result => granted = result);

            Assert.IsTrue(granted);
        }
    }
}
