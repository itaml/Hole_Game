using System;

namespace Meta.State
{
    [Serializable]
    public sealed class StarContestState
    {
        // Unlock marker: so we can “create on unlock when enter menu”
        public bool unlockedOnce;

        // Season (daily)
        public long seasonStartUtcTicks;
        public long seasonEndUtcTicks;

        // Simulation
        public long lastSimUtcTicks;
        public int seasonSeed;
        public int simStepIndex;

        // Player
        public int playerStars;

        // Star Contest win streak (separate from ProfileState.currentWinStreak)
        public int winStreak;
        public int multiplier; // cached for quick UI (1..10)

        // Bots
        public StarContestEntryState[] bots = Array.Empty<StarContestEntryState>();

        // Rewards safety: pay once per season
        public bool seasonRewardPaid;

        public bool HasSeason => seasonEndUtcTicks > 0;
    }
}