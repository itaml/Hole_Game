using UnityEngine;

namespace Core.Levels
{
    /// <summary>
    /// If player loses a Master/Challenge level once, this level becomes "burned":
    /// no Master/Challenge bonuses and UI markers anymore for this level.
    /// Stored per-device/profile via PlayerPrefs.
    /// </summary>
    public static class SpecialLevelBurnStorage
    {
        private static string Key(int levelIndex) => $"SPECIAL_LEVEL_BURN_{levelIndex}";

        public static bool IsBurned(int levelIndex)
        {
            return PlayerPrefs.GetInt(Key(levelIndex), 0) == 1;
        }

        public static void Burn(int levelIndex)
        {
            PlayerPrefs.SetInt(Key(levelIndex), 1);
            PlayerPrefs.Save();
        }
    }
}
