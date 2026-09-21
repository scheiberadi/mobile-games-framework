using NUnit.Framework;
using MobileGamesFramework.Monetization;

namespace MobileGamesFramework.Tests
{
    public class MockIapProviderTests
    {
        [Test]
        public void IsPurchased_InitiallyFalse()
        {
            var provider = new MockIapProvider();

            Assert.IsFalse(provider.IsPurchased("remove_ads"));
        }

        [Test]
        public void Purchase_InvokesCallbackWithTrueAndMarksAsPurchased()
        {
            var provider = new MockIapProvider();
            var succeeded = false;

            provider.Purchase("remove_ads", result => succeeded = result);

            Assert.IsTrue(succeeded);
            Assert.IsTrue(provider.IsPurchased("remove_ads"));
        }

        [Test]
        public void Purchase_DifferentProductIds_AreIndependent()
        {
            var provider = new MockIapProvider();

            provider.Purchase("remove_ads", _ => { });

            Assert.IsFalse(provider.IsPurchased("extra_lives"));
        }
    }
}
