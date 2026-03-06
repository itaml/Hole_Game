using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GrowTempBoost : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private HoleGrowth holeGrowth;

    [Header("State")]
    public bool IsActive { get; private set; }
    public float Duration => duration;
    public float Remaining => remaining;

    [Header("Timing")]
    [SerializeField] private float duration = 6f;

    [Header("Effect")]
    [Tooltip("Во сколько раз увеличить размер дыры на время буста.")]
    [SerializeField] private float scaleMultiplier = 1.35f;

    [Header("UI (active only while boost active)")]
    [SerializeField] private Image[] activeImages;

    private float remaining;
    private Coroutine routine;

    private void Awake()
    {
        if (holeGrowth == null)
            holeGrowth = FindFirstObjectByType<HoleGrowth>();

        SetActiveImages(false);
    }

    /// <summary>
    /// Включить буст.
    /// Если буст уже активен, таймер перезапустится.
    /// </summary>
    public void Activate()
    {
        if (holeGrowth == null)
        {
            Debug.LogError("[GrowTempBoost] HoleGrowth not found.");
            return;
        }

        if (routine != null)
            StopCoroutine(routine);

        IsActive = true;
        remaining = duration;

        holeGrowth.SetTempScaleMultiplier(scaleMultiplier);
        SetActiveImages(true);

        routine = StartCoroutine(Work());

        SfxClipRouter.Instance?.Play(SfxKey.Size);
    }

    /// <summary>
    /// Принудительно выключить буст.
    /// </summary>
    public void Stop()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        IsActive = false;
        remaining = 0f;

        if (holeGrowth != null)
            holeGrowth.ClearTempScaleMultiplier();

        SetActiveImages(false);
    }

    private IEnumerator Work()
    {
        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;
            yield return null;
        }

        routine = null;
        IsActive = false;
        remaining = 0f;

        if (holeGrowth != null)
            holeGrowth.ClearTempScaleMultiplier();

        SetActiveImages(false);
    }

    private void SetActiveImages(bool on)
    {
        if (activeImages == null) return;

        for (int i = 0; i < activeImages.Length; i++)
        {
            if (activeImages[i] != null)
                activeImages[i].gameObject.SetActive(on);
        }
    }
}