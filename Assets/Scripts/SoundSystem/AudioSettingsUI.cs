using UnityEngine;
using UnityEngine.UI;

public sealed class AudioSettingsUI : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider soundSlider;
    [SerializeField] private Slider musicSlider;

    private const int Steps = 5;

    private void Start()
    {
        if (AudioManager.Instance == null) return;

        float sfx = AudioManager.Instance.SFXVolume;
        float music = AudioManager.Instance.MusicVolume;

        soundSlider.value = sfx;
        musicSlider.value = music;

        soundSlider.onValueChanged.AddListener(OnSoundChanged);
        musicSlider.onValueChanged.AddListener(OnMusicChanged);
    }

    private void OnSoundChanged(float value)
    {
        float snapped = Snap(value);
        soundSlider.SetValueWithoutNotify(snapped);

        AudioManager.Instance.SetSFXVolume(snapped);
    }

    private void OnMusicChanged(float value)
    {
        float snapped = Snap(value);
        musicSlider.SetValueWithoutNotify(snapped);

        AudioManager.Instance.SetMusicVolume(snapped);
    }

    private float Snap(float value)
    {
        float step = 1f / Steps;
        return Mathf.Round(value / step) * step;
    }
}