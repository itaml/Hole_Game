using Core.Configs;
using Core.Save;
using Core.Time;
using GameBridge.Contracts;
using Meta.State;
using System;
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

        public void SetCharacterName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = "Player";

            name = name.Trim();

            // можно ограничить длину, чтобы UI не ломался
            const int maxLen = 16;
            if (name.Length > maxLen)
                name = name.Substring(0, maxLen);

            Save.profile.characterName = name;
            _saveSystem.Save();
        }

        public void SetAvatar(int avatarId)
        {
            if (avatarId < 0) avatarId = 0;
            Save.profile.avatarId = avatarId;
            _saveSystem.Save();
        }

        public void SetFrame(int frameId)
        {
            if (frameId < 0) frameId = 0;
            Save.profile.frameId = frameId;
            _saveSystem.Save();
        }

        public string GetCharacterName() => Save.profile.characterName;
        public int GetAvatarId() => Save.profile.avatarId;
        public int GetFrameId() => Save.profile.frameId;

        public bool CanStartGame() => _lives.CanStartGame(Save);

        public bool IsInfiniteLivesActive() => _lives.IsInfiniteLivesActive(Save);

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
                Save.profile.currentWinStreak = 0;

                Save.progress.failedLevels.Add(r.levelIndex);

                _lives.ConsumeLifeOnLose(Save);

                if (_unlocks.IsWinStreakUnlocked(r.levelIndex))
                    _streak.OnLose(Save);
            }
            else // Win
            {
                if (r.starsEarned == 3)
                {
                    Save.profile.threeStarWins++;
                }

                Save.profile.currentWinStreak++;

                if (Save.profile.currentWinStreak > Save.profile.longestWinStreak)
                    Save.profile.longestWinStreak = Save.profile.currentWinStreak;

                bool failedBefore = Save.progress.failedLevels.Contains(r.levelIndex);

                if (!failedBefore)
                    Save.profile.firstTryWins++;
                Save.progress.failedLevels.Remove(r.levelIndex);

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

        public void RegisterLogin()
        {
            var profile = Save.profile;
            var now = _time.UtcNow.Date;

            if (profile.lastLoginUtcTicks == 0)
            {
                profile.loginDaysStreak = 1;
            }
            else
            {
                var lastLoginDate = new DateTime(profile.lastLoginUtcTicks).Date;
                int diff = (now - lastLoginDate).Days;

                if (diff == 1)
                {
                    profile.loginDaysStreak++;
                }
                else if (diff > 1)
                {
                    profile.loginDaysStreak = 1;
                }
                // diff == 0 → ничего не делаем
            }

            profile.lastLoginUtcTicks = now.Ticks;

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
            int bonusSpawnLevel = 0;
            if (_unlocks.IsWinStreakUnlocked(level))
                bonusSpawnLevel = _streak.GetStreak(Save);

            // Buff availability: only if unlocked by level; otherwise send 0 to Game UI
            bool buff1Unlocked = _unlocks.IsBuff1Unlocked(level);
            bool buff2Unlocked = _unlocks.IsBuff2Unlocked(level);
            bool buff3Unlocked = _unlocks.IsBuff3Unlocked(level);
            bool buff4Unlocked = _unlocks.IsBuff4Unlocked(level);

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

                bonusSpawnLevel = bonusSpawnLevel,

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

            bool infinite = IsInfiniteBoost1Active();
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

            bool infinite = IsInfiniteBoost2Active();
            if (infinite) return true;

            // consumable
            if (Save.inventory.boostExtraTime <= 0) return false;
            Save.inventory.boostExtraTime--;
            return true;
        }
    }
}