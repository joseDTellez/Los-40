using UnityEngine; // Importa las herramientas base de Unity
using System.Collections; // Permite el uso de Corrutinas para las animaciones

public class VRInteractiveMessage : MonoBehaviour // Clase para el mensaje que sigue al jugador
{
    [Header("Referencias")] // Títulos en el inspector
    [SerializeField] private CanvasGroup canvasGroup; // Referencia al componente de transparencia del Canvas
    [SerializeField] private Transform playerCamera; // Referencia a la cámara del jugador VR

    [Header("Ajustes de Vista")] // Parámetros configurables
    [SerializeField] private float distance = 2.0f; // Distancia entre la cara del jugador y el mensaje
    [SerializeField] private float followSpeed = 5.0f; // Suavidad con la que el mensaje sigue la mirada
    [SerializeField] private float fadeDuration = 0.5f; // Duración de la transición de transparencia

    private bool isPlayerInside = false; // Controla si el jugador está dentro del área
    private Coroutine fadeCoroutine; // Guarda la animación de Fade actual

    private void Start() // Se ejecuta al arrancar el juego
    {
        if (canvasGroup != null) // Si el Canvas está asignado...
        {
            canvasGroup.alpha = 0; // Lo hace invisible al inicio
        }

        if (playerCamera == null) // Si no se asignó cámara en el Inspector...
        {
            playerCamera = Camera.main.transform; // Busca la cámara principal automáticamente
        }
    }

    private void Update() // Se ejecuta en cada fotograma
    {
        if (isPlayerInside && canvasGroup != null) // Si el jugador está dentro y hay un mensaje asignado...
        {
            ActualizarPosicionMensaje(); // Mueve el mensaje (pero no el bloque)
        }
    }

    private void ActualizarPosicionMensaje() // Función para posicionar el mensaje
    {
        // Calcula el punto exacto frente al jugador
        Vector3 targetPosition = playerCamera.position + (playerCamera.forward * distance);

        // MUEVE EL CANVAS (canvasGroup.transform), NO el objeto del script (this.transform)
        canvasGroup.transform.position = Vector3.Lerp(canvasGroup.transform.position, targetPosition, Time.deltaTime * followSpeed);

        // HACE QUE EL CANVAS MIRE AL JUGADOR
        canvasGroup.transform.LookAt(playerCamera);

        // CORRIGE LA ROTACIÓN PARA QUE EL TEXTO NO SE VEA AL REVÉS
        canvasGroup.transform.Rotate(0, 180, 0);
    }

    private void OnTriggerEnter(Collider other) // Cuando el jugador pisa el área
    {
        if (other.CompareTag("MainCamera") || other.CompareTag("Player")) // Si el objeto es el jugador...
        {
            isPlayerInside = true; // Activa el seguimiento del mensaje
            IniciarFade(1f); // Hace que el mensaje aparezca suavemente
        }
    }

    private void OnTriggerExit(Collider other) // Cuando el jugador sale del área
    {
        if (other.CompareTag("MainCamera") || other.CompareTag("Player")) // Si el jugador se retira...
        {
            isPlayerInside = false; // Detiene el seguimiento
            IniciarFade(0f); // Hace que el mensaje desaparezca suavemente
        }
    }

    private void IniciarFade(float targetAlpha) // Gestiona el inicio de la animación
    {
        if (fadeCoroutine != null) // Si ya se estaba animando...
        {
            StopCoroutine(fadeCoroutine); // Detiene la animación vieja para que no haya conflicto
        }
        fadeCoroutine = StartCoroutine(DoFade(targetAlpha)); // Inicia la nueva transición
    }

    private IEnumerator DoFade(float targetAlpha) // Lógica de la animación de transparencia
    {
        float startAlpha = canvasGroup.alpha; // Toma el valor de transparencia actual
        float time = 0; // Contador de tiempo

        while (time < fadeDuration) // Mientras dure la animación...
        {
            time += Time.deltaTime; // Suma el tiempo transcurrido
            // Interpola la transparencia gradualmente
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null; // Espera al siguiente frame
        }
        canvasGroup.alpha = targetAlpha; // Asegura que el valor final sea exacto
    }
}