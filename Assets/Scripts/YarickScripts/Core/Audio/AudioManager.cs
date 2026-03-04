using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string musicVolumeParam = "MusicVolume";
    [SerializeField] private string sfxVolumeParam = "SFXVolume";

    [Header("Music Clips")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameMusic;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip loseSound;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    private string currentScene = "";
    private float musicVolume = 1f;
    private float sfxVolume = 1f;

    // Добавляем флаг для отслеживания, играет ли уже музыка
    private bool isMusicPlaying = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadVolumeSettings();
    }

    void Start()
    {
        // Начинаем с музыки меню
        PlayMusicForScene("Menu");
        FindAllButtons();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ИСПРАВЛЕНИЕ: Логика выбора музыки
        string sceneName = scene.name;

        // Если музыка еще не играет, начинаем с соответствующей сцене
        if (!isMusicPlaying)
        {
            PlayMusicForScene(sceneName);
        }
        else
        {
            // Музыка уже играет, проверяем нужно ли ее менять
            bool needMusicChange = false;
            AudioClip targetClip = null;

            if (sceneName == "Game" && musicSource.clip != gameMusic)
            {
                // Перешли в Game - нужна игровая музыка
                targetClip = gameMusic;
                needMusicChange = true;
            }
            else if ((sceneName == "Menu" || sceneName == "Intro") && musicSource.clip != menuMusic)
            {
                // Перешли в Menu или Intro, а играет Game музыка - меняем на Menu
                targetClip = menuMusic;
                needMusicChange = true;
            }

            if (needMusicChange && targetClip != null)
            {
                musicSource.Stop();
                musicSource.clip = targetClip;
                musicSource.Play();
                Debug.Log($"🎵 Music changed to: {targetClip.name} for scene: {sceneName}");
            }
            else
            {
                Debug.Log($"🎵 Keeping current music for scene: {sceneName}");
            }
        }

        currentScene = sceneName;
        FindAllButtons(); // Ищем кнопки в новой сцене
    }

    private void FindAllButtons()
    {
        Button[] allButtons = FindObjectsOfType<Button>(true);
        foreach (Button button in allButtons)
        {
            // Очищаем старые слушатели чтобы не добавлять по несколько раз
            button.onClick.RemoveListener(PlayButtonClick);
            button.onClick.AddListener(PlayButtonClick);
        }
        Debug.Log($"AudioManager: Добавлен звук на {allButtons.Length} кнопок");
    }

    private void PlayMusicForScene(string sceneName)
    {
        AudioClip clip;

        // Определяем нужный клип для сцены
        if (sceneName == "Game")
        {
            clip = gameMusic;
        }
        else // Menu или Intro
        {
            clip = menuMusic;
        }

        // Если уже играет этот клип, ничего не делаем
        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            isMusicPlaying = true;
            return;
        }

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.Play();
        isMusicPlaying = true;

        Debug.Log($"🎵 Playing music: {clip.name} for scene: {sceneName}");
    }

    #region Public Methods для звуков

    public void PlayButtonClick()
    {
        PlaySound(buttonClickSound);
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    #endregion

    #region Volume Control

    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        if (audioMixer != null)
        {
            float dbVolume = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
            audioMixer.SetFloat(musicVolumeParam, dbVolume);
            musicSource.volume = volume;
        }
        PlayerPrefs.SetFloat("MusicVolume", volume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        if (audioMixer != null)
        {
            float dbVolume = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
            audioMixer.SetFloat(sfxVolumeParam, dbVolume);
            sfxSource.volume = volume;
        }
        PlayerPrefs.SetFloat("SFXVolume", volume);
        PlayerPrefs.Save();
    }

    private void LoadVolumeSettings()
    {
        SetMusicVolume(PlayerPrefs.GetFloat("MusicVolume", 1f));
        SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume", 1f));
    }

    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;

    #endregion
}