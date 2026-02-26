using System;

namespace Core.Configs
{
    [Serializable]
    public sealed class Reward
    {
        public int coins = 0;

        // Infinite lives
        public int infiniteLivesMinutes = 0;

        // Infinite boosts (общие — если хочешь оставить)
        public int infiniteBoostsMinutes = 0;

        // 🔥 NEW: Infinite per boost type
        public int infiniteBoost1Minutes = 0;
        public int infiniteBoost2Minutes = 0;

        // Boost amounts (если расходуемые)
        public int boost1Amount = 0;
        public int boost2Amount = 0;

        // 🔥 NEW: Buff amounts (4 типа)
        public int buff1Amount = 0; // GrowTemp
        public int buff2Amount = 0; // Radar
        public int buff3Amount = 0; // Magnet
        public int buff4Amount = 0; // FreezeTime
    }
}