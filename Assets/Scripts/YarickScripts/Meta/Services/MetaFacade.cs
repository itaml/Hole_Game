using Core.Configs;
using Core.Levels;
using Core.Save;
using Core.Time;
using GameBridge.Contracts;
using Meta.State;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.SocialPlatforms.Impl;

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
        private readonly LeaderboardService _leaderboard;
        private readonly BountyService _bounty;

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
            LeaderboardService leaderboard,
            ITimeProvider time,
            BountyService bounty) // <-- добавили
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
            _leaderboard = leaderboard;
            _time = time;

            _bounty = bounty; // <-- добавили
        }

        public PlayerSave Save => _saveSystem.Current;

        public void SaveNow() => _saveSystem.Save();

        private readonly List<Reward> _grantedRewards = new();
        public BountyService Bounty => _bounty;

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
            _leaderboard.Tick(Save);

            // Bounty: создаём первый пул при первом заходе в меню после unlock
            // и далее делаем refresh по таймеру
            if (_unlocks.IsBountyUnlocked(Save.progress.currentLevel))
                _bounty.EnsureInitializedOrRefreshed();

            _saveSystem.Save();
        }

        public void OnLeaderboardOpened()
        {
            _leaderboard.OnLeaderboardOpened(Save);
            _saveSystem.Save();
        }

        public LeaderboardSnapshot GetLeaderboardSnapshot()
        {
            return _leaderboard.GetSnapshot(Save);
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

            int prevLevelBefore = Save.progress.currentLevel;

            // Keep timers up to date first
            _lives.TickRegen(Save);

            // 0) Sanity: result должен относиться к текущему уровню (или хотя бы к ожидаемому)
            // Можно убрать если не нужно
            // if (r.levelIndex != Save.progress.currentLevel) ...

            // 1) Применяем итоговые значения (coins + buffs)
            ApplyFinalCoinsFromGame(r);
            ApplyFinalBuffCountsFromGame(r);
            ApplyFinalBankCoinsFromGame(r);

            // 2) Outcome handling: lives + streak
            if (r.outcome == LevelOutcome.Lose)
            {
                // If player loses a Master/Challenge level once, this level becomes 'burned' (no bonuses anymore)
                if (LevelTypeUtils.IsMasterLevel(r.levelIndex) || LevelTypeUtils.IsChallengeLevel(r.levelIndex))
                    SpecialLevelBurnStorage.Burn(r.levelIndex);

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

                int add = Math.Max(0, r.starsEarned) * 3; // как ты и хотел: зависит от stars
                _leaderboard.AddPlayerScore(Save, add);

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

    int itemsToAdd = r.battlepassItemsCollected;

    // Multiply rewards only on Win and only if this special level wasn't burned by a previous loss
    if (r.outcome == LevelOutcome.Win && itemsToAdd > 0 && !SpecialLevelBurnStorage.IsBurned(r.levelIndex))
    {
        if (LevelTypeUtils.IsMasterLevel(r.levelIndex))
            itemsToAdd *= 5;
        else if (LevelTypeUtils.IsChallengeLevel(r.levelIndex))
            itemsToAdd *= 3;
    }

    _battlepass.AddItems(Save, itemsToAdd);
}

            // 6) Progression: advance only on win
            if (r.outcome == LevelOutcome.Win)
            {
                if (Save.progress.currentLevel <= r.levelIndex)
                    Save.progress.currentLevel = r.levelIndex + 1;
            }

// 6.5) Feature unlock rewards + tutorials (one-time)
if (r.outcome == LevelOutcome.Win)
    HandleUnlocksAndTutorials(prevLevelBefore, Save.progress.currentLevel);

            _saveSystem.Save();
        }

        private void HandleUnlocksAndTutorials(int prevLevel, int newLevel)
        {
            if (_unlocks == null) return;
            if (Save == null) return;

            if (newLevel <= prevLevel) return;

            const int DefaultBuffGrant = 3;
            const int DefaultBoostGrant = 3;

            // Buff 1
            if (!Save.tutorial.buff1Granted &&
                prevLevel < _unlocks.Buff1UnlockLevel && newLevel >= _unlocks.Buff1UnlockLevel)
            {
                Save.inventory.buffGrowTemp += DefaultBuffGrant;
                Save.tutorial.buff1Granted = true;
                _grantedRewards.Add(new Reward { buff1Amount = DefaultBuffGrant });
            }

            // Buff 2
            if (!Save.tutorial.buff2Granted &&
                prevLevel < _unlocks.Buff2UnlockLevel && newLevel >= _unlocks.Buff2UnlockLevel)
            {
                Save.inventory.buffRadar += DefaultBuffGrant;
                Save.tutorial.buff2Granted = true;
                _grantedRewards.Add(new Reward { buff2Amount = DefaultBuffGrant });
            }

            // Buff 3
            if (!Save.tutorial.buff3Granted &&
                prevLevel < _unlocks.Buff3UnlockLevel && newLevel >= _unlocks.Buff3UnlockLevel)
            {
                Save.inventory.buffMagnet += DefaultBuffGrant;
                Save.tutorial.buff3Granted = true;
                _grantedRewards.Add(new Reward { buff3Amount = DefaultBuffGrant });
            }

            // Buff 4
            if (!Save.tutorial.buff4Granted &&
                prevLevel < _unlocks.Buff4UnlockLevel && newLevel >= _unlocks.Buff4UnlockLevel)
            {
                Save.inventory.buffFreezeTime += DefaultBuffGrant;
                Save.tutorial.buff4Granted = true;
                _grantedRewards.Add(new Reward { buff4Amount = DefaultBuffGrant });
            }

            // Boost 1
            if (!Save.tutorial.boost1Granted &&
                prevLevel < _unlocks.Boost1UnlockLevel && newLevel >= _unlocks.Boost1UnlockLevel)
            {
                Save.inventory.boostGrowWholeLevel += DefaultBoostGrant;
                Save.tutorial.boost1Granted = true;
                _grantedRewards.Add(new Reward { boost1Amount = DefaultBoostGrant });
if (!Save.tutorial.boost1StartTutorialShown &&
    Save.tutorial.pendingStartTutorialId == 0)
{
    Save.tutorial.pendingStartTutorialId = 2;
}
}

            // Boost 2
            if (!Save.tutorial.boost2Granted &&
                prevLevel < _unlocks.Boost2UnlockLevel && newLevel >= _unlocks.Boost2UnlockLevel)
            {
                Save.inventory.boostExtraTime += DefaultBoostGrant;
                Save.tutorial.boost2Granted = true;
                _grantedRewards.Add(new Reward { boost2Amount = DefaultBoostGrant });
if (!Save.tutorial.boost2StartTutorialShown &&
    Save.tutorial.pendingStartTutorialId == 0)
{
    Save.tutorial.pendingStartTutorialId = 3;
}
}

            // Win streak tutorial: show ON TOP of StartPopup after unlocking
            if (!Save.tutorial.winStreakStartTutorialShown &&
                Save.tutorial.pendingStartTutorialId == 0 &&
                prevLevel < _unlocks.WinStreakUnlockLevel && newLevel >= _unlocks.WinStreakUnlockLevel)
            {
                Save.tutorial.pendingStartTutorialId = 1;
            }

            // Profile tutorial (post-win): when reaching level 18 (after cinematic/rewards)
            const int ProfileTutorialLevel = 3;
            if (!Save.tutorial.profilePostWinTutorialShownProfile &&
                Save.tutorial.pendingPostWinTutorialId == 0 &&
                prevLevel < ProfileTutorialLevel && newLevel >= ProfileTutorialLevel)
            {
                Save.tutorial.pendingPostWinTutorialId = 1;
            }

            if (!Save.tutorial.leaderboardUnlockTutorialShown &&
                Save.tutorial.pendingStartTutorialId == 0 &&
                prevLevel < _unlocks.LeaderboardUnlockLevel && newLevel >= _unlocks.LeaderboardUnlockLevel)
            {
                Save.tutorial.pendingStartTutorialId = 4;
            }

            if (!Save.tutorial.battlepassUnlockTutorialShown &&
    Save.tutorial.pendingStartTutorialId == 0 &&
    prevLevel < _unlocks.BattlepassUnlockLevel && newLevel >= _unlocks.BattlepassUnlockLevel)
            {
                Save.tutorial.pendingStartTutorialId = 5; // батлпас
            }
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

        private void ApplyFinalBankCoinsFromGame(LevelResult r)
        {
            Save.bank.bankCoins = System.Math.Max(0, r.bankCoinsResult);
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

            bool bpUnlocked = _unlocks != null && _unlocks.IsBattlepassUnlocked(level);

            bool bankUnlocked = _unlocks != null && _unlocks.IsBankUnlocked(level);

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

                isBattlepasOpen = bpUnlocked,
                isBankOpen = bankUnlocked,
                bankCoinsSnapshot = bankUnlocked ? Save.bank.bankCoins : 0,
                bankCapacitySnapshot = bankUnlocked ? _bank.Capacity : 0,
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