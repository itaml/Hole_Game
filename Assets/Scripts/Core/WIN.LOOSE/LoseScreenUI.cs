using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoseScreenUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;

    [SerializeField] private Button reviveButton;
    [SerializeField] private TMP_Text reviveLabel;

    [SerializeField] private Button retryButton;

    public void Show(string title, string subtitle, bool canRevive, string reviveText,
        System.Action onRevive, System.Action onRetry)
    {
        if (root) root.SetActive(true);

        if (titleText) titleText.text = title;
        if (subtitleText) subtitleText.text = subtitle;

        if (reviveLabel) reviveLabel.text = reviveText;

        if (reviveButton)
        {
            reviveButton.interactable = canRevive;
            reviveButton.onClick.RemoveAllListeners();
            reviveButton.onClick.AddListener(() => onRevive?.Invoke());
        }

        if (retryButton)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(() => onRetry?.Invoke());
        }
    }

    public void Hide()
    {
        if (root) root.SetActive(false);
    }
}