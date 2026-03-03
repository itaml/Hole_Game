using GameBridge.Contracts;
using GameBridge.SceneFlow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunController : MonoBehaviour
{
    public enum LoseReason { TimeUp, Kaboom }

    [Header("Run Time (minutes)")]
    [SerializeField] private float levelDurationMinutes = 2.3f;

    [Header("Revive Limits (total revives per run, TimeUp only)")]
    [SerializeField] private int reviveMaxPerRun = 4;

    [Header("Revive Costs")]
    [SerializeField] private int timeUpPaidReviveCost = 1490;
    [SerializeField] private int kaboomReviveCost = 900;

    [Header("Revive Add Time (seconds)")]
    [SerializeField] private float timeUpFreeAddSeconds = 60f;
    [SerializeField] private float timeUpPaidAddSeconds = 75f;

    [Header("Kaboom (no time add!)")]
    [SerializeField] private float kaboomAddSeconds = 0f; // оставлено, но НЕ используется (кабум не добавляет время)

    [Header("TimeUp Paid Limit")]
    [SerializeField] private int timeUpPaidMaxPerRun = 3;

    [Header("Stars by time (percent of total time left)")]
    [Range(0f, 1f)] [SerializeField] private float threeStarsLeftPercent = 0.60f;
    [Range(0f, 1f)] [SerializeField] private float twoStarsLeftPercent = 0.30f;

    [Header("Refs")]
    [SerializeField] private ObjectiveTracker objectives;
    [SerializeField] private GoalUI goalUI;
    [SerializeField] private TimerUI timerUI;
    [SerializeField] private HoleGrowth holeGrowth;
    [SerializeField] private FlyToUiIconSpawner flyToUi;
    [SerializeField] private GoalFinderBoost goalFinderBoost;
    [SerializeField] private WinIntroPopup winIntroPopup;
    [SerializeField] private FreezeTimeBoost freezeTimeBoost;

    [Header("Buff Inventory (source of truth for buff counts)")]
    [SerializeField] private BuffInventory buffInventory;

    [Header("Dev Fallback (when RunConfig is null)")]
    [SerializeField] private bool useFallbackBuffsWhenNoRunConfig = true;
    [SerializeField] private int fallbackGrowTemp = 5;
    [SerializeField] private int fallbackRadar = 5;
    [SerializeField] private int fallbackMagnet = 5;
    [SerializeField] private int fallbackFreezeTime = 5;

    [Header("UI")]
    [SerializeField] private WinScreenUI winUI;
[SerializeField] private LoseScreenUI loseTimeUpUI;
[SerializeField] private LoseKaboomUI loseKaboomUI;

    [Header("Lose UI Containers")]
    [SerializeField] private GameObject timeUpFreeContainer; // FREE revive
    [SerializeField] private GameObject timeUpPaidContainer; // paid TimeUp revive
    [SerializeField] private GameObject kaboomContainer;     // kaboom revive container

    [Header("TimeUp Art (optional)")]
    [SerializeField] private Image timeUpArtImage;
    [SerializeField] private Sprite[] timeUpArts = new Sprite[4]; // 0..3

    [Header("Coins Text (optional, TMP)")]
    [SerializeField] private TMP_Text[] coinsText;

    [Header("Debug UI (optional)")]
    [SerializeField] private Text debugTimerText;

    public float DefaultDurationMinutes => levelDurationMinutes;
    public bool IsRunning { get; private set; }

    private float _timeTotal;
    private float _timeLeft;

    private int _revivesUsed;       // общий счётчик TimeUp revive (free + paid)
    private int _timeUpPaidUsed;    // платные TimeUp revive (0..3)

    private int _coins;
    private int _collectedBP;

    private bool _losePending;
    private bool _initialized;
    private bool _ended;

    [Header("Boosts")]
[SerializeField] private bool boostsEnabled = true;
public RunConfig PendingConfig => _cfg;

    private RunConfig _cfg;

    // (локально) оставим, чтобы не ломать уже вставленное, но основной контроль через PlayerPrefs
    private int _timeUpFreeRevivesUsed;

    public float TimeLeft => _timeLeft;
    public float TimeTotal => _timeTotal;

    // FREE должен показываться, пока не использовали, и исчезнуть навсегда после использования
    private const string KEY_TIMEUP_FREE_USED = "TimeUpFreeReviveUsed";
    private bool IsTimeUpFreeEverUsed
    {
        get => PlayerPrefs.GetInt(KEY_TIMEUP_FREE_USED, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(KEY_TIMEUP_FREE_USED, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    private void Awake()
    {
        Init();
    }

    public void Init()
    {
        if (_initialized) return;

        _cfg = SceneFlow.PendingRunConfig;

        if (buffInventory == null)
            buffInventory = BuffInventory.Instance;

        if (buffInventory == null)
            buffInventory = FindFirstObjectByType<BuffInventory>();

        if (_cfg == null)
        {
            Debug.LogWarning("[RunController.Init] RunConfig is null (Game scene started directly).");

            if (buffInventory != null && useFallbackBuffsWhenNoRunConfig)
            {
                var fake = new RunConfig
                {
                    buff1Count = fallbackGrowTemp,
                    buff2Count = fallbackRadar,
                    buff3Count = fallbackMagnet,
                    buff4Count = fallbackFreezeTime,
                    walletCoinsSnapshot = 0,
                    levelIndex = 0
                };

                buffInventory.ApplyRunConfig(fake);
                _coins = 0;
            }
            else
            {
                Debug.LogError("[RunController.Init] BuffInventory not found. Boost buttons will stay disabled.");
            }

            _initialized = true;
            UpdateCoinsUI();
            return;
        }

        _initialized = true;

        _coins = Mathf.Max(0, _cfg.walletCoinsSnapshot);

        if (buffInventory != null)
            buffInventory.ApplyRunConfig(_cfg);
        else
            Debug.LogError("[RunController.Init] BuffInventory not found. Boost buttons will stay disabled.");

        UpdateCoinsUI();
    }

    private void OnEnable()
    {
        if (objectives != null)
        {
            objectives.OnRemainingChanged += HandleRemainingChanged;
            objectives.OnGoalCompleted += HandleGoalCompleted;
        }
    }

    private void OnDisable()
    {
        if (objectives != null)
        {
            objectives.OnRemainingChanged -= HandleRemainingChanged;
            objectives.OnGoalCompleted -= HandleGoalCompleted;
        }
    }

    public void ApplyLevelDuration(float minutesOverride)
    {
        if (minutesOverride > 0f)
            levelDurationMinutes = minutesOverride;
    }

 public void StartRun()
{
    HideScreens();

    _timeTotal = Mathf.Max(1f, levelDurationMinutes * 60f);
    _timeLeft = _timeTotal;

    _revivesUsed = 0;
    _losePending = false;
    _ended = false;
    IsRunning = true;

    // если у тебя есть эти поля
    _timeUpPaidUsed = 0;
    _timeUpFreeRevivesUsed = 0;

    holeGrowth?.ResetRun();
    timerUI?.Set(_timeLeft, _timeTotal);
    UpdateDebugTimerText();
    UpdateCoinsUI();
    SetLoseContainers(null);

    // ✅ Battlepass корзина в начале рана
    var bp = FindFirstObjectByType<BattlepassBoostSpawner>();
    if (bp != null)
    {
        bp.ResetForNewRun();
        bp.SpawnIfNeeded();
    }
}

    private void Update()
    {
        if (!IsRunning) return;

        if (freezeTimeBoost == null || !freezeTimeBoost.IsActive)
        {
            _timeLeft -= Time.deltaTime;
            if (_timeLeft < 0f) _timeLeft = 0f;
        }

        timerUI?.Set(_timeLeft, _timeTotal);
        UpdateDebugTimerText();

        if (_timeLeft <= 0f)
            Lose(LoseReason.TimeUp);
    }

    private void UpdateDebugTimerText()
    {
        if (!debugTimerText) return;

        int sec = Mathf.CeilToInt(_timeLeft);
        int m = sec / 60;
        int s = sec % 60;
        debugTimerText.text = $"{m:00}:{s:00}";
    }

    // ---------------- Coins ----------------

    public int Coins => _coins;

    public bool ChangeCoins(int delta)
    {
        int next = _coins + delta;
        if (next < 0) return false;

        _coins = next;
        UpdateCoinsUI();
        return true;
    }

    public bool TrySpendCoins(int amount) => amount <= 0 || ChangeCoins(-amount);

    public void AddCoins(int amount)
    {
        if (amount > 0) ChangeCoins(amount);
    }

    public void AddBattlepassItems(int amount)
    {
        if (amount > 0) _collectedBP += amount;
    }

    private void UpdateCoinsUI()
    {
        if (coinsText != null)
        for (int i = 0; i < coinsText.Length; i++)
        {
                        coinsText[i].text = _coins.ToString();
        }

    }

    // ---------------- Lose containers + art ----------------

    private void SetLoseContainers(LoseReason? reason)
    {
        if (timeUpFreeContainer) timeUpFreeContainer.SetActive(false);
        if (timeUpPaidContainer) timeUpPaidContainer.SetActive(false);
        if (kaboomContainer) kaboomContainer.SetActive(false);

        if (reason == null) return;

        if (reason.Value == LoseReason.Kaboom)
        {
            if (kaboomContainer) kaboomContainer.SetActive(true);
            return;
        }

        // TimeUp
        bool showFree = !IsTimeUpFreeEverUsed;
        if (timeUpFreeContainer) timeUpFreeContainer.SetActive(showFree);
        if (timeUpPaidContainer) timeUpPaidContainer.SetActive(!showFree);
    }

    private void UpdateTimeUpArt()
    {
        if (timeUpArtImage == null) return;
        if (timeUpArts == null || timeUpArts.Length == 0) return;

        // по числу платных попыток: 0..3
        int idx = Mathf.Clamp(_timeUpPaidUsed, 0, 3);

        if (idx < timeUpArts.Length && timeUpArts[idx] != null)
            timeUpArtImage.sprite = timeUpArts[idx];
    }

    // ---------------- Gameplay ----------------

    public void GameOverByBomb()
    {
        if (!IsRunning) return;
        Lose(LoseReason.Kaboom);
    }

public void OnItemCollected(AbsorbablePhysicsItem item)
{
    if (item.TryGetComponent<BoostPickupItem>(out var boost))
{
    ApplyBoostPickup(boost, item);
    return;
}
    Debug.Log($"[Collected] {item.name} type={item.Type} xp={item.XpValue} timeLeft={_timeLeft:F2}");
    if (!IsRunning || item == null) return;

    // фикс: сохраняем позицию сразу (до любых Destroy/Disable внутри других скриптов)
Vector3 pos = holeGrowth != null ? holeGrowth.transform.position : item.transform.position;

    // если XP=0 — спавним "on absorbed" объект/эффект
    if (!item.HasXp)
        item.SpawnOnAbsorbed(pos);

    // 💣 Bomb: проигрыш сразу, без XP и целей
if (item.Type == ItemType.Bomb)
{
    Debug.Log("[Collected] BOMB -> GameOverByBomb()");
    GameOverByBomb();
    return;
}

    // XP только если > 0
    if (item.HasXp)
        holeGrowth?.AddXp(item.XpValue);

    // цели + fly-to
    if (objectives != null && objectives.IsGoalItem(item.Type))
    {
        objectives.Add(item.Type, 1);
        goalFinderBoost?.OnGoalItemCollected(item);

        var target = GetGoalIconTarget(item.Type);
        var slotUi = GetGoalSlotUI(item.Type);

        if (flyToUi != null && target != null && item.UiIcon != null)
        {
            flyToUi.Spawn(
                item.transform.position,
                item.UiIcon,
                target,
                onArrived: () => slotUi?.PlayArrivePunch()
            );
        }

        if (objectives.IsComplete())
            Win();
    }
}

private void ApplyBoostPickup(BoostPickupItem boost, AbsorbablePhysicsItem item)
{
    // эффект "после проглатывания" (если есть)
    Vector3 fxPos = (holeGrowth ? holeGrowth.transform.position : item.transform.position) + Vector3.up * 0.3f;
    if (!item.HasXp) item.SpawnOnAbsorbed(fxPos);

    if (boost.Type == BoostPickupType.GrowWholeLevel)
    {
        holeGrowth?.AddSizeLevels(1); // см. ниже
        return;
    }

    if (boost.Type == BoostPickupType.ExtraLevelTime)
    {
        float add = boost.GetAddSeconds();
        AddExtraTimeSeconds(add);
        return;
    }
}

private void AddExtraTimeSeconds(float addSeconds)
{
    if (addSeconds <= 0f) return;

    // ✅ важно: чтобы добавленное время реально работало всегда,
    // увеличиваем и left, и total (иначе Clamp может съесть прибавку)
    _timeLeft += addSeconds;
    _timeTotal += addSeconds;

    timerUI?.Set(_timeLeft, _timeTotal);
    UpdateDebugTimerText();
}

private void ApplyBoost(BoostPickupItem boost, AbsorbablePhysicsItem item)
{
    if (boost == null) return;

    // эффекты "после проглатывания" (если назначены)
    // спавним в точке дыры, а не item.position, потому что item улетает под землю
    Vector3 fxPos = (holeGrowth ? holeGrowth.transform.position : item.transform.position) + Vector3.up * 0.3f;
    if (!item.HasXp) item.SpawnOnAbsorbed(fxPos);

    switch (boost.Type)
    {
        case BoostPickupType.GrowWholeLevel:
            holeGrowth?.AddSizeLevels(1);
            break;

        case BoostPickupType.ExtraLevelTime:
            AddExtraTimeSeconds(boost.GetAddSeconds());
            break;
    }
}



    public RectTransform GetGoalIconTarget(ItemType t) => goalUI ? goalUI.GetTarget(t) : null;
    public GoalSlotUI GetGoalSlotUI(ItemType t) => goalUI ? goalUI.GetSlotUI(t) : null;

    private void HandleRemainingChanged(ItemType type, int remaining)
    {
        goalUI?.GetSlotUI(type)?.SetRemaining(remaining);
    }

    private void HandleGoalCompleted(ItemType type)
    {
        goalUI?.GetSlotUI(type)?.MarkComplete();
    }

    // ---------------- Win/Lose ----------------

    private void Win()
    {
        if (!IsRunning) return;

        IsRunning = false;
        HideScreens();

        float timeSpent = _timeTotal - _timeLeft;
        int stars = CalculateStars();

        if (winIntroPopup != null)
        {
            winIntroPopup.Show(() =>
            {
                winUI?.Show(stars, timeSpent, onNext: () => ContinueAfterWin(stars));
            });
        }
        else
        {
            winUI?.Show(stars, timeSpent, onNext: () => ContinueAfterWin(stars));
        }
    }

    private void ContinueAfterWin(int stars)
    {
        if (_ended) return;
        ReturnToMenu(LevelOutcome.Win, stars);
    }

    private void Lose(LoseReason reason)
{
    if (!IsRunning) return;

    IsRunning = false;
    HideScreens();
    _losePending = true;

    UpdateCoinsUI();

    if (reason == LoseReason.TimeUp)
    {
        SetLoseContainers(LoseReason.TimeUp);
        UpdateTimeUpArt();

        loseTimeUpUI?.Show(
            "Ooops!! Time's up",
            "Get more time to continue!",
            onFreeRevive: (!IsTimeUpFreeEverUsed && _revivesUsed < reviveMaxPerRun) ? TryRevive_TimeUpFree : null,
            onPaidRevive: (_timeUpPaidUsed < timeUpPaidMaxPerRun && _coins >= timeUpPaidReviveCost && _revivesUsed < reviveMaxPerRun) ? TryRevive_TimeUpPaid : null,
            onKaboomRevive: null,
            onRetry: ContinueAfterLose
        );
    }
    else // Kaboom
    {
        SetLoseContainers(LoseReason.Kaboom);

        bool canAfford = _coins >= kaboomReviveCost;

        loseKaboomUI?.Show(
            onKaboomRevive: canAfford ? TryRevive_Kaboom : null,
            onRetry: ContinueAfterLose
        );
    }
}
    private void ContinueAfterLose()
    {
        if (!_losePending) return;

        _losePending = false;

        if (_ended) return;
        ReturnToMenu(LevelOutcome.Lose, 0);
    }

    private int CalculateStars()
    {
        float leftPercent = (_timeTotal <= 0f) ? 0f : (_timeLeft / _timeTotal);
        if (leftPercent >= threeStarsLeftPercent) return 3;
        if (leftPercent >= twoStarsLeftPercent) return 2;
        return 1;
    }

    // ---------------- Revive handlers ----------------

    private void TryRevive_TimeUpFree()
    {
        if (IsTimeUpFreeEverUsed) return;
        if (_revivesUsed >= reviveMaxPerRun) return;

        // FREE используем навсегда
        IsTimeUpFreeEverUsed = true;
        _timeUpFreeRevivesUsed = 1;

        SetLoseContainers(LoseReason.TimeUp);

        ApplyTimeUpRevive(timeUpFreeAddSeconds); // ✅ +60
    }

    private void TryRevive_TimeUpPaid()
    {
        if (_revivesUsed >= reviveMaxPerRun) return;
        if (_timeUpPaidUsed >= timeUpPaidMaxPerRun) return;

        if (!TrySpendCoins(timeUpPaidReviveCost))
            return;

        _timeUpPaidUsed++;

        ApplyTimeUpRevive(timeUpPaidAddSeconds); // ✅ +75
    }

    private void TryRevive_Kaboom()
    {
        if (!TrySpendCoins(kaboomReviveCost))
            return;

        // ✅ КАБУМ НЕ ДОБАВЛЯЕТ ВРЕМЯ, просто продолжаем
        HideScreens();
        _losePending = false;
        IsRunning = true;

        timerUI?.Set(_timeLeft, _timeTotal);
        UpdateDebugTimerText();
        UpdateCoinsUI();
    }

    // ✅ TimeUp revive: расширяем TOTAL, чтобы бонус не съедал Clamp
    private void ApplyTimeUpRevive(float addTimeSeconds)
    {
        _revivesUsed++;

        _timeTotal += addTimeSeconds;
        _timeLeft += addTimeSeconds;

        HideScreens();
        _losePending = false;
        IsRunning = true;

        timerUI?.Set(_timeLeft, _timeTotal);
        UpdateDebugTimerText();
        UpdateCoinsUI();
    }

private void HideScreens()
{
    winUI?.Hide();
    loseTimeUpUI?.Hide();
    loseKaboomUI?.Hide();
    SetLoseContainers(null);
}

    // ---------------- Return to Menu ----------------

    private void ReturnToMenu(LevelOutcome outcome, int stars)
    {
        _ended = true;

        if (_cfg == null)
        {
            Debug.LogWarning("[RunController] ReturnToMenu called but RunConfig is null (started directly).");
            return;
        }

        int b1 = buffInventory ? buffInventory.GrowTempCount : 0;
        int b2 = buffInventory ? buffInventory.RadarCount : 0;
        int b3 = buffInventory ? buffInventory.MagnetCount : 0;
        int b4 = buffInventory ? buffInventory.FreezeTimeCount : 0;

        var result = new LevelResult
        {
            levelIndex = _cfg.levelIndex,
            outcome = outcome,

            starsEarned = (outcome == LevelOutcome.Win) ? Mathf.Max(0, stars) : 0,
            coinsResult = Mathf.Max(0, _coins),
            battlepassItemsCollected = Mathf.Max(0, _collectedBP),

            buff1Count = Mathf.Max(0, b1),
            buff2Count = Mathf.Max(0, b2),
            buff3Count = Mathf.Max(0, b3),
            buff4Count = Mathf.Max(0, b4),
        };

        SceneFlow.ReturnToMenu(result);
    }
    

    public void QuitToMenuFromPause()
    {
        if (_ended) return;

        IsRunning = false;
        HideScreens();

        ReturnToMenu(LevelOutcome.Lose, 0);
    }
}