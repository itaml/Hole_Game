using System.Collections;
using GameBridge.Contracts;
using GameBridge.SceneFlow;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TutorialPopupUI popup;
    [SerializeField] private RunController run;

    [Header("Level 1 conditions")]
    [SerializeField] private ObjectiveTracker objectives;
    [SerializeField] private Transform holeTransform;
    [SerializeField] private float moveThreshold = 0.25f;

    [Header("Texts")]
    [TextArea] [SerializeField] private string level1_step1 = "Drag around to movethe hole.";
    [TextArea] [SerializeField] private string level1_step2 = "Collect items shownon goal bar!";

    [TextArea] [SerializeField] private string level2_text = "Collect all of the goals beforetime is over!";

    [TextArea] [SerializeField] private string level4_text = "Temporarily boost your holesize for 10 seconds";
    [TextArea] [SerializeField] private string level8_text = "Pulls nearby itemstowards you for 10 sec";
    [TextArea] [SerializeField] private string level10_text = "Stops the timefor 15 sec";

    [Header("Unlock roots (each contains boost + arrow, you already made them красивыми)")]
    [SerializeField] private GameObject unlockLvl4Root;
    [SerializeField] private GameObject unlockLvl8Root;
    [SerializeField] private GameObject unlockLvl10Root;

    private bool _waitingForClose;

    private void Awake()
    {
       // popup?.Close();

        if (unlockLvl4Root) unlockLvl4Root.SetActive(false);
        if (unlockLvl8Root) unlockLvl8Root.SetActive(false);
        if (unlockLvl10Root) unlockLvl10Root.SetActive(false);
    }

    private void Start()
    {
        if (popup == null || run == null)
            return;

        int lvl = GetLevel1Based();

        if (lvl == 1) StartCoroutine(Level1Routine());
        else if (lvl == 2) StartCoroutine(Level2Routine());
        else if (lvl == 4) StartCoroutine(BoostRoutine(4));
        else if (lvl == 8) StartCoroutine(BoostRoutine(8));
        else if (lvl == 10) StartCoroutine(BoostRoutine(10));
    }

    // ЭТО ВЕШАЕШЬ НА КНОПКУ, ЕСЛИ ХОЧЕШЬ (но popup сам её вызывает через Action)
public void CloseCurrentGuide()
{
    _waitingForClose = false;

    popup.Close();
    popup.gameObject.SetActive(false);

    if (unlockLvl4Root) unlockLvl4Root.SetActive(false);
    if (unlockLvl8Root) unlockLvl8Root.SetActive(false);
    if (unlockLvl10Root) unlockLvl10Root.SetActive(false);
}

    private IEnumerator Level1Routine()
    {
        while (!run.IsRunning) yield return null;

        if (objectives == null || holeTransform == null)
            yield break;

        // Step 1
        popup.ShowLevel1(level1_step1);

        Vector3 startPos = holeTransform.position;
        while (Vector3.Distance(holeTransform.position, startPos) < moveThreshold)
            yield return null;

        popup.Close();
        yield return new WaitForSecondsRealtime(0.15f);

        // Step 2
        popup.ShowLevel1(level1_step2);

        while (!objectives.IsComplete())
            yield return null;

        popup.Close();
    }

    private IEnumerator Level2Routine()
    {
        while (!run.IsRunning) yield return null;

        _waitingForClose = true;

        // Level 2 закрывается кнопкой внутри level2Container
        popup.ShowLevel2(level2_text, CloseCurrentGuide);

        while (_waitingForClose) yield return null;
    }

    private IEnumerator BoostRoutine(int level)
    {
        while (!run.IsRunning) yield return null;

        // включаем нужный unlock-root (стрелка+буст)
        if (level == 4 && unlockLvl4Root) unlockLvl4Root.SetActive(true);
        if (level == 8 && unlockLvl8Root) unlockLvl8Root.SetActive(true);
        if (level == 10 && unlockLvl10Root) unlockLvl10Root.SetActive(true);

        string msg = level switch
        {
            4 => level4_text,
            8 => level8_text,
            10 => level10_text,
            _ => ""
        };

        _waitingForClose = true;

        // 4/8/10 используют ОДНУ boost-панель и ОДИН TMP_Text внутри неё
        popup.ShowBoost(msg, CloseCurrentGuide);

        while (_waitingForClose) yield return null;
    }

    private int GetLevel1Based()
    {
        RunConfig cfg = SceneFlow.PendingRunConfig;
        if (cfg != null) return Mathf.Max(1, cfg.levelIndex);
        return 1;
    }
}