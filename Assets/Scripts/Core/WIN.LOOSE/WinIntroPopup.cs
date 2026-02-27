using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WinIntroPopup : MonoBehaviour
{
    [Header("Where to show (Canvas/Parent)")]
    [SerializeField] private RectTransform uiParent;          // Твой Canvas или любой UI-root в Canvas

    [Header("Prefab (the window with logo)")]
    [SerializeField] private GameObject panelPrefab;          // Префаб окна (НЕ объект в сцене!)

    [Header("Firework prefab")]
    [SerializeField] private FireworkSpriteAnimUI fireworkPrefab;

    [Header("Animation")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fps = 24f;

    [Header("Timing")]
    [SerializeField] private float startDelay = 0.15f;
    [SerializeField] private float delayBetweenFireworks = 0.25f;
    [SerializeField] private float afterLastDelay = 0.35f;
    [SerializeField] private float maxPreviewTime = 2.5f;

    [Header("Firework visual fix")]
    [SerializeField] private Vector2 fireworkSize = new Vector2(420, 420); // сделай больше если надо
    [SerializeField] private float fireworkScale = 1.2f;                  // сделай 1.5-2 если надо
    [SerializeField] private bool forceFireworkSize = true;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip fireworkSfx;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

    private Coroutine _routine;
    private readonly List<GameObject> _spawned = new();

    private GameObject _panelInstance;
    private WinIntroPopupView _view;

    public void Show(Action onFinished)
    {
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError("[WinIntroPopup] GameObject inactive. Keep WinIntroPopup active.");
            return;
        }

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ShowRoutine(onFinished));
    }

    public void Hide()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = null;

        CleanupSpawned();

        if (_panelInstance) _panelInstance.SetActive(false);
    }

    private IEnumerator ShowRoutine(Action onFinished)
    {
        // базовые проверки
        if (!uiParent)
        {
            Debug.LogError("[WinIntroPopup] uiParent is NULL. Assign your Canvas (RectTransform).");
            yield break;
        }
        if (!panelPrefab)
        {
            Debug.LogError("[WinIntroPopup] panelPrefab is NULL. Assign the window prefab.");
            yield break;
        }
        if (!fireworkPrefab)
        {
            Debug.LogError("[WinIntroPopup] fireworkPrefab is NULL.");
            yield break;
        }
        if (frames == null || frames.Length == 0)
        {
            Debug.LogError("[WinIntroPopup] frames empty.");
            yield break;
        }

        EnsurePanelInstance();

        if (!_view || !_view.fireworksRoot)
        {
            Debug.LogError("[WinIntroPopup] WinIntroPopupView/fireworksRoot missing on prefab instance.");
            yield break;
        }

        _panelInstance.SetActive(true);
        _view.fireworksRoot.SetAsLastSibling(); // салюты поверх

        CleanupSpawned();

        float startedAt = Time.unscaledTime;

        if (startDelay > 0f)
            yield return new WaitForSecondsRealtime(startDelay);

        var points = (_view.spawnPoints != null) ? _view.spawnPoints : null;
        int count = (points != null && points.Count > 0) ? points.Count : 3;

        for (int i = 0; i < count; i++)
        {
            if (Time.unscaledTime - startedAt > maxPreviewTime)
                break;

            RectTransform point = (points != null && i < points.Count) ? points[i] : null;

            var fw = Instantiate(fireworkPrefab, _view.fireworksRoot);
            var rt = fw.transform as RectTransform;

            if (!rt)
            {
                Debug.LogError("[WinIntroPopup] Firework prefab must be UI (RectTransform).");
                Destroy(fw.gameObject);
                break;
            }

            if (point != null)
                CopyRect(rt, point);
            else
                PlaceCenter(rt);

            // фикс “крошечного” салюта
            if (forceFireworkSize)
            {
                rt.sizeDelta = fireworkSize;
                rt.localScale = Vector3.one * Mathf.Max(0.01f, fireworkScale);
            }

            fw.transform.SetAsLastSibling();
            _spawned.Add(fw.gameObject);

            if (audioSource && fireworkSfx)
                audioSource.PlayOneShot(fireworkSfx, sfxVolume);

            yield return fw.Play(frames, fps);

            Destroy(fw.gameObject);

            if (delayBetweenFireworks > 0f)
                yield return new WaitForSecondsRealtime(delayBetweenFireworks);
        }

        if (afterLastDelay > 0f)
            yield return new WaitForSecondsRealtime(afterLastDelay);

        CleanupSpawned();
        if (_panelInstance) _panelInstance.SetActive(false);

        onFinished?.Invoke();
        _routine = null;
    }

    private void EnsurePanelInstance()
    {
        if (_panelInstance) return;

        _panelInstance = Instantiate(panelPrefab, uiParent);
        _view = _panelInstance.GetComponent<WinIntroPopupView>();
        _panelInstance.SetActive(false);
    }

    private void CleanupSpawned()
    {
        for (int i = 0; i < _spawned.Count; i++)
            if (_spawned[i]) Destroy(_spawned[i]);

        _spawned.Clear();
    }

    private static void PlaceCenter(RectTransform rt)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private static void CopyRect(RectTransform target, RectTransform source)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.anchoredPosition = source.anchoredPosition;

        // ВАЖНО: не тащим нулевые размеры
        if (source.sizeDelta != Vector2.zero)
            target.sizeDelta = source.sizeDelta;

        if (source.localScale != Vector3.zero)
            target.localScale = source.localScale;

        target.localRotation = source.localRotation;
    }
}