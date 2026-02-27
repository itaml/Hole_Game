using UnityEngine;

public static class LevelProgress
{
    private const string KEY_LEVEL = "current_level_index";

    public static int CurrentLevelIndex { get; private set; } = 1;

    public static void Load()
    {
        CurrentLevelIndex = Mathf.Max(1, PlayerPrefs.GetInt(KEY_LEVEL, 1));
    }

    public static void Save()
    {
        PlayerPrefs.SetInt(KEY_LEVEL, CurrentLevelIndex);
        PlayerPrefs.Save();
    }

    public static void SetLevel(int index)
    {
        CurrentLevelIndex = Mathf.Max(1, index);
        Save();
    }

    public static void NextLevel()
    {
        CurrentLevelIndex = Mathf.Max(1, CurrentLevelIndex + 1);
        Save();
    }
}