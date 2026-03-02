using GameBridge.Contracts;
using GameBridge.SceneFlow;
using UnityEngine;

public class ShowOnlyOnLevelUI : MonoBehaviour
{
    [Tooltip("Human level (1-based). Example: 2 means Level 2.")]
    [SerializeField] private int showOnLevel = 2;

    [Header("Fallback when RunConfig is null")]
    [SerializeField] private int debugLevelIndex = 1;

    private void Awake()
    {
        // по умолчанию выключаем, чтобы точно не мигало
        gameObject.SetActive(false);

        int lvl = GetHumanLevel1Based();
        if (lvl == showOnLevel)
            gameObject.SetActive(true);
    }

    private int GetHumanLevel1Based()
    {
        RunConfig cfg = SceneFlow.PendingRunConfig;
        if (cfg != null)
            return Mathf.Max(1, cfg.levelIndex); // 1-based как у тебя

        return Mathf.Max(1, debugLevelIndex);
    }
}