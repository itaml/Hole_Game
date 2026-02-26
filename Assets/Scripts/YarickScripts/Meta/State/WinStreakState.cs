using System;

namespace Meta.State
{
    [Serializable]
    public sealed class WinStreakState
    {
        public int currentStreak = 0;
        public bool bonusBagArmed = false;
    }
}
