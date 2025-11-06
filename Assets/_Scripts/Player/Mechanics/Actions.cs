using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using FirstGearGames.SmoothCameraShaker;
public class Actions : MonoBehaviour
{
    [SerializeField] private Movement movement;
    [SerializeField] private PlayerColor plrColor;
    public ShakeData parryShakeData;
    
    [Header("Parry Settings")]
    public float parryWindow;
    public float parryFailCd;

    [Header("Dodge Settings")] 
    private float _dodgeSpeed;
    public float dodgeWindow;
    public float dodgeCd;
    public float dodgeSpeedMult;
    
    private bool _parrying;
    private bool _parryCdActive;
    private bool _dodging;
    private bool _dodgeCdActive;
    private bool _parryHit;

    private Collider2D _parriedAttack; // for debugging
    void Awake()
    {
        _dodgeSpeed = movement.speed;
    }
    public void StartParry()
    {
        if (_parrying || _parryCdActive) return;
        gameObject.layer = LayerMask.NameToLayer("PlayerParry");
        StartCoroutine(ParryRoutine());
    }
    private IEnumerator ParryRoutine() // parry mechanic
    {
        _parryHit = false;
        _parrying = true;
        
        gameObject.layer = LayerMask.NameToLayer("PlayerParry");
        StartCoroutine(plrColor.ColorSprite(plrColor.parryColor));
        
        float timer = 0f;
        while (timer < parryWindow && !_parryHit) // parry window timer
        {
            timer += Time.deltaTime;
            yield return null;
        }

        StartCoroutine(plrColor.ColorSprite(plrColor.neutralColor));
        _parrying = false;
        gameObject.layer = LayerMask.NameToLayer("Player");
        if (!_parryHit)
        {
            _parryCdActive = true;
            yield return new WaitForSeconds(parryFailCd); // Counter parry spam
            _parryCdActive = false;
        }
    }
    public void ParrySuccess()
    {
        StatsManager.Instance.RecordParry();
        AudioManager.PlaySFX(AudioManager.Instance.parrySFX);
        CameraShakerHandler.Shake(parryShakeData);
        _parryHit = true;
    }
    public void StartDodge()
    {
        if (_dodging || _dodgeCdActive) return;
        gameObject.layer = LayerMask.NameToLayer("PlayerDodge");
        StartCoroutine(DodgeRoutine());

    }
    private IEnumerator DodgeRoutine() // dodge mechanic
    {
        _dodging = true; // prevent multiple dodges activating at once, start player invulnerability
        StartCoroutine(plrColor.ColorSprite(plrColor.dodgeColor));
        movement.speed += movement.speed * dodgeSpeedMult; // increase player speed on dodge

        float time = 0f;
        while (time < dodgeWindow && _dodging) // timer with max window so players can't dodge forever & can cancel
        {
            time += Time.deltaTime;
            yield return null;
        }

        StartCoroutine(plrColor.ColorSprite(plrColor.neutralColor));
        _dodging = false; // end of player invulnerability
        movement.speed = _dodgeSpeed;
        gameObject.layer = LayerMask.NameToLayer("Player");
        _dodgeCdActive = true; // start dodge cd
        yield return new WaitForSeconds(dodgeCd);
        _dodgeCdActive = false;
    }
    public void CancelDodge()
    {
        if (_dodging) _dodging = false; movement.speed = _dodgeSpeed;
    }
}
