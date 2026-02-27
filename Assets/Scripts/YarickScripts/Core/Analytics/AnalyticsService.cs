using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Analytics;

public static class AnalyticsService
{
    public static void LogEvent(string eventName, params (string key, object value)[] parameters)
    {
        if (parameters == null || parameters.Length == 0)
        {
            FirebaseAnalytics.LogEvent(eventName);
            return;
        }

        var firebaseParams = new List<Parameter>();

        foreach (var (key, value) in parameters)
        {
            switch (value)
            {
                case int i:
                    firebaseParams.Add(new Parameter(key, i));
                    break;
                case long l:
                    firebaseParams.Add(new Parameter(key, l));
                    break;
                case float f:
                    firebaseParams.Add(new Parameter(key, f));
                    break;
                case double d:
                    firebaseParams.Add(new Parameter(key, d));
                    break;
                case string s:
                    firebaseParams.Add(new Parameter(key, s));
                    break;
                case bool b:
                    firebaseParams.Add(new Parameter(key, b ? 1 : 0));
                    break;
                default:
                    firebaseParams.Add(new Parameter(key, value.ToString()));
                    break;
            }
        }

        FirebaseAnalytics.LogEvent(eventName, firebaseParams.ToArray());
        Debug.Log($"[Analytics] {eventName}");
    }
}