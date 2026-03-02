using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PreRunGoalSlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countText;

    public void Set(Sprite sprite, int required)
    {
        if (icon) icon.sprite = sprite;
        if (countText) countText.text = required.ToString();
    }
}