using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HoleGrowth : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private HolePhysics holePhysics;
    [SerializeField] private Transform holeVisualRoot;

    [Header("UI")]
    [SerializeField] private TMP_Text sizeText;
    [SerializeField] private Slider xpSlider;

    [Header("Growth")]
    [SerializeField] private int startSize = 1;
    [SerializeField] private float startVisualScale = 1.0f;
    [SerializeField] private float scalePerSize = 0.12f;

    [Tooltip("Физический радиус дыры при Size1. Дальше растёт пропорционально scale.")]
    [SerializeField] private float baseHoleRadius = 1.2f;

    [Tooltip("XP для апа каждого следующего Size. Индекс = (size-1).")]
    [SerializeField] private int[] xpToNextSize = { 10, 25, 45, 70, 100 };

    [Header("Level Up Scale Punch")]
    [SerializeField] private float punchScaleMultiplier = 1.15f;
    [SerializeField] private float punchUpTime = 0.08f;
    [SerializeField] private float punchDownTime = 0.15f;

    [Header("Level Up FX Spawn")]
    [SerializeField] private Transform levelUpFxRoot;
    [SerializeField] private GameObject sizeUpSpritePrefab;
    [SerializeField] private GameObject levelUpParticlePrefab;

    [Header("Size Up Sprite Animation")]
    [SerializeField] private Vector3 spriteLocalOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private float spriteStartScale = 0.2f;
    [SerializeField] private float spriteOvershootScale = 1.15f;
    [SerializeField] private float spriteFinalScale = 1f;
    [SerializeField] private float spriteEnterDuration = 0.18f;
    [SerializeField] private float spriteSettleDuration = 0.10f;
    [SerializeField] private float spriteHoldDuration = 0.18f;
    [SerializeField] private float spriteFlyUpDistance = 1.4f;
    [SerializeField] private float spriteFlyUpDuration = 0.32f;
    [SerializeField] private float spriteFadeOutDuration = 0.22f;
    [SerializeField] private float spriteWobbleAngle = 8f;

    [Header("Particles")]
    [SerializeField] private float particleAutoDestroyDelay = 2.5f;

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

        if (levelUpFxRoot == null)
            levelUpFxRoot = transform;
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
        SfxClipRouter.Instance?.Play(SfxKey.Item);

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

SpawnSizeUpSpriteFx();
        SpawnLevelUpParticles();

        SfxClipRouter.Instance?.Play(SfxKey.Size);
    }

 private void SpawnSizeUpSpriteFx()
{
    if (sizeUpSpritePrefab == null)
    {
        Debug.LogError("SizeUp prefab NULL");
        return;
    }

    Transform parent = levelUpFxRoot != null ? levelUpFxRoot : transform;

    GameObject fx = Instantiate(sizeUpSpritePrefab);
    
    // позиция
    fx.transform.position = parent.position + new Vector3(0f, 2f, 0f);

    // нормальный размер
    fx.transform.localScale = Vector3.one * 1.5f;

    // чтобы точно был поверх
    SpriteRenderer sr = fx.GetComponent<SpriteRenderer>();
    if (sr != null)
    {
        sr.sortingOrder = 500;
        Color c = sr.color;
        c.a = 1f;
        sr.color = c;
    }

    Debug.Log("SIZE UP SPAWNED");
}
    private void SpawnLevelUpParticles()
    {
        if (levelUpParticlePrefab == null)
            return;

        Transform parent = levelUpFxRoot != null ? levelUpFxRoot : transform;
        GameObject particles = Instantiate(levelUpParticlePrefab, parent);
        particles.transform.localPosition = spriteLocalOffset;
        particles.transform.localRotation = Quaternion.identity;
        particles.transform.localScale = Vector3.one;

        ParticleSystem[] systems = particles.GetComponentsInChildren<ParticleSystem>(true);
        if (systems.Length > 0)
        {
            float maxLife = 0f;

            for (int i = 0; i < systems.Length; i++)
            {
                systems[i].Play();

                var main = systems[i].main;
                float duration = main.duration;

                float lifetime = 0f;
                if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
                    lifetime = main.startLifetime.constantMax;
                else if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
                    lifetime = main.startLifetime.constant;
                else
                    lifetime = 1f;

                maxLife = Mathf.Max(maxLife, duration + lifetime);
            }

            Destroy(particles, Mathf.Max(0.5f, maxLife));
        }
        else
        {
            Destroy(particles, particleAutoDestroyDelay);
        }
    }

    private IEnumerator ScalePunch()
    {
        if (holeVisualRoot == null) yield break;

        Vector3 baseScale = holeVisualRoot.localScale;
        Vector3 punchScale = new Vector3(
            baseScale.x * punchScaleMultiplier,
            baseScale.y,
            baseScale.z * punchScaleMultiplier
        );

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

    public void AddSizeLevels(int addLevels)
    {
        if (addLevels <= 0) return;

        for (int i = 0; i < addLevels; i++)
            LevelUp();
    }
}