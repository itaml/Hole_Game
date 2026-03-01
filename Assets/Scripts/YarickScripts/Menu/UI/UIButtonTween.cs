using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Menu.UI
{
    public sealed class UIButtonTween : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private float downScale = 0.95f;        // Чуть больше (было 0.92)
        [SerializeField] private float downTime = 0.1f;          // Немедленный отклик
        [SerializeField] private float upTime = 0.15f;           // Чуть длиннее возврат
        [SerializeField] private float springStrength = 0.03f;   // Легкая пружинка

        private Vector3 _baseScale;
        private Tween _t;

        private void Awake()
        {
            if (target == null) target = transform as RectTransform;
            _baseScale = target.localScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _t?.Kill();
            _t = target.DOScale(_baseScale * downScale, downTime)
                .SetEase(Ease.OutQuad);  // Плавное начало, мягкий конец
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _t?.Kill();

            // Последовательность: возврат к нормальному размеру + легкий пружинный эффект
            Sequence seq = DOTween.Sequence();
            seq.Append(target.DOScale(_baseScale, upTime).SetEase(Ease.OutBack, 0.5f));
            // Ease.OutBack дает легкий перелет с возвратом - очень приятно

            _t = seq;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_t != null && _t.IsActive() && _t.IsPlaying()) return;

            _t?.Kill();
            _t = target.DOScale(_baseScale, upTime).SetEase(Ease.OutCubic);
        }
    }
}