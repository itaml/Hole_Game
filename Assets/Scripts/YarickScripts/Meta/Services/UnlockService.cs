using Core.Configs;

namespace Meta.Services
{
    public sealed class UnlockService
    {
        private readonly UnlockConfig _cfg;

        public UnlockService(UnlockConfig cfg) { _cfg = cfg; }

        // Expose thresholds
        public int StarsChestUnlockLevel => _cfg.starsChestUnlockLevel;
        public int LevelsChestUnlockLevel => _cfg.levelsChestUnlockLevel;

        public int BankUnlockLevel => _cfg.bankUnlockLevel;
        public int BattlepassUnlockLevel => _cfg.battlepassUnlockLevel;

        public int Boost1UnlockLevel => _cfg.boost1UnlockLevel;
        public int Boost2UnlockLevel => _cfg.boost2UnlockLevel;

        public int Buff1UnlockLevel => _cfg.buff1UnlockLevel;
        public int Buff2UnlockLevel => _cfg.buff2UnlockLevel;
        public int Buff3UnlockLevel => _cfg.buff3UnlockLevel;
        public int Buff4UnlockLevel => _cfg.buff4UnlockLevel;

        public int WinStreakUnlockLevel => _cfg.winStreakUnlockLevel;
        public int InterstitialAdsUnlockLevel => _cfg.interstitialAdsUnlockLevel;

        // Existing helpers
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