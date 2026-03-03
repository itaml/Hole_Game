using UnityEngine;

[RequireComponent(typeof(AbsorbablePhysicsItem))]
public class BoostPickupItem : MonoBehaviour
{
    [Header("Type")]
    [SerializeField] private BoostPickupType boostType = BoostPickupType.GrowWholeLevel;

    [Header("Source")]
    [SerializeField] private BoostSpawnSource source = BoostSpawnSource.Spawner;

    [Header("Time values (only for ExtraLevelTime)")]
    [SerializeField] private float spawnerAddSeconds = 5f;
    [SerializeField] private float battlepassAddSeconds = 20f;

    public BoostPickupType Type => boostType;
    public BoostSpawnSource Source => source;

    public void SetSource(BoostSpawnSource s) => source = s;

    public float GetAddSeconds()
    {
        return source == BoostSpawnSource.Battlepass ? battlepassAddSeconds : spawnerAddSeconds;
    }
}