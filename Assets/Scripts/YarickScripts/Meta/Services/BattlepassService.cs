using System;
using Core.Configs;
using Core.Time;
using Meta.State;

namespace Meta.Services
{
    public sealed class BattlepassService
    {
        private readonly BattlepassConfig _cfg;
        private readonly ITimeProvider _time;
        private readonly WalletService _wallet;

        public BattlepassService(BattlepassConfig cfg, ITimeProvider time, WalletService wallet)
        {
            _cfg = cfg;
            _time = time;
            _wallet = wallet;
        }

        public void EnsureSeason(PlayerSave save)
        {
            if (save.battlepass.seasonStartUtcTicks == 0)
            {
                ResetSeason(save);
                return;
            }

            var start = new DateTime(save.battlepass.seasonStartUtcTicks, DateTimeKind.Utc);
            var days = (_time.UtcNow - start).TotalDays;

            if (days >= _cfg.seasonDays)
                ResetSeason(save);
        }

        private void ResetSeason(PlayerSave save)
        {
            save.battlepass.seasonStartUtcTicks = _time.UtcNow.Ticks;
            save.battlepass.tier = 0;
            save.battlepass.tierProgress = 0;
        }

        public void AddItems(PlayerSave save, int items)
        {
            if (items <= 0) return;

            EnsureSeason(save);

            save.battlepass.tierProgress += items;

            while (save.battlepass.tier < (_cfg.tiers?.Length ?? 0))
            {
                var tierCfg = _cfg.tiers[save.battlepass.tier];
                if (tierCfg.needItems <= 0) break;

                if (save.battlepass.tierProgress < tierCfg.needItems) break;

                save.battlepass.tierProgress -= tierCfg.needItems;
                GrantReward(save, tierCfg.reward);
                save.battlepass.tier++;
            }
        }

        private void GrantReward(PlayerSave save, Reward r)
        {
            if (r == null) return;

            if (r.coins > 0)
                _wallet.AddCoins(save, r.coins);

            // Infinite lives
            if (r.infiniteLivesMinutes > 0)
            {
                long until = _time.UtcNow.AddMinutes(r.infiniteLivesMinutes).Ticks;
                save.timeBonuses.infiniteLivesUntilUtcTicks =
                    System.Math.Max(save.timeBonuses.infiniteLivesUntilUtcTicks, until);
            }

            // Infinite boosts (global)
            if (r.infiniteBoostsMinutes > 0)
            {
                long until = _time.UtcNow.AddMinutes(r.infiniteBoostsMinutes).Ticks;
                save.timeBonuses.infiniteBoostsUntilUtcTicks =
                    System.Math.Max(save.timeBonuses.infiniteBoostsUntilUtcTicks, until);
            }

            // 🔥 Infinite Boost 1
            if (r.infiniteBoost1Minutes > 0)
            {
                long until = _time.UtcNow.AddMinutes(r.infiniteBoost1Minutes).Ticks;
                save.timeBonuses.infiniteBoost1UntilUtcTicks =
                    System.Math.Max(save.timeBonuses.infiniteBoost1UntilUtcTicks, until);
            }

            // 🔥 Infinite Boost 2
            if (r.infiniteBoost2Minutes > 0)
            {
                long until = _time.UtcNow.AddMinutes(r.infiniteBoost2Minutes).Ticks;
                save.timeBonuses.infiniteBoost2UntilUtcTicks =
                    System.Math.Max(save.timeBonuses.infiniteBoost2UntilUtcTicks, until);
            }

            // Boost amounts
            if (r.boost1Amount > 0)
                save.inventory.boostGrowWholeLevel += r.boost1Amount;

            if (r.boost2Amount > 0)
                save.inventory.boostExtraTime += r.boost2Amount;

            // 🔥 Buff amounts
            if (r.buff1Amount > 0)
                save.inventory.buffGrowTemp += r.buff1Amount;

            if (r.buff2Amount > 0)
                save.inventory.buffRadar += r.buff2Amount;

            if (r.buff3Amount > 0)
                save.inventory.buffMagnet += r.buff3Amount;

            if (r.buff4Amount > 0)
                save.inventory.buffFreezeTime += r.buff4Amount;
        }
    }
}
