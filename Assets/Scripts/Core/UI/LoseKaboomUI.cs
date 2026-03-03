using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoseKaboomUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Art")]
    [SerializeField] private Image artImage;
    [SerializeField] private Sprite kaboomArt;

    [Header("Kaboom Revive")]
    [SerializeField] private Button kaboomButton;


    public void Hide()
    {
        if (root) root.SetActive(false);
    }

    public void Show(
        bool canKaboomRevive,
        Action onKaboom,
        Action onRetry)
    {
        if (root) root.SetActive(true);

        if (artImage != null && kaboomArt != null)
            artImage.sprite = kaboomArt;

        SetupButton(kaboomButton, canKaboomRevive, onKaboom);
    }

    private void SetupButton(Button btn, bool interactable, Action action)
    {
        if (!btn) return;
        btn.interactable = interactable;
        btn.onClick.RemoveAllListeners();
        if (action != null) btn.onClick.AddListener(() => action.Invoke());
    }
}