using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoseScreenUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;

    [Header("Buttons")]
    [SerializeField] private Button freeReviveButton;
    [SerializeField] private Button paidReviveButton;
    [SerializeField] private Button kaboomReviveButton;
    [SerializeField] private Button retryButton;

    public void Show(
        string title,
        string subtitle,
        Action onFreeRevive,
        Action onPaidRevive,
        Action onKaboomRevive,
        Action onRetry)
    {
        if (root) root.SetActive(true);

        if (titleText) titleText.text = title;
        if (subtitleText) subtitleText.text = subtitle;

        SetupButton(freeReviveButton, onFreeRevive);
        SetupButton(paidReviveButton, onPaidRevive);
        SetupButton(kaboomReviveButton, onKaboomRevive);
        SetupButton(retryButton, onRetry);
    }

    public void Hide()
    {
        if (root) root.SetActive(false);
    }

    private void SetupButton(Button btn, Action action)
    {
        if (!btn) return;

        btn.onClick.RemoveAllListeners();

        if (action == null)
        {
            btn.interactable = false;
            return;
        }

        btn.interactable = true;
        btn.onClick.AddListener(() => action.Invoke());
    }
}