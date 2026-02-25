using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FlyToUiIconSpawner : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private Canvas canvas;                 // твой HUD canvas
    [SerializeField] private RectTransform root;            // контейнер для летающих иконок
    [SerializeField] private Image iconPrefab;              // prefab Image

    [Header("Camera")]
    [SerializeField] private Camera worldCamera;            // обычно MainCamera

    [Header("Anim")]
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float startScale = 1.0f;
    [SerializeField] private float endScale = 0.4f;

    private void Reset()
    {
        worldCamera = Camera.main;
    }

    public void Spawn(Vector3 worldPos, Sprite sprite, RectTransform target)
    {
        if (sprite == null || target == null || iconPrefab == null || canvas == null || root == null) return;
        StartCoroutine(FlyRoutine(worldPos, sprite, target));
    }

    private IEnumerator FlyRoutine(Vector3 worldPos, Sprite sprite, RectTransform target)
    {
        var img = Instantiate(iconPrefab, root);
        img.sprite = sprite;
        var rt = img.rectTransform;

        // старт: world -> screen
        Vector3 screenPos = (worldCamera != null)
            ? worldCamera.WorldToScreenPoint(worldPos)
            : RectTransformUtility.WorldToScreenPoint(null, worldPos);

        // screen -> local point в root
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            root, screenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : worldCamera,
            out Vector2 startLocal
        );

        rt.anchoredPosition = startLocal;
        rt.localScale = Vector3.one * startScale;

        // цель: target world -> screen -> local (в root)
        Vector3 targetScreen = RectTransformUtility.WorldToScreenPoint(
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : worldCamera,
            target.position
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            root, targetScreen,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : worldCamera,
            out Vector2 endLocal
        );

        float t = 0f;
        Vector2 a = startLocal;
        Vector2 b = endLocal;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = ease.Evaluate(Mathf.Clamp01(t / duration));

            rt.anchoredPosition = Vector2.Lerp(a, b, k);
            rt.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, k);

            yield return null;
        }

        Destroy(img.gameObject);
    }
}