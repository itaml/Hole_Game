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
        [SerializeField] private TMP_Text capacityText;
        [SerializeField] private TMP_Text halfcapacityText;
        [SerializeField] private TMP_Text text;

        [SerializeField] private PopupTween tween; // добавить

        private MenuRoot _menu;


        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (rootObject == null) rootObject = gameObject;

            if (tween == null) tween = GetComponent<PopupTween>(); // добавить
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

            tween?.PlayShow(); // добавить

            Render();
        }

        public void Hide()
        {
            _menu = null;

            if (tween != null)
            {
                tween.PlayHide(() =>
                {
                    if (rootObject != null) rootObject.SetActive(false);
                    else gameObject.SetActive(false);
                });
            }
            else
            {
                if (rootObject != null) rootObject.SetActive(false);
                else gameObject.SetActive(false);
            }
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

            int half = _menu.bankConfig.capacity / 2;

            capacityText.text = _menu.bankConfig.capacity.ToString();
            halfcapacityText.text = half.ToString();

            if (coins == _menu.bankConfig.capacity) text.text = "Hotel safe is full. Break it now for best deal!";
            else if (coins >= half) text.text = "Hotel safe to collect golds or save more gold for best deal";
            else text.text = "Add at least " + half.ToString() + " gold to the Hotel safe to buy it at a great deal";

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