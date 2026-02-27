using UnityEngine;

[CreateAssetMenu(menuName = "Hole/Level Definition", fileName = "Level_01")]
public class LevelDefinition : ScriptableObject
{
    [Header("Spawn")]
    public LevelSpawnConfig spawnConfig;

    [Header("Goals")]
    public LevelGoals goals;

    [Header("Catalog (icons + world prefabs)")]
    public ItemCatalog catalog;

    [Header("Optional")]
    [Tooltip("0 = не переопределять")]
    public float durationMinutesOverride = 0f;
}