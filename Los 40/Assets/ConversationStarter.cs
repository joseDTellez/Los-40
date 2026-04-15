using UnityEngine;
using UnityEngine.InputSystem;
using DialogueEditor;

public class ConversationStarter : MonoBehaviour
{
    [Header("Conversation")]
    [SerializeField] private NPCConversation conversation;

    [Header("Player")]
    [SerializeField] private MonoBehaviour playerMovement;

    [Header("Input")]
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
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            Debug.Log("Jugador DENTRO del trigger");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            Debug.Log("Jugador FUERA del trigger");
        }
    }

    private void Update()
    {
        if (!playerInside || isInConversation) return;

        if (Keyboard.current[interactKey].wasPressedThisFrame)
        {
            StartDialogue();
        }
    }

    private void StartDialogue()
    {
        Debug.Log("🟢 Iniciando conversación");

        if (playerMovement != null)
            playerMovement.enabled = false;

        isInConversation = true;

        ConversationManager.Instance.StartConversation(conversation);
    }

    private void OnConversationEnded()
    {
        Debug.Log("🔴 Finalizó conversación");

        if (playerMovement != null)
            playerMovement.enabled = true;

        isInConversation = false;
    }
}