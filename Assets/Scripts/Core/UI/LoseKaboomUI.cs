using System;
using UnityEngine;
using UnityEngine.UI;

public class LoseKaboomUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Art (optional)")]
    [SerializeField] private Image artImage;
    [SerializeField] private Sprite kaboomSprite;

    [Header("Buttons")]
    [SerializeField] private Button kaboomButton; // кнопка revive за 900
    [SerializeField] private Button retryButton;  // если есть, можно убрать

    private Action _onKaboomRevive;
    private Action _onRetry;

    private void Awake()
    {
        if (kaboomButton)
        {
            kaboomButton.onClick.RemoveAllListeners();
            kaboomButton.onClick.AddListener(OnKaboomClicked);
        }

        if (retryButton)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnRetryClicked);
        }
    }

    public void Show(Action onKaboomRevive, Action onRetry)
    {
        _onKaboomRevive = onKaboomRevive;
        _onRetry = onRetry;

        if (root) root.SetActive(true);

        if (artImage && kaboomSprite)
            artImage.sprite = kaboomSprite;

        if (kaboomButton)
            kaboomButton.interactable = (_onKaboomRevive != null);

        if (retryButton)
            retryButton.interactable = (_onRetry != null);
    }

    public void Hide()
    {
        if (root) root.SetActive(false);
        _onKaboomRevive = null;
        _onRetry = null;
    }

    private void OnKaboomClicked()
    {
        _onKaboomRevive?.Invoke();
    }

    private void OnRetryClicked()
    {
        _onRetry?.Invoke();
    }
}