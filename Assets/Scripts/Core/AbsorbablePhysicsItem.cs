using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AbsorbablePhysicsItem : MonoBehaviour
{
    [Header("Identity")]
    [field: SerializeField] public ItemType Type { get; private set; } = ItemType.None;

    [field: SerializeField] public Sprite UiIcon { get; private set; }   // ✅ Sprite, не RectTransform

    [Header("Rewards")] 
    [field: SerializeField] public int XpValue { get; private set; } = 1;

    /// <summary>
    /// Удобный флаг: если XP = 0, то не показываем XP-текст/попап.
    /// </summary>
    public bool HasXp => XpValue > 0;

    [Header("On Absorbed (optional)")]
    [Tooltip("Если задано — будет заспавнено в момент, когда предмет засчитан/проглочен.")]
    [SerializeField] private GameObject onAbsorbedSpawn;

    [Tooltip("Смещение точки спавна относительно позиции предмета (например, чуть выше пола).")]
    [SerializeField] private Vector3 onAbsorbedSpawnOffset = Vector3.zero;

    [Tooltip("Если > 0 — заспавненный объект будет уничтожен через N секунд.")]
    [SerializeField] private float destroySpawnAfterSeconds = 2f;

    [Header("Fit override (optional)")]
    [SerializeField] private float overrideRadius = 0f;

    private Rigidbody _rb;
    private Collider _col;

    private int _defaultLayer;
    private bool _layerSaved;

    public Rigidbody Rb => _rb;
    public Collider Col => _col;

    /// <summary>Можно использовать снаружи, чтобы понять есть ли что спавнить.</summary>
    public GameObject OnAbsorbedSpawnPrefab => onAbsorbedSpawn;

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

    /// <summary>
    /// Вызывай в момент "предмет засчитан/проглочен" (не при касании триггера).
    /// </summary>
public void SpawnOnAbsorbed(Vector3 worldPos)
{
    Debug.Log($"[AbsorbablePhysicsItem] SpawnOnAbsorbed: item={name} " +
              $"prefab={(onAbsorbedSpawn ? onAbsorbedSpawn.name : "NULL")} " +
              $"pos={worldPos} offset={onAbsorbedSpawnOffset} xp={XpValue}");

    if (onAbsorbedSpawn == null) return;

    var go = Instantiate(onAbsorbedSpawn, worldPos + onAbsorbedSpawnOffset, Quaternion.identity);

    Debug.Log($"[AbsorbablePhysicsItem] Spawned GO: {(go ? go.name : "NULL")} activeSelf={(go ? go.activeSelf : false)}");

    if (destroySpawnAfterSeconds > 0f)
        Destroy(go, destroySpawnAfterSeconds);
}
}