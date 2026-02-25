using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RunController : MonoBehaviour
{
    
    [Header("Run Time (minutes)")]
    [Tooltip("Время уровня в минутах. Например 2.3 = 2 минуты 18 секунд.")]
    [SerializeField] private float levelDurationMinutes = 2.3f;

    [Header("Refs")]
    [SerializeField] private HoleController hole;
    [SerializeField] private ObjectiveTracker objectives;
    [SerializeField] private GoalUI goalUI;
    [SerializeField] private TimerUI timerUI;
    

    [Header("Screens (optional)")]
    [SerializeField] private GameObject winScreen;
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private GameObject reviveOfferScreen;
    [SerializeField] private GameObject multiplierOfferScreen;
    [SerializeField] private GameObject chestOfferScreen;

    [Header("Debug UI (optional)")]
    [SerializeField] private Text debugTimerText; // если хочешь видеть mm:ss без TMP
    

    public bool IsRunning { get; private set; }

    private float _timeTotal;
    private float _timeLeft;

    private int _revivesUsed;
    private int _reviveMax;

    private int _winMultValue;
    private int _chestCooldownSec;
    private int _chestRewardValue;
    private bool _iapDisable;

    private const string CHEST_LAST_TIME_KEY = "chest_last_time_utc"; // PlayerPrefs

    

    private void Awake()
    {
        // Без “магии” и гонок инициализации.
        Sdk.EnsureInitialized();
    }

    private void Start()
    {
        // RC (stub сейчас, позже подменят на Firebase Remote Config)
        _iapDisable = Sdk.RemoteConfig.GetBool(RemoteKeys.IAP_DISABLE, false);
        _reviveMax = Sdk.RemoteConfig.GetInt(RemoteKeys.REVIVE_MAX_PER_RUN, 2);
        _winMultValue = Sdk.RemoteConfig.GetInt(RemoteKeys.WIN_MULT_VALUE, 2);
        _chestCooldownSec = Sdk.RemoteConfig.GetInt(RemoteKeys.CHEST_COOLDOWN_SEC, 300);
        _chestRewardValue = Sdk.RemoteConfig.GetInt(RemoteKeys.CHEST_REWARD_VALUE, 50);

        StartRun();
    }

public void StartRun()
{
    HideAllScreens();

    _timeTotal = Mathf.Max(1f, levelDurationMinutes * 60f);
    _timeLeft = _timeTotal;

    _revivesUsed = 0;
    IsRunning = true;

    if (objectives != null)
        objectives.ResetProgress(); // ✅ вместо Init() можно ResetProgress()

    Sdk.Analytics.LogEvent(AnalyticsEvents.RUN_START);
}

    

    private void Update()
    {
        if (!IsRunning) return;

        _timeLeft -= Time.deltaTime;
        if (_timeLeft < 0f) _timeLeft = 0f;

        timerUI?.Set(_timeLeft, _timeTotal);
        UpdateDebugTimerText();

        if (_timeLeft <= 0f)
        {
            GameOverByTime();
        }
    }

    private void UpdateDebugTimerText()
    {
        if (!debugTimerText) return;

        int sec = Mathf.CeilToInt(_timeLeft);
        int m = sec / 60;
        int s = sec % 60;
        debugTimerText.text = $"{m:00}:{s:00}";
    }

    // Вызывай из AbsorbDetector, когда предмет “пойман” дырой (начал проваливаться)
public void OnItemCollected(AbsorbablePhysicsItem item)
{
    if (!IsRunning || item == null) return;

    // цели
    if (objectives != null && objectives.IsGoalItem(item.Type))
    {
        objectives.Add(item.Type, 1);

        if (objectives.IsComplete())
        {
            Win();
        }
    }
}

    public bool IsGoalItem(ItemType t)
        => objectives != null && objectives.IsGoalItem(t);

    public RectTransform GetGoalIconTarget(ItemType t)
        => goalUI ? goalUI.GetTarget(t) : null;

    #region Run States

    private void Win()
    {
        if (!IsRunning) return;
        IsRunning = false;

        if (winScreen) winScreen.SetActive(true);

        // После победы обычно предлагают multiplier ad
        ShowMultiplierOffer();
    }

    private void GameOverByTime()
    {
        if (!IsRunning) return;
        IsRunning = false;

        // revive offer только если есть попытки
        if (_revivesUsed < _reviveMax)
        {
            ShowReviveOffer();
        }
        else
        {
            ShowGameOver();
        }
    }

    private void ShowGameOver()
    {
        HideAllScreens();
        if (gameOverScreen) gameOverScreen.SetActive(true);
    }

    private void HideAllScreens()
    {
        if (winScreen) winScreen.SetActive(false);
        if (gameOverScreen) gameOverScreen.SetActive(false);
        if (reviveOfferScreen) reviveOfferScreen.SetActive(false);
        if (multiplierOfferScreen) multiplierOfferScreen.SetActive(false);
        if (chestOfferScreen) chestOfferScreen.SetActive(false);
    }

    #endregion

    #region Revive Flow

    private void ShowReviveOffer()
    {
        HideAllScreens();
        if (reviveOfferScreen) reviveOfferScreen.SetActive(true);

        Sdk.Analytics.LogEvent(AnalyticsEvents.REVIVE_OFFER_SHOWN);
    }

    // Вешай на кнопку "Revive"
    public void ReviveClicked()
    {
        if (_revivesUsed >= _reviveMax) return;

        Sdk.Analytics.LogEvent(AnalyticsEvents.REVIVE_CLICKED);

        Sdk.Ads.ShowRewarded(
            placement: "revive",
            onStarted: () => Sdk.Analytics.LogEvent(AnalyticsEvents.REVIVE_AD_STARTED),
            onCompleted: () => Sdk.Analytics.LogEvent(AnalyticsEvents.REVIVE_AD_COMPLETED),
            onRewardGranted: () =>
            {
                Sdk.Analytics.LogEvent(AnalyticsEvents.REVIVE_REWARD_GRANTED);
                ApplyRevive();
            }
        );
    }

    public void ReviveDeclined()
    {
        ShowGameOver();
    }

    private void ApplyRevive()
    {
        _revivesUsed++;

        // Даем минимум 30 секунд (как пример, можешь поменять)
        _timeLeft = Mathf.Max(_timeLeft, 30f);

        HideAllScreens();
        IsRunning = true;
    }

    #endregion

    #region Multiplier Flow (after win)

    private void ShowMultiplierOffer()
    {
        if (!multiplierOfferScreen) return;

        multiplierOfferScreen.SetActive(true);
        Sdk.Analytics.LogEvent(AnalyticsEvents.MULTIPLIER_OFFER_SHOWN);
    }

    public void MultiplierClicked()
    {
        Sdk.Analytics.LogEvent(AnalyticsEvents.MULTIPLIER_CLICKED);

        Sdk.Ads.ShowRewarded(
            placement: "multiplier",
            onStarted: () => Sdk.Analytics.LogEvent(AnalyticsEvents.MULTIPLIER_AD_STARTED),
            onCompleted: () => Sdk.Analytics.LogEvent(AnalyticsEvents.MULTIPLIER_AD_COMPLETED),
            onRewardGranted: () =>
            {
                Sdk.Analytics.LogEvent(AnalyticsEvents.MULTIPLIER_REWARD_GRANTED);

                // Тут применишь множитель награды (например монеты/опыт)
                // value = _winMultValue
                multiplierOfferScreen.SetActive(false);
            }
        );
    }

    public void MultiplierDeclined()
    {
        if (multiplierOfferScreen) multiplierOfferScreen.SetActive(false);
    }

    #endregion

    #region Chest Flow (cooldown + rewarded)

    public void TryShowChestOffer()
    {
        if (!chestOfferScreen) return;

        if (!IsChestAvailable())
            return;

        chestOfferScreen.SetActive(true);
        Sdk.Analytics.LogEvent(AnalyticsEvents.CHEST_OFFER_SHOWN);
    }

    public void ChestClicked()
    {
        if (!IsChestAvailable()) return;

        Sdk.Analytics.LogEvent(AnalyticsEvents.CHEST_CLICKED);

        Sdk.Ads.ShowRewarded(
            placement: "chest",
            onStarted: () => Sdk.Analytics.LogEvent(AnalyticsEvents.CHEST_AD_STARTED),
            onCompleted: () => Sdk.Analytics.LogEvent(AnalyticsEvents.CHEST_AD_COMPLETED),
            onRewardGranted: () =>
            {
                Sdk.Analytics.LogEvent(AnalyticsEvents.CHEST_REWARD_GRANTED);

                GrantChestReward(_chestRewardValue);
                MarkChestClaimedNow();

                if (chestOfferScreen) chestOfferScreen.SetActive(false);
            }
        );
    }

    public void ChestDeclined()
    {
        if (chestOfferScreen) chestOfferScreen.SetActive(false);
    }

    private bool IsChestAvailable()
    {
        long last = (long)PlayerPrefs.GetFloat(CHEST_LAST_TIME_KEY, 0f);
        long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (last <= 0) return true;

        long diff = now - last;
        return diff >= _chestCooldownSec;
    }

    private void MarkChestClaimedNow()
    {
        long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        PlayerPrefs.SetFloat(CHEST_LAST_TIME_KEY, now);
        PlayerPrefs.Save();
    }

    private void GrantChestReward(int value)
    {
        // Заглушка: тут выдача награды (монеты/очки)
        Debug.Log($"[CHEST] Reward granted: {value}");
    }

    #endregion

    #region IAP Disable Ads (stub)

    public bool IsIapDisabledByRemoteConfig() => _iapDisable;

    public void PurchaseDisableAds()
    {
        if (_iapDisable)
        {
            Debug.Log("[IAP] Disabled by RemoteConfig (iap_disable=true).");
            return;
        }

        Sdk.Analytics.LogEvent(AnalyticsEvents.IAP_DISABLE_ADS_START);

        Sdk.Iap.PurchaseDisableAds(
            onSuccess: () =>
            {
                Sdk.Analytics.LogEvent(AnalyticsEvents.IAP_DISABLE_ADS_SUCCESS);
                Debug.Log("[IAP] Disable Ads purchased (stub).");
            },
            onFail: (err) =>
            {
                Debug.Log($"[IAP] Purchase failed: {err}");
            }
        );
    }

    #endregion

    #region Public helpers

    public float GetTimeLeftSeconds() => _timeLeft;
    public float GetTimeTotalSeconds() => _timeTotal;

    public void AddTimeSeconds(float seconds)
    {
        if (!IsRunning) return;
        _timeLeft = Mathf.Clamp(_timeLeft + Mathf.Max(0f, seconds), 0f, _timeTotal);
    }

    #endregion
}