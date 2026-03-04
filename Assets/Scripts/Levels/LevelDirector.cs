using System.Collections.Generic;
using TMPro;
using UnityEngine;
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

    [Header("Procedural (Levels 2+)")]
    [SerializeField] private ProceduralSpawnBuilder proceduralBuilder;
    [SerializeField] private ProceduralGoalBuilder proceduralGoals;

    [Header("Catalogs by Level")]
    [SerializeField] private CatalogProgression catalogProgression;

    [Header("Indexing")]
    [Tooltip("Если PendingRunConfig.levelIndex приходит 1..N — включи. Если 0..N-1 — выключи.")]
    [SerializeField] private bool pendingRunConfigIsOneBased = true;

    [Header("UI")]
    [SerializeField] private TMP_Text levelText;

    public int CurrentLevelIndex { get; private set; }

    private LevelGoals _runtimeGoals;
    private ItemCatalog _activeCatalog;
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

        // Level 1: без прерана
        if (index == 0)
        {
            run?.StartRun();
            return;
        }

        // Level 2+: PreRun показывает актуальные goals и активный каталог
        if (preRunPopup != null)
        {
            preRunPopup.Show(
                _currentLevel,
                run != null ? run.DefaultDurationMinutes : 2.3f,
                () => run?.StartRun(),
                goalsOverride: _runtimeGoals,
                catalogOverride: _activeCatalog
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
            if (pendingRunConfigIsOneBased) idx -= 1;
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

        run?.ApplyLevelDuration(level.durationMinutesOverride);

        // -------------------------
        // Level 1 — ручной
        // -------------------------
        if (index == 0)
        {
            _runtimeGoals = null;
            _activeCatalog = level.catalog;

            goalBuilder?.Build(level.goals, _activeCatalog);

            if (level.spawnConfig != null)
                spawner?.Spawn(level.spawnConfig, _activeCatalog);

            return level;
        }

        // -------------------------
        // Level 2+ — procedural
        // -------------------------
        int humanLevel = index + 1;
        int seed = humanLevel * 10007;

        // выбрать активный каталог и (опционально) goal pool по диапазону уровней
        GoalPool poolOverride = null;
        _activeCatalog = level.catalog;

        if (catalogProgression != null)
        {
            var range = catalogProgression.GetRange(humanLevel);
            if (range != null)
            {
                if (range.catalog != null) _activeCatalog = range.catalog;
                poolOverride = range.goalPoolOverride;
            }
        }

        if (_activeCatalog == null)
        {
            Debug.LogError("[LevelDirector] Active catalog is null (catalogProgression + level.catalog are null)");
            return level;
        }

        if (proceduralBuilder == null)
        {
            Debug.LogError("[LevelDirector] proceduralBuilder is null. Fallback to manual spawn/goals.");
            _runtimeGoals = level.goals;
            goalBuilder?.Build(_runtimeGoals, _activeCatalog);
            if (level.spawnConfig != null) spawner?.Spawn(level.spawnConfig, _activeCatalog);
            return level;
        }

        // 1) build runtime spawn config (без спавна)
var runtimeCfg = proceduralBuilder.BuildConfig(humanLevel, _activeCatalog, seed, poolOverride);
        
        if (runtimeCfg == null || runtimeCfg.groups == null || runtimeCfg.groups.Count == 0)
        {
            Debug.LogError("[LevelDirector] Procedural spawn config is empty. Fallback to manual.");
            _runtimeGoals = level.goals;
            goalBuilder?.Build(_runtimeGoals, _activeCatalog);
            if (level.spawnConfig != null) spawner?.Spawn(level.spawnConfig, _activeCatalog);
            return level;
        }

        // 2) count items from runtime config
        Dictionary<ItemType, int> counts = SpawnConfigCounter.CountAll(runtimeCfg);

        // 3) build goals ONLY from what exists
        _runtimeGoals = null;
        if (proceduralGoals != null)
            _runtimeGoals = proceduralGoals.BuildGoalsFromSpawn(humanLevel, seed, counts, poolOverride);

        if (_runtimeGoals == null)
        {
            Debug.LogWarning("[LevelDirector] Failed to build procedural goals. Fallback to manual goals.");
            _runtimeGoals = level.goals;
        }

        // 4) build goals UI using ACTIVE catalog (icons)
        goalBuilder?.Build(_runtimeGoals, _activeCatalog);

        // 5) spawn using ACTIVE catalog (prefabs)
        spawner?.Spawn(runtimeCfg, _activeCatalog);

        return level;
    }
}