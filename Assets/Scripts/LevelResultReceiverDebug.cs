using UnityEngine;
using GameBridge.Contracts;

public class LevelResultReceiverDebug : MonoBehaviour, ILevelResultReceiver
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void SubmitLevelResult(LevelResult result)
    {
        Debug.Log($"[LevelResult] level={result.levelIndex} outcome={result.outcome} stars={result.starsEarned} " +
                  $"spent={result.coinsSpentInGame} used: {result.buff1Used},{result.buff2Used},{result.buff3Used},{result.buff4Used}");
    }
}