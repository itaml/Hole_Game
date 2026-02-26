using UnityEngine;

namespace Core.Configs
{
    [CreateAssetMenu(menuName = "Configs/EconomyConfig")]
    public sealed class EconomyConfig : ScriptableObject
    {
        public int lifeRestoreSeconds = 900; // 15 min default
        public int buyLifeCostCoins = 100;
    }
}
