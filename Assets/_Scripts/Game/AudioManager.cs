using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;
    
    [Header("Music")]  // many sound clips
    [SerializeField] private int sfxPoolSize = 8;
    public AudioClip menuMusic;
    public AudioClip levelMusic;
    
    [Header("Player")]
    public AudioClip parrySFX;
    public AudioClip hitSFX;
    public AudioClip deathSFX;
    
    [Header("Enemies")]
    public AudioClip bulletSFX;
    public AudioClip laserSFX;
    public AudioClip areaSFX;
    public AudioClip missileLoopSFX;
    public AudioClip explosionSFX;

    [Header("UI")] 
    public AudioClip tickSFX;
    public AudioClip cooldownSFX;
    public AudioClip menuPressSFX;
    public AudioClip menuHoverSFX;

    [Header("Volume Settings")] 
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume;
    [Range(0f, 1f)] public float sfxVolume;
    private const float sliderExp = 1.5f;
    private AudioSource[] sfxSources;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        sfxSources = new AudioSource[sfxPoolSize]; // make pool for audio sources to make sound stacking possible, similar to AttackPoolManager
        for (int i = 0; i < sfxPoolSize; i++)
        {
            GameObject go = new GameObject("SFX_" + i);
            go.transform.SetParent(transform);
            AudioSource src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            sfxSources[i] = src;
        }
    }
    private void Start()
    {
        musicSource.volume = 0f;
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", .5f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", .5f); // set volume preferences from local data
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", .5f);
        ApplyVolume();
        foreach (var src in sfxSources) src.volume = MapSliderToVolume(sfxVolume); // adjust volume-slider ratio
        StartCoroutine(FadeInMusicWithPanel(menuMusic, 2f, true)); // start loading buffer
    }
    
    private float MapSliderToVolume(float sliderValue)
    {
        return Mathf.Pow(sliderValue, sliderExp); // use exponential to change slider-volume ratio
    }
    
    private void ApplyVolume()
    {
        float mappedMusic = MapSliderToVolume(musicVolume) * masterVolume;
        musicSource.volume = mappedMusic;

        float mappedSFX = MapSliderToVolume(sfxVolume) * masterVolume;
        foreach (var src in sfxSources)
            src.volume = mappedSFX;
    }
    
    private IEnumerator FadeInMusicWithPanel(AudioClip clip, float fadeDuration, bool loop)
    {
        yield return new WaitForSeconds(2f);
        if (clip == null) yield break;
        float targetVolume = MapSliderToVolume(musicVolume) * masterVolume;
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = 0f;
        musicSource.Play();
        
        float t = 0f;
        StartCoroutine(TransitionHandler.Instance.FadeIn(2f));
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float progress = t / fadeDuration;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, progress);

            yield return null;
        }
        musicSource.volume = targetVolume;
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = value;
        ApplyVolume();
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
    }
    public void SetMusicVolume(float value) // volume setters for music/sfx
    {
        musicVolume = value;
        ApplyVolume();
        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = value;
        ApplyVolume();
        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();
    }
    public bool IsPlayingClip(AudioClip clip) // helper function
    {
        return musicSource.clip == clip && musicSource.isPlaying;
    }

    public IEnumerator FadeInMusic(AudioClip clip, float duration, bool loop) // fade music by lerping volume
    {
        if (clip == null) yield break;
        float targetVolume = MapSliderToVolume(musicVolume) * masterVolume;
        if (MapSliderToVolume(musicVolume) == 0)
        {
            targetVolume = MapSliderToVolume(PlayerPrefs.GetFloat("MusicVolume", .2f)) * masterVolume;
        }
        musicSource.clip = clip;
        musicSource.volume = 0f;
        musicSource.loop = loop;
        musicSource.Play();

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, t / duration);
            yield return null;
        }
        musicSource.volume = targetVolume;
    }

    public IEnumerator FadeOutMusic(float duration) // reverse above function
    {
        float startVolume = musicSource.volume;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / duration);
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = startVolume;
    }

    public IEnumerator CrossfadeMusic(AudioClip newClip, float fadeDuration = 2f, bool loop = true) // trigger both above functions concurrently to crossfade
    {
        if (musicSource.isPlaying)
            yield return StartCoroutine(FadeOutMusic(fadeDuration));

        yield return StartCoroutine(FadeInMusic(newClip, fadeDuration, loop));
    }
    public static AudioSource PlaySFX(AudioClip clip, bool looping = false, 
        float pitchMin = 0.95f, float pitchMax = 1.05f, float volumeMin = 0.85f, float volumeMax = 1.0f) // global soundclip player with randomizing pitch/volume
    {
        if (clip == null) return null;
        AudioSource src = null;
        for (int i = 0; i < Instance.sfxSources.Length; i++) // find an open audiosource to play sfx from
        {
            if (!Instance.sfxSources[i].isPlaying)
            {
                src = Instance.sfxSources[i];
                break;
            }
        }
        if (src == null) src = Instance.sfxSources[0]; // fallback
        float baseVolume = Instance.MapSliderToVolume(Instance.sfxVolume) * Instance.masterVolume;
        src.volume = baseVolume * Random.Range(volumeMin, volumeMax);
        src.pitch = Random.Range(pitchMin, pitchMax);
        src.loop = looping;
        src.clip = clip;
        src.Play();
        return src;
    }
    public static void StopSFX(AudioSource source) // global sfx stopper (for looping sounds)
    {
        if (source == null) return;
        source.Stop();
        source.clip = null;
        source.loop = false;
    }
    
}
