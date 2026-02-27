using UnityEngine;
using Unity.Services.LevelPlay;

/// <summary>
/// Interstitial ads via Unity LevelPlay (IronSource).
/// Drop this object into your bootstrap scene (same scene where LevelPlay.Init is called).
/// It will persist across scenes and can be called from Menu/Game:
///     LevelPlayInterstitialController.Instance.Show("after_win_menu");
///
/// Note: This script assumes someone else calls LevelPlay.Init(appKey) (your LevelPlayManager already does).
/// </summary>
public sealed class LevelPlayInterstitialController : MonoBehaviour
{
    [Header("Interstitial Ad Unit IDs")]
    [SerializeField] private string interstitialAdUnitIdAndroid;
    [SerializeField] private string interstitialAdUnitIdIOS;

    public static LevelPlayInterstitialController Instance { get; private set; }

    private string _adUnitId;
    private LevelPlayInterstitialAd _ad;
    private bool _initialized;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_ANDROID
        _adUnitId = interstitialAdUnitIdAndroid;
#elif UNITY_IOS
        _adUnitId = interstitialAdUnitIdIOS;
#else
        _adUnitId = string.Empty;
#endif

        if (string.IsNullOrEmpty(_adUnitId))
            Debug.LogWarning("[LevelPlayInterstitial] AdUnitId is empty for this platform");

        // If LevelPlay already initialized before this object woke up, we can still create/load safely.
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;
    }

    private void OnDestroy()
    {
        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;
    }

    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        _initialized = true;
        CreateIfNeeded();
        Load();
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        _initialized = false;
        Debug.LogWarning($"[LevelPlayInterstitial] Init failed: {error.ErrorMessage}");
    }

    private void CreateIfNeeded()
    {
        if (_ad != null) return;
        if (string.IsNullOrEmpty(_adUnitId)) return;

        _ad = new LevelPlayInterstitialAd(_adUnitId);

        _ad.OnAdLoaded += (info) => Debug.Log("[LevelPlayInterstitial] Loaded");
        _ad.OnAdLoadFailed += (err) =>
        {
            Debug.LogWarning($"[LevelPlayInterstitial] Load failed: {err.ErrorMessage}");
            Invoke(nameof(Load), 5f);
        };

        _ad.OnAdDisplayed += (info) =>
        {
            Debug.Log("[LevelPlayInterstitial] Displayed");
            AnalyticsService.LogEvent("interstitial_shown");
        };

        _ad.OnAdDisplayFailed += (info, err) =>
        {
            Debug.LogWarning($"[LevelPlayInterstitial] Display failed: {err.ErrorMessage}");
            AnalyticsService.LogEvent("interstitial_show_failed");
            Load();
        };

        _ad.OnAdClosed += (info) =>
        {
            Debug.Log("[LevelPlayInterstitial] Closed");
            AnalyticsService.LogEvent("interstitial_closed");
            Load();
        };
    }

    public bool IsReady()
        => _ad != null && _ad.IsAdReady();

    public void Load()
    {
        if (!_initialized) return;
        if (string.IsNullOrEmpty(_adUnitId)) return;
        CreateIfNeeded();
        _ad?.LoadAd();
    }

    public void Show(string placement)
    {
        if (!_initialized)
        {
            Debug.Log("[LevelPlayInterstitial] Not initialized");
            return;
        }

        if (_ad == null)
        {
            Debug.Log("[LevelPlayInterstitial] No ad instance, loading...");
            Load();
            return;
        }

        if (!_ad.IsAdReady())
        {
            Debug.Log("[LevelPlayInterstitial] Not ready, loading...");
            Load();
            return;
        }

        AnalyticsService.LogEvent("interstitial_requested", ("placement", placement));
        _ad.ShowAd();
    }
}
