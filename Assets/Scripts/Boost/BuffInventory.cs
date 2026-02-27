using UnityEngine;
using GameBridge.Contracts;

public class BuffInventory : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool allowBoostsWhenEmpty = false;

    public bool AllowBoostsWhenEmpty => allowBoostsWhenEmpty;

    public int GrowTempCount { get; private set; }
    public int RadarCount { get; private set; }
    public int MagnetCount { get; private set; }
    public int FreezeTimeCount { get; private set; }

    // сколько раз реально использовали в этом ране
    public int GrowTempUsed { get; private set; }
    public int RadarUsed { get; private set; }
    public int MagnetUsed { get; private set; }
    public int FreezeTimeUsed { get; private set; }

    public void ResetUsedCounters()
    {
        GrowTempUsed = RadarUsed = MagnetUsed = FreezeTimeUsed = 0;
    }

    public void ApplyRunConfig(RunConfig cfg)
    {
        if (cfg == null) return;

        GrowTempCount = Mathf.Max(0, cfg.buff1Count);
        RadarCount = Mathf.Max(0, cfg.buff2Count);
        MagnetCount = Mathf.Max(0, cfg.buff3Count);
        FreezeTimeCount = Mathf.Max(0, cfg.buff4Count);

        ResetUsedCounters();
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
        if (allowBoostsWhenEmpty)
            return true; // для теста не списываем и не считаем used (или хочешь считать? скажу ниже)

        switch (type)
        {
            case BuffType.GrowTemp:
                if (GrowTempCount <= 0) return false;
                GrowTempCount--; GrowTempUsed++; return true;

            case BuffType.Radar:
                if (RadarCount <= 0) return false;
                RadarCount--; RadarUsed++; return true;

            case BuffType.Magnet:
                if (MagnetCount <= 0) return false;
                MagnetCount--; MagnetUsed++; return true;

            case BuffType.FreezeTime:
                if (FreezeTimeCount <= 0) return false;
                FreezeTimeCount--; FreezeTimeUsed++; return true;

            default:
                return false;
        }
    }
}