using System.Collections.Generic;
using UnityEngine;

public static class SpawnConfigCounter
{
    public static Dictionary<ItemType, int> CountAll(LevelSpawnConfig cfg)
    {
        var map = new Dictionary<ItemType, int>();
        if (cfg == null || cfg.groups == null) return map;

        foreach (var g in cfg.groups)
        {
            if (g == null) continue;

            int n = CountGroup(g);
            if (n <= 0) continue;

            if (map.TryGetValue(g.type, out int cur))
                map[g.type] = cur + n;
            else
                map[g.type] = n;
        }

        return map;
    }

    public static int CountGroup(SpawnGroup g)
    {
        if (g == null) return 0;

        switch (g.formation)
        {
            case FormationType.Circle:
                return Mathf.Max(0, g.circleCount);

            case FormationType.Line:
                return Mathf.Max(0, g.lineCount);

            case FormationType.Grid:
                return Mathf.Max(0, g.rows) * Mathf.Max(0, g.cols);

            case FormationType.CustomPoints:
                return g.localPoints != null ? Mathf.Max(0, g.localPoints.Count) : 0;

            default:
                return 0;
        }
    }
}