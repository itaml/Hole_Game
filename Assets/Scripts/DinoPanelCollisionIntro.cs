using DG.Tweening;
using UnityEngine;

public class DinoPanelCollisionIntro : MonoBehaviour
{
    [Header("Refs (UI)")]
    [SerializeField] private RectTransform dino;   // динозавр (отдельный объект)
    [SerializeField] private RectTransform panel;  // панель (отдельный объект)

    [Header("Timings")]
    [SerializeField] private float flyTime = 0.55f;
    [SerializeField] private float settleTime = 0.20f;

    [Header("Offsets (start positions)")]
    [SerializeField] private float dinoFromLeft = 900f;   // насколько левее стартовать
    [SerializeField] private float panelFromRight = 900f; // насколько правее стартовать

    [Header("Ease")]
    [SerializeField] private Ease flyEase = Ease.OutCubic;
    [SerializeField] private Ease settleEase = Ease.OutBack;

    [Header("Impact")]
    [SerializeField] private float impactPunchScale = 0.10f;
    [SerializeField] private float impactPunchTime = 0.18f;
    [SerializeField] private float impactShakeTime = 0.12f;
    [SerializeField] private float impactShakeStrength = 12f; // для UI это px
    [SerializeField] private int impactShakeVibrato = 18;

    [Header("Optional tilt on impact")]
    [SerializeField] private float tiltDeg = 6f;
    [SerializeField] private float tiltTime = 0.10f;

    [Header("Behaviour")]
    [Tooltip("Анимация будет работать даже если Time.timeScale = 0.")]
    [SerializeField] private bool useUnscaledTime = true;

    private Vector2 _dinoHome;
    private Vector2 _panelHome;
    private bool _cached;

    private Tween _tween;

    private void Awake()
    {
        CacheHome();
    }

    private void OnDisable()
    {
        _tween?.Kill();
        dino?.DOKill();
        panel?.DOKill();
    }

    private void Start()
    {
        Play();
    }

    private void CacheHome()
    {
        if (_cached) return;
        if (dino != null) _dinoHome = dino.anchoredPosition;
        if (panel != null) _panelHome = panel.anchoredPosition;
        _cached = true;
    }

    public void Play()
    {
        if (dino == null || panel == null) return;

        CacheHome();

        _tween?.Kill();
        dino.DOKill();
        panel.DOKill();

        // стартовые позиции: динозавр левее, панель правее
        dino.anchoredPosition = _dinoHome + Vector2.left * Mathf.Abs(dinoFromLeft);
        panel.anchoredPosition = _panelHome + Vector2.right * Mathf.Abs(panelFromRight);

        // небольшой стартовый масштаб (чтоб “влетали” бодрее)
        dino.localScale = Vector3.one * 0.98f;
        panel.localScale = Vector3.one * 0.98f;

        var seq = DOTween.Sequence();
        if (useUnscaledTime) seq.SetUpdate(true);

        // 1) влёт навстречу
        seq.Append(dino.DOAnchorPos(_dinoHome, flyTime).SetEase(flyEase));
        seq.Join(panel.DOAnchorPos(_panelHome, flyTime).SetEase(flyEase));

        // 2) impact: punch + shake + tilt
        seq.AppendCallback(() =>
        {
            // punch scale обоим (ощущение удара)
            dino.DOPunchScale(Vector3.one * impactPunchScale, impactPunchTime, 8, 0.85f);
            panel.DOPunchScale(Vector3.one * impactPunchScale, impactPunchTime, 8, 0.85f);

            // shake позиции чуть-чуть
            dino.DOShakeAnchorPos(impactShakeTime, impactShakeStrength, impactShakeVibrato, 0f, false, true);
            panel.DOShakeAnchorPos(impactShakeTime, impactShakeStrength, impactShakeVibrato, 0f, false, true);

            // лёгкий наклон в разные стороны
            if (Mathf.Abs(tiltDeg) > 0.01f)
            {
                dino.DORotate(new Vector3(0, 0, tiltDeg), tiltTime).SetEase(Ease.OutCubic)
                    .OnComplete(() => dino.DORotate(Vector3.zero, tiltTime).SetEase(Ease.InCubic));

                panel.DORotate(new Vector3(0, 0, -tiltDeg), tiltTime).SetEase(Ease.OutCubic)
                    .OnComplete(() => panel.DORotate(Vector3.zero, tiltTime).SetEase(Ease.InCubic));
            }
        });

        // 3) settle: вернуть идеальный scale
        seq.Append(dino.DOScale(1f, settleTime).SetEase(settleEase));
        seq.Join(panel.DOScale(1f, settleTime).SetEase(settleEase));

        _tween = seq;
    }
}