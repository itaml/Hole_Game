using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PreRunPopupUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Time")]
    [SerializeField] private TMP_Text timeText;

    [Header("Goals")]
    [SerializeField] private Transform slotsParent;
    [SerializeField] private PreRunGoalSlotUI goalSlotPrefab;

    [Header("Buttons")]
    [SerializeField] private Button startButton;

    private readonly List<GameObject> _spawned = new();

    private void Awake()
    {
        if (root) root.SetActive(false);
    }

    public void Show(LevelDefinition level, float defaultMinutes, Action onStart)
    {
        if (level == null)
        {
            Debug.LogError("[PreRunPopupUI] LevelDefinition is null");
            return;
        }

        if (root) root.SetActive(true);

        // 1) Time
        float minutes = (level.durationMinutesOverride > 0f) ? level.durationMinutesOverride : defaultMinutes;
        int totalSeconds = Mathf.Max(1, Mathf.RoundToInt(minutes * 60f));
        if (timeText) timeText.text = FormatTime(totalSeconds);

        // 2) Goals
        RebuildGoals(level);

        // 3) Button
        if (startButton)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(() =>
            {
                Hide();
                onStart?.Invoke();
            });
        }
    }

    public void Hide()
    {
        CleanupGoals();
        if (root) root.SetActive(false);
    }

    private void RebuildGoals(LevelDefinition level)
    {
        CleanupGoals();

        if (level.goals == null || level.goals.goals == null)
            return;

        if (slotsParent == null || goalSlotPrefab == null)
        {
            Debug.LogError("[PreRunPopupUI] slotsParent or goalSlotPrefab is NULL");
            return;
        }

        var catalog = level.catalog;
        foreach (var g in level.goals.goals)
        {
            var slot = Instantiate(goalSlotPrefab, slotsParent);
            _spawned.Add(slot.gameObject);

            Sprite icon = null;
            if (catalog != null)
                icon = catalog.GetIcon(g.type); // предполагаю, что у тебя есть такой метод

            slot.Set(icon, g.required);
        }
    }

    private void CleanupGoals()
    {
        for (int i = 0; i < _spawned.Count; i++)
            if (_spawned[i]) Destroy(_spawned[i]);

        _spawned.Clear();
    }

    private static string FormatTime(int totalSeconds)
    {
        int m = totalSeconds / 60;
        int s = totalSeconds % 60;
        return $"{m:0}:{s:00}";
    }
}