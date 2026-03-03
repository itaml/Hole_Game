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
    Debug.Log($"[BP] SpawnIfNeeded. cfg={(run.PendingConfig==null?"null":"ok")} b1={run.PendingConfig?.boost1Activated} b2={run.PendingConfig?.boost2Activated}");
    var cfg = run.PendingConfig;
    if (cfg == null) return;

    if (!cfg.boost1Activated && !cfg.boost2Activated)
        return;

    // ✅ инстансим сразу в точке старта прилёта
var basket = Instantiate(basketPrefab, flyFromPoint.position, flyFromPoint.rotation);
basket.gameObject.SetActive(true);
Debug.Log($"[BP] Basket instanced: {basket.name} active={basket.gameObject.activeInHierarchy} pos={basket.transform.position}");

    if (cfg.boost1Activated)
        basket.AddGrowLevelBoost(BoostSpawnSource.Battlepass);

    if (cfg.boost2Activated)
        basket.AddExtraTimeBoost(BoostSpawnSource.Battlepass);

    // ✅ корзина прилетает к flyToPoint и выстреливает вперёд
    basket.FlyInAndShoot(flyFromPoint, flyToPoint, flyToPoint.forward);
}
}