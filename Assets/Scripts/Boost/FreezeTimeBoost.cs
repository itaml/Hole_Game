using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FreezeTimeBoost : MonoBehaviour
{
    [Header("State")]
    public bool IsActive { get; private set; }
    public float Duration => duration;
    public float Remaining => remaining;

    [Header("Timing")]
    [SerializeField] private float duration = 6f;

    [Header("UI (active only while boost active)")]
    [Tooltip("Эти UI-иконки/подсветки будут включаться на время Freeze.")]
    [SerializeField] private GameObject[] activeImages;

    private float remaining;
    private Coroutine routine;

    public void Activate()
    {
        if (IsActive) return;

        IsActive = true;
        remaining = duration;

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

        SetActiveImages(false);
    }

    private IEnumerator Work()
    {
        // Обычное время: зависит от Time.timeScale (как ты и хотел)
        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;
            yield return null;
        }

        IsActive = false;
        remaining = 0f;
        routine = null;

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