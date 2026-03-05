using Meta.State;
using System;
using UnityEngine;

namespace Meta.Services
{
    public readonly struct StarContestSnapshot
    {
        public readonly TimeSpan Remaining;
        public readonly int PlayerStars;
        public readonly int PlayerRank;     // 1-based
        public readonly int Multiplier;     // 1..10
        public readonly StarContestEntryState[] Top; // top-7 combined (bots + maybe player)
        public readonly DateTime SeasonEndUtc;
        public readonly DateTime SeasonStartUtc;

        public StarContestSnapshot(
            TimeSpan remaining,
            int playerStars,
            int playerRank,
            int multiplier,
            StarContestEntryState[] top,
            DateTime endUtc,
            DateTime startUtc)
        {
            Remaining = remaining;
            PlayerStars = playerStars;
            PlayerRank = playerRank;
            Multiplier = multiplier;
            Top = top;
            SeasonEndUtc = endUtc;
            SeasonStartUtc = startUtc;
        }
    }

    public sealed class StarContestService
    {
        private readonly Core.Time.ITimeProvider _time;

        // same approach as LeaderboardService
        private readonly TimeSpan _simInterval = TimeSpan.FromMinutes(30);

        private const int VisibleTopCount = 7;
        private const int BotCount = 20;

        private static readonly string[] NickPool =
        {
            "Nova", "Rex", "Milo", "Zara", "Kira", "Axel", "Luna", "Orion", "Viper", "Skye",
            "Bolt", "Raven", "Echo", "Iris", "Frost", "Blaze", "Drift", "Nyx", "Kai", "Jett",
            "Sage", "Pixel", "Storm", "Cosmo", "Vega", "Rune", "Sparrow", "Wolf", "Neon", "Zephyr"
        };

        public StarContestService(Core.Time.ITimeProvider time)
        {
            _time = time;
        }

        /// <summary>
        /// Call when menu opens Star Contest page OR general meta tick in menu.
        /// Handles: season rollover + reward payout once.
        /// </summary>
        public void Tick(PlayerSave save, WalletService wallet)
        {
            if (save == null) return;
            EnsureUnlocked(save);
            if (!save.tutorial.starContestUnlockTutorialShown) return;

            EnsureSeasonAndPayIfEnded(save, wallet);
            SimulateIfNeeded(save);
        }

        public void OnOpened(PlayerSave save, WalletService wallet)
        {
            Tick(save, wallet);
        }

        public void OnWin(PlayerSave save, int starsEarned)
        {
            if (save == null) return;
            if (!save.tutorial.starContestUnlockTutorialShown) return;

            EnsureSeason(save.starContest, _time.UtcNow);

            // update streak -> multiplier
            save.starContest.winStreak++;
            save.starContest.multiplier = ComputeMultiplier(save.starContest.winStreak);

            int add = Math.Max(0, starsEarned) * save.starContest.multiplier;
            save.starContest.playerStars += add;
        }

        public void OnLose(PlayerSave save)
        {
            if (save == null) return;
            if (!save.tutorial.starContestUnlockTutorialShown) return;

            EnsureSeason(save.starContest, _time.UtcNow);

            save.starContest.winStreak = 0;
            save.starContest.multiplier = 1;
        }

        public StarContestSnapshot GetSnapshot(PlayerSave save)
        {
            if (save == null) return default;

            EnsureUnlocked(save);
            if (!save.tutorial.starContestUnlockTutorialShown) return default;

            EnsureSeason(save.starContest, _time.UtcNow);
            SimulateIfNeeded(save);

            var sc = save.starContest;
            var now = _time.UtcNow;
            var remaining = new TimeSpan(Math.Max(0, sc.seasonEndUtcTicks - now.Ticks));

            var combined = BuildCombinedSorted(save);
            int playerRank = ComputePlayerRank(combined);

            var top = new StarContestEntryState[Math.Min(VisibleTopCount, combined.Length)];
            Array.Copy(combined, 0, top, 0, top.Length);

            return new StarContestSnapshot(
                remaining,
                sc.playerStars,
                playerRank,
                Math.Max(1, sc.multiplier),
                top,
                new DateTime(sc.seasonEndUtcTicks, DateTimeKind.Utc),
                new DateTime(sc.seasonStartUtcTicks, DateTimeKind.Utc)
            );
        }

        // -------------------------
        // Internals
        // -------------------------

        private void EnsureUnlocked(PlayerSave save)
        {
            if (save.starContest == null) save.starContest = new StarContestState();
        }

        private void EnsureSeasonAndPayIfEnded(PlayerSave save, WalletService wallet)
        {
            var sc = save.starContest;
            var now = _time.UtcNow;

            // If season exists and ended => pay reward (once) then start new season
            if (sc.HasSeason && now.Ticks >= sc.seasonEndUtcTicks)
            {
                if (!sc.seasonRewardPaid)
                {
                    int rank = ComputeFinalRankAtSeasonEnd(save);
                    int reward = RewardByRank(rank);
                    if (reward > 0 && wallet != null)
                        wallet.AddCoins(save, reward);

                    sc.seasonRewardPaid = true;
                }

                StartNewSeason(sc, now);
            }
            else
            {
                // no season yet
                if (!sc.HasSeason)
                    StartNewSeason(sc, now);
            }

            // safety for old saves
            if (sc.bots == null || sc.bots.Length != BotCount)
            {
                GenerateBots(sc);
                SortBotsDesc(sc);
            }

            // clamp multiplier always valid
            if (sc.multiplier <= 0) sc.multiplier = 1;
        }

        private void EnsureSeason(StarContestState sc, DateTime nowUtc)
        {
            if (sc == null) return;

            if (!sc.HasSeason)
            {
                StartNewSeason(sc, nowUtc);
                return;
            }

            // Note: payout is handled in EnsureSeasonAndPayIfEnded (Tick),
            // but GetSnapshot/OnWin/OnLose should not “pay” coins.
            if (nowUtc.Ticks >= sc.seasonEndUtcTicks)
            {
                // If snapshot is requested exactly after end but before Tick:
                // just start new season (reward payout will happen in Tick when menu open).
                StartNewSeason(sc, nowUtc);
            }
        }

        private void StartNewSeason(StarContestState sc, DateTime nowUtc)
        {
            sc.seasonStartUtcTicks = nowUtc.Ticks;
            sc.seasonEndUtcTicks = nowUtc.AddDays(1).Ticks; // daily season

            sc.lastSimUtcTicks = nowUtc.Ticks;
            sc.seasonSeed = MakeSeed(nowUtc);
            sc.simStepIndex = 0;

            // reset player for new season
            sc.playerStars = 0;

            // reset star contest streak & multiplier
            sc.winStreak = 0;
            sc.multiplier = 1;

            sc.seasonRewardPaid = false;

            GenerateBots(sc);
            SortBotsDesc(sc);
        }

        private int MakeSeed(DateTime nowUtc)
        {
            unchecked
            {
                long t = nowUtc.Ticks;
                return (int)(t ^ (t >> 32) ^ 0x6a09e667);
            }
        }

        private void GenerateBots(StarContestState sc)
        {
            var rnd = new System.Random(sc.seasonSeed);
            sc.bots = new StarContestEntryState[BotCount];

            // Daily contest: let scores be relatively “reachable”
            // Start range: 10..80 stars
            for (int i = 0; i < BotCount; i++)
            {
                var e = new StarContestEntryState();
                e.nickName = NickPool[rnd.Next(NickPool.Length)] + rnd.Next(10, 99);
                e.avatarId = rnd.Next(0, 9);
                e.avatarFrameId = rnd.Next(0, 9);

                e.stars = rnd.Next(10, 80);
                e.isPlayer = false;

                sc.bots[i] = e;
            }
        }

        private void SimulateIfNeeded(PlayerSave save)
        {
            var sc = save.starContest;
            var now = _time.UtcNow;

            if (now.Ticks < sc.lastSimUtcTicks) sc.lastSimUtcTicks = now.Ticks;

            var elapsed = new TimeSpan(now.Ticks - sc.lastSimUtcTicks);
            if (elapsed < _simInterval) return;

            int steps = Mathf.Clamp((int)(elapsed.Ticks / _simInterval.Ticks), 1, 48);

            var rnd = new System.Random(sc.seasonSeed ^ sc.simStepIndex);

            for (int s = 0; s < steps; s++)
            {
                // each step bots add small amount to feel alive
                for (int i = 0; i < sc.bots.Length; i++)
                {
                    int add = rnd.Next(0, 6); // 0..5
                    sc.bots[i].stars += add;
                }

                sc.simStepIndex++;
            }

            sc.lastSimUtcTicks = now.Ticks;
            SortBotsDesc(sc);
        }

        private void SortBotsDesc(StarContestState sc)
        {
            Array.Sort(sc.bots, (a, b) => b.stars.CompareTo(a.stars));
        }

        private StarContestEntryState[] BuildCombinedSorted(PlayerSave save)
        {
            var sc = save.starContest;

            // player entry
            var p = new StarContestEntryState
            {
                nickName = save.profile.characterName,
                avatarId = save.profile.avatarId,
                avatarFrameId = save.profile.frameId,
                stars = sc.playerStars,
                isPlayer = true
            };

            var arr = new StarContestEntryState[sc.bots.Length + 1];
            Array.Copy(sc.bots, 0, arr, 0, sc.bots.Length);
            arr[arr.Length - 1] = p;

            Array.Sort(arr, (x, y) =>
            {
                int c = y.stars.CompareTo(x.stars);
                if (c != 0) return c;

                // tie-break: prefer player slightly higher (feel-good)
                if (x.isPlayer && !y.isPlayer) return -1;
                if (!x.isPlayer && y.isPlayer) return 1;

                return string.CompareOrdinal(x.nickName, y.nickName);
            });

            return arr;
        }

        private int ComputePlayerRank(StarContestEntryState[] combinedSorted)
        {
            for (int i = 0; i < combinedSorted.Length; i++)
                if (combinedSorted[i].isPlayer)
                    return i + 1;
            return combinedSorted.Length;
        }

        private int ComputeFinalRankAtSeasonEnd(PlayerSave save)
        {
            // rank among combined list at the moment of payout
            var combined = BuildCombinedSorted(save);
            return ComputePlayerRank(combined);
        }

        private int RewardByRank(int rank)
        {
            if (rank <= 0) return 0;
            if (rank == 1) return 1000;
            if (rank == 2) return 700;
            if (rank == 3) return 400;
            if (rank >= 4 && rank <= 7) return 200;
            return 0;
        }

        // Table you requested: 1-2-4-6-10
        private int ComputeMultiplier(int winStreak)
        {
            if (winStreak <= 0) return 1;
            if (winStreak == 1) return 2;
            if (winStreak == 2) return 4;
            if (winStreak == 3) return 6;
            return 10; // 4+ wins => cap
        }
    }
}