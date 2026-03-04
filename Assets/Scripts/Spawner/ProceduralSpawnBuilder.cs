using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ProceduralSpawnBuilder (Clean + Composition Plan)
/// - Выбирает модули (с учетом unlock tags, если надо)
/// - Строит "план композиции": 1-2 больших GRID, потом чередуем формы
/// - Размещает аккуратно: кольца (layers) + слоты по окружности + сектора
/// - Snap-to-grid для "как в оригинале"
/// - No-overlap AABB по XZ
/// - Бомба с 30 уровня
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
    [SerializeField] private float ringMaxRadius = 22f;

    [Header("Module footprints (world units)")]
    [SerializeField] private float circleModuleSize = 7f;   // 7x7
    [SerializeField] private float gridModuleSize = 8.5f;   // 8.5x8.5
    [SerializeField] private float gap = 0.25f;             // "воздух" между модулями

    [Header("Clean Layout")]
    [SerializeField] private bool snapToGrid = true;
    [SerializeField] private float gridStep = 0.5f;         // 0.5 / 1.0
    [SerializeField] private float angleQuantizeDeg = 15f;  // 15/30 делает "ровно"

    [Header("Distribution")]
    [Tooltip("Сколько секторов по кругу. 6..10 обычно ок.")]
    [SerializeField] private int sectors = 8;

    [Tooltip("Максимум радиальных слоёв. Больше = плотнее/дальше.")]
    [SerializeField] private int maxRadialLayers = 24;

    [Tooltip("Сколько попыток на слой (смещение стартового угла/сектора).")]
    [SerializeField] private int extraLayerTries = 6;

    [Header("Composition Plan")]
    [Tooltip("Сколько больших GRID модулей пытаться поставить первыми.")]
    [SerializeField] private int bigGridsFirstMin = 1;
    [SerializeField] private int bigGridsFirstMax = 2;

    [Tooltip("Стараться чередовать формы (GRID/CIRCLE), чтобы не было каши.")]
    [SerializeField] private bool alternateShapes = true;

    [Tooltip("Не допускать 2 одинаковых модулей подряд (если повторения разрешены).")]
    [SerializeField] private bool avoidSameModuleBackToBack = true;

    [Header("Rules")]
    [SerializeField] private bool forceStartSafeFirst = true;
    [SerializeField] private bool useTagFiltering = true;

    [Header("Module Count Growth")]
    [SerializeField] private int modulesBase = 10;
    [SerializeField] private int modulesMax = 24;
    [SerializeField] private int addEveryLevels = 3;
    [SerializeField] private int addModulesStep = 1;

    [Header("Bomb")]
    [SerializeField] private bool enableBomb = true;
    [SerializeField] private int bombFromLevelInclusive = 30;
    [SerializeField] private ItemType bombItemType;
    [SerializeField] private int bombCountMin = 1;
    [SerializeField] private int bombCountMax = 2;
    [SerializeField] private float bombRingMin = 8f;
    [SerializeField] private float bombRingMax = 12f;

    // ===================== PUBLIC API =====================

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
        int budget = curve.GetBudget(levelIndex, rng); // оставляем для логов/будущего

        int targetCount = GetTargetModuleCount(levelIndex);

        // 1) Берём кандидатов (по тегам или без)
        var candidates = CollectCandidates(unlock);

        // 2) Выбираем модули под "план композиции"
        var picked = PickWithCompositionPlan(candidates, targetCount, unlock, rng);

        // 3) Build runtime cfg
        var runtimeCfg = ScriptableObject.CreateInstance<LevelSpawnConfig>();
        runtimeCfg.groups = new List<SpawnGroup>(256);

        // 4) Place (clean packed ring)
        PlaceModulesClean(picked, runtimeCfg.groups, rng, goalPoolOverride);

        // 5) Bomb
        TryAddBombGroup(levelIndex, runtimeCfg.groups, rng, catalogOverride);

        Debug.Log($"[ProcGen] level={levelIndex} budget={budget} target={targetCount} picked={picked.Count} groups={runtimeCfg.groups.Count}");
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

    // ===================== CANDIDATES =====================

    private List<SpawnModule> CollectCandidates(DifficultyCurve.UnlockRule unlock)
    {
        var list = new List<SpawnModule>(library.modules.Count);

        foreach (var m in library.modules)
        {
            if (!m) continue;

            if (useTagFiltering)
            {
                if ((m.tags & unlock.allowedTags) == 0) continue;
            }

            list.Add(m);
        }

        // fallback: если по тегам пусто - берём всё
        if (list.Count == 0)
        {
            foreach (var m in library.modules)
                if (m) list.Add(m);
        }

        return list;
    }

    // ===================== COMPOSITION PLAN PICK =====================

    private List<SpawnModule> PickWithCompositionPlan(
        List<SpawnModule> candidates,
        int targetCount,
        DifficultyCurve.UnlockRule unlock,
        System.Random rng)
    {
        var result = new List<SpawnModule>(targetCount);
        if (candidates == null || candidates.Count == 0) return result;

        // Чтобы не ломалось, если targetCount больше уникальных
        // (у тебя всего 10 модулей, и это норм).
        // Будем позволять повторы, но:
        // - StartSafe первым
        // - без одинакового подряд
        // - при alternateShapes стараемся чередовать

        // 1) StartSafe первым
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

        // 2) Сколько больших GRID поставить первыми
        int bigCount = rng.Next(Mathf.Min(bigGridsFirstMin, bigGridsFirstMax),
                                Mathf.Max(bigGridsFirstMin, bigGridsFirstMax) + 1);
        bigCount = Mathf.Clamp(bigCount, 0, Mathf.Max(0, targetCount - result.Count));

        // Собираем “большие” кандидаты: shape Grid (или по размеру)
        var gridCandidates = new List<SpawnModule>();
        var circleCandidates = new List<SpawnModule>();
        foreach (var m in candidates)
        {
            if (!m) continue;
            if (m.shape == SpawnModule.ModuleShape.Grid) gridCandidates.Add(m);
            else circleCandidates.Add(m);
        }

        // 2a) добираем big grids
        for (int i = 0; i < bigCount && result.Count < targetCount; i++)
        {
            var pick = PickAvoidingBackToBack(gridCandidates.Count > 0 ? gridCandidates : candidates, rng, last);
            if (pick == null) break;

            result.Add(pick);
            last = pick;
        }

        // 3) Остальное: “план” чередования форм
        SpawnModule.ModuleShape want = SpawnModule.ModuleShape.Circle;

        if (alternateShapes && last != null)
            want = (last.shape == SpawnModule.ModuleShape.Grid) ? SpawnModule.ModuleShape.Circle : SpawnModule.ModuleShape.Grid;

        int guard = 0;
        while (result.Count < targetCount && guard++ < 5000)
        {
            SpawnModule pick = null;

            if (alternateShapes)
            {
                // пытаемся взять желаемую форму
                var list = (want == SpawnModule.ModuleShape.Grid) ? gridCandidates : circleCandidates;
                pick = PickAvoidingBackToBack(list.Count > 0 ? list : candidates, rng, last);

                // если не нашли — берём что есть
                if (pick == null)
                    pick = PickAvoidingBackToBack(candidates, rng, last);

                // переключаем желаемую форму
                want = (want == SpawnModule.ModuleShape.Grid) ? SpawnModule.ModuleShape.Circle : SpawnModule.ModuleShape.Grid;
            }
            else
            {
                pick = PickAvoidingBackToBack(candidates, rng, last);
            }

            if (pick == null) break;

            result.Add(pick);
            last = pick;
        }

        return result;
    }

    private SpawnModule PickAvoidingBackToBack(List<SpawnModule> list, System.Random rng, SpawnModule last)
    {
        if (list == null || list.Count == 0) return null;

        // 8 попыток найти не тот же самый
        for (int i = 0; i < 8; i++)
        {
            var m = list[rng.Next(0, list.Count)];
            if (!m) continue;

            if (avoidSameModuleBackToBack && last == m)
                continue;

            return m;
        }

        // fallback
        return list[rng.Next(0, list.Count)];
    }

    private SpawnModule FindModuleWithTag(List<SpawnModule> list, ModuleTag tag, System.Random rng)
    {
        if (list == null || list.Count == 0) return null;

        var tmp = new List<SpawnModule>();
        foreach (var m in list)
            if (m && (m.tags & tag) != 0) tmp.Add(m);

        if (tmp.Count == 0) return null;
        return tmp[rng.Next(0, tmp.Count)];
    }

    // ===================== PLACEMENT (CLEAN) =====================

    private struct PlacedRect { public Rect rectXZ; }

    private void PlaceModulesClean(
        List<SpawnModule> modules,
        List<SpawnGroup> outGroups,
        System.Random rng,
        GoalPool goalPoolOverride)
    {
        if (modules == null || modules.Count == 0) return;

        Vector3 center = (hole != null) ? hole.position : Vector3.zero;

        var placed = new List<PlacedRect>(modules.Count);

        float maxSize = Mathf.Max(circleModuleSize, gridModuleSize);
        float layerStep = maxSize + gap; // шаг радиальных колец

        float rStart = Mathf.Max(0.01f, ringMinRadius);
        float rEnd = Mathf.Max(rStart, ringMaxRadius);

        int moduleIndex = 0;

        // “секторный” распределитель, чтобы не всё в одну сторону
        int secCount = Mathf.Clamp(sectors, 4, 12);
        int secCursor = rng.Next(0, secCount);

        for (int layer = 0; layer < maxRadialLayers && moduleIndex < modules.Count; layer++)
        {
            float r = rStart + layer * layerStep;
            if (r > rEnd) break;

            // сколько слотов по окружности на этом радиусе
            int slots = Mathf.Max(6, Mathf.RoundToInt((2f * Mathf.PI * r) / (maxSize + gap)));
            float stepDeg = 360f / slots;

            // базовый угол для слоя: привязан к сектору
            float sectorDeg = 360f / secCount;
            float baseAngleDeg = secCursor * sectorDeg + (float)rng.NextDouble() * (sectorDeg * 0.25f);

            int placedThisLayer = 0;

            for (int extra = 0; extra < Mathf.Max(1, extraLayerTries) && moduleIndex < modules.Count; extra++)
            {
                // смещаемся по секторам, чтобы равномерно заполнялось
                int sec = (secCursor + extra) % secCount;
                float offsetDeg = sec * sectorDeg;

                // делаем "оригинальный" ритм: каждый extra прыгаем на следующий сектор
                float layerAngleOffset = baseAngleDeg + offsetDeg;

                for (int s = 0; s < slots && moduleIndex < modules.Count; s++)
                {
                    var module = modules[moduleIndex];
                    if (!module) { moduleIndex++; continue; }

                    Vector2 size = GetModuleFootprint(module);

                    float angleDeg = layerAngleOffset + s * stepDeg;
                    angleDeg = QuantizeAngleDeg(angleDeg);

                    float a = angleDeg * Mathf.Deg2Rad;

                    Vector3 pos = new Vector3(
                        center.x + Mathf.Cos(a) * r,
                        center.y,
                        center.z + Mathf.Sin(a) * r
                    );

                    pos = Snap(pos);

                    Rect rect = MakeFootprintRect(pos, size, gap);

                    if (OverlapsAny(rect, placed))
                        continue;

                    placed.Add(new PlacedRect { rectXZ = rect });
                    placedThisLayer++;

                    // Поворот 0/90/180/270 (аккуратнее выглядит, чем рандомный)
                    int rotY = (rng.Next(0, 4) * 90);
                    bool mirrorX = rng.NextDouble() < 0.5;
                    bool mirrorZ = rng.NextDouble() < 0.5;

                    EmitModule(module, pos, rotY, mirrorX, mirrorZ, outGroups, rng, goalPoolOverride);

                    moduleIndex++;

                    // каждый успешный модуль двигает сектор, чтобы не было "комка"
                    secCursor = (secCursor + 1) % secCount;
                }
            }

            // если слой пустой, всё равно идём дальше
            _ = placedThisLayer;
        }

        if (moduleIndex < modules.Count)
        {
            Debug.LogWarning($"[ProcGen] Not enough space in ring to place all modules. Placed {moduleIndex}/{modules.Count}. Increase ringMaxRadius or reduce modulesMax/gap.");
        }
    }

    private Vector2 GetModuleFootprint(SpawnModule module)
    {
        float s = (module.shape == SpawnModule.ModuleShape.Circle) ? circleModuleSize : gridModuleSize;
        return new Vector2(s, s);
    }

    private Vector3 Snap(Vector3 p)
    {
        if (!snapToGrid) return p;
        float step = Mathf.Max(0.01f, gridStep);
        p.x = Mathf.Round(p.x / step) * step;
        p.z = Mathf.Round(p.z / step) * step;
        return p;
    }

    private float QuantizeAngleDeg(float deg)
    {
        if (angleQuantizeDeg <= 0.01f) return deg;
        return Mathf.Round(deg / angleQuantizeDeg) * angleQuantizeDeg;
    }

    private Rect MakeFootprintRect(Vector3 center, Vector2 size, float padding)
    {
        float halfX = Mathf.Max(0.1f, size.x * 0.5f + padding);
        float halfZ = Mathf.Max(0.1f, size.y * 0.5f + padding);

        return new Rect(center.x - halfX, center.z - halfZ, halfX * 2f, halfZ * 2f);
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
            if (roll < acc) return it.type;
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