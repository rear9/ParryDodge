using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class FastBullet : EnemyAttackCore, IEnemyAttack // inherit from core and attack interface
{
    [Header("Overrides")] 
    [SerializeField] private float chargeTime = 1f;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Transform traceTransform;
    [SerializeField] private float maxTraceLength = 100f;
    [SerializeField] private Color pulseColor = Color.white;
    
    private Transform _player;
    private bool _moving;
    private float _speed;
    private Vector2 _moveDir;
    private Vector2 _originScale;
    private Vector2 _originPos;
    private Color _baseColor;

    protected override void Awake()
    {
        base.Awake();
        _originPos = sr.transform.localPosition;
        _originScale = sr.transform.localScale;
        traceTransform = sr.transform;
        
        _baseColor = sr.color;
    }
    
    public void InitAttack(Transform player)
    {
        _player = player;
        if (_player == null) return;

        StopAllCoroutines();
        sr.color = _baseColor; // reset visual
        SetTraceScale(0.1f);
        _moving = false;
        _active = false;
        
        // rotate towards player
        Vector2 dir = (_player.position - transform.position).normalized;
        var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);
        _moveDir = transform.up;

        // start attack sequence
        StartCoroutine(AttackSequence());
    }

    private void SetTraceScale(float length)
    {
        // scale in Y
        traceTransform.localScale = new Vector2(_originScale.x, length);
        
        // offset pos so it grows forward, assumes tracer points up (positive Y)
        traceTransform.localPosition = new Vector3(
            _originPos.x,
            _originPos.y + (length * 0.5f) // move vertically by half the scale
        );
    }
    
    private IEnumerator AttackSequence()
    {
        _active = false;
        
        if (chargeTime > 0)
        {
            float timer = 0f;
            while (timer < chargeTime) // color telegraph with ping-pong lerp
            {
                float progress = timer / chargeTime;
                
                // scale the tracer length over time
                float currentLength = Mathf.Lerp(0.1f, maxTraceLength, progress);
                SetTraceScale(currentLength);
                
                float t = Mathf.PingPong(timer * 2f, 1f);
                sr.color = Color.Lerp(_baseColor, pulseColor, t);
            
                timer += Time.deltaTime;
                yield return null;
            }
        }
        SetTraceScale(maxTraceLength);
        sr.color = _baseColor;
        AudioManager.PlaySFX(AudioManager.Instance.bulletSFX); // play sfx / start bullet movement
        _active = true;
        _speed = stats ? stats.attackSpeed : 5f;
        _moving = true;
    }

    private void FixedUpdate()
    {
        if (_moving && _active)
        {
            _rb.MovePosition(_rb.position + _moveDir * (_speed * Time.fixedDeltaTime));
        }
    }
    
    protected override void OnParried(Transform parrySource)
    {
        if (!_active) return;
        GameManager.Instance.TriggerHitstop(0.05f);
        gameObject.layer = LayerMask.NameToLayer("PlayerAttack");
        if (parrySource.TryGetComponent(out Collider2D parryCollider)) // on parry, reflect in random direction (360 degrees) (might restructure to reflect towards active attacks)
        {
            var reflectDir = ((Vector2)transform.position - parryCollider.ClosestPoint(transform.position)).normalized;
            if (reflectDir.sqrMagnitude < 0.01f)
            {
                var randomAngle = UnityEngine.Random.Range(0f, 360f);
                reflectDir = new Vector2(Mathf.Cos(randomAngle * Mathf.Deg2Rad), Mathf.Sin(randomAngle * Mathf.Deg2Rad));
            }
                

            _moveDir = reflectDir;
            
            float angle = Mathf.Atan2(_moveDir.y, _moveDir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90);

            _moving = true;
            _active = true;
        }
    }
}
