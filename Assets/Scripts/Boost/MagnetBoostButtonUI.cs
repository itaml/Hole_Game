using UnityEngine;
using GameBridge.Contracts;

public class MagnetBoostButtonUI : BoostButtonUIBase
{
    [SerializeField] private MagnetBoost boost;

    protected override BuffType GetBuffType() => BuffType.Magnet;
    protected override bool IsBoostActive() => boost != null && boost.IsActive;
    protected override float GetRemaining() => boost != null ? boost.Remaining : 0f;
    protected override float GetDuration() => boost != null ? boost.Duration : 1f;

public void Click()
{
    Debug.Log("[MagnetBoostButtonUI] Click() CALLED");

    if (boost == null)
    {
        Debug.LogError("[MagnetBoostButtonUI] boost is NULL");
        return;
    }

    if (boost.IsActive)
    {
        Debug.Log("[MagnetBoostButtonUI] boost already active -> ignore click");
        return;
    }

    EnsureRefs();

    Debug.Log($"[MagnetBoostButtonUI] inventory={(inventory ? inventory.name : "NULL")} allowEmpty={(inventory ? inventory.AllowBoostsWhenEmpty : false)} count={(inventory ? inventory.GetCount(GameBridge.Contracts.BuffType.Magnet) : -1)}");

    if (inventory != null)
    {
        bool ok = inventory.TryConsume(GameBridge.Contracts.BuffType.Magnet);
        Debug.Log($"[MagnetBoostButtonUI] TryConsume(Magnet) => {ok}. NewCount={inventory.GetCount(GameBridge.Contracts.BuffType.Magnet)}");
        if (!ok) return;
    }
    else
    {
        Debug.LogWarning("[MagnetBoostButtonUI] inventory is NULL -> consumption skipped");
    }

    boost.Activate();
}
}