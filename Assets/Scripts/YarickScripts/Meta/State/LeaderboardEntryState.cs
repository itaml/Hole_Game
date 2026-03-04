using System;

namespace Meta.State
{
    [Serializable]
    public sealed class LeaderboardEntryState
    {
        public string nickName;
        public int avatarId;      // 0..8
        public int avatarFrameId; // любой int (или тоже 0..8 Ч как решишь)
        public int score;
    }
}