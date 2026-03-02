using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameBridge.Contracts;

public abstract class BoostButtonUIBase : MonoBehaviour
{
    [SerializeField] protected BuffInventory inventory;

    [Header("UI")]
    [SerializeField] protected Image fillImage;
    [SerializeField] protected Button button;
    [SerializeField] protected TMP_Text countText;

    protected virtual void Awake()
    {
        if (fillImage != null)
            fillImage.type = Image.Type.Filled;

        EnsureRefs();
    }

    protected virtual void OnEnable()
    {
        EnsureRefs();
        Refresh(); // сразу обновим UI при включении
    }

    protected virtual void Update()
    {
        Refresh();
    }

    protected void EnsureRefs()
    {
        if (inventory == null)
        {
#if UNITY_2023_1_OR_NEWER
            inventory = Object.FindAnyObjectByType<BuffInventory>();
#else
            inventory = Object.FindFirstObjectByType<BuffInventory>();
#endif
        }
    }

    private void Refresh()
    {
        // COUNT TEXT
        if (countText != null && inventory != null)
        {
            countText.text = inventory.AllowBoostsWhenEmpty
                ? "∞"
                : inventory.GetCount(GetBuffType()).ToString();
        }

        // FILL
        if (fillImage != null)
        {
            if (IsBoostActive())
                fillImage.fillAmount = Mathf.Clamp01(GetRemaining() / Mathf.Max(0.001f, GetDuration()));
            else
                fillImage.fillAmount = 0f;
        }

        // BUTTON INTERACTABLE
        if (button != null)
        {
            if (IsBoostActive())
                button.interactable = false;
            else
                button.interactable = (inventory == null) || inventory.CanUse(GetBuffType());
        }
    }

    // ---- to be implemented by each button ----
    protected abstract BuffType GetBuffType();
    protected abstract bool IsBoostActive();
    protected abstract float GetRemaining();
    protected abstract float GetDuration();
}