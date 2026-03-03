using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BoostBasketSpawner : MonoBehaviour
{
    [Header("Basket Refs")]
    [SerializeField] private Transform basketRoot;          // Model (визуал)
    [SerializeField] private Transform spawnInsidePoint;    // SpawnPointInside (внутри корзины)

    [Header("Boost Prefabs")]
    [SerializeField] private GameObject growWholeLevelBoostPrefab;
    [SerializeField] private GameObject extraLevelTimeBoostPrefab;

    [Header("Inside placement")]
    [SerializeField] private float insideScatterRadius = 0.12f;
    [SerializeField] private float insideUpOffset = 0.05f;

    [Header("Fly In (DOTween)")]
    [SerializeField] private float flyInTime = 1.0f;
    [SerializeField] private Ease flyInEase = Ease.OutCubic;

    [Header("Face Camera")]
    [Tooltip("Повернуть ВИЗУАЛ корзины к камере по прилёту (чтобы не была задом).")]
    [SerializeField] private bool faceCameraOnArrive = true;

    [Tooltip("Доп. поворот по Y (если модель развернута боком/задом). Обычно 180.")]
    [SerializeField] private float faceCameraYawOffset = 180f;

    [Tooltip("Если true — во время полёта тоже будет поворачиваться к камере (смотрится как billboard).")]
    [SerializeField] private bool faceCameraDuringFly = false;

    [Header("Arrive / Reveal / Shoot timing")]
    [SerializeField] private float shakeTime = 0.18f;
    [SerializeField] private float shakeStrength = 0.08f;
    [SerializeField] private int shakeVibrato = 20;

    [SerializeField] private float delayAfterArrive = 0.10f;
    [SerializeField] private float revealBeforeShootSeconds = 0.20f;

    [Header("Shoot physics (UP style)")]
    [Tooltip("Сила выстрела ВВЕРХ (главная).")]
    [SerializeField] private float shootUpImpulse = 3.2f;

    [Tooltip("Немного вперёд (по желанию). 0 = строго вверх.")]
    [SerializeField] private float shootForwardImpulse = 0.6f;

    [Tooltip("Разброс влево-вправо.")]
    [SerializeField] private float shootSideImpulse = 1.6f;

    [Tooltip("Случайный крутящий момент.")]
    [SerializeField] private float randomTorque = 2.0f;

    [Tooltip("Задержка между выстрелами по одному бусту.")]
    [SerializeField] private float perBoostShootStagger = 0.04f;

    [Header("Jelly on shoot")]
    [Tooltip("Если есть компонент JellyEffect на basketRoot/детях — дёрнем его. Если нет — будет punch scale.")]
    [SerializeField] private bool playJellyOnShoot = true;

    [SerializeField] private float jellyPunchScale = 0.10f;   // 0.10 = +10%
    [SerializeField] private float jellyPunchTime = 0.18f;

    [Header("Auto cleanup")]
    [SerializeField] private bool autoDestroy = true;
    [SerializeField] private float destroyAfterSeconds = 3f;

    private readonly List<GameObject> _spawned = new();
    private Tween _tween;
    private Camera _cam;

    private void Awake()
    {
        if (basketRoot == null) basketRoot = transform;
        if (spawnInsidePoint == null) spawnInsidePoint = basketRoot;

        _cam = Camera.main;
    }

    private void OnDisable()
    {
        _tween?.Kill();
        transform.DOKill();
        if (basketRoot != null) basketRoot.DOKill();
    }

    // ---------------- Public API ----------------

    public void AddGrowLevelBoost(BoostSpawnSource source)
    {
        if (growWholeLevelBoostPrefab == null) return;
        SpawnInside(growWholeLevelBoostPrefab, source);
    }

    public void AddExtraTimeBoost(BoostSpawnSource source)
    {
        if (extraLevelTimeBoostPrefab == null) return;
        SpawnInside(extraLevelTimeBoostPrefab, source);
    }

    public void AddRandomBoosts(int count, BoostSpawnSource source)
    {
        count = Mathf.Clamp(count, 1, 10);

        for (int i = 0; i < count; i++)
        {
            var prefab = (Random.value < 0.5f) ? growWholeLevelBoostPrefab : extraLevelTimeBoostPrefab;
            if (prefab == null) continue;
            SpawnInside(prefab, source);
        }
    }

    /// <summary>
    /// Корзина летит flyFrom -> flyTo, затем shake -> delay -> reveal -> (jelly) -> shoot вверх.
    /// </summary>
    public void FlyInAndShoot(Transform flyFrom, Transform flyTo, Vector3 forwardAxisWorld)
    {
        if (_spawned.Count == 0)
        {
            Cleanup();
            return;
        }

        if (flyFrom == null || flyTo == null)
        {
            RevealAllBoosts();
            PlayJelly();
            ShootAll(forwardAxisWorld);
            Cleanup();
            return;
        }

        _tween?.Kill();
        transform.DOKill();
        if (basketRoot != null) basketRoot.DOKill();

        // ставим ВСЮ корзину в точку старта
        transform.position = flyFrom.position;
        transform.rotation = flyFrom.rotation;

        Sequence seq = DOTween.Sequence();

        // полёт
        seq.Append(transform.DOMove(flyTo.position, flyInTime).SetEase(flyInEase));
        seq.Join(transform.DORotateQuaternion(flyTo.rotation, flyInTime).SetEase(flyInEase));

        // если хочешь billboard во время полёта
        if (faceCameraDuringFly)
        {
            seq.Join(DOTween.To(
                () => 0f,
                _ => FaceModelToCamera(),
                1f,
                flyInTime
            ));
        }

        // приземление (shake) на визуале
        if (basketRoot != null)
        {
            seq.Append(basketRoot.DOShakePosition(shakeTime, shakeStrength, shakeVibrato, 90f, false, true));
        }
        else
        {
            seq.AppendInterval(shakeTime);
        }

        // по прилёту поворачиваем МОДЕЛЬ к камере
        if (faceCameraOnArrive)
            seq.AppendCallback(FaceModelToCamera);

        if (delayAfterArrive > 0f)
            seq.AppendInterval(delayAfterArrive);

        // показать бусты перед выстрелом
        seq.AppendCallback(RevealAllBoosts);

        if (revealBeforeShootSeconds > 0f)
            seq.AppendInterval(revealBeforeShootSeconds);

        // желе прямо перед выстрелом
        seq.AppendCallback(PlayJelly);

        // выстрел
        seq.OnComplete(() =>
        {
            ShootAll(forwardAxisWorld);
            Cleanup();
        });

        _tween = seq;
    }

    // ---------------- Internal ----------------

    private void FaceModelToCamera()
    {
        if (!faceCameraOnArrive) return;
        if (basketRoot == null) return;

        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        // смотрим на камеру только по Y (без завала вверх-вниз)
        Vector3 camPos = _cam.transform.position;
        Vector3 p = basketRoot.position;
        Vector3 dir = camPos - p;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion look = Quaternion.LookRotation(dir.normalized, Vector3.up);
        // корректировка, если модель изначально “задом”
        look *= Quaternion.Euler(0f, faceCameraYawOffset, 0f);

        basketRoot.rotation = look;
    }

    private void SpawnInside(GameObject prefab, BoostSpawnSource source)
    {
        if (spawnInsidePoint == null) return;

        Vector3 localOffset = new Vector3(
            Random.Range(-insideScatterRadius, insideScatterRadius),
            insideUpOffset,
            Random.Range(-insideScatterRadius, insideScatterRadius)
        );

        var go = Instantiate(prefab, spawnInsidePoint);
        go.transform.localPosition = localOffset;
        go.transform.localRotation = Random.rotation;

        if (go.TryGetComponent<BoostPickupItem>(out var boost))
            boost.SetSource(source);

        if (go.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // скрываем до reveal
        SetVisible(go, false);
        SetColliders(go, false);

        _spawned.Add(go);
    }

    private void RevealAllBoosts()
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            var go = _spawned[i];
            if (go == null) continue;
            SetVisible(go, true);
        }
    }

    private void PlayJelly()
    {
        if (!playJellyOnShoot || basketRoot == null) return;

        // 1) если у тебя есть свой JellyEffect (ты упоминал такой в других проектах)
        // попробуем найти и дернуть, если у него есть метод Play/Trigger.
        var jelly = basketRoot.GetComponentInChildren<MonoBehaviour>(true);
        // Мы не знаем твоего конкретного API JellyEffect, поэтому делаем безопасный punch-scale.
        // Если хочешь точечно под твой JellyEffect — скинешь класс, и я подключу “правильный” вызов.

        basketRoot.DOKill();
        float punch = Mathf.Max(0.01f, jellyPunchScale);
        basketRoot.DOPunchScale(Vector3.one * punch, jellyPunchTime, vibrato: 8, elasticity: 0.8f);
    }

    private void ShootAll(Vector3 forwardAxisWorld)
    {
        // направление "вперёд" для небольшого разброса (можно 0)
        Vector3 fwd = forwardAxisWorld.sqrMagnitude > 0.0001f ? forwardAxisWorld.normalized : transform.forward;
        Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

        for (int i = 0; i < _spawned.Count; i++)
        {
            var go = _spawned[i];
            if (go == null) continue;

            float delay = i * Mathf.Max(0f, perBoostShootStagger);

            DOVirtual.DelayedCall(delay, () =>
            {
                if (go == null) return;

                // включаем коллайдеры перед выстрелом
                SetColliders(go, true);

                go.transform.SetParent(null, true);

                if (go.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;

                    Vector3 side = right * Random.Range(-shootSideImpulse, shootSideImpulse);
                    Vector3 up = Vector3.up * shootUpImpulse;
                    Vector3 forward = fwd * shootForwardImpulse;

                    // ✅ основной импульс вверх
                    rb.AddForce(up + forward + side, ForceMode.Impulse);
                    rb.AddTorque(Random.insideUnitSphere * randomTorque, ForceMode.Impulse);
                }
            });
        }

        _spawned.Clear();
    }

    private void SetVisible(GameObject go, bool visible)
    {
        var rens = go.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rens.Length; i++)
            rens[i].enabled = visible;

        var srs = go.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < srs.Length; i++)
            srs[i].enabled = visible;

        var cgs = go.GetComponentsInChildren<CanvasGroup>(true);
        for (int i = 0; i < cgs.Length; i++)
            cgs[i].alpha = visible ? 1f : 0f;
    }

    private void SetColliders(GameObject go, bool enabled)
    {
        var cols = go.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            cols[i].enabled = enabled;
    }

    private void Cleanup()
    {
        if (!autoDestroy) return;
        Destroy(gameObject, Mathf.Max(0.1f, destroyAfterSeconds));
    }
}