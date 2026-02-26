using System.Collections.Generic;
using UnityEngine;

public class GoalUIBuilder : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private LevelGoals levelGoals;
    [SerializeField] private ItemCatalog catalog;

    [Header("UI")]
    [SerializeField] private Transform slotsParent;
    [SerializeField] private GoalSlotUI slotPrefab;

    [Header("Targets")]
    [SerializeField] private GoalUI goalUI;
    [SerializeField] private ObjectiveTracker objectives;

    private readonly List<GameObject> _spawned = new();

    public void Build()
    {
        if (levelGoals == null) { Debug.LogError("GoalUIBuilder: levelGoals is NULL"); return; }
        if (catalog == null) { Debug.LogError("GoalUIBuilder: catalog is NULL"); return; }
        if (slotsParent == null) { Debug.LogError("GoalUIBuilder: slotsParent is NULL"); return; }
        if (slotPrefab == null) { Debug.LogError("GoalUIBuilder: slotPrefab is NULL"); return; }
        if (goalUI == null) { Debug.LogError("GoalUIBuilder: goalUI is NULL"); return; }
        if (objectives == null) { Debug.LogError("GoalUIBuilder: objectives is NULL"); return; }

        Clear();

        var uiSlots = new List<GoalUI.Slot>(levelGoals.goals.Count);
        var trackerGoals = new List<ObjectiveTracker.Goal>(levelGoals.goals.Count);

        foreach (var g in levelGoals.goals)
        {
            if (g == null) continue;

            var slot = Instantiate(slotPrefab, slotsParent);
            _spawned.Add(slot.gameObject);

            var icon = catalog.GetIcon(g.type);
            slot.Setup(icon, g.required);

            // ✅ теперь показываем “оставшееся”, старт = required
            slot.SetRemaining(g.required);

            uiSlots.Add(new GoalUI.Slot
            {
                type = g.type,
                target = slot.Target,
                slotUI = slot
            });

            trackerGoals.Add(new ObjectiveTracker.Goal
            {
                type = g.type,
                required = g.required
            });
        }

        goalUI.SetSlots(uiSlots);
        objectives.SetGoals(trackerGoals);
    }

    public void Clear()
    {
        foreach (var go in _spawned)
            if (go) Destroy(go);

        _spawned.Clear();

        if (goalUI != null)
            goalUI.ClearSlots();
    }
}