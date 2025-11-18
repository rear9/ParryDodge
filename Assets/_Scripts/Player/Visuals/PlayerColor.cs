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
    public IEnumerator ColorSprite(Color clr)
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
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }
        Color c = sr.color;
        c.a = 0f;
        sr.color = c;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, t / fadeDuration);
            sr.color = c;
            yield return null;
        }

        c.a = 1f;
        sr.color = c;
    }
    
}
