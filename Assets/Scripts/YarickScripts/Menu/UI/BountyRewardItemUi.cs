using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.UI.Bounty
{
    public sealed class BountyRewardItemUi : MonoBehaviour
    {
        [SerializeField] private Transform rewardParent;
        [SerializeField] private TMP_Text rewardText;

        [SerializeField] private Button freeButton;
        [SerializeField] private Button buyButton;
        [SerializeField] private TMP_Text buyButtonText;

        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private GameObject claimedOverlay;

        private int _index;
        private System.Action<int> _onFree;
        private System.Action<int> _onBuy;

        public void Init(int index, System.Action<int> onFree, System.Action<int> onBuy)
        {
            _index = index;
            _onFree = onFree;
            _onBuy = onBuy;

            if (freeButton != null)
            {
                freeButton.onClick.RemoveAllListeners();
                freeButton.onClick.AddListener(() => _onFree?.Invoke(_index));
            }

            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(() => _onBuy?.Invoke(_index));
            }
        }

        public void SetContent(int i, string text)
        {
            if (rewardParent != null) rewardParent.transform.GetChild(i).gameObject.SetActive(true);
            if (rewardText != null) rewardText.text = text;
        }

        public void SetBuyText(string priceText)
        {
            if (buyButtonText != null) buyButtonText.text = priceText;
        }

        public void SetState(bool locked, bool claimed, bool paidSlot)
        {
            if (lockedOverlay != null) lockedOverlay.SetActive(locked && !claimed);
            if (claimedOverlay != null) claimedOverlay.SetActive(claimed);

            if (freeButton != null)
            {
                freeButton.gameObject.SetActive(!paidSlot);
                freeButton.interactable = !locked && !claimed;
            }

            if (buyButton != null)
            {
                buyButton.gameObject.SetActive(paidSlot);
                buyButton.interactable = !locked && !claimed;
            }
        }
    }
}