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

                var template = PickReward(cfg);

                // ВЫДАЕМ и получаем снапшот того, что реально начислили
                var granted = GrantReward(save, template);

                if (granted != null)
                    grantedRewards?.Add(granted);
            }

            if (isStars) save.starsChest.progress = progress;
            else save.levelsChest.progress = progress;
        }

        private Reward PickReward(ChestConfig cfg)
        {
            if (cfg.possibleRewards == null || cfg.possibleRewards.Length == 0)
                return new Reward();

            int totalWeight = 0;
            for (int i = 0; i < cfg.possibleRewards.Length; i++)
                totalWeight += Mathf.Max(0, cfg.possibleRewards[i].weight);

            // Если веса не заданы или все 0 — fallback на старый равномерный рандом
            if (totalWeight <= 0)
            {
                int idx = Random.Range(0, cfg.possibleRewards.Length);
                return cfg.possibleRewards[idx];
            }

            int roll = Random.Range(0, totalWeight);
            int acc = 0;

            for (int i = 0; i < cfg.possibleRewards.Length; i++)
            {
                acc += Mathf.Max(0, cfg.possibleRewards[i].weight);
                if (roll < acc)
                    return cfg.possibleRewards[i];
            }

            return cfg.possibleRewards[cfg.possibleRewards.Length - 1];
        }

        private Reward GrantReward(PlayerSave save, Reward r)
        {
            if (r == null) return null;

            // СНАПШОТ ДЛЯ UI (важно для coinsMin/coinsMax)
            var granted = new Reward
            {
                coins = 0,
                coinsMin = 0,
                coinsMax = 0,

                infiniteLivesMinutes = r.infiniteLivesMinutes,
                infiniteBoost1Minutes = r.infiniteBoost1Minutes,
                infiniteBoost2Minutes = r.infiniteBoost2Minutes,

                boost1Amount = r.boost1Amount,
                boost2Amount = r.boost2Amount,

                buff1Amount = r.buff1Amount,
                buff2Amount = r.buff2Amount,
                buff3Amount = r.buff3Amount,
                buff4Amount = r.buff4Amount,
            };

            int coins = r.GetCoins();
            if (coins != 0)
            {
                _wallet.AddCoins(save, coins);
                granted.coins = coins; // ВОТ ТУТ фикс для UI
            }

            if (r.infiniteLivesMinutes > 0)
            {
                long until = _time.UtcNow.AddMinutes(r.infiniteLivesMinutes).Ticks;
                save.timeBonuses.infiniteLivesUntilUtcTicks =
                    System.Math.Max(save.timeBonuses.infiniteLivesUntilUtcTicks, until);
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

            return granted;
        }
    }
}