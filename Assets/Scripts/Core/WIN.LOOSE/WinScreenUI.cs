using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WinScreenUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Stars (empty are always visible)")]
    [SerializeField] private Image[] emptyStars;   // 3 серых
    [SerializeField] private Image[] goldStars;    // 3 золотых (поверх)

    [Header("Time")]
    [SerializeField] private TMP_Text timeText;

    [Header("Buttons")]
    [SerializeField] private Button nextButton;

    [Header("Animation")]
    [SerializeField] private float firstStarDelay = 0.15f;
    [SerializeField] private float betweenStarsDelay = 0.18f;
    [SerializeField] private float popDuration = 0.18f;
    [SerializeField] private float overshootScale = 1.15f;

    private Coroutine _routine;

    public void Show(int starsCount, float timeSpentSeconds, System.Action onNext)
    {
        if (root) root.SetActive(true);

        // время
        if (timeText)
            timeText.text = FormatTime(timeSpentSeconds);

        // кнопка
        if (nextButton)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(() => onNext?.Invoke());
        }

        // пустые звезды всегда "пустые"
        SetEmptyStars();

        // золотые скрыть и сбросить
        ResetGoldStars();

        // анимация золота
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(AnimateGoldStars(Mathf.Clamp(starsCount, 0, 3)));
    }

    public void Hide()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = null;

        if (root) root.SetActive(false);
    }

    private void SetEmptyStars()
    {
        if (emptyStars == null) return;
        for (int i = 0; i < emptyStars.Length; i++)
        {
            if (!emptyStars[i]) continue;
            emptyStars[i].enabled = true;
            emptyStars[i].transform.localScale = Vector3.one;
            var c = emptyStars[i].color;
            c.a = 1f;
            emptyStars[i].color = c;
        }
    }

    private void ResetGoldStars()
    {
        if (goldStars == null) return;
        for (int i = 0; i < goldStars.Length; i++)
        {
            if (!goldStars[i]) continue;

            goldStars[i].enabled = true;

            // скрываем: 0 scale + 0 alpha
            goldStars[i].transform.localScale = Vector3.zero;
            var c = goldStars[i].color;
            c.a = 0f;
            goldStars[i].color = c;
        }
    }

    private IEnumerator AnimateGoldStars(int starsCount)
    {
        if (goldStars == null || goldStars.Length == 0) yield break;

        if (firstStarDelay > 0f)
            yield return new WaitForSeconds(firstStarDelay);

        for (int i = 0; i < goldStars.Length; i++)
        {
            if (i >= starsCount) yield break;
            var star = goldStars[i];
            if (!star) continue;

            yield return PopStar(star);

            if (betweenStarsDelay > 0f)
                yield return new WaitForSeconds(betweenStarsDelay);
        }
    }

    private IEnumerator PopStar(Image star)
    {
        // 0 -> overshoot -> 1 + alpha 0 -> 1
        float t = 0f;
        float dur = Mathf.Max(0.01f, popDuration);

        // старт
        star.transform.localScale = Vector3.zero;
        var c = star.color; c.a = 0f; star.color = c;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);

            // простая "пружинка" без DOTween:
            // k < 0.7: разгон к overshoot, потом спад к 1
            float scale;
            if (k < 0.7f)
            {
                float a = k / 0.7f;
                scale = Mathf.Lerp(0f, overshootScale, a);
            }
            else
            {
                float a = (k - 0.7f) / 0.3f;
                scale = Mathf.Lerp(overshootScale, 1f, a);
            }

            star.transform.localScale = Vector3.one * scale;

            var cc = star.color;
            cc.a = Mathf.Lerp(0f, 1f, k);
            star.color = cc;

            yield return null;
        }

        star.transform.localScale = Vector3.one;
        var c2 = star.color; c2.a = 1f; star.color = c2;
    }

    private static string FormatTime(float seconds)
    {
        int sec = Mathf.Clamp(Mathf.CeilToInt(seconds), 0, 99 * 60 + 59);
        int m = sec / 60;
        int s = sec % 60;
        return $"{m:00}:{s:00}";
    }
}