using System.Collections;
using GameBridge.Contracts;
using GameBridge.SceneFlow;
using UnityEngine;
using UnityEngine.UI;

public class BoostUnlockSlotUI : MonoBehaviour
{
    [Header("Unlock (same indexing as LevelDirector ResolveLevelIndex)")]
    [SerializeField] private int unlockAtLevel = 1;

    [Header("Fallback when RunConfig is null (same as LevelDirector debugLevelIndex)")]
    [SerializeField] private int debugLevelIndex = 0;

    [Header("UI")]
    [SerializeField] private GameObject lockObj;
    [SerializeField] private GameObject badgeObj;
    [SerializeField] private Button button; // ← ВАЖНО

    private Coroutine _routine;

    private void OnEnable()
    {
        // на старте кружок скрываем, чтобы не мешал
        if (badgeObj) badgeObj.SetActive(false);

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ApplyNextFrame());
    }

    private void OnDisable()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = null;
    }

    private IEnumerator ApplyNextFrame()
    {
        // 1 кадр ждём на случай, если PendingRunConfig выставляется чуть позже
        yield return null;

        int currentLevel = ResolveLevelIndexSameAsDirector();
        bool unlocked = currentLevel >= unlockAtLevel;

        if (lockObj) lockObj.SetActive(!unlocked);

        // кружок показываем только если открыто
        if (badgeObj) badgeObj.SetActive(unlocked);

        // 🔒 КНОПКА
        if (button)
            button.interactable = unlocked;
    }

    private int ResolveLevelIndexSameAsDirector()
    {
        RunConfig cfg = SceneFlow.PendingRunConfig;
        if (cfg != null)
            return Mathf.Max(0, cfg.levelIndex);

        return Mathf.Max(0, debugLevelIndex);
    }

    public void HideBadge()
    {
        if (badgeObj) badgeObj.SetActive(false);
    }
}