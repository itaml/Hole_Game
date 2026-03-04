using System;

namespace Meta.State
{
    [Serializable]
    public sealed class LeaderboardState
    {
        // Season
        public long seasonStartUtcTicks;
        public long seasonEndUtcTicks;

        // Simulation
        public long lastSimUtcTicks;
        public int seasonSeed;
        public int simStepIndex;

        // Player
        public int playerScore;

        // We keep 10 bots to guarantee: player never reaches top10
        public LeaderboardEntryState[] bots = Array.Empty<LeaderboardEntryState>();

        public bool HasSeason => seasonEndUtcTicks > 0;
    }
}