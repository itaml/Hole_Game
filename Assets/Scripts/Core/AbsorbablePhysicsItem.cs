using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AbsorbablePhysicsItem : MonoBehaviour
{
    [Header("Identity")]
    [field: SerializeField] public ItemType Type { get; private set; } = ItemType.None;

    [field: SerializeField] public Sprite UiIcon { get; private set; }   // ✅ Sprite, не RectTransform

    [Header("Rewards")]
    [field: SerializeField] public int XpValue { get; private set; } = 1;

    [Header("Fit override (optional)")]
    [SerializeField] private float overrideRadius = 0f;

    private Rigidbody _rb;
    private Collider _col;

    private int _defaultLayer;
    private bool _layerSaved;

    public Rigidbody Rb => _rb;
    public Collider Col => _col;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
        _defaultLayer = gameObject.layer;
        _layerSaved = true;
    }

    public float GetApproxRadius()
    {
        if (overrideRadius > 0f) return overrideRadius;

        if (_col != null)
        {
            var b = _col.bounds;
            float d = Mathf.Max(b.size.x, b.size.z);
            return d * 0.5f;
        }

        return 0.25f;
    }

    public void SetPassThroughGround(bool enabled, int passThroughLayer)
    {
        if (this == null) return;
        if (!_layerSaved) { _defaultLayer = gameObject.layer; _layerSaved = true; }

        gameObject.layer = enabled ? passThroughLayer : _defaultLayer;
    }
}