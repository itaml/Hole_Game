using UnityEngine;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private bool isMusicSlider = true; // false для звуков

    void Start()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        // Устанавливаем значение из AudioManager
        if (AudioManager.Instance != null)
        {
            float currentVolume = isMusicSlider ?
                AudioManager.Instance.GetMusicVolume() :
                AudioManager.Instance.GetSFXVolume();

            // Устанавливаем значение без вызова события
            slider.SetValueWithoutNotify(currentVolume);
        }

        // Подписываемся на изменение
        slider.onValueChanged.AddListener(OnVolumeChanged);
    }

    void OnVolumeChanged(float value)
    {
        if (AudioManager.Instance == null) return;

        if (isMusicSlider)
            AudioManager.Instance.SetMusicVolume(value);
        else
            AudioManager.Instance.SetSFXVolume(value);
    }

    void OnDestroy()
    {
        if (slider != null)
            slider.onValueChanged.RemoveListener(OnVolumeChanged);
    }
}