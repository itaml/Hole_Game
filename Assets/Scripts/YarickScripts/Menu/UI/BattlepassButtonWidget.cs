using Core.Configs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.UI
{
    public sealed class BattlepassButtonWidget : MonoBehaviour
    {
        [SerializeField] private MenuRoot menuRoot;
        [SerializeField] private UnlockConfig unlockConfig;

        [Header("Locked")]
        [SerializeField] private GameObject closedRoot;

        [Header("Progress")]
        [SerializeField] private Image progressBar;
        [SerializeField] private TMP_Text progressText; // "3/5" или "0/10"

        [SerializeField] private BattlepassWindowAnimator windowAnimator;
        [SerializeField] private Button openButton;
        [SerializeField] private GameObject openObj;

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

            bool unlocked = menuRoot.Meta.Save.progress.currentLevel >= unlockConfig.battlepassUnlockLevel;

            if (closedRoot) closedRoot.SetActive(!unlocked);
            if (openObj) openObj.SetActive(unlocked);
            if (openButton) openButton.interactable = unlocked;

            if (!unlocked)
            {
                SetProgress(0, 1);
                return;
            }

            var save = menuRoot.Meta.Save;
            var cfg = menuRoot.battlepassConfig;

            int tier = save.battlepass.tier;
            int need = GetNeedItems(cfg, tier);
            int have = Mathf.Clamp(save.battlepass.tierProgress, 0, need);

            SetProgress(have, need);
        }

        private void OnClickOpen()
        {
            if (menuRoot?.Meta == null) return;

            menuRoot.Meta.Tick();        // <-- добавь
                                         // menuRoot.Meta.SaveNow();  // если у тебя есть

            bool unlocked = menuRoot.Meta.Save.progress.currentLevel >= unlockConfig.battlepassUnlockLevel;
            if (!unlocked) return;

            windowAnimator?.Show();
        }

        private void SetProgress(int have, int need)
        {
            if (need <= 0) need = 1;

            if (progressBar)
            {
                progressBar.fillAmount = have;
            }

            if (progressText)
                progressText.text = $"{have}/{need}";
        }

        private int GetNeedItems(BattlepassConfig cfg, int tier)
        {
            if (cfg == null || cfg.tiers == null || cfg.tiers.Length == 0) return 1;
            if (tier < 0) tier = 0;
            if (tier >= cfg.tiers.Length) tier = cfg.tiers.Length - 1;
            return Mathf.Max(1, cfg.tiers[tier].needItems);
        }
    }
}