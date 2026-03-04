using System.Collections;
using DG.Tweening;
using GameBridge.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.UI
{
    public sealed class MenuWinCinematicController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private MenuRoot root;
        [SerializeField] private RewardPopupRouter popupRouter;
        [SerializeField] private RewardPopupQueue popupQueue;

        [Header("Tutorial")]
        [SerializeField] private StartTutorialPopupUi tutorialPopupProfile;
        [SerializeField] private StartTutorialPopupUi tutorialPopupLeaderboard;
        [SerializeField] private StartTutorialPopupUi tutorialPopupBattlepassStep1;
        [SerializeField] private StartTutorialPopupUi tutorialPopupBattlepassStep2;

        [Header("Block input")]
        [SerializeField] private CanvasGroup inputBlocker;

        [Header("Fly VFX")]
        [SerializeField] private RectTransform flyLayer;
        [SerializeField] private RectTransform flyIconPrefab;
        [SerializeField] private RectTransform flySpawnCenter;

        [Header("Targets")]
        [SerializeField] private RectTransform battlepassButton;
        [SerializeField] private RectTransform levelsChestButton;
        [SerializeField] private RectTransform starsChestButton;

        [Header("Token Sprites")]
        [SerializeField] private Sprite battlepassTokenSprite;
        [SerializeField] private Sprite levelChestTokenSprite;
        [SerializeField] private Sprite starTokenSprite;

        [Header("Timing")]
        [Tooltip("1 = как сейчас. 1.3-1.8 обычно комфортно. 2 = заметно медленнее.")]
        [SerializeField] private float speedMultiplier = 1.6f;

        [SerializeField] private float preFlyHold = 0.18f;        // пауза после появления токена перед полётом
        [SerializeField] private float postFlyHold = 0.12f;       // пауза после прилёта
        [SerializeField] private float betweenStagesHold = 0.12f; // пауза между крупными стадиями

        [SerializeField] private float flyBattlepass = 0.55f;
        [SerializeField] private float flyLevel = 0.48f;
        [SerializeField] private float flyStars = 0.50f;

        [SerializeField] private float punchTime = 0.26f;
        [SerializeField] private float openPunchTime = 0.34f;

        [SerializeField] private float popupAfterOpenHold = 0.18f;

        private void Reset()
        {
            root = FindFirstObjectByType<MenuRoot>();
            popupQueue = FindFirstObjectByType<RewardPopupQueue>();
        }

        private void Start()
        {
            if (root == null) return;
            if (root.AppliedLevelResult != null)
                StartCoroutine(PlayFlow(root.AppliedLevelResult));
        }

        private IEnumerator PlayFlow(LevelResult r)
        {
            if (r.outcome != LevelOutcome.Win)
                yield break;

            SetBlocked(true);
            root.SuppressAutoRewardPopups = true;

            var allGranted = root.Meta.ConsumeGrantedRewards();

            // 1) Battlepass
            if (IsBattlepassUnlocked(r.levelIndex) && r.battlepassItemsCollected > 0 && battlepassButton != null)
            {
                yield return FlyTo(
                    sprite: battlepassTokenSprite,
                    amount: r.battlepassItemsCollected,
                    from: flySpawnCenter,
                    to: battlepassButton,
                    duration: flyBattlepass * speedMultiplier);

                Punch(battlepassButton, punchTime * speedMultiplier);
                yield return Wait(betweenStagesHold * speedMultiplier);
            }

            // 2) Level chest token
            int levelChestAdds = IsLevelsChestUnlocked(r.levelIndex) ? 1 : 0;
            int levelOpenCount = 0;

            if (levelChestAdds > 0 && levelsChestButton != null)
            {
                yield return FlyTo(levelChestTokenSprite, levelChestAdds, flySpawnCenter, levelsChestButton, flyLevel * speedMultiplier);
                Punch(levelsChestButton, punchTime * speedMultiplier);
                yield return Wait(postFlyHold * speedMultiplier);

                int threshold = Mathf.Max(1, root.levelsChestConfig.threshold);
                int total = root.PreLevelsChestProgress + levelChestAdds;
                levelOpenCount = total / threshold;

                if (levelOpenCount > 0)
                {
                    // OPEN (заметнее и дольше)
                    levelsChestButton.DOPunchRotation(new Vector3(0, 0, 12f), openPunchTime * speedMultiplier, 12, 0.9f);
                    levelsChestButton.DOPunchScale(Vector3.one * 0.28f, openPunchTime * speedMultiplier, 12, 0.9f);

                    yield return Wait(popupAfterOpenHold * speedMultiplier);

                    if (allGranted != null && allGranted.Length > 0)
                    {
                        var lvlRewards = TakeFirst(allGranted, levelOpenCount);
                        ShowRewards(lvlRewards);
                        yield return WaitForPopups();
                    }

                    yield return Wait(betweenStagesHold * speedMultiplier);
                }
            }

            // 3) Stars
            int starsAdds = IsStarsChestUnlocked(r.levelIndex) ? Mathf.Max(0, r.starsEarned) : 0;
            int starOpenCount = 0;

            if (starsAdds > 0 && starsChestButton != null)
            {
                yield return FlyTo(starTokenSprite, starsAdds, flySpawnCenter, starsChestButton, flyStars * speedMultiplier);
                Punch(starsChestButton, punchTime * speedMultiplier);
                yield return Wait(postFlyHold * speedMultiplier);

                int threshold = Mathf.Max(1, root.starsChestConfig.threshold);
                int total = root.PreStarsChestProgress + starsAdds;
                starOpenCount = total / threshold;

                if (starOpenCount > 0)
                {
                    starsChestButton.DOPunchRotation(new Vector3(0, 0, -12f), openPunchTime * speedMultiplier, 12, 0.9f);
                    starsChestButton.DOPunchScale(Vector3.one * 0.28f, openPunchTime * speedMultiplier, 12, 0.9f);

                    yield return Wait(popupAfterOpenHold * speedMultiplier);

                    var starRewards = SkipFirst(allGranted, levelOpenCount);
                    starRewards = TakeFirst(starRewards, starOpenCount);

                    ShowRewards(starRewards);
                    yield return WaitForPopups();
                }
            }

            
// 4) Any remaining rewards (e.g., feature unlock welcome grants: buffs/boosts, etc.)
// MenuWinCinematicController ранее показывал только награды, связанные с открытием сундуков.
// Из-за этого "welcome rewards" могли теряться и попап не появлялся.
if (allGranted != null && allGranted.Length > 0)
{
    int consumed = levelOpenCount + starOpenCount;
    var remaining = SkipFirst(allGranted, consumed);
    if (remaining != null && remaining.Length > 0)
    {
        ShowRewards(remaining);
        yield return WaitForPopups();
    }
}

            // 5) Post-win tutorial popups (after all animations + reward popups)
            yield return TryShowPostWinProfileTutorial();
            yield return TryShowStartLeaderboardTutorial();
            yield return TryShowStartBattlepassTutorial();
            root.SuppressAutoRewardPopups = false;
            SetBlocked(false);
        }

        private IEnumerator TryShowStartBattlepassTutorial()
        {
            if (tutorialPopupBattlepassStep1 == null) yield break;
            if (tutorialPopupBattlepassStep2 == null) yield break;
            if (root == null || root.Meta == null) yield break;

            var save = root.Meta.Save;
            if (save == null || save.tutorial == null) yield break;

            int id = save.tutorial.pendingStartTutorialId;
            if (id != 5) yield break;

            // Clear pending immediately
            save.tutorial.pendingStartTutorialId = 0;

            // Step 1 (кнопка Close в этом попапе = "Continue")
            tutorialPopupBattlepassStep1.Show();
            while (tutorialPopupBattlepassStep1 != null && tutorialPopupBattlepassStep1.IsShown)
                yield return null;

            // Step 2
            tutorialPopupBattlepassStep2.Show();
            while (tutorialPopupBattlepassStep2 != null && tutorialPopupBattlepassStep2.IsShown)
                yield return null;

            // Mark shown
            save.tutorial.battlepassUnlockTutorialShown = true;

            root.Meta.SaveNow();
        }

        private IEnumerator TryShowStartLeaderboardTutorial()
        {
            if (tutorialPopupLeaderboard == null) yield break;
            if (root == null || root.Meta == null) yield break;

            var save = root.Meta.Save;
            if (save == null || save.tutorial == null) yield break;

            int id = save.tutorial.pendingStartTutorialId;
            Debug.Log($"StartTutorialId = {id}");

            if (id != 4) yield break; // нам нужен именно лидерборд

            // Clear pending immediately
            save.tutorial.pendingStartTutorialId = 0;

            tutorialPopupLeaderboard.Show();

            while (tutorialPopupLeaderboard != null && tutorialPopupLeaderboard.IsShown)
                yield return null;

            // Mark shown
            save.tutorial.leaderboardUnlockTutorialShown = true;

            root.Meta.SaveNow();
        }

        private IEnumerator TryShowPostWinProfileTutorial()
        {
            if (tutorialPopupProfile == null) yield break;
            if (root == null || root.Meta == null) yield break;

            var save = root.Meta.Save;
            if (save == null || save.tutorial == null) yield break;

            int id = save.tutorial.pendingPostWinTutorialId;
            if (id == 0) yield break;

            // Clear pending immediately to avoid duplicates if UI re-renders.
            save.tutorial.pendingPostWinTutorialId = 0;

            tutorialPopupProfile.Show();

            // Wait until player closes the tutorial popup
            while (tutorialPopupProfile != null && tutorialPopupProfile.IsShown)
                yield return null;

            // Mark shown
            if (id == 1)
                save.tutorial.profilePostWinTutorialShownProfile = true;

            root.Meta.SaveNow();
        }

        private void ShowRewards(Core.Configs.Reward[] rewards)
        {
            if (rewards == null || rewards.Length == 0) return;
            popupRouter.ShowRewards(rewards);
        }

        private IEnumerator WaitForPopups()
        {
            if (popupQueue == null) yield break;
            while (popupQueue.IsBusy) yield return null;

            // маленькая пауза после закрытия последнего попапа, чтобы не “рубило”
            yield return Wait(0.10f * speedMultiplier);
        }

        private IEnumerator FlyTo(Sprite sprite, int amount, RectTransform from, RectTransform to, float duration)
        {
            if (flyLayer == null || flyIconPrefab == null || from == null || to == null)
                yield break;

            var inst = Instantiate(flyIconPrefab, flyLayer);
            inst.gameObject.SetActive(true);

            var img = inst.GetComponentInChildren<Image>(true);
            if (img != null) img.sprite = sprite;

            var txt = inst.GetComponentInChildren<TMP_Text>(true);
            if (txt != null) txt.text = amount > 1 ? $"+{amount}" : "";

            inst.position = from.position;

            // появление — чтобы игрок успел увидеть объект ДО полёта
            inst.localScale = Vector3.one * 0.70f;
            inst.DOScale(1.0f, 0.18f * speedMultiplier).SetEase(Ease.OutBack).SetUpdate(true);

            yield return Wait(preFlyHold * speedMultiplier);

            // легкий “подпрыг” перед стартом
            inst.DOPunchScale(Vector3.one * 0.08f, 0.18f * speedMultiplier, 8, 0.8f).SetUpdate(true);

            Vector3 p0 = from.position;
            Vector3 p2 = to.position;
            Vector3 mid = (p0 + p2) * 0.5f + Vector3.up * 200f;

            bool done = false;

            inst.DOPath(new[] { p0, mid, p2 }, duration, PathType.CatmullRom)
                .SetEase(Ease.InOutCubic)
                .SetUpdate(true)
                .OnComplete(() => done = true);

            inst.DOScale(0.65f, duration).SetEase(Ease.InCubic).SetUpdate(true);

            while (!done) yield return null;

            // микропаузка “приземления”
            yield return Wait(postFlyHold * speedMultiplier);

            Destroy(inst.gameObject);
        }

        private static void Punch(RectTransform tr, float time)
        {
            if (tr == null) return;
            tr.DOPunchScale(Vector3.one * 0.12f, time, 10, 0.8f);
        }

        private void SetBlocked(bool blocked)
        {
            if (inputBlocker == null) return;
            inputBlocker.alpha = 0f;
            inputBlocker.blocksRaycasts = blocked;
            inputBlocker.interactable = blocked;
        }

        private static IEnumerator Wait(float t)
        {
            if (t <= 0f) yield break;
            yield return new WaitForSecondsRealtime(t);
        }

        private bool IsBattlepassUnlocked(int levelIndex)
            => root != null && root.unlockConfig != null && levelIndex >= root.unlockConfig.battlepassUnlockLevel;

        private bool IsLevelsChestUnlocked(int levelIndex)
            => root != null && root.unlockConfig != null && levelIndex >= root.unlockConfig.levelsChestUnlockLevel;

        private bool IsStarsChestUnlocked(int levelIndex)
            => root != null && root.unlockConfig != null && levelIndex >= root.unlockConfig.starsChestUnlockLevel;

        private static T[] TakeFirst<T>(T[] arr, int count)
        {
            if (arr == null || count <= 0) return System.Array.Empty<T>();
            if (count >= arr.Length) return arr;

            var res = new T[count];
            System.Array.Copy(arr, 0, res, 0, count);
            return res;
        }

        private static T[] SkipFirst<T>(T[] arr, int skip)
        {
            if (arr == null) return System.Array.Empty<T>();
            if (skip <= 0) return arr;
            if (skip >= arr.Length) return System.Array.Empty<T>();

            int n = arr.Length - skip;
            var res = new T[n];
            System.Array.Copy(arr, skip, res, 0, n);
            return res;
        }
    }
}