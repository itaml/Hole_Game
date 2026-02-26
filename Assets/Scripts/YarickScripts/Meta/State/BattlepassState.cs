using System;

namespace Meta.State
{
    [Serializable]
    public sealed class BattlepassState
    {
        public long seasonStartUtcTicks = 0;
        public int tier = 0;
        public int tierProgress = 0;
    }
}
