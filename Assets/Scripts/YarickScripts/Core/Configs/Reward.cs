using System;
using UnityEngine;

namespace Core.Configs
{
    [Serializable]
    public sealed class Reward
    {
        // Вес выпадения (чем больше, тем чаще)
        public int weight = 1;

        // Coins (fixed or ranged)
        public int coins = 0;
        public int coinsMin = 0;
        public int coinsMax = 0;

        // Infinite lives
        public int infiniteLivesMinutes = 0;

        // Infinite per boost type
        public int infiniteBoost1Minutes = 0;
        public int infiniteBoost2Minutes = 0;

        // Boost amounts (consumables)
        public int boost1Amount = 0;
        public int boost2Amount = 0;

        // Buff amounts (4 types)
        public int buff1Amount = 0; // GrowTemp
        public int buff2Amount = 0; // Radar
        public int buff3Amount = 0; // Magnet
        public int buff4Amount = 0; // FreezeTime

        public int GetCoins()
        {
            // Диапазон монет имеет приоритет
            if (coinsMax > 0 && coinsMax >= coinsMin)
                return UnityEngine.Random.Range(coinsMin, coinsMax + 1);

            return coins;
        }
    }
}