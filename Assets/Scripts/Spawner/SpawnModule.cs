using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hole/ProcGen/Spawn Module", fileName = "SpawnModule_")]
public class SpawnModule : ScriptableObject
{
    public enum ModuleShape { Grid, Circle }
public ModuleShape shape = ModuleShape.Grid;
    [Header("Meta")]
    public string id = "module";
    public ModuleTag tags = ModuleTag.None;

    [Header("Budget Cost")]
    public int cost = 2;

    [Header("Size (module footprint, world units)")]
    public Vector2 size = new Vector2(10f, 10f);

    [Header("Content (templates in module space)")]
    public List<SpawnGroupTemplate> groups = new();
}