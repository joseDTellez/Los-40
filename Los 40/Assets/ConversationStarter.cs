using UnityEngine;
using UnityEngine.InputSystem;
using DialogueEditor;
using System.Collections;

public class ConversationStarter : MonoBehaviour
{
    public enum JoystickOrientation { Standard, Rotated90Degrees, RotatedMinus90Degrees }

    [Header("Configuración de Diálogo")]
    [SerializeField] private NPCConversation conversation;

    [Header("Componentes del Jugador")]
    [SerializeField] private VRBoxController playerMovementScript;
    [SerializeField] private Rigidbody playerRigidbody;

    [Header("Ajuste de VR Box (Joystick Rotado)")]
    [Tooltip("Standard: Y = Arriba/Abajo. Rotated: X = Arriba/Abajo.")]
    public JoystickOrientation orientation = JoystickOrientation.Rotated90Degrees;

    [Tooltip("Invertir si el joystick responde al revés de lo deseado")]
    public bool invertSelection = false;

    [Range(0.1f, 0.9f)]
    public float deadzone = 0.5f;
    public float scrollDelay = 0.3f;

    [Header("Input Manual")]
    [SerializeField] private Key interactKey = Key.L;

    private bool playerInside = false;
    private bool isInConversation = false;
    private bool canScroll = true;

    private void OnEnable() => ConversationManager.OnConversationEnded += OnConversationEnded;
    private void OnDisable() => ConversationManager.OnConversationEnded -= OnConversationEnded;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerInside = false;
    }

    private void Update()
    {
        if (!playerInside) return;

        // Si estamos en conversación, procesamos la navegación corregida
        if (isInConversation)
        {
            HandleNavigation();
            return;
        }

        // Lógica de inicio (Teclado o Gamepad)
        if (Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame)
            StartDialogue();

        if (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.rightShoulder.wasPressedThisFrame))
            StartDialogue();
    }

    private void HandleNavigation()
    {
        if (Gamepad.current == null || !canScroll) return;

        Vector2 stick = Gamepad.current.leftStick.ReadValue();
        float moveValue = 0;

        // --- CORRECCIÓN DE ROTACIÓN ---
        switch (orientation)
        {
            case JoystickOrientation.Standard:
                moveValue = stick.y; // El estándar usa el eje vertical
                break;
            case JoystickOrientation.Rotated90Degrees:
                moveValue = stick.x; // Si derecha es arriba, usamos el eje horizontal
                break;
            case JoystickOrientation.RotatedMinus90Degrees:
                moveValue = -stick.x;
                break;
        }

        if (invertSelection) moveValue *= -1;

        // Ejecutar selección
        if (Mathf.Abs(moveValue) > deadzone)
        {
            if (moveValue > 0)
                ConversationManager.Instance.SelectPreviousOption(); // Sube en la lista
            else
                ConversationManager.Instance.SelectNextOption();     // Baja en la lista

            StartCoroutine(ScrollCooldown());
        }
    }

    private IEnumerator ScrollCooldown()
    {
        canScroll = false;
        yield return new WaitForSecondsRealtime(scrollDelay);
        canScroll = true;
    }

    public void StartDialogue()
    {
        if (isInConversation) return;
        isInConversation = true;

        if (playerMovementScript != null)
        {
            playerMovementScript.canMove = false;
            playerMovementScript.canInteract = false;
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        if (xrSimulator != null) xrSimulator.SetActive(false);

        ConversationManager.Instance.StartConversation(conversation);
    }

    private void OnConversationEnded()
    {
        isInConversation = false;
        if (playerMovementScript != null)
        {
            playerMovementScript.canMove = true;
            playerMovementScript.canInteract = true;
        }
        if (xrSimulator != null) xrSimulator.SetActive(true);
    }

    [Header("Simulador PC")]
    [SerializeField] private GameObject xrSimulator;

    public void OnInteract() => StartDialogue();
    public void OnPointerClick() => StartDialogue();
}