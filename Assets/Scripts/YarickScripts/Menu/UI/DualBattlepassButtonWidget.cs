using Core.Configs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.UI
{
    public sealed class DualBattlepassButtonWidget : MonoBehaviour
    {
        [SerializeField] private MenuRoot menuRoot;
        [SerializeField] private UnlockConfig unlockConfig;

        [Header("Locked")]
        [SerializeField] private GameObject closedRoot;

        [Header("Open")]
        [SerializeField] private DualBattlepassWindowAnimator windowAnimator;
        [SerializeField] private Button openButton;

        private void Awake()
        {
            if (openButton != null)
                openButton.onClick.AddListener(OnClickOpen);
        }

        private void OnEnable() => Refresh();

        public void Refresh()
        {
            if (menuRoot?.Meta == null || unlockConfig == null) return;

            menuRoot.Meta.Tick(); // чтобы сезон/таймеры актуализировались

            bool unlocked = menuRoot.Meta.Save.progress.currentLevel >= unlockConfig.dualBattlepassUnlockLevel;

            if (closedRoot) closedRoot.SetActive(!unlocked);
            if (openButton) openButton.interactable = unlocked;

            var save = menuRoot.Meta.Save;
            var cfg = menuRoot.dualBattlepassConfig;

            // вычисляем текущий тир по freeGranted (как в сервисе)
            int tierIndex = GetCurrentTierIndex(cfg, save.dualBattlepass.freeGranted);

            int prevNeedWins = GetPrevNeedWins(cfg, tierIndex);
            int needWins = GetNeedWins(cfg, tierIndex);

            // внутри тира:
            int needWithin = Mathf.Max(1, needWins - prevNeedWins);
            int haveWithin = Mathf.Clamp(save.dualBattlepass.wins - prevNeedWins, 0, needWithin);
        }

        public void OnClickOpen()
        {
            if (menuRoot?.Meta == null || unlockConfig == null) return;

            menuRoot.Meta.Tick();

            bool unlocked = menuRoot.Meta.Save.progress.currentLevel >= unlockConfig.dualBattlepassUnlockLevel;
            if (!unlocked) return;

            windowAnimator?.Show();
        }

        private int GetCurrentTierIndex(DualBattlepassConfig cfg, int freeGranted)
        {
            int count = cfg != null && cfg.tiers != null ? cfg.tiers.Length : 0;
            if (count <= 0) return 0;

            int idx = Mathf.Clamp(freeGranted, 0, count - 1);
            return idx;
        }

        private int GetNeedWins(DualBattlepassConfig cfg, int tierIndex)
        {
            if (cfg == null || cfg.tiers == null || cfg.tiers.Length == 0) return 1;

            tierIndex = Mathf.Clamp(tierIndex, 0, cfg.tiers.Length - 1);
            return Mathf.Max(1, cfg.tiers[tierIndex].needWins);
        }

        private int GetPrevNeedWins(DualBattlepassConfig cfg, int tierIndex)
        {
            if (cfg == null || cfg.tiers == null || cfg.tiers.Length == 0) return 0;

            tierIndex = Mathf.Clamp(tierIndex, 0, cfg.tiers.Length - 1);
            if (tierIndex == 0) return 0;

            return Mathf.Max(0, cfg.tiers[tierIndex - 1].needWins);
        }
    }
}