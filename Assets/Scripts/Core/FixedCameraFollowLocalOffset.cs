using UnityEngine;

public class FixedCameraFollowLocalOffset : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Fixed Rotation")]
    [SerializeField] private Vector3 fixedEuler = new Vector3(55f, 0f, 0f);

    [Header("Offset (local to camera rotation)")]
    [Tooltip("Смещение в локальных осях камеры: X=вправо, Y=вверх, Z=вперёд (обычно отрицательное, чтобы быть сзади).")]
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 12f, -8f);

    [Header("Smoothing")]
    [SerializeField] private float followLerp = 12f;

    private void LateUpdate()
    {
        if (!target) return;

        // 1) всегда фиксируем rotation
        var rot = Quaternion.Euler(fixedEuler);
        transform.rotation = rot;

        // 2) считаем позицию: target + (rot * localOffset)
        Vector3 desired = target.position + rot * localOffset;

        transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * followLerp);
    }
}   