using UnityEngine;
using GameBridge.Contracts;

public class MagnetBoostButtonUI : BoostButtonUIBase
{
    [SerializeField] private MagnetBoost boost;

    private void Update()
    {
        if (boost == null) return;

        if (fillImage != null)
        {
            if (boost.IsActive)
                fillImage.fillAmount = Mathf.Clamp01(boost.Remaining / Mathf.Max(0.001f, boost.Duration));
            else
                fillImage.fillAmount = 0f;
        }

        if (countText != null && inventory != null)
            countText.text = inventory.AllowBoostsWhenEmpty ? "∞" : inventory.GetCount(BuffType.Magnet).ToString();

        if (button != null)
        {
            if (boost.IsActive) button.interactable = false;
            else button.interactable = (inventory == null) ? true : inventory.CanUse(BuffType.Magnet);
        }
    }

    public void Click()
    {
        if (boost == null || boost.IsActive) return;

        EnsureRefs();

        if (inventory != null)
        {
            if (!inventory.TryConsume(BuffType.Magnet))
                return;

            run?.RegisterBuffUsed(BuffType.Magnet);
        }

        boost.Activate();
    }
}