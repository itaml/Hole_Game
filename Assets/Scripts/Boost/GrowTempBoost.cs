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
    [Tooltip("Во сколько раз увеличить размер/радиус дыры на время буста.")]
    [SerializeField] private float scaleMultiplier = 1.35f;

    [Header("UI (active only while boost active)")]
    [SerializeField] private Image[] activeImages;

    private float remaining;
    private Coroutine routine;

    private void Awake()
    {
        if (holeGrowth == null)
            holeGrowth = FindFirstObjectByType<HoleGrowth>();
    }

    public void Activate()
    {
        if (IsActive) return;

        if (holeGrowth == null)
        {
            Debug.LogError("[GrowTempBoost] HoleGrowth not found/assigned.");
            return;
        }

        IsActive = true;
        remaining = duration;

        holeGrowth.SetTempScaleMultiplier(scaleMultiplier);
        SetActiveImages(true);

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Work());
    }

    public void Stop()
    {
        if (!IsActive) return;

        IsActive = false;
        remaining = 0f;

        if (routine != null) StopCoroutine(routine);
        routine = null;

        if (holeGrowth != null)
            holeGrowth.ClearTempScaleMultiplier();

        SetActiveImages(false);
    }

    private IEnumerator Work()
    {
        while (remaining > 0f)
        {
            remaining -= Time.deltaTime; // обычное время
            yield return null;
        }

        IsActive = false;
        remaining = 0f;
        routine = null;

        if (holeGrowth != null)
            holeGrowth.ClearTempScaleMultiplier();

        SetActiveImages(false);
    }

    private void SetActiveImages(bool on)
    {
        if (activeImages == null) return;
        for (int i = 0; i < activeImages.Length; i++)
            if (activeImages[i] != null)
                activeImages[i].gameObject.SetActive(on);
    }
}