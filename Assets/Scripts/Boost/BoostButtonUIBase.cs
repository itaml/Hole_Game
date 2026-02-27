using UnityEngine;
using UnityEngine.UI;
using TMPro;

public abstract class BoostButtonUIBase : MonoBehaviour
{
    [SerializeField] protected BuffInventory inventory;

    [Header("UI")]
    [SerializeField] protected Image fillImage;
    [SerializeField] protected Button button;
    [SerializeField] protected TMP_Text countText;

    protected RunController run;

    protected virtual void Awake()
    {
        if (fillImage != null)
            fillImage.type = Image.Type.Filled;

        EnsureRefs();
    }

    protected virtual void OnEnable()
    {
        EnsureRefs();
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

        if (run == null)
        {
#if UNITY_2023_1_OR_NEWER
            run = Object.FindAnyObjectByType<RunController>();
#else
            run = Object.FindFirstObjectByType<RunController>();
#endif
        }
    }
}