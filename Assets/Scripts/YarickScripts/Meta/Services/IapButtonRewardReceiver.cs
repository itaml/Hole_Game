using UnityEngine;
using UnityEngine.Purchasing;

public class IapButtonRewardReceiver : MonoBehaviour
{
    [SerializeField] private ShopController shopController;

    /// <summary>
    /// Вызывается IAP Button'ом при успешной покупке.
    /// В параметр прилетает Product, из него берём definition.id
    /// </summary>
    public void OnPurchaseComplete(Product product)
    {
        if (shopController == null)
        {
            Debug.LogError("[IAP Receiver] shopController is null");
            return;
        }

        if (product == null)
        {
            Debug.LogError("[IAP Receiver] product is null");
            return;
        }

        string id = product.definition.id;
        shopController.OnPurchaseSucceeded(id);
    }

    /// <summary>На случай, если хочешь логировать ошибки</summary>
    public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
    {
        string id = product != null ? product.definition.id : "(null)";
        Debug.LogWarning($"[IAP Receiver] Purchase failed: {id}, reason={reason}");
    }
}