using System.Collections.Generic;
using Core.Configs;
using GameBridge.Contracts;

namespace Meta.Services
{
    public sealed class UnlockService
    {
        private readonly UnlockConfig _cfg;

        public UnlockService(UnlockConfig cfg) { _cfg = cfg; }

        public bool IsStarsChestUnlocked(int level) => level >= _cfg.starsChestUnlockLevel;
        public bool IsLevelsChestUnlocked(int level) => level >= _cfg.levelsChestUnlockLevel;

        public bool IsBankUnlocked(int level) => level >= _cfg.bankUnlockLevel;
        public bool IsBattlepassUnlocked(int level) => level >= _cfg.battlepassUnlockLevel;

        public bool IsBoost1Unlocked(int level) => level >= _cfg.boost1UnlockLevel;
        public bool IsBoost2Unlocked(int level) => level >= _cfg.boost2UnlockLevel;

        public bool IsBuff1Unlocked(int level) => level >= _cfg.buff1UnlockLevel;
        public bool IsBuff2Unlocked(int level) => level >= _cfg.buff2UnlockLevel;
        public bool IsBuff3Unlocked(int level) => level >= _cfg.buff3UnlockLevel;
        public bool IsBuff4Unlocked(int level) => level >= _cfg.buff4UnlockLevel;

        public bool IsWinStreakUnlocked(int level) => level >= _cfg.winStreakUnlockLevel;
        public bool IsInterstitialUnlocked(int level) => level >= _cfg.interstitialAdsUnlockLevel;
    }
}
