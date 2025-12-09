using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI; // for Image reference (hit screen)

public class PlayerHealth : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float maxHP = 10;
    private float _currentHP;

    [Header("References")] 
    [SerializeField] private SpriteRenderer playerSprite;
    [SerializeField] private Sprite deathSprite;
    [SerializeField] private Image hitScreen; // assign red overlay image
    [SerializeField] private float hitScreenDuration = 0.5f;
    
    private UIManager _ui;

    private void Awake() // update HP objects to show when player loads in
    {
        _currentHP = maxHP;
        _ui = FindFirstObjectByType<UIManager>();
        if (_ui != null) _ui.UpdateHP(_currentHP, maxHP);
        if (hitScreen != null) hitScreen.enabled = false;
    }

    public void TakeDamage(float dmg) // plays sfx, updates UI and flashes screen, calls death function if HP is 0
    {
        _currentHP -= dmg;
        AudioManager.PlaySFX(AudioManager.Instance.hitSFX);
        _currentHP = Mathf.Clamp(_currentHP, 0, maxHP);
        if (_ui != null) _ui.UpdateHP(_currentHP, maxHP);
        StartCoroutine(HitFlash());
        if (_currentHP <= 0) StartCoroutine(Die());
    }

    private IEnumerator HitFlash()
    {
        if (hitScreen == null || playerSprite == null)
            yield break;

        // save original color
        Color screenColor = hitScreen.color;
        Color playerColor = playerSprite.color;

        hitScreen.enabled = true;

        float halfDuration = hitScreenDuration * 0.5f;
        float elapsed = 0f;

        while (elapsed < hitScreenDuration)
        {
            elapsed += Time.deltaTime;

            // fade in/out
            if (elapsed < halfDuration)
            {
                screenColor.a = Mathf.Lerp(0f, 0.5f, elapsed / halfDuration);
            }
            else
            {
                screenColor.a = Mathf.Lerp(0.5f, 0f, (elapsed - halfDuration) / halfDuration);
            }
            hitScreen.color = screenColor;

            // flash player white
            if (elapsed < 0.05f)
            {
                playerSprite.color = Color.white;
            }
            else
            {
                playerSprite.color = playerColor; // revert
            }

            yield return null;
        }

        hitScreen.enabled = false;
        playerSprite.color = playerColor; // make sure original color restored
    }
    
    private IEnumerator Die() // record death in stats and send player back to main menu
    {
        AttackPoolManager.Instance?.ReturnAllToPool();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        player.GetComponents<CircleCollider2D>()[1].enabled = false; // disabling colliders so the player can't collide with multiple attacks at the same time and 'die more than once'
        player.GetComponents<CircleCollider2D>()[0].enabled = false;
        Time.timeScale = 0.1f;
        StartCoroutine(AudioManager.Instance.FadeOutMusic(0));
        AudioManager.PlaySFX(AudioManager.Instance.deathSFX);
        yield return new WaitForSecondsRealtime(1.5f);
        if (player != null)
        {
            playerSprite.sprite = deathSprite;
        }
        yield return new WaitForSecondsRealtime(1f);
        AttackPoolManager.Instance?.ReturnAllToPool(); // make sure there aren't any attacks that just spawned randomly after the delay
        if (StatsManager.Instance != null && _ui != null)
        {
            string currentWave = _ui != null ? _ui.GetCurrentWaveName() : "N/A";
            StatsManager.Instance.SetGameState("Death"); // set states and send data
            StatsManager.Instance.RecordDeath(currentWave); // death data collection hook
        }
        // Return to menu after death
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMenu();
            Time.timeScale = 1;
        }
    }
}