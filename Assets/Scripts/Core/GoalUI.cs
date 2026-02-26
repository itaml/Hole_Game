using System;
using System.Collections.Generic;
using UnityEngine;

public class GoalUI : MonoBehaviour
{
    [Serializable]
    public class Slot
    {
        public ItemType type;
        public RectTransform target;   // куда летит
        public GoalSlotUI slotUI;      // сам слот
    }

    [SerializeField] private List<Slot> slots = new();
    private Dictionary<ItemType, Slot> _map;

    private void Awake() => Rebuild();

    private void Rebuild()
    {
        _map = new Dictionary<ItemType, Slot>(slots.Count);
        foreach (var s in slots)
            if (s != null) _map[s.type] = s;
    }

    public RectTransform GetTarget(ItemType type)
    {
        if (_map == null) Rebuild();
        return _map.TryGetValue(type, out var s) ? s.target : null;
    }

    public GoalSlotUI GetSlotUI(ItemType type)
    {
        if (_map == null) Rebuild();
        return _map.TryGetValue(type, out var s) ? s.slotUI : null;
    }

    public void SetSlots(List<Slot> newSlots)
    {
        slots = newSlots ?? new List<Slot>();
        Rebuild();
    }

    public void ClearSlots()
    {
        slots = new List<Slot>();
        Rebuild();
    }
}