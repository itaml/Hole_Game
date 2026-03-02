using UnityEngine;
using GameBridge.Contracts;

public class GrowTempBoostButtonUI : BoostButtonUIBase
{
    [SerializeField] private GrowTempBoost boost;

    protected override BuffType GetBuffType() => BuffType.GrowTemp;
    protected override bool IsBoostActive() => boost != null && boost.IsActive;
    protected override float GetRemaining() => boost != null ? boost.Remaining : 0f;
    protected override float GetDuration() => boost != null ? boost.Duration : 1f;

    public void Click()
    {
        if (boost == null || boost.IsActive) return;

        EnsureRefs();

        if (inventory != null && !inventory.TryConsume(BuffType.GrowTemp))
            return;

        boost.Activate();
    }
}