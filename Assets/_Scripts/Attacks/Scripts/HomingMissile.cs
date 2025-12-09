using System.Collections;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class HomingMissile : EnemyAttackCore, IEnemyAttack
{
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer missileRenderer;
    [SerializeField] private GameObject explosion;
    [SerializeField] private SpriteRenderer explosionRenderer;
    [SerializeField] private CircleCollider2D explosionCollider;
    [SerializeField] private ParticleSystem explosionParticles;
    [SerializeField] private ParticleSystem parryParticles;
    
    [Header("Attack")]
    [SerializeField] private float playerRotationSpeed = 180f; // slower homing to player
    [SerializeField] private float parryRotationSpeed = 720f;  // fast homing to targets
    [SerializeField] private float explosionDuration = 1f;
    private AudioSource attackSfx;
    private bool _reflected;
    private Transform _player;
    private bool _exploding;

    public void InitAttack(Transform player)
    {
        _player = player; // reset variables
        _exploding = false;
        _reflected = false;
        _playerHit = false;
        _active = true;

        missileRenderer.enabled = true;
        explosionRenderer.enabled = false;
        if (explosionCollider) explosionCollider.enabled = false; // make sure only the missile is visible

        if (_player != null)
        {
            var dir = (_player.position - transform.position).normalized; // get player direction
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f); // rotate towards player to start
        }
        StartCoroutine(LifetimeWatcher()); // to explode missile before it despawns
        attackSfx = AudioManager.PlaySFX(AudioManager.Instance.missileLoopSFX, true); // play looping sfx
    }
    
    private IEnumerator LifetimeWatcher()
    {
        float delay = stats.lifetime - explosionDuration; // find perfect time to wait for seamless explosion on lifetime end
        if (delay < 0f) delay = 0f;

        yield return new WaitForSeconds(delay);

        if (!_exploding && _active)
        {
            AudioManager.StopSFX(attackSfx); // stop sfx and explode
            StartCoroutine(Explode());
        }
    }

    private void FixedUpdate()
    {
        if (_exploding || !_active) return;

        if (!_reflected && _player != null)
        {
            // smooth slow rotation towards player to mimic a homing missile
            Vector2 dir = ((Vector2)_player.position - _rb.position).normalized; 
            float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f; // get direction and rotate with RotateTowards
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0, 0, targetAngle), playerRotationSpeed * Time.fixedDeltaTime);
        }

        // move forward
        _rb.MovePosition(_rb.position + (Vector2)transform.up * (stats.attackSpeed * Time.fixedDeltaTime));
    }
    
    private void SetLayerRecursive(int layer) // make sure layers of object & children (in this case the missile & explosion collider) are the same, to stop parried explosions from damaging plr
    {
        gameObject.layer = layer;
        foreach (Transform child in transform)
            child.gameObject.layer = layer;
    }
    
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (_exploding || !_active) return;

        int layer = other.gameObject.layer;
        if (layer == LayerMask.NameToLayer("Player") && gameObject.layer == LayerMask.NameToLayer("EnemyAttack"))
        {
            if (other.TryGetComponent(out PlayerHealth health)) health.TakeDamage(stats.damage); // take damage and explode
            TriggerExplode();
        }
        else if (layer == LayerMask.NameToLayer("PlayerParry") && stats.parryable && !_playerHit)
        {
            _playerHit = true;
            if (other.TryGetComponent(out Actions plrActions)) plrActions.ParrySuccess();
            OnParried(other.transform); // reflect function with player as source
        }
        else if (layer == LayerMask.NameToLayer("PlayerDodge"))
        {
            if (_playerHit) return;
            _playerHit = true;
            StatsManager.Instance.RecordDodge();
        }
        else if (other.TryGetComponent(out HomingMissile otherMissile))
        {
            TriggerExplode(); // if it hits another missile, explode this and the colliding missile
            otherMissile.TriggerExplode();
        }
        else if (layer == LayerMask.NameToLayer("PlayerAttack"))
        {
            gameObject.layer = LayerMask.NameToLayer("PlayerAttack");
            SetLayerRecursive(LayerMask.NameToLayer("PlayerAttack"));
            TriggerExplode();
        }
        else if (other.CompareTag("ExplosiveAttack") || other.CompareTag("DestructibleAttack") || other.CompareTag("ReflectiveAttack"))
        {
            if (gameObject.layer == LayerMask.NameToLayer("PlayerAttack")) // if it collides with an attack, make that attack unable to damage player
            {
                other.gameObject.layer = LayerMask.NameToLayer("PlayerAttack");
            }
            else
            {
                TriggerExplode();
            }
        }
    }

    protected override void OnParried(Transform parrySource)
    {
        SetLayerRecursive(LayerMask.NameToLayer("PlayerAttack"));
        Transform target = null;
        float nearestSqrDist = float.MaxValue; // range of attack detection to deflect towards when parried (infinite for prototyping, can be changed later)
        if (parryParticles != null) // play particles
        {
            var parryParticlesMain = parryParticles.main;
            parryParticlesMain.useUnscaledTime = true;
            parryParticles.Play();
        }
        Vector2 pos = _rb.position; // used to calculate rotation towards nearest enemy attack
        GameManager.Instance.TriggerHitstop(); // parry hitstop
        foreach (var a in AttackPoolManager.Instance.GetActiveAttacks())
        {
            if (a == gameObject) continue; // blacklists itself for reflection
            if (a.layer != LayerMask.NameToLayer("EnemyAttack")) continue; // whitelists enemy attacks
            bool validTag = a.CompareTag("ExplosiveAttack") || a.CompareTag("DestructibleAttack");
            if (!validTag) continue; // whitelists attacks that have custom logic

            float sqrDist = ((Vector2)a.transform.position - pos).sqrMagnitude; // finds closest enemy attack and makes it the target
            if (sqrDist < nearestSqrDist)
            {
                nearestSqrDist = sqrDist; // to ensure that only one attack is selected to be deflected towards
                target = a.transform; // also needed to calculate rotation along with Vector2 pos (line 134)
            }
        }

        if (target != null)
        {
            Vector2 dir = ((Vector2)target.position - _rb.position).normalized;
            float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f; // once again another rotation towards a target
            transform.rotation = Quaternion.Euler(0, 0, targetAngle);
        }
        gameObject.layer = LayerMask.NameToLayer("PlayerAttack"); // so the attack/explosion can't damage the player anymore
        SetLayerRecursive(LayerMask.NameToLayer("PlayerAttack"));
        _reflected = true;
    }

    public void TriggerExplode() // helper for missile collisions
    {
        StartCoroutine(Explode());
    }

    private IEnumerator Explode()
    {
        if (_exploding) yield break; // so it doesn't explode multiple times at once (would turn into an infinite loop with 2 missiles colliding and freeze the game)
        _exploding = true;
        _active = false;
        missileRenderer.enabled = false; // no more missile, show explosion
        AudioManager.StopSFX(attackSfx);
        AudioManager.PlaySFX(AudioManager.Instance.explosionSFX);
        if (explosionParticles != null) explosionParticles.Play();
        GameManager.Instance.TriggerHitstop();
        // start collision
        if (explosionCollider) explosionCollider.enabled = true;
        yield return new WaitForEndOfFrame();
        
        // store origin values
        Color originalColor = explosionRenderer.color;
        Quaternion originalRotation = explosionRenderer.transform.rotation;
        float flashDuration = 0.2f; // quick white flash
        Vector3 explosionScale = explosionRenderer.transform.localScale; // final explosion size
        float startAlpha = explosionRenderer.color.a;
        
        explosionRenderer.transform.localScale = Vector3.zero; // start at 0
        explosionRenderer.enabled = true;
        Color startColor = originalColor;

        float timer = 0f;
        // explosion anim
        while (timer < explosionDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / explosionDuration;

            // scale the explosion
            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
            explosionRenderer.transform.localScale = Vector3.Lerp(Vector3.zero, explosionScale, eased);

            // color lerp
            Color c;

            if (timer < flashDuration)
            {
                // flash white, maintain alpha
                float flashT = timer / flashDuration;
                c = Color.Lerp(Color.white, startColor, flashT);
                c.a = startAlpha;
            }
            else
            {
                // fade alpha to 0 over the remainder of explosion
                float fadeT = (timer - flashDuration) / (explosionDuration - flashDuration);
                c = startColor;
                c.a = Mathf.Lerp(startAlpha, 0f, fadeT);
            }
            explosionRenderer.color = c;
            yield return null;
        }
        
        // reset to original state and return to pool
        explosionCollider.enabled = false;
        explosionRenderer.enabled = false;
        explosionRenderer.transform.localScale = explosionScale;
        explosionRenderer.transform.rotation = originalRotation;
        explosionRenderer.color = originalColor;
        ReturnToPool();
    }
}
