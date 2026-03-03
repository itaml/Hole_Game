using UnityEngine;
using TMPro;
using GameBridge.SceneFlow;
using GameBridge.Contracts;
using System.Collections.Generic;

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

    [Header("Procedural (Levels 2+)")]
    [SerializeField] private ProceduralSpawnBuilder proceduralBuilder;
    [SerializeField] private ProceduralGoalBuilder proceduralGoals;

    [Header("Indexing")]
    [Tooltip("Если PendingRunConfig.levelIndex приходит 1..N (1-based) — включи. Если 0..N-1 — выключи.")]
    [SerializeField] private bool pendingRunConfigIsOneBased = true;

    [Header("UI")]
    [SerializeField] private TMP_Text levelText;

    public int CurrentLevelIndex { get; private set; }

    private LevelGoals _runtimeGoals;
    private LevelDefinition _currentLevel;

    private void Awake()
    {
        CurrentLevelIndex = ResolveLevelIndex();
    }

    private void Start()
    {
        int index = CurrentLevelIndex;

        _currentLevel = LoadLevel(index);
        if (_currentLevel == null) return;

        // LEVEL 1: без прерана
        if (index == 0)
        {
            run?.StartRun();
            return;
        }

        // LEVEL 2+: преран показывает актуальные goals
        if (preRunPopup != null)
        {
            preRunPopup.Show(
                _currentLevel,
                run != null ? run.DefaultDurationMinutes : 2.3f,
                () => run?.StartRun(),
                goalsOverride: _runtimeGoals,
                catalogOverride: _currentLevel.catalog
            );
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
        {
            int idx = cfg.levelIndex;
            if (pendingRunConfigIsOneBased)
                idx -= 1; // 1..N -> 0..N-1

            return Mathf.Max(0, idx);
        }

        return Mathf.Max(0, debugLevelIndex);
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

        // duration override
        run?.ApplyLevelDuration(level.durationMinutesOverride);

        // -------------------------
        // LEVEL 1 (index 0) — ручной
        // -------------------------
        if (index == 0)
        {
            _runtimeGoals = null;

            goalBuilder?.Build(level.goals, level.catalog);

            if (level.spawnConfig != null)
                spawner?.Spawn(level.spawnConfig, level.catalog);
            else
                Debug.LogWarning("[LevelDirector] Level 1 spawnConfig is null.");

            return level;
        }

        // --------------------------------
        // LEVEL 2+ — procedural: config -> count -> goals -> ui -> spawn
        // --------------------------------
        int humanLevel = index + 1;
        int seed = humanLevel * 10007;

        if (proceduralBuilder == null)
        {
            Debug.LogError("[LevelDirector] proceduralBuilder is null. Fallback to manual spawn+goals.");
            _runtimeGoals = level.goals;
            goalBuilder?.Build(_runtimeGoals, level.catalog);
            if (level.spawnConfig != null) spawner?.Spawn(level.spawnConfig, level.catalog);
            return level;
        }

        // 1) build runtime spawn config (без спавна)
        var runtimeCfg = proceduralBuilder.BuildConfig(humanLevel, seed);
        if (runtimeCfg == null || runtimeCfg.groups == null || runtimeCfg.groups.Count == 0)
        {
            Debug.LogError("[LevelDirector] Procedural spawn config is empty. Fallback to manual.");
            _runtimeGoals = level.goals;
            goalBuilder?.Build(_runtimeGoals, level.catalog);
            if (level.spawnConfig != null) spawner?.Spawn(level.spawnConfig, level.catalog);
            return level;
        }

        // 2) count items реально присутствующие на карте
        Dictionary<ItemType, int> counts = SpawnConfigCounter.CountAll(runtimeCfg);

        // 3) build goals from counts
        _runtimeGoals = null;
        if (proceduralGoals != null)
            _runtimeGoals = proceduralGoals.BuildGoalsFromSpawn(humanLevel, seed, counts);

        if (_runtimeGoals == null)
        {
            Debug.LogWarning("[LevelDirector] Failed to build procedural goals. Fallback to manual goals.");
            _runtimeGoals = level.goals;
        }

        // 4) goals UI
        goalBuilder?.Build(_runtimeGoals, level.catalog);

        // 5) spawn exactly that config
        spawner?.Spawn(runtimeCfg, level.catalog);

        return level;
    }
}