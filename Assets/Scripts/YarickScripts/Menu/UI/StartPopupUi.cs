using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.UI
{
    public sealed class StartPopupUi : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject rootObject;
        [SerializeField] private TMP_Text levelText;

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

        private MenuRoot _menu;

        private void Awake()
        {
            if (boost1Button != null) boost1Button.onClick.AddListener(ToggleBoost1);
            if (boost2Button != null) boost2Button.onClick.AddListener(ToggleBoost2);
            if (startGameButton != null) startGameButton.onClick.AddListener(OnClickStartGame);
            if (closeButton != null) closeButton.onClick.AddListener(Hide);

            if (rootObject == null) rootObject = gameObject;
        }

        private void OnDestroy()
        {
            if (boost1Button != null) boost1Button.onClick.RemoveListener(ToggleBoost1);
            if (boost2Button != null) boost2Button.onClick.RemoveListener(ToggleBoost2);
            if (startGameButton != null) startGameButton.onClick.RemoveListener(OnClickStartGame);
            if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
        }

        public void Show(MenuRoot menu)
        {
            _menu = menu;
            if (rootObject != null) rootObject.SetActive(true);
            else gameObject.SetActive(true);
            Render();
        }

        public void Hide()
        {
            _menu = null;
            if (rootObject != null) rootObject.SetActive(false);
            else gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_menu == null || _menu.Meta == null) return;
            Render();
        }

        private void Render()
        {
            var save = _menu.Meta.Save;
            int level = save.progress.currentLevel;

            if (levelText != null)
                levelText.text = $"Level {level}";

            bool boost1Unlocked = _menu.unlockConfig != null && level >= _menu.unlockConfig.boost1UnlockLevel;
            bool boost2Unlocked = _menu.unlockConfig != null && level >= _menu.unlockConfig.boost2UnlockLevel;

            if (boost1Closed != null) boost1Closed.SetActive(!boost1Unlocked);
            if (boost2Closed != null) boost2Closed.SetActive(!boost2Unlocked);

            if (boost1CountText != null)
                boost1CountText.text = save.inventory.boostGrowWholeLevel.ToString();
            if (boost2CountText != null)
                boost2CountText.text = save.inventory.boostExtraTime.ToString();

            bool inf1 = _menu.Meta.IsInfiniteBoostsActive() || _menu.Meta.IsInfiniteBoost1Active();
            bool inf2 = _menu.Meta.IsInfiniteBoostsActive() || _menu.Meta.IsInfiniteBoost2Active();

            if (boost1Infinity != null) boost1Infinity.SetActive(inf1);
            if (boost2Infinity != null) boost2Infinity.SetActive(inf2);

            if (boost1InfinityTimerText != null)
                boost1InfinityTimerText.text = inf1 ? UiTimeFormat.FormatMinutesSeconds(GetBoost1TimeLeft()) : "";
            if (boost2InfinityTimerText != null)
                boost2InfinityTimerText.text = inf2 ? UiTimeFormat.FormatMinutesSeconds(GetBoost2TimeLeft()) : "";

            if (boost1SelectedMark != null) boost1SelectedMark.SetActive(_menu.boost1Selected);
            if (boost2SelectedMark != null) boost2SelectedMark.SetActive(_menu.boost2Selected);

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

            if (startGameButton != null)
                startGameButton.interactable = _menu.Meta.CanStartGame();
        }

        private TimeSpan GetBoost1TimeLeft()
        {
            long until = Math.Max(_menu.Meta.Save.timeBonuses.infiniteBoost1UntilUtcTicks,
                                 _menu.Meta.Save.timeBonuses.infiniteBoostsUntilUtcTicks);
            DateTime now = _menu.Time != null ? _menu.Time.UtcNow : DateTime.UtcNow;
            long dt = until - now.Ticks;
            if (dt <= 0) return TimeSpan.Zero;
            return TimeSpan.FromTicks(dt);
        }

        private TimeSpan GetBoost2TimeLeft()
        {
            long until = Math.Max(_menu.Meta.Save.timeBonuses.infiniteBoost2UntilUtcTicks,
                                 _menu.Meta.Save.timeBonuses.infiniteBoostsUntilUtcTicks);
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