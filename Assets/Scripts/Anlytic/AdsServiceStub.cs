using UnityEngine;

public interface IAdsService
{
    void ShowRewarded(string placement, System.Action onStarted, System.Action onCompleted, System.Action onRewardGranted);
}



public class AdsServiceStub : IAdsService
{
    public void ShowRewarded(string placement, System.Action onStarted, System.Action onCompleted, System.Action onRewardGranted)
    {
        Debug.Log($"[ADS] ShowRewarded (stub) placement={placement}");
        onStarted?.Invoke();
        // мгновенно “досмотрели”
        onCompleted?.Invoke();
        onRewardGranted?.Invoke();
    }
}