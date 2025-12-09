using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] private PlayerColor playerColor;
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private Button menuButton;
    [SerializeField] private Slider pauseMusicSlider;
    [SerializeField] private Slider pauseMasterSlider;
    [SerializeField] private Slider pauseSfxSlider;
    private float _hitstopTimer = 0f;
    private bool _hitstopActive = false;
    private bool _isPaused = false;
    private bool fromMenu;
    
    private void Awake() // we love singletons
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); 
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start() // pause UI initialization
    {
        pauseMasterSlider.value = AudioManager.Instance.masterVolume; // set pause slider values
        pauseMusicSlider.value = AudioManager.Instance.musicVolume;
        pauseSfxSlider.value = AudioManager.Instance.sfxVolume;
        pauseMasterSlider.onValueChanged.AddListener(AudioManager.Instance.SetMasterVolume); // update global volumes for menu as well
        pauseMusicSlider.onValueChanged.AddListener(AudioManager.Instance.SetMusicVolume);
        pauseSfxSlider.onValueChanged.AddListener(AudioManager.Instance.SetSFXVolume);
        menuButton.onClick.AddListener(ReturnToMenu); // return to menu events
        menuButton.onClick.AddListener(MenuSave);
    }
    private void Update()
    {
        if (_hitstopTimer > 0f) // countdown hit-stop to 0
        {
            Time.timeScale = 0f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            _hitstopTimer -= Time.unscaledDeltaTime;
        }
        else if (_hitstopActive) // when it hits 0, set timescale to 1
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            _hitstopActive = false;
        }
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        playerColor = FindFirstObjectByType<PlayerColor>();
        if (playerColor != null) playerColor.FadeIn(1f); // fade player into scene if it exists
        
        tutorialManager = FindFirstObjectByType<TutorialManager>();
        if (tutorialManager != null) tutorialManager.StartTutorial(); // start tutorial once player is visible
        
        if (_isPaused) // prevent weird hardlocks
        {
            UnpauseGame();
        }
    }
    
    public void TriggerHitstop(float duration = 0.016f)
    {
        _hitstopTimer = Mathf.Max(_hitstopTimer, duration); // extend timer if longer than current
        _hitstopActive = true;
    }
    
    public void TogglePause()
    {
        // only allow pausing in the stage scene & outside of tutorial (to prevent timescale bugs)
        if (SceneManager.GetActiveScene().name != "Stage" || !GameObject.Find("WaveSpawner").GetComponent<WaveSpawner>().enabled)
        {
            return;
        }
        
        if (_isPaused) // basic flip-flop for pausing
        {
            UnpauseGame();
        }
        else
        {
            PauseGame();
        }
    }
    
    private void PauseGame() // freeze time and activate pause menu
    {
        _isPaused = true;
        Time.timeScale = 0f;
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(true);
        }
        Cursor.visible = true;
    }
    
    private void UnpauseGame() // reverse above
    {
        _isPaused = false;
        Time.timeScale = 1f;
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
        }
        Cursor.visible = false;
    }
    
    public void StartGame() // for menu play button
    {
        AudioManager.Instance.StartCoroutine(AudioManager.Instance.CrossfadeMusic(AudioManager.Instance.levelMusic,1f));
        SceneManager.LoadScene("Stage");
        Cursor.visible = false;
    }

    public void QuitGame() // for menu quit button
    {
        StartCoroutine(QuitCoroutine());
    }

    private IEnumerator QuitCoroutine() // coroutine to allow for a proper fade out on quit
    {
        StatsManager.Instance.RecordFull("Menu");
        yield return StartCoroutine(TransitionHandler.Instance.FadeOut());
        #if UNITY_EDITOR // quit play mode if in editor
                EditorApplication.isPlaying = false;
        #else
                    Application.Quit(); // quit application if in build
        #endif
    }

    public void ReturnToMenu()
    {
        StartCoroutine(ReturnToMenuCoroutine());
    }

    private IEnumerator ReturnToMenuCoroutine() // coroutine again to allow for scene transition from main scene pause -> menu
    {
        AttackPoolManager.Instance.ReturnAllToPool();
        if (_isPaused)
        {
            UnpauseGame();
        }
        AudioManager.Instance.StartCoroutine(AudioManager.Instance.CrossfadeMusic(AudioManager.Instance.menuMusic, 1f));
        yield return StartCoroutine(TransitionHandler.Instance.FadeOut()); // fade to black
        AttackPoolManager.Instance.ReturnAllToPool(); // make sure there aren't any attacks that spawn just before scene change
        SceneManager.LoadScene("Menu");
        yield return new WaitForSecondsRealtime(0.5f); // once menu is fully loaded, fade in from black
        yield return StartCoroutine(TransitionHandler.Instance.FadeIn());
    }

    private void MenuSave() // to save when player returns to menu from pause
    {
        StatsManager.Instance.RecordFull("Menu");
    }
}
