using DG.Tweening;
using UnityEngine;

public class LogoIntroSpin : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private int spins = 2;
    [SerializeField] private float duration = 1.2f;
    [SerializeField] private Ease ease = Ease.OutCubic;

    [Header("Scale Punch")]
    [SerializeField] private float startScale = 0.8f;
    [SerializeField] private float punch = 0.15f;
    [SerializeField] private float punchTime = 0.25f;

    private Vector3 _startScale;

    void Start()
    {
        _startScale = transform.localScale;

        transform.localScale = _startScale * startScale;

        Sequence seq = DOTween.Sequence();
        seq.SetUpdate(true); // ← UN SCALED

        seq.Append(
            transform.DORotate(
                new Vector3(0, 360 * spins, 0),
                duration,
                RotateMode.FastBeyond360
            ).SetEase(ease)
        );

        seq.Join(
            transform.DOScale(_startScale, duration * 0.6f)
                .SetEase(Ease.OutBack)
        );

        seq.Append(
            transform.DOPunchScale(
                Vector3.one * punch,
                punchTime,
                6,
                0.8f
            )
        );
    }
}