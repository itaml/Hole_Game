using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.UI
{
    public sealed class BattlepassWindowAnimator : MonoBehaviour
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

            var ctrl = GetComponent<BattlepassWindowController>();
            if (ctrl != null)
                ctrl.RefreshAll();
            gameObject.SetActive(true);

            if (currentTween != null)
                currentTween.Kill();

            canvasGroup.alpha = 0;

            Vector2 startPos = windowRoot.anchoredPosition;
            startPos.y = startYOffset;
            windowRoot.anchoredPosition = startPos;

            currentTween = DOTween.Sequence()
                .Append(canvasGroup.DOFade(1f, duration * 0.8f))
                .Join(windowRoot.DOAnchorPosY(0, duration).SetEase(Ease.OutBack));
        }

        public void Hide()
        {
            if (currentTween != null)
                currentTween.Kill();

            currentTween = DOTween.Sequence()
                .Append(canvasGroup.DOFade(0f, duration * 0.6f))
                .Join(windowRoot.DOAnchorPosY(startYOffset, duration).SetEase(Ease.InBack))
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                });
        }
    }
}