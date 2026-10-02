using UnityEngine;
using DialogueEditor;

// Bloquea al jugador (movimiento e interaccion por mirada) mientras hay un dialogo activo,
// para que las teclas de navegacion del dialogo no muevan tambien al personaje.
public class DialogueControllerBridge : MonoBehaviour
{
    [Tooltip("Controlador del jugador. Si se deja vacio se busca en este mismo objeto.")]
    public PCController playerController;

    private Rigidbody playerRigidbody;

    private void Awake()
    {
        if (playerController == null) playerController = GetComponent<PCController>();
        playerRigidbody = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        ConversationManager.OnConversationStarted += OnConversationStarted;
        ConversationManager.OnConversationEnded += OnConversationEnded;
    }

    private void OnDisable()
    {
        ConversationManager.OnConversationStarted -= OnConversationStarted;
        ConversationManager.OnConversationEnded -= OnConversationEnded;
    }

    private void OnConversationStarted() => Bloquear(true);
    private void OnConversationEnded() => Bloquear(false);

    private void Bloquear(bool bloquear)
    {
        if (playerController != null)
        {
            playerController.canMove = !bloquear;
            playerController.canInteract = !bloquear;
        }

        // Frenamos cualquier deslizamiento horizontal, pero dejamos actuar la gravedad
        if (bloquear && playerRigidbody != null && !playerRigidbody.isKinematic)
            playerRigidbody.linearVelocity = new Vector3(0f, playerRigidbody.linearVelocity.y, 0f);
    }
}
