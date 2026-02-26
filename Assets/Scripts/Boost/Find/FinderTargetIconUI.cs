using UnityEngine;
using UnityEngine.UI;

public class FinderTargetIconUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private bool hideWhenBehindCamera = true;

    private Transform _target;
    private Camera _cam;
    private RectTransform _rt;
    private Canvas _canvas;
    private RectTransform _canvasRect;

    public void Init(Transform target, Sprite icon, Camera cam)
    {
        _target = target;
        _cam = cam;

        if (iconImage != null) iconImage.sprite = icon;

        _rt = (RectTransform)transform;
        _canvas = GetComponentInParent<Canvas>();
        _canvasRect = _canvas != null ? _canvas.GetComponent<RectTransform>() : null;
    }

    private void LateUpdate()
    {
        if (_target == null || _cam == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 wp = _target.position + worldOffset;
        Vector3 sp = _cam.WorldToScreenPoint(wp);

        if (hideWhenBehindCamera && sp.z < 0.01f)
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
            return;
        }
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        if (_canvas == null)
        {
            // fallback
            _rt.position = sp;
            return;
        }

        // ✅ Overlay: можно напрямую screen position
        if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            _rt.position = sp;
            return;
        }

        // ✅ ScreenSpaceCamera / WorldSpace: переводим в локальные координаты Canvas
        Camera uiCam = _canvas.worldCamera != null ? _canvas.worldCamera : _cam;
        if (_canvasRect != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, sp, uiCam, out var local))
        {
            _rt.anchoredPosition = local;
        }
        else
        {
            _rt.position = sp; // запасной вариант
        }
    }
}