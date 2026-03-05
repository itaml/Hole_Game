using System;

namespace Meta.State
{
    [Serializable]
    public sealed class InventoryState
    {
        // Boosts
        public int boostGrowWholeLevel = 0;
        public int boostExtraTime = 0;

        // 🔥 Buffs (если они должны накапливаться)
        public int buffGrowTemp = 0;
        public int buffRadar = 0;
        public int buffMagnet = 0;
        public int buffFreezeTime = 0;
    }
}