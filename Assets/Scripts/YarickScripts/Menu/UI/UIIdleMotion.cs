using DG.Tweening;
using UnityEngine;

namespace Menu.UI
{
    public sealed class UIIdleMotion : MonoBehaviour
    {
        [SerializeField] private RectTransform target;

        [Header("Position float")]
        [SerializeField] private float floatY = 8f;
        [SerializeField] private float floatTime = 2.2f;

        [Header("Scale breathe")]
        [SerializeField] private float scaleAmp = 0.02f;
        [SerializeField] private float scaleTime = 2.6f;

        [Header("Optional rotate wiggle")]
        [SerializeField] private float rotateZ = 2f;
        [SerializeField] private float rotateTime = 3.0f;

        private Vector3 _basePos;
        private Vector3 _baseScale;
        private Vector3 _baseRot;

        private void Awake()
        {
            if (target == null) target = transform as RectTransform;
        }

        private void OnEnable()
        {
            if (target == null) return;

            _basePos = target.anchoredPosition3D;
            _baseScale = target.localScale;
            _baseRot = target.localEulerAngles;

            // float
            if (floatY != 0f)
            {
                target.anchoredPosition3D = _basePos;
                target.DOAnchorPos3DY(_basePos.y + floatY, floatTime)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetId(this);
            }

            // breathe
            if (scaleAmp != 0f)
            {
                target.localScale = _baseScale;
                target.DOScale(_baseScale * (1f + scaleAmp), scaleTime)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetId(this);
            }

            // rotate wiggle
            if (rotateZ != 0f)
            {
                target.localEulerAngles = _baseRot;
                target.DOLocalRotate(new Vector3(_baseRot.x, _baseRot.y, _baseRot.z + rotateZ), rotateTime)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetId(this);
            }
        }

        private void OnDisable()
        {
            DOTween.Kill(this);
        }
    }
}