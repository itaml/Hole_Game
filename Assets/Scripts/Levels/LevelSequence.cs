using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hole/Level Sequence", fileName = "LevelSequence")]
public class LevelSequence : ScriptableObject
{
    public List<LevelSpawnConfig> levels = new();

    public LevelSpawnConfig Get(int levelIndex)
    {
        if (levels == null || levels.Count == 0) return null;
        levelIndex = Mathf.Clamp(levelIndex, 0, levels.Count - 1);
        return levels[levelIndex];
    }
}