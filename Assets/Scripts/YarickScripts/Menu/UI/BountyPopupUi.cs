using Core.Configs;
using Menu.UI.Bounty;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.UI
{
    public sealed class BountyPopupUi : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Button closeButton;

        [SerializeField] private BountyRewardItemUi[] items = new BountyRewardItemUi[6];

        [SerializeField] private string slot3PriceText = "$0.99";
        [SerializeField] private string slot4PriceText = "$0.99";

        [SerializeField] private PopupTween tween;

        private MenuRoot _root;

        private void Awake()
        {
            if (root == null) root = gameObject;
            if (closeButton != null) closeButton.onClick.AddListener(Hide);

            for (int i = 0; i < items.Length; i++)
                if (items[i] != null) items[i].Init(i, OnFreeClicked, OnBuyClicked);

            root.SetActive(false);
        }

        private void OnBountyChanged()
        {
            if (root.activeInHierarchy)
                Render();
        }

        public void Show(MenuRoot menuRoot)
        {
            _root = menuRoot;

            // На всякий случай обновим/инициализируем (Tick уже делает, но это безопасно)
            if (_root.Meta != null)
                _root.Meta.Bounty.EnsureInitializedOrRefreshed();

            Render();
            root.SetActive(true); // Popup Tween отработает сам
            tween?.PlayShow(); // добавить

            _root.Meta.Bounty.Changed += OnBountyChanged;
        }

        public void Hide()
        {
            if (tween != null)
            {
                tween.PlayHide(() =>
                {
                    if (root != null) root.SetActive(false);
                    else gameObject.SetActive(false);
                });
            }
            else
            {
                if (root != null) root.SetActive(false);
                else gameObject.SetActive(false);
            }

            _root.Meta.Bounty.Changed -= OnBountyChanged;
        }

        private void Render()
        {
            var bounty = _root.Meta.Bounty;
            var s = bounty.State;

            for (int i = 0; i < 6; i++)
            {
                Reward r = s.slots[i];

                bool paid = bounty.IsPaidSlot(i);
                bool claimed = bounty.IsClaimed(i);
                bool locked = !bounty.CanClaim(i);

                items[i].SetContent(GetIcon(r), ToText(r));
                if (i == 2) items[i].SetBuyText(slot3PriceText);
                if (i == 3) items[i].SetBuyText(slot4PriceText);

                items[i].SetState(locked, claimed, paid);
            }
        }

        private void OnFreeClicked(int index)
        {
            if (_root.Meta.Bounty.TryClaimFree(index))
            {
                _root.Meta.SaveNow();
                Render();
            }
        }

        private void OnBuyClicked(int index)
        {
            // Покупку инициируй Unity IAP Button на buyButton слота 3/4.
            // Выдача награды будет в OnPurchaseSucceeded -> TryClaimPaid(index).
            Debug.Log($"Bounty buy clicked slot {index}. Use IAP Button.");
        }

        private int GetIcon(Reward r)
        {
            bool hasCoins = r.coins > 0 || (r.coinsMax > 0 && r.coinsMax >= r.coinsMin);
            if (hasCoins) return 0;

            if (r.boost1Amount > 0) return 1;
            if (r.boost2Amount > 0) return 2;

            if (r.buff1Amount > 0) return 3;
            if (r.buff2Amount > 0) return 4;
            if (r.buff3Amount > 0) return 5;
            if (r.buff4Amount > 0) return 6;

            return -1;
        }

        private string ToText(Reward r)
        {
            int coins = r.GetCoins();
            bool hasCoins = coins > 0;

            if (hasCoins) return $"+{coins}";

            if (r.boost1Amount > 0) return $"x{r.boost1Amount}";
            if (r.boost2Amount > 0) return $"x{r.boost2Amount}";

            if (r.buff1Amount > 0) return $"x{r.buff1Amount}";
            if (r.buff2Amount > 0) return $"x{r.buff2Amount}";
            if (r.buff3Amount > 0) return $"x{r.buff3Amount}";
            if (r.buff4Amount > 0) return $"x{r.buff4Amount}";

            return "x1";
        }
    }
}