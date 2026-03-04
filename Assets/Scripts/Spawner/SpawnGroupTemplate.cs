using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum ModuleTag
{
    None = 0,
    StartSafe = 1 << 0,
    Dense = 1 << 1,
    Narrow = 1 << 2,
    Teaching = 1 << 3,
    Chaos = 1 << 4,
    Relax = 1 << 5,
    Risk = 1 << 6,
    Priority = 1 << 7,

    Everything = ~0
}

[Serializable]
public class SpawnGroupTemplate
{
    [Header("Type")]
    public ItemType type;
    public bool useTemplateSizing = true; 

    [Tooltip("Если включено — type будет выбран случайно из GoalPool текущей темы (Japan/English/etc).")]
    public bool randomTypeFromGoalPool = false;

    [Header("Formation")]
    public FormationType formation = FormationType.Grid;

    [Header("Local Transform (module space)")]
    public Vector3 localCenter;
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