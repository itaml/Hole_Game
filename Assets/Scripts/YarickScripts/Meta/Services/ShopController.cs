using Menu.UI;
using Meta.State;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ShopController : MonoBehaviour
{
    [SerializeField] private List<ShopProduct> products = new();
    [SerializeField] private MenuRoot _menuRoot;
    [SerializeField] private HideIfAdsRemoved hide;

    private Dictionary<string, ShopProduct> _map;

    private void Awake()
    {
        _map = new Dictionary<string, ShopProduct>(products.Count);
        foreach (var p in products)
        {
            if (!string.IsNullOrWhiteSpace(p.productId))
                _map[p.productId] = p;
        }
    }

    public void OnPurchaseSucceeded(string productId)
    {
        var save = _menuRoot?.Meta?.Save;
        if (save == null)
        {
            Debug.LogError("[ShopController] No Save.");
            return;
        }

        if (!_map.TryGetValue(productId, out var product))
        {
            Debug.LogError($"[ShopController] Unknown productId: {productId}");
            return;
        }

        // Если это remove ads и уже куплено — просто игнор (на всякий)
        if (product.removesAds && save.profile.adsRemoved)
        {
            Debug.Log("[ShopController] Ads already removed, ignoring duplicate grant.");
            return;
        }

        // 1) начисляем награду (coins/buffs/boosts/infinite life etc.)
        ApplyReward(save, product.reward);

        // 2) special: remove ads
        if (product.removesAds)
            save.profile.adsRemoved = true;

        _menuRoot.Meta.SaveNow();

        hide.Refresh();
    }

    private void ApplyReward(PlayerSave save, ShopReward r)
    {
        if (r.coins != 0) save.wallet.coins += r.coins;

        if (r.buff1 != 0) save.inventory.buffGrowTemp += r.buff1;
        if (r.buff2 != 0) save.inventory.buffRadar += r.buff2;
        if (r.buff3 != 0) save.inventory.buffMagnet += r.buff3;
        if (r.buff4 != 0) save.inventory.buffFreezeTime += r.buff4;

        if (r.boost1 != 0) save.inventory.boostGrowWholeLevel += r.boost1;
        if (r.boost2 != 0) save.inventory.boostExtraTime += r.boost2;

        if (r.infiniteLivesHours > 0)
            AddHoursToUtcTicks(ref save.timeBonuses.infiniteLivesUntilUtcTicks, r.infiniteLivesHours);
    }

    private static void AddHoursToUtcTicks(ref long untilUtcTicks, int hoursToAdd)
    {
        long now = DateTime.UtcNow.Ticks;
        long add = TimeSpan.FromHours(hoursToAdd).Ticks;
        long baseTicks = untilUtcTicks > now ? untilUtcTicks : now;
        untilUtcTicks = baseTicks + add;
    }
}