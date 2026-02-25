using UnityEngine;
using UnityEngine.UI;

public class FloatingXpTextSpawner : MonoBehaviour
{
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private Text prefab;
    [SerializeField] private float floatUp = 1.2f;
    [SerializeField] private float lifeTime = 0.8f;

    public void Spawn(Vector3 worldPos, int xp)
    {
        if (!worldCanvas || !prefab) return;

        var t = Instantiate(prefab, worldCanvas.transform);
        t.text = $"+{xp}";

        // конверт world -> screen (для World Space canvas не нужно, но зависит от твоей настройки)
        Vector3 screen = Camera.main.WorldToScreenPoint(worldPos);
        t.transform.position = screen;

        StartCoroutine(Animate(t));
    }

    private System.Collections.IEnumerator Animate(Text t)
    {
        float time = 0f;
        Vector3 start = t.transform.position;
        Vector3 end = start + Vector3.up * floatUp * 100f;

        Color c = t.color;

        while (time < lifeTime)
        {
            time += Time.deltaTime;
            float k = time / lifeTime;

            t.transform.position = Vector3.Lerp(start, end, k);
            c.a = 1f - k;
            t.color = c;
            yield return null;
        }

        if (t) Destroy(t.gameObject);
    }
}