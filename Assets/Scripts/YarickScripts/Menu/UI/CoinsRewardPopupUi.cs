using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.UI
{
    public sealed class CoinsRewardPopupUi : MonoBehaviour
    {
        [SerializeField] private GameObject rootObject;
        [SerializeField] private TMP_Text amountText; // "+100"
        [SerializeField] private Button closeButton;

        private RewardPopupQueue _queue;

        private void Awake()
        {
            if (rootObject == null) rootObject = gameObject;
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        public void Init(RewardPopupQueue queue) => _queue = queue;

        public void Show(int coins)
        {
            if (amountText != null) amountText.text = $"+{coins}";
            rootObject.SetActive(true);
        }

        private void Close()
        {
            rootObject.SetActive(false);
            _queue?.NotifyClosed();
        }
    }
}