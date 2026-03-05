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

    [Header("Bounty IAP productIds")]
    public string bountySlot3ProductId = "bounty_slot3";
    public string bountySlot4ProductId = "bounty_slot4";

    [Header("Dual Battlepass")]
    public string dualBattlepassPremiumProductId = "dual_battlepass_premium";

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
        var meta = _menuRoot?.Meta;
        var save = meta?.Save;
        if (save == null)
        {
            Debug.LogError("[ShopController] No Save.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(productId) && productId == dualBattlepassPremiumProductId)
        {
            if (productId == dualBattlepassPremiumProductId)
            {
                meta.PurchaseDualBattlepassPremium();
                return;
            }
            meta.SaveNow();
            return;
        }

        // ✅ BOUNTY purchases (slot 3 / slot 4)
        if (!string.IsNullOrWhiteSpace(productId))
        {
            if (productId == bountySlot3ProductId)
            {
                // slot index 2
                meta.Bounty.EnsureInitializedOrRefreshed(); // на всякий, если купили сразу после unlock
                bool ok = meta.Bounty.TryClaimPaid(2);

                Debug.Log("buyes");
                meta.SaveNow();

                if (!ok)
                    Debug.LogWarning("[ShopController] Bounty slot3 purchase succeeded but reward wasn't claimed (locked/claimed?).");

                return;
            }

            if (productId == bountySlot4ProductId)
            {
                // slot index 3
                meta.Bounty.EnsureInitializedOrRefreshed();
                bool ok = meta.Bounty.TryClaimPaid(3);
                meta.SaveNow();

                if (!ok)
                    Debug.LogWarning("[ShopController] Bounty slot4 purchase succeeded but reward wasn't claimed (locked/claimed?).");

                return;
            }
        }

        // ---- OLD SHOP FLOW ----

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

        meta.SaveNow();

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