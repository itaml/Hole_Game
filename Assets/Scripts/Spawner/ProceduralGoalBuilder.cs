using System;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralGoalBuilder : MonoBehaviour
{
    [Serializable]
    public class PoolRange
    {
        [Min(1)] public int fromLevelInclusive = 1;
        [Min(1)] public int toLevelInclusive = 10;
        public GoalPool pool;

        public bool Contains(int level) => level >= fromLevelInclusive && level <= toLevelInclusive;
    }

    [Header("Defaults")]
    [Tooltip("Фолбэк, если ни один диапазон не подошёл.")]
    [SerializeField] private GoalPool pool;

    [SerializeField] private GoalCurve curve;

    [Header("Goal Pools by Level (3 pools total)")]
    [Tooltip("Диапазоны уровней -> какой GoalPool использовать. Можно 3 элемента (как ты хочешь).")]
    [SerializeField] private List<PoolRange> poolByLevel = new List<PoolRange>(3);

    [Header("CatalogProgression (catalog/themes from here)")]
    [SerializeField] private CatalogProgression catalogProgression;

    private void Awake()
    {
        if (catalogProgression == null)
            catalogProgression = FindFirstObjectByType<CatalogProgression>();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // чуть прибираем мусор, чтобы не было "от 20 до 3"
        if (poolByLevel == null) return;

        for (int i = 0; i < poolByLevel.Count; i++)
        {
            var r = poolByLevel[i];
            if (r == null) continue;

            r.fromLevelInclusive = Mathf.Max(1, r.fromLevelInclusive);
            r.toLevelInclusive = Mathf.Max(1, r.toLevelInclusive);

            if (r.toLevelInclusive < r.fromLevelInclusive)
            {
                // меняем местами
                int tmp = r.fromLevelInclusive;
                r.fromLevelInclusive = r.toLevelInclusive;
                r.toLevelInclusive = tmp;
            }
        }

        // сортировка по from
        poolByLevel.Sort((a, b) => a.fromLevelInclusive.CompareTo(b.fromLevelInclusive));
    }
#endif

    public LevelGoals BuildGoalsFromSpawn(
        int humanLevelNumber,
        int seed,
        Dictionary<ItemType, int> availableCounts,
        GoalPool poolOverride = null)
    {
        // poolOverride (если ты передал извне) имеет приоритет
        var usePool = ResolvePoolForLevel(humanLevelNumber, poolOverride);

        if (usePool == null || curve == null)
        {
            Debug.LogError("[ProcGoals] pool/curve missing");
            return null;
        }

        if (availableCounts == null || availableCounts.Count == 0)
        {
            Debug.LogError("[ProcGoals] availableCounts is empty");
            return null;
        }

        var rng = new System.Random(seed);
        var rule = curve.GetRule(humanLevelNumber);

        int goalTypes = rng.Next(rule.goalTypesMinMax.x, rule.goalTypesMinMax.y + 1);
        goalTypes = Mathf.Clamp(goalTypes, 1, 4);

        // кандидаты = пересечение usePool и того, что реально есть на карте
        var candidates = new List<GoalPool.GoalItem>();
        if (usePool.items != null)
        {
            foreach (var it in usePool.items)
            {
                if (it == null) continue;
                if (availableCounts.TryGetValue(it.type, out int cnt) && cnt > 0)
                    candidates.Add(it);
            }
        }

        if (candidates.Count == 0)
        {
            Debug.LogError($"[ProcGoals] No goal candidates exist in spawned content. pool={usePool.name} level={humanLevelNumber}");
            return null;
        }

        goalTypes = Mathf.Clamp(goalTypes, 1, candidates.Count);

        float baseMinPct = 0.55f;
        float baseMaxPct = 0.85f;

        float levelAdd = Mathf.Clamp01((humanLevelNumber - 1) / 30f) * 0.10f; // +0..0.10
        float minPct = Mathf.Clamp01(baseMinPct + levelAdd);
        float maxPct = Mathf.Clamp01(baseMaxPct + levelAdd);

        var goals = ScriptableObject.CreateInstance<LevelGoals>();
        goals.goals = new List<LevelGoals.GoalDef>(goalTypes);

        for (int i = 0; i < goalTypes; i++)
        {
            var pick = PickWeighted(candidates, rng);
            candidates.Remove(pick);

            int available = availableCounts[pick.type];

            float pct = Lerp(minPct, maxPct, (float)rng.NextDouble());
            int req = Mathf.RoundToInt(available * pct);

            req = Mathf.Max(1, Mathf.RoundToInt(req * Mathf.Max(0.1f, rule.requiredMultiplier)));
            req = Mathf.Clamp(req, 1, available);

            goals.goals.Add(new LevelGoals.GoalDef { type = pick.type, required = req });
        }

        Debug.Log($"[ProcGoals] level={humanLevelNumber} goals={goals.goals.Count} pool={usePool.name}");
        return goals;
    }

    private GoalPool ResolvePoolForLevel(int humanLevelNumber, GoalPool poolOverride)
    {
        if (poolOverride != null)
            return poolOverride;

        if (poolByLevel != null)
        {
            for (int i = 0; i < poolByLevel.Count; i++)
            {
                var r = poolByLevel[i];
                if (r == null || r.pool == null) continue;
                if (r.Contains(humanLevelNumber))
                    return r.pool;
            }
        }

        return pool; // fallback
    }

    private static GoalPool.GoalItem PickWeighted(List<GoalPool.GoalItem> items, System.Random rng)
    {
        if (items == null || items.Count == 0) return null;

        int sum = 0;
        foreach (var it in items) sum += Mathf.Max(0, it.weight);

        if (sum <= 0)
            return items[rng.Next(0, items.Count)];

        int roll = rng.Next(0, sum);
        int acc = 0;

        foreach (var it in items)
        {
            acc += Mathf.Max(0, it.weight);
            if (roll < acc) return it;
        }

        return items[items.Count - 1];
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * Mathf.Clamp01(t);
}