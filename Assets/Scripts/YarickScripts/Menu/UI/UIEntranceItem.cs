using DG.Tweening;
using UnityEngine;

namespace Menu.UI
{
    public sealed class UIEntranceItem : MonoBehaviour
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private CanvasGroup cg;

        [Header("Move")]
        [Tooltip("From offset in anchoredPosition (UI space). Example (-300,0) = come from left.")]
        [SerializeField] private Vector2 fromOffset = new Vector2(-300f, 0f);

        [SerializeField] private float delay = 0f;
        [SerializeField] private float duration = 0.45f;
        [SerializeField] private Ease ease = Ease.OutBack;

        [Header("Fade")]
        [SerializeField] private bool useFade = true;

        [Header("Extras")]
        [SerializeField] private bool punchOnArrive = false;
        [SerializeField] private float punchScale = 0.10f;

        private Vector2 _basePos;
        private Vector3 _baseScale;
        private bool _cached;

        private void Awake()
        {
            if (target == null) target = transform as RectTransform;
            Cache();
        }

        private void Cache()
        {
            if (_cached || target == null) return;
            _basePos = target.anchoredPosition;
            _baseScale = target.localScale;
            _cached = true;

            if (cg == null)
                cg = target.GetComponent<CanvasGroup>();
        }

        public void PrepareInstantHidden()
        {
            Cache();

            if (useFade)
            {
                if (cg == null) cg = target.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
            }

            target.anchoredPosition = _basePos + fromOffset;
        }

        public Tween Play()
        {
            Cache();

            DOTween.Kill(this);

            if (useFade)
            {
                if (cg == null) cg = target.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
            }

            // стартовая позиция
            target.anchoredPosition = _basePos + fromOffset;
            target.localScale = _baseScale; // не ломаем scale

            Sequence s = DOTween.Sequence().SetUpdate(true).SetId(this);
            s.AppendInterval(delay);

            if (useFade) s.Join(cg.DOFade(1f, duration * 0.6f).SetEase(Ease.OutQuad));
            s.Join(target.DOAnchorPos(_basePos, duration).SetEase(ease));

            if (punchOnArrive)
                s.Append(target.DOPunchScale(_baseScale * punchScale, 0.18f, 10, 0.8f));

            return s;
        }

        private void OnDisable()
        {
            DOTween.Kill(this);
            // возвращаем в базу, чтобы не было дрейфа при повторном заходе
            if (_cached && target != null)
            {
                target.anchoredPosition = _basePos;
                target.localScale = _baseScale;
                if (cg != null) cg.alpha = 1f;
            }
        }
    }
}