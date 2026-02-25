using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimerUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image fill;
    [SerializeField] private TextMeshProUGUI timeText; // вот это тебе и нужно

    [Header("Warning")]
    [SerializeField] private float warnThresholdSec = 30f;
    [SerializeField] private float blinkSpeed = 6f;

    private Color _normalTextColor;
    private Color _normalFillColor;
    private readonly Color _warnColor = Color.red;

    private void Awake()
    {
        if (timeText != null) _normalTextColor = timeText.color;
        if (fill != null) _normalFillColor = fill.color;
    }

    /// <summary>
    /// timeLeftSec и timeTotalSec в секундах.
    /// </summary>
    public void Set(float timeLeftSec, float timeTotalSec)
    {
        // fill
        if (fill != null)
        {
            float amount = timeLeftSec / Mathf.Max(0.001f, timeTotalSec);
            fill.fillAmount = Mathf.Clamp01(amount);
        }

        // текст mm:ss
        if (timeText != null)
        {
            int sec = Mathf.CeilToInt(Mathf.Max(0f, timeLeftSec));
            int m = sec / 60;
            int s = sec % 60;
            timeText.text = $"{m:00}:{s:00}";
        }

        // warning
        bool warn = timeLeftSec <= warnThresholdSec;

        if (warn)
        {
            if (fill != null) fill.color = _warnColor;

            if (timeText != null)
            {
                // мигаем между красным и обычным
                float t = Mathf.Abs(Mathf.Sin(Time.unscaledTime * blinkSpeed));
                timeText.color = Color.Lerp(_warnColor, _normalTextColor, t);
            }
        }
        else
        {
            if (fill != null) fill.color = _normalFillColor;
            if (timeText != null) timeText.color = _normalTextColor;
        }
    }
}