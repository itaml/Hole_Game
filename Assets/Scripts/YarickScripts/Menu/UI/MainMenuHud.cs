using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.UI
{
    public sealed class MainMenuHud : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private MenuRoot root;

        [Header("Top")]
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text livesText;
        [SerializeField] private Button livesPlusButton;

        [Header("Chests")]
        [SerializeField] private TMP_Text levelsChestText;
        [SerializeField] private TMP_Text starsChestText;

        [Header("Bank")]
        [SerializeField] private GameObject bankClosed;
        [SerializeField] private Button bankOpenButton;
        [SerializeField] private BankPopupUi bankPopup;

        [Header("Bonus (win streak)")]
        [SerializeField] private GameObject bonusClosed;
        [SerializeField] private Image bonusBar;

        [Header("Start")]
        [SerializeField] private TMP_Text startLevelText;
        [SerializeField] private Button startButton;
        [SerializeField] private StartPopupUi startPopup;

        [Header("Popups")]
        [SerializeField] private LivesPopupUi livesPopup;

        private float _tickAccum;

        private void Reset()
        {
            root = FindFirstObjectByType<MenuRoot>();
        }

        private void OnEnable()
        {
            if (livesPlusButton != null) livesPlusButton.onClick.AddListener(OpenLivesPopup);
            if (bankOpenButton != null) bankOpenButton.onClick.AddListener(OpenBankPopup);
            if (startButton != null) startButton.onClick.AddListener(OpenStartPopup);
        }

        private void OnDisable()
        {
            if (livesPlusButton != null) livesPlusButton.onClick.RemoveListener(OpenLivesPopup);
            if (bankOpenButton != null) bankOpenButton.onClick.RemoveListener(OpenBankPopup);
            if (startButton != null) startButton.onClick.RemoveListener(OpenStartPopup);
        }

        private void Update()
        {
            if (root == null || root.Meta == null) return;

            _tickAccum += Time.unscaledDeltaTime;
            if (_tickAccum >= 1f)
            {
                _tickAccum = 0f;
                root.Meta.Tick();
            }

            Render();
        }

        private void Render()
        {
            var save = root.Meta.Save;
            int level = save.progress.currentLevel;

            if (coinsText != null)
                coinsText.text = save.wallet.coins.ToString();

            if (livesText != null)
            {
                if (root.Meta.IsInfiniteLivesActive())
                {
                    livesText.text = "∞";
                }
                else if (save.lives.currentLives >= save.lives.maxLives)
                {
                    livesText.text = "Full";
                }
                else
                {
                    var t = GetTimeToNextLife(save.lives.nextLifeReadyAtUtcTicks);
                    livesText.text = UiTimeFormat.FormatHMS(t);
                }
            }

            if (levelsChestText != null && root.levelsChestConfig != null)
            {
                int p = save.levelsChest.progress;
                int th = Mathf.Max(0, root.levelsChestConfig.threshold);
                levelsChestText.text = $"{p:00}/{th:00}";
            }

            if (starsChestText != null && root.starsChestConfig != null)
            {
                int p = save.starsChest.progress;
                int th = Mathf.Max(0, root.starsChestConfig.threshold);
                starsChestText.text = $"{p:00}/{th:00}";
            }

            bool bankUnlocked = root.unlockConfig != null && level >= root.unlockConfig.bankUnlockLevel;
            if (bankClosed != null) bankClosed.SetActive(!bankUnlocked);
            if (bankOpenButton != null) bankOpenButton.gameObject.SetActive(bankUnlocked);

            bool bonusUnlocked = root.unlockConfig != null && level >= root.unlockConfig.winStreakUnlockLevel;
            if (bonusClosed != null) bonusClosed.SetActive(!bonusUnlocked);
            if (bonusBar != null)
            {
                float fill = bonusUnlocked ? Mathf.Clamp01(save.winStreak.currentStreak / 3f) : 0f;
                bonusBar.fillAmount = fill;
            }

            if (startLevelText != null)
                startLevelText.text = $"Level {level}";
        }

        private TimeSpan GetTimeToNextLife(long nextLifeReadyAtUtcTicks)
        {
            if (nextLifeReadyAtUtcTicks <= 0) return TimeSpan.Zero;
            DateTime now = root.Time != null ? root.Time.UtcNow : DateTime.UtcNow;
            long deltaTicks = nextLifeReadyAtUtcTicks - now.Ticks;
            if (deltaTicks <= 0) return TimeSpan.Zero;
            return TimeSpan.FromTicks(deltaTicks);
        }

        private void OpenLivesPopup()
        {
            if (livesPopup == null || root == null) return;
            livesPopup.Show(root);
        }

        private void OpenBankPopup()
        {
            if (bankPopup == null || root == null) return;
            bankPopup.Show(root);
        }

        private void OpenStartPopup()
        {
            if (startPopup == null || root == null) return;
            startPopup.Show(root);
        }
    }
}