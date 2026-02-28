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
    [Range(0f, 1f)][SerializeField] private float threeStarsLeftPercent = 0.60f;
    [Range(0f, 1f)][SerializeField] private float twoStarsLeftPercent = 0.30f;

    [Header("Refs")]
    [SerializeField] private ObjectiveTracker objectives;
    [SerializeField] private GoalUI goalUI;
    [SerializeField] private TimerUI timerUI;
    [SerializeField] private HoleGrowth holeGrowth;
    [SerializeField] private FlyToUiIconSpawner flyToUi;
    [SerializeField] private GoalFinderBoost goalFinderBoost;
    [SerializeField] private WinIntroPopup winIntroPopup;
    [SerializeField] private FreezeTimeBoost freezeTimeBoost;

    [Header("UI")]
    [SerializeField] private WinScreenUI winUI;
    [SerializeField] private LoseScreenUI loseUI;

    [Header("Debug UI (optional)")]
    [SerializeField] private Text debugTimerText;

    public bool IsRunning { get; private set; }

    private float _timeTotal;
    private float _timeLeft;

    private int _revivesUsed;
    private int _freeRevivesUsed;

    private int _coins; // В начале игры ты получаешь сколько коинов есть у игрока, ты можешь их менять во время игры и в конце мне вернешь сколько коинов стало в итоге, я их присваиваю в преф
    private int _buff1Count; // GrowTemp    Так же и с бустами
    private int _buff2Count; // Radar    
    private int _buff3Count; // Magnet    
    private int _buff4Count; // FreezeTime    
    private bool _bonusSpawn; //Спавн бонусных бафов если это 3 из 3 выигранных подряд уровней    
    private int _currentLevel; //Нынешний уровень
    private bool _boost1Activated; //Буст увеличивающий на весь уровень (не временно) размер дыры    
    private bool _boost2Activated; //Буст увеличивающий таймер (1 раз) в начале уровня.    
    private int _collectedBP; //Сколько элементов Батлпаса игрок сожрал за уровень

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
        if (_cfg == null)
        {
            Debug.LogError("RunController.Init: RunConfig is null. Start Game scene from Menu via SceneFlow.StartGame(cfg).");
            return;
        }

        _initialized = true;

        _currentLevel = _cfg.levelIndex;

        _coins = Mathf.Max(0, _cfg.walletCoinsSnapshot);
        _buff1Count = Mathf.Max(0, _cfg.buff1Count);
        _buff2Count = Mathf.Max(0, _cfg.buff2Count);
        _buff3Count = Mathf.Max(0, _cfg.buff3Count);
        _buff4Count = Mathf.Max(0, _cfg.buff4Count);

        _bonusSpawn = _cfg.bonusSpawnActive;
        _boost1Activated = _cfg.boost1Activated;
        _boost2Activated = _cfg.boost2Activated;

        Debug.Log($"Init OK. Level={_cfg.levelIndex} boost1={_cfg.boost1Activated} boost2={_cfg.boost2Activated} bonusSpawn={_cfg.bonusSpawnActive} " + 
            $"buff1={_cfg.buff1Count} buff2={_cfg.buff2Count} buff3={_cfg.buff3Count} buff4={_cfg.buff4Count} coins={_cfg.walletCoinsSnapshot}");
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
        if (minutesOverride > 0f) levelDurationMinutes = minutesOverride; 
    }

    public void StartRun()
    {
        HideScreens();

        _timeTotal = Mathf.Max(1f, levelDurationMinutes * 60f);
        _timeLeft = _timeTotal;

        _revivesUsed = 0;
        _freeRevivesUsed = 0;
        _losePending = false;

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

    // =========================================================
    // БАЛАНСЫ / ЭКОНОМИКА В РАНЕ (главное)
    // =========================================================

    public int GetBuffCount(BuffType type)
    {
        return type switch
        {
            BuffType.GrowTemp => _buff1Count,
            BuffType.Radar => _buff2Count,
            BuffType.Magnet => _buff3Count,
            BuffType.FreezeTime => _buff4Count,
            _ => 0
        };
    }

    public void RegisterBuffUsed(BuffType type)
    {
        switch (type)
        {
            case BuffType.GrowTemp: TryUseBuff(BuffType.GrowTemp, 1); break;
            case BuffType.Radar: TryUseBuff(BuffType.GrowTemp, 1);break;
            case BuffType.Magnet: TryUseBuff(BuffType.GrowTemp, 1); break;
            case BuffType.FreezeTime: TryUseBuff(BuffType.GrowTemp, 1); break;
        }
    }

    /// <summary>
    /// Любое изменение коинов: delta может быть + или -.
    /// Минус НЕ пройдет, если не хватает.
    /// </summary>
    public bool ChangeCoins(int delta)
    {
        if (delta == 0) return true;

        int next = _coins + delta;
        if (next < 0)
        {
            // Не хватает — отказ
            return false;
        }

        _coins = next;
        // TODO: если есть UI кошелька — обновляй здесь
        return true;
    }

    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0) return true;
        return ChangeCoins(-amount);
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        ChangeCoins(amount);
    }

    /// <summary>
    /// Любое изменение бафа: delta может быть + или -.
    /// Минус НЕ пройдет, если не хватает.
    /// </summary>
    public bool ChangeBuff(BuffType type, int delta)
    {
        if (delta == 0) return true;

        ref int slot = ref GetBuffSlotRef(type);

        int next = slot + delta;
        if (next < 0)
            return false;

        slot = next;
        // TODO: если есть UI бафов — обновляй здесь
        return true;
    }

    public bool TryUseBuff(BuffType type, int count = 1)
    {
        if (count <= 0) return true;
        return ChangeBuff(type, -count);
    }

    public void AddBuff(BuffType type, int count = 1)
    {
        if (count <= 0) return;
        ChangeBuff(type, count);
    }

    private ref int GetBuffSlotRef(BuffType type)
    {
        switch (type)
        {
            case BuffType.GrowTemp: return ref _buff1Count;
            case BuffType.Radar: return ref _buff2Count;
            case BuffType.Magnet: return ref _buff3Count;
            case BuffType.FreezeTime: return ref _buff4Count;
            default:
                // Нельзя вернуть ref на локальную, поэтому делаем "фиктивный" слот:
                // но лучше убедись, что BuffType всегда из этих 4.
                throw new System.ArgumentOutOfRangeException(nameof(type), type, "Unknown BuffType");
        }
    }

    // =========================================================
    // Gameplay
    // =========================================================

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

    // =========================================================
    // Win/Lose
    // =========================================================

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
        OnWin(starsEarned: stars, _coins, _collectedBP, _buff1Count, _buff2Count, _buff3Count, _buff4Count);
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

        string reviveText = !canRevive
            ? "No revives"
            : (isFree ? "Revive (FREE)" : "Revive");

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

        OnLose(_coins, _buff1Count, _buff2Count, _buff3Count, _buff4Count);
    }

    private int CalculateStars()
    {
        float leftPercent = (_timeTotal <= 0f) ? 0f : (_timeLeft / _timeTotal);
        if (leftPercent >= threeStarsLeftPercent) return 3;
        if (leftPercent >= twoStarsLeftPercent) return 2;
        return 1;
    }

    // =========================================================
    // Revive
    // =========================================================

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


    public void OnWin(int starsEarned, int coins, int battlepassItems, int buff1, int buff2, int buff3, int buff4)
    {
        if (!_initialized || _ended) return;
        ReturnToMenu(LevelOutcome.Win, starsEarned, coins, battlepassItems, buff1, buff2, buff3, buff4);
    }

    public void OnLose(int coins, int buff1, int buff2, int buff3, int buff4)
    {
        if (!_initialized || _ended) return;
        ReturnToMenu(LevelOutcome.Lose, 0, coins, 0, buff1, buff2, buff3, buff4);
    }

    private void ReturnToMenu(LevelOutcome outcome, int stars, int coins, int bpItems, int buff1, int buff2, int buff3, int buff4)
    {
        _ended = true;

        var result = new LevelResult
        {
            levelIndex = _cfg.levelIndex,
            outcome = outcome,

            starsEarned = (outcome == LevelOutcome.Win) ? Mathf.Max(0, stars) : 0,
            coinsResult = Mathf.Max(0, coins),
            battlepassItemsCollected = Mathf.Max(0, bpItems),
            buff1Count = Mathf.Max(0, buff1),
            buff2Count = Mathf.Max(0, buff2),
            buff3Count = Mathf.Max(0, buff3),
            buff4Count = Mathf.Max(0, buff4),
        };

        Debug.Log(
            $"ReturnToMenu: outcome={outcome} level={result.levelIndex} " +
            $"stars={result.starsEarned} wallet+={result.coinsResult} bpItems={result.battlepassItemsCollected} " +
            $"buffsCount=({result.buff1Count},{result.buff2Count},{result.buff3Count},{result.buff4Count})"
        );

        SceneFlow.ReturnToMenu(result);
    }
}