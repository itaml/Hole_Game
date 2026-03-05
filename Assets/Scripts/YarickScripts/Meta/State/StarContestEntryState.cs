using System;

namespace Meta.State
{
    [Serializable]
    public sealed class StarContestEntryState
    {
        public string nickName;
        public int avatarId;      // 0..8
        public int avatarFrameId; // 0..8
        public int stars;         // score in Star Contest
        public bool isPlayer;     // for UI highlight if needed
    }
}