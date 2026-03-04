using System;

namespace Meta.State
{
    [Serializable]
    public sealed class TutorialState
    {
        // One-time "welcome rewards" for newly unlocked features
        public bool buff1Granted;
        public bool buff2Granted;
        public bool buff3Granted;
        public bool buff4Granted;

        public bool boost1Granted;
        public bool boost2Granted;

        // Start-popup tutorial queue (0 = none)
        public int pendingStartTutorialId;

        // One-time shown flags for start tutorials
        public bool winStreakStartTutorialShown;

        public bool boost1StartTutorialShown;
        public bool boost2StartTutorialShown;

        // Post-win tutorial queue (0 = none). Used for tutorials that should appear
        // after win cinematic / reward flow finishes.
        public int pendingPostWinTutorialId;

        // One-time shown flags for post-win tutorials
        public bool profilePostWinTutorialShownProfile;

        public long dailyBonusLastClaimUtcTicks;

        public bool leaderboardUnlockTutorialShown;
        public bool battlepassUnlockTutorialShown;
    }
}
