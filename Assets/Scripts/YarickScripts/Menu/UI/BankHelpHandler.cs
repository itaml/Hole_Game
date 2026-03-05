using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace Menu.UI
{
    public sealed class BankHelpHandler : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private BankPopupUi popupUi;
        [SerializeField] private CodelessIAPButton iapButton;

        private MenuRoot _menu;

        private void Awake()
        {
            if (popupUi == null)
                popupUi = GetComponent<BankPopupUi>();
        }

        private void OnEnable()
        {
            if (iapButton != null)
            {
                iapButton.onPurchaseComplete.AddListener(OnPurchaseComplete);
            }
        }

        private void OnDisable()
        {
            if (iapButton != null)
            {
                iapButton.onPurchaseComplete.RemoveListener(OnPurchaseComplete);
            }
        }

        /// <summary>
        /// ВАЖНО: вызови это из BankPopupUi.Show(menu) или где тебе удобно,
        /// чтобы handler знал какой MenuRoot сейчас активен.
        /// </summary>
        public void Bind(MenuRoot menu)
        {
            _menu = menu;
        }

        private void OnPurchaseComplete(Product product)
        {
            if (_menu == null || _menu.Meta == null) return;

            // На всякий случай проверим id продукта:
            if (product == null || product.definition == null) return;
            if (product.definition.id != "com.adsyunity.p26003.bank") return;

            var save = _menu.Meta.Save;
            int amount = Mathf.Max(0, save.bank.bankCoins);
            if (amount <= 0) return;

            // 1) добавить в wallet
            // ВАЖНО: замени на свой реальный способ начисления монет в кошелёк.
            // Например: save.wallet.coins += amount; или _menu.Meta.Wallet.Add(amount)
            save.wallet.coins += amount;

            // 2) обнулить банк
            save.bank.bankCoins = 0;

            // 4) закрыть попап (по желанию)
            popupUi?.Hide();
        }
    }
}