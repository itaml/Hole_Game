using System;
using Core.Configs;
using Core.Time;
using Meta.State;

namespace Meta.Services
{
    public sealed class LivesService
    {
        private readonly EconomyConfig _eco;
        private readonly ITimeProvider _time;

        public LivesService(EconomyConfig eco, ITimeProvider time)
        {
            _eco = eco;
            _time = time;
        }

        public bool IsInfiniteLivesActive(PlayerSave save)
            => save.timeBonuses.infiniteLivesUntilUtcTicks > _time.UtcNow.Ticks;

        public bool CanStartGame(PlayerSave save)
            => IsInfiniteLivesActive(save) || save.lives.currentLives > 0;

        public void TickRegen(PlayerSave save)
        {
            if (save.lives.currentLives >= save.lives.maxLives)
            {
                save.lives.nextLifeReadyAtUtcTicks = 0;
                return;
            }

            if (save.lives.nextLifeReadyAtUtcTicks == 0)
            {
                save.lives.nextLifeReadyAtUtcTicks = _time.UtcNow.AddSeconds(_eco.lifeRestoreSeconds).Ticks;
                return;
            }

            long step = TimeSpan.FromSeconds(_eco.lifeRestoreSeconds).Ticks;

            while (save.lives.currentLives < save.lives.maxLives &&
                   _time.UtcNow.Ticks >= save.lives.nextLifeReadyAtUtcTicks)
            {
                save.lives.currentLives++;

                if (save.lives.currentLives < save.lives.maxLives)
                    save.lives.nextLifeReadyAtUtcTicks += step;
                else
                    save.lives.nextLifeReadyAtUtcTicks = 0;
            }
        }

        public void ConsumeLifeOnLose(PlayerSave save)
        {
            if (IsInfiniteLivesActive(save)) return;

            if (save.lives.currentLives > 0)
                save.lives.currentLives--;

            if (save.lives.currentLives < save.lives.maxLives && save.lives.nextLifeReadyAtUtcTicks == 0)
                save.lives.nextLifeReadyAtUtcTicks = _time.UtcNow.AddSeconds(_eco.lifeRestoreSeconds).Ticks;
        }

        public bool BuyLife(PlayerSave save, WalletService wallet)
        {
            if (save.lives.currentLives >= save.lives.maxLives) return false;
            if (!wallet.TrySpend(save, _eco.buyLifeCostCoins)) return false;

            save.lives.currentLives++;
            if (save.lives.currentLives >= save.lives.maxLives)
                save.lives.nextLifeReadyAtUtcTicks = 0;

            return true;
        }
    }
}
