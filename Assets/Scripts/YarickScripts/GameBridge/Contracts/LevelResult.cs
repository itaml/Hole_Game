using System;

namespace GameBridge.Contracts
{
    [Serializable]
    public sealed class LevelResult
    {
        public int levelIndex;
        public LevelOutcome outcome;

        // Earnings (Game -> Menu)
        public int starsEarned;
        public int battlepassItemsCollected;

        public int coinsResult;

        public int buff1Count; // GrowTemp
        public int buff2Count; // Radar
        public int buff3Count; // Magnet
        public int buff4Count; // FreezeTime
    }
}