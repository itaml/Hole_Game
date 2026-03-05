using System;
using UnityEngine;

namespace Core.Configs
{
    [CreateAssetMenu(menuName = "Configs/Dual Battlepass Config")]
    public sealed class DualBattlepassConfig : ScriptableObject
    {
        public int seasonDays = 14;
        public DualBattlepassTier[] tiers;
    }

    [Serializable]
    public sealed class DualBattlepassTier
    {
        public int needWins;
        public Reward freeReward;
        public Reward premiumReward;
    }
}