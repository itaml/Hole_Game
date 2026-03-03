using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hole/ProcGen/Difficulty Curve", fileName = "DifficultyCurve")]
public class DifficultyCurve : ScriptableObject
{
    [Serializable]
    public class BudgetRange
    {
        public int fromLevelInclusive = 1;
        public int toLevelInclusive = 5;
        public Vector2Int budgetMinMax = new Vector2Int(8, 12);
    }

    [Serializable]
    public class UnlockRule
    {
        public int fromLevelInclusive = 1;
        public ModuleTag allowedTags = ModuleTag.StartSafe | ModuleTag.Relax | ModuleTag.Teaching;
        public int maxModules = 6;
        public int minModules = 3;
    }

    public List<BudgetRange> budgets = new();
    public List<UnlockRule> unlocks = new();

    public int GetBudget(int levelIndex, System.Random rng)
    {
        int lvl = Mathf.Max(1, levelIndex);
        foreach (var r in budgets)
        {
            if (lvl >= r.fromLevelInclusive && lvl <= r.toLevelInclusive)
                return rng.Next(r.budgetMinMax.x, r.budgetMinMax.y + 1);
        }
        // fallback: последняя запись или дефолт
        if (budgets.Count > 0)
        {
            var last = budgets[budgets.Count - 1];
            return rng.Next(last.budgetMinMax.x, last.budgetMinMax.y + 1);
        }
        return rng.Next(8, 13);
    }

    public UnlockRule GetUnlock(int levelIndex)
    {
        int lvl = Mathf.Max(1, levelIndex);
        UnlockRule best = null;
        foreach (var u in unlocks)
        {
            if (lvl >= u.fromLevelInclusive)
                best = u;
        }
        return best ?? new UnlockRule();
    }
}