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
    private bool _reflected;
    private Transform _player;
    private bool _exploding;

    public void InitAttack(Transform player)
    {
        _player = player;
        _exploding = false;
        _reflected = false;
        _active = true;
        _playerHit = false;

        missileRenderer.enabled = true;
        explosionRenderer.enabled = false;
        if (explosionCollider) explosionCollider.enabled = false;

        if (_player != null)
        {
            var dir = (_player.position - transform.position).normalized;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
        }
        StartCoroutine(LifetimeWatcher());
    }
    
    private IEnumerator LifetimeWatcher()
    {
        float delay = stats.lifetime - explosionDuration;
        if (delay < 0f) delay = 0f;

        yield return new WaitForSeconds(delay);

        if (!_exploding && _active)
            StartCoroutine(Explode());
    }

    private void FixedUpdate()
    {
        if (_exploding || !_active) return;

        if (!_reflected && _player != null)
        {
            // smooth rotation towards player
            Vector2 dir = ((Vector2)_player.position - _rb.position).normalized;
            float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.Euler(0, 0, targetAngle),
                playerRotationSpeed * Time.fixedDeltaTime);
        }

        // move forward
        _rb.MovePosition(_rb.position + (Vector2)transform.up * (stats.attackSpeed * Time.fixedDeltaTime));
    }
    
    private void SetLayerRecursive(int layer)
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
            if (other.TryGetComponent(out PlayerHealth health)) health.TakeDamage(stats.damage); // take damage
            TriggerExplode();
        }
        else if (layer == LayerMask.NameToLayer("PlayerParry") && stats.parryable && !_playerHit)
        {
            _playerHit = true;
            if (other.TryGetComponent(out Actions plrActions)) plrActions.ParrySuccess();
            OnParried(other.transform); // reflect function with player as source
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
        float nearestSqrDist = float.MaxValue;
        Vector2 pos = _rb.position;
        if (parryParticles != null)
        {
            var parryParticlesMain = parryParticles.main;
            parryParticlesMain.useUnscaledTime = true;
            parryParticles.Play();
        }
        GameManager.Instance.TriggerHitstop();
        foreach (var a in AttackPoolManager.Instance.GetActiveAttacks())
        {
            if (a == gameObject) continue; // blacklists itself for reflection
            if (a.layer != LayerMask.NameToLayer("EnemyAttack")) continue; // whitelists enemy attacks
            bool validTag = a.CompareTag("ExplosiveAttack") || a.CompareTag("DestructibleAttack");
            if (!validTag) continue; // whitelists attacks that have custom logic

            float sqrDist = ((Vector2)a.transform.position - pos).sqrMagnitude; // finds closest enemy attack and makes it the target
            if (sqrDist < nearestSqrDist)
            {
                nearestSqrDist = sqrDist;
                target = a.transform;
            }
        }

        if (target != null)
        {
            Vector2 dir = ((Vector2)target.position - _rb.position).normalized;
            float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, targetAngle); // instant rotation towards nearest attack
        }
        gameObject.layer = LayerMask.NameToLayer("PlayerAttack");
        _reflected = true;
    }

    public void TriggerExplode() // helper for missile collisions
    {
        StartCoroutine(Explode());
    }

    private IEnumerator Explode()
    {
        if (_exploding) yield break;
        _exploding = true;
        _active = false;
        missileRenderer.enabled = false;
        
        
        if (explosionParticles != null) explosionParticles.Play();
        GameManager.Instance.TriggerHitstop();
        // start collision
        if (explosionCollider) explosionCollider.enabled = true;
        yield return new WaitForEndOfFrame();
        
        // store origin values
        Color originalColor = explosionRenderer.color;
        Quaternion originalRotation = explosionRenderer.transform.rotation;
        float flashDuration = 0.2f; // quick white flash
        Vector3 explosionScale = explosionRenderer.transform.localScale; // your final explosion size
        float startAlpha = explosionRenderer.color.a;
        
        explosionRenderer.transform.localScale = Vector3.zero; // start at 0
        explosionRenderer.enabled = true;
        Color startColor = originalColor;

        float timer = 0f;
        // ---- Explosion animation ----
        while (timer < explosionDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / explosionDuration;

            // SCALE (ease-out cubic)
            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
            explosionRenderer.transform.localScale = Vector3.Lerp(Vector3.zero, explosionScale, eased);

            // COLOR
            Color c;

            if (timer < flashDuration)
            {
                // Flash white, maintain constant original alpha
                float flashT = timer / flashDuration;
                c = Color.Lerp(Color.white, startColor, flashT);
                c.a = startAlpha;                           // maintain 0.15
            }
            else
            {
                // Fade alpha 0.15 -> 0 over the remainder
                float fadeT = (timer - flashDuration) / (explosionDuration - flashDuration);
                c = startColor;
                c.a = Mathf.Lerp(startAlpha, 0f, fadeT);    // fade to 0
            }

            explosionRenderer.color = c;

            yield return null;
        }
        
        // Reset to original state
        explosionCollider.enabled = false;
        explosionRenderer.enabled = false;
        explosionRenderer.transform.localScale = explosionScale;
        explosionRenderer.transform.rotation = originalRotation;
        explosionRenderer.color = originalColor;
        ReturnToPool();
    }
}
