using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hole/Item Catalog", fileName = "ItemCatalog")]
public class ItemCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public ItemType type;
        public Sprite icon;
        public GameObject worldPrefab;   // ✅ добавили префаб предмета в мире
    }

    [SerializeField] private List<Entry> entries = new();

    private Dictionary<ItemType, Entry> _map;

    private void OnEnable()
    {
        _map = new Dictionary<ItemType, Entry>(entries.Count);
        foreach (var e in entries)
        {
            if (e == null) continue;
            _map[e.type] = e;
        }
    }

    public Sprite GetIcon(ItemType type)
    {
        if (_map == null) OnEnable();
        return _map.TryGetValue(type, out var e) ? e.icon : null;
    }

    public GameObject GetWorldPrefab(ItemType type)
    {
        if (_map == null) OnEnable();
        return _map.TryGetValue(type, out var e) ? e.worldPrefab : null;
    }
}