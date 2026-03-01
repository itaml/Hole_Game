using System;
using System.Collections;
using UnityEngine;

#if UNITY_IOS
using Unity.Advertisement.IosSupport;
#endif

public class ATTRequestor : MonoBehaviour
{
    public static ATTRequestor Instance { get; private set; }

    public bool IsDone { get; private set; }
    public bool IsAuthorized { get; private set; }

    public event Action<bool> OnFinished; // bool = authorized

    [Header("Settings")]
    [Tooltip("Автоматически показать ATT при старте")]
    [SerializeField] private bool autoRequestOnStart = false; // Изменено: по умолчанию false

    [Tooltip("Если true — не будем ждать долго (на всякий случай), чтобы игра не зависала.")]
    [SerializeField] private bool useTimeout = true;

    [SerializeField] private float timeoutSec = 3f;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Только если autoRequestOnStart = true
        if (autoRequestOnStart)
        {
            Request();
        }
    }

    public void Request()
    {
        if (IsDone)
        {
            Debug.Log("ATT already done, skipping request");
            OnFinished?.Invoke(IsAuthorized);
            return;
        }

#if UNITY_EDITOR
        // В редакторе попапа не будет. Симулируем "Denied".
        Finish(false);
        return;
#elif UNITY_IOS
        var status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();

        // Уже решено ранее (попап показывается только 1 раз на установку)
        if (status != ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
        {
            Finish(status == ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED);
            return;
        }

        // Запрашиваем разрешение
        Debug.Log("Showing ATT popup...");
        ATTrackingStatusBinding.RequestAuthorizationTracking();

        // Ждём пока статус изменится
        StartCoroutine(WaitForATT());
#else
        // Не iOS: считаем завершенным
        Finish(false);
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    private IEnumerator WaitForATT()
    {
        float start = Time.realtimeSinceStartup;

        while (true)
        {
            var s = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
            if (s != ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
            {
                Finish(s == ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED);
                yield break;
            }

            if (useTimeout && (Time.realtimeSinceStartup - start) >= timeoutSec)
            {
                // Не дождались (редко, но бывает). Считаем как denied и идём дальше.
                Finish(false);
                yield break;
            }

            yield return null;
        }
    }
#endif

    private void Finish(bool authorized)
    {
        IsDone = true;
        IsAuthorized = authorized;
        OnFinished?.Invoke(authorized);

        Debug.Log($"[ATT] Done. authorized={authorized}");
    }
}