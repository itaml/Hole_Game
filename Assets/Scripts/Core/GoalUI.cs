using System;
using System.Collections.Generic;
using UnityEngine;

public class GoalUI : MonoBehaviour
{
    [Serializable]
    public class GoalSlot
    {
        public ItemType type;
        public RectTransform target;   // куда летит иконка
    }

    [SerializeField] private List<GoalSlot> slots = new();

    private Dictionary<ItemType, RectTransform> _map;

    private void Awake()
    {
        _map = new Dictionary<ItemType, RectTransform>(slots.Count);
        foreach (var s in slots)
        {
            if (s == null || s.target == null) continue;
            _map[s.type] = s.target;
        }
    }

    public RectTransform GetTarget(ItemType type)
    {
        if (_map == null) Awake();
        return _map.TryGetValue(type, out var t) ? t : null;
    }
}