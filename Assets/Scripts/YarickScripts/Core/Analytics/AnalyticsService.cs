using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Analytics;

public static class AnalyticsService
{
#if !UNITY_EDITOR
    private sealed class PendingEvent
    {
        public string name;
        public Parameter[] parameters;
    }

    private static bool subscribed;
    private static bool firebaseReady;
    private static readonly Queue<PendingEvent> pending = new Queue<PendingEvent>(64);
    private const int MAX_PENDING = 256;
#endif

    public static void LogEvent(string eventName)
        => LogEvent(eventName, null);

    public static void LogEvent(string eventName, params Parameter[] parameters)
    {
        if (string.IsNullOrWhiteSpace(eventName)) return;

        // Firebase требования: имена событий латиница/цифры/underscore, длина ограничена.
        // Лучше хотя бы чуть-чуть нормализовать:
        eventName = SanitizeEventName(eventName);

#if UNITY_EDITOR
        Debug.Log($"[Analytics][Editor] {eventName}");
        return;
#else
        EnsureHooked();

        if (!firebaseReady)
        {
            Enqueue(eventName, parameters);
            return;
        }

        SendToFirebase(eventName, parameters);
#endif
    }

    public static void SetUserProperty(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

#if UNITY_EDITOR
        Debug.Log($"[Analytics][Editor] user_property {name}={value}");
        return;
#else
        EnsureHooked();

        if (!firebaseReady)
        {
            // Если нужно — можно тоже поставить в очередь. Обычно не критично.
            return;
        }

        try
        {
            FirebaseAnalytics.SetUserProperty(name, value);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Analytics] Failed SetUserProperty {name}: {e.Message}");
        }
#endif
    }

#if !UNITY_EDITOR
    private static void EnsureHooked()
    {
        // Если FirebaseBootstrapper ещё не создан — не “фиксируем” subscribed,
        // чтобы следующая LogEvent попыталась подключиться снова.
        if (FirebaseBootstrapper.Instance == null)
            return;

        // Готово? Тогда просто отметим и флашнем
        if (FirebaseBootstrapper.Instance.IsReady)
        {
            firebaseReady = true;
            FlushPending();
            return;
        }

        // Подписка только один раз (когда Instance уже существует)
        if (subscribed) return;
        subscribed = true;
        FirebaseBootstrapper.Instance.OnReady += HandleFirebaseReady;
    }

    private static void HandleFirebaseReady()
    {
        firebaseReady = true;
        FlushPending();
    }

    private static void Enqueue(string name, Parameter[] parameters)
    {
        if (pending.Count >= MAX_PENDING)
            return;

        pending.Enqueue(new PendingEvent { name = name, parameters = parameters });
    }

    private static void FlushPending()
    {
        while (pending.Count > 0)
        {
            var e = pending.Dequeue();
            SendToFirebase(e.name, e.parameters);
        }
    }

    private static void SendToFirebase(string eventName, Parameter[] parameters)
    {
        try
        {
            if (parameters == null || parameters.Length == 0)
                FirebaseAnalytics.LogEvent(eventName);
            else
                FirebaseAnalytics.LogEvent(eventName, parameters);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Analytics] Failed to send {eventName}: {e.Message}");
        }
    }
#endif

    private static string SanitizeEventName(string s)
    {
        // простой санитайзер: пробелы -> _, нижний регистр
        // если хочешь строгий (только [a-z0-9_]) — скажи, сделаю.
        return s.Trim().Replace(' ', '_').ToLowerInvariant();
    }
}