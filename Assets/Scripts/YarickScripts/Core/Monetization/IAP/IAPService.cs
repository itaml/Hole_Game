using System;
using UnityEngine;

namespace Core.Monetization.IAP
{
    /// <summary>
    /// Unity IAP wrapper.
    /// Requires Unity In-App Purchasing package. If installed, UNITY_PURCHASING is defined by Unity.
    /// </summary>
    public sealed class IAPService
#if UNITY_PURCHASING
        : UnityEngine.Purchasing.IStoreListener
#endif
    {
        public const string PRODUCT_CLAIM_BANK = "claim_bank";      // example (consumable / non-consumable - your choice)
        public const string PRODUCT_NO_ADS = "no_ads";              // example (non-consumable)
        public const string PRODUCT_INFINITE_LIVES = "infinite_lives_7d"; // example (subscription or consumable time-bonus)

        private bool _initialized;

#if UNITY_PURCHASING
        private UnityEngine.Purchasing.IStoreController _controller;
        private UnityEngine.Purchasing.IExtensionProvider _extensions;
#endif

        public event Action<string> OnPurchaseSucceeded;
        public event Action<string, string> OnPurchaseFailed;

        public bool IsInitialized => _initialized;

        public void Initialize()
        {
#if UNITY_PURCHASING
            if (_initialized) return;

            var builder = UnityEngine.Purchasing.ConfigurationBuilder.Instance(
                UnityEngine.Purchasing.StandardPurchasingModule.Instance());

            // TODO: configure product types correctly for your store design
            builder.AddProduct(PRODUCT_CLAIM_BANK, UnityEngine.Purchasing.ProductType.Consumable);
            builder.AddProduct(PRODUCT_NO_ADS, UnityEngine.Purchasing.ProductType.NonConsumable);
            builder.AddProduct(PRODUCT_INFINITE_LIVES, UnityEngine.Purchasing.ProductType.Consumable);

            UnityEngine.Purchasing.UnityPurchasing.Initialize(this, builder);
#else
            _initialized = true;
            Debug.Log("[IAP] UNITY_PURCHASING not available. IAP will be disabled.");
#endif
        }

        public void Buy(string productId)
        {
#if UNITY_PURCHASING
            if (!_initialized || _controller == null)
            {
                Debug.LogWarning("[IAP] Not initialized");
                OnPurchaseFailed?.Invoke(productId, "not_initialized");
                return;
            }

            _controller.InitiatePurchase(productId);
#else
            OnPurchaseFailed?.Invoke(productId, "iap_disabled");
#endif
        }

#if UNITY_PURCHASING
        public void OnInitialized(UnityEngine.Purchasing.IStoreController controller,
                                  UnityEngine.Purchasing.IExtensionProvider extensions)
        {
            _controller = controller;
            _extensions = extensions;
            _initialized = true;
            Debug.Log("[IAP] Initialized");
        }

        public void OnInitializeFailed(UnityEngine.Purchasing.InitializationFailureReason error)
        {
            _initialized = false;
            Debug.LogWarning("[IAP] Init failed: " + error);
        }

#if UNITY_2020_3_OR_NEWER
        public void OnInitializeFailed(UnityEngine.Purchasing.InitializationFailureReason error, string message)
        {
            _initialized = false;
            Debug.LogWarning("[IAP] Init failed: " + error + " " + message);
        }
#endif

        public UnityEngine.Purchasing.PurchaseProcessingResult ProcessPurchase(UnityEngine.Purchasing.PurchaseEventArgs e)
        {
            Debug.Log("[IAP] Purchase success: " + e.purchasedProduct.definition.id);
            OnPurchaseSucceeded?.Invoke(e.purchasedProduct.definition.id);
            return UnityEngine.Purchasing.PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(UnityEngine.Purchasing.Product product, UnityEngine.Purchasing.PurchaseFailureReason failureReason)
        {
            Debug.LogWarning("[IAP] Purchase failed: " + product.definition.id + " " + failureReason);
            OnPurchaseFailed?.Invoke(product.definition.id, failureReason.ToString());
        }
#endif
    }
}
