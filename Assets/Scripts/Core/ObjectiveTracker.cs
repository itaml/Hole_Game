using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveTracker : MonoBehaviour
{
    [Serializable]
    public class Goal
    {
        public ItemType type;
        public int required;
        [NonSerialized] public int current;
    }

    [SerializeField] private List<Goal> goals = new();
    private Dictionary<ItemType, Goal> _map;

    public event Action<ItemType, int> OnRemainingChanged;  // type, remaining
    public event Action<ItemType> OnGoalCompleted;          // type

    private void Awake() => Init();

    public void Init()
    {
        _map = new Dictionary<ItemType, Goal>(goals.Count);
        foreach (var g in goals)
        {
            if (g == null) continue;
            g.current = 0;
            _map[g.type] = g;
        }
    }

    public void SetGoals(List<Goal> newGoals)
    {
        goals = newGoals ?? new List<Goal>();
        Init();
        // сразу разошлём стартовые remaining
        foreach (var g in goals)
            if (g != null) OnRemainingChanged?.Invoke(g.type, GetRemaining(g.type));
    }

    public bool IsGoalItem(ItemType type) => _map != null && _map.ContainsKey(type);

    public int GetRemaining(ItemType type)
    {
        if (_map == null) Init();
        if (!_map.TryGetValue(type, out var g)) return 0;
        return Mathf.Max(0, g.required - g.current);
    }

    public void Add(ItemType type, int amount)
    {
        if (_map == null) Init();
        if (!_map.TryGetValue(type, out var g)) return;

        int before = g.current;
        g.current = Mathf.Clamp(g.current + amount, 0, g.required);

        int remaining = Mathf.Max(0, g.required - g.current);
        OnRemainingChanged?.Invoke(type, remaining);

        // если только что добили цель
        if (before < g.required && g.current >= g.required)
            OnGoalCompleted?.Invoke(type);
    }

    public bool IsComplete()
    {
        foreach (var g in goals)
            if (g != null && g.current < g.required) return false;
        return true;
    }
}