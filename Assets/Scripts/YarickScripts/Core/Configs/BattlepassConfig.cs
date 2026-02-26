using UnityEngine;

namespace Core.Configs
{
    [CreateAssetMenu(menuName = "Configs/BattlepassConfig")]
    public sealed class BattlepassConfig : ScriptableObject
    {
        public int seasonDays = 7;
        public BattlepassTier[] tiers;
    }
}
