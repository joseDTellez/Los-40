using UnityEngine;
using UnityEngine.UI;
using DialogueEditor;

public class NPCIndicatorAndInteraction : MonoBehaviour
{
    private bool _conversationActive = false;
    private CanvasGroup _indicatorCG;
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

        // Posición
        indicatorRoot.position = transform.position + indicatorOffset;

        // Billboard
        indicatorRoot.forward = Camera.main.transform.forward;

        // Escala por distancia
        float dist = Vector3.Distance(_player.position, transform.position);
        float t = Mathf.InverseLerp(minDistance, maxDistance, dist);
        float s = Mathf.Lerp(minScale, maxScale, t);
        indicatorRoot.localScale = Vector3.one * Mathf.Clamp(s, minScale, maxScale);
    }

    // ─── GAZE ─────────────────────────────

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

        float dist = Vector3.Distance(_player.position, transform.position);
        bool canInteract = _isGazing && dist <= maxGazeDistance;

        SetInteractionIconVisible(canInteract);
    }

    private void SetInteractionIconVisible(bool visible)
    {
        if (gazeInteractionIcon != null)
            gazeInteractionIcon.gameObject.SetActive(visible);
    }

    private System.Collections.IEnumerator SwapSprite(Sprite newSprite)
    {
        float duration = 0.15f;

        // Fade OUT
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            _indicatorCG.alpha = Mathf.Lerp(1, 0, t / duration);
            yield return null;
        }

        // Cambiar sprite
        indicatorImage.sprite = newSprite;

        // Fade IN
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
        _isVisited = true;
        RefreshIndicatorSprite();
    }
    private void OnConversationStart()
    {
        _conversationActive = true;

        // Ocultar inmediatamente
        SetInteractionIconVisible(false);
    }

    private void OnConversationEnd()
    {
        _conversationActive = false;

        // Re-evaluar estado normal
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