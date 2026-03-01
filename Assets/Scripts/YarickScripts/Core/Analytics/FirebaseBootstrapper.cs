using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Analytics;

public class FirebaseBootstrapper : MonoBehaviour
{
    public static FirebaseBootstrapper Instance { get; private set; }
    public bool IsReady { get; private set; }
    public System.Action OnReady;

    private async void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
        Debug.Log("[Firebase] Editor mode - simulating ready state");
        IsReady = true;
        OnReady?.Invoke();
        return;
#endif

        await InitFirebaseAsync();
    }

    private async Task InitFirebaseAsync()
    {
        IsReady = false;

        var dep = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dep != DependencyStatus.Available)
        {
            Debug.LogError($"[Firebase] Dependencies not available: {dep}");
            return;
        }

        FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
        IsReady = true;
        OnReady?.Invoke();
        Debug.Log("[Firebase] Ready");
    }
}