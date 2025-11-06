using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private Image bufferPanel;
    [Header("Music")] 
    [SerializeField] private int sfxPoolSize = 8;
    public AudioClip menuMusic;
    public AudioClip levelMusic;
    [Header("Player")]
    public AudioClip parrySFX;
    public AudioClip deathSFX;
    [Header("Enemies")]
    public AudioClip explosionSFX;
    
    [Header("UI")]
    public AudioClip menuPressSFX;
    public AudioClip menuHoverSFX;
    
    [Header("Volume Settings")]
    [Range(0f, 1f)] public float musicVolume;
    [Range(0f, 1f)] public float sfxVolume;
    private float sliderExp = 1.5f;
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
        
        sfxSources = new AudioSource[sfxPoolSize];
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
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", .2f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", .2f);
        foreach (var src in sfxSources) src.volume = MapSliderToVolume(sfxVolume);
        if(bufferPanel != null) bufferPanel.gameObject.SetActive(true);
        StartCoroutine(StartWithBuffer());
    }
    
    private IEnumerator StartWithBuffer()
    {
        float bufferDuration = 1f;
        float fadeDuration = 3f;
        yield return null;
        yield return StartCoroutine(FadeInMusicWithPanel(menuMusic, fadeDuration, true, bufferDuration));
    }
    
    private IEnumerator FadeInMusicWithPanel(AudioClip clip, float fadeDuration, bool loop, float initialDelay)
    {
        if (clip == null) yield break;
        if (initialDelay > 0f) yield return new WaitForSeconds(initialDelay);
        float targetVolume = MapSliderToVolume(musicVolume);
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = 0f;
        musicSource.Play();

        float t = 0f;
        Color panelColor = bufferPanel != null ? bufferPanel.color : Color.black;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float progress = t / fadeDuration;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, progress);
            Color c = panelColor;
            c.a = Mathf.Lerp(1f, 0f, progress);
            bufferPanel.color = c;
            yield return null;
        }
        musicSource.volume = targetVolume;
        bufferPanel.gameObject.SetActive(false);
    }
    
    private float MapSliderToVolume(float sliderValue)
    {
        return Mathf.Pow(sliderValue, sliderExp);
    }
    public void SetMusicVolume(float value)
    {
        musicSource.volume = MapSliderToVolume(value);
        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float value)
    {
        foreach (var src in sfxSources) src.volume = MapSliderToVolume(value);
        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();
    }


    
    public bool IsPlayingClip(AudioClip clip)
    {
        return musicSource.clip == clip && musicSource.isPlaying;
    }

    public IEnumerator FadeInMusic(AudioClip clip, float duration, bool loop)
    {
        if (clip == null) yield break;
        float targetVolume = MapSliderToVolume(musicVolume);
        if (MapSliderToVolume(musicVolume) == 0)
        {
            targetVolume = MapSliderToVolume(PlayerPrefs.GetFloat("MusicVolume", .2f));
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

    public IEnumerator FadeOutMusic(float duration)
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

    public IEnumerator CrossfadeMusic(AudioClip newClip, float fadeDuration = 2f, bool loop = true)
    {
        if (musicSource.isPlaying)
            yield return StartCoroutine(FadeOutMusic(fadeDuration));

        yield return StartCoroutine(FadeInMusic(newClip, fadeDuration, loop));
    }
    
    public static void PlaySFX(
        AudioClip clip, float pitchMin = 0.95f, float pitchMax = 1.05f, 
        float volumeMin = 0.85f, float volumeMax = 1.0f)
    {
        if (clip == null) return;

        // Find an available AudioSource
        AudioSource src = null;
        for (int i = 0; i < Instance.sfxSources.Length; i++)
        {
            if (!Instance.sfxSources[i].isPlaying)
            {
                src = Instance.sfxSources[i];
                break;
            }
        }

        if (src == null)
            src = Instance.sfxSources[0]; // recycle first if all busy

        // Randomize pitch and volume
        src.pitch = Random.Range(pitchMin, pitchMax);
        float baseVolume = Instance.MapSliderToVolume(Instance.sfxVolume);
        src.volume = baseVolume * Random.Range(volumeMin, volumeMax);
        src.PlayOneShot(clip);
    }
}
