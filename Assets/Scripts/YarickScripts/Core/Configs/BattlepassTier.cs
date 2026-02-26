using System;

namespace Core.Configs
{
    [Serializable]
    public sealed class BattlepassTier
    {
        public int needItems = 10;
        public Reward reward = new Reward();
    }
}
