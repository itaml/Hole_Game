using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hole/ProcGen/Goal Pool", fileName = "GoalPool")]
public class GoalPool : ScriptableObject
{
    [Serializable]
    public class GoalItem
    {
        public ItemType type;
        public Vector2Int requiredMinMax = new Vector2Int(3, 8);
        public int weight = 1; // шанс выпадения
    }

    public List<GoalItem> items = new();
}