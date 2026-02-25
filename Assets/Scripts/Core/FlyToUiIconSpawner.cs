using UnityEngine;
using UnityEngine.UI;

public class FlyToUiIconSpawner : MonoBehaviour
{
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private Image iconPrefab;
    [SerializeField] private float flyTime = 0.45f;

    public void Spawn(Vector3 worldPos, Sprite sprite, RectTransform target)
    {
        if (!uiCanvas || !iconPrefab || sprite == null || target == null) return;

        var img = Instantiate(iconPrefab, uiCanvas.transform);
        img.sprite = sprite;

        Vector3 start = Camera.main.WorldToScreenPoint(worldPos);
        img.rectTransform.position = start;

        StartCoroutine(Fly(img.rectTransform, target.position, img));
    }

    private System.Collections.IEnumerator Fly(RectTransform rt, Vector3 targetScreenPos, Image img)
    {
        float t = 0f;
        Vector3 start = rt.position;

        while (t < flyTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / flyTime);
            rt.position = Vector3.Lerp(start, targetScreenPos, k);
            yield return null;
        }

        if (img) Destroy(img.gameObject);
    }
}