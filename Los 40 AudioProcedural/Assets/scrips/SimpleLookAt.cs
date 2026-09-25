using UnityEngine; // Importa las librerías esenciales de Unity

public class VRCenterHUD : MonoBehaviour // Define la clase para que el objeto siga la mirada
{
    [Header("Configuración de Seguimiento")] // Crea un encabezado en el inspector
    [SerializeField] private Transform playerCamera; // Referencia a la cámara de los lentes VR
    [SerializeField] private float distance = 2.0f; // Qué tan lejos de la cara flotará el mensaje
    [SerializeField] private float followSpeed = 10.0f; // Qué tan rápido alcanza el centro de tu mirada

    private void Start() // Se ejecuta al iniciar el juego
    {
        if (playerCamera == null) // Si no arrastraste la cámara en el Inspector...
        {
            playerCamera = Camera.main.transform; // Busca automáticamente la cámara principal
        }
    }

    // Usamos LateUpdate para que el movimiento sea después de que la cámara se mueva
    // Esto evita vibraciones (jitter) en los lentes VR
    private void LateUpdate()
    {
        if (playerCamera != null) // Verifica que la cámara exista para no dar errores
        {
            // Calcula la posición "objetivo" (donde está la cámara + hacia donde mira * la distancia)
            Vector3 targetPosition = playerCamera.position + (playerCamera.forward * distance);

            // Mueve el Canvas desde su posición actual hacia la posición objetivo suavemente
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);

            // Hace que el Canvas siempre mire hacia la cámara del jugador
            transform.LookAt(playerCamera);

            // Rota 180 grados para corregir el efecto espejo y que el texto sea legible
            transform.Rotate(0, 180, 0);
        }
    }
}