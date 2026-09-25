using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class NPCIndicatorSystem : MonoBehaviour
{
    // ─── INDICATOR (World Space) ─────────────────────────────
    [Header("World-Space Indicator")]
    public Transform indicatorRoot;
    public Image indicatorImage;
    public Sprite notVisitedSprite;
    public Sprite visitedSprite;
    public Vector3 indicatorOffset = new Vector3(0f, 2.5f, 0f);

    [Header("Indicator Scale by Distance")]
    public float minDistance = 2f;
    public float maxDistance = 15f;
    public float minScale = 0.4f;
    public float maxScale = 1.6f;

    // ─── INPUT ICON ──────────────────────────────────────────
    [Header("World-Space Input Icon")]
    public Image inputIconImage;
    public Sprite inputIconSprite;

    // ─── INTERACTION ICON (SCREEN SPACE) ─────────────────────
    [Header("Screen-Space Interaction Icon")]
    public Image gazeInteractionIcon;
    public Sprite interactionSprite;

    // ─── ESTADO ──────────────────────────────────────────────
    private Transform _player;
    private bool _isVisited = false;
    private bool _playerInTrigger = false;
    private bool _isGazing = false;

    // ════════════════════════════════════════════════════════
    // INIT
    // ════════════════════════════════════════════════════════

    private void Start()
    {
        GameObject playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
            _player = playerGO.transform;
        else
            Debug.LogWarning("No se encontró objeto con tag Player");

        // Asegurar trigger
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        // Estado inicial UI
        SetInputIconVisible(false);
        SetInteractionIconVisible(false);
        RefreshIndicatorSprite();

        // Asignar sprites
        if (gazeInteractionIcon != null && interactionSprite != null)
            gazeInteractionIcon.sprite = interactionSprite;

        if (inputIconImage != null && inputIconSprite != null)
            inputIconImage.sprite = inputIconSprite;
    }

    // ════════════════════════════════════════════════════════
    // UPDATE
    // ════════════════════════════════════════════════════════

    private void Update()
    {
        if (_player == null || indicatorRoot == null) return;

        // Posición sobre el NPC
        indicatorRoot.position = transform.position + indicatorOffset;

        // Mirar a la cámara
        indicatorRoot.LookAt(
            indicatorRoot.position + Camera.main.transform.rotation * Vector3.forward,
            Camera.main.transform.rotation * Vector3.up
        );

        // Escalado por distancia
        float dist = Vector3.Distance(_player.position, transform.position);
        float t = Mathf.InverseLerp(minDistance, maxDistance, dist);
        float s = Mathf.Lerp(minScale, maxScale, t);
        s = Mathf.Clamp(s, minScale, maxScale);

        indicatorRoot.localScale = Vector3.one * s;
    }

    // ════════════════════════════════════════════════════════
    // TRIGGERS
    // ════════════════════════════════════════════════════════

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _playerInTrigger = true;
        EvaluateGazeUI();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _playerInTrigger = false;
        EvaluateGazeUI();
    }

    // ════════════════════════════════════════════════════════
    // GAZE (COMPATIBLE CON CARDBOARD)
    // ════════════════════════════════════════════════════════

    public void OnPointerEnter()
    {
        _isGazing = true;
        EvaluateGazeUI();
    }

    public void OnPointerExit()
    {
        _isGazing = false;
        EvaluateGazeUI();
    }

    // (Opcional, evita warnings)
    public void OnPointerClick()
    {
        Debug.Log("Click en NPC");
    }

    // ════════════════════════════════════════════════════════
    // LÓGICA
    // ════════════════════════════════════════════════════════

private void EvaluateGazeUI()
{
    // Input icon → SOLO proximidad
    SetInputIconVisible(_playerInTrigger);

    // Interaction icon → proximidad + mirada
    SetInteractionIconVisible(_playerInTrigger && _isGazing);
}

    private void SetInputIconVisible(bool visible)
    {
        if (inputIconImage != null)
            inputIconImage.gameObject.SetActive(visible);
    }

    private void SetInteractionIconVisible(bool visible)
    {
        if (gazeInteractionIcon != null)
            gazeInteractionIcon.gameObject.SetActive(visible);
    }

    private void RefreshIndicatorSprite()
    {
        if (indicatorImage == null) return;

        indicatorImage.sprite = _isVisited ? visitedSprite : notVisitedSprite;
    }

    // ════════════════════════════════════════════════════════
    // API
    // ════════════════════════════════════════════════════════

    public void MarkAsVisited()
    {
        _isVisited = true;
        RefreshIndicatorSprite();
    }

    public bool IsVisited => _isVisited;
}