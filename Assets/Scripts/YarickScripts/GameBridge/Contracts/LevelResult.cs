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
        public int coinsEarnedToWallet;
        public int coinsEarnedToBank;
        public int battlepassItemsCollected;

        // Spending/usage (Game -> Menu)
        public int coinsSpentInGame; // continue, etc.

        public int buff1Used; // GrowTemp
        public int buff2Used; // Radar
        public int buff3Used; // Magnet
        public int buff4Used; // FreezeTime
    }
}