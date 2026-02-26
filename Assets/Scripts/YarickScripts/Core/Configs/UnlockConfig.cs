using UnityEngine;

namespace Core.Configs
{
    [CreateAssetMenu(menuName = "Configs/UnlockConfig")]
    public sealed class UnlockConfig : ScriptableObject
    {
        public int starsChestUnlockLevel = 1;
        public int levelsChestUnlockLevel = 1;

        public int buff1UnlockLevel = 4;
        public int buff2UnlockLevel = 6;
        public int buff3UnlockLevel = 8;
        public int buff4UnlockLevel = 10;

        public int bankUnlockLevel = 6;
        public int battlepassUnlockLevel = 12;

        public int boost1UnlockLevel = 14;
        public int boost2UnlockLevel = 16;

        public int winStreakUnlockLevel = 20;
        public int interstitialAdsUnlockLevel = 10;
    }
}
