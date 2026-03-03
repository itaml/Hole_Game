using UnityEngine;
using System.Collections;
using DG.Tweening;

public class UIPageManager : MonoBehaviour
{
    public static UIPageManager I { get; private set; }

    public GameObject inputBlocker;

    [Header("Pages")]
    public UIPageBase collections;
    public UIPageBase leaderboard;
    public UIPageBase team;

    private UIPageBase current;
    private bool transitioning;

    void Awake()
    {
        I = this;

        if (collections) collections.PrepareHideInstant();
        if (leaderboard) leaderboard.PrepareHideInstant();
        if (team) team.PrepareHideInstant();

        if (inputBlocker) inputBlocker.SetActive(false);
    }

    public void OpenCollections() => GoTo(collections);
    public void OpenLeaderboard() => GoTo(leaderboard);
    public void OpenTeam() => GoTo(team);

    public void GoHome() => StartCoroutine(GoHomeRoutine());

    public void GoTo(UIPageBase page) => StartCoroutine(GoToRoutine(page));

    private IEnumerator GoToRoutine(UIPageBase next)
    {
        if (next == null) yield break;
        if (transitioning) yield break;
        if (current == next) yield break; // текущая кнопка таба

        transitioning = true;
        if (inputBlocker) inputBlocker.SetActive(true);

        if (current != null)
        {
            var outSeq = current.PlayOut();
            yield return outSeq.WaitForCompletion();
            current.PrepareHideInstant();
        }

        current = next;
        current.PrepareShow();
        var inSeq = current.PlayIn();
        yield return inSeq.WaitForCompletion();

        if (inputBlocker) inputBlocker.SetActive(false);
        transitioning = false;
    }

    private IEnumerator GoHomeRoutine()
    {
        if (transitioning) yield break;
        if (current == null) yield break;

        transitioning = true;
        if (inputBlocker) inputBlocker.SetActive(true);

        var outSeq = current.PlayOut();
        yield return outSeq.WaitForCompletion();
        current.PrepareHideInstant();
        current = null;

        if (inputBlocker) inputBlocker.SetActive(false);
        transitioning = false;
    }
}