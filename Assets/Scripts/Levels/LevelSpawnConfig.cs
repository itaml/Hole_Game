using System.Collections.Generic;
using UnityEngine;

public enum FormationType
{
    Circle,
    Line,
    Grid,
    CustomPoints
}

[System.Serializable]
public class SpawnGroup
{
    public ItemType type;
    public FormationType formation = FormationType.Grid;

    [Header("Transform")]
    public Vector3 center;
    public float rotationY;

    [Header("Common")]
    [Min(0f)] public float jitter = 0f;
    public int seed = 0;

    [Header("Circle")]
    [Min(1)] public int circleCount = 10;
    [Min(0f)] public float circleRadius = 5f;

    [Header("Line")]
    [Min(1)] public int lineCount = 12;
    public Vector3 lineDirection = Vector3.right;
    [Min(0f)] public float lineSpacing = 1.2f;

    [Header("Grid")]
    [Min(1)] public int rows = 6;
    [Min(1)] public int cols = 6;
    [Min(0f)] public float spacingX = 1.2f;
    [Min(0f)] public float spacingZ = 1.2f;

    [Header("Custom Points (local space)")]
    public List<Vector3> localPoints;
}

[CreateAssetMenu(menuName = "Hole/Level Spawn Config", fileName = "LevelSpawn_01")]
public class LevelSpawnConfig : ScriptableObject
{
    public List<SpawnGroup> groups = new();
}