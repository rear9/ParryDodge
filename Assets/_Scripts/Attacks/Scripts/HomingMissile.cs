using System.Collections;
using System.Linq;
using UnityEngine;

public class HomingMissile : EnemyAttackCore, IEnemyAttack
{
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer missileRenderer;
    [SerializeField] private SpriteRenderer explosionRenderer;
    [SerializeField] private CircleCollider2D explosionCollider;

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
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (_exploding || !_active) return;

        int layer = other.gameObject.layer;
        if (layer == LayerMask.NameToLayer("Player") && gameObject.layer == LayerMask.NameToLayer("EnemyAttack"))
        {
            if (other.TryGetComponent(out PlayerHealth health)) health.TakeDamage(stats.damage); // take damage
            StartCoroutine(Explode());
        }
        else if (layer == LayerMask.NameToLayer("PlayerParry") && stats.parryable && !_playerHit)
        {
            _playerHit = true;
            if (other.TryGetComponent(out Actions plrActions)) plrActions.ParrySuccess();
            OnParried(other.transform); // reflect function with player as source
        }
        else if (other.TryGetComponent(out HomingMissile otherMissile))
        {
            StartCoroutine(Explode()); // if it hits another missile, explode this and the colliding missile
            otherMissile.TriggerExplode();
        }
        else if (other.CompareTag("ExplosiveAttack") || other.CompareTag("DestructibleAttack") || other.CompareTag("ReflectiveAttack"))
        {
            if (gameObject.layer == LayerMask.NameToLayer("PlayerAttack")) // if it collides with an attack, make that attack unable to damage player
            {
                other.gameObject.layer = LayerMask.NameToLayer("PlayerAttack");
            }
            else
            {
                StartCoroutine(Explode());
            }
        }
    }

    protected override void OnParried(Transform parrySource)
    {
        Transform target = null;
        float nearestSqrDist = float.MaxValue;
        Vector2 pos = _rb.position;
        GameManager.Instance.TriggerHitstop(0.05f);
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
        if (!_exploding)
            StartCoroutine(Explode());
    }

    private IEnumerator Explode() // explosion bubble
    {
        if (_exploding) yield break;
        _exploding = true;
        _active = false;
        GameManager.Instance.TriggerHitstop(0.03f);
        missileRenderer.enabled = false; // make missile invisible on explosion
        explosionRenderer.enabled = true;

        if (explosionCollider)
        {
            explosionCollider.enabled = true;

            Collider2D[] hits = Physics2D.OverlapCircleAll( // make explosion bubble trigger collisions for chain-reaction
                explosionCollider.transform.position,
                explosionCollider.radius,
                LayerMask.GetMask("Player", "EnemyAttack")
            );

            foreach (var hit in hits)
            {
                if (hit.gameObject.layer == LayerMask.NameToLayer("Player") &&
                    gameObject.layer == LayerMask.NameToLayer("EnemyAttack") &&
                    hit.TryGetComponent(out PlayerHealth health))
                {
                    health.TakeDamage(1f); // missile doesn't deal damage on contact, explosion will deal damage if it is still an enemy attack
                }

                if (hit.TryGetComponent(out EnemyAttackCore enemyAtk) &&
                    gameObject.layer == LayerMask.NameToLayer("PlayerAttack"))
                {
                    enemyAtk.gameObject.layer = LayerMask.NameToLayer("PlayerAttack");
                }
            }

            explosionCollider.enabled = false;
        }

        yield return new WaitForSeconds(explosionDuration);
        explosionRenderer.enabled = false;
        ReturnToPool();
    }
}
