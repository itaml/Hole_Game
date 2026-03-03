using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hole/ProcGen/Spawn Module", fileName = "SpawnModule_")]
public class SpawnModule : ScriptableObject
{
    [Header("Meta")]
    public string id = "module";
    public ModuleTag tags = ModuleTag.None;

    [Header("Budget Cost")]
    [Min(0)] public int cost = 1;

    [Header("Size (module footprint, world units)")]
    public Vector2 size = new Vector2(10f, 10f);

    [Header("Content (as templates in module space)")]
    public List<SpawnGroupTemplate> groups = new();
}