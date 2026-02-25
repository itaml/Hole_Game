using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GoalRequirement
{
    public ItemType type;
    public int required;
}

public class ObjectiveTracker : MonoBehaviour
{
    [SerializeField] private List<GoalRequirement> goals = new();

    private Dictionary<ItemType, int> _current = new();
    private Dictionary<ItemType, int> _required = new();

    public void Init()
    {
        _current.Clear();
        _required.Clear();
        foreach (var g in goals)
        {
            _required[g.type] = Mathf.Max(0, g.required);
            _current[g.type] = 0;
        }
    }

    public bool IsGoalItem(ItemType t) => _required.ContainsKey(t);

    public void Add(ItemType t, int amount = 1)
    {
        if (!_required.ContainsKey(t)) return;
        _current[t] = Mathf.Clamp(_current[t] + amount, 0, _required[t]);
    }

    public bool IsComplete()
    {
        foreach (var kv in _required)
        {
            if (_current.TryGetValue(kv.Key, out int cur) == false) return false;
            if (cur < kv.Value) return false;
        }
        return true;
    }

    public int GetCurrent(ItemType t) => _current.TryGetValue(t, out var v) ? v : 0;
    public int GetRequired(ItemType t) => _required.TryGetValue(t, out var v) ? v : 0;
}