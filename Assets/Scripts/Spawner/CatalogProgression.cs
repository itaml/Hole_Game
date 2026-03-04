using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hole/ProcGen/Catalog Progression", fileName = "CatalogProgression")]
public class CatalogProgression : ScriptableObject
{
    [Serializable]
    public class Range
    {
        public int fromLevelInclusive = 1;
        public int toLevelInclusive = 20;

        [Header("Catalog for this range")]
        public ItemCatalog catalog;

        [Header("Optional: GoalPool override for this range")]
        public GoalPool goalPoolOverride;
    }

    public List<Range> ranges = new();

    public Range GetRange(int humanLevel)
    {
        humanLevel = Mathf.Max(1, humanLevel);

        foreach (var r in ranges)
        {
            if (r == null) continue;
            if (humanLevel >= r.fromLevelInclusive && humanLevel <= r.toLevelInclusive)
                return r;
        }

        return ranges.Count > 0 ? ranges[ranges.Count - 1] : null;
    }

    public GoalPool GetGoalPoolOverride(int humanLevel)
{
    if (ranges == null) return null;

    for (int i = 0; i < ranges.Count; i++)
    {
        var r = ranges[i];
        if (humanLevel >= r.fromLevelInclusive && humanLevel <= r.toLevelInclusive)
            return r.goalPoolOverride; // может быть null
    }

    return null;
}
}