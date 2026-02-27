using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagnetBoost : MonoBehaviour
{
    [Header("State")]
    public bool IsActive { get; private set; }
    public float Duration => duration;
    public float Remaining => remaining;

    [Header("Timing")]
    [SerializeField] private float duration = 6f;

    [Header("Target")]
    [Tooltip("Куда тянуть предметы. Обычно это transform дыры/центра.")]
    [SerializeField] private Transform pullTarget;

    [Header("Magnet params")]
    [SerializeField] private float radius = 10f;
    [SerializeField] private float force = 45f;
    [SerializeField] private float maxPullSpeed = 12f;
    [SerializeField] private float minDistanceStop = 0.35f;

    [Header("Filter")]
    [SerializeField] private LayerMask itemMask = ~0;
    [SerializeField] private bool requireAbsorbableItem = true;

    [Header("Scale assist (easier absorb)")]
    [Tooltip("Во сколько раз уменьшать предметы во время магнита (0.7 = на 30% меньше).")]
    [Range(0.3f, 1f)]
    [SerializeField] private float shrinkMultiplier = 0.75f;

    [Tooltip("Скорость изменения масштаба.")]
    [SerializeField] private float shrinkSpeed = 8f;

    [Tooltip("Возвращать исходный масштаб после окончания магнита.")]
    [SerializeField] private bool restoreScaleOnStop = true;

    [Header("Performance")]
    [SerializeField] private float scanInterval = 0.06f;
    [SerializeField] private int maxItemsPerScan = 60;

    private float remaining;
    private Coroutine routine;

    private readonly Collider[] hits = new Collider[128];

    // Храним оригинальные масштабы, чтобы вернуть обратно
    private readonly Dictionary<Transform, Vector3> originalScales = new();

    public void SetTarget(Transform target) => pullTarget = target;

    public void Activate()
    {
        if (IsActive) return;

        if (pullTarget == null)
        {
            Debug.LogError("[MagnetBoost] pullTarget is NULL. Assign hole/target transform.");
            return;
        }

        IsActive = true;
        remaining = duration;

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

        if (restoreScaleOnStop)
            RestoreAllScaled();
        else
            originalScales.Clear();
    }

    private IEnumerator Work()
{
    float last = Time.time;

    while (remaining > 0f)
    {
        float now = Time.time;
        float dt = now - last;
        last = now;

        remaining -= dt;

        PullNearbyItems();

        if (scanInterval > 0f)
            yield return new WaitForSeconds(scanInterval);
        else
            yield return null;
    }

    IsActive = false;
    remaining = 0f;
    routine = null;

    if (restoreScaleOnStop)
        RestoreAllScaled();
    else
        originalScales.Clear();
}

    private void PullNearbyItems()
    {
        if (pullTarget == null) return;

        Vector3 center = pullTarget.position;

        int count = Physics.OverlapSphereNonAlloc(center, radius, hits, itemMask, QueryTriggerInteraction.Ignore);
        if (count <= 0) return;

        int processed = 0;

        for (int i = 0; i < count; i++)
        {
            if (processed >= maxItemsPerScan) break;

            Collider col = hits[i];
            if (!col) continue;

            Rigidbody rb = col.attachedRigidbody;
            if (!rb) continue;

            Transform tr = rb.transform;

            if (requireAbsorbableItem)
            {
                if (rb.GetComponent<AbsorbablePhysicsItem>() == null &&
                    col.GetComponentInParent<AbsorbablePhysicsItem>() == null)
                    continue;
            }

            // ---- Scale assist ----
            ApplyShrink(tr);

            // ---- Pull force ----
            Vector3 toCenter = (center - rb.worldCenterOfMass);
            float dist = toCenter.magnitude;
            if (dist < minDistanceStop) continue;

            Vector3 dir = toCenter / Mathf.Max(0.0001f, dist);

            float t = Mathf.Clamp01(1f - (dist / radius));
            float f = force * Mathf.Lerp(0.35f, 1f, t);

            if (rb.linearVelocity.magnitude < maxPullSpeed)
                rb.AddForce(dir * f, ForceMode.Acceleration);

            processed++;
        }
    }

    private void ApplyShrink(Transform tr)
    {
        if (tr == null) return;

        // Запоминаем исходный scale один раз
        if (!originalScales.ContainsKey(tr))
            originalScales[tr] = tr.localScale;

        Vector3 original = originalScales[tr];
        Vector3 target = original * Mathf.Clamp(shrinkMultiplier, 0.01f, 1f);

        tr.localScale = Vector3.Lerp(tr.localScale, target, Time.unscaledDeltaTime * Mathf.Max(0.01f, shrinkSpeed));
    }

    private void RestoreAllScaled()
    {
        // Возвращаем только тем, кто ещё жив
        foreach (var kv in originalScales)
        {
            if (kv.Key != null)
                kv.Key.localScale = kv.Value;
        }
        originalScales.Clear();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (pullTarget == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(pullTarget.position, radius);
    }
#endif
}