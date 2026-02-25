using UnityEngine;
public interface IIapService
{
    void PurchaseDisableAds(System.Action onSuccess, System.Action<string> onFail);
}



public class IapServiceStub : IIapService
{
    public void PurchaseDisableAds(System.Action onSuccess, System.Action<string> onFail)
    {
        Debug.Log("[IAP] PurchaseDisableAds (stub) -> success");
        onSuccess?.Invoke();
    }
}