using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DialogueEditor;

public class NPCIndicatorAndInteraction : MonoBehaviour
{
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
    private Transform _camera;
    private CanvasGroup _indicatorCG;
    private Coroutine _swapRoutine;
    private bool _conversationActive = false;
    private bool _isVisited = false;
    private bool _isGazing = false;

    void Start()
    {
        if (indicatorImage != null)
        {
            _indicatorCG = indicatorImage.GetComponent<CanvasGroup>();
            if (_indicatorCG == null) _indicatorCG = indicatorImage.gameObject.AddComponent<CanvasGroup>();
            _indicatorCG.alpha = 1f;
        }

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

        if (_camera == null)
        {
            if (Camera.main == null) return;
            _camera = Camera.main.transform;
        }

        // 1. Posicionamiento y orientación del indicador hacia la cámara
        indicatorRoot.position = transform.position + indicatorOffset;
        indicatorRoot.forward = _camera.forward;

        // 2. Escala por distancia
        float dist = Vector3.Distance(_player.position, transform.position);
        float t = Mathf.InverseLerp(minDistance, maxDistance, dist);
        indicatorRoot.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, t);

        // 3. Interacción: solo si estamos mirando, cerca y sin otra charla en curso
        if (_isGazing && !_conversationActive && dist <= maxGazeDistance && InteraccionPulsada())
            ComenzarInteraccion();
    }

    private static bool InteraccionPulsada()
    {
        return (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame) ||
               (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame) ||
               (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
    }

    private void ComenzarInteraccion()
    {
        ConversationManager cm = ConversationManager.Instance;
        if (cm == null || cm.IsConversationActive) return;

        if (myConversation == null)
        {
            Debug.LogWarning($"NPC '{name}': no hay una conversación asignada en el Inspector.", this);
            return;
        }

        cm.StartConversation(myConversation);
        MarkAsVisited();
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
        if (_conversationActive || _player == null)
        {
            SetInteractionIconVisible(false);
            return;
        }

        float dist = Vector3.Distance(_player.position, transform.position);
        SetInteractionIconVisible(_isGazing && dist <= maxGazeDistance);
    }

    private void SetInteractionIconVisible(bool visible)
    {
        if (gazeInteractionIcon != null)
            gazeInteractionIcon.gameObject.SetActive(visible);
    }

    // ─── VISUALES ─────────────────────────────

    private IEnumerator SwapSprite(Sprite newSprite)
    {
        const float duration = 0.15f;

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            _indicatorCG.alpha = Mathf.Lerp(1f, 0f, t / duration);
            yield return null;
        }

        indicatorImage.sprite = newSprite;

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            _indicatorCG.alpha = Mathf.Lerp(0f, 1f, t / duration);
            yield return null;
        }

        _indicatorCG.alpha = 1f;
        _swapRoutine = null;
    }

    private void RefreshIndicatorSprite()
    {
        if (indicatorImage == null || _indicatorCG == null) return;

        if (_swapRoutine != null) StopCoroutine(_swapRoutine);
        _swapRoutine = StartCoroutine(SwapSprite(_isVisited ? visitedSprite : notVisitedSprite));
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
