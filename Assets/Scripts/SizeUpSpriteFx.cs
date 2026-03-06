using DG.Tweening;
using UnityEngine;

public class SizeUpSpriteFx : MonoBehaviour
{
    [Header("Look At Camera")]
    [SerializeField] private bool faceCamera = true;
    [SerializeField] private bool lockXRotation = false;
    [SerializeField] private bool lockZRotation = false;

    [Header("Spawn Animation")]
    [SerializeField] private float startScale = 0.2f;
    [SerializeField] private float overshootScale = 1.2f;
    [SerializeField] private float finalScale = 1f;
    [SerializeField] private float enterDuration = 0.18f;
    [SerializeField] private float settleDuration = 0.10f;

    [Header("Punch / Wobble")]
    [SerializeField] private float punchScale = 0.12f;
    [SerializeField] private float punchDuration = 0.18f;
    [SerializeField] private float wobbleAngle = 8f;
    [SerializeField] private float wobbleDuration = 0.18f;

    [Header("Fly Up")]
    [SerializeField] private float holdDuration = 0.15f;
    [SerializeField] private float flyUpDistance = 1.5f;
    [SerializeField] private float flyUpDuration = 0.35f;

    [Header("Destroy")]
    [SerializeField] private bool destroyOnComplete = true;

    private Sequence _sequence;
    private Camera _cam;
    private Vector3 _baseScale;
    private Vector3 _startPos;

    private void Awake()
    {
        _cam = Camera.main;
        _baseScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
    }

    private void Start()
    {
        Play();
    }

    private void LateUpdate()
    {
        if (!faceCamera) return;

        if (_cam == null)
            _cam = Camera.main;

        if (_cam == null) return;

        Vector3 dir = transform.position - _cam.transform.position;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion lookRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        Vector3 euler = lookRot.eulerAngles;

        if (lockXRotation) euler.x = 0f;
        if (lockZRotation) euler.z = 0f;

        transform.rotation = Quaternion.Euler(euler);
    }

    public void Play()
    {
        if (_sequence != null)
            _sequence.Kill();

        _startPos = transform.position;
        transform.localScale = _baseScale * startScale;

        _sequence = DOTween.Sequence();
        _sequence.SetUpdate(false);

        // Красивое появление
        _sequence.Append(
            transform.DOScale(_baseScale * overshootScale, enterDuration)
                .SetEase(Ease.OutBack)
        );

        _sequence.Append(
            transform.DOScale(_baseScale * finalScale, settleDuration)
                .SetEase(Ease.OutCubic)
        );

        // Легкий punch
        _sequence.Append(
            transform.DOPunchScale(Vector3.one * punchScale, punchDuration, 1, 0.4f)
        );

        // Небольшой wobble
        _sequence.Join(
            transform.DORotate(new Vector3(0f, 0f, wobbleAngle), wobbleDuration * 0.33f)
                .SetRelative(true)
                .SetEase(Ease.OutSine)
        );

        _sequence.Append(
            transform.DORotate(new Vector3(0f, 0f, -wobbleAngle * 1.4f), wobbleDuration * 0.33f)
                .SetRelative(true)
                .SetEase(Ease.OutSine)
        );

        _sequence.Append(
            transform.DORotate(new Vector3(0f, 0f, wobbleAngle * 0.4f), wobbleDuration * 0.34f)
                .SetRelative(true)
                .SetEase(Ease.OutSine)
        );

        // Короткая пауза
        _sequence.AppendInterval(holdDuration);

        // Полет вверх
        Vector3 endPos = _startPos + Vector3.up * flyUpDistance;

        _sequence.Append(
            transform.DOMove(endPos, flyUpDuration)
                .SetEase(Ease.OutQuad)
        );

        _sequence.Join(
            transform.DOScale(_baseScale * 1.08f, flyUpDuration * 0.35f)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.OutSine)
        );

        _sequence.OnComplete(() =>
        {
            if (destroyOnComplete)
                Destroy(gameObject);
        });
    }

    private void OnDestroy()
    {
        if (_sequence != null)
            _sequence.Kill();
    }
}