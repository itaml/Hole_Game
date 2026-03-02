using UnityEngine;
using UnityEngine.UI;

public class VolumeToggleButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private Image icon;

    [Header("Sprites")]
    [SerializeField] private Sprite enabledSprite;
    [SerializeField] private Sprite disabledSprite;

    [Header("Settings")]
    [SerializeField] private bool isMusic = true; // false = SFX

    private bool isEnabled;

    void Start()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (icon == null)
            icon = GetComponent<Image>();

        if (AudioManager.Instance != null)
        {
            float volume = isMusic
                ? AudioManager.Instance.GetMusicVolume()
                : AudioManager.Instance.GetSFXVolume();

            isEnabled = volume > 0.01f;
        }
        else
        {
            isEnabled = true;
        }

        RefreshVisual();
        button.onClick.AddListener(Toggle);
    }

    private void Toggle()
    {
        isEnabled = !isEnabled;

        if (AudioManager.Instance != null)
        {
            if (isMusic)
                AudioManager.Instance.SetMusicVolume(isEnabled ? 1f : 0f);
            else
                AudioManager.Instance.SetSFXVolume(isEnabled ? 1f : 0f);
        }

        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (icon != null)
            icon.sprite = isEnabled ? enabledSprite : disabledSprite;
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(Toggle);
    }
}