using System;
using Core.Configs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.UI
{
    public sealed class DualBattlepassWindowController : MonoBehaviour
    {
        [Header("Deps")]
        [SerializeField] private MenuRoot menuRoot;
        [SerializeField] private UnlockConfig unlockConfig; // добавишь dualBattlepassUnlockLevel

        [Header("Top UI - Season")]
        [SerializeField] private TMP_Text seasonTimerText;   // "10d 5h"
        [SerializeField] private TMP_Text seasonTimerText2;  // optional
        [SerializeField] private TMP_Text seasonTimerText3;  // optional

        [Header("Current Tier Progress")]
        [SerializeField] private Image currentTierProgressBar; // fill 0..1
        [SerializeField] private TMP_Text currentTierProgressText; // "3/10"
        [SerializeField] private TMP_Text currentTierLevelText;    // "Tier 2" или "2"

        [Header("Overall Tiers Progress")]
        [SerializeField] private Image tiersProgressBar;      // fill 0..1

        [Header("Rewards list (30 items)")]
        [SerializeField] private DualBattlepassRewardItemView[] items;

        [Header("Premium Button (Buy/Active)")]
        [SerializeField] private Button premiumButton;
        [SerializeField] private TMP_Text premiumButtonText; // "Buy"/"Active"

        [Header("Premium Popup")]
        [SerializeField] private GameObject premiumPopupRoot;
        [SerializeField] private PopupTween premiumPopupTween;
        [SerializeField] private Button premiumPopupCloseButton;

        [Header("Tutorial popups (Info button)")]
        [SerializeField] private StartTutorialPopupUi tutorialStep2;
        [SerializeField] private Button infoButton;

        // ВАЖНО:
        // На кнопке Buy внутри попапа ты сам повесишь Unity IAP Button,
        // и у него в OnPurchaseComplete будет IapButtonRewardReceiver -> ShopController.
        // Здесь нам buyButton не нужен — мы только открываем/закрываем попап.

        private bool _lastPremiumActive;

        private void Awake()
        {
            if (premiumButton != null) premiumButton.onClick.AddListener(OnClickPremium);

            if (premiumPopupCloseButton != null)
                premiumPopupCloseButton.onClick.AddListener(HidePremiumPopup);

            if (premiumPopupRoot != null)
                premiumPopupRoot.SetActive(false);

            if (infoButton != null)
                infoButton.onClick.AddListener(OnClickInfo);
        }

        private void OnDestroy()
        {
            if (premiumButton != null) premiumButton.onClick.RemoveListener(OnClickPremium);
            if (premiumPopupCloseButton != null) premiumPopupCloseButton.onClick.RemoveListener(HidePremiumPopup);
            if (infoButton != null)
                infoButton.onClick.RemoveListener(OnClickInfo);
        }

        private void Update()
        {
            RefreshTimerOnly();
            RefreshAll(); // у тебя так же в BattlepassWindowController
        }

        public void RefreshAll()
        {
            if (menuRoot?.Meta == null || menuRoot.dualBattlepassConfig == null || unlockConfig == null) return;

            menuRoot.Meta.Tick();

            bool unlocked = menuRoot.Meta.Save.progress.currentLevel >= unlockConfig.dualBattlepassUnlockLevel;
            if (!unlocked)
            {
                SetTimer(TimeSpan.Zero);
                SetCurrentTierProgress(0, 1, 1);
                SetOverallTierProgress(0, 30);
                SetPremiumButton(false, false);
                BuildRewardsList(locked: true);
                return;
            }

            RefreshTimerOnly();
            RefreshProgressOnly();
            BuildRewardsList(locked: false);
            RefreshPremiumStateAndPopup();
        }

        private void RefreshTimerOnly()
        {
            if (menuRoot?.Meta == null || menuRoot.dualBattlepassConfig == null) return;

            var cfg = menuRoot.dualBattlepassConfig;
            var s = menuRoot.Meta.Save.dualBattlepass;

            DateTime start = s.seasonStartUtcTicks == 0
                ? menuRoot.Time.UtcNow
                : new DateTime(s.seasonStartUtcTicks, DateTimeKind.Utc);

            DateTime end = start.AddDays(Mathf.Max(1, cfg.seasonDays));
            TimeSpan remaining = end - menuRoot.Time.UtcNow;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;

            SetTimer(remaining);
        }

        private void RefreshProgressOnly()
        {
            var cfg = menuRoot.dualBattlepassConfig;
            var save = menuRoot.Meta.Save;
            var s = save.dualBattlepass;

            var tiers = cfg.tiers;
            int tiersCount = tiers != null ? tiers.Length : 0;
            if (tiersCount <= 0)
            {
                SetCurrentTierProgress(0, 1, 1);
                SetOverallTierProgress(0, 1);
                return;
            }

            // Сколько тиров уже "закрыто" по free (freeGranted увеличивается когда тир выдан)
            int completed = Mathf.Clamp(s.freeGranted, 0, tiersCount);
            SetOverallTierProgress(completed, tiersCount);

            // Текущий тир = следующий к выдаче (если всё выдано — считаем последний)
            int currentTierIndex = Mathf.Clamp(s.freeGranted, 0, tiersCount - 1);
            int currentTierNumber = currentTierIndex + 1;

            // needWins текущего тира и prevNeedWins
            int needWins = Mathf.Max(1, tiers[currentTierIndex].needWins);
            int prevNeedWins = 0;
            if (currentTierIndex > 0)
                prevNeedWins = Mathf.Max(0, tiers[currentTierIndex - 1].needWins);

            int withinNeed = Mathf.Max(1, needWins - prevNeedWins);
            int withinHave = Mathf.Clamp(s.wins - prevNeedWins, 0, withinNeed);

            SetCurrentTierProgress(withinHave, withinNeed, currentTierNumber);
            SetPremiumButton(true, s.premiumActive);
        }

        private void RefreshPremiumStateAndPopup()
        {
            var s = menuRoot.Meta.Save.dualBattlepass;
            bool premiumActive = s.premiumActive;

            // если попап открыт и покупка произошла -> закрываем попап
            if (premiumPopupRoot != null && premiumPopupRoot.activeInHierarchy)
            {
                if (!_lastPremiumActive && premiumActive)
                    HidePremiumPopup();
            }

            _lastPremiumActive = premiumActive;
        }

        private void SetTimer(TimeSpan remaining)
        {
            int d = Mathf.Max(0, remaining.Days);
            int h = Mathf.Max(0, remaining.Hours);

            if (seasonTimerText) seasonTimerText.text = $"{d}d {h}h";
            if (seasonTimerText2) seasonTimerText2.text = $"{d}d {h}h";
            if (seasonTimerText3) seasonTimerText3.text = $"{d}d {h}h";
        }

        private void SetCurrentTierProgress(int have, int need, int tierNumber)
        {
            if (need <= 0) need = 1;

            if (currentTierProgressBar)
                currentTierProgressBar.fillAmount = Mathf.Clamp01((float)have / need);

            if (currentTierProgressText)
                currentTierProgressText.text = $"{have}/{need}";

            if (currentTierLevelText)
                currentTierLevelText.text = tierNumber.ToString(); // или $"Tier {tierNumber}"
        }

        private void SetOverallTierProgress(int completed, int total)
        {
            if (total <= 0) total = 1;

            if (tiersProgressBar)
                tiersProgressBar.fillAmount = Mathf.Clamp01((float)completed / total);
        }

        private void SetPremiumButton(bool featureUnlocked, bool premiumActive)
        {
            if (premiumButton)
                premiumButton.interactable = featureUnlocked && !premiumActive;

            if (premiumButtonText)
                premiumButtonText.text = premiumActive ? "Active" : "Buy";
        }

        private void BuildRewardsList(bool locked)
        {
            var cfg = menuRoot.dualBattlepassConfig;
            var tiers = cfg.tiers;
            if (tiers == null || tiers.Length == 0 || items == null || items.Length == 0) return;

            var save = menuRoot.Meta.Save;
            var s = save.dualBattlepass;

            bool premiumActive = s.premiumActive;

            int count = Mathf.Min(tiers.Length, items.Length);
            for (int i = 0; i < count; i++)
            {
                var view = items[i];
                if (view == null) continue;

                var tier = tiers[i];
                if (tier == null) continue;

                bool freeClaimed = !locked && (s.freeGranted > i);
                bool premiumClaimed = !locked && premiumActive && (s.premiumGranted > i);

                view.gameObject.SetActive(true);

                var (freeIcon, freeTxt) = PresentReward(tier.freeReward);
                view.SetFree(freeIcon, freeTxt, freeClaimed);

                var (premIcon, premTxt) = PresentReward(tier.premiumReward);
                view.SetPremium(premIcon, premTxt, premiumClaimed, premiumActive);
            }

            // если items больше tiers — выключим остаток
            for (int i = count; i < items.Length; i++)
                if (items[i] != null) items[i].gameObject.SetActive(false);
        }

        private void OnClickPremium()
        {
            if (menuRoot?.Meta == null) return;

            var s = menuRoot.Meta.Save.dualBattlepass;
            if (s.premiumActive) return; // уже активен

            ShowPremiumPopup();
        }

        private void ShowPremiumPopup()
        {
            if (premiumPopupRoot == null) return;

            premiumPopupRoot.SetActive(true);
            premiumPopupTween?.PlayShow();
        }

        private void HidePremiumPopup()
        {
            if (premiumPopupRoot == null) return;

            if (premiumPopupTween != null)
            {
                premiumPopupTween.PlayHide(() =>
                {
                    premiumPopupRoot.SetActive(false);
                });
            }
            else
            {
                premiumPopupRoot.SetActive(false);
            }
        }

        private void OnClickInfo()
        {
            // Показать 2 попапа обучения по кнопке info (даже если уже показывали)
            if (tutorialStep2 == null) return;
            StartCoroutine(ShowInfoTutorial());
        }

        private System.Collections.IEnumerator ShowInfoTutorial()
        {
            tutorialStep2.Show();
            while (tutorialStep2 != null && tutorialStep2.IsShown)
                yield return null;
        }

        // ===== Reward presentation (иконка + текст) =====
        private (int, string) PresentReward(Reward r)
        {
            if (r == null) return (-1, "");

            // иконки такие же как у твоего BattlepassWindowController:
            // 0 coins, 1..4 buffs, 5 boost1, 6 boost2, 7 infinite lives
            if (r.coins > 0 || r.coinsMax > 0)
            {
                int amount = r.coinsMax > 0 ? Mathf.Max(0, (r.coinsMin + r.coinsMax) / 2) : r.coins;
                return (0, $"x{amount}");
            }

            if (r.infiniteLivesMinutes > 0) return (7, FormatMinutes(r.infiniteLivesMinutes));
            if (r.infiniteBoost1Minutes > 0) return (5, FormatMinutes(r.infiniteBoost1Minutes));
            if (r.infiniteBoost2Minutes > 0) return (6, FormatMinutes(r.infiniteBoost2Minutes));

            if (r.boost1Amount > 0) return (5, $"x{r.boost1Amount}");
            if (r.boost2Amount > 0) return (6, $"x{r.boost2Amount}");

            if (r.buff1Amount > 0) return (1, $"x{r.buff1Amount}");
            if (r.buff2Amount > 0) return (2, $"x{r.buff2Amount}");
            if (r.buff3Amount > 0) return (3, $"x{r.buff3Amount}");
            if (r.buff4Amount > 0) return (4, $"x{r.buff4Amount}");

            return (-1, "");
        }

        private static string FormatMinutes(int minutes)
        {
            minutes = Mathf.Max(0, minutes);
            int d = minutes / (60 * 24);
            int h = (minutes % (60 * 24)) / 60;
            int m = minutes % 60;

            if (d > 0) return $"{d}d {h}h";
            if (h > 0) return m > 0 ? $"{h}h {m}m" : $"{h}h";
            return $"{m}m";
        }
    }
}