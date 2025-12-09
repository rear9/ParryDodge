using System;
using System.Collections;
using UnityEngine;

public class PlayerColor : MonoBehaviour
{
    [Header("Colours")] 
    public Color neutralColor = new Color(1, 0, 0);
    public Color parryColor = new Color(1, .65f, 0);
    public Color dodgeColor = new Color(.3f, .6f, 1);

    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private float colourSpeed = .2f;
    [SerializeField] private float fadeDuration = 1f;
    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }
    public IEnumerator ColorSprite(Color clr) // for parry/dodging visual colours
    {
        float time = 0;
        while (time < colourSpeed)
        {
            time += Time.deltaTime;
            sr.color = Color.Lerp(sr.color, clr, time / colourSpeed);
            yield return null;
        }
    }

    public void FadeIn(float delay)
    {
        StopAllCoroutines();
        StartCoroutine(FadeInRoutine(delay));
    }
    private IEnumerator FadeInRoutine(float delay)
    {
        // get and apply saved color with alpha 0
        Color savedColor = MenuManager.GetSavedPlayerColor();
        Color startColor = new Color(savedColor.r, savedColor.g, savedColor.b, 0f);
        sr.color = startColor;
        
        // ypdate neutral color to use the saved hue for gameplay
        neutralColor = savedColor;
        
        // wait for delay
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        // ensure we're still at alpha 0 after delay
        sr.color = startColor;

        // fade in
        Color targetColor = new Color(savedColor.r, savedColor.g, savedColor.b, 1f);
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            sr.color = new Color(savedColor.r, savedColor.g, savedColor.b, alpha);
            yield return null;
        }

        sr.color = targetColor;
    }
}
