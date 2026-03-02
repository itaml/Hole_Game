using System;
using GameBridge.Contracts;
using GameBridge.SceneFlow;
using UnityEngine;
using UnityEngine.UI;

public class RunController : MonoBehaviour
{
    public enum LoseReason { TimeUp, Kaboom }

    [Header("Run Time (minutes)")]
    [SerializeField] private float levelDurationMinutes = 2.3f;

    [Header("Revive")]
    [SerializeField] private int reviveMaxPerRun = 4;
    [SerializeField] private int freeRevivesPerRun = 1;
    [SerializeField] private float reviveAddTimeSeconds = 30f;

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
    [SerializeField] private LoseScreenUI loseUI;

    [Header("Debug UI (optional)")]
    [SerializeField] private Text debugTimerText;
    public float DefaultDurationMinutes => levelDurationMinutes;

    public bool IsRunning { get; private set; }

    private float _timeTotal;
    private float _timeLeft;

    private int _revivesUsed;
    private int _freeRevivesUsed;

    private int _coins;
    private int _collectedBP;

    private bool _losePending;

    private RunConfig _cfg;
    private bool _initialized;
    private bool _ended;

    private void Awake()
    {
        Init();
    }

    public void Init()
    {
        if (_initialized) return;

        _cfg = SceneFlow.PendingRunConfig;

        // ✅ важное: всегда держим ссылку на SINGLETON,
        // чтобы кнопки и RunController работали с одним инвентарём
        if (buffInventory == null)
            buffInventory = BuffInventory.Instance;

        if (buffInventory == null)
            buffInventory = FindFirstObjectByType<BuffInventory>(); // fallback, если Instance не настроен

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

                Debug.LogWarning($"[RunController.Init] Using FALLBACK buffs: ({fallbackGrowTemp},{fallbackRadar},{fallbackMagnet},{fallbackFreezeTime})");
            }
            else
            {
                Debug.LogError("[RunController.Init] BuffInventory not found. Boost buttons will stay disabled.");
            }

            _initialized = true;
            return;
        }

        _initialized = true;

        _coins = Mathf.Max(0, _cfg.walletCoinsSnapshot);

        if (buffInventory != null)
            buffInventory.ApplyRunConfig(_cfg);
        else
            Debug.LogError("[RunController.Init] BuffInventory not found. Boost buttons will stay disabled.");

        Debug.Log($"[RunController.Init] OK. Level={_cfg.levelIndex} bonusSpawn={_cfg.bonusSpawnLevel} " +
                  $"buffs=({_cfg.buff1Count},{_cfg.buff2Count},{_cfg.buff3Count},{_cfg.buff4Count}) coins={_cfg.walletCoinsSnapshot}");
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
        _freeRevivesUsed = 0;
        _losePending = false;
        _ended = false;

        IsRunning = true;

        holeGrowth?.ResetRun();
        timerUI?.Set(_timeLeft, _timeTotal);
        UpdateDebugTimerText();
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
        return true;
    }

    public bool TrySpendCoins(int amount) => amount <= 0 || ChangeCoins(-amount);
    public void AddCoins(int amount) { if (amount > 0) ChangeCoins(amount); }

    public void AddBattlepassItems(int amount)
    {
        if (amount > 0) _collectedBP += amount;
    }

    // ---------------- Gameplay ----------------

    public void GameOverByBomb()
    {
        if (!IsRunning) return;
        Lose(LoseReason.Kaboom);
    }

    public void OnItemCollected(AbsorbablePhysicsItem item)
    {
        if (!IsRunning || item == null) return;

        holeGrowth?.AddXp(item.XpValue);

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

        bool canRevive = _revivesUsed < reviveMaxPerRun;
        bool isFree = _freeRevivesUsed < freeRevivesPerRun;

        string title = reason == LoseReason.TimeUp ? "Oops! Time's up!" : "Kaboom!";
        string subtitle = reason == LoseReason.TimeUp ? "Get more time to continue!" : "Use a revive to keep playing!";
        string reviveText = !canRevive ? "No revives" : (isFree ? "Revive (FREE)" : "Revive");

        loseUI?.Show(
            title, subtitle,
            canRevive,
            reviveText,
            onRevive: () => TryRevive(isFree),
            onRetry: ContinueAfterLose
        );
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

    // ---------------- Revive ----------------

    private void TryRevive(bool free)
    {
        if (_revivesUsed >= reviveMaxPerRun) return;

        if (free)
        {
            _freeRevivesUsed++;
            ApplyRevive();
            return;
        }

        // TODO: rewarded ad / purchase
        ApplyRevive();
    }

    private void ApplyRevive()
    {
        _revivesUsed++;

        _timeLeft = Mathf.Clamp(_timeLeft + reviveAddTimeSeconds, 0f, _timeTotal);
        HideScreens();

        _losePending = false;
        IsRunning = true;
    }

    private void HideScreens()
    {
        winUI?.Hide();
        loseUI?.Hide();
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

        // ✅ читаем остатки ТОЛЬКО из BuffInventory
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

        Debug.Log($"[RunController] ReturnToMenu outcome={outcome} level={result.levelIndex} " +
                  $"stars={result.starsEarned} coins={result.coinsResult} bp={result.battlepassItemsCollected} " +
                  $"buffs=({result.buff1Count},{result.buff2Count},{result.buff3Count},{result.buff4Count})");

        SceneFlow.ReturnToMenu(result);
    }
}