using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PopupTweenAnimator : MonoBehaviour
{
    public enum SlideFrom { None, Top, Bottom, Left, Right }

    [Header("Refs")]
    [SerializeField] private GameObject root;            // что включаем/выключаем
    [SerializeField] private RectTransform panel;        // что двигаем/скейлим
    [SerializeField] private CanvasGroup canvasGroup;    // для fade (optional)
    [SerializeField] private Button closeButton;         // optional
    [SerializeField] private Button backdropButton;      // optional (клик по фону закрывает)

    [Header("Show")]
    [SerializeField] private float showDuration = 0.25f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private float showStartScale = 0.85f;

    [Header("Hide")]
    [SerializeField] private float hideDuration = 0.18f;
    [SerializeField] private Ease hideEase = Ease.InBack;
    [SerializeField] private float hideEndScale = 0.85f;

    [Header("Fade")]
    [SerializeField] private bool useFade = true;
    [SerializeField] private float showStartAlpha = 0f;
    [SerializeField] private float showEndAlpha = 1f;

    [Header("Slide")]
    [SerializeField] private SlideFrom slideFrom = SlideFrom.None;
    [SerializeField] private float slideDistance = 120f;

    [Header("Behaviour")]
    [Tooltip("Анимации будут работать даже если Time.timeScale = 0 (на паузе/lose экране).")]
    [SerializeField] private bool useUnscaledTime = true;

    [Tooltip("Блокировать клики на время анимации.")]
    [SerializeField] private bool blockRaycastsDuringTween = true;

    public bool IsVisible { get; private set; }

    private Tween _tween;
    private Vector2 _panelAnchoredDefault;
    private bool _hasDefaultPos;

    private void Awake()
    {
        if (root == null) root = gameObject;
        if (panel == null) panel = root.GetComponent<RectTransform>();
        if (canvasGroup == null && useFade)
            canvasGroup = root.GetComponent<CanvasGroup>();

        if (canvasGroup == null && useFade)
            canvasGroup = root.AddComponent<CanvasGroup>();

        if (panel != null)
        {
            _panelAnchoredDefault = panel.anchoredPosition;
            _hasDefaultPos = true;
        }
    }

    void Start()
{
    if (root.activeSelf)
        Show();
}

    private void OnDestroy()
    {
        KillTween();
    }

    public void Show(Action onShown = null)
    {
        if (root == null) return;

        root.SetActive(true);
        IsVisible = true;

        KillTween();

        // стартовые значения
        if (panel != null)
        {
            panel.localScale = Vector3.one * Mathf.Max(0.01f, showStartScale);

            if (_hasDefaultPos)
                panel.anchoredPosition = _panelAnchoredDefault + GetSlideOffset();
        }

        if (useFade && canvasGroup != null)
        {
            canvasGroup.alpha = showStartAlpha;
            if (blockRaycastsDuringTween)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        // анимация
        var seq = DOTween.Sequence();
        if (useUnscaledTime) seq.SetUpdate(true);

        if (useFade && canvasGroup != null)
            seq.Join(canvasGroup.DOFade(showEndAlpha, showDuration).SetEase(Ease.Linear));

        if (panel != null)
        {
            seq.Join(panel.DOScale(1f, showDuration).SetEase(showEase));

            if (_hasDefaultPos && slideFrom != SlideFrom.None)
                seq.Join(panel.DOAnchorPos(_panelAnchoredDefault, showDuration).SetEase(Ease.OutCubic));
        }

        seq.OnComplete(() =>
        {
            if (useFade && canvasGroup != null && blockRaycastsDuringTween)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            onShown?.Invoke();
        });

        _tween = seq;
    }

    public void Hide(Action onHidden = null)
    {
        if (root == null) return;
        if (!root.activeSelf) { onHidden?.Invoke(); return; }

        KillTween();

        if (useFade && canvasGroup != null && blockRaycastsDuringTween)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        var seq = DOTween.Sequence();
        if (useUnscaledTime) seq.SetUpdate(true);

        if (useFade && canvasGroup != null)
            seq.Join(canvasGroup.DOFade(showStartAlpha, hideDuration).SetEase(Ease.Linear));

        if (panel != null)
        {
            seq.Join(panel.DOScale(hideEndScale, hideDuration).SetEase(hideEase));

            if (_hasDefaultPos && slideFrom != SlideFrom.None)
                seq.Join(panel.DOAnchorPos(_panelAnchoredDefault + GetSlideOffset(), hideDuration).SetEase(Ease.InCubic));
        }

        seq.OnComplete(() =>
        {
            root.SetActive(false);
            IsVisible = false;

            // вернуть дефолт
            if (panel != null)
            {
                panel.localScale = Vector3.one;
                if (_hasDefaultPos) panel.anchoredPosition = _panelAnchoredDefault;
            }

            if (useFade && canvasGroup != null)
            {
                canvasGroup.alpha = showEndAlpha;
                if (blockRaycastsDuringTween)
                {
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                }
            }

            onHidden?.Invoke();
        });

        _tween = seq;
    }

    private Vector2 GetSlideOffset()
    {
        float d = Mathf.Abs(slideDistance);
        return slideFrom switch
        {
            SlideFrom.Top => new Vector2(0f, d),
            SlideFrom.Bottom => new Vector2(0f, -d),
            SlideFrom.Left => new Vector2(-d, 0f),
            SlideFrom.Right => new Vector2(d, 0f),
            _ => Vector2.zero
        };
    }

    private void KillTween()
    {
        if (_tween != null && _tween.IsActive())
            _tween.Kill();
        _tween = null;
    }

    void OnEnable()
    {
                Show();
    }
}