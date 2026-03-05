using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HotelSafeWidget : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RunController run;
    [SerializeField] private BankIapPopup bankPopup; // большой попап

    [Header("UI")]
    [SerializeField] private GameObject root;         // сама плашка
    [SerializeField] private Button activeButton;

    [SerializeField] private TMP_Text coinsText;      // сколько в банке сейчас (например 3000)
    [SerializeField] private TMP_Text capacityText;   // вместимость (например 6000)
    [SerializeField] private Image fillBar;           // если есть прогресс-бар (optional)

    private void Awake()
    {
        if (root == null) root = gameObject;
        if (run == null) run = FindFirstObjectByType<RunController>();

        if (activeButton != null)
            activeButton.onClick.AddListener(OnClickActive);

        Hide();
    }

    private void OnDestroy()
    {
        if (activeButton != null)
            activeButton.onClick.RemoveListener(OnClickActive);
    }

    /// <summary>Вызывай когда нужно обновить виджет (например при открытии Lose UI).</summary>
    public void Refresh()
    {
        if (run == null || run.PendingConfig == null)
        {
            Hide();
            return;
        }

        // банк должен быть открыт
        if (!run.BankOpen)
        {
            Hide();
            return;
        }

        int cap = run.BankCapacity;
        int coins = run.BankCoins;

        if (cap <= 0)
        {
            Hide();
            return;
        }

        int halfNeed = Mathf.CeilToInt(cap * 0.5f);
        bool show = coins >= halfNeed;

        if (!show)
        {
            Hide();
            return;
        }

        Show();

        if (coinsText) coinsText.text = coins.ToString();
        if (capacityText) capacityText.text = cap.ToString();

        if (fillBar) fillBar.fillAmount = Mathf.Clamp01(coins / (float)cap);

        if (activeButton) activeButton.interactable = true;
    }

    private void OnClickActive()
    {
        if (run == null) return;

        // ещё раз проверим условия (на всякий случай)
        if (!run.CanUseBank) return;

        if (bankPopup == null)
        {
            Debug.LogWarning("[HotelSafeWidget] BankIapPopup reference missing.");
            return;
        }

        bankPopup.Setup(run);
        bankPopup.Show();
    }

    private void Show()
    {
        if (root) root.SetActive(true);
        else gameObject.SetActive(true);
    }

    private void Hide()
    {
        if (root) root.SetActive(false);
        else gameObject.SetActive(false);
    }

    private void OnEnable()
{
    if (run == null) run = FindFirstObjectByType<RunController>();
    if (run != null) run.BankChanged += Refresh;
}

private void OnDisable()
{
    if (run != null) run.BankChanged -= Refresh;
}
}