using System;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralGoalBuilder : MonoBehaviour
{
    [SerializeField] private GoalPool pool;
    [SerializeField] private GoalCurve curve;

    /// <summary>
    /// Генерирует goals только из тех ItemType, которые реально присутствуют на поле (availableCounts).
    /// required никогда не больше availableCounts[type].
    /// </summary>
    public LevelGoals BuildGoalsFromSpawn(int humanLevelNumber, int seed, Dictionary<ItemType, int> availableCounts)
    {
        if (pool == null || curve == null)
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

        // Сколько разных типов целей на уровень
        int goalTypes = rng.Next(rule.goalTypesMinMax.x, rule.goalTypesMinMax.y + 1);
        goalTypes = Mathf.Clamp(goalTypes, 1, 3);

        // Кандидаты = пересечение pool и того, что реально есть на карте
        var candidates = new List<GoalPool.GoalItem>();
        foreach (var it in pool.items)
        {
            if (it == null) continue;

            if (availableCounts.TryGetValue(it.type, out int cnt) && cnt > 0)
                candidates.Add(it);
        }

        if (candidates.Count == 0)
        {
            Debug.LogError("[ProcGoals] No goal candidates exist in spawned content.");
            return null;
        }

        goalTypes = Mathf.Clamp(goalTypes, 1, candidates.Count);

        // Процент от доступного, который требуем (растёт по уровню)
        float baseMinPct = 0.55f;
        float baseMaxPct = 0.85f;

        float levelAdd = Mathf.Clamp01((humanLevelNumber - 1) / 20f) * 0.10f; // +0..0.10
        float minPct = Mathf.Clamp01(baseMinPct + levelAdd);
        float maxPct = Mathf.Clamp01(baseMaxPct + levelAdd);

        var goals = ScriptableObject.CreateInstance<LevelGoals>();
        goals.goals = new List<LevelGoals.GoalDef>(goalTypes);

        // Weighted выбор без повторов
        for (int i = 0; i < goalTypes; i++)
        {
            var pick = PickWeighted(candidates, rng);
            candidates.Remove(pick);

            int available = availableCounts[pick.type];

            float pct = Lerp(minPct, maxPct, (float)rng.NextDouble());
            int req = Mathf.RoundToInt(available * pct);
            req = Mathf.Clamp(req, 1, available);

            goals.goals.Add(new LevelGoals.GoalDef
            {
                type = pick.type,
                required = req
            });
        }

        Debug.Log($"[ProcGoals] level={humanLevelNumber} goals={goals.goals.Count}");
        return goals;
    }

    private static GoalPool.GoalItem PickWeighted(List<GoalPool.GoalItem> items, System.Random rng)
    {
        if (items == null || items.Count == 0) return null;

        int sum = 0;
        foreach (var it in items)
            sum += Mathf.Max(0, it.weight);

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