using System;
using System.Collections.Generic;

namespace MobileGamesFramework.Monetization
{
    public class MockIapProvider : IIapProvider
    {
        private readonly HashSet<string> _purchased = new HashSet<string>();

        public bool IsPurchased(string productId) => _purchased.Contains(productId);

        public void Purchase(string productId, Action<bool> onComplete)
        {
            _purchased.Add(productId);
            onComplete?.Invoke(true);
        }
    }
}
