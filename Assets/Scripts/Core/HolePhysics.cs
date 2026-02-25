using UnityEngine;

public class HolePhysics : MonoBehaviour
{
[Header("Stabilization (anti spin / anti slingshot)")]
[SerializeField] private bool stabilizeItems = true;
[SerializeField] private float maxAngularSpeed = 4f;
[SerializeField] private float tangentialDamp = 6f;
[SerializeField] private float angularDampBoost = 2f;

    [Header("Hole Settings")]
    [SerializeField] private float holeRadius = 0.6f;
    [SerializeField] private float fitFactor = 0.95f;
    [SerializeField] private Transform holeCenter;

    [Header("Absorb Speed")]
    [Tooltip("Множитель скорости всасывания. 1 = базово, 1.5 = быстрее, 2 = очень быстро.")]
    [Range(0.2f, 10f)]
    [SerializeField] private float absorbSpeed = 1.2f; // <— вот твой “слайдер”

    [Header("Forces (base)")]
    [SerializeField] private float pullCenterMin = 2f;
    [SerializeField] private float pullCenterMax = 8f;
    [SerializeField] private float pullDownMin = 3f;
    [SerializeField] private float pullDownMax = 14f;

    [Header("Speed Limit (base)")]
    [SerializeField] private float maxSpeedNearEdge = 2.5f;
    [SerializeField] private float maxSpeedNearCenter = 6.5f;

    public float HoleRadius => holeRadius;

    private void Reset()
    {
        holeCenter = transform;
    }

    public void SetHoleRadius(float r)
    {
        holeRadius = Mathf.Max(0.05f, r);
    }

    public bool CanFit(AbsorbablePhysicsItem item)
    {
        if (item == null) return false;
        float itemR = item.GetApproxRadius();
        return itemR <= holeRadius * fitFactor;
    }

    // “Гравитационное” всасывание, но теперь регулируемое
    public void ApplyHoleForces(AbsorbablePhysicsItem item)
    {
        if (item == null) return;
        var rb = item.Rb;
        if (rb == null) return;

        Vector3 center = holeCenter.position;
        Vector3 pos = rb.worldCenterOfMass;

        Vector3 toCenter = center - pos;
        toCenter.y = 0f;

        float dist = toCenter.magnitude;
        float t = Mathf.Clamp01(1f - dist / Mathf.Max(0.001f, holeRadius)); // 0..1

        float pullCenter = Mathf.Lerp(pullCenterMin, pullCenterMax, t) * absorbSpeed;
        float pullDown   = Mathf.Lerp(pullDownMin,   pullDownMax,   t) * absorbSpeed;

        Vector3 force = Vector3.down * pullDown;

        if (dist > 0.001f)
            force += toCenter.normalized * pullCenter;

        rb.AddForce(force, ForceMode.Acceleration);

        float maxSpeed = Mathf.Lerp(maxSpeedNearEdge, maxSpeedNearCenter, t) * absorbSpeed;
        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;

        // “мягкость”
        rb.linearDamping = 0.6f;
        rb.angularDamping = 1.2f;

            if (stabilizeItems)
    {
        Stabilize(item, t);
    }
    }

    private void Stabilize(AbsorbablePhysicsItem item, float centerT)
{
    var rb = item.Rb;
    if (rb == null) return;

    // 1) Ограничиваем угловую скорость (перестаем превращать предметы в пропеллер)
    Vector3 w = rb.angularVelocity;
    float wMag = w.magnitude;
    float maxW = Mathf.Lerp(maxAngularSpeed * 0.6f, maxAngularSpeed, centerT);
    if (wMag > maxW)
        rb.angularVelocity = w.normalized * maxW;

    // 2) Гасим тангенциальную скорость вокруг центра
    Vector3 center = holeCenter.position;
    Vector3 pos = rb.worldCenterOfMass;

    Vector3 r = pos - center;
    r.y = 0f;

    if (r.sqrMagnitude > 0.0001f)
    {
        Vector3 radial = r.normalized;

        Vector3 v = rb.linearVelocity;
        Vector3 vXZ = new Vector3(v.x, 0f, v.z);

        // radial component (к центру/от центра)
        Vector3 vRadial = Vector3.Dot(vXZ, radial) * radial;

        // tangential component (вокруг центра) -> её гасим
        Vector3 vTangential = vXZ - vRadial;

        float damp = tangentialDamp * Mathf.Lerp(0.3f, 1f, centerT);
        vTangential = Vector3.Lerp(vTangential, Vector3.zero, Time.fixedDeltaTime * damp);

        Vector3 newVXZ = vRadial + vTangential;
        rb.linearVelocity = new Vector3(newVXZ.x, v.y, newVXZ.z);
    }

    // 3) Усиливаем angular damping, чтобы быстрее успокаивался
    rb.angularDamping = Mathf.Max(rb.angularDamping, Mathf.Lerp(1.5f, 3.0f + angularDampBoost, centerT));
}
}