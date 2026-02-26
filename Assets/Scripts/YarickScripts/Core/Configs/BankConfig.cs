using UnityEngine;

namespace Core.Configs
{
    [CreateAssetMenu(menuName = "Configs/BankConfig")]
    public sealed class BankConfig : ScriptableObject
    {
        public int capacity = 0; // 0 = unlimited
    }
}
