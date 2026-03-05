using System;
using System.Collections.Generic;
using UnityEngine;

public enum SfxKey
{
    ButtonClick,
    Win,
    Lose,
    Purchase,
    Error,
    BombLoose,
    Item,
    Passport,
    Time,
    Size,
    Radar,
    Magnit,
    TryRevive,
    Frozen

    // добавишь свои по мере надобности
}

public class SfxClipRouter : MonoBehaviour
{
    [Serializable]
    public struct Entry
    {
        public SfxKey key;
        public AudioClip clip;
    }

    [Header("Library")]
    [SerializeField] private Entry[] clips;

    [Header("Optional")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    public static SfxClipRouter Instance { get; private set; }

    private Dictionary<SfxKey, AudioClip> _map;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        BuildMap();
    }

    private void BuildMap()
    {
        _map = new Dictionary<SfxKey, AudioClip>(32);

        if (clips == null) return;

        for (int i = 0; i < clips.Length; i++)
        {
            var e = clips[i];
            if (e.clip == null) continue;
            _map[e.key] = e.clip;
        }
    }

    public void Play(SfxKey key)
    {
        // 1) пробуем играть через AudioManager.PlaySound(clip)
        if (_map != null && _map.TryGetValue(key, out var clip) && clip != null)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySound(clip);
            return;
        }

        // 2) fallback: если ключ ButtonClick, дернем стандартный метод
        if (key == SfxKey.ButtonClick && AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();
    }

    public void Play(AudioClip clip)
    {
        if (clip == null) return;
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.PlaySound(clip);
    }
}