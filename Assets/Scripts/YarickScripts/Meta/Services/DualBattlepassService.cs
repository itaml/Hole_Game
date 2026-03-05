using System;
using Core.Configs;
using Core.Time;
using Meta.State;

namespace Meta.Services
{
    public sealed class DualBattlepassService
    {
        private readonly DualBattlepassConfig _cfg;
        private readonly ITimeProvider _time;
        private readonly WalletService _wallet;

        public DualBattlepassService(DualBattlepassConfig cfg, ITimeProvider time, WalletService wallet)
        {
            _cfg = cfg;
            _time = time;
            _wallet = wallet;
        }

        public DualBattlepassState GetState(PlayerSave save) => save != null ? save.dualBattlepass : null;

        public void EnsureSeason(PlayerSave save)
        {
            if (save == null) return;

            var s = save.dualBattlepass;

            // first start
            if (s.seasonStartUtcTicks == 0)
            {
                ResetSeason(save);
                return;
            }

            int seasonDays = _cfg != null ? Math.Max(1, _cfg.seasonDays) : 14;

            var startUtc = new DateTime(s.seasonStartUtcTicks, DateTimeKind.Utc);
            var elapsedDays = (_time.UtcNow - startUtc).TotalDays;

            if (elapsedDays >= seasonDays)
                ResetSeason(save);
        }

        public void ResetSeason(PlayerSave save)
        {
            if (save == null) return;

            var s = save.dualBattlepass;

            s.seasonStartUtcTicks = _time.UtcNow.Ticks;
            s.wins = 0;
            s.freeGranted = 0;

            s.premiumActive = false;
            s.premiumGranted = 0;
        }

        /// <summary>
        /// Добавить победы (обычно 1 за win) и выдать все доступные тиры.
        /// </summary>
        public void AddWins(PlayerSave save, int addWins)
        {
            if (save == null) return;
            if (addWins <= 0) return;

            EnsureSeason(save);

            var s = save.dualBattlepass;
            s.wins = Math.Max(0, s.wins + addWins);

            GrantPending(save);
        }

        /// <summary>
        /// Активировать Premium. Довыдаст премиум-награды за уже достигнутые тиры.
        /// </summary>
        public void ActivatePremium(PlayerSave save)
        {
            if (save == null) return;

            EnsureSeason(save);

            var s = save.dualBattlepass;
            if (s.premiumActive) return;

            s.premiumActive = true;

            // Довыдать премиум за уже достигнутые тиры
            GrantPending(save);
        }

        /// <summary>
        /// Выдает все награды, доступные по текущим wins, в порядке тиров,
        /// отдельно для free и premium (как в основном батлпасе).
        /// </summary>
        public void GrantPending(PlayerSave save)
        {
            if (save == null) return;

            var tiers = _cfg != null ? _cfg.tiers : null;
            if (tiers == null || tiers.Length == 0) return;

            EnsureSeason(save);

            var s = save.dualBattlepass;

            // --- FREE ---
            while (s.freeGranted < tiers.Length)
            {
                var tier = tiers[s.freeGranted];
                if (tier == null) { s.freeGranted++; continue; } // на всякий

                if (s.wins < tier.needWins) break;

                GrantReward(save, tier.freeReward);
                s.freeGranted++;
            }

            // --- PREMIUM ---
            if (!s.premiumActive) return;

            while (s.premiumGranted < tiers.Length)
            {
                var tier = tiers[s.premiumGranted];
                if (tier == null) { s.premiumGranted++; continue; }

                if (s.wins < tier.needWins) break;

                GrantReward(save, tier.premiumReward);
                s.premiumGranted++;
            }
        }

        private void GrantReward(PlayerSave save, Reward r)
        {
            if (save == null || r == null) return;

            // Coins
            if (r.coins > 0)
                _wallet.AddCoins(save, r.coins);

            // Infinite Lives
            if (r.infiniteLivesMinutes > 0)
            {
                long until = _time.UtcNow.AddMinutes(r.infiniteLivesMinutes).Ticks;
                save.timeBonuses.infiniteLivesUntilUtcTicks =
                    Math.Max(save.timeBonuses.infiniteLivesUntilUtcTicks, until);
            }

            // Infinite Boost 1
            if (r.infiniteBoost1Minutes > 0)
            {
                long until = _time.UtcNow.AddMinutes(r.infiniteBoost1Minutes).Ticks;
                save.timeBonuses.infiniteBoost1UntilUtcTicks =
                    Math.Max(save.timeBonuses.infiniteBoost1UntilUtcTicks, until);
            }

            // Infinite Boost 2
            if (r.infiniteBoost2Minutes > 0)
            {
                long until = _time.UtcNow.AddMinutes(r.infiniteBoost2Minutes).Ticks;
                save.timeBonuses.infiniteBoost2UntilUtcTicks =
                    Math.Max(save.timeBonuses.infiniteBoost2UntilUtcTicks, until);
            }

            // Boosts inventory
            if (r.boost1Amount > 0)
                save.inventory.boostGrowWholeLevel += r.boost1Amount;

            if (r.boost2Amount > 0)
                save.inventory.boostExtraTime += r.boost2Amount;

            // Buffs inventory
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