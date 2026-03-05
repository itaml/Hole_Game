using UnityEngine;

namespace Core.Configs
{
    [CreateAssetMenu(menuName = "Configs/BountyConfig", fileName = "BountyConfig")]
    public sealed class BountyConfig : ScriptableObject
    {
        [Min(1)] public int refreshDays = 2;
        public Reward[] possibleRewards;
    }
}