using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.UI
{
    public sealed class DualBattlepassWindowAnimator : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private RectTransform windowRoot;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;

        [Header("Animation")]
        [SerializeField] private float duration = 0.35f;
        [SerializeField] private float startYOffset = -800f;

        private Tween currentTween;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);
        }

        public void Show()
        {
            var ctrl = GetComponent<DualBattlepassWindowController>();
            if (ctrl != null)
                ctrl.RefreshAll();

            gameObject.SetActive(true);

            currentTween?.Kill();

            if (canvasGroup) canvasGroup.alpha = 0;

            if (windowRoot)
            {
                Vector2 startPos = windowRoot.anchoredPosition;
                startPos.y = startYOffset;
                windowRoot.anchoredPosition = startPos;
            }

            currentTween = DOTween.Sequence()
                .Append(canvasGroup != null ? canvasGroup.DOFade(1f, duration * 0.8f) : null)
                .Join(windowRoot != null ? windowRoot.DOAnchorPosY(0, duration).SetEase(Ease.OutBack) : null);
        }

        public void Hide()
        {
            currentTween?.Kill();

            currentTween = DOTween.Sequence()
                .Append(canvasGroup != null ? canvasGroup.DOFade(0f, duration * 0.6f) : null)
                .Join(windowRoot != null ? windowRoot.DOAnchorPosY(startYOffset, duration).SetEase(Ease.InBack) : null)
                .OnComplete(() => gameObject.SetActive(false));
        }
    }
}