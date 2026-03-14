using UnityEngine;

public sealed class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Default Music")]
    [SerializeField] private AudioClip defaultMusic;

    [Header("UI Sounds")]
    [SerializeField] private AudioClip uiClickClip;

    [Header("Default Volumes")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultMusicVolume = 0.5f;

    [Range(0f, 1f)]
    [SerializeField] private float defaultSFXVolume = 0.8f;

    private const string MusicVolumeKey = "MusicVolume";
    private const string SFXVolumeKey = "SFXVolume";

    public float MusicVolume => musicSource != null ? musicSource.volume : 0f;
    public float SFXVolume => sfxSource != null ? sfxSource.volume : 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupSources();
        LoadVolumes();
    }

    private void Start()
    {
        if (defaultMusic != null && musicSource != null && !musicSource.isPlaying)
        {
            PlayMusic(defaultMusic);
        }
    }

    private void SetupSources()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
    }

    private void LoadVolumes()
    {
        float musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, defaultMusicVolume);
        float sfxVolume = PlayerPrefs.GetFloat(SFXVolumeKey, defaultSFXVolume);

        SetMusicVolume(musicVolume);
        SetSFXVolume(sfxVolume);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null)
            return;

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.clip = clip;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource == null)
            return;

        musicSource.Stop();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null)
            return;

        sfxSource.PlayOneShot(clip, sfxSource.volume);
    }

    public void PlayUIClick()
    {
        PlaySFX(uiClickClip);
    }

    public void SetMusicVolume(float value)
    {
        value = Mathf.Clamp01(value);

        if (musicSource != null)
            musicSource.volume = value;

        PlayerPrefs.SetFloat(MusicVolumeKey, value);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float value)
    {
        value = Mathf.Clamp01(value);

        if (sfxSource != null)
            sfxSource.volume = value;

        PlayerPrefs.SetFloat(SFXVolumeKey, value);
        PlayerPrefs.Save();
    }

    public void SetDefaultMusic(AudioClip clip)
    {
        defaultMusic = clip;
    }
}