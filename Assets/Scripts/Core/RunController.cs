using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using GameBridge.Contracts;

public interface ILevelResultReceiver
{
    void SubmitLevelResult(LevelResult result);
}

public class RunController : MonoBehaviour
{
    public enum LoseReason { TimeUp, Kaboom }

    private const string PREF_LEVEL_INDEX = "current_level_index";

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

    [Header("Bridge (Menu will process LevelResult)")]
    [Tooltip("Объект (обычно живущий через DontDestroyOnLoad), который примет LevelResult.")]
    [SerializeField] private MonoBehaviour levelResultReceiverBehaviour;

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

    // Result tracking
    private ILevelResultReceiver _receiver;
    private bool _resultSent;

    // Lose is pending until player chooses Retry (because revive exists)
    private bool _losePending;

    // Spending/usage for LevelResult
    private int _coinsSpentInGame;

    private int _buff1Used; // GrowTemp
    private int _buff2Used; // Radar
    private int _buff3Used; // Magnet
    private int _buff4Used; // FreezeTime

    private void Awake()
    {
        _receiver = levelResultReceiverBehaviour as ILevelResultReceiver;
        if (_receiver == null && levelResultReceiverBehaviour != null)
            Debug.LogError("[RunController] levelResultReceiverBehaviour does not implement ILevelResultReceiver.");
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

    // Called by LevelDirector after LoadLevel() (duration override already applied)
    public void StartRun()
    {
        HideScreens();

        _timeTotal = Mathf.Max(1f, levelDurationMinutes * 60f);
        _timeLeft = _timeTotal;

        _revivesUsed = 0;
        _freeRevivesUsed = 0;

        _resultSent = false;
        _losePending = false;

        _coinsSpentInGame = 0;
        _buff1Used = _buff2Used = _buff3Used = _buff4Used = 0;

        IsRunning = true;

        holeGrowth?.ResetRun();

        timerUI?.Set(_timeLeft, _timeTotal);
        UpdateDebugTimerText();
    }

    private void Update()
    {
        if (!IsRunning) return;

        // FreezeTimeBoost freezes only the run timer
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

    // ---------- Level duration override (from LevelDefinition) ----------
    public void ApplyLevelDuration(float minutesOverride)
    {
        if (minutesOverride > 0f)
            levelDurationMinutes = minutesOverride;
        // if 0 -> keep default
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

        // XP always
        holeGrowth?.AddXp(item.XpValue);

        // goals only for goal-item
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

    public bool IsGoalItem(ItemType t) => objectives != null && objectives.IsGoalItem(t);
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
    // Call this from boost buttons AFTER successful TryConsume(...)
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

    // Optional: call when player spends coins in game (paid continue)
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

        // WIN is final -> send result immediately
        SubmitLevelResult(LevelOutcome.Win, stars);

        // keep your scene reload flow
        SaveCurrentLevelIndex();

        if (winIntroPopup != null)
        {
            winIntroPopup.Show(() =>
            {
                winUI?.Show(stars, timeSpent, onNext: ContinueNextLevel);
            });
        }
        else
        {
            winUI?.Show(stars, timeSpent, onNext: ContinueNextLevel);
        }
    }

    private void Lose(LoseReason reason)
    {
        if (!IsRunning) return;

        // Do NOT send result here (revive exists)
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
            onRetry: FinalizeLoseAndReload
        );
    }

    private void FinalizeLoseAndReload()
    {
        if (_losePending)
        {
            SubmitLevelResult(LevelOutcome.Lose, 0);
            _losePending = false;
        }

        ReloadCurrentLevelScene();
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

        _losePending = false; // revive cancels final lose
        IsRunning = true;
    }

    // ---------- Scene reload / next level ----------
    private void ContinueNextLevel()
    {
        LevelProgress.NextLevel(); // your system (likely 1-based)
        SaveCurrentLevelIndex();   // store 0-based for LevelDirector
        ReloadScene();
    }

    private void ReloadCurrentLevelScene()
    {
        SaveCurrentLevelIndex();
        ReloadScene();
    }

    private void ReloadScene()
    {
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    private void SaveCurrentLevelIndex()
    {
        // Your LevelDirector loads 0-based index
        int zeroBased = Mathf.Max(0, LevelProgress.CurrentLevelIndex - 1);
        PlayerPrefs.SetInt(PREF_LEVEL_INDEX, zeroBased);
        PlayerPrefs.Save();
    }

    // ---------- LevelResult submit ----------
    private void SubmitLevelResult(LevelOutcome outcome, int starsEarned)
    {
        if (_resultSent) return;
        _resultSent = true;

        var result = new LevelResult();

        // levelIndex must match menu logic. We use 0-based, same as LevelDirector.
        result.levelIndex = PlayerPrefs.GetInt(PREF_LEVEL_INDEX, 0);

        result.outcome = outcome;

        result.starsEarned = Mathf.Clamp(starsEarned, 0, 3);
        result.coinsEarnedToWallet = 0;
        result.coinsEarnedToBank = 0;
        result.battlepassItemsCollected = 0;

        result.coinsSpentInGame = Mathf.Max(0, _coinsSpentInGame);

        result.buff1Used = Mathf.Max(0, _buff1Used);
        result.buff2Used = Mathf.Max(0, _buff2Used);
        result.buff3Used = Mathf.Max(0, _buff3Used);
        result.buff4Used = Mathf.Max(0, _buff4Used);

        if (_receiver == null)
        {
            Debug.LogWarning("[RunController] No ILevelResultReceiver assigned. LevelResult not sent.");
            return;
        }

        _receiver.SubmitLevelResult(result);
    }

    private void HideScreens()
    {
        winUI?.Hide();
        loseUI?.Hide();
    }
}