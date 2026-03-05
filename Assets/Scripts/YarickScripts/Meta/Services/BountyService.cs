using System;
using Core.Configs;
using Core.Save;
using Core.Time;
using Meta.State;
using UnityEngine;

namespace Meta.Services
{
    public sealed class BountyService
    {
        private readonly BountyConfig _config;
        private readonly SaveSystem _saveSystem;
        private readonly ITimeProvider _time;

        public BountyState State => _saveSystem.Current.bounty;

        public event Action Changed;

        public BountyService(BountyConfig config, SaveSystem saveSystem, ITimeProvider time)
        {
            _config = config;
            _saveSystem = saveSystem;
            _time = time;
        }

        public bool IsPaidSlot(int index) => (index == 2 || index == 3);
        public bool IsFreeSlot(int index) => !IsPaidSlot(index);

        // Вызывается когда фича стала доступна (unlock) или когда открыли попап
        public void EnsureInitializedOrRefreshed()
        {
            var s = State;

            if (!s.initialized)
            {
                s.initialized = true;
                GenerateNewSet();
                _saveSystem.Save();
                return;
            }

            if (s.generatedAtUtcTicks <= 0)
            {
                GenerateNewSet();
                _saveSystem.Save();
                return;
            }

            var now = _time.UtcNow;
            var generated = new DateTime(s.generatedAtUtcTicks, DateTimeKind.Utc);
            var days = (now - generated).TotalDays;

            int refreshDays = _config != null ? Mathf.Max(1, _config.refreshDays) : 2;

            if (days >= refreshDays)
            {
                GenerateNewSet();
                _saveSystem.Save();
            }
        }

        public bool IsClaimed(int index) => State.IsClaimed(index);

        public bool CanClaim(int index)
        {
            if (index < 0 || index >= 6) return false;
            if (IsClaimed(index)) return false;

            // цепочка
            if (index == 0) return true;
            return IsClaimed(index - 1);
        }

        public bool TryClaimFree(int index)
        {
            if (!IsFreeSlot(index)) return false;
            if (!CanClaim(index)) return false;

            ApplyReward(State.slots[index]);
            State.SetClaimed(index);
            _saveSystem.Save();
            Changed?.Invoke();
            return true;
        }

        // Вызвать ТОЛЬКО после успешной IAP
        public bool TryClaimPaid(int index)
        {
            if (!IsPaidSlot(index)) return false;
            if (!CanClaim(index)) return false;

            ApplyReward(State.slots[index]);
            State.SetClaimed(index);
            _saveSystem.Save();
            Changed?.Invoke();
            return true;
        }

        private void GenerateNewSet()
        {
            var s = State;
            s.ResetClaims();
            s.generatedAtUtcTicks = _time.UtcNow.Ticks;

            if (s.slots == null || s.slots.Length != 6)
                s.slots = new Reward[6];

            for (int i = 0; i < 6; i++)
                s.slots[i] = RollFromPool();
        }

        private Reward RollFromPool()
        {
            var pool = _config != null ? _config.possibleRewards : null;
            if (pool == null || pool.Length == 0)
                return new Reward { coins = 100, weight = 1 };

            int totalWeight = 0;
            for (int i = 0; i < pool.Length; i++)
            {
                if (!IsCountable(pool[i])) continue;
                totalWeight += Mathf.Max(1, pool[i].weight);
            }

            if (totalWeight <= 0)
                return new Reward { coins = 100, weight = 1 };

            int roll = UnityEngine.Random.Range(0, totalWeight);
            int acc = 0;

            for (int i = 0; i < pool.Length; i++)
            {
                var r = pool[i];
                if (!IsCountable(r)) continue;

                acc += Mathf.Max(1, r.weight);
                if (roll < acc)
                    return r;
            }

            return pool[0];
        }

        // В bounty разрешаем ТОЛЬКО исчисляемые:
        // coins / boosts / buffs (без infinite и т.п.)
        private bool IsCountable(Reward r)
        {
            bool hasCoins = r.coins > 0 || (r.coinsMax > 0 && r.coinsMax >= r.coinsMin);
            return hasCoins
                   || r.boost1Amount > 0
                   || r.boost2Amount > 0
                   || r.buff1Amount > 0
                   || r.buff2Amount > 0
                   || r.buff3Amount > 0
                   || r.buff4Amount > 0;
        }

        private void ApplyReward(Reward r)
        {
            var save = _saveSystem.Current;

            int coins = r.GetCoins();
            if (coins > 0) save.wallet.coins += coins;

            if (r.boost1Amount > 0) save.inventory.boostGrowWholeLevel += r.boost1Amount;
            if (r.boost2Amount > 0) save.inventory.boostExtraTime += r.boost2Amount;

            if (r.buff1Amount > 0) save.inventory.buffGrowTemp += r.buff1Amount;
            if (r.buff2Amount > 0) save.inventory.buffRadar += r.buff2Amount;
            if (r.buff3Amount > 0) save.inventory.buffMagnet += r.buff3Amount;
            if (r.buff4Amount > 0) save.inventory.buffFreezeTime += r.buff4Amount;
        }
    }
}