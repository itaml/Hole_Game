using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PreRunGoalSlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countText;

public void Set(Sprite sprite, int required)
{
    ApplySpriteProperSize(sprite);

    if (countText)
        countText.text = required.ToString();
}
private void ApplySpriteProperSize(Sprite sprite)
{
    if (!icon) return;

    icon.sprite = sprite;

    if (sprite == null) return;

    icon.preserveAspect = true;

    icon.SetNativeSize();

    float maxSize = 80f; // тот же размер для консистентности

    RectTransform rt = icon.rectTransform;
    float width = rt.sizeDelta.x;
    float height = rt.sizeDelta.y;

    float scale = Mathf.Min(maxSize / width, maxSize / height, 1f);
    rt.sizeDelta = new Vector2(width * scale, height * scale);
}

}