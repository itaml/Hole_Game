using System.Collections.Generic;
using UnityEngine;

public class GoalUIBuilder : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform slotsParent;
    [SerializeField] private GoalSlotUI slotPrefab;

    [Header("Targets")]
    [SerializeField] private GoalUI goalUI;
    [SerializeField] private ObjectiveTracker objectives;

    private readonly List<GameObject> _spawned = new();

public void Build(LevelGoals goals, ItemCatalog catalog)
{
    if (goals == null) { Debug.LogError("GoalUIBuilder: goals is NULL"); return; }
    if (catalog == null) { Debug.LogError("GoalUIBuilder: catalog is NULL"); return; }

    Clear();

    var uiSlots = new List<GoalUI.Slot>(goals.goals.Count);
    var trackerGoals = new List<ObjectiveTracker.Goal>(goals.goals.Count);

    foreach (var g in goals.goals)
    {
        if (g == null) continue;

        var slot = Instantiate(slotPrefab, slotsParent);
        _spawned.Add(slot.gameObject);

        var icon = catalog.GetIcon(g.type);
        slot.Setup(icon, g.required);
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