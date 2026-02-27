using System;

namespace GameBridge.Contracts
{
    [Serializable]
    public sealed class RunConfig
    {
        public int levelIndex;

        public bool boost1Activated; // BoostType.GrowWholeLevel
        public bool boost2Activated; // BoostType.ExtraLevelTime

        // 3 wins in a row => in 4th run enable bonus spawn
        public bool bonusSpawnActive;

        // Buff inventory snapshot (for Game UI + usage logic)
        // BuffType:
        // 1) GrowTemp, 2) Radar, 3) Magnet, 4) FreezeTime
        public int buff1Count;
        public int buff2Count;
        public int buff3Count;
        public int buff4Count;

        // Convenience for "continue" offer logic in Game
        public int walletCoinsSnapshot;
    }
}