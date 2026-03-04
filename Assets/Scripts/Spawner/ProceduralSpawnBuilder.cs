using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Clean ProceduralSpawnBuilder
/// - Выбирает N модулей (уникальные по возможности)
/// - Размещает их плотно в кольце вокруг hole (packing по радиусам + углам)
/// - Строгий no-overlap по AABB (модули квадратные -> ок)
/// - Бомба добавляется с 30 уровня
/// - Каталог (тематика) приходит снаружи: catalogOverride
/// </summary>
public class ProceduralSpawnBuilder : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private LevelItemSpawner spawner;
    [SerializeField] private SpawnModuleLibrary library;
    [SerializeField] private DifficultyCurve curve;

    [Header("Hole / Center")]
    [SerializeField] private Transform hole;

    [Header("Ring")]
    [SerializeField] private float ringMinRadius = 6f;
    [SerializeField] private float ringMaxRadius = 14f;

    [Header("Module footprints (world units)")]
    [SerializeField] private float circleModuleSize = 7f;   // 7x7
    [SerializeField] private float gridModuleSize = 8.5f;   // 8.5x8.5
    [SerializeField] private float gap = 0.10f;             // почти впритык

    [Header("Placement quality")]
    [Tooltip("Сколько радиальных слоёв максимум перебираем (больше = плотнее, но медленнее).")]
    [SerializeField] private int maxRadialLayers = 24;

    [Tooltip("Доп. попытки, если слой получился пустой (чтобы не залипать на одном радиусе).")]
    [SerializeField] private int extraLayerTries = 6;

    [Header("Module Count Growth")]
    [SerializeField] private int modulesBase = 10;     // НА СТАРТЕ ХОТЯ БЫ 10
    [SerializeField] private int modulesMax = 24;
    [SerializeField] private int addEveryLevels = 3;
    [SerializeField] private int addModulesStep = 1;

    [Header("Rules")]
    [SerializeField] private bool forceStartSafeFirst = true;
    [SerializeField] private bool useTagFiltering = true;
    [SerializeField] private bool uniqueModules = true; // делать разные все (пока хватает)

    [Header("Bomb")]
    [SerializeField] private bool enableBomb = true;
    [SerializeField] private int bombFromLevelInclusive = 30;
    [SerializeField] private ItemType bombItemType;
    [SerializeField] private int bombCountMin = 1;
    [SerializeField] private int bombCountMax = 2;
    [SerializeField] private float bombRingMin = 8f;
    [SerializeField] private float bombRingMax = 12f;

    // ===================== PUBLIC API =====================

    /// <summary>
    /// BuildConfig(humanLevel (1..), activeCatalog, seed, goalPoolOverride)
    /// </summary>
    public LevelSpawnConfig BuildConfig(int levelIndex, ItemCatalog catalogOverride, int seedOverride = 0, GoalPool goalPoolOverride = null)
    {
        if (!library || !curve)
        {
            Debug.LogError("[ProcGen] Missing library/curve");
            return null;
        }

        if (catalogOverride == null)
        {
            Debug.LogError("[ProcGen] catalogOverride is NULL");
            return null;
        }

        var rng = (seedOverride != 0)
            ? new System.Random(seedOverride)
            : new System.Random(levelIndex * 10007);

        var unlock = curve.GetUnlock(levelIndex);
        int budget = curve.GetBudget(levelIndex, rng); // можно оставить, даже если не используем жёстко

        int targetCount = GetTargetModuleCount(levelIndex);
        var picked = PickModules(targetCount, unlock, rng);

        var runtimeCfg = ScriptableObject.CreateInstance<LevelSpawnConfig>();
        runtimeCfg.groups = new List<SpawnGroup>(256);

        PlaceModulesPacked(picked, runtimeCfg.groups, rng, goalPoolOverride);

        TryAddBombGroup(levelIndex, runtimeCfg.groups, rng, catalogOverride);

        Debug.Log($"[ProcGen] level={levelIndex} budget={budget} modules={picked.Count} groups={runtimeCfg.groups.Count}");
        return runtimeCfg;
    }

    public void BuildAndSpawn(int levelIndex, ItemCatalog catalogOverride, int seedOverride = 0, GoalPool goalPoolOverride = null)
    {
        if (!spawner)
        {
            Debug.LogError("[ProcGen] spawner missing");
            return;
        }

        var cfg = BuildConfig(levelIndex, catalogOverride, seedOverride, goalPoolOverride);
        if (cfg == null || cfg.groups == null || cfg.groups.Count == 0)
        {
            Debug.LogError("[ProcGen] empty config");
            return;
        }

        spawner.Spawn(cfg, catalogOverride);
    }

    private int GetTargetModuleCount(int humanLevel)
    {
        humanLevel = Mathf.Max(1, humanLevel);

        int adds = (humanLevel - 1) / Mathf.Max(1, addEveryLevels);
        int count = modulesBase + adds * Mathf.Max(1, addModulesStep);

        return Mathf.Clamp(count, 1, Mathf.Max(1, modulesMax));
    }

    // ===================== MODULE PICKING =====================

   private List<SpawnModule> PickModules(int targetCount, DifficultyCurve.UnlockRule unlock, System.Random rng)
{
    var candidates = new List<SpawnModule>(library.modules.Count);

    foreach (var m in library.modules)
    {
        if (!m) continue;

        if (useTagFiltering && (m.tags & unlock.allowedTags) == 0)
            continue;

        candidates.Add(m);
    }

    if (candidates.Count == 0)
    {
        foreach (var m in library.modules)
            if (m) candidates.Add(m);
    }

    if (candidates.Count == 0)
        return new List<SpawnModule>();

    var result = new List<SpawnModule>(targetCount);

    // start safe первым (если есть)
    SpawnModule last = null;
    if (forceStartSafeFirst)
    {
        var ss = FindModuleWithTag(candidates, ModuleTag.StartSafe, rng);
        if (ss != null)
        {
            result.Add(ss);
            last = ss;
        }
    }

    // чтобы не было "один и тот же" постоянно: держим небольшой буфер последних
    int recentBuffer = Mathf.Min(3, candidates.Count - 1);
    var recent = new Queue<SpawnModule>(recentBuffer);
    if (last != null && recentBuffer > 0) recent.Enqueue(last);

    int guard = 0;
    while (result.Count < targetCount && guard++ < 5000)
    {
        var m = candidates[rng.Next(0, candidates.Count)];
        if (!m) continue;

        // не повторяем прям подряд
        if (last == m) continue;

        // не повторяем слишком часто (в пределах буфера)
        bool inRecent = false;
        foreach (var r in recent)
        {
            if (r == m) { inRecent = true; break; }
        }
        if (inRecent) continue;

        result.Add(m);

        last = m;
        if (recentBuffer > 0)
        {
            recent.Enqueue(m);
            while (recent.Count > recentBuffer) recent.Dequeue();
        }
    }

    return result;
}

    private SpawnModule FindModuleWithTag(List<SpawnModule> list, ModuleTag tag, System.Random rng)
    {
        var tmp = new List<SpawnModule>();
        foreach (var m in list)
            if (m && (m.tags & tag) != 0) tmp.Add(m);

        if (tmp.Count == 0) return null;
        return tmp[rng.Next(0, tmp.Count)];
    }

    // ===================== PLACEMENT (PACKED RING) =====================

    private struct PlacedRect
    {
        public Rect rectXZ;
    }

    private void PlaceModulesPacked(
        List<SpawnModule> modules,
        List<SpawnGroup> outGroups,
        System.Random rng,
        GoalPool goalPoolOverride)
    {
        if (modules == null || modules.Count == 0) return;

        Vector3 center = (hole != null) ? hole.position : Vector3.zero;

        // занятые AABB по XZ
        var placed = new List<PlacedRect>(modules.Count);

        // радиусный шаг примерно по "самому большому модулю"
        float maxSize = Mathf.Max(circleModuleSize, gridModuleSize);
        float layerStep = maxSize + gap;

        // стартуем с min
        float rStart = Mathf.Max(0.01f, ringMinRadius);
        float rEnd = Mathf.Max(rStart, ringMaxRadius);

        int moduleIndex = 0;

        // Несколько проходов по слоям: r = rStart + layer * layerStep
        // На каждом слое пробуем углы с шагом, зависящим от размера модуля (чтобы по окружности реально уместилось много).
        for (int layer = 0; layer < maxRadialLayers && moduleIndex < modules.Count; layer++)
        {
            float r = rStart + layer * layerStep;
            if (r > rEnd) break;

            // рандомный поворот слоя, чтобы уровни отличались
            float baseAngle = (float)rng.NextDouble() * 360f;

            int placedThisLayer = 0;

            // пытаться на этом слое несколько раз (чуть сдвигая базовый угол)
            for (int extra = 0; extra < Mathf.Max(1, extraLayerTries) && moduleIndex < modules.Count; extra++)
            {
                float layerAngleOffset = baseAngle + extra * 11.5f;

                // чтобы шаг был разумным: берём размер текущего (или максимальный) и делаем шаг по дуге
                float sizeGuess = maxSize;
                float arcStep = sizeGuess + gap; // сколько хотим дуги между центрами
                float stepRad = arcStep / Mathf.Max(0.5f, r);
                float stepDeg = Mathf.Clamp(stepRad * Mathf.Rad2Deg, 6f, 30f);

                int steps = Mathf.CeilToInt(360f / stepDeg);

                for (int s = 0; s < steps && moduleIndex < modules.Count; s++)
                {
                    var module = modules[moduleIndex];
                    if (!module) { moduleIndex++; continue; }

                    Vector2 size = GetModuleFootprint(module);
                    float angleDeg = layerAngleOffset + s * stepDeg;
                    float angle = angleDeg * Mathf.Deg2Rad;

                    Vector3 pos = new Vector3(
                        center.x + Mathf.Cos(angle) * r,
                        center.y,
                        center.z + Mathf.Sin(angle) * r
                    );

                    Rect rect = MakeFootprintRect(pos, size, gap);

                    if (OverlapsAny(rect, placed))
                        continue;

                    placed.Add(new PlacedRect { rectXZ = rect });
                    placedThisLayer++;

                    // Ротация: можно оставить 0/90/180/270, но модули квадратные и так
                    int rotY = (rng.Next(0, 4) * 90);

                    bool mirrorX = rng.NextDouble() < 0.5;
                    bool mirrorZ = rng.NextDouble() < 0.5;

                    EmitModule(module, pos, rotY, mirrorX, mirrorZ, outGroups, rng, goalPoolOverride);

                    moduleIndex++;
                }
            }

            // если слой вообще пустой, то всё равно идём дальше (иначе можно залипнуть)
            _ = placedThisLayer;
        }

        // Если вдруг не успели разместить все (кольцо маленькое или modulesMax огромный)
        // то просто остановимся. Лучше “меньше, но красиво”, чем “все в одной точке”.
        if (moduleIndex < modules.Count)
        {
            Debug.LogWarning($"[ProcGen] Not enough space in ring to place all modules. Placed {moduleIndex}/{modules.Count}. Consider increasing ringMaxRadius.");
        }
    }

    private Vector2 GetModuleFootprint(SpawnModule module)
    {
        float s = (module.shape == SpawnModule.ModuleShape.Circle) ? circleModuleSize : gridModuleSize;
        return new Vector2(s, s);
    }

    private Rect MakeFootprintRect(Vector3 center, Vector2 size, float padding)
    {
        float halfX = Mathf.Max(0.1f, size.x * 0.5f + padding);
        float halfZ = Mathf.Max(0.1f, size.y * 0.5f + padding);

        return new Rect(
            center.x - halfX,
            center.z - halfZ,
            halfX * 2f,
            halfZ * 2f
        );
    }

    private static bool OverlapsAny(Rect r, List<PlacedRect> placed)
    {
        for (int i = 0; i < placed.Count; i++)
            if (r.Overlaps(placed[i].rectXZ)) return true;
        return false;
    }

    // ===================== EMIT MODULE =====================

    private void EmitModule(
        SpawnModule module,
        Vector3 worldCenter,
        float rotationY,
        bool mirrorX,
        bool mirrorZ,
        List<SpawnGroup> outGroups,
        System.Random rng,
        GoalPool goalPoolOverride)
    {
        if (module.groups == null) return;

        var rot = Quaternion.Euler(0f, rotationY, 0f);

        foreach (var t in module.groups)
        {
            if (t == null) continue;

            ItemType finalType = t.type;

            // если группа хочет брать тип из goal pool
            if (t.randomTypeFromGoalPool && goalPoolOverride != null)
                finalType = PickTypeFromGoalPool(goalPoolOverride, rng, finalType);

            Vector3 center = worldCenter + rot * Mirror(new Vector3(t.localCenter.x, 0f, t.localCenter.z), mirrorX, mirrorZ);

            var g = new SpawnGroup
            {
                type = finalType,
                formation = t.formation,

                center = center,
                rotationY = t.rotationY + rotationY,

                jitter = t.jitter,
                seed = t.seed,

                circleCount = t.circleCount,
                circleRadius = t.circleRadius,

                lineCount = t.lineCount,
                lineDirection = rot * Mirror(t.lineDirection, mirrorX, mirrorZ),
                lineSpacing = t.lineSpacing,

                rows = t.rows,
                cols = t.cols,
                spacingX = t.spacingX,
                spacingZ = t.spacingZ,

                localPoints = t.localPoints != null ? new List<Vector3>(t.localPoints) : null
            };

            if (g.formation == FormationType.CustomPoints && g.localPoints != null)
            {
                for (int i = 0; i < g.localPoints.Count; i++)
                    g.localPoints[i] = Mirror(g.localPoints[i], mirrorX, mirrorZ);
            }

            outGroups.Add(g);
        }
    }

    private static Vector3 Mirror(Vector3 v, bool mirrorX, bool mirrorZ)
    {
        if (mirrorX) v.x *= -1f;
        if (mirrorZ) v.z *= -1f;
        return v;
    }

    // ===================== GOAL POOL PICK =====================

    private ItemType PickTypeFromGoalPool(GoalPool pool, System.Random rng, ItemType fallback)
    {
        if (pool == null || pool.items == null || pool.items.Count == 0)
            return fallback;

        int sum = 0;
        foreach (var it in pool.items)
            if (it != null) sum += Mathf.Max(0, it.weight);

        if (sum <= 0)
        {
            var it = pool.items[rng.Next(0, pool.items.Count)];
            return it != null ? it.type : fallback;
        }

        int roll = rng.Next(0, sum);
        int acc = 0;

        foreach (var it in pool.items)
        {
            if (it == null) continue;
            acc += Mathf.Max(0, it.weight);
            if (roll < acc)
                return it.type;
        }

        return fallback;
    }

    // ===================== BOMB =====================

    private void TryAddBombGroup(int levelIndex, List<SpawnGroup> groups, System.Random rng, ItemCatalog catalogOverride)
    {
        if (!enableBomb) return;
        if (levelIndex < bombFromLevelInclusive) return;
        if (groups == null) return;

        if (catalogOverride == null)
        {
            Debug.LogWarning("[ProcGen] Bomb skipped: catalogOverride null");
            return;
        }

        if (catalogOverride.GetWorldPrefab(bombItemType) == null)
        {
            Debug.LogWarning($"[ProcGen] Bomb skipped: catalog has no worldPrefab for {bombItemType}");
            return;
        }

        int min = Mathf.Min(bombCountMin, bombCountMax);
        int max = Mathf.Max(bombCountMin, bombCountMax);
        int count = rng.Next(min, max + 1);

        float radius = (float)(bombRingMin + (bombRingMax - bombRingMin) * rng.NextDouble());

        groups.Add(new SpawnGroup
        {
            type = bombItemType,
            formation = FormationType.Circle,
            center = hole != null ? hole.position : Vector3.zero,
            rotationY = 0f,
            jitter = 0.15f,
            seed = 0,
            circleCount = count,
            circleRadius = radius
        });

        Debug.Log($"[ProcGen] Bomb added: level={levelIndex} count={count} radius={radius:0.00}");
    }
}