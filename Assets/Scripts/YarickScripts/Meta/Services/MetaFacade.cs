using Core.Save;
using Core.Time;
using GameBridge.Contracts;
using Meta.State;

namespace Meta.Services
{
    /// <summary>
    /// Single entry point for Menu meta. Game never edits save directly; it returns LevelResult.
    /// Boosts are selected in Menu (bools). Buffs are inventory resources used in Game.
    /// </summary>
    public sealed class MetaFacade
    {
        private readonly SaveSystem _saveSystem;
        private readonly UnlockService _unlocks;
        private readonly LivesService _lives;
        private readonly WalletService _wallet;
        private readonly ChestService _chests;
        private readonly BankService _bank;
        private readonly BattlepassService _battlepass;
        private readonly WinStreakService _streak;
        private readonly AdsPolicyService _ads;
        private readonly ITimeProvider _time;

        public MetaFacade(
            SaveSystem saveSystem,
            UnlockService unlocks,
            LivesService lives,
            WalletService wallet,
            ChestService chests,
            BankService bank,
            BattlepassService battlepass,
            WinStreakService streak,
            AdsPolicyService ads,
            ITimeProvider time)
        {
            _saveSystem = saveSystem;
            _unlocks = unlocks;
            _lives = lives;
            _wallet = wallet;
            _chests = chests;
            _bank = bank;
            _battlepass = battlepass;
            _streak = streak;
            _ads = ads;
            _time = time;
        }

        public PlayerSave Save => _saveSystem.Current;

        public void Tick()
        {
            _lives.TickRegen(Save);
            _battlepass.EnsureSeason(Save);
            _saveSystem.Save();
        }

        public bool CanStartGame() => _lives.CanStartGame(Save);

        public bool IsInfiniteLivesActive() => _lives.IsInfiniteLivesActive(Save);

        // Global infinite boosts (if you still use it)
        public bool IsInfiniteBoostsActive()
            => Save.timeBonuses.infiniteBoostsUntilUtcTicks > _time.UtcNow.Ticks;

        // Per-boost infinite
        public bool IsInfiniteBoost1Active()
            => Save.timeBonuses.infiniteBoost1UntilUtcTicks > _time.UtcNow.Ticks;

        public bool IsInfiniteBoost2Active()
            => Save.timeBonuses.infiniteBoost2UntilUtcTicks > _time.UtcNow.Ticks;

        public bool ShouldShowInterstitialAfterWin(int levelIndexJustWon)
            => _ads.ShouldShowInterstitial(_unlocks, levelIndexJustWon);

        /// <summary>
        /// Apply final level outcome (called when Menu loads after Game).
        /// IMPORTANT: Game sends final result only once (after win or final lose).
        /// </summary>
        public void ApplyLevelResult(LevelResult r)
        {
            if (r == null) return;

            // Keep timers up to date first
            _lives.TickRegen(Save);

            // 0) Spend coins used inside Game (continue, etc.)
            if (r.coinsSpentInGame > 0)
            {
                // If for some reason not enough coins (desync), clamp and don't go negative.
                if (!_wallet.TrySpend(Save, r.coinsSpentInGame))
                    Save.wallet.coins = System.Math.Max(0, Save.wallet.coins - r.coinsSpentInGame);
            }

            // 1) Deduct used buffs from inventory (clamped)
            DeductBuffsFromInventory(r);

            // 2) Outcome handling: lives + streak
            if (r.outcome == LevelOutcome.Lose)
            {
                _lives.ConsumeLifeOnLose(Save);

                // streak only after unlock
                if (_unlocks.IsWinStreakUnlocked(r.levelIndex))
                    _streak.OnLose(Save);
            }
            else // Win
            {
                if (_unlocks.IsWinStreakUnlocked(r.levelIndex))
                    _streak.OnWin(Save);
            }

            // 3) Wallet / Bank earnings
            _wallet.AddCoins(Save, r.coinsEarnedToWallet);

            if (_unlocks.IsBankUnlocked(r.levelIndex))
                _bank.AddToBank(Save, r.coinsEarnedToBank);

            // 4) Star chest
            if (_unlocks.IsStarsChestUnlocked(r.levelIndex))
                _chests.AddStarsAndOpenIfReady(Save, r.starsEarned);

            // 5) Level chest (only on win)
            if (r.outcome == LevelOutcome.Win && _unlocks.IsLevelsChestUnlocked(r.levelIndex))
                _chests.AddLevelWinAndOpenIfReady(Save);

            // 6) Battlepass
            if (_unlocks.IsBattlepassUnlocked(r.levelIndex))
            {
                _battlepass.EnsureSeason(Save);
                _battlepass.AddItems(Save, r.battlepassItemsCollected);
            }

            // 7) Progression: advance only on win
            if (r.outcome == LevelOutcome.Win)
            {
                // Save.progress.currentLevel is the next playable level
                if (Save.progress.currentLevel <= r.levelIndex)
                    Save.progress.currentLevel = r.levelIndex + 1;
            }

            _saveSystem.Save();
        }

        private void DeductBuffsFromInventory(LevelResult r)
        {
            // If buff not unlocked at this level, we still clamp (should be 0 anyway).
            int used1 = System.Math.Max(0, r.buff1Used);
            int used2 = System.Math.Max(0, r.buff2Used);
            int used3 = System.Math.Max(0, r.buff3Used);
            int used4 = System.Math.Max(0, r.buff4Used);

            Save.inventory.buffGrowTemp = System.Math.Max(0, Save.inventory.buffGrowTemp - used1);
            Save.inventory.buffRadar = System.Math.Max(0, Save.inventory.buffRadar - used2);
            Save.inventory.buffMagnet = System.Math.Max(0, Save.inventory.buffMagnet - used3);
            Save.inventory.buffFreezeTime = System.Math.Max(0, Save.inventory.buffFreezeTime - used4);
        }

        /// <summary>
        /// Build run config for Game.
        /// boost1Selected/boost2Selected come from Menu selection (player choice).
        /// </summary>
        public RunConfig BuildRunConfig(bool boost1Selected, bool boost2Selected)
        {
            int level = Save.progress.currentLevel;

            // Bonus bag for 4th game after 3 wins (only after unlock)
            bool bonusSpawn = false;
            if (_unlocks.IsWinStreakUnlocked(level))
                bonusSpawn = _streak.ConsumeBonusBagIfArmed(Save);

            // Buff availability: only if unlocked by level; otherwise send 0 to Game UI
            bool buff1Unlocked = level >= 4;
            bool buff2Unlocked = level >= 6;
            bool buff3Unlocked = level >= 8;
            bool buff4Unlocked = level >= 10;

            int buff1Count = buff1Unlocked ? Save.inventory.buffGrowTemp : 0;
            int buff2Count = buff2Unlocked ? Save.inventory.buffRadar : 0;
            int buff3Count = buff3Unlocked ? Save.inventory.buffMagnet : 0;
            int buff4Count = buff4Unlocked ? Save.inventory.buffFreezeTime : 0;

            // Boost activation (must respect unlock + inventory or infinite)
            bool boost1Active = ResolveBoost1Activation(level, boost1Selected);
            bool boost2Active = ResolveBoost2Activation(level, boost2Selected);

            var cfg = new RunConfig
            {
                levelIndex = level,

                boost1Activated = boost1Active,
                boost2Activated = boost2Active,

                bonusSpawnActive = bonusSpawn,

                buff1Count = buff1Count,
                buff2Count = buff2Count,
                buff3Count = buff3Count,
                buff4Count = buff4Count,

                walletCoinsSnapshot = Save.wallet.coins,
                infiniteLivesActive = _lives.IsInfiniteLivesActive(Save)
            };

            _saveSystem.Save(); // consumes bonusBagArmed + potentially consumes boosts
            return cfg;
        }

        private bool ResolveBoost1Activation(int level, bool selected)
        {
            if (!selected) return false;
            if (!_unlocks.IsBoost1Unlocked(level)) return false;

            bool infinite = IsInfiniteBoostsActive() || IsInfiniteBoost1Active();
            if (infinite) return true;

            // consumable
            if (Save.inventory.boostGrowWholeLevel <= 0) return false;
            Save.inventory.boostGrowWholeLevel--;
            return true;
        }

        private bool ResolveBoost2Activation(int level, bool selected)
        {
            if (!selected) return false;
            if (!_unlocks.IsBoost2Unlocked(level)) return false;

            bool infinite = IsInfiniteBoostsActive() || IsInfiniteBoost2Active();
            if (infinite) return true;

            // consumable
            if (Save.inventory.boostExtraTime <= 0) return false;
            Save.inventory.boostExtraTime--;
            return true;
        }
    }
}