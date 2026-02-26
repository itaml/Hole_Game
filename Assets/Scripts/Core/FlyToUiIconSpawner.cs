using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FlyToUiIconSpawner : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private Canvas canvas;          // HUD canvas
    [SerializeField] private RectTransform root;     // FlyLayer (full-screen)
    [SerializeField] private Image iconPrefab;       // prefab Image

    [Header("Camera")]
    [SerializeField] private Camera worldCamera;     // MainCamera (для ScreenSpace-Camera/World)

    [Header("Anim")]
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float startScale = 1f;
    [SerializeField] private float endScale = 0.4f;

    [Header("Icon Size")]
    [SerializeField] private bool useNativeSize = true;
    [Range(0.2f, 2f)]
    [SerializeField] private float nativeScale = 1f;

    private void Reset()
    {
        canvas = GetComponentInParent<Canvas>();
        worldCamera = Camera.main;
    }

public void Spawn(Vector3 worldPos, Sprite sprite, RectTransform target, System.Action onArrived = null)
    {
        if (sprite == null || target == null) return;
        if (canvas == null || root == null || iconPrefab == null)
        {
            Debug.LogWarning("[FlyToUiIconSpawner] Missing refs (canvas/root/iconPrefab).");
            return;
        }

    StartCoroutine(FlyRoutine(worldPos, sprite, target, onArrived));
    }

private IEnumerator FlyRoutine(Vector3 worldPos, Sprite sprite, RectTransform target, System.Action onArrived)
{
    {
        var img = Instantiate(iconPrefab, root);
        img.sprite = sprite;

        if (useNativeSize)
        {
            img.SetNativeSize();
            img.rectTransform.sizeDelta *= nativeScale;
        }

        var rt = img.rectTransform;

        // 1) WORLD -> SCREEN
        Vector3 startScreen = GetScreenPointFromWorld(worldPos);

        // 2) SCREEN -> ROOT LOCAL
        Vector2 startLocal = ScreenToLocal(root, startScreen);

        // 3) TARGET WORLD(UI) -> SCREEN -> ROOT LOCAL
        Vector3 targetScreen = RectTransformUtility.WorldToScreenPoint(GetUiCamera(), target.position);
        Vector2 endLocal = ScreenToLocal(root, targetScreen);

        rt.anchoredPosition = startLocal;
        rt.localScale = Vector3.one * startScale;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = ease.Evaluate(Mathf.Clamp01(t / duration));

            rt.anchoredPosition = Vector2.Lerp(startLocal, endLocal, k);
            rt.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, k);

            yield return null;
        }
    onArrived?.Invoke();
    Destroy(img.gameObject);
    }
    }

    private Camera GetUiCamera()
    {
        // Для Overlay камера не нужна
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;

        // Для ScreenSpace-Camera/World Space нужна камера
        return canvas.worldCamera != null ? canvas.worldCamera : worldCamera;
    }

    private Vector3 GetScreenPointFromWorld(Vector3 worldPos)
    {
        // обычно worldPos в 3D мире -> MainCamera
        var cam = worldCamera != null ? worldCamera : Camera.main;
        if (cam == null) return new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        return cam.WorldToScreenPoint(worldPos);
    }

    private Vector2 ScreenToLocal(RectTransform rect, Vector3 screenPoint)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rect,
            screenPoint,
            GetUiCamera(),
            out Vector2 local
        );
        return local;
    }
}