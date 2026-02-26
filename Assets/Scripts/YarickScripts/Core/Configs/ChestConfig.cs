using UnityEngine;

namespace Core.Configs
{
    [CreateAssetMenu(menuName = "Configs/ChestConfig")]
    public sealed class ChestConfig : ScriptableObject
    {
        public int threshold = 20;
        public Reward[] possibleRewards;
    }
}
