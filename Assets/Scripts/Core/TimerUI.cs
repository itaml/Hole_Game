using UnityEngine;
using UnityEngine.UI;

public class TimerUI : MonoBehaviour
{
    [SerializeField] private Image fill;
    [SerializeField] private Graphic timerGraphic; // Text или TMP или Image рамки
    [SerializeField] private float warnThreshold = 30f;
    [SerializeField] private float blinkSpeed = 6f;

    private Color _normalColor;
    private Color _warnColor = Color.red;

    private void Awake()
    {
        if (timerGraphic) _normalColor = timerGraphic.color;
    }

    public void Set(float timeLeft, float timeTotal)
    {
        if (fill) fill.fillAmount = Mathf.Clamp01(timeLeft / Mathf.Max(0.001f, timeTotal));

        bool warn = timeLeft <= warnThreshold;

        if (warn)
        {
            if (fill) fill.color = _warnColor;
            if (timerGraphic)
            {
                float t = Mathf.Abs(Mathf.Sin(Time.unscaledTime * blinkSpeed));
                timerGraphic.color = Color.Lerp(_warnColor, _normalColor, t);
            }
        }
        else
        {
            if (fill) fill.color = _normalColor;
            if (timerGraphic) timerGraphic.color = _normalColor;
        }
    }
}