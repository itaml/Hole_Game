using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelItemSpawner : MonoBehaviour
{
    public enum SpawnStabilizationMode
    {
        None,
        FreezeRigidbodiesKinematic,   // ✅ рекомендую
        DisableColliders              // если без Rigidbody или хочется иначе
    }

    [Header("Refs")]
    [SerializeField] private ItemCatalog itemCatalog;
    [SerializeField] private Transform itemsParent;

    [Header("Spawn")]
    [SerializeField] private float spawnY = 0f;
    [SerializeField] private bool clearBeforeSpawn = true;

    [Header("Start Stabilization (to prevent explosion)")]
    [SerializeField] private SpawnStabilizationMode stabilization = SpawnStabilizationMode.FreezeRigidbodiesKinematic;
    [SerializeField] private float unfreezeDelay = 0.25f;
    [SerializeField] private bool disableGravityWhileFrozen = true;

    [Header("Gizmos Preview (Editor)")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private bool drawOnlyWhenSelected = true;
    [SerializeField] private float gizmoPointRadius = 0.15f;
    [Tooltip("Если задано, Gizmos будут рисоваться по этому конфигу (без Play). Если пусто, будет рисоваться последний Spawn() конфиг.")]
    [SerializeField] private LevelSpawnConfig previewConfig;

    private LevelSpawnConfig _lastSpawnedConfig;

    // кеши для разморозки
    private readonly List<Rigidbody> _frozenBodies = new();
    private readonly List<bool> _frozenBodiesPrevGravity = new();

    private readonly List<Collider> _disabledColliders = new();

    public void Clear()
    {
        if (itemsParent == null) return;

        for (int i = itemsParent.childCount - 1; i >= 0; i--)
            Destroy(itemsParent.GetChild(i).gameObject);
    }

public void Spawn(LevelSpawnConfig config, ItemCatalog catalogOverride = null)
{
    if (config == null)
    {
        Debug.LogWarning("LevelItemSpawner: config is null");
        return;
    }

    // ✅ берём override, если задан, иначе дефолтный из инспектора
    var catalog = catalogOverride != null ? catalogOverride : itemCatalog;
    if (catalog == null)
    {
        Debug.LogError("LevelItemSpawner: ItemCatalog not set (override and default are null)");
        return;
    }

    if (itemsParent == null)
    {
        Debug.LogError("LevelItemSpawner: itemsParent not set");
        return;
    }

    _lastSpawnedConfig = config;

    if (clearBeforeSpawn)
        Clear();

    _frozenBodies.Clear();
    _frozenBodiesPrevGravity.Clear();
    _disabledColliders.Clear();

    foreach (var g in config.groups)
    {
        // ✅ ВАЖНО: берём prefab из выбранного catalog, а не из itemCatalog
        var prefab = catalog.GetWorldPrefab(g.type);
        if (prefab == null)
        {
            Debug.LogWarning($"LevelItemSpawner: No worldPrefab for {g.type}");
            continue;
        }

        var points = BuildPoints(g);

        var rot = Quaternion.Euler(0f, g.rotationY, 0f);
        var rng = (g.seed != 0) ? new System.Random(g.seed) : null;

        foreach (var localPoint in points)
        {
            var pos = g.center + rot * localPoint;
            pos.y = spawnY;

            if (g.jitter > 0f)
            {
                pos.x += Rand(rng, -g.jitter, g.jitter);
                pos.z += Rand(rng, -g.jitter, g.jitter);
            }

            var go = Instantiate(prefab, pos, Quaternion.identity, itemsParent);
            ApplyStabilization(go);
        }
    }

    if (stabilization != SpawnStabilizationMode.None && unfreezeDelay > 0f)
        StartCoroutine(UnfreezeRoutine());
    else
        FinishStabilizationImmediately();
}

    private void ApplyStabilization(GameObject go)
    {
        if (go == null) return;

        switch (stabilization)
        {
            case SpawnStabilizationMode.None:
                return;

            case SpawnStabilizationMode.FreezeRigidbodiesKinematic:
            {
                // морозим ВСЕ Rigidbody в объекте (и дочерних), чтобы ничего не толкалось
                var bodies = go.GetComponentsInChildren<Rigidbody>(includeInactive: true);
                foreach (var rb in bodies)
                {
                    if (rb == null) continue;

                    _frozenBodies.Add(rb);
                    _frozenBodiesPrevGravity.Add(rb.useGravity);

                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;

                    if (disableGravityWhileFrozen)
                        rb.useGravity = false;

                    rb.isKinematic = true;   // ✅ физика не двигает
                }
                break;
            }

            case SpawnStabilizationMode.DisableColliders:
            {
                // временно отключаем коллайдеры, потом включим
                var cols = go.GetComponentsInChildren<Collider>(includeInactive: true);
                foreach (var c in cols)
                {
                    if (c == null) continue;
                    if (!c.enabled) continue;

                    _disabledColliders.Add(c);
                    c.enabled = false;
                }
                break;
            }
        }
    }

    private IEnumerator UnfreezeRoutine()
    {
        yield return new WaitForSeconds(unfreezeDelay);
        FinishStabilizationImmediately();
    }

    private void FinishStabilizationImmediately()
    {
        switch (stabilization)
        {
            case SpawnStabilizationMode.FreezeRigidbodiesKinematic:
            {
                for (int i = 0; i < _frozenBodies.Count; i++)
                {
                    var rb = _frozenBodies[i];
                    if (!rb) continue;

                    rb.isKinematic = false;

                    if (disableGravityWhileFrozen)
                    {
                        // вернуть гравитацию как было
                        bool prev = (i < _frozenBodiesPrevGravity.Count) ? _frozenBodiesPrevGravity[i] : true;
                        rb.useGravity = prev;
                    }

                    // чтобы не стартанули с "остаточной паникой"
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.Sleep(); // пусть спокойно полежат и подумают о своём поведении
                }

                _frozenBodies.Clear();
                _frozenBodiesPrevGravity.Clear();
                break;
            }

            case SpawnStabilizationMode.DisableColliders:
            {
                foreach (var c in _disabledColliders)
                {
                    if (c) c.enabled = true;
                }
                _disabledColliders.Clear();
                break;
            }
        }
    }

    private static float Rand(System.Random rng, float min, float max)
    {
        if (rng == null) return UnityEngine.Random.Range(min, max);
        return (float)(min + (max - min) * rng.NextDouble());
    }

    // ---------- Points generation ----------

    private static List<Vector3> BuildPoints(SpawnGroup g)
    {
        var list = new List<Vector3>(128);

        switch (g.formation)
        {
            case FormationType.Circle:
            {
                int count = Mathf.Max(1, g.circleCount);
                for (int i = 0; i < count; i++)
                {
                    float t = (float)i / count;
                    float ang = t * Mathf.PI * 2f;
                    float x = Mathf.Cos(ang) * g.circleRadius;
                    float z = Mathf.Sin(ang) * g.circleRadius;
                    list.Add(new Vector3(x, 0f, z));
                }
                break;
            }

            case FormationType.Line:
            {
                int count = Mathf.Max(1, g.lineCount);
                Vector3 dir = g.lineDirection.sqrMagnitude < 0.0001f ? Vector3.right : g.lineDirection.normalized;
                float half = (count - 1) * 0.5f;

                for (int i = 0; i < count; i++)
                {
                    float offset = (i - half) * g.lineSpacing;
                    list.Add(dir * offset);
                }
                break;
            }

            case FormationType.Grid:
            {
                int rows = Mathf.Max(1, g.rows);
                int cols = Mathf.Max(1, g.cols);

                float halfX = (cols - 1) * 0.5f;
                float halfZ = (rows - 1) * 0.5f;

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        float x = (c - halfX) * g.spacingX;
                        float z = (r - halfZ) * g.spacingZ;
                        list.Add(new Vector3(x, 0f, z));
                    }
                }
                break;
            }

            case FormationType.CustomPoints:
            {
                if (g.localPoints != null)
                    list.AddRange(g.localPoints);
                break;
            }
        }

        return list;
    }

    // ---------- Gizmos preview ----------

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        if (drawOnlyWhenSelected) return;
        DrawConfigGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        if (!drawOnlyWhenSelected) return;
        DrawConfigGizmos();
    }

    private void DrawConfigGizmos()
    {
        var cfg = previewConfig != null ? previewConfig : _lastSpawnedConfig;
        if (cfg == null || cfg.groups == null) return;

        // рисуем точки
        foreach (var g in cfg.groups)
        {
            var pts = BuildPoints(g);
            var rot = Quaternion.Euler(0f, g.rotationY, 0f);

            // маркер центра
            Gizmos.DrawWireSphere(new Vector3(g.center.x, spawnY, g.center.z), gizmoPointRadius * 1.5f);

            foreach (var lp in pts)
            {
                var pos = g.center + rot * lp;
                pos.y = spawnY;
                Gizmos.DrawSphere(pos, gizmoPointRadius);
            }
        }
    }
}