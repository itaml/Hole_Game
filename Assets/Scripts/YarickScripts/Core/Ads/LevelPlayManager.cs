using UnityEngine;
using Unity.Services.Core;
using Unity.Services.LevelPlay;
using System;
using System.Threading.Tasks;

public sealed class LevelPlayManager : MonoBehaviour
{
    [Header("LevelPlay Settings")]
    [SerializeField] private string appKeyAndroid;
    [SerializeField] private string appKeyIOS;

    [Header("Interstitial Ad Unit IDs")]
    [SerializeField] private string interstitialAdUnitIdAndroid;
    [SerializeField] private string interstitialAdUnitIdIOS;

    public static LevelPlayManager Instance { get; private set; }

    public bool IsInitialized => isInitialized;

    public event Action OnInterstitialShown;
    public event Action OnInterstitialClosed;
    public event Action<string> OnInterstitialShowFailed; // error msg

    private bool isInitialized;
    private bool initStarted;

    private string currentAppKey;
    private string currentInterstitialAdUnitId;

    private LevelPlayInterstitialAd interstitial;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_ANDROID
        currentAppKey = appKeyAndroid;
        currentInterstitialAdUnitId = interstitialAdUnitIdAndroid;
        Debug.Log($"[LevelPlay] Platform Android. AppKey={currentAppKey}");
#elif UNITY_IOS
        currentAppKey = appKeyIOS;
        currentInterstitialAdUnitId = interstitialAdUnitIdIOS;
        Debug.Log($"[LevelPlay] Platform iOS. AppKey={currentAppKey}");
#else
        Debug.LogError("[LevelPlay] Unsupported platform");
#endif
    }

    // ¬ј∆Ќќ: больше не инициализируемс€ автоматически в Start()
    private void Start() { }

    public void SetUserConsent(bool hasConsent)
    {
#if UNITY_IOS || UNITY_ANDROID
        try
        {
            LevelPlay.SetConsent(hasConsent);
            Debug.Log($"[LevelPlay] Consent set: {hasConsent}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[LevelPlay] SetConsent failed: {e.Message}");
        }
#endif
    }

    public async Task InitializeAsync()
    {
        if (isInitialized) return;
        if (initStarted) return;
        initStarted = true;

        if (string.IsNullOrEmpty(currentAppKey))
        {
            Debug.LogError("[LevelPlay] AppKey is empty");
            initStarted = false;
            return;
        }

        if (string.IsNullOrEmpty(currentInterstitialAdUnitId))
        {
            Debug.LogError("[LevelPlay] Interstitial Ad Unit ID is empty");
            // ћожно продолжать init, но без interstitial смысла мало
        }

        await UnityServices.InitializeAsync();
        LevelPlay.ValidateIntegration();

        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;

        Debug.Log($"[LevelPlay] Init with AppKey: {currentAppKey}");
        LevelPlay.Init(currentAppKey);
    }

    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log("[LevelPlay] Initialized successfully");
        isInitialized = true;

        SetupInterstitial();
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogError($"[LevelPlay] Init failed: {error.ErrorMessage}");
        isInitialized = false;
        initStarted = false;
    }

    private void SetupInterstitial()
    {
        if (string.IsNullOrEmpty(currentInterstitialAdUnitId))
            return;

        interstitial = new LevelPlayInterstitialAd(currentInterstitialAdUnitId);

        interstitial.OnAdLoaded += (adInfo) =>
        {
            Debug.Log("[Interstitial] Loaded");
        };

        interstitial.OnAdLoadFailed += (err) =>
        {
            Debug.LogError($"[Interstitial] Load failed: {err.ErrorMessage}");
            // м€гкий ретрай
            Invoke(nameof(LoadInterstitial), 5f);
        };

        interstitial.OnAdDisplayed += (adInfo) =>
        {
            Debug.Log("[Interstitial] Displayed");
            AnalyticsService.LogEvent("interstitial_shown");
            OnInterstitialShown?.Invoke();
        };

        interstitial.OnAdDisplayFailed += (adInfo, err) =>
        {
            Debug.LogError($"[Interstitial] Display failed: {err.ErrorMessage}");
            AnalyticsService.LogEvent("interstitial_show_failed");
            OnInterstitialShowFailed?.Invoke(err.ErrorMessage);

            // после фейла пробуем перезагрузить
            LoadInterstitial();
        };

        interstitial.OnAdClosed += (adInfo) =>
        {
            Debug.Log("[Interstitial] Closed");
            AnalyticsService.LogEvent("interstitial_closed");
            OnInterstitialClosed?.Invoke();

            // подгружаем следующий
            LoadInterstitial();
        };

        LoadInterstitial();
    }

    public void LoadInterstitial()
    {
        if (!isInitialized || interstitial == null) return;
        interstitial.LoadAd();
    }

    public bool IsInterstitialReady()
    {
        return isInitialized && interstitial != null && interstitial.IsAdReady();
    }

    /// <param name="placement">опционально: "level_end", "menu", "fail" Ч дл€ аналитики</param>
    public void ShowInterstitial(string placement = null)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[Interstitial] Not initialized");
            return;
        }

        if (interstitial == null)
        {
            Debug.LogWarning("[Interstitial] Not created");
            return;
        }

        if (!interstitial.IsAdReady())
        {
            Debug.Log("[Interstitial] Not ready, loading...");
            interstitial.LoadAd();
            return;
        }

        if (!string.IsNullOrEmpty(placement))
            AnalyticsService.LogEvent("interstitial_request_" + placement);

        Debug.Log($"[Interstitial] Show (placement={placement})");
        interstitial.ShowAd();
    }

    private void OnDestroy()
    {
        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;

        OnInterstitialShown = null;
        OnInterstitialClosed = null;
        OnInterstitialShowFailed = null;
    }
}