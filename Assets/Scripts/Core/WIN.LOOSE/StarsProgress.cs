using UnityEngine;

public static class StarsProgress
{
    private const string TOTAL_KEY = "stars_total";
    private const string LEVEL_KEY_PREFIX = "level_stars_";

    public static int GetTotalStars() => Mathf.Max(0, PlayerPrefs.GetInt(TOTAL_KEY, 0));

    public static int GetLevelStars(int levelIndex)
        => Mathf.Clamp(PlayerPrefs.GetInt(LEVEL_KEY_PREFIX + levelIndex, 0), 0, 3);

    // Возвращает сколько добавили к total
    public static int ApplyLevelResult(int levelIndex, int newStars)
    {
        newStars = Mathf.Clamp(newStars, 0, 3);
        int prev = GetLevelStars(levelIndex);

        if (newStars <= prev) return 0;

        int delta = newStars - prev;
        PlayerPrefs.SetInt(LEVEL_KEY_PREFIX + levelIndex, newStars);
        PlayerPrefs.SetInt(TOTAL_KEY, GetTotalStars() + delta);
        PlayerPrefs.Save();

        return delta;
    }
}