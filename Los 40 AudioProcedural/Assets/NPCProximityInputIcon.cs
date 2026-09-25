using UnityEngine;
using UnityEngine.UI;
using DialogueEditor;

[RequireComponent(typeof(Collider))]
public class NPCProximityInputIcon : MonoBehaviour
{
    private bool _conversationActive = false;
    [Header("Input Icon (World Space)")]
    public Image inputIconImage;
    public Transform inputIconRoot;

    private void Start()
    {
        if (inputIconImage != null)
            inputIconImage.gameObject.SetActive(false);

        GetComponent<Collider>().isTrigger = true;
    }
    private void Update()
    {
        if (inputIconRoot == null || Camera.main == null) return;

        Vector3 camPos = Camera.main.transform.position;

        // Ignorar diferencia en altura (Y)
        camPos.y = inputIconRoot.position.y;

        inputIconRoot.LookAt(camPos);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        SetVisible(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (inputIconImage != null)
            inputIconImage.gameObject.SetActive(visible);
    }
    private void OnConversationStart()
    {
        _conversationActive = true;
        SetVisible(false);
    }

    private void OnConversationEnd()
    {
        _conversationActive = false;
    }
    private void OnEnable()
    {
        ConversationManager.OnConversationStarted += OnConversationStart;
        ConversationManager.OnConversationEnded += OnConversationEnd;
    }

    private void OnDisable()
    {
        ConversationManager.OnConversationStarted -= OnConversationStart;
        ConversationManager.OnConversationEnded -= OnConversationEnd;
    }
}