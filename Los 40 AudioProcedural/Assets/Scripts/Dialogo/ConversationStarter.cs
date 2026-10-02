using UnityEngine;
using UnityEngine.InputSystem;
using DialogueEditor;

// Inicia una conversacion cuando el jugador esta dentro del trigger y pulsa la tecla de interaccion.
// La navegacion dentro del dialogo la hace DialogueInputManager y el bloqueo del jugador
// DialogueControllerBridge.
public class ConversationStarter : MonoBehaviour
{
    [Header("Configuración de Diálogo")]
    [SerializeField] private NPCConversation conversation;

    [Header("Input Manual")]
    [SerializeField] private Key interactKey = Key.L;

    // Contador en vez de bool: el jugador puede tener varios colliders con el tag Player
    private int collidersDentro = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) collidersDentro++;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) collidersDentro = Mathf.Max(0, collidersDentro - 1);
    }

    private void OnDisable()
    {
        collidersDentro = 0;
    }

    private void Update()
    {
        if (collidersDentro == 0) return;

        bool pulsado = (Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame) ||
                       (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame ||
                                                    Gamepad.current.rightShoulder.wasPressedThisFrame));
        if (pulsado) StartDialogue();
    }

    public void StartDialogue()
    {
        ConversationManager cm = ConversationManager.Instance;

        // Si ya hay una conversacion (o se esta cerrando) no la reiniciamos
        if (conversation == null || cm == null || cm.IsConversationActive) return;

        cm.StartConversation(conversation);
    }

    public void OnInteract() => StartDialogue();
    public void OnPointerClick() => StartDialogue();
}
