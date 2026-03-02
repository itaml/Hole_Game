using UnityEngine;
using GameBridge.Contracts;

public class BuffInventory : MonoBehaviour
{
    public static BuffInventory Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool allowBoostsWhenEmpty = false;
    public bool AllowBoostsWhenEmpty => allowBoostsWhenEmpty;

    public int GrowTempCount { get; private set; }
    public int RadarCount { get; private set; }
    public int MagnetCount { get; private set; }
    public int FreezeTimeCount { get; private set; }

    public void ApplyRunConfig(RunConfig cfg)
    {
        if (cfg == null) return;

        GrowTempCount = Mathf.Max(0, cfg.buff1Count);
        RadarCount = Mathf.Max(0, cfg.buff2Count);
        MagnetCount = Mathf.Max(0, cfg.buff3Count);
        FreezeTimeCount = Mathf.Max(0, cfg.buff4Count);

        Debug.Log($"[BuffInventory] Init buffs: ({GrowTempCount},{RadarCount},{MagnetCount},{FreezeTimeCount})");
    }

    public int GetCount(BuffType type) => type switch
    {
        BuffType.GrowTemp => GrowTempCount,
        BuffType.Radar => RadarCount,
        BuffType.Magnet => MagnetCount,
        BuffType.FreezeTime => FreezeTimeCount,
        _ => 0
    };

    public bool CanUse(BuffType type) => allowBoostsWhenEmpty || GetCount(type) > 0;

    public bool TryConsume(BuffType type)
    {
Debug.Log($"[BuffInventory:{name}] TryConsume({type}) allowEmpty={allowBoostsWhenEmpty} before=({GrowTempCount},{RadarCount},{MagnetCount},{FreezeTimeCount})");

        switch (type)
        {
            case BuffType.GrowTemp:
                if (GrowTempCount > 0) { GrowTempCount--; return true; }
                return allowBoostsWhenEmpty;

            case BuffType.Radar:
                if (RadarCount > 0) { RadarCount--; return true; }
                return allowBoostsWhenEmpty;

            case BuffType.Magnet:
                if (MagnetCount > 0) { MagnetCount--; return true; }
                return allowBoostsWhenEmpty;

            case BuffType.FreezeTime:
                if (FreezeTimeCount > 0) { FreezeTimeCount--; return true; }
                return allowBoostsWhenEmpty;

            default:
                return false;
        }
    }
}