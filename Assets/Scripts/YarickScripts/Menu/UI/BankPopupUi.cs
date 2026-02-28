using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.UI
{
    public sealed class BankPopupUi : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject rootObject;
        [SerializeField] private Image bar;
        [SerializeField] private Button iapButton;
        [SerializeField] private Button closeButton;

        private MenuRoot _menu;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (rootObject == null) rootObject = gameObject;
        }

        private void OnDestroy()
        {
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

            int coins = save.bank.bankCoins;
            int cap = (_menu.bankConfig != null) ? _menu.bankConfig.capacity : 0;

            float fill = 0f;
            if (cap > 0)
                fill = Mathf.Clamp01(coins / (float)cap);

            if (bar != null)
                bar.fillAmount = fill;

            if (iapButton != null)
            {
                bool active = cap > 0 && coins >= Mathf.CeilToInt(cap * 0.5f);
                iapButton.interactable = active;
            }
        }
    }
}