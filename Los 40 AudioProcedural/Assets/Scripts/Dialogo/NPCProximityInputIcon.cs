using UnityEngine;
using UnityEngine.UI;
using DialogueEditor;

// Muestra el icono de "puedes hablar" mientras el jugador está dentro del trigger del NPC.
[RequireComponent(typeof(Collider))]
public class NPCProximityInputIcon : MonoBehaviour
{
    [Header("Input Icon (World Space)")]
    public Image inputIconImage;
    public Transform inputIconRoot;

    private bool _conversationActive = false;
    // Contador en vez de bool: el jugador puede tener varios colliders con el tag Player
    private int _collidersDentro = 0;
    private Transform _camera;

    private void Start()
    {
        SetVisible(false);
        GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
        if (inputIconRoot == null || !IconoVisible()) return;

        if (_camera == null)
        {
            if (Camera.main == null) return;
            _camera = Camera.main.transform;
        }

        // Mira hacia la cámara ignorando la diferencia de altura (Y)
        Vector3 camPos = _camera.position;
        camPos.y = inputIconRoot.position.y;
        inputIconRoot.LookAt(camPos);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _collidersDentro++;
        Refrescar();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _collidersDentro = Mathf.Max(0, _collidersDentro - 1);
        Refrescar();
    }

    private void Refrescar()
    {
        SetVisible(_collidersDentro > 0 && !_conversationActive);
    }

    private bool IconoVisible()
    {
        return inputIconImage != null && inputIconImage.gameObject.activeSelf;
    }

    private void SetVisible(bool visible)
    {
        if (inputIconImage != null)
            inputIconImage.gameObject.SetActive(visible);
    }

    private void OnConversationStart()
    {
        _conversationActive = true;
        Refrescar();
    }

    private void OnConversationEnd()
    {
        // Si el jugador sigue al lado del NPC, el icono vuelve a aparecer
        _conversationActive = false;
        Refrescar();
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
        _collidersDentro = 0;
    }
}
