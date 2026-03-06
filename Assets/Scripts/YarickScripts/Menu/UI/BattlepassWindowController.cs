using System;
using Core.Configs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.UI
{
    public sealed class BattlepassWindowController : MonoBehaviour
    {
        [Header("Deps")]
        [SerializeField] private MenuRoot menuRoot;
        [SerializeField] private UnlockConfig unlockConfig;

        [Header("Top UI")]
        [SerializeField] private TMP_Text seasonTimerText; // "10d 5h"
        [SerializeField] private TMP_Text seasonTimerText2; // "10d 5h"
        [SerializeField] private Image progressBar;
        [SerializeField] private Image progressBar2;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text progressText2;// "3/5"

        [Header("Rewards list")]
        [SerializeField] private BattlepassRewardItemView[] itemPrefab;

        [Header("Overall tiers progress")]
        [SerializeField] private Image tiersProgressBar;     // fill 0..1

        [Header("Tutorial popups (Info button)")]
        [SerializeField] private StartTutorialPopupUi tutorialStep2;
        [SerializeField] private Button infoButton;

        [SerializeField] private Transform rewardIconParent;
        [SerializeField] private Transform rewardIconParent2;

        private void Awake()
        {
            if (infoButton != null)
                infoButton.onClick.AddListener(OnClickInfo);
        }

        private void OnDestroy()
        {
            if (infoButton != null)
                infoButton.onClick.RemoveListener(OnClickInfo);
        }

        private void Update()
        {
            RefreshTimerOnly();
            RefreshAll();
        }

        public void RefreshAll()
        {
            if (menuRoot?.Meta == null || unlockConfig == null) return;

            menuRoot.Meta.Tick();

            bool unlocked = menuRoot.Meta.Save.progress.currentLevel >= unlockConfig.battlepassUnlockLevel;
            if (!unlocked)
            {
                SetTimer(TimeSpan.Zero);
                SetProgress(0, 1);
                SetTiersProgress(0, 1);
                return;
            }

            RefreshTimerOnly();
            RefreshProgressOnly();
            BuildRewardsList();
        }

        private void RefreshTimerOnly()
        {
            if (menuRoot?.Meta == null) return;

            var cfg = menuRoot.battlepassConfig;
            var bp = menuRoot.Meta.Save.battlepass;
            if (cfg == null) return;

            DateTime start = bp.seasonStartUtcTicks == 0
                ? menuRoot.Time.UtcNow
                : new DateTime(bp.seasonStartUtcTicks, DateTimeKind.Utc);

            DateTime end = start.AddDays(cfg.seasonDays);
            TimeSpan remaining = end - menuRoot.Time.UtcNow;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;

            SetTimer(remaining);
        }

        private void RefreshProgressOnly()
        {
            var save = menuRoot.Meta.Save;
            var cfg = menuRoot.battlepassConfig;
            int tier = save.battlepass.tier;
            int need = GetNeedItems(cfg, tier);
            int have = Mathf.Clamp(save.battlepass.tierProgress, 0, need);

            if (tier < cfg.tiers.Length)
            {
                int iconId = GetRewardIconId(cfg.tiers[tier].reward);
                SetCurrentRewardIcon(iconId);
            }

            SetProgress(have, need);

            // ---- NEW: overall tiers progress ----
            int total = cfg != null && cfg.tiers != null ? cfg.tiers.Length : 0;
            int completed = Mathf.Clamp(tier, 0, total); // tier = сколько тиров уже получено
            SetTiersProgress(completed, total);
        }

        private void SetTiersProgress(int completed, int total)
        {
            if (total <= 0) total = 1;

            if (tiersProgressBar)
                tiersProgressBar.fillAmount = Mathf.Clamp01((float)completed / total);
        }

        private void SetTimer(TimeSpan remaining)
        {
            int d = Mathf.Max(0, remaining.Days);
            int h = Mathf.Max(0, remaining.Hours);

            if (seasonTimerText)
                seasonTimerText.text = $"{d}d {h}h";
            if (seasonTimerText2)
                seasonTimerText2.text = $"{d}d {h}h";
        }

        private void SetProgress(int have, int need)
        {
            if (need <= 0) need = 1;

            if (progressBar)
                progressBar.fillAmount = Mathf.Clamp01((float)have / need);

            if (progressBar2)
                progressBar2.fillAmount = Mathf.Clamp01((float)have / need);

            if (progressText)
                progressText.text = $"{have}/{need}";

            if (progressText2)
                progressText2.text = $"{have}/{need}";
        }

        private void BuildRewardsList()
        {
            var cfg = menuRoot.battlepassConfig;
            if (cfg == null || cfg.tiers == null || cfg.tiers.Length == 0) return;

            var save = menuRoot.Meta.Save;

            for (int i = 0; i < cfg.tiers.Length; i++)
            {
                var tierCfg = cfg.tiers[i];
                var inst = itemPrefab[i];
                inst.gameObject.SetActive(true);

                // claimed: все тира < текущего tier
                bool claimed = save.battlepass.tier > i;
                inst.SetClaimed(claimed);

                // icon + text
                var (sprId, txt) = PresentReward(tierCfg.reward);
                inst.SetIcon(sprId);
                inst.SetValue(txt);
            }
        }

        private void SetCurrentRewardIcon(int iconId)
        {
            if (rewardIconParent == null) return;

            for (int i = 0; i < rewardIconParent.childCount; i++)
            {
                rewardIconParent.GetChild(i).gameObject.SetActive(i == iconId);
                rewardIconParent2.GetChild(i).gameObject.SetActive(i == iconId);
            }
        }

        private int GetRewardIconId(Reward r)
        {
            if (r == null) return -1;

            if (r.coins > 0 || r.coinsMax > 0)
                return 0;

            if (r.infiniteLivesMinutes > 0)
                return 7;

            if (r.infiniteBoost1Minutes > 0)
                return 5;

            if (r.infiniteBoost2Minutes > 0)
                return 6;

            if (r.boost1Amount > 0)
                return 5;

            if (r.boost2Amount > 0)
                return 6;

            if (r.buff1Amount > 0)
                return 1;

            if (r.buff2Amount > 0)
                return 2;

            if (r.buff3Amount > 0)
                return 3;

            if (r.buff4Amount > 0)
                return 4;

            return -1;
        }

        private (int, string) PresentReward(Reward r)
        {
            if (r == null) return (-1, "");

            if (r.coins > 0 || r.coinsMax > 0)
            {
                int amount = r.coinsMax > 0 ? Mathf.Max(0, (r.coinsMin + r.coinsMax) / 2) : r.coins;
                return (0, $"x{amount}");
            }

            if (r.infiniteLivesMinutes > 0)
                return (7, FormatMinutes(r.infiniteLivesMinutes));

            if (r.infiniteBoost1Minutes > 0)
                return (5, FormatMinutes(r.infiniteBoost1Minutes));

            if (r.infiniteBoost2Minutes > 0)
                return (6, FormatMinutes(r.infiniteBoost2Minutes));

            if (r.boost1Amount > 0)
                return (5, $"x{r.boost1Amount}");

            if (r.boost2Amount > 0)
                return (6, $"x{r.boost2Amount}");

            if (r.buff1Amount > 0)
                return (1, $"x{r.buff1Amount}");

            if (r.buff2Amount > 0)
                return (2, $"x{r.buff2Amount}");

            if (r.buff3Amount > 0)
                return (3, $"x{r.buff3Amount}");

            if (r.buff4Amount > 0)
                return (4, $"x{r.buff4Amount}");

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

        private int GetNeedItems(BattlepassConfig cfg, int tier)
        {
            if (cfg == null || cfg.tiers == null || cfg.tiers.Length == 0) return 1;
            if (tier < 0) tier = 0;
            if (tier >= cfg.tiers.Length) tier = cfg.tiers.Length - 1;
            return Mathf.Max(1, cfg.tiers[tier].needItems);
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
    }
}