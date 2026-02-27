using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FireworkSpriteAnimUI : MonoBehaviour
{
    [SerializeField] private Image image;

    private void Awake()
    {
        EnsureVisible();
    }

    public IEnumerator Play(Sprite[] frames, float fps)
    {
        EnsureVisible();

        if (image == null)
        {
            Debug.LogError("[FireworkSpriteAnimUI] Image reference is NULL (assign it in prefab).");
            yield break;
        }

        if (frames == null || frames.Length == 0)
        {
            Debug.LogError("[FireworkSpriteAnimUI] Frames are empty.");
            yield break;
        }

        float frameTime = 1f / Mathf.Max(1f, fps);

        for (int i = 0; i < frames.Length; i++)
        {
            image.sprite = frames[i];
            yield return new WaitForSecondsRealtime(frameTime);
        }
    }

    private void EnsureVisible()
    {
        if (transform is RectTransform rt)
        {
            rt.localScale = Vector3.one;
            if (rt.sizeDelta == Vector2.zero)
                rt.sizeDelta = new Vector2(300, 300); // чтобы точно было видно
        }

        if (image != null)
        {
            image.enabled = true;
            var c = image.color;
            c.a = 1f;
            image.color = c;
        }
    }
}