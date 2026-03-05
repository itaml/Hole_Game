using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Purchasing;
using UnityEngine.UI;

public sealed class BankIapPopup : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject rootObject;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button iapButton_Button;

    [SerializeField] private Image bar;
    [SerializeField] private TMP_Text capacityText;
    [SerializeField] private TMP_Text halfcapacityText;
    [SerializeField] private TMP_Text textDesc;

    [Header("IAP")]
    [SerializeField] private CodelessIAPButton iapButton_IAPButton;
    [SerializeField] private string productId = "com.adsyunity.p26003.bank";

    [Header("Events")]
    public UnityEvent onPurchaseSucceeded;
    public UnityEvent<string> onPurchaseFailed; // reason string

    // Опционально: чтобы можно было из кода сделать onSuccess += ...
    public event Action PurchaseSucceeded;

    private bool _bound;

    private void Awake()
    {
        if (rootObject == null) rootObject = gameObject;

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    private void Update()
    {
        Render();
    }

    void Render()
    {
        //int coins = save.bank.bankCoins; // Тут надо подставить вместо save.bank.bankCoins просто bankCoins из твоего локального кошелька где ты в начале спарсил _cfg.bankCoinsSnapshot
        //int cap = bankConfig.capacity : 0; // Тут надо подставить вместо bankConfig.capacity просто bankCapacity из твоего локального кошелька где ты в начале спарсил _cfg.bankCapacitySnapshot

        //int half = cap / 2;

        //capacityText.text = cap.ToString();
        //halfcapacityText.text = half.ToString();

        //if (coins == _menu.bankConfig.capacity) text.text = "Hotel safe is full. Break it now for best deal!";
        //else if (coins >= half) text.text = "Hotel safe to collect golds or save more gold for best deal";
        //else text.text = "Add at least " + half.ToString() + " gold to the Hotel safe to buy it at a great deal";

        float fill = 0f;
        //if (cap > 0)
            //fill = Mathf.Clamp01(coins / (float)cap);

        if (bar != null)
            bar.fillAmount = fill;

        if (iapButton_Button != null)
        {
            //bool active = cap > 0 && coins >= Mathf.CeilToInt(cap * 0.5f);
            //iapButton_Button.interactable = active;
        }
    }

    private void OnEnable()
    {
        BindIap();
    }

    private void OnDisable()
    {
        UnbindIap();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Hide);

        UnbindIap();
    }

    private void BindIap()
    {
        if (_bound) return;
        if (iapButton_IAPButton == null) return;

        iapButton_IAPButton.onPurchaseComplete.AddListener(OnPurchaseComplete);
        _bound = true;
    }

    private void UnbindIap()
    {
        if (!_bound) return;
        if (iapButton_IAPButton == null) return;

        iapButton_IAPButton.onPurchaseComplete.RemoveListener(OnPurchaseComplete);
        _bound = false;
    }

    public void Show()
    {
        if (rootObject != null) rootObject.SetActive(true);
        else gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (rootObject != null) rootObject.SetActive(false);
        else gameObject.SetActive(false);
    }

    private void OnPurchaseComplete(Product product)
    {
        if (product == null || product.definition == null) return;

        // защита: реагируем только на нужный продукт
        if (!string.IsNullOrEmpty(productId) && product.definition.id != productId)
            return;

        // ТУТ твоя логика (или ты в инспекторе повесишь UnityEvent)
        onPurchaseSucceeded?.Invoke();
        PurchaseSucceeded?.Invoke();

        // по желанию: закрывать сразу
        Hide();
    }
}