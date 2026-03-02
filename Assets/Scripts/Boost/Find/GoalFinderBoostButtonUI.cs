using UnityEngine;
using GameBridge.Contracts;

public class GoalFinderBoostButtonUI : BoostButtonUIBase
{
    [SerializeField] private GoalFinderBoost boost;

    protected override BuffType GetBuffType() => BuffType.Radar;
    protected override bool IsBoostActive() => boost != null && boost.IsActive;
    protected override float GetRemaining() => boost != null ? boost.Remaining : 0f;
    protected override float GetDuration() => boost != null ? boost.Duration : 1f;

    public void Click()
    {
        if (boost == null || boost.IsActive) return;

        EnsureRefs();

        if (inventory != null && !inventory.TryConsume(BuffType.Radar))
            return;

        boost.Activate();
    }
}