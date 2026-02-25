using System.Collections.Generic;
using UnityEngine;

public interface IAnalyticsService
{
    void LogEvent(string eventName, Dictionary<string, object> parameters = null);
}

public class AnalyticsServiceStub : IAnalyticsService
{
    public void LogEvent(string eventName, Dictionary<string, object> parameters = null)
    {
        if (parameters == null || parameters.Count == 0)
        {
            Debug.Log($"[ANALYTICS] {eventName}");
            return;
        }

        string payload = "";
        foreach (var kv in parameters) payload += $"{kv.Key}={kv.Value}, ";
        Debug.Log($"[ANALYTICS] {eventName} | {payload}");
    }
}