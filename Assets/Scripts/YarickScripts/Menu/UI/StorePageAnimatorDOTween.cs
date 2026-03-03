using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class StorePageAnimatorDOTween : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject storePageRoot;   // StorePage (Image на весь экран)
    [SerializeField] private CanvasGroup pageGroup;      // CanvasGroup на StorePage
    [SerializeField] private RectTransform pageRect;     // RectTransform StorePage

    [Header("Header")]
    [SerializeField] private RectTransform headerRect;   // HeaderImg
    [SerializeField] private CanvasGroup headerGroup;    // CanvasGroup на HeaderImg (можно null)
    [SerializeField] private float headerFromY = 120f;

    [Header("Scroll")]
    [SerializeField] private RectTransform scrollRect;   // Scroll View (или контейнер)
    [SerializeField] private CanvasGroup scrollGroup;    // CanvasGroup на Scroll View (можно null)
    [SerializeField] private float scrollFromY = -80f;

    [Header("Items (stagger)")]
    [SerializeField] private RectTransform contentRoot;  // Content внутри ScrollView
    [SerializeField] private float itemFromY = -30f;
    [SerializeField] private float itemStagger = 0.04f;

    [Header("Timings")]
    [SerializeField] private float openDuration = 0.35f;
    [SerializeField] private float closeDuration = 0.25f;
    [SerializeField] private float headerDuration = 0.25f;
    [SerializeField] private float scrollDuration = 0.25f;
    [SerializeField] private float itemDuration = 0.22f;

    [Header("Scale")]
    [SerializeField] private float openFromScale = 0.96f;
    [SerializeField] private float openOvershoot = 1.02f;

    [Header("Speed")]
    [SerializeField, Range(0.5f, 3f)]
    private float speedMultiplier = 1.5f; // 1 = normal, 2 = 2x faster, 3 = very fast

    [Header("Ease")]
    [SerializeField] private Ease openEase = Ease.OutCubic;
    [SerializeField] private Ease closeEase = Ease.InCubic;
    [SerializeField] private Ease moveEase = Ease.OutCubic;
    [SerializeField] private Ease itemEase = Ease.OutCubic;

    private Sequence _seq;
    private bool _isOpen;

    private Vector2 _pagePos0;
    private Vector2 _headerPos0;
    private Vector2 _scrollPos0;

    private readonly List<RectTransform> _items = new();
    private readonly List<CanvasGroup> _itemGroups = new();
    private readonly List<Vector2> _itemPos0 = new();

    private void Awake()
    {
        if (storePageRoot == null) storePageRoot = gameObject;
        if (pageRect == null) pageRect = storePageRoot.GetComponent<RectTransform>();
        if (pageGroup == null) pageGroup = storePageRoot.GetComponent<CanvasGroup>();
        if (pageGroup == null) pageGroup = storePageRoot.AddComponent<CanvasGroup>();

        if (headerRect != null && headerGroup == null)
            headerGroup = EnsureCanvasGroup(headerRect.gameObject);

        if (scrollRect != null && scrollGroup == null)
            scrollGroup = EnsureCanvasGroup(scrollRect.gameObject);

        _pagePos0 = pageRect.anchoredPosition;
        if (headerRect != null) _headerPos0 = headerRect.anchoredPosition;
        if (scrollRect != null) _scrollPos0 = scrollRect.anchoredPosition;

        CacheItems();
    }

    private CanvasGroup EnsureCanvasGroup(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        return cg != null ? cg : go.AddComponent<CanvasGroup>();
    }

    private void CacheItems()
    {
        _items.Clear();
        _itemGroups.Clear();
        _itemPos0.Clear();

        if (contentRoot == null) return;

        // Важно: сначала дать Layout пересчитать позиции (особенно если что-то скрыли)
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);

        for (int i = 0; i < contentRoot.childCount; i++)
        {
            var rt = contentRoot.GetChild(i) as RectTransform;
            if (rt == null) continue;

            // ✅ пропускаем выключенные
            if (!rt.gameObject.activeInHierarchy) continue;

            // ✅ пропускаем элементы, которые не участвуют в layout
            var le = rt.GetComponent<LayoutElement>();
            if (le != null && le.ignoreLayout) continue;

            var cg = rt.GetComponent<CanvasGroup>();
            if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();

            _items.Add(rt);
            _itemGroups.Add(cg);
            _itemPos0.Add(rt.anchoredPosition);
        }
    }

    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;

        KillSequence();

        storePageRoot.SetActive(true);   // ✅ сначала включаем

        CacheItems();
        KillSequence();

        storePageRoot.SetActive(true);

        // speed-scaled durations
        float od = openDuration / speedMultiplier;
        float hd = headerDuration / speedMultiplier;
        float sd = scrollDuration / speedMultiplier;
        float id = itemDuration / speedMultiplier;
        float stagger = itemStagger / speedMultiplier;

        // block input while animating
        pageGroup.interactable = false;
        pageGroup.blocksRaycasts = true; // блокируем клики в HomePage под ним

        // initial states
        pageGroup.alpha = 0f;
        pageRect.localScale = Vector3.one * openFromScale;

        if (headerRect != null)
        {
            headerGroup.alpha = 0f;
            headerRect.anchoredPosition = _headerPos0 + new Vector2(0, headerFromY);
        }

        if (scrollRect != null)
        {
            scrollGroup.alpha = 0f;
            scrollRect.anchoredPosition = _scrollPos0 + new Vector2(0, scrollFromY);
        }

        for (int i = 0; i < _items.Count; i++)
        {
            _itemGroups[i].alpha = 0f;
            _items[i].anchoredPosition = _itemPos0[i] + new Vector2(0, itemFromY);
        }

        _seq = DOTween.Sequence().SetUpdate(true);

        // Page fade + scale (с overshoot)
        _seq.Join(pageGroup.DOFade(1f, od).SetEase(openEase));
        _seq.Join(pageRect.DOScale(openOvershoot, od).SetEase(openEase));
        _seq.Append(pageRect.DOScale(1f, 0.12f / speedMultiplier).SetEase(Ease.OutQuad));

        // Header
        if (headerRect != null)
        {
            _seq.AppendInterval(0.04f / speedMultiplier);
            _seq.Join(headerGroup.DOFade(1f, hd).SetEase(openEase));
            _seq.Join(headerRect.DOAnchorPos(_headerPos0, hd).SetEase(moveEase));
        }

        // Scroll
        if (scrollRect != null)
        {
            _seq.AppendInterval(0.06f / speedMultiplier);
            _seq.Join(scrollGroup.DOFade(1f, sd).SetEase(openEase));
            _seq.Join(scrollRect.DOAnchorPos(_scrollPos0, sd).SetEase(moveEase));
        }

        // Items stagger (каскадом)
        if (_items.Count > 0)
        {
            _seq.AppendInterval(0.05f / speedMultiplier);

            // фикс: базовое время для Insert (чтобы не зависеть от _seq.Duration() в цикле)
            float baseTime = _seq.Duration();

            for (int i = 0; i < _items.Count; i++)
            {
                int idx = i;
                float at = baseTime + idx * stagger;

                _seq.Insert(at, _itemGroups[idx].DOFade(1f, id).SetEase(itemEase));
                _seq.Insert(at, _items[idx].DOAnchorPos(_itemPos0[idx], id).SetEase(itemEase));
            }
        }

        _seq.OnComplete(() =>
        {
            pageGroup.interactable = true;
            pageGroup.blocksRaycasts = true;
        });
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;

        CacheItems();
        KillSequence();

        float cd = closeDuration / speedMultiplier;

        pageGroup.interactable = false;
        pageGroup.blocksRaycasts = true;

        _seq = DOTween.Sequence().SetUpdate(true);

        // items out quickly (optional)
        for (int i = 0; i < _items.Count; i++)
            _seq.Join(_itemGroups[i].DOFade(0f, cd * 0.7f).SetEase(closeEase));

        // header/scroll out in parallel
        if (headerRect != null)
        {
            _seq.Join(headerGroup.DOFade(0f, cd * 0.85f).SetEase(closeEase));
            _seq.Join(headerRect.DOAnchorPos(_headerPos0 + new Vector2(0, headerFromY * 0.4f), cd * 0.85f).SetEase(closeEase));
        }

        if (scrollRect != null)
        {
            _seq.Join(scrollGroup.DOFade(0f, cd * 0.85f).SetEase(closeEase));
            _seq.Join(scrollRect.DOAnchorPos(_scrollPos0 + new Vector2(0, scrollFromY * 0.4f), cd * 0.85f).SetEase(closeEase));
        }

        // page fade + scale down
        _seq.Join(pageGroup.DOFade(0f, cd).SetEase(closeEase));
        _seq.Join(pageRect.DOScale(openFromScale, cd).SetEase(closeEase));

        _seq.OnComplete(() =>
        {
            // reset positions to be safe
            pageRect.localScale = Vector3.one;
            pageGroup.alpha = 1f;

            if (headerRect != null) headerRect.anchoredPosition = _headerPos0;
            if (scrollRect != null) scrollRect.anchoredPosition = _scrollPos0;

            storePageRoot.SetActive(false);
        });
    }

    private void KillSequence()
    {
        if (_seq != null && _seq.IsActive())
            _seq.Kill();
        _seq = null;

        if (pageRect != null) pageRect.DOKill();
        if (pageGroup != null) pageGroup.DOKill();
        if (headerRect != null) headerRect.DOKill();
        if (headerGroup != null) headerGroup.DOKill();
        if (scrollRect != null) scrollRect.DOKill();
        if (scrollGroup != null) scrollGroup.DOKill();

        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i] != null) _items[i].DOKill();
            if (_itemGroups[i] != null) _itemGroups[i].DOKill();
        }
    }
}