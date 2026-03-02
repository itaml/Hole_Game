using GameBridge.SceneFlow;
using GameBridge.Contracts;
using UnityEngine;

public class Level1UIRules : MonoBehaviour
{
    [Header("Hide on Level 1")]
    [SerializeField] private GameObject timerRoot;        // корень таймера (панель/контейнер)
    [SerializeField] private GameObject preRunPopupRoot;  // корень PreRunPopup (panel/root)
    [SerializeField] private GameObject boostersRoot;     // корень бустеров (панель с бустами)

    private void Start()
    {
        if (!IsLevel1()) return;

        if (timerRoot) timerRoot.SetActive(false);
        if (preRunPopupRoot) preRunPopupRoot.SetActive(false);
        if (boostersRoot) boostersRoot.SetActive(false);
    }

    private bool IsLevel1()
    {
        RunConfig cfg = SceneFlow.PendingRunConfig;
        if (cfg == null) return true; // если сцену запускают напрямую, считаем это Level 1
        return cfg.levelIndex <= 1;   // 1-based
    }
}