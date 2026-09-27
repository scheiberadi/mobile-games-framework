using System;

namespace MobileGamesFramework.Monetization
{
    public interface IIapProvider
    {
        bool IsPurchased(string productId);
        void Purchase(string productId, Action<bool> onComplete);
    }
}
