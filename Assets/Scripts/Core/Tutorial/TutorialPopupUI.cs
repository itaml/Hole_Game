using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialPopupUI : MonoBehaviour
{
    [Header("ROOT (visual popup)")]
    [SerializeField] private GameObject popupRoot; // TutorialPopup (Image + CanvasGroup)
    [Header("Background")]
[SerializeField] private Image backgroundImage;

    [Header("Containers (children of popupRoot)")]
    [SerializeField] private GameObject level1Container; // DownDialog
    [SerializeField] private GameObject level2Container; // Lvl2
    [SerializeField] private GameObject boostContainer;  // ONE panel for 4 / 8 / 10

    [Header("Texts")]
    [SerializeField] private TMP_Text level1Text;
    [SerializeField] private TMP_Text level2Text;
    [SerializeField] private TMP_Text boostText;

    [Header("Buttons")]
    [SerializeField] private Button level2CloseButton;   // optional
    [SerializeField] private Button boostCloseButton;    // GOT IT button (4/8/10)

    [Header("Typing")]
    [SerializeField] private float charsPerSecond = 45f;

    private Coroutine typingRoutine;

    private void Awake()
    {
        // при старте ВСЁ скрыто
        ForceHideAll();
    }

    // ===================== SHOW =====================

    public void ShowLevel1(string message)
    {
            if (backgroundImage)
        backgroundImage.enabled = false; // <<< ВАЖНО
        EnsureRootVisible();
        HidePanelsOnly();
        if (level1Container) level1Container.SetActive(true);
        StartTyping(level1Text, message);
    }

    public void ShowLevel2(string message, Action onClose)
    {
        EnsureRootVisible();
        HidePanelsOnly();
        if (level2Container) level2Container.SetActive(true);
        StartTyping(level2Text, message);

        if (level2CloseButton)
        {
            level2CloseButton.onClick.RemoveAllListeners();
            level2CloseButton.onClick.AddListener(() => onClose?.Invoke());
        }
    }

    public void ShowBoost(string message, Action onClose)
    {
        EnsureRootVisible();
        HidePanelsOnly();
        if (boostContainer) boostContainer.SetActive(true);
        StartTyping(boostText, message);

        if (boostCloseButton)
        {
            boostCloseButton.onClick.RemoveAllListeners();
            boostCloseButton.onClick.AddListener(() => onClose?.Invoke());
        }
    }

    // ===================== CLOSE =====================

    // 🔴 ЭТОТ МЕТОД ВЕШАЕШЬ НА КНОПКУ
    public void Close()
    {
        ForceHideAll();
    }

    // ===================== INTERNAL =====================

    private void EnsureRootVisible()
    {
        if (popupRoot && !popupRoot.activeSelf)
            popupRoot.SetActive(true);
    }

    private void HidePanelsOnly()
    {
        StopTyping();

        if (level1Container) level1Container.SetActive(false);
        if (level2Container) level2Container.SetActive(false);
        if (boostContainer) boostContainer.SetActive(false);
    }

    private void ForceHideAll()
    {
        StopTyping();

        if (level1Container) level1Container.SetActive(false);
        if (level2Container) level2Container.SetActive(false);
        if (boostContainer) boostContainer.SetActive(false);

        if (popupRoot) popupRoot.SetActive(false);
    }

    // ===================== TYPING =====================

    private void StartTyping(TMP_Text field, string message)
    {
        StopTyping();
        if (!field) return;
        typingRoutine = StartCoroutine(TypeRoutine(field, message));
    }

    private void StopTyping()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }
    }

    private IEnumerator TypeRoutine(TMP_Text field, string message)
    {
        field.text = "";

        float delay = charsPerSecond <= 0f ? 0f : 1f / charsPerSecond;

        for (int i = 0; i < message.Length; i++)
        {
            field.text += message[i];

            float t = 0f;
            while (t < delay)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }
}