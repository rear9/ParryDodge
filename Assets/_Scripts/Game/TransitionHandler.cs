using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public class TransitionHandler : MonoBehaviour // for smooth scene transitions
{
    public static TransitionHandler Instance;
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        fadeImage.gameObject.SetActive(true);
        fadeImage.color = new Color(0, 0, 0, 1);
    }

    public IEnumerator FadeOut(float duration = 1f) // fade to black
    {
        yield return Fade(0f, 1f);
    }

    public IEnumerator FadeIn(float duration = 1f) // fade from black
    {
        yield return Fade(1f, 0f);
    }

    private IEnumerator Fade(float start, float end) // colour alpha lerp
    {
        fadeImage.gameObject.SetActive(true);
        float t = 0f;
        Color c = fadeImage.color;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(start, end, t / fadeDuration);
            fadeImage.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }
        fadeImage.color = new Color(c.r, c.g, c.b, end);
        if (end == 0)
        {
            fadeImage.gameObject.SetActive(false); // so it doesn't overlap UI elements
        }
    }
}