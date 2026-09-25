using UnityEngine; // Importa las funciones base de Unity
using UnityEngine.InputSystem; // Importa el sistema de entrada
using DialogueEditor; // Importa el sistema de diálogos

public class DialogueVRBridge : MonoBehaviour // Clase principal para el puente de VR
{
    public VRBoxController vrController; // Referencia al script de movimiento físico
    public GameObject xrSimulatorObject; // Referencia al objeto del simulador (usando GameObject para evitar errores)
    private bool isConversing = false; // Estado para controlar si hay una charla activa

    private void Awake() // Se ejecuta al iniciar el juego
    {
        if (vrController == null) vrController = GetComponent<VRBoxController>(); // Busca el controlador si no está asignado
    }

    private void OnEnable() // Se ejecuta al activar el componente
    {
        ConversationManager.OnConversationStarted += StartDialogueState; // Suscribe la función al inicio de conversación
        ConversationManager.OnConversationEnded += EndDialogueState; // Suscribe la función al fin de conversación
    }

    private void OnDisable() // Se ejecuta al desactivar el componente
    {
        ConversationManager.OnConversationStarted -= StartDialogueState; // Desvincula la función al inicio de conversación
        ConversationManager.OnConversationEnded -= EndDialogueState; // Desvincula la función al fin de conversación
    }

    private void StartDialogueState() // Define qué pasa al empezar el diálogo
    {
        isConversing = true; // Cambia el estado a conversando
        if (vrController != null) // Verifica si el controlador existe
        {
            vrController.canMove = false; // Bloquea el caminar del jugador
            vrController.canInteract = false; // Bloquea el rayo de interacción
        }
        if (xrSimulatorObject != null) xrSimulatorObject.SetActive(false); // Desactiva el objeto del simulador para bloquear el teclado
    }

    private void EndDialogueState() // Define qué pasa al terminar el diálogo
    {
        isConversing = false; // Cambia el estado a no conversando
        if (vrController != null) // Verifica si el controlador existe
        {
            vrController.canMove = true; // Reactiva el caminar del jugador
            vrController.canInteract = true; // Reactiva el rayo de interacción
        }
        if (xrSimulatorObject != null) xrSimulatorObject.SetActive(true); // Reactiva el objeto del simulador
    }

    private void Update() // Se ejecuta en cada frame
    {
        if (!isConversing || ConversationManager.Instance == null) return; // Si no hay charla o manager, detiene la ejecución
        HandleKeyboard(); // Llama a la lectura de teclado
        HandleGamepad(); // Llama a la lectura de mando
    }

    private void HandleKeyboard() // Procesa la entrada de teclado
    {
        if (Keyboard.current == null) return; // Sale si no hay teclado detectado
        if (Keyboard.current.upArrowKey.wasPressedThisFrame) ConversationManager.Instance.SelectPreviousOption(); // Flecha arriba: opción anterior
        if (Keyboard.current.downArrowKey.wasPressedThisFrame) ConversationManager.Instance.SelectNextOption(); // Flecha abajo: siguiente opción
        if (Keyboard.current.enterKey.wasPressedThisFrame) ConversationManager.Instance.PressSelectedOption(); // Tecla Enter: confirmar selección
    }

    private void HandleGamepad() // Procesa la entrada de mando/gamepad
    {
        if (Gamepad.current == null) return; // Sale si no hay mando detectado
        if (Gamepad.current.dpad.up.wasPressedThisFrame || Gamepad.current.leftStick.up.wasPressedThisFrame) ConversationManager.Instance.SelectPreviousOption(); // Arriba: opción anterior
        if (Gamepad.current.dpad.down.wasPressedThisFrame || Gamepad.current.leftStick.down.wasPressedThisFrame) ConversationManager.Instance.SelectNextOption(); // Abajo: siguiente opción
        if (Gamepad.current.buttonSouth.wasPressedThisFrame) ConversationManager.Instance.PressSelectedOption(); // Botón A: confirmar selección
    }
}