using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Usamos esto para acceder al color sin que dé error TMPro

public class GestorPistasUI : MonoBehaviour
{
    [Header("Objetos de Texto de Pistas")]
    public GameObject textoTendero;
    public GameObject textoVecinos;
    public GameObject textoNino;

    [Header("Efectos Visuales")]
    public float escalaMaxima = 1.5f; // Aumentado para que el crecimiento se note MUCHO más
    public float velocidadEfecto = 0.5f; // Reducido para que la transición sea más "smooth" y lenta

    private void Start()
    {
        ActualizarTextos();
    }

    public void ActualizarTextos()
    {
        if (GameManagerNew.Instance == null) return;

        int interacciones = GameManagerNew.Instance.interaccionesClave;

        // 1. Apagamos todos por seguridad
        if (textoTendero != null) textoTendero.SetActive(false);
        if (textoVecinos != null) textoVecinos.SetActive(false);
        if (textoNino != null) textoNino.SetActive(false);

        GameObject textoAAnimar = null;

        // 2. Encendemos solo el que corresponde
        if (interacciones == 0)
        {
            if (textoTendero != null)
            {
                textoTendero.SetActive(true);
                textoAAnimar = textoTendero;
            }
        }
        else if (interacciones == 1)
        {
            if (textoVecinos != null)
            {
                textoVecinos.SetActive(true);
                textoAAnimar = textoVecinos;
            }
        }
        else if (interacciones == 2)
        {
            if (textoNino != null)
            {
                textoNino.SetActive(true);
                textoAAnimar = textoNino;
            }
        }

        // 3. Ejecutamos el efecto de tamaño y color
        if (textoAAnimar != null && gameObject.activeInHierarchy)
        {
            StopAllCoroutines();
            StartCoroutine(EfectoPalpitoYColor(textoAAnimar));
        }
    }

    // Nueva Rutina: Crece y cambia a Naranja -> Azul, luego se encoge y cambia a Blanco
    private IEnumerator EfectoPalpitoYColor(GameObject objetoTexto)
    {
        Transform transformTexto = objetoTexto.transform;

        // Obtenemos el componente visual para pintarlo (funciona perfecto con TextMeshPro)
        Graphic componenteVisual = objetoTexto.GetComponent<Graphic>();

        Vector3 escalaOriginal = Vector3.one; // Tamaño base (1,1,1)
        Vector3 escalaDestino = Vector3.one * escalaMaxima;

        // Definimos los colores
        Color colorNaranja = new Color(1f, 0.5f, 0f, 1f);
        Color colorAzul = new Color(0.2f, 0.6f, 1f, 1f); // Un azul agradable a la vista
        Color colorBlanco = Color.white;

        // Empezamos forzando el color inicial
        if (componenteVisual != null) componenteVisual.color = colorNaranja;

        // FASE 1: Crecer (Naranja a Azul)
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * velocidadEfecto;

            // Suavizamos el movimiento con SmoothStep para que no sea robótico
            float interpolacion = Mathf.SmoothStep(0f, 1f, t);

            transformTexto.localScale = Vector3.Lerp(escalaOriginal, escalaDestino, interpolacion);

            if (componenteVisual != null)
                componenteVisual.color = Color.Lerp(colorNaranja, colorAzul, interpolacion);

            yield return null;
        }

        // FASE 2: Encoger de vuelta (Azul a Blanco)
        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * velocidadEfecto;

            float interpolacion = Mathf.SmoothStep(0f, 1f, t);

            transformTexto.localScale = Vector3.Lerp(escalaDestino, escalaOriginal, interpolacion);

            if (componenteVisual != null)
                componenteVisual.color = Color.Lerp(colorAzul, colorBlanco, interpolacion);

            yield return null;
        }

        // FASE 3: Asegurar valores finales limpios
        transformTexto.localScale = escalaOriginal;
        if (componenteVisual != null) componenteVisual.color = colorBlanco;
    }
}