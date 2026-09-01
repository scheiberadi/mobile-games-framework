using System;
using System.Collections.Generic;
using UnityEngine.Purchasing;
using MobileGamesFramework.Monetization;

namespace Game01_2048
{
    public class UnityIapProvider : IIapProvider, IStoreListener
    {
        private IStoreController _controller;
        private Action<bool> _pendingPurchaseCallback;
        private string _pendingProductId;

        public UnityIapProvider(IEnumerable<string> nonConsumableProductIds)
        {
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            foreach (var productId in nonConsumableProductIds)
                builder.AddProduct(productId, ProductType.NonConsumable);

            UnityPurchasing.Initialize(this, builder);
        }

        public bool IsPurchased(string productId)
        {
            var product = _controller?.products.WithID(productId);
            return product != null && product.hasReceipt;
        }

        public void Purchase(string productId, Action<bool> onComplete)
        {
            if (_controller == null)
            {
                onComplete?.Invoke(false);
                return;
            }

            _pendingProductId = productId;
            _pendingPurchaseCallback = onComplete;
            _controller.InitiatePurchase(productId);
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _controller = controller;
        }

        public void OnInitializeFailed(InitializationFailureReason error) { }
        public void OnInitializeFailed(InitializationFailureReason error, string message = null) { }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            if (args.purchasedProduct.definition.id == _pendingProductId)
            {
                _pendingPurchaseCallback?.Invoke(true);
                _pendingPurchaseCallback = null;
                _pendingProductId = null;
            }

            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            if (product.definition.id == _pendingProductId)
            {
                _pendingPurchaseCallback?.Invoke(false);
                _pendingPurchaseCallback = null;
                _pendingProductId = null;
            }
        }
    }
}
