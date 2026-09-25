using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneFadeTransition : MonoBehaviour
{
    public static SceneFadeTransition Instance;

    public Image fadeImage;
    public float fadeDuration = 1.5f;

    private void Awake()
    {
        Instance = this; // sin singleton complejo, cada escena tiene el suyo
    }

    private void Start()
    {
        StartCoroutine(FadeIn()); // fade de entrada siempre
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(FadeOutAndLoad(sceneName));
    }

    IEnumerator FadeIn()
    {
        SetAlpha(1f);
        float time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            SetAlpha(1f - (time / fadeDuration));
            yield return null;
        }
        SetAlpha(0f);
    }

    IEnumerator FadeOutAndLoad(string sceneName)
    {
        SetAlpha(0f);
        float time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            SetAlpha(time / fadeDuration);
            yield return null;
        }
        SetAlpha(1f);
        SceneManager.LoadScene(sceneName);
    }

    private void SetAlpha(float alpha)
    {
        Color c = fadeImage.color;
        c.a = Mathf.Clamp01(alpha);
        fadeImage.color = c;
    }
}