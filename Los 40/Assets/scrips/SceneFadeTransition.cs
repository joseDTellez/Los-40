using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneFadeTransition : MonoBehaviour
{
    public static SceneFadeTransition Instance;

    [Header("Imagen negra para fade")]
    public Image fadeImage;

    [Header("Configuración")]
    public float fadeDuration = 1.5f;

    private void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Siempre hacer fade in al iniciar escena
        StartCoroutine(FadeIn());
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(FadeOutAndLoad(sceneName));
    }

    IEnumerator FadeIn()
    {
        fadeImage.gameObject.SetActive(true);

        Color color = fadeImage.color;
        color.a = 1f;
        fadeImage.color = color;

        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float alpha = 1f - (time / fadeDuration);
            fadeImage.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        fadeImage.color = new Color(color.r, color.g, color.b, 0f);
        fadeImage.gameObject.SetActive(false);
    }

    IEnumerator FadeOutAndLoad(string sceneName)
    {
        fadeImage.gameObject.SetActive(true);

        Color color = fadeImage.color;
        color.a = 0f;
        fadeImage.color = color;

        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float alpha = time / fadeDuration;
            fadeImage.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        fadeImage.color = new Color(color.r, color.g, color.b, 1f);

        SceneManager.LoadScene(sceneName);

        // IMPORTANTE: esperar un frame y hacer fade in
        yield return new WaitForEndOfFrame();
        StartCoroutine(FadeIn());
    }
}