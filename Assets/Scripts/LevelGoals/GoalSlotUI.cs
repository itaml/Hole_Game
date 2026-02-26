using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoalSlotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Image checkImage; // галочка

    [Header("Punch Anim")]
    [SerializeField] private float punchScale = 1.15f;
    [SerializeField] private float punchUpTime = 0.08f;
    [SerializeField] private float punchDownTime = 0.12f;

    [Header("Complete Collapse")]
    [SerializeField] private float completeDelay = 0.35f;
    [SerializeField] private float collapseTime = 0.18f;
    [SerializeField] private bool destroyOnComplete = true;

    private RectTransform _rt;
    private Vector3 _baseScale;
    private bool _completed;

    public RectTransform Target => (RectTransform)transform;

    private void Awake()
    {
        _rt = (RectTransform)transform;
        _baseScale = _rt.localScale;
        if (checkImage) checkImage.gameObject.SetActive(false);
    }

    public void Setup(Sprite icon, int requiredRemaining)
    {
        if (iconImage) iconImage.sprite = icon;
        SetRemaining(requiredRemaining);
    }

    public void SetRemaining(int remaining)
    {
        if (countText) countText.text = Mathf.Max(0, remaining).ToString();
    }

    /// <summary>Вызывай, когда иконка долетела до слота.</summary>
    public void PlayArrivePunch()
    {
        if (_completed) return;
        StopAllCoroutines();
        StartCoroutine(PunchRoutine());
    }

    public void MarkComplete()
    {
        if (_completed) return;
        _completed = true;

        // 0 остаётся
        SetRemaining(0);

        // показать галочку
        if (checkImage) checkImage.gameObject.SetActive(true);

        StopAllCoroutines();
        StartCoroutine(CompleteRoutine());
    }

    private IEnumerator PunchRoutine()
    {
        // чуть увеличили
        float t = 0f;
        Vector3 up = _baseScale * punchScale;

        while (t < punchUpTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / punchUpTime);
            _rt.localScale = Vector3.Lerp(_baseScale, up, k);
            yield return null;
        }

        // вернули обратно
        t = 0f;
        while (t < punchDownTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / punchDownTime);
            _rt.localScale = Vector3.Lerp(up, _baseScale, k);
            yield return null;
        }

        _rt.localScale = _baseScale;
    }

    private IEnumerator CompleteRoutine()
    {
        // небольшая пауза как в оригинале
        float wait = Mathf.Max(0f, completeDelay);
        float t = 0f;
        while (t < wait)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // схлопывание
        t = 0f;
        Vector3 from = _baseScale;
        Vector3 to = Vector3.zero;

        while (t < collapseTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / collapseTime);
            _rt.localScale = Vector3.Lerp(from, to, k);
            yield return null;
        }

        _rt.localScale = Vector3.zero;

        if (destroyOnComplete)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }
}