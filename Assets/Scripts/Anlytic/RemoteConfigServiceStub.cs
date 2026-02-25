using System.Collections.Generic;
using UnityEngine;

public class RemoteConfigServiceStub : IRemoteConfigService
{
    // Тут можно “подменять” значения в инспекторе через ScriptableObject,
    // но пока просто словарь.
    private readonly Dictionary<string, object> _values = new()
    {
        { RemoteKeys.IAP_DISABLE, false },
        { RemoteKeys.REVIVE_MAX_PER_RUN, 2 },
        { RemoteKeys.WIN_MULT_VALUE, 2 },
        { RemoteKeys.CHEST_COOLDOWN_SEC, 300 },
        { RemoteKeys.CHEST_REWARD_VALUE, 50 },
    };

    public bool GetBool(string key, bool defaultValue)
        => _values.TryGetValue(key, out var v) && v is bool b ? b : defaultValue;

    public int GetInt(string key, int defaultValue)
        => _values.TryGetValue(key, out var v) && v is int i ? i : defaultValue;

    public void FetchAndActivate(System.Action onDone = null)
    {
        Debug.Log("[RC] FetchAndActivate (stub) done.");
        onDone?.Invoke();
    }
}