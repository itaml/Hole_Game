using System.Collections.Generic;
using UnityEngine;

public class GoalFinderBoost : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ObjectiveTracker objectives;
    [SerializeField] private Transform itemsParent;
    [SerializeField] private Camera worldCamera;

    [Header("UI Roots")]
    [SerializeField] private RectTransform iconsRoot;
    [SerializeField] private RectTransform arrowsRoot;

    [Header("UI Prefabs")]
    [SerializeField] private FinderTargetIconUI iconPrefab;
    [SerializeField] private FinderArrowUI arrowPrefab;

    [Header("Same icon for ALL targets")]
    [SerializeField] private Sprite overrideIcon;

    [Header("Duration (no cooldown)")]
    [SerializeField] private float durationSeconds = 6f;

    [Header("Arrows count")]
    [Tooltip("0 = показывать на все подходящие цели. Иначе лимит.")]
    [SerializeField] private int maxArrows = 5;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    public bool IsActive => _active;
    public float Remaining => _remaining;
    public float Duration => durationSeconds;

    private bool _active;
    private float _remaining;

    private readonly Dictionary<AbsorbablePhysicsItem, FinderTargetIconUI> _icons = new();
    private readonly Dictionary<AbsorbablePhysicsItem, FinderArrowUI> _arrows = new();

    private void Awake()
    {
        if (worldCamera == null) worldCamera = Camera.main;
    }

    private void Update()
    {
        if (!_active) return;

        _remaining -= Time.deltaTime;
        if (_remaining <= 0f)
        {
            StopBoost();
            return;
        }

        CleanupInvalidTargets();
    }

    public void Activate()
    {
        if (_active) return; // пока активен, кнопка должна быть неактивна

        if (worldCamera == null) worldCamera = Camera.main;

        _active = true;
        _remaining = durationSeconds;

        if (debugLogs)
            Debug.Log($"[FinderBoost] Activate duration={durationSeconds:0.##}");

        BuildTargets();
    }

    public void StopBoost()
    {
        if (!_active) return;

        _active = false;
        _remaining = 0f;
        ClearAll();

        if (debugLogs)
            Debug.Log("[FinderBoost] StopBoost -> cleared");
    }

    // дергай из RunController.OnItemCollected для goal-item
    public void OnGoalItemCollected(AbsorbablePhysicsItem item)
    {
        if (item == null) return;

        if (_icons.TryGetValue(item, out var icon) && icon != null) Destroy(icon.gameObject);
        _icons.Remove(item);

        if (_arrows.TryGetValue(item, out var arrow) && arrow != null) Destroy(arrow.gameObject);
        _arrows.Remove(item);
    }

    private void BuildTargets()
    {
        ClearAll();

        if (objectives == null || itemsParent == null)
        {
            Debug.LogWarning("[FinderBoost] objectives/itemsParent missing");
            return;
        }

        if (overrideIcon == null)
        {
            Debug.LogWarning("[FinderBoost] overrideIcon is NULL (ты просил одинаковую иконку)");
            // можно не return, но тогда иконки не появятся
        }

        var items = itemsParent.GetComponentsInChildren<AbsorbablePhysicsItem>(false);
        var candidates = new List<AbsorbablePhysicsItem>(items.Length);

        foreach (var item in items)
        {
            if (item == null) continue;
            if (!objectives.IsGoalItem(item.Type)) continue;
            if (objectives.GetRemaining(item.Type) <= 0) continue;
            candidates.Add(item);
        }

        if (debugLogs)
            Debug.Log($"[FinderBoost] Candidates={candidates.Count}");

        // иконки над всеми кандидатами
        foreach (var item in candidates)
        {
            if (overrideIcon == null) continue;

            var ui = Instantiate(iconPrefab, iconsRoot);
            ui.Init(item.transform, overrideIcon, worldCamera);
            _icons[item] = ui;
        }

        // стрелки: либо все, либо лимит ближайших к дыре/камере
        if (maxArrows > 0 && candidates.Count > maxArrows)
        {
            // сортируем по расстоянию до камеры (нормально для начала)
            candidates.Sort((a, b) =>
            {
                float da = (a.transform.position - worldCamera.transform.position).sqrMagnitude;
                float db = (b.transform.position - worldCamera.transform.position).sqrMagnitude;
                return da.CompareTo(db);
            });

            candidates.RemoveRange(maxArrows, candidates.Count - maxArrows);
        }

        foreach (var item in candidates)
        {
            var arrow = Instantiate(arrowPrefab, arrowsRoot);
            arrow.Init(item.transform, worldCamera);
            _arrows[item] = arrow;
        }

        if (debugLogs)
            Debug.Log($"[FinderBoost] Spawned icons={_icons.Count}, arrows={_arrows.Count}");
    }

    private void CleanupInvalidTargets()
    {
        // убираем если предмет исчез или цель по типу закрылась
        var dead = new List<AbsorbablePhysicsItem>();

        foreach (var kv in _icons)
        {
            var item = kv.Key;
            if (item == null ||
                !objectives.IsGoalItem(item.Type) ||
                objectives.GetRemaining(item.Type) <= 0)
            {
                dead.Add(item);
            }
        }

        foreach (var item in dead)
        {
            if (_icons.TryGetValue(item, out var icon) && icon != null) Destroy(icon.gameObject);
            _icons.Remove(item);

            if (_arrows.TryGetValue(item, out var arrow) && arrow != null) Destroy(arrow.gameObject);
            _arrows.Remove(item);
        }
    }

    private void ClearAll()
    {
        foreach (var v in _icons.Values) if (v != null) Destroy(v.gameObject);
        _icons.Clear();

        foreach (var v in _arrows.Values) if (v != null) Destroy(v.gameObject);
        _arrows.Clear();
    }
}