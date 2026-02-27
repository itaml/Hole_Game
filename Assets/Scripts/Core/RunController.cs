using UnityEngine;
using UnityEngine.UI;
using GameBridge.Contracts;
using Game; // GameEntry namespace

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

    [Header("Bridge")]
    [SerializeField] private GameEntry gameEntry;

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

    // Per-run usage/spending for LevelResult
    private int _coinsSpentInGame;
    private int _buff1Used; // GrowTemp
    private int _buff2Used; // Radar
    private int _buff3Used; // Magnet
    private int _buff4Used; // FreezeTime

    // Lose pending because revive exists
    private bool _losePending;

    private void Awake()
    {
        if (gameEntry == null)
            gameEntry = FindFirstObjectByType<GameEntry>();

        // ОБЯЗАТЕЛЬНО: подтянуть RunConfig из SceneFlow.PendingRunConfig
        gameEntry?.Init();
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

    public void StartRun()
    {
        HideScreens();

        _timeTotal = Mathf.Max(1f, levelDurationMinutes * 60f);
        _timeLeft = _timeTotal;

        _revivesUsed = 0;
        _freeRevivesUsed = 0;

        _coinsSpentInGame = 0;
        _buff1Used = _buff2Used = _buff3Used = _buff4Used = 0;

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

    public void ApplyLevelDuration(float minutesOverride)
    {
        if (minutesOverride > 0f)
            levelDurationMinutes = minutesOverride;
    }

    private void UpdateDebugTimerText()
    {
        if (!debugTimerText) return;

        int sec = Mathf.CeilToInt(_timeLeft);
        int m = sec / 60;
        int s = sec % 60;
        debugTimerText.text = $"{m:00}:{s:00}";
    }

    // ---------- External loses ----------
    public void GameOverByBomb()
    {
        if (!IsRunning) return;
        Lose(LoseReason.Kaboom);
    }

    // ---------- Collection ----------
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

    // ---------- Goal UI updates ----------
    private void HandleRemainingChanged(ItemType type, int remaining)
    {
        goalUI?.GetSlotUI(type)?.SetRemaining(remaining);
    }

    private void HandleGoalCompleted(ItemType type)
    {
        goalUI?.GetSlotUI(type)?.MarkComplete();
    }

    // ---------- Buff usage reporting ----------
    public void RegisterBuffUsed(BuffType type)
    {
        switch (type)
        {
            case BuffType.GrowTemp:   _buff1Used++; break;
            case BuffType.Radar:      _buff2Used++; break;
            case BuffType.Magnet:     _buff3Used++; break;
            case BuffType.FreezeTime: _buff4Used++; break;
        }
    }

    public void AddCoinsSpentInGame(int amount)
    {
        if (amount <= 0) return;
        _coinsSpentInGame += amount;
    }

    // ---------- Win/Lose ----------
    private void Win()
    {
        if (!IsRunning) return;
        IsRunning = false;

        HideScreens();

        float timeSpent = _timeTotal - _timeLeft;
        int stars = CalculateStars();

        // ВАЖНО: результат НЕ отправляем сразу.
        // Отправим по кнопке Continue, потому что ты так хочешь.

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
        // Здесь возвращаемся в меню через SceneFlow
        if (gameEntry == null)
        {
            Debug.LogError("[RunController] GameEntry is NULL. Can't return to menu.");
            return;
        }

        gameEntry.OnWin(
            starsEarned: stars,
            coinsToWallet: 0,
            coinsToBank: 0,
            battlepassItems: 0,
            coinsSpent: _coinsSpentInGame,
            buff1Used: _buff1Used,
            buff2Used: _buff2Used,
            buff3Used: _buff3Used,
            buff4Used: _buff4Used
        );
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

        if (gameEntry == null)
        {
            Debug.LogError("[RunController] GameEntry is NULL. Can't return to menu.");
            return;
        }

        gameEntry.OnLose();
    }

    private int CalculateStars()
    {
        float leftPercent = (_timeTotal <= 0f) ? 0f : (_timeLeft / _timeTotal);

        if (leftPercent >= threeStarsLeftPercent) return 3;
        if (leftPercent >= twoStarsLeftPercent) return 2;
        return 1;
    }

    // ---------- Revive ----------
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
}