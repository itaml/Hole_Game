using Meta.State;
using System;
using UnityEngine;

namespace Meta.Services
{
    public sealed class LeaderboardService
    {
        private readonly Core.Time.ITimeProvider _time;

        // Каждые N минут "шевелим" таблицу.
        private readonly TimeSpan _simInterval = TimeSpan.FromMinutes(30);

        // Гарантия: игрок никогда не попадёт в топ10
        private const int SafetyMarginAbovePlayer = 2000;

        // Топ-7 для UI
        private const int VisibleTopCount = 7;

        // Храним 10 ботов (чтобы у игрока всегда было минимум 10 человек выше)
        private const int BotCount = 10;

        private static readonly string[] NickPool =
        {
            "Nova", "Rex", "Milo", "Zara", "Kira", "Axel", "Luna", "Orion", "Viper", "Skye",
            "Bolt", "Raven", "Echo", "Iris", "Frost", "Blaze", "Drift", "Nyx", "Kai", "Jett",
            "Sage", "Pixel", "Storm", "Cosmo", "Vega", "Rune", "Sparrow", "Wolf", "Neon", "Zephyr"
        };

        public LeaderboardService(Core.Time.ITimeProvider time)
        {
            _time = time;
        }

        public void OnLeaderboardOpened(PlayerSave save)
        {
            EnsureSeason(save);
            SimulateIfNeeded(save);
        }

        public void Tick(PlayerSave save)
        {
            EnsureSeason(save);
            SimulateIfNeeded(save);
        }

        public void AddPlayerScore(PlayerSave save, int delta)
        {
            if (delta <= 0) return;
            EnsureSeason(save);
            save.leaderboard.playerScore += delta;
            // после изменения игрока — подправим ботов, чтобы гарантия топ10 сохранялась
            EnforceNeverTop10(save);
        }

        public LeaderboardSnapshot GetSnapshot(PlayerSave save)
        {
            EnsureSeason(save);
            SimulateIfNeeded(save);

            var lb = save.leaderboard;
            var now = _time.UtcNow;
            var remaining = new TimeSpan(Math.Max(0, lb.seasonEndUtcTicks - now.Ticks));

            // Bots already sorted desc by score
            var top = new LeaderboardEntryState[Math.Min(VisibleTopCount, lb.bots.Length)];
            Array.Copy(lb.bots, 0, top, 0, top.Length);

            int playerRank = ComputePlayerRank(lb);

            return new LeaderboardSnapshot(
                remaining,
                lb.playerScore,
                playerRank,
                top,
                new DateTime(lb.seasonEndUtcTicks, DateTimeKind.Utc),
                new DateTime(lb.seasonStartUtcTicks, DateTimeKind.Utc)
            );
        }

        private void EnsureSeason(PlayerSave save)
        {
            if (save == null) return;
            if (save.leaderboard == null) save.leaderboard = new LeaderboardState();

            var lb = save.leaderboard;
            var now = _time.UtcNow;

            // No season yet OR expired => new season
            if (!lb.HasSeason || now.Ticks >= lb.seasonEndUtcTicks)
            {
                StartNewSeason(lb, now);
            }

            // If bots are missing (old saves) => regen
            if (lb.bots == null || lb.bots.Length != BotCount)
            {
                GenerateBots(lb);
                SortBotsDesc(lb);
                EnforceNeverTop10(save);
            }
        }

        private void StartNewSeason(LeaderboardState lb, DateTime nowUtc)
        {
            lb.seasonStartUtcTicks = nowUtc.Ticks;
            lb.seasonEndUtcTicks = nowUtc.AddDays(7).Ticks;

            lb.lastSimUtcTicks = nowUtc.Ticks;
            lb.seasonSeed = MakeSeed(nowUtc);
            lb.simStepIndex = 0;

            // reset player score for the new season
            lb.playerScore = 0;

            GenerateBots(lb);
            SortBotsDesc(lb);
        }

        private int MakeSeed(DateTime nowUtc)
        {
            unchecked
            {
                long t = nowUtc.Ticks;
                return (int)(t ^ (t >> 32) ^ 0x5f3759df);
            }
        }

        private void GenerateBots(LeaderboardState lb)
        {
            var rnd = new System.Random(lb.seasonSeed);

            lb.bots = new LeaderboardEntryState[BotCount];

            // База очков сезона: намеренно высокая, чтобы игрок не мог догнать
            // (и потом мы всё равно будем поддерживать SafetyMarginAbovePlayer)
            int baseTop = 6000 + rnd.Next(0, 800); // топ1

            int current = baseTop;

            for (int i = 0; i < BotCount; i++)
            {
                // Градация: каждый следующий ниже (gap 180..420)
                if (i > 0)
                    current -= rnd.Next(180, 421);

                lb.bots[i] = new LeaderboardEntryState
                {
                    nickName = MakeNick(rnd, i),
                    avatarId = rnd.Next(0, 9), // 0..8
                    avatarFrameId = rnd.Next(0, 9),
                    score = Math.Max(0, current)
                };
            }

            // На всякий случай — отсортировать
            SortBotsDesc(lb);
        }

        private string MakeNick(System.Random rnd, int index)
        {
            // Чуть разнообразия, без сложных генераторов
            string baseNick = NickPool[rnd.Next(0, NickPool.Length)];
            int suffix = rnd.Next(10, 99);
            return index < 3 ? $"{baseNick}{suffix}" : $"{baseNick}_{suffix}";
        }

        private void SimulateIfNeeded(PlayerSave save)
        {
            var lb = save.leaderboard;
            var now = _time.UtcNow;

            long last = lb.lastSimUtcTicks;
            if (last <= 0) { lb.lastSimUtcTicks = now.Ticks; return; }

            var elapsed = new TimeSpan(now.Ticks - last);
            if (elapsed < _simInterval) return;

            int steps = (int)Math.Floor(elapsed.TotalMinutes / _simInterval.TotalMinutes);
            steps = Math.Clamp(steps, 1, 48); // чтобы после долгого оффлайна не улетело в космос

            for (int s = 0; s < steps; s++)
                SimStep(lb);

            lb.lastSimUtcTicks = now.Ticks;

            SortBotsDesc(lb);
            EnforceNeverTop10(save);
        }

        private void SimStep(LeaderboardState lb)
        {
            // Детерминированный rnd на шаг (чтобы поведение было "стабильным" для сохранения)
            var rnd = new System.Random(unchecked(lb.seasonSeed + lb.simStepIndex * 1013));
            lb.simStepIndex++;

            // 1) Меняем очки у ботов (имитируем "после забегов")
            for (int i = 0; i < lb.bots.Length; i++)
            {
                int delta = rnd.Next(-60, 181); // иногда чуть падают, чаще растут
                lb.bots[i].score = Math.Max(0, lb.bots[i].score + delta);
            }

            // 2) Иногда "новый игрок" врывается (заменяем кого-то из хвоста 6..9)
            if (rnd.NextDouble() < 0.18)
            {
                int replaceIndex = rnd.Next(6, BotCount); // не трогаем топ-5 слишком часто
                int nearScore = lb.bots[replaceIndex].score + rnd.Next(-120, 220);

                lb.bots[replaceIndex] = new LeaderboardEntryState
                {
                    nickName = MakeNick(rnd, replaceIndex),
                    avatarId = rnd.Next(0, 9),
                    avatarFrameId = rnd.Next(0, 9),
                    score = Math.Max(0, nearScore)
                };
            }

            // 3) Лёгкая “подтяжка” лидера иногда
            if (rnd.NextDouble() < 0.12)
            {
                lb.bots[0].score += rnd.Next(80, 220);
            }
        }

        private void EnforceNeverTop10(PlayerSave save)
        {
            var lb = save.leaderboard;
            if (lb.bots == null || lb.bots.Length < BotCount) return;

            // Сортируем
            SortBotsDesc(lb);

            // гарантируем что 10-е место (index 9) минимум playerScore + SafetyMargin
            int min10th = lb.playerScore + SafetyMarginAbovePlayer;

            if (lb.bots[BotCount - 1].score < min10th)
            {
                int need = min10th - lb.bots[BotCount - 1].score;

                // поднимем всем хвостом (чтобы градация сохранялась)
                for (int i = BotCount - 1; i >= 0; i--)
                    lb.bots[i].score += need;

                // пересортируем на всякий
                SortBotsDesc(lb);
            }

            // сохраняем строгую убывающую градацию (чтобы визуально не было одинаковых)
            for (int i = 1; i < BotCount; i++)
            {
                if (lb.bots[i].score >= lb.bots[i - 1].score)
                    lb.bots[i].score = Math.Max(0, lb.bots[i - 1].score - 1);
            }
        }

        private void SortBotsDesc(LeaderboardState lb)
        {
            Array.Sort(lb.bots, (a, b) => b.score.CompareTo(a.score));
        }

        private int ComputePlayerRank(LeaderboardState lb)
        {
            // 10 ботов всегда выше игрока (мы это enforce'им)
            int higherBots = 0;
            for (int i = 0; i < lb.bots.Length; i++)
                if (lb.bots[i].score > lb.playerScore) higherBots++;

            // Виртуальная "популяция" сезона: 4000..9000
            // Детерминировано по seed
            int population = 4000 + (PositiveHash(lb.seasonSeed) % 5001);

            // Чем ближе игрок к 10 месту, тем лучше (меньше rank), но никогда не топ10.
            // Берём отношение playerScore / score10.
            int score10 = lb.bots[lb.bots.Length - 1].score; // 10-е место
            float p = (score10 <= 0) ? 0f : Mathf.Clamp01((float)lb.playerScore / score10);

            // p=0 => далеко внизу => rank ~ population
            // p=1 => почти догнал 10-е => rank ~ 11..200
            // Сделаем кривую: чем выше p, тем резче уменьшается rank
            float curve = 1f - Mathf.Pow(p, 0.65f);

            int baseRank = Mathf.RoundToInt(11 + curve * (population - 11));

            // Немного "дрожания" ранка от симуляции (но стабильно и в разумных пределах)
            int jitter = (PositiveHash(lb.seasonSeed + lb.simStepIndex * 97) % 41) - 20; // -20..+20

            int rank = baseRank + jitter;

            // Гарантия: никогда не станет <= 11
            if (rank < 12) rank = 12;

            // Также логично: если higherBots почему-то > 0, rank минимум higherBots+1
            if (rank < higherBots + 1) rank = higherBots + 1;

            return rank;
        }

        private static int PositiveHash(int x)
        {
            unchecked
            {
                // быстрый детерминированный "хэш"
                int h = x;
                h ^= (h << 13);
                h ^= (h >> 17);
                h ^= (h << 5);
                return h & int.MaxValue;
            }
        }
    }

    public readonly struct LeaderboardSnapshot
    {
        public readonly TimeSpan Remaining;
        public readonly int PlayerScore;
        public readonly int PlayerRank;
        public readonly LeaderboardEntryState[] Top;
        public readonly DateTime SeasonEndUtc;
        public readonly DateTime SeasonStartUtc;

        public LeaderboardSnapshot(
            TimeSpan remaining,
            int playerScore,
            int playerRank,
            LeaderboardEntryState[] top,
            DateTime seasonEndUtc,
            DateTime seasonStartUtc)
        {
            Remaining = remaining;
            PlayerScore = playerScore;
            PlayerRank = playerRank;
            Top = top;
            SeasonEndUtc = seasonEndUtc;
            SeasonStartUtc = seasonStartUtc;
        }
    }
}