using GameBridge.Contracts;
using UnityEngine;

public class BattlepassBoostSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RunController run;

    [Header("Basket Prefab")]
    [SerializeField] private BoostBasketSpawner basketPrefab;

    [Header("Spawn Point (in front of hole)")]
    [SerializeField] private Transform basketSpawnPoint;

    [Header("Behaviour")]
    [SerializeField] private bool spawnOncePerRun = true;
    [SerializeField] private Transform flyFromPoint;
    [SerializeField] private Transform flyToPoint;

    private bool _spawnedThisRun;

    private void Awake()
    {
        if (run == null) run = FindFirstObjectByType<RunController>();
    }

    public void ResetForNewRun()
    {
        _spawnedThisRun = false;
    }

public void SpawnIfNeeded()
{
    var cfg = run.PendingConfig;
    if (cfg == null) return;

    // ✅ если бусты выбраны вручную — батл пас НЕ спавним
    if (cfg.boost1Activated || cfg.boost2Activated)
        return;

    // ✅ батл пас включён?
    if (!cfg.isBattlepasOpen)
        return;

    if (spawnOncePerRun && _spawnedThisRun)
        return;

    // ✅ 0 = вообще не спавним
    int bpLevelRaw = cfg.bonusSpawnLevel;

    if (bpLevelRaw <= 0)
        return;

    // ✅ 1..3
    int bpLevel = Mathf.Clamp(bpLevelRaw, 1, 3);

    if (basketPrefab == null || flyFromPoint == null || flyToPoint == null)
    {
        Debug.LogWarning("[BP] Missing basketPrefab/fly points");
        return;
    }

    _spawnedThisRun = true;

    var basket = Instantiate(basketPrefab, flyFromPoint.position, flyFromPoint.rotation);
    basket.gameObject.SetActive(true);

    // ✅ по уровню батл паса: 1 -> по 1, 2 -> по 2, 3 -> по 3
    for (int i = 0; i < bpLevel; i++)
    {
        basket.AddGrowLevelBoost(BoostSpawnSource.Battlepass);
        basket.AddExtraTimeBoost(BoostSpawnSource.Battlepass);
    }

    basket.FlyInAndShoot(flyFromPoint, flyToPoint, flyToPoint.forward);

    Debug.Log($"[BP] Spawned basket. bpLevel={bpLevel} raw={bpLevelRaw}");
}
}