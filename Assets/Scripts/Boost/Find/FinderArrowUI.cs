using UnityEngine;

public class FinderArrowUI : MonoBehaviour
{
    [Header("Edge mode")]
    [SerializeField] private float edgePadding = 90f;

    [Header("Near target mode")]
    [Tooltip("Если цель на экране или почти на экране, стрелка будет рядом с целью.")]
    [SerializeField] private bool stickToTargetWhenVisible = true;

    [Tooltip("На каком расстоянии от цели (в пикселях) размещать стрелку, когда цель видима.")]
    [SerializeField] private float nearOffset = 55f;

    [Tooltip("Если цель уже почти на экране, можно включить 'прилипание' (в пикселях).")]
    [SerializeField] private float nearScreenMargin = 120f;

    [Header("Smoothing")]
    [SerializeField] private float rotateSmooth = 18f;

    private Transform _target;
    private Camera _cam;
    private RectTransform _rt;

    public void Init(Transform target, Camera cam)
    {
        _target = target;
        _cam = cam;
        _rt = (RectTransform)transform;
    }

    private void Awake()
    {
        _rt = (RectTransform)transform;
        if (_cam == null) _cam = Camera.main;
    }

    private void Update()
    {
        if (_target == null)
        {
            Destroy(gameObject);
            return;
        }

        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        Vector3 sp3 = _cam.WorldToScreenPoint(_target.position);

        // если цель за камерой, инвертим координаты
        if (sp3.z < 0.01f)
        {
            sp3.x = Screen.width - sp3.x;
            sp3.y = Screen.height - sp3.y;
            sp3.z = 1f;
        }

        Vector2 sp = new Vector2(sp3.x, sp3.y);

        // проверяем "почти на экране" (с запасом)
        bool nearScreen =
            sp.x >= -nearScreenMargin && sp.x <= Screen.width + nearScreenMargin &&
            sp.y >= -nearScreenMargin && sp.y <= Screen.height + nearScreenMargin;

        // полностью на экране
        bool onScreen =
            sp.x >= 0f && sp.x <= Screen.width &&
            sp.y >= 0f && sp.y <= Screen.height;

        if (stickToTargetWhenVisible && nearScreen)
        {
            // ---- РЕЖИМ "РЯДОМ С ЦЕЛЬЮ" ----
            // ставим стрелку рядом с целью, а не на краю
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            // направление "куда смотреть" (от центра экрана к цели)
            Vector2 dirFromCenter = (sp - center);
            if (dirFromCenter.sqrMagnitude < 0.001f) dirFromCenter = Vector2.right;
            dirFromCenter.Normalize();

            // позиция стрелки: чуть сдвинута от цели в сторону центра (чтобы не перекрывать цель)
            Vector2 arrowPos = sp - dirFromCenter * nearOffset;

            // чтобы стрелка не уехала за экран
            arrowPos.x = Mathf.Clamp(arrowPos.x, edgePadding, Screen.width - edgePadding);
            arrowPos.y = Mathf.Clamp(arrowPos.y, edgePadding, Screen.height - edgePadding);

            _rt.position = new Vector3(arrowPos.x, arrowPos.y, 0f);

            // вращаем стрелку так, чтобы она указывала ИЗ стрелки В цель
            Vector2 dirToTarget = (sp - arrowPos);
            if (dirToTarget.sqrMagnitude < 0.001f) dirToTarget = dirFromCenter;
            dirToTarget.Normalize();

            float angle = Mathf.Atan2(dirToTarget.y, dirToTarget.x) * Mathf.Rad2Deg;
            SmoothRotate(angle);

            // если цель полностью на экране, можно (по желанию) скрывать стрелку:
            // if (onScreen) gameObject.SetActive(false);
            // но тебе нужно "показывала на него" -> оставляем видимой.
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            return;
        }

        // ---- РЕЖИМ "НА КРАЮ ЭКРАНА" ----
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);

            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 dir = (sp - center);
            if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;
            dir.Normalize();

            float halfW = Screen.width * 0.5f - edgePadding;
            float halfH = Screen.height * 0.5f - edgePadding;

            float tX = Mathf.Abs(dir.x) < 0.0001f ? float.MaxValue : halfW / Mathf.Abs(dir.x);
            float tY = Mathf.Abs(dir.y) < 0.0001f ? float.MaxValue : halfH / Mathf.Abs(dir.y);
            float t = Mathf.Min(tX, tY);

            Vector2 pos = center + dir * t;
            _rt.position = new Vector3(pos.x, pos.y, 0f);

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            SmoothRotate(angle);
        }
    }

    private void SmoothRotate(float angle)
    {
        Quaternion targetRot = Quaternion.Euler(0f, 0f, angle);
        _rt.rotation = Quaternion.Slerp(_rt.rotation, targetRot, Time.unscaledDeltaTime * rotateSmooth);
    }
}