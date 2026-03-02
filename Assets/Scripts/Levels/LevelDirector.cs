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

    [Header("UI")]
    [SerializeField] private TMP_Text levelText;

    private const string PREF_LEVEL_INDEX = "current_level_index";

    private void Start()
    {
        int index = ResolveLevelIndex();
        LoadLevel(index);
        run?.StartRun();
    }

private int ResolveLevelIndex()
{
    RunConfig cfg = SceneFlow.PendingRunConfig;
    if (cfg != null)
    {
        // ✅ Конвертируем в 0-based для LevelSequence
        int zeroBased = Mathf.Max(0, cfg.levelIndex - 1);
        Debug.Log($"[LevelDirector] RunConfig.levelIndex={cfg.levelIndex} -> zeroBased={zeroBased}");
        return zeroBased;
    }

    return PlayerPrefs.GetInt(PREF_LEVEL_INDEX, debugLevelIndex);
}

    public void LoadLevel(int index)
    {
        if (levelSequence == null)
        {
            Debug.LogError("[LevelDirector] levelSequence missing");
            return;
        }

        var level = levelSequence.Get(index);
        if (level == null)
        {
            Debug.LogError($"[LevelDirector] LevelDefinition is null for index={index}. Fallback to 0.");
            index = 0;
            level = levelSequence.Get(index);
            if (level == null)
            {
                Debug.LogError("[LevelDirector] LevelDefinition is null even for index=0");
                return;
            }
        }

        if (levelText != null)
            levelText.text = $"Level {index + 1}";

        // применяем длительность уровня (если override=0, RunController оставит дефолт)
        run?.ApplyLevelDuration(level.durationMinutesOverride);

        // goals + UI
        goalBuilder?.Build(level.goals, level.catalog);

        // spawn
        spawner?.Spawn(level.spawnConfig, level.catalog);
    }
}