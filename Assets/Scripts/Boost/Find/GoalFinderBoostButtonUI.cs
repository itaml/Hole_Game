using UnityEngine;
using GameBridge.Contracts;

public class GoalFinderBoostButtonUI : BoostButtonUIBase
{
    [SerializeField] private GoalFinderBoost boost;

    private void Update()
    {
        if (boost == null) return;

        // fill
        if (fillImage != null)
        {
            if (boost.IsActive)
                fillImage.fillAmount = Mathf.Clamp01(boost.Remaining / Mathf.Max(0.001f, boost.Duration));
            else
                fillImage.fillAmount = 0f;
        }

        // count
        if (countText != null && inventory != null)
            countText.text = inventory.AllowBoostsWhenEmpty ? "∞" : inventory.GetCount(BuffType.Radar).ToString();

        // interactable
        if (button != null)
        {
            if (boost.IsActive) button.interactable = false;
            else button.interactable = (inventory == null) ? true : inventory.CanUse(BuffType.Radar);
        }
    }

    public void Click()
    {
        if (boost == null || boost.IsActive) return;

        EnsureRefs();

        if (inventory != null)
        {
            if (!inventory.TryConsume(BuffType.Radar))
                return;

            run?.RegisterBuffUsed(BuffType.Radar);
        }

        boost.Activate();
    }
}