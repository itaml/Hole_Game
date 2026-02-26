using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hole/Level Goals", fileName = "LevelGoals")]
public class LevelGoals : ScriptableObject
{
    [Serializable]
    public class GoalDef
    {
        public ItemType type;
        public int required = 1;
    }

    public List<GoalDef> goals = new();
}