using UnityEngine;

public class HoleBoundsLimiter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Collider groundBounds;     // коллайдер зелёной земли (границы)
    [SerializeField] private HolePhysics holePhysics;    // чтобы взять HoleRadius (или любой источник радиуса)

    [Header("Options")]
    [Tooltip("Если у тебя центр дыры не на transform.position, укажи сдвиг.")]
    [SerializeField] private Vector3 centerOffset = Vector3.zero;

    [Tooltip("Доп. отступ внутрь (на всякий случай).")]
    [SerializeField] private float padding = 0f;

    private void LateUpdate()
    {
        if (groundBounds == null) return;

        Bounds b = groundBounds.bounds;

        // Радиус дыры: берём из HolePhysics (один источник правды).
        float r = holePhysics != null ? holePhysics.HoleRadius : 0f;
        r += padding;

        Vector3 pos = transform.position;
        Vector3 center = pos + centerOffset;

        // Двигаемся по XZ, Y не трогаем
        center.x = Mathf.Clamp(center.x, b.min.x + r, b.max.x - r);
        center.z = Mathf.Clamp(center.z, b.min.z + r, b.max.z - r);

        transform.position = center - centerOffset;
    }
}