using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HoleGrowth : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private HolePhysics holePhysics;
    [SerializeField] private Transform holeVisualRoot; // что скейлим (меш/объект дыры)

    [Header("UI")]
    [SerializeField] private TMP_Text sizeText;   // "Size 3"
    [SerializeField] private Slider xpSlider;     // прогресс XP

    [Header("Growth")]
    [SerializeField] private int startSize = 1;
    [SerializeField] private float startVisualScale = 1.0f;  // scale при Size1
    [SerializeField] private float scalePerSize = 0.12f;     // +scale за каждый size

    [Tooltip("Физический радиус дыры при Size1. Дальше растёт пропорционально scale.")]
    [SerializeField] private float baseHoleRadius = 1.2f;    // <-- ВАЖНО: настроишь под себя

    [Tooltip("XP для апа каждого следующего Size. Индекс = (size-1).")]
    [SerializeField] private int[] xpToNextSize = { 10, 25, 45, 70, 100 };

    [Header("Level Up Scale Punch")]
    [SerializeField] private float punchScaleMultiplier = 1.15f;
    [SerializeField] private float punchUpTime = 0.08f;
    [SerializeField] private float punchDownTime = 0.15f;

    private float _tempScaleMultiplier = 1f;

    private int _size;
    private int _xp;
    private Coroutine _punchRoutine;

    public int SizeLevel => _size;
    public int CurrentXp => _xp;

    private void Awake()
    {
        if (holeVisualRoot == null)
            holeVisualRoot = transform;

        if (holePhysics == null)
            holePhysics = GetComponentInChildren<HolePhysics>();
    }

    public void ResetRun()
    {
        _size = Mathf.Max(1, startSize);
        _xp = 0;

        ApplyScaleAndRadius();
        UpdateSizeText();
        UpdateSlider();
    }

    public void AddXp(int amount)
    {
        if (amount <= 0) return;

        _xp += amount;

        bool leveledUp = false;

        while (CanLevelUp())
        {
            _xp -= GetNeedXp();
            LevelUp();
            leveledUp = true;
        }

        if (!leveledUp)
            UpdateSlider();
    }

    public void SetTempScaleMultiplier(float multiplier)
{
    _tempScaleMultiplier = Mathf.Clamp(multiplier, 0.01f, 10f);
    ApplyScaleAndRadius();
}

public void ClearTempScaleMultiplier()
{
    _tempScaleMultiplier = 1f;
    ApplyScaleAndRadius();
}

    // ---------------- Internal ----------------

    private void LevelUp()
    {
        _size++;

        ApplyScaleAndRadius();
        UpdateSizeText();
        UpdateSlider();

        PlayLevelUpEffects();
    }

    private void PlayLevelUpEffects()
    {
        if (_punchRoutine != null)
            StopCoroutine(_punchRoutine);

        _punchRoutine = StartCoroutine(ScalePunch());
    }

    private IEnumerator ScalePunch()
    {
        if (holeVisualRoot == null) yield break;

        Vector3 baseScale = holeVisualRoot.localScale;
        Vector3 punchScale = baseScale * punchScaleMultiplier;

        float t = 0f;

        while (t < punchUpTime)
        {
            t += Time.deltaTime;
            float k = punchUpTime <= 0f ? 1f : Mathf.Clamp01(t / punchUpTime);
            holeVisualRoot.localScale = Vector3.Lerp(baseScale, punchScale, k);
            yield return null;
        }

        t = 0f;

        while (t < punchDownTime)
        {
            t += Time.deltaTime;
            float k = punchDownTime <= 0f ? 1f : Mathf.Clamp01(t / punchDownTime);
            holeVisualRoot.localScale = Vector3.Lerp(punchScale, baseScale, k);
            yield return null;
        }

        holeVisualRoot.localScale = baseScale;
        _punchRoutine = null;
    }

    private bool CanLevelUp()
    {
        int need = GetNeedXp();
        return _xp >= need;
    }

    private int GetNeedXp()
    {
        if (xpToNextSize == null || xpToNextSize.Length == 0)
            return int.MaxValue;

        int idx = Mathf.Clamp(_size - 1, 0, xpToNextSize.Length - 1);
        return Mathf.Max(1, xpToNextSize[idx]);
    }

    private float GetVisualScaleFactor()
    {
        // Size1 -> startVisualScale, Size2 -> startVisualScale + scalePerSize, ...
        return startVisualScale + (_size - 1) * scalePerSize;
    }

private void ApplyScaleAndRadius()
{
    float s = GetVisualScaleFactor() * _tempScaleMultiplier;

    holeVisualRoot.localScale = new Vector3(s, 1f, s);

    if (holePhysics != null)
    {
        float radius = baseHoleRadius * s;
        holePhysics.SetHoleRadius(radius);
    }
}

    private void UpdateSizeText()
    {
        if (sizeText)
            sizeText.text = $"Size {_size}";
    }

    private void UpdateSlider()
    {
        if (!xpSlider) return;

        int need = GetNeedXp();
        xpSlider.maxValue = need;
        xpSlider.value = Mathf.Clamp(_xp, 0, need);
    }
}