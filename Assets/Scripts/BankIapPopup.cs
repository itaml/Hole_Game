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
    [SerializeField] private TMP_Text capacityText;       // cap
    [SerializeField] private TMP_Text halfcapacityText;   // half
    [SerializeField] private TMP_Text textDesc;           // description

    [Header("IAP")]
    [SerializeField] private CodelessIAPButton iapButton_IAPButton;
    [SerializeField] private string productId = "com.adsyunity.p26003.bank";

    [Header("Events")]
    public UnityEvent onPurchaseSucceeded;
    public UnityEvent<string> onPurchaseFailed;

    public event Action PurchaseSucceeded;

    private bool _bound;
    private RunController _run;

    private void Awake()
    {
        if (rootObject == null) rootObject = gameObject;

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    private void OnEnable() => BindIap();
    private void OnDisable() => UnbindIap();

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Hide);

        UnbindIap();
    }

    public void Setup(RunController run)
    {
        _run = run;
        Refresh();
    }

    public void Show()
    {
        if (rootObject != null) rootObject.SetActive(true);
        else gameObject.SetActive(true);

        Refresh();
    }

    public void Hide()
    {
        if (rootObject != null) rootObject.SetActive(false);
        else gameObject.SetActive(false);
    }

    public void Refresh()
    {
        if (_run == null)
        {
            SetVisible(false);
            return;
        }

        int coins = _run.BankCoins;
        int cap = _run.BankCapacity;

        // если банк закрыт или данных нет - не показываем
        if (!_run.BankOpen || cap <= 0)
        {
            SetVisible(false);
            return;
        }

        int half = cap / 2;
        bool active = coins >= Mathf.CeilToInt(cap * 0.5f);

        SetVisible(active); // показываем ТОЛЬКО если >= половины (по твоему ТЗ)

        if (!active)
            return;

        if (capacityText) capacityText.text = cap.ToString();
        if (halfcapacityText) halfcapacityText.text = half.ToString();

        float fill = Mathf.Clamp01(coins / (float)cap);
        if (bar) bar.fillAmount = fill;

        if (iapButton_Button)
            iapButton_Button.interactable = true;

        if (textDesc)
        {
            if (coins >= cap)
                textDesc.text = "Hotel safe is full. Break it now for best deal!";
            else
                textDesc.text = "Hotel safe is ready. Break it to collect your gold!";
        }
    }

    private void SetVisible(bool visible)
    {
        if (rootObject != null) rootObject.SetActive(visible);
        else gameObject.SetActive(visible);
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

    private void OnPurchaseComplete(Product product)
    {
        if (product == null || product.definition == null) return;

        if (!string.IsNullOrEmpty(productId) && product.definition.id != productId)
            return;

        // ✅ переносим банк в coins и обнуляем банк
        _run?.ClaimBankToCoins();

        onPurchaseSucceeded?.Invoke();
        PurchaseSucceeded?.Invoke();

        Hide();
    }
}