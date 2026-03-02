using UnityEngine;
using UnityEngine.UI;

public class PauseUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Buttons")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button exitButton;

    [Header("Refs")]
    [SerializeField] private RunController run;

    private bool _paused;

    private void Awake()
    {
        if (root) root.SetActive(false);

        if (pauseButton)
            pauseButton.onClick.AddListener(Pause);

        if (resumeButton)
            resumeButton.onClick.AddListener(Resume);

        if (exitButton)
            exitButton.onClick.AddListener(ExitToMenuLose);
    }

    private void Pause()
    {
        if (_paused || run == null || !run.IsRunning) return;

        _paused = true;
        Time.timeScale = 0f;

        if (root) root.SetActive(true);
    }

    private void Resume()
    {
        if (!_paused) return;

        _paused = false;
        Time.timeScale = 1f;

        if (root) root.SetActive(false);
    }

    private void ExitToMenuLose()
    {
        if (run == null) return;

        _paused = false;
        Time.timeScale = 1f;

        if (root) root.SetActive(false);

        // выход из паузы = луз
        run.QuitToMenuFromPause();
    }
}