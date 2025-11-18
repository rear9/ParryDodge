using UnityEngine;

public class LineSweep : EnemyAttackCore, IEnemyAttack
{
    private Vector2 _dir;
    private float _speed;
    private Vector2 _origin = Vector2.zero;
    private AudioSource attackSfx;
    
    public void InitAttack(Transform player)
    {
        _speed = stats ? stats.attackSpeed : 5f;

        // direction from spawn toward center (0,0)
        _dir = (_origin - (Vector2)transform.position).normalized;

        // rotate sprite to face direction of movement
        float angle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        attackSfx = AudioManager.PlaySFX(AudioManager.Instance.laserSFX,true);
        _active = true;
    }

    private void FixedUpdate()
    {
        if (!_active) return;
        _rb.MovePosition(_rb.position + _dir * (_speed * Time.fixedDeltaTime));
    }

    void OnTransformParentChanged()
    {
        // detect when parent changes (active -> pool or vice versa)
        if (transform.parent != null &&
            transform.parent.name.EndsWith("_Pool")) // returned to pool
        {
            if (attackSfx != null)
            {
                AudioManager.StopSFX(attackSfx); // stop looping sound effect when attack is returned
                attackSfx = null;
            }
        }
    }
    
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (!_active && !_playerHit) return;

        int layer = other.gameObject.layer;
        if (layer == LayerMask.NameToLayer("Player") || layer == LayerMask.NameToLayer("PlayerParry")) // if player isn't performing an action or parrying
        {
            if(_playerHit) return;
            _playerHit = true;
            if (other.TryGetComponent(out PlayerHealth health))
            {
                health.TakeDamage(stats.damage > 0f ? stats.damage : 1f); // damage player
            }
        }
        else if (layer == LayerMask.NameToLayer("PlayerDodge"))
        {
            if (_playerHit) return;
            _playerHit = true;
            StatsManager.Instance.RecordDodge();
        }
        
        // ignore self-collisions and other neutrals
        if (CompareTag("NeutralAttack")) return;
        
        // destroy destructible and explosive attacks
        string otherTag = other.tag;
        if (otherTag == "DestructibleAttack" || otherTag == "ExplosiveAttack")
        {
            if (other.TryGetComponent(out EnemyAttackCore attack))
                attack.SendMessage("ReturnToPool", SendMessageOptions.DontRequireReceiver);
        }
    }
}