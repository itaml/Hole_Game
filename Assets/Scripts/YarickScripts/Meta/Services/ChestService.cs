using Core.Configs;
using Core.Time;
using Meta.State;
using System.Collections.Generic;
using UnityEngine;

namespace Meta.Services
{
    public sealed class ChestService
    {
        private readonly ChestConfig _starsCfg;
        private readonly ChestConfig _levelsCfg;
        private readonly WalletService _wallet;
        private readonly ITimeProvider _time;

        public ChestService(ChestConfig starsCfg, ChestConfig levelsCfg, WalletService wallet, ITimeProvider time)
        {
            _starsCfg = starsCfg;
            _levelsCfg = levelsCfg;
            _wallet = wallet;
            _time = time;
        }

        public void AddStarsAndOpenIfReady(PlayerSave save, int starsEarned, List<Reward> grantedRewards)
        {
            if (starsEarned <= 0) return;
            save.starsChest.progress += starsEarned;
            OpenLoop(save, isStars: true, grantedRewards);
        }

        public void AddLevelWinAndOpenIfReady(PlayerSave save, List<Reward> grantedRewards)
        {
            save.levelsChest.progress += 1;
            OpenLoop(save, isStars: false, grantedRewards);
        }

        private void OpenLoop(PlayerSave save, bool isStars, List<Reward> grantedRewards)
        {
            ChestConfig cfg = isStars ? _starsCfg : _levelsCfg;

            int progress = isStars ? save.starsChest.progress : save.levelsChest.progress;

            while (cfg.threshold > 0 && progress >= cfg.threshold)
            {
                progress -= cfg.threshold;

                var reward = PickReward(cfg);

                // 🔥 ВОТ ТУТ МОМЕНТ ВЫДАЧИ НАГРАДЫ: сохраняем reward для UI
                if (reward != null)
                    grantedRewards?.Add(reward);

                GrantReward(save, reward);
            }

            if (isStars) save.starsChest.progress = progress;
            else save.levelsChest.progress = progress;
        }

        private Reward PickReward(ChestConfig cfg)
        {
            if (cfg.possibleRewards == null || cfg.possibleRewards.Length == 0) return new Reward();
            int idx = Random.Range(0, cfg.possibleRewards.Length);
            return cfg.possibleRewards[idx];
        }

        private void GrantReward(PlayerSave save, Reward r)
        {
            if (r == null) return;

            if (r.coins > 0)
                _wallet.AddCoins(save, r.coins);

            if (r.infiniteLivesMinutes > 0)
            {
                long until = _time.UtcNow.AddMinutes(r.infiniteLivesMinutes).Ticks;
                save.timeBonuses.infiniteLivesUntilUtcTicks =
                    System.Math.Max(save.timeBonuses.infiniteLivesUntilUtcTicks, until);
            }

            if (r.infiniteBoostsMinutes > 0)
            {
                long until = _time.UtcNow.AddMinutes(r.infiniteBoostsMinutes).Ticks;
                save.timeBonuses.infiniteBoostsUntilUtcTicks =
                    System.Math.Max(save.timeBonuses.infiniteBoostsUntilUtcTicks, until);
            }

            if (r.infiniteBoost1Minutes > 0)
            {
                long until = _time.UtcNow.AddMinutes(r.infiniteBoost1Minutes).Ticks;
                save.timeBonuses.infiniteBoost1UntilUtcTicks =
                    System.Math.Max(save.timeBonuses.infiniteBoost1UntilUtcTicks, until);
            }

            if (r.infiniteBoost2Minutes > 0)
            {
                long until = _time.UtcNow.AddMinutes(r.infiniteBoost2Minutes).Ticks;
                save.timeBonuses.infiniteBoost2UntilUtcTicks =
                    System.Math.Max(save.timeBonuses.infiniteBoost2UntilUtcTicks, until);
            }

            if (r.boost1Amount > 0)
                save.inventory.boostGrowWholeLevel += r.boost1Amount;

            if (r.boost2Amount > 0)
                save.inventory.boostExtraTime += r.boost2Amount;

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