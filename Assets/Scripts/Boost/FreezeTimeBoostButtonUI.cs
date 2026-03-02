using UnityEngine;
using GameBridge.Contracts;

public class FreezeTimeBoostButtonUI : BoostButtonUIBase
{
    [SerializeField] private FreezeTimeBoost boost;

    protected override BuffType GetBuffType() => BuffType.FreezeTime;
    protected override bool IsBoostActive() => boost != null && boost.IsActive;
    protected override float GetRemaining() => boost != null ? boost.Remaining : 0f;
    protected override float GetDuration() => boost != null ? boost.Duration : 1f;

    public void Click()
    {
        if (boost == null || boost.IsActive) return;

        // списание ТОЛЬКО через inventory
        if (inventory != null && !inventory.TryConsume(BuffType.FreezeTime))
            return;

        boost.Activate();
    }
}