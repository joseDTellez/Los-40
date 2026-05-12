using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LetreroNotificacion : MonoBehaviour
{
    [Header("Configuración")]
    public float tiempoVisible = 3f;
    public float tiempoFade = 0.8f;

    [Header("Texto")]
    public Text textoLetrero; // Arrastra aquí el componente Text del recuadro

    private CanvasGroup canvasGroup;
    private Coroutine rutinaActual;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    // ? Llama esto desde el Dialogue Editor en TODOS los NPCs
    public void MostrarLetrero()
    {
        if (GameManagerNew.Instance == null)
        {
            Debug.LogError("GameManagerNew no encontrado");
            return;
        }

        int actual = GameManagerNew.Instance.interaccionesClave;
        int necesarias = GameManagerNew.Instance.interaccionesNecesarias;

        if (textoLetrero != null)
            textoLetrero.text = $"{actual}/{necesarias}";

        if (rutinaActual != null)
            StopCoroutine(rutinaActual);

        // ? Activa el objeto ANTES de iniciar la coroutine
        gameObject.SetActive(true);
        canvasGroup.alpha = 0f;

        rutinaActual = StartCoroutine(RutinaLetrero());
    }

    private IEnumerator RutinaLetrero()
    {
        gameObject.SetActive(true);

        // FADE IN
        float t = 0f;
        while (t < tiempoFade)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(t / tiempoFade);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // ESPERA
        yield return new WaitForSeconds(tiempoVisible);

        // FADE OUT
        t = 0f;
        while (t < tiempoFade)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(1f - (t / tiempoFade));
            yield return null;
        }
        canvasGroup.alpha = 0f;

        gameObject.SetActive(false);
        rutinaActual = null;
    }
}