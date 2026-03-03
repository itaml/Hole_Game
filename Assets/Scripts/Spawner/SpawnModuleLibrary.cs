using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hole/ProcGen/Module Library", fileName = "ModuleLibrary")]
public class SpawnModuleLibrary : ScriptableObject
{
    public List<SpawnModule> modules = new();
}