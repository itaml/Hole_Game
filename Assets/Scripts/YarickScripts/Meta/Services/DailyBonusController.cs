using Menu.UI;
using Meta.State;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DailyBonusController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button claimButton;
    [SerializeField] private GameObject timerRoot; // контейнер с таймером (иконка+текст)
    [SerializeField] private TMP_Text timerText;

    [Header("Reward")]
    [SerializeField] private int freeCoins = 200;

    [Header("Cooldown")]
    [SerializeField] private int cooldownHours = 24;

    [Header("Meta")]
    [SerializeField] private MenuRoot _menuRoot;

    private void Awake()
    {
        if (claimButton != null)
            claimButton.onClick.AddListener(Claim);
    }

    private void OnEnable()
    {
        RefreshUI();
    }

    private void Update()
    {
        var save = GetSave();
        if (save == null) return;

        if (!IsAvailable(save))
        {
            var remain = GetRemaining(save);

            if (timerText != null)
                timerText.text = FormatRemaining(remain);

            if (remain <= TimeSpan.Zero)
                RefreshUI();
        }
    }

    private void Claim()
    {
        var save = GetSave();
        if (save == null) return;

        if (!IsAvailable(save))
            return;

        save.wallet.coins += freeCoins;
        save.tutorial.dailyBonusLastClaimUtcTicks = DateTime.UtcNow.Ticks;

        _menuRoot.Meta.SaveNow();
        RefreshUI();
    }

    private PlayerSave GetSave()
    {
        if (_menuRoot == null || _menuRoot.Meta == null || _menuRoot.Meta.Save == null)
            return null;

        return _menuRoot.Meta.Save;
    }

    private bool IsAvailable(PlayerSave save)
    {
        return GetRemaining(save) <= TimeSpan.Zero;
    }

    private TimeSpan GetRemaining(PlayerSave save)
    {
        long lastTicks = save.tutorial.dailyBonusLastClaimUtcTicks;
        if (lastTicks <= 0) return TimeSpan.Zero;

        var lastUtc = new DateTime(lastTicks, DateTimeKind.Utc);
        var nextUtc = lastUtc + TimeSpan.FromHours(cooldownHours);
        return nextUtc - DateTime.UtcNow;
    }

    private static string FormatRemaining(TimeSpan t)
    {
        if (t <= TimeSpan.Zero) return "0h 0m";

        int hours = (int)Math.Floor(t.TotalHours);
        int minutes = t.Minutes;
        return $"{hours}h {minutes}m";
    }

    private void RefreshUI()
    {
        var save = GetSave();
        if (save == null) return;

        bool available = IsAvailable(save);

        if (claimButton != null) claimButton.gameObject.SetActive(available);
        if (timerRoot != null) timerRoot.SetActive(!available);

        if (!available && timerText != null)
            timerText.text = FormatRemaining(GetRemaining(save));
    }

    private void OnDestroy()
    {
        if (claimButton != null)
            claimButton.onClick.RemoveListener(Claim);
    }
}