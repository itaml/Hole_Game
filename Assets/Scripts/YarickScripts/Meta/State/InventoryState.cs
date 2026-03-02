using System;

namespace Meta.State
{
    [Serializable]
    public sealed class InventoryState
    {
        // Boosts
        public int boostGrowWholeLevel = 5;
        public int boostExtraTime = 5;

        // 🔥 Buffs (если они должны накапливаться)
        public int buffGrowTemp = 3;
        public int buffRadar = 3;
        public int buffMagnet = 3;
        public int buffFreezeTime = 5;
    }
}