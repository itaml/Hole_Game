using UnityEngine;
using TMPro;

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
    int index = PlayerPrefs.GetInt(PREF_LEVEL_INDEX, debugLevelIndex); // 0-based
    LoadLevel(index);
    run?.StartRun();
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
        Debug.LogError("[LevelDirector] LevelDefinition is null");
        return;
    }

    if (levelText != null)
        levelText.text = $"Level {index + 1}";

    // 🔹 применяем длительность уровня
    run?.ApplyLevelDuration(level.durationMinutesOverride);

    // goals + UI
    goalBuilder?.Build(level.goals, level.catalog);

    // spawn
    spawner?.Spawn(level.spawnConfig, level.catalog);
}
}