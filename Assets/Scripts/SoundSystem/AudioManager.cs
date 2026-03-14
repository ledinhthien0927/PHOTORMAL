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

    [Header("Game SFX Sources")]
    [SerializeField] private AudioSource clownAppearSource;
    [SerializeField] private AudioSource supportCallSource;
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private AudioSource knockDoorSource;

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
        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

        ConfigureSource(musicSource, true, false);
        ConfigureSource(sfxSource, false, false);
        
        // Ensure game-specific sources are also 2D for consistent volume
        ConfigureSource(clownAppearSource, false, false);
        ConfigureSource(supportCallSource, false, false);
        ConfigureSource(footstepSource, true, false); // Loop for footsteps
        ConfigureSource(knockDoorSource, false, false);
    }

    private void ConfigureSource(AudioSource source, bool loop, bool playOnAwake)
    {
        if (source == null) return;
        source.playOnAwake = playOnAwake;
        source.loop = loop;
        source.spatialBlend = 0f; // 2D Sound
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

    public void PlayClownAppear()
    {
        if (clownAppearSource != null) clownAppearSource.Play();
    }

    public void PlaySupportCall()
    {
        if (supportCallSource != null) supportCallSource.Play();
    }

    public void StopSupportCall()
    {
        if (supportCallSource != null) supportCallSource.Stop();
    }

    public void PlayKnockDoor()
    {
        if (knockDoorSource != null) knockDoorSource.Play();
    }

    public void PlayFootstep(bool isPlaying)
    {
        if (footstepSource == null) return;

        if (isPlaying)
        {
            if (!footstepSource.isPlaying) 
            {
                Debug.Log($"[AudioManager] Playing Footstep at volume: {footstepSource.volume}");
                footstepSource.Play();
            }
        }
        else
        {
            if (footstepSource.isPlaying) footstepSource.Stop();
        }
    }

    public void SetFootstepVolume(float volume)
    {
        if (footstepSource != null)
        {
            footstepSource.volume = Mathf.Clamp01(volume);
        }
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

        // Apply volume to game-specific sources too
        if (clownAppearSource != null) clownAppearSource.volume = value;
        if (supportCallSource != null) supportCallSource.volume = value;
        if (footstepSource != null) footstepSource.volume = value;
        if (knockDoorSource != null) knockDoorSource.volume = value;

        PlayerPrefs.SetFloat(SFXVolumeKey, value);
        PlayerPrefs.Save();
    }

    public void SetDefaultMusic(AudioClip clip)
    {
        defaultMusic = clip;
    }
}