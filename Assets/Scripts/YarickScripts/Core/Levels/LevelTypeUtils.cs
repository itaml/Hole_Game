using UnityEngine;

namespace Core.Levels
{
    public static class LevelTypeUtils
    {
        public static bool IsMasterLevel(int levelIndex)
        {
            // Every 10th level: 10, 20, 30...
            return levelIndex > 0 && levelIndex % 10 == 0;
        }

        public static bool IsChallengeLevel(int levelIndex)
        {
            // Every 5th level but not 10th: 5, 15, 25...
            return levelIndex > 0 && levelIndex % 5 == 0 && levelIndex % 10 != 0;
        }
    }
}
