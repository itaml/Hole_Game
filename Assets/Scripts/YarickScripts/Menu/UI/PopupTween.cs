using System;
using DG.Tweening;
using UnityEngine;

namespace Menu.UI
{
    public sealed class PopupTween : MonoBehaviour
    {
        [SerializeField] private CanvasGroup overlay;   // затемнение
        [SerializeField] private CanvasGroup panelCg;   // можно null
        [SerializeField] private RectTransform panel;

        [Header("Show")]
        [SerializeField] private float overlayFade = 0.15f;
        [SerializeField] private float panelFade = 0.12f;
        [SerializeField] private float panelTime = 0.20f;
        [SerializeField] private float panelStartScaleMul = 0.85f;
        [SerializeField] private float panelFromY = -40f;

        [Header("Hide")]
        [SerializeField] private float hideTime = 0.12f;

        private Vector2 _basePos;
        private Vector3 _baseScale;

        private void Awake()
        {
            if (panel != null)
            {
                _basePos = panel.anchoredPosition;
                _baseScale = panel.localScale;
            }
        }

        public void PlayShow()
        {
            DOTween.Kill(this);

            if (overlay != null) overlay.alpha = 0f;
            if (panelCg != null) panelCg.alpha = 0f;

            if (panel != null)
            {
                panel.localScale = _baseScale * panelStartScaleMul;
                panel.anchoredPosition = _basePos + new Vector2(0, panelFromY);
            }

            var s = DOTween.Sequence().SetUpdate(true).SetId(this);

            if (overlay != null)
                s.Join(overlay.DOFade(1f, overlayFade).SetEase(Ease.OutQuad));

            if (panelCg != null)
                s.Join(panelCg.DOFade(1f, panelFade).SetEase(Ease.OutQuad));

            if (panel != null)
            {
                s.Join(panel.DOScale(_baseScale, panelTime).SetEase(Ease.OutBack));
                s.Join(panel.DOAnchorPos(_basePos, panelTime).SetEase(Ease.OutCubic));
            }
        }

        public void PlayHide(Action onComplete)
        {
            DOTween.Kill(this);

            var s = DOTween.Sequence().SetUpdate(true).SetId(this);

            if (overlay != null) s.Join(overlay.DOFade(0f, hideTime).SetEase(Ease.InQuad));
            if (panelCg != null) s.Join(panelCg.DOFade(0f, hideTime).SetEase(Ease.InQuad));

            if (panel != null)
            {
                s.Join(panel.DOScale(_baseScale * 0.95f, hideTime).SetEase(Ease.InQuad));
                s.Join(panel.DOAnchorPos(_basePos + new Vector2(0, panelFromY * 0.5f), hideTime).SetEase(Ease.InQuad));
            }

            s.OnComplete(() => onComplete?.Invoke());
        }
    }
}