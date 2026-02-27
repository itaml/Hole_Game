using UnityEngine;
using Core.Bootstrap;
using Meta.Services;

namespace Menu.UI
{
    /// <summary>
    /// Simple glue between Menu UI buttons and IAP service.
    /// Attach to a Menu GameObject and assign MenuRoot.
    /// </summary>
    public sealed class MenuIapController : MonoBehaviour
    {
        [SerializeField] private MenuRoot menuRoot;

        private MetaFacade _meta;

        private void Awake()
        {
            if (menuRoot == null) menuRoot = FindFirstObjectByType<MenuRoot>();
            _meta = menuRoot != null ? menuRoot.Meta : null;

            AppServices.EnsureExists();

            // Subscribe to purchase callbacks
            AppServices.IAP.OnPurchaseSucceeded += HandlePurchaseSuccess;
            AppServices.IAP.OnPurchaseFailed += HandlePurchaseFailed;
        }

        private void OnDestroy()
        {
            if (AppServices.Instance == null) return;
            AppServices.IAP.OnPurchaseSucceeded -= HandlePurchaseSuccess;
            AppServices.IAP.OnPurchaseFailed -= HandlePurchaseFailed;
        }

        // ---------- UI buttons ----------

        public void OnClickBuyClaimBank()
        {
            AnalyticsService.LogEvent("Product Claim Bank Buyed");
            AppServices.IAP.Buy(Core.Monetization.IAP.IAPService.PRODUCT_CLAIM_BANK);
        }

        public void OnClickBuyInfiniteLives7d()
        {
            AnalyticsService.LogEvent("Product Infinite Lives Buyed");
            AppServices.IAP.Buy(Core.Monetization.IAP.IAPService.PRODUCT_INFINITE_LIVES);
        }

        // ---------- callbacks ----------

        private void HandlePurchaseSuccess(string productId)
        {
            if (_meta == null)
            {
                Debug.LogWarning("[MenuIap] MetaFacade not found");
                return;
            }

            if (productId == Core.Monetization.IAP.IAPService.PRODUCT_CLAIM_BANK)
            {
                int bonus = AppServices.RemoteConfig.GetInt("iap_claim_bank_bonus", 0);
                _meta.ClaimBankIap(bonus);
                Debug.Log("[MenuIap] Bank claimed. Bonus: " + bonus);
            }
            else if (productId == Core.Monetization.IAP.IAPService.PRODUCT_INFINITE_LIVES)
            {
                int days = AppServices.RemoteConfig.GetInt("iap_infinite_lives_days", 7);
                _meta.GrantInfiniteLives(System.TimeSpan.FromDays(days));
                Debug.Log("[MenuIap] Infinite lives granted: " + days + "d");
            }
        }

        private void HandlePurchaseFailed(string productId, string reason)
        {
            Debug.LogWarning("[MenuIap] Purchase failed: " + productId + " reason=" + reason);
        }
    }
}
