using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] private PlayerColor playerColor;
    [SerializeField] private TutorialManager tutorialManager;
    private bool fromMenu;
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
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        playerColor = FindFirstObjectByType<PlayerColor>();
        if (playerColor != null) playerColor.FadeIn(1f); // fade player into scene if it exists
        
        tutorialManager = FindFirstObjectByType<TutorialManager>();
        if (tutorialManager != null) tutorialManager.StartTutorial(); // start tutorial once player is visible
    }

        public void TriggerHitstop(float duration = 0.016f) // public hitstop trigger to get called through instance
        {
            StartCoroutine(HitstopCoroutine(duration));
        }
    
        private IEnumerator HitstopCoroutine(float duration)
        {
            float previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;  // freeze game
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return new WaitForSecondsRealtime(duration); // real-time wait
            Time.timeScale = previousTimeScale; // unfreeze
            Time.fixedDeltaTime = 0.02f;
        }
    
    public static void StartGame() // for menu play button
    {
        AudioManager.Instance.StartCoroutine(AudioManager.Instance.CrossfadeMusic(AudioManager.Instance.levelMusic,1f));
        SceneManager.LoadScene("Level1");
        Cursor.visible = false;
    }

    public static void QuitGame() // for menu quit button
    {
        StatsManager.Instance.RecordFull("Menu");
        Application.Quit();
    }

    public static void ReturnToMenu() // this is called when the player beats game (can be later called from esc pause menu)
    {
        SceneManager.LoadScene("Menu");
        TransitionHandler.Instance.StartCoroutine(TransitionHandler.Instance.FadeIn(2f));
    }
}
