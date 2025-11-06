using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] private PlayerColor playerColor;
    [SerializeField] private TutorialManager tutorialManager;

    private void Awake()
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

    private bool firstLoad = true;
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        playerColor = FindFirstObjectByType<PlayerColor>();
        if (playerColor != null) playerColor.FadeIn(1f);
        
        tutorialManager = FindFirstObjectByType<TutorialManager>();
        if (tutorialManager != null) tutorialManager.StartTutorial();
        
        if (AudioManager.Instance == null) return;
        if (firstLoad)
        {
            firstLoad = false;
            return;
        }
        if (scene.name == "Menu" && !AudioManager.Instance.IsPlayingClip(AudioManager.Instance.menuMusic))
        {
            StartCoroutine(AudioManager.Instance.CrossfadeMusic(AudioManager.Instance.menuMusic, 2f));
        }
        else if (scene.name == "Level1" && !AudioManager.Instance.IsPlayingClip(AudioManager.Instance.levelMusic))
        {
            StartCoroutine(AudioManager.Instance.CrossfadeMusic(AudioManager.Instance.levelMusic, 2f));
        }
    }

    
    public static void StartGame()
    {
        SceneManager.LoadScene("Level1");
        Cursor.visible = false;
    }

    public static void QuitGame()
    {
        Application.Quit();
    }

    public static void ReturnToMenu()
    {
        SceneManager.LoadScene("Menu");
    }
}
