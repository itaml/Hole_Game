using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hole/ProcGen/Goal Curve", fileName = "GoalCurve")]
public class GoalCurve : ScriptableObject
{
    [Serializable]
    public class GoalRule
    {
        public int fromLevelInclusive = 1;
        public int toLevelInclusive = 5;

        [Tooltip("Сколько разных типов целей на уровне (например: 1-2 типа).")]
        public Vector2Int goalTypesMinMax = new Vector2Int(1, 2);

        [Tooltip("Множитель required (на поздних уровнях больше).")]
        public float requiredMultiplier = 1f;
    }

    public List<GoalRule> rules = new();

    public GoalRule GetRule(int level)
    {
        level = Mathf.Max(1, level);
        foreach (var r in rules)
            if (level >= r.fromLevelInclusive && level <= r.toLevelInclusive)
                return r;

        return rules.Count > 0 ? rules[rules.Count - 1] : new GoalRule();
    }
}