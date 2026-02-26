using UnityEngine;

public class HoleDirectionArrow : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform hole;              // дыра
    [SerializeField] private FloatingJoystick joystick;   // ввод
    [SerializeField] private Transform arrow;             // объект стрелки (спрайт)

    [Header("Radius (auto)")]
    [Tooltip("Если включено, радиус берётся от текущего размера дыры по XZ.")]
    [SerializeField] private bool autoRadiusFromHoleScale = true;

    [Tooltip("Базовый радиус при holeScaleXZ = 1.")]
    [SerializeField] private float baseRadius = 1.2f;

    [Tooltip("Доп. отступ от края дыры (в мировых единицах).")]
    [SerializeField] private float extraPadding = 0.15f;

    [Header("Placement")]
    [Tooltip("Высота стрелки над полом.")]
    [SerializeField] private float yOffset = 0.02f;

    [Header("Behavior")]
    [Tooltip("Порог для ПОКАЗА (выше).")]
    [SerializeField] private float showThreshold = 0.18f;

    [Tooltip("Порог для СКРЫТИЯ (ниже). Должен быть меньше showThreshold.")]
    [SerializeField] private float hideThreshold = 0.12f;

    [SerializeField] private float posLerp = 18f;
    [SerializeField] private float rotLerp = 18f;

    [Tooltip("Если стрелка нарисована не остриём вверх/вперёд, подстрой смещение угла.")]
    [SerializeField] private float angleOffsetDeg = 0f;

    [Header("Orientation")]
    [Tooltip("Если включено, стрелка всегда 'лежит' на земле (rotation X=90).")]
    [SerializeField] private bool layOnGround = true;

    [Header("Debug")]
    [SerializeField] private bool debugDraw = false;

    private bool _visible;

    private void Awake()
    {
        // защита от случайно одинаковых порогов
        if (hideThreshold >= showThreshold)
            hideThreshold = Mathf.Max(0f, showThreshold - 0.05f);

        if (arrow != null)
            arrow.gameObject.SetActive(false);

        _visible = false;
    }

    private void Update()
    {
        if (!hole || !joystick || !arrow) return;

        Vector2 v = new Vector2(joystick.Horizontal, joystick.Vertical);
        float mag = v.magnitude;

        // ---- анти-мерцание: гистерезис ----
        if (!_visible)
        {
            if (mag >= showThreshold)
                SetVisible(true);
            else
                return;
        }
        else
        {
            if (mag <= hideThreshold)
            {
                SetVisible(false);
                return;
            }
        }

        // направление
        Vector3 dir = new Vector3(v.x, 0f, v.y);
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        float r = ComputeRadius();

        // позиция
        Vector3 desiredPos = hole.position + dir * r;
        desiredPos.y = hole.position.y + yOffset;

        arrow.position = Vector3.Lerp(arrow.position, desiredPos, Time.deltaTime * posLerp);

        // поворот
        float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg + angleOffsetDeg;

        Quaternion desiredRot = layOnGround
            ? Quaternion.Euler(90f, yaw, 0f)   // лежит на земле
            : Quaternion.Euler(0f, yaw, 0f);   // стоит вертикально

        arrow.rotation = Quaternion.Slerp(arrow.rotation, desiredRot, Time.deltaTime * rotLerp);

        if (debugDraw)
        {
            Debug.DrawLine(hole.position + Vector3.up * 0.02f, desiredPos, Color.yellow);
        }
    }

    private void SetVisible(bool visible)
    {
        _visible = visible;
        if (arrow.gameObject.activeSelf != visible)
            arrow.gameObject.SetActive(visible);
    }

    private float ComputeRadius()
    {
        if (!autoRadiusFromHoleScale) return baseRadius;

        // hole растёт по XZ (как ты и хотел), Y игнорируем
        Vector3 s = hole.lossyScale;
        float holeScaleXZ = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z));

        // радиус стрелки = (базовый * scale) + небольшой отступ, чтобы не залезала под дыру
        return baseRadius * holeScaleXZ + extraPadding;
    }
}