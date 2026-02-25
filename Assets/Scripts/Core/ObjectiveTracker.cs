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

    private void Awake()
    {
        Init();
    }

    /// <summary>
    /// Инициализация/переинициализация карты целей.
    /// Можно безопасно вызывать сколько угодно раз.
    /// </summary>
    public void Init()
    {
        if (_map == null)
            _map = new Dictionary<ItemType, Goal>(goals.Count);
        else
            _map.Clear();

        foreach (var g in goals)
        {
            if (g == null) continue;
            g.current = 0;
            _map[g.type] = g;
        }
    }

    /// <summary>
    /// Сброс прогресса целей без пересоздания списка.
    /// </summary>
    public void ResetProgress()
    {
        if (_map == null) Init();
        foreach (var g in goals)
        {
            if (g == null) continue;
            g.current = 0;
        }
    }

    public bool IsGoalItem(ItemType type) => _map != null && _map.ContainsKey(type);

    public void Add(ItemType type, int amount)
    {
        if (_map == null) Init();
        if (!_map.TryGetValue(type, out var g)) return;
        g.current = Mathf.Clamp(g.current + amount, 0, g.required);
    }

    public bool IsComplete()
    {
        foreach (var g in goals)
            if (g != null && g.current < g.required) return false;
        return true;
    }

    public IReadOnlyList<Goal> GetGoals() => goals;
}