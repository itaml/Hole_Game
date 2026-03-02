using UnityEngine;
using TMPro;
using GameBridge.SceneFlow;
using GameBridge.Contracts;

public class LevelDirector : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private LevelSequence levelSequence;
    [SerializeField] private int debugLevelIndex = 0;

    [Header("Scene Refs")]
    [SerializeField] private LevelItemSpawner spawner;
    [SerializeField] private GoalUIBuilder goalBuilder;
    [SerializeField] private RunController run;

    [Header("PreRun Popup")]
    [SerializeField] private PreRunPopupUI preRunPopup;

    [Header("UI")]
    [SerializeField] private TMP_Text levelText;

    private void Start()
    {
        int index = ResolveLevelIndex();

        var level = LoadLevel(index);
        if (level == null) return;

        // ВАЖНО: ран НЕ стартуем сразу.
        // Показываем попап, и только после закрытия запускаем таймер.
        if (preRunPopup != null)
        {
            preRunPopup.Show(level, run != null ? run.DefaultDurationMinutes : 2.3f, () =>
            {
                run?.StartRun();
            });
        }
        else
        {
            run?.StartRun();
        }
    }

    private int ResolveLevelIndex()
    {
        RunConfig cfg = SceneFlow.PendingRunConfig;
        if (cfg != null)
            return Mathf.Max(0, cfg.levelIndex);

        return debugLevelIndex;
    }

    private LevelDefinition LoadLevel(int index)
    {
        if (levelSequence == null)
        {
            Debug.LogError("[LevelDirector] levelSequence missing");
            return null;
        }

        var level = levelSequence.Get(index);
        if (level == null)
        {
            Debug.LogError($"[LevelDirector] LevelDefinition is null for index={index}");
            return null;
        }

        if (levelText != null)
            levelText.text = $"Level {index + 1}";

        // применяем длительность уровня в RunController (если override=0 — не трогаем)
        run?.ApplyLevelDuration(level.durationMinutesOverride);

        // goals + UI
        goalBuilder?.Build(level.goals, level.catalog);

        // spawn
        spawner?.Spawn(level.spawnConfig, level.catalog);

        return level;
    }
}