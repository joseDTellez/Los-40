using UnityEngine;
using UnityEngine.InputSystem; // Necesario para Gamepad y Keyboard
using DialogueEditor;

/// <summary>
/// Controla el inicio de conversaciones mediante Teclado (L), Gatillos de Gamepad (L2/R2)
/// e interacción por mirada (Gaze/Cardboard Trigger).
/// </summary>
public class ConversationStarter : MonoBehaviour
{
    [Header("Configuración de Diálogo")]
    [SerializeField] private NPCConversation conversation;

    [Header("Componentes del Jugador")]
    [Tooltip("Script VRBoxController que está en tu Player.")]
    [SerializeField] private VRBoxController playerMovementScript;
    
    [Tooltip("Rigidbody del Player para evitar que deslice.")]
    [SerializeField] private Rigidbody playerRigidbody;

    [Header("Simulador (Solo para pruebas en PC)")]
    [Tooltip("Objeto 'XR Interaction Simulator' de tu jerarquía.")]
    [SerializeField] private GameObject xrSimulator;

    [Header("Input Manual")]
    [SerializeField] private Key interactKey = Key.L;

    private bool playerInside = false;
    private bool isInConversation = false;

    private void OnEnable()
    {
        ConversationManager.OnConversationEnded += OnConversationEnded;
    }

    private void OnDisable()
    {
        ConversationManager.OnConversationEnded -= OnConversationEnded;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verifica que el Player tenga el Tag "Player"
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            Debug.Log("Jugador cerca. Presiona L o Gatillos para hablar.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
        }
    }

    private void Update()
    {
        // Si no estamos en el rango o ya estamos en medio de una charla, ignorar
        if (!playerInside || isInConversation) return;

        // 1. CONTROL POR TECLADO (L)
        if (Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame)
        {
            StartDialogue();
        }

        // 2. CONTROL POR GAMEPAD (Gatillo Derecho e Izquierdo)
        if (Gamepad.current != null)
        {
            if (Gamepad.current.rightTrigger.wasPressedThisFrame || Gamepad.current.leftTrigger.wasPressedThisFrame)
            {
                StartDialogue();
            }
        }
    }

    /// <summary>
    /// Activa la conversación y congela al jugador por completo.
    /// </summary>
    public void StartDialogue()
    {
        if (isInConversation) return;

        Debug.Log("Iniciando Diálogo y bloqueando controles...");
        isInConversation = true;

        // Desactivar movimiento de Joystick
        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        // Congelar físicas para que no 'patine'
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.isKinematic = true; 
        }

        // Apagar simulador de XR (evita que el mouse mueva la cámara en PC)
        if (xrSimulator != null)
            xrSimulator.SetActive(false);

        ConversationManager.Instance.StartConversation(conversation);
    }

    /// <summary>
    /// Restaura el control cuando el Dialogue Editor termina la charla.
    /// </summary>
    private void OnConversationEnded()
    {
        Debug.Log("Fin del diálogo. Controles restaurados.");
        isInConversation = false;

        if (playerMovementScript != null)
            playerMovementScript.enabled = true;

        if (playerRigidbody != null)
            playerRigidbody.isKinematic = false;

        if (xrSimulator != null)
            xrSimulator.SetActive(true);

        Time.timeScale = 1f; // Asegurar que el tiempo no quede en 0
    }

    // ========================================================
    // COMPATIBILIDAD CON CARDBOARD Y VRBOXCONTROLLER
    // ========================================================

    // Se activa con el Botón B de tu VRBoxController actual
    public void OnInteract()
    {
        StartDialogue();
    }

    // Se activa cuando haces "click" (tap en pantalla o botón magnético) mirando al NPC
    public void OnPointerClick()
    {
        StartDialogue();
    }

    // Funciones vacías para evitar errores de consola cuando el puntero mira al NPC
    public void OnPointerEnter() { }
    public void OnPointerExit() { }
}