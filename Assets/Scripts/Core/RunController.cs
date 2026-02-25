using UnityEngine;

public class RunController : MonoBehaviour
{
    [Header("Run")]
    [SerializeField] private float levelDurationSec = 180f;

    [Header("Refs")]
    [SerializeField] private HoleController hole;
    [SerializeField] private ObjectiveTracker objectives;
    [SerializeField] private TimerUI timerUI;

    public bool IsRunning { get; private set; }

    private float _timeLeft;
    private int _revivesUsed;
    private int _reviveMax;

    private void Start()
    {
        _reviveMax = Sdk.RemoteConfig.GetInt(RemoteKeys.REVIVE_MAX_PER_RUN, 2);
        StartRun();
    }

    public void StartRun()
    {
        IsRunning = true;
        _timeLeft = levelDurationSec;
        _revivesUsed = 0;

        objectives?.Init();

        Sdk.Analytics.LogEvent(AnalyticsEvents.RUN_START);
    }

    private void Update()
    {
        if (!IsRunning) return;

        _timeLeft -= Time.deltaTime;
        timerUI?.Set(_timeLeft, levelDurationSec);

        if (_timeLeft <= 0f)
        {
            _timeLeft = 0f;
            GameOverByTime();
        }
    }

    public void OnItemCollected(AbsorbableItem item)
    {
        if (!item) return;

        if (objectives && objectives.IsGoalItem(item.Type))
        {
            objectives.Add(item.Type, 1);
            if (objectives.IsComplete())
            {
                Win();
            }
        }
    }

    public bool IsGoalItem(ItemType t) => objectives && objectives.IsGoalItem(t);

    // Заглушка: UI-слот для “летящей” иконки
    public RectTransform GetGoalIconTarget(ItemType t)
    {
        // Сделай отдельный UI-скрипт GoalUI, который хранит mapping ItemType->RectTransform
        return null;
    }

    private void Win()
    {
        if (!IsRunning) return;
        IsRunning = false;

        // тут откроешь win screen + предложишь multiplier ad
        // Sdk.Analytics.LogEvent( ... если нужно доп события)
        Debug.Log("WIN");
    }

    private void GameOverByTime()
    {
        if (!IsRunning) return;
        IsRunning = false;

        // показываем revive offer, если не превысили лимит
        if (_revivesUsed < _reviveMax)
        {
            Sdk.Analytics.LogEvent(AnalyticsEvents.REVIVE_OFFER_SHOWN);
            Debug.Log("GAME OVER (time). Revive offer shown.");
        }
        else
        {
            Debug.Log("GAME OVER (time). No revives left.");
        }
    }

    // Вешается на кнопку “Revive”
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

    private void ApplyRevive()
    {
        _revivesUsed++;
        IsRunning = true;
        _timeLeft = Mathf.Max(_timeLeft, 30f); // например, дать 30 секунд, подстроишь
        Debug.Log("REVIVED");
    }

    // Пример буста на таймер (сундук/пауэр-ап)
    public void AddTime(float seconds)
    {
        _timeLeft += Mathf.Max(0f, seconds);
        _timeLeft = Mathf.Min(_timeLeft, levelDurationSec);
    }
}