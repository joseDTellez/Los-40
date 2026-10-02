using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneFadeTransition : MonoBehaviour
{
    public static SceneFadeTransition Instance { get; private set; }

    // Avisa, con la duración del fundido, cuando la escena empieza a salir (p. ej. para bajar la música)
    public static event System.Action<float> AlSalirDeEscena;

    public Image fadeImage;
    public float fadeDuration = 1.5f;

    private bool _cargando = false;

    private void Awake()
    {
        Instance = this; // sin singleton complejo, cada escena tiene el suyo
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        StartCoroutine(FadeIn()); // fade de entrada siempre
    }

    // Carga con fundido si la escena tiene SceneFadeTransition; si no, carga directamente
    public static void Cargar(string sceneName)
    {
        if (Instance != null)
            Instance.LoadScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    public void LoadScene(string sceneName)
    {
        // Evita cargar la escena dos veces si varios scripts piden el cambio
        if (_cargando) return;
        _cargando = true;
        AlSalirDeEscena?.Invoke(fadeDuration);
        StartCoroutine(FadeOutAndLoad(sceneName));
    }

    IEnumerator FadeIn()
    {
        for (float time = 0f; time < fadeDuration; time += Time.deltaTime)
        {
            SetAlpha(1f - (time / fadeDuration));
            yield return null;
        }
        SetAlpha(0f);
    }

    IEnumerator FadeOutAndLoad(string sceneName)
    {
        for (float time = 0f; time < fadeDuration; time += Time.deltaTime)
        {
            SetAlpha(time / fadeDuration);
            yield return null;
        }
        SetAlpha(1f);
        SceneManager.LoadScene(sceneName);
    }

    private void SetAlpha(float alpha)
    {
        if (fadeImage == null) return;
        Color c = fadeImage.color;
        c.a = Mathf.Clamp01(alpha);
        fadeImage.color = c;
    }
}
