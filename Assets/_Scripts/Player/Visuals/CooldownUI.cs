using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CooldownUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image parryImage;
    [SerializeField] private Image dodgeImage;

    [Header("Flash Settings")]
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private Color readyFlashColor = Color.white;

    private Color _parryBaseColor;
    private Color _dodgeBaseColor;
    private void Awake()
    {
        if (parryImage != null) _parryBaseColor = parryImage.color;
        if (dodgeImage != null) _dodgeBaseColor = dodgeImage.color;
    }
    private IEnumerator CooldownRoutine(Image img, float duration) // set radial fill value to 0 and lerp to 1 over action cooldown time
    {
        img.fillAmount = 0f;
        var t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            img.fillAmount = Mathf.Clamp01(t / duration);
            yield return null;
        }
        yield return Flash(img); // flash action icon when radial is full
    }

    private IEnumerator Flash(Image img)
    {
        AudioManager.PlaySFX(AudioManager.Instance.cooldownSFX);
        if (img == null) yield break;
        Color baseColor = img == parryImage ? _parryBaseColor : _dodgeBaseColor;
        
        float timer = 0f; // another lerp for colour
        while (timer < flashDuration)
        {
            timer += Time.deltaTime;
            img.color = Color.Lerp(readyFlashColor, baseColor, timer / flashDuration);
            yield return null;
        }
        img.color = baseColor;
    }
    public void StartParryCooldown(float duration) => StartCoroutine(CooldownRoutine(parryImage, duration)); // these get called from player action script
    public void StartDodgeCooldown(float duration) => StartCoroutine(CooldownRoutine(dodgeImage, duration));
}
