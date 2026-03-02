using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Core.Levels;

namespace Menu.UI
{
    public sealed class StartPopupUi : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject rootObject;
        [SerializeField] private TMP_Text levelText;

        [Header("Level Type")]
        [SerializeField] private GameObject masterLevel;
        [SerializeField] private GameObject challengeLevel;
        [SerializeField] private Sprite challengeLevelSpr;
        [SerializeField] private Sprite masterLevelSpr;
        [SerializeField] private Sprite defaultLevelSpr;
        [SerializeField] private Image levelImg;

        [Header("Tutorial (optional)")]
        [SerializeField] private StartTutorialPopupUi tutorialPopup;
        [SerializeField] private StartTutorialPopupUi tutorialPopup2;
        [SerializeField] private StartTutorialPopupUi tutorialPopup3;
        [SerializeField] private Sprite unlimitedBoostActive;
        [SerializeField] private Sprite selectedBoostActive;
        [SerializeField] private Sprite unSelectedBoostActive;

        [Header("Boost 1")]
        [SerializeField] private GameObject boost1Closed;
        [SerializeField] private Button boost1Button;
        [SerializeField] private TMP_Text boost1CountText;
        [SerializeField] private GameObject boost1Infinity;
        [SerializeField] private TMP_Text boost1InfinityTimerText;
        [SerializeField] private GameObject boost1SelectedMark;

        [Header("Boost 2")]
        [SerializeField] private GameObject boost2Closed;
        [SerializeField] private Button boost2Button;
        [SerializeField] private TMP_Text boost2CountText;
        [SerializeField] private GameObject boost2Infinity;
        [SerializeField] private TMP_Text boost2InfinityTimerText;
        [SerializeField] private GameObject boost2SelectedMark;

        [Header("Bottom")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button closeButton;

        [SerializeField] private Button infoBtn;
        [SerializeField] private Button infoCloseBtn;
        [SerializeField] private Button infoClose2Btn;
        [SerializeField] private GameObject infoLabel;

        [SerializeField] private PopupTween tween;
        [SerializeField] private PopupTween tweenInfo;

        private MenuRoot _menu;

        private void Awake()
        {
            if (boost1Button != null) boost1Button.onClick.AddListener(ToggleBoost1);
            if (boost2Button != null) boost2Button.onClick.AddListener(ToggleBoost2);
            if (startGameButton != null) startGameButton.onClick.AddListener(OnClickStartGame);
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (infoBtn != null) infoBtn.onClick.AddListener(ShowInfo);
            if (infoCloseBtn != null) infoCloseBtn.onClick.AddListener(HideInfo);
            if (infoClose2Btn != null) infoClose2Btn.onClick.AddListener(HideInfo);

            if (tween == null) tween = GetComponent<PopupTween>();

            if (rootObject == null) rootObject = gameObject;
        }

        private void OnDestroy()
        {
            if (boost1Button != null) boost1Button.onClick.RemoveListener(ToggleBoost1);
            if (boost2Button != null) boost2Button.onClick.RemoveListener(ToggleBoost2);
            if (startGameButton != null) startGameButton.onClick.RemoveListener(OnClickStartGame);
            if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
            if (infoBtn != null) infoBtn.onClick.RemoveListener(ShowInfo);
            if (infoCloseBtn != null) infoCloseBtn.onClick.RemoveListener(HideInfo);
            if (infoClose2Btn != null) infoClose2Btn.onClick.RemoveListener(HideInfo);
        }

        private GameObject StartGo => rootObject != null ? rootObject : gameObject;

        private void SetStartActive(bool active) => StartGo.SetActive(active);

        private void SetInfoActive(bool active)
        {
            if (infoLabel != null) infoLabel.SetActive(active);
        }

        public void ShowInfo()
        {
            //   ,    
            SetInfoActive(true);

            //  start,     GO
            if (tween != null)
            {
                tween.PlayHide(() => SetStartActive(false));
            }
            else
            {
                SetStartActive(false);
            }

            //  
            tweenInfo?.PlayShow();
        }

        public void HideInfo()
        {
            //      GO
            if (tweenInfo != null)
            {
                tweenInfo.PlayHide(() => SetInfoActive(false));
            }
            else
            {
                SetInfoActive(false);
            }

            //  start GO  
            SetStartActive(true);
            tween?.PlayShow();
        }

        public void Show(MenuRoot menu)
        {
            _menu = menu;

            if (rootObject != null) rootObject.SetActive(true);
            else gameObject.SetActive(true);

            tween?.PlayShow(); // 

            Render();
            TryShowStartTutorial();
        }

        public void Hide()
        {
            _menu = null;

            if (tween != null)
            {
                tween.PlayHide(() =>
                {
                    if (rootObject != null) rootObject.SetActive(false);
                    else gameObject.SetActive(false);
                });
            }
            else
            {
                if (rootObject != null) rootObject.SetActive(false);
                else gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (_menu == null || _menu.Meta == null) return;
            Render();
            TryShowStartTutorial();
        }


private void TryShowStartTutorial()
{
    if (tutorialPopup == null) return;
    if (_menu == null || _menu.Meta == null) return;

    var save = _menu.Meta.Save;
    if (save == null || save.tutorial == null) return;

    int id = save.tutorial.pendingStartTutorialId;
    if (id == 0) return;

    // Clear pending immediately to avoid duplicates if StartPopup re-renders.
    save.tutorial.pendingStartTutorialId = 0;

    if (id == 1) tutorialPopup.Show();
    if (id == 2) tutorialPopup2.Show();
    if (id == 3) tutorialPopup3.Show();

    if (id == 1) save.tutorial.winStreakStartTutorialShown = true;
    if (id == 2) save.tutorial.boost1StartTutorialShown = true;
    if (id == 3) save.tutorial.boost2StartTutorialShown = true;

    _menu.Meta.SaveNow();
}

        private void Render()
        {
            var save = _menu.Meta.Save;
            int level = save.progress.currentLevel;

            if (levelText != null)
                levelText.text = $"Level {level}";

bool burnedSpecial = SpecialLevelBurnStorage.IsBurned(level);

            if (!burnedSpecial)
            {
                if (LevelTypeUtils.IsMasterLevel(level)) levelImg.sprite = masterLevelSpr;
                if (LevelTypeUtils.IsChallengeLevel(level)) levelImg.sprite = challengeLevelSpr;
            }
            else
            {
                levelImg.sprite = defaultLevelSpr;
            }

if (masterLevel != null)
                masterLevel.SetActive(!burnedSpecial && LevelTypeUtils.IsMasterLevel(level));
if (challengeLevel != null)
    challengeLevel.SetActive(!burnedSpecial && LevelTypeUtils.IsChallengeLevel(level));

            bool boost1Unlocked = _menu.unlockConfig != null && level >= _menu.unlockConfig.boost1UnlockLevel;
            bool boost2Unlocked = _menu.unlockConfig != null && level >= _menu.unlockConfig.boost2UnlockLevel;

            if (boost1Closed != null)
            {
                boost1Closed.SetActive(!boost1Unlocked);
                boost1Button.gameObject.SetActive(boost1Unlocked);
            }
            if (boost2Closed != null)
            {
                boost2Closed.SetActive(!boost2Unlocked);
                boost2Button.gameObject.SetActive(boost2Unlocked);
            }

            if (boost1CountText != null)
                boost1CountText.text = save.inventory.boostGrowWholeLevel.ToString();
            if (boost2CountText != null)
                boost2CountText.text = save.inventory.boostExtraTime.ToString();

            bool inf1 = _menu.Meta.IsInfiniteBoost1Active();
            bool inf2 = _menu.Meta.IsInfiniteBoost2Active();

            if (boost1Infinity != null)
            {
                boost1Infinity.SetActive(inf1);
                boost1CountText.transform.parent.gameObject.SetActive(!inf1);
            }
            if (boost2Infinity != null)
            {
                boost2Infinity.SetActive(inf2);
                boost2CountText.transform.parent.gameObject.SetActive(!inf2);
            }

            if (boost1InfinityTimerText != null)
                boost1InfinityTimerText.text = inf1 ? UiTimeFormat.FormatMinutesSeconds(GetBoost1TimeLeft()) : "";
            if (boost2InfinityTimerText != null)
                boost2InfinityTimerText.text = inf2 ? UiTimeFormat.FormatMinutesSeconds(GetBoost2TimeLeft()) : "";

            if (boost1SelectedMark != null)
            {
                boost1SelectedMark.SetActive(_menu.boost1Selected);
                boost1CountText.transform.parent.gameObject.SetActive(!_menu.boost1Selected);
                if (_menu.boost1Selected) boost1Button.GetComponent<Image>().sprite = selectedBoostActive;
                else boost1Button.GetComponent<Image>().sprite = unSelectedBoostActive;
            }
            if (boost2SelectedMark != null)
            {
                boost2SelectedMark.SetActive(_menu.boost2Selected);
                boost2CountText.transform.parent.gameObject.SetActive(!_menu.boost2Selected);
                if (_menu.boost2Selected) boost2Button.GetComponent<Image>().sprite = selectedBoostActive;
                else boost2Button.GetComponent<Image>().sprite = unSelectedBoostActive;
            }

            if (boost1Button != null)
            {
                bool canUse = boost1Unlocked && (inf1 || save.inventory.boostGrowWholeLevel > 0);
                boost1Button.interactable = canUse;
                if (!canUse) _menu.boost1Selected = false;
            }

            if (boost2Button != null)
            {
                bool canUse = boost2Unlocked && (inf2 || save.inventory.boostExtraTime > 0);
                boost2Button.interactable = canUse;
                if (!canUse) _menu.boost2Selected = false;
            }

            if (inf1) boost1Button.GetComponent<Image>().sprite = unlimitedBoostActive;
            if (inf2) boost2Button.GetComponent<Image>().sprite = unlimitedBoostActive;

            if (startGameButton != null)
                startGameButton.interactable = _menu.Meta.CanStartGame();
        }

        private TimeSpan GetBoost1TimeLeft()
        {
            long until = _menu.Meta.Save.timeBonuses.infiniteBoost1UntilUtcTicks;
            DateTime now = _menu.Time != null ? _menu.Time.UtcNow : DateTime.UtcNow;
            long dt = until - now.Ticks;
            if (dt <= 0) return TimeSpan.Zero;
            return TimeSpan.FromTicks(dt);
        }

        private TimeSpan GetBoost2TimeLeft()
        {
            long until = _menu.Meta.Save.timeBonuses.infiniteBoost2UntilUtcTicks;
            DateTime now = _menu.Time != null ? _menu.Time.UtcNow : DateTime.UtcNow;
            long dt = until - now.Ticks;
            if (dt <= 0) return TimeSpan.Zero;
            return TimeSpan.FromTicks(dt);
        }

        private void ToggleBoost1()
        {
            if (_menu == null) return;
            _menu.boost1Selected = !_menu.boost1Selected;
        }

        private void ToggleBoost2()
        {
            if (_menu == null) return;
            _menu.boost2Selected = !_menu.boost2Selected;
        }

        private void OnClickStartGame()
        {
            if (_menu == null) return;
            _menu.OnClickStart();
        }
    }
}