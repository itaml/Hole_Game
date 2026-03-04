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
            int add = CountGroup(g);
            if (add <= 0) continue;

            if (map.TryGetValue(g.type, out int cur)) map[g.type] = cur + add;
            else map[g.type] = add;
        }

        return map;
    }

    private static int CountGroup(SpawnGroup g)
    {
        switch (g.formation)
        {
            case FormationType.Grid:
                return Mathf.Max(1, g.rows) * Mathf.Max(1, g.cols);

            case FormationType.Line:
                return Mathf.Max(1, g.lineCount);

            case FormationType.Circle:
                return Mathf.Max(1, g.circleCount);

            case FormationType.CustomPoints:
                return g.localPoints != null ? g.localPoints.Count : 0;
        }

        return 0;
    }
}