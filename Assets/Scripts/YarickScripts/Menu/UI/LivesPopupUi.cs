using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.UI
{
    public sealed class LivesPopupUi : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject rootObject;
        [SerializeField] private TMP_Text livesCountText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private Button buyLifeButton;
        [SerializeField] private TMP_Text buyLifePriceText; // optional
        [SerializeField] private Button closeButton;

        private MenuRoot _menu;

        private void Awake()
        {
            if (buyLifeButton != null) buyLifeButton.onClick.AddListener(OnClickBuyLife);
            if (closeButton != null) closeButton.onClick.AddListener(Hide);

            if (rootObject == null)
                rootObject = gameObject;
        }

        private void OnDestroy()
        {
            if (buyLifeButton != null) buyLifeButton.onClick.RemoveListener(OnClickBuyLife);
            if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
        }

        public void Show(MenuRoot menu)
        {
            _menu = menu;

            if (buyLifePriceText != null && _menu != null && _menu.economyConfig != null)
                buyLifePriceText.text = _menu.economyConfig.buyLifeCostCoins.ToString();

            if (rootObject != null) rootObject.SetActive(true);
            else gameObject.SetActive(true);
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

            _menu.Meta.Tick();

            var save = _menu.Meta.Save;

            if (livesCountText != null)
                livesCountText.text = $"{save.lives.currentLives}/{save.lives.maxLives}";

            if (timerText != null)
            {
                if (_menu.Meta.IsInfiniteLivesActive())
                {
                    timerText.text = "∞";
                }
                else if (save.lives.currentLives >= save.lives.maxLives)
                {
                    timerText.text = "Full";
                }
                else
                {
                    var t = GetTimeToNextLife(save.lives.nextLifeReadyAtUtcTicks);
                    timerText.text = UiTimeFormat.FormatHMS(t);
                }
            }

            if (buyLifeButton != null)
            {
                int cost = (_menu.economyConfig != null) ? _menu.economyConfig.buyLifeCostCoins : int.MaxValue;

                bool canBuy = !_menu.Meta.IsInfiniteLivesActive()
                             && save.lives.currentLives < save.lives.maxLives
                             && save.wallet.coins >= cost;

                buyLifeButton.interactable = canBuy;
            }
        }

        private TimeSpan GetTimeToNextLife(long nextLifeReadyAtUtcTicks)
        {
            if (nextLifeReadyAtUtcTicks <= 0) return TimeSpan.Zero;
            DateTime now = _menu.Time != null ? _menu.Time.UtcNow : DateTime.UtcNow;
            long delta = nextLifeReadyAtUtcTicks - now.Ticks;
            if (delta <= 0) return TimeSpan.Zero;
            return TimeSpan.FromTicks(delta);
        }

        private void OnClickBuyLife()
        {
            if (_menu == null) return;
            _menu.TryBuyLife();
        }
    }
}