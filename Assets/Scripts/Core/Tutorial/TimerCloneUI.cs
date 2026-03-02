using TMPro;
using UnityEngine;

public class TimerCloneUI : MonoBehaviour
{
    [SerializeField] private RunController run;
    [SerializeField] private TMP_Text timeText;

    private void Update()
    {
        if (run == null || timeText == null)
            return;

        if (!run.IsRunning)
            return;

        int sec = Mathf.CeilToInt(run.TimeLeft);
        int m = sec / 60;
        int s = sec % 60;

        timeText.text = $"{m:00}:{s:00}";
    }
}