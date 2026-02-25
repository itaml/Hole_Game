using System.Collections.Generic;
using UnityEngine;

public class HoleMouthTrigger : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private HolePhysics hole;

    [Header("Pass-through")]
    [SerializeField] private string passThroughLayerName = "ItemsPassThrough";

    [Header("Gating (center + time)")]
    [SerializeField] private float dwellTimeToPass = 0.12f;
    [Range(0.2f, 0.95f)]
    [SerializeField] private float centerGateFactor = 0.55f;

    [Header("Scale rule (XZ only)")]
    [Tooltip("Если true: сравниваем только XZ scale предмета и дыры. Y игнорируется (высокие можно).")]
    [SerializeField] private bool useScaleFitRule = true;

    [Tooltip("Небольшая погрешность. 1.02 означает: предмет может быть на 2% шире/длиннее и всё равно 'влезает'.")]
    [Range(1.0f, 1.2f)]
    [SerializeField] private float scaleFitTolerance = 1.02f;

    [Header("Ground recovery (prevent under-floor)")]
    [SerializeField] private LayerMask groundMask;      // слой пола (Ground)
    [SerializeField] private float recoverRayUp = 1.0f;
    [SerializeField] private float recoverRayDown = 4.0f;
    [SerializeField] private float recoverOffset = 0.02f;

    private int _passLayer;

    private readonly Dictionary<AbsorbablePhysicsItem, float> _insideTime = new();
    private readonly HashSet<AbsorbablePhysicsItem> _seenThisStep = new();

    private void Awake()
    {
        _passLayer = LayerMask.NameToLayer(passThroughLayerName);
        if (_passLayer < 0)
            Debug.LogError($"Layer '{passThroughLayerName}' not found. Create it and set Physics matrix.");
    }

    private void Reset()
    {
        hole = GetComponentInParent<HolePhysics>();
    }

    private void FixedUpdate()
    {
        if (_insideTime.Count == 0)
        {
            _seenThisStep.Clear();
            return;
        }

        var toRemove = ListPool<AbsorbablePhysicsItem>.Get();

        foreach (var kv in _insideTime)
        {
            var item = kv.Key;

            if (item == null)
            {
                toRemove.Add(item);
                continue;
            }

            if (!_seenThisStep.Contains(item))
            {
                item.SetPassThroughGround(false, _passLayer);
                RecoverToGround(item);
                toRemove.Add(item);
            }
        }

        foreach (var item in toRemove)
            _insideTime.Remove(item);

        ListPool<AbsorbablePhysicsItem>.Release(toRemove);
        _seenThisStep.Clear();
    }

    private void OnTriggerStay(Collider other)
    {
        if (hole == null) return;

        var item = other.GetComponentInParent<AbsorbablePhysicsItem>();
        if (item == null || item.Rb == null) return;

        _seenThisStep.Add(item);

        // 1) Проверка "влезает ли" по XZ scale
        if (useScaleFitRule && !FitsByScaleXZ(item))
        {
            item.SetPassThroughGround(false, _passLayer);
            RecoverToGround(item);
            _insideTime.Remove(item);
            return;
        }

        // 2) Копим время внутри MouthTrigger
        float tInside = 0f;
        _insideTime.TryGetValue(item, out tInside);
        tInside += Time.fixedDeltaTime;
        _insideTime[item] = tInside;

        // 3) Центровая зона (чтобы “пролётом” не проваливалось)
        Vector3 c = hole.transform.position;
        Vector3 p = item.Rb.worldCenterOfMass;
        float distXZ = Vector2.Distance(new Vector2(c.x, c.z), new Vector2(p.x, p.z));
        float gateRadius = hole.HoleRadius * centerGateFactor;
        bool inCenterGate = distXZ <= gateRadius;

        // 4) Подтягиваем силами (воронка)
        hole.ApplyHoleForces(item);

        // 5) Pass-through включаем только если реально над центром и прожил чуть-чуть
        bool allowed = inCenterGate && (tInside >= dwellTimeToPass);
        item.SetPassThroughGround(allowed, _passLayer);

        if (!allowed)
            RecoverToGround(item);
    }

    private void OnTriggerExit(Collider other)
    {
        var item = other.GetComponentInParent<AbsorbablePhysicsItem>();
        if (item == null) return;

        item.SetPassThroughGround(false, _passLayer);
        RecoverToGround(item);

        _insideTime.Remove(item);
        _seenThisStep.Remove(item);
    }

    private bool FitsByScaleXZ(AbsorbablePhysicsItem item)
    {
        // сравниваем только ширину/длину, Y игнорируем
        Vector3 i = item.transform.lossyScale;
        Vector3 h = hole.transform.lossyScale;

        float itemXZ = Mathf.Max(Mathf.Abs(i.x), Mathf.Abs(i.z));
        float holeXZ = Mathf.Max(Mathf.Abs(h.x), Mathf.Abs(h.z));

        return itemXZ <= holeXZ * scaleFitTolerance;
    }

    private void RecoverToGround(AbsorbablePhysicsItem item)
    {
        if (item == null || item.Rb == null || item.Col == null) return;
        if (groundMask.value == 0) return;

        Vector3 origin = item.Rb.worldCenterOfMass + Vector3.up * recoverRayUp;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, recoverRayDown, groundMask, QueryTriggerInteraction.Ignore))
        {
            float bottomY = item.Col.bounds.min.y;
            float groundY = hit.point.y;

            if (bottomY < groundY + recoverOffset)
            {
                float lift = (groundY + recoverOffset) - bottomY;
                item.transform.position += new Vector3(0f, lift, 0f);

                var v = item.Rb.linearVelocity;
                if (v.y < 0f) v.y = 0f;
                item.Rb.linearVelocity = v;

                item.Rb.angularVelocity *= 0.2f;
            }
        }
    }
}

static class ListPool<T>
{
    private static readonly Stack<List<T>> Pool = new();
    public static List<T> Get() => Pool.Count > 0 ? Pool.Pop() : new List<T>(16);
    public static void Release(List<T> list) { list.Clear(); Pool.Push(list); }
}