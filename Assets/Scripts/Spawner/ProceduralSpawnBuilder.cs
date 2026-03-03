using System;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralSpawnBuilder : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private LevelItemSpawner spawner;
    [SerializeField] private SpawnModuleLibrary library;
    [SerializeField] private DifficultyCurve curve;

    [Header("Field Bounds (world)")]
    [SerializeField] private Vector3 fieldCenter = Vector3.zero;
    [SerializeField] private Vector2 fieldSize = new Vector2(40f, 40f);

    [Header("Slot Grid (layout)")]
    [SerializeField] private int gridCols = 3;
    [SerializeField] private int gridRows = 2;

    [Header("Rules")]
    [SerializeField] private bool forceStartSafeFirst = true;
    [SerializeField] private bool avoidRepeatingSameModule = true;

    [SerializeField] private float slotJitter = 1.2f;
    [SerializeField] private int[] allowedRotations = { 0, 90, 180, 270 };

    /// <summary>
    /// Собирает runtime LevelSpawnConfig (без спавна).
    /// </summary>
    public LevelSpawnConfig BuildConfig(int levelIndex, int seedOverride = 0)
    {
        if (!library || !curve)
        {
            Debug.LogError("[ProcGen] Missing library/curve");
            return null;
        }

        var rng = (seedOverride != 0)
            ? new System.Random(seedOverride)
            : new System.Random(levelIndex * 10007);

        var unlock = curve.GetUnlock(levelIndex);
        int budget = curve.GetBudget(levelIndex, rng);

        Debug.Log($"[ProcGen] BuildConfig level={levelIndex} budget={budget} allowed={unlock.allowedTags}");

        var picked = PickModules(levelIndex, budget, unlock, rng);

        Debug.Log($"[ProcGen] pickedModules={picked.Count}");
        for (int i = 0; i < picked.Count; i++)
        {
            var m = picked[i];
            Debug.Log($"[ProcGen] picked[{i}]={m.name} tags={m.tags} cost={m.cost} groups={(m.groups != null ? m.groups.Count : -1)}");
        }

        var runtimeCfg = ScriptableObject.CreateInstance<LevelSpawnConfig>();
        runtimeCfg.groups = new List<SpawnGroup>(256);

        PlaceModulesIntoConfig(picked, runtimeCfg.groups, rng);

        Debug.Log($"[ProcGen] BuildConfig groups={runtimeCfg.groups.Count}");
        return runtimeCfg;
    }

    /// <summary>
    /// Старый удобный метод: собрать конфиг и сразу заспавнить.
    /// </summary>
    public void BuildAndSpawn(int levelIndex, ItemCatalog catalogOverride = null, int seedOverride = 0)
    {
        if (!spawner)
        {
            Debug.LogError("[ProcGen] spawner missing");
            return;
        }

        var cfg = BuildConfig(levelIndex, seedOverride);
        if (cfg == null || cfg.groups == null || cfg.groups.Count == 0)
        {
            Debug.LogError("[ProcGen] BuildAndSpawn failed: empty config");
            return;
        }

        spawner.Spawn(cfg, catalogOverride);
    }

    private List<SpawnModule> PickModules(int levelIndex, int budget, DifficultyCurve.UnlockRule unlock, System.Random rng)
    {
        var candidates = new List<SpawnModule>(library.modules.Count);

        foreach (var m in library.modules)
        {
            if (!m) continue;

            // фильтр по тегам (flags)
            if ((m.tags & unlock.allowedTags) == 0) continue;

            candidates.Add(m);
        }

        Debug.Log($"[ProcGen] candidatesByTag={candidates.Count} allowed={unlock.allowedTags}");

        if (candidates.Count == 0)
        {
            // fallback: берём всё, иначе будет пустота
            foreach (var m in library.modules)
                if (m) candidates.Add(m);
        }

        int minCount = Mathf.Max(1, unlock.minModules);
        int maxCount = Mathf.Max(minCount, unlock.maxModules);

        var result = new List<SpawnModule>(maxCount);
        int remaining = Mathf.Max(0, budget);

        // StartSafe первым
        if (forceStartSafeFirst)
        {
            var ss = FindModuleWithTag(candidates, ModuleTag.StartSafe, rng);
            if (ss != null && ss.cost <= remaining)
            {
                result.Add(ss);
                remaining -= ss.cost;
            }
        }

        int guard = 0;
        while (result.Count < maxCount && remaining > 0 && guard++ < 500)
        {
            var m = candidates[rng.Next(0, candidates.Count)];
            if (!m) continue;

            if (avoidRepeatingSameModule && result.Contains(m))
                continue;

            if (m.cost > remaining)
                continue;

            result.Add(m);
            remaining -= m.cost;

            if (result.Count >= minCount && remaining <= 1)
                break;
        }

        // если не набрали минимум, добиваем самыми дешёвыми
        if (result.Count < minCount)
        {
            candidates.Sort((a, b) => a.cost.CompareTo(b.cost));
            foreach (var m in candidates)
            {
                if (!m) continue;
                if (avoidRepeatingSameModule && result.Contains(m)) continue;
                if (m.cost > remaining) continue;

                result.Add(m);
                remaining -= m.cost;
                if (result.Count >= minCount) break;
            }
        }

        return result;
    }

    private void PlaceModulesIntoConfig(List<SpawnModule> modules, List<SpawnGroup> outGroups, System.Random rng)
    {
        if (modules == null || modules.Count == 0) return;

        int slotsCount = Mathf.Max(1, gridCols * gridRows);
        var slotCenters = BuildSlotCenters(slotsCount);

        for (int i = 0; i < modules.Count && i < slotCenters.Count; i++)
        {
            var module = modules[i];
            if (!module) continue;

            // первый модуль в первый слот (обычно StartSafe)
            var baseCenter = (i == 0) ? slotCenters[0] : slotCenters[rng.Next(0, slotCenters.Count)];

            float jitterX = Rand(rng, -slotJitter, slotJitter);
            float jitterZ = Rand(rng, -slotJitter, slotJitter);
            var moduleCenter = baseCenter + new Vector3(jitterX, 0f, jitterZ);

            int rotY = (allowedRotations != null && allowedRotations.Length > 0)
                ? allowedRotations[rng.Next(0, allowedRotations.Length)]
                : 0;

            bool mirrorX = rng.NextDouble() < 0.5;
            bool mirrorZ = rng.NextDouble() < 0.5;

            EmitModule(module, moduleCenter, rotY, mirrorX, mirrorZ, outGroups);
        }
    }

    private void EmitModule(
        SpawnModule module,
        Vector3 worldCenter,
        float rotationY,
        bool mirrorX,
        bool mirrorZ,
        List<SpawnGroup> outGroups)
    {
        var rot = Quaternion.Euler(0f, rotationY, 0f);

        foreach (var t in module.groups)
        {
            if (t == null) continue;

            var g = new SpawnGroup
            {
                type = t.type,
                formation = t.formation,

                center = worldCenter + rot * Mirror(new Vector3(t.localCenter.x, 0f, t.localCenter.z), mirrorX, mirrorZ),
                rotationY = t.rotationY + rotationY,

                jitter = t.jitter,
                seed = t.seed, // можно подмешать, если хочешь

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
                {
                    var lp = g.localPoints[i];
                    g.localPoints[i] = Mirror(lp, mirrorX, mirrorZ);
                }
            }

            outGroups.Add(g);
        }
    }

    private List<Vector3> BuildSlotCenters(int slotsCount)
    {
        var list = new List<Vector3>(slotsCount);

        float cellW = fieldSize.x / Mathf.Max(1, gridCols);
        float cellH = fieldSize.y / Mathf.Max(1, gridRows);

        float startX = fieldCenter.x - fieldSize.x * 0.5f + cellW * 0.5f;
        float startZ = fieldCenter.z - fieldSize.y * 0.5f + cellH * 0.5f;

        for (int r = 0; r < gridRows; r++)
        {
            for (int c = 0; c < gridCols; c++)
            {
                list.Add(new Vector3(
                    startX + c * cellW,
                    fieldCenter.y,
                    startZ + r * cellH
                ));
            }
        }

        return list;
    }

    private SpawnModule FindModuleWithTag(List<SpawnModule> list, ModuleTag tag, System.Random rng)
    {
        var tmp = new List<SpawnModule>();
        foreach (var m in list)
            if (m && (m.tags & tag) != 0) tmp.Add(m);

        if (tmp.Count == 0) return null;
        return tmp[rng.Next(0, tmp.Count)];
    }

    private static Vector3 Mirror(Vector3 v, bool mirrorX, bool mirrorZ)
    {
        if (mirrorX) v.x *= -1f;
        if (mirrorZ) v.z *= -1f;
        return v;
    }

    private static float Rand(System.Random rng, float min, float max)
    {
        return (float)(min + (max - min) * rng.NextDouble());
    }
}