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
        Debug.Log("[GrowTempBoostButtonUI] Click() CALLED");

        if (boost == null)
        {
            Debug.LogError("[GrowTempBoostButtonUI] boost is NULL");
            return;
        }

        if (boost.IsActive)
        {
            Debug.Log("[GrowTempBoostButtonUI] boost already active -> ignore click");
            return;
        }

        EnsureRefs();

        Debug.Log(
            $"[GrowTempBoostButtonUI] inventory={(inventory ? inventory.name : "NULL")} " +
            $"allowEmpty={(inventory ? inventory.AllowBoostsWhenEmpty : false)} " +
            $"count={(inventory ? inventory.GetCount(BuffType.GrowTemp) : -1)}"
        );

        if (inventory != null)
        {
            bool ok = inventory.TryConsume(BuffType.GrowTemp);
            Debug.Log($"[GrowTempBoostButtonUI] TryConsume(GrowTemp) => {ok}. NewCount={inventory.GetCount(BuffType.GrowTemp)}");
            if (!ok) return;
        }
        else
        {
            Debug.LogWarning("[GrowTempBoostButtonUI] inventory is NULL -> consumption skipped");
        }

        boost.Activate();
    }
}