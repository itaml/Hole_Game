using Core.Configs;
using Core.Save;
using Core.Time;
using GameBridge.Contracts;
using Meta.State;
using System.Collections.Generic;

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

        private readonly List<Reward> _grantedRewards = new();

        public Reward[] ConsumeGrantedRewards()
        {
            if (_grantedRewards.Count == 0) return System.Array.Empty<Reward>();
            var arr = _grantedRewards.ToArray();
            _grantedRewards.Clear();
            return arr;
        }

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


        public void ApplyLevelResult(LevelResult r)
        {
            if (r == null) return;

            // Keep timers up to date first
            _lives.TickRegen(Save);

            // 0) Sanity: result должен относиться к текущему уровню (или хотя бы к ожидаемому)
            // Можно убрать если не нужно
            // if (r.levelIndex != Save.progress.currentLevel) ...

            // 1) Применяем итоговые значения (coins + buffs)
            ApplyFinalCoinsFromGame(r);
            ApplyFinalBuffCountsFromGame(r);

            // 2) Outcome handling: lives + streak
            if (r.outcome == LevelOutcome.Lose)
            {
                _lives.ConsumeLifeOnLose(Save);

                if (_unlocks.IsWinStreakUnlocked(r.levelIndex))
                    _streak.OnLose(Save);
            }
            else // Win
            {
                if (_unlocks.IsWinStreakUnlocked(r.levelIndex))
                    _streak.OnWin(Save);

                if (_unlocks.IsBankUnlocked(r.levelIndex))
                    _bank.AddWinDeposit(Save);
            }

            _grantedRewards.Clear();

            // 3) Star chest (auto-open inside ChestService)
            if (_unlocks.IsStarsChestUnlocked(r.levelIndex))
                _chests.AddStarsAndOpenIfReady(Save, r.starsEarned, _grantedRewards);

            // 4) Level chest (auto-open inside ChestService)
            if (r.outcome == LevelOutcome.Win && _unlocks.IsLevelsChestUnlocked(r.levelIndex))
                _chests.AddLevelWinAndOpenIfReady(Save, _grantedRewards);

            // 5) Battlepass
            if (_unlocks.IsBattlepassUnlocked(r.levelIndex))
            {
                _battlepass.EnsureSeason(Save);
                _battlepass.AddItems(Save, r.battlepassItemsCollected);
            }

            // 6) Progression: advance only on win
            if (r.outcome == LevelOutcome.Win)
            {
                if (Save.progress.currentLevel <= r.levelIndex)
                    Save.progress.currentLevel = r.levelIndex + 1;
            }

            _saveSystem.Save();
        }

        private void ApplyFinalCoinsFromGame(LevelResult r)
        {
            Save.wallet.coins = System.Math.Max(0, r.coinsResult);
        }

        private void ApplyFinalBuffCountsFromGame(LevelResult r)
        {
            Save.inventory.buffGrowTemp = System.Math.Max(0, r.buff1Count);
            Save.inventory.buffRadar = System.Math.Max(0, r.buff2Count);
            Save.inventory.buffMagnet = System.Math.Max(0, r.buff3Count);
            Save.inventory.buffFreezeTime = System.Math.Max(0, r.buff4Count);
        }

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