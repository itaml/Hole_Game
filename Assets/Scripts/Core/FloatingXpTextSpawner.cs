using UnityEngine;
using TMPro;

public class FloatingXpTextSpawner : MonoBehaviour
{
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private RectTransform uiRoot;
    [SerializeField] private TextMeshProUGUI prefab;

    [Header("Anim")]
    [SerializeField] private float floatUpPx = 140f;
    [SerializeField] private float lifeTime = 0.8f;

    private Camera _cam;

    private void Awake()
    {
        _cam = Camera.main;
        if (uiCanvas != null && uiRoot == null)
            uiRoot = uiCanvas.transform as RectTransform;
    }

    public void Spawn(Vector3 worldPos, int xp)
    {
        if (!uiCanvas || !prefab || uiRoot == null) return;

        var t = Instantiate(prefab, uiRoot);
        t.text = $"+{xp}";

        Vector2 screen = RectTransformUtility.WorldToScreenPoint(_cam, worldPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            uiRoot,
            screen,
            uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _cam,
            out Vector2 localPos
        );

        var rt = t.rectTransform;
        rt.anchoredPosition = localPos;

        StartCoroutine(Animate(t));
    }

    private System.Collections.IEnumerator Animate(TextMeshProUGUI t)
    {
        float time = 0f;
        var rt = t.rectTransform;

        Vector2 start = rt.anchoredPosition;
        Vector2 end = start + Vector2.up * floatUpPx;

        Color c = t.color;
        float startAlpha = c.a;

        while (time < lifeTime)
        {
            time += Time.deltaTime;
            float k = Mathf.Clamp01(time / lifeTime);
            float ease = 1f - Mathf.Pow(1f - k, 3f);

            rt.anchoredPosition = Vector2.Lerp(start, end, ease);
            c.a = Mathf.Lerp(startAlpha, 0f, k);
            t.color = c;

            yield return null;
        }

        Destroy(t.gameObject);
    }
}