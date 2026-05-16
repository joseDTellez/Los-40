using UnityEngine;
using UnityEngine.UI;
using DialogueEditor;
using UnityEngine.InputSystem; // Necesario para detectar los periféricos

public class NPCIndicatorAndInteraction : MonoBehaviour
{
    private bool _conversationActive = false;
    private CanvasGroup _indicatorCG;

    [Header("Conversation Data")]
    public NPCConversation myConversation; // Arrastra aquí tu asset de Dialogue Editor

    [Header("Indicator (World Space)")]
    public Transform indicatorRoot;
    public Image indicatorImage;
    public Sprite notVisitedSprite;
    public Sprite visitedSprite;
    public Vector3 indicatorOffset = new Vector3(0f, 2.5f, 0f);

    [Header("Scale by Distance")]
    public float minDistance = 2f;
    public float maxDistance = 15f;
    public float minScale = 0.4f;
    public float maxScale = 1.6f;

    [Header("Interaction Icon (Screen Space)")]
    public Image gazeInteractionIcon;
    public Sprite interactionSprite;

    [Header("Gaze Distance")]
    public float maxGazeDistance = 6f;

    private Transform _player;
    private bool _isVisited = false;
    private bool _isGazing = false;

    void Start()
    {
        _indicatorCG = indicatorImage.GetComponent<CanvasGroup>();
        if (_indicatorCG == null) _indicatorCG = indicatorImage.gameObject.AddComponent<CanvasGroup>();

        _indicatorCG.alpha = 1f;

        GameObject playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
            _player = playerGO.transform;

        SetInteractionIconVisible(false);
        RefreshIndicatorSprite();

        if (gazeInteractionIcon != null && interactionSprite != null)
            gazeInteractionIcon.sprite = interactionSprite;
    }

    void Update()
    {
        if (_player == null || indicatorRoot == null) return;

        // 1. Posicionamiento y Billboard del indicador
        indicatorRoot.position = transform.position + indicatorOffset;
        indicatorRoot.forward = Camera.main.transform.forward;

        // 2. Escala por distancia
        float dist = Vector3.Distance(_player.position, transform.position);
        float t = Mathf.InverseLerp(minDistance, maxDistance, dist);
        float s = Mathf.Lerp(minScale, maxScale, t);
        indicatorRoot.localScale = Vector3.one * Mathf.Clamp(s, minScale, maxScale);

        // 3. DETECCIÓN DE INTERACCIÓN (Input)
        // Solo si estamos mirando, no estamos en una charla y estamos cerca
        if (_isGazing && !_conversationActive && dist <= maxGazeDistance)
        {
            bool interactPressed = (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame) ||
                                   (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame) ||
                                   (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

            if (interactPressed)
            {
                ComenzarInteraccion();
            }
        }
    }

    private void ComenzarInteraccion()
    {
        if (myConversation != null)
        {
            // Iniciar la conversación con Dialogue Editor
            ConversationManager.Instance.StartConversation(myConversation);
            MarkAsVisited();
        }
        else
        {
            Debug.LogWarning("NPC: No hay una conversación asignada en el Inspector.");
        }
    }

    // ─── GAZE / POINTER ─────────────────────────────

    public void OnPointerEnter()
    {
        _isGazing = true;
        EvaluateUI();
    }

    public void OnPointerExit()
    {
        _isGazing = false;
        EvaluateUI();
    }

    private void EvaluateUI()
    {
        if (_conversationActive)
        {
            SetInteractionIconVisible(false);
            return;
        }

        if (_player == null) return;

        float dist = Vector3.Distance(_player.position, transform.position);
        bool canInteract = _isGazing && dist <= maxGazeDistance;

        SetInteractionIconVisible(canInteract);
    }

    private void SetInteractionIconVisible(bool visible)
    {
        if (gazeInteractionIcon != null)
            gazeInteractionIcon.gameObject.SetActive(visible);
    }

    // ─── VISUALES ─────────────────────────────

    private System.Collections.IEnumerator SwapSprite(Sprite newSprite)
    {
        float duration = 0.15f;
        float t = 0;

        while (t < duration)
        {
            t += Time.deltaTime;
            _indicatorCG.alpha = Mathf.Lerp(1, 0, t / duration);
            yield return null;
        }

        indicatorImage.sprite = newSprite;

        t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            _indicatorCG.alpha = Mathf.Lerp(0, 1, t / duration);
            yield return null;
        }

        _indicatorCG.alpha = 1;
    }

    private void RefreshIndicatorSprite()
    {
        if (indicatorImage == null) return;
        StopCoroutine("SwapSprite");
        StartCoroutine(SwapSprite(_isVisited ? visitedSprite : notVisitedSprite));
    }

    public void MarkAsVisited()
    {
        if (_isVisited) return;
        _isVisited = true;
        RefreshIndicatorSprite();
    }

    // ─── EVENTOS DE CONVERSACIÓN ─────────────────────

    private void OnConversationStart()
    {
        _conversationActive = true;
        SetInteractionIconVisible(false);
    }

    private void OnConversationEnd()
    {
        _conversationActive = false;
        EvaluateUI();
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