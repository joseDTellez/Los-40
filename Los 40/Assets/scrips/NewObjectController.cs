using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider))]
public class NewObjectController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    // INSPECTOR
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Cámara")]
    public Transform cameraTransform;

    [Header("Inspección")]
    public float distanceInFront = 0.7f;
    public float transitionSpeed = 5f;

    [Header("Sonidos")]
    public AudioSource audioSource;
    public AudioClip soundEntry;
    public AudioClip soundExit;
    public AudioClip soundHover;

    [Header("Outline (hover visual)")]
    public Image loadingCircle;

    [Header("UI — Hover")]
    public GameObject hoverPanelRoot;

    [Header("UI — Objetivo inicial")]
    public GameObject objectivePanelRoot;
    public float fadeDuration = 0.8f;
    public float displayDuration = 3f;

    [Header("UI — Objetivo final")]
    public GameObject objectiveFinalPanelRoot;
    public float fadeDurationFinal = 0.8f;
    public float displayDurationFinal = 3f;

    // ─────────────────────────────────────────────────────────────────────────
    // PRIVADOS
    // ─────────────────────────────────────────────────────────────────────────

    private enum UIState { None, Hover, ObjectiveInitial, ObjectiveFinal }

    private UIState _currentUIState = UIState.None;
    private Coroutine _uiTransitionCoroutine;

    private Outline _outline;
    private bool _isGazing = false;
    private bool _isNear = false;

    private Vector3 _origPos;
    private Quaternion _origRot;
    private Vector3 _inspectPos;
    private Quaternion _inspectRot;

    private CanvasGroup _hoverCanvasGroup;
    private CanvasGroup _objectiveCanvasGroup;
    private CanvasGroup _objectiveFinalCanvasGroup;

    // ─────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        _outline = GetComponent<Outline>();
        if (_outline != null) _outline.enabled = false;

        _origPos = transform.position;
        _origRot = transform.rotation;

        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        InicializarPanel(hoverPanelRoot, ref _hoverCanvasGroup);
        InicializarPanel(objectivePanelRoot, ref _objectiveCanvasGroup);
        InicializarPanel(objectiveFinalPanelRoot, ref _objectiveFinalCanvasGroup);
    }

    void Update()
    {
        MoverObjeto();
        LeerInput();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // INPUT
    // ─────────────────────────────────────────────────────────────────────────

    private void LeerInput()
    {
        if (!_isGazing) return;

        if (Keyboard.current.kKey.wasPressedThisFrame)
            AlternarInspeccion();

        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
            AlternarInspeccion();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // INSPECCIÓN
    // ─────────────────────────────────────────────────────────────────────────

    private void AlternarInspeccion()
    {
        // Si ya está cerca, ignorar (la suelta ocurre en OnPointerExit)
        if (_isNear) return;

        _inspectPos = cameraTransform.position + (cameraTransform.forward * distanceInFront);
        _inspectRot = Quaternion.LookRotation(cameraTransform.position - _inspectPos);

        _isNear = true;
        PlaySound(soundEntry);

        // Ocultar hover al agarrar y mostrar objetivo inicial
        CambiarUIState(UIState.ObjectiveInitial);
    }

    private void MoverObjeto()
    {
        var targetPos = _isNear ? _inspectPos : _origPos;
        var targetRot = _isNear ? _inspectRot : _origRot;

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * transitionSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * transitionSpeed);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // EVENTOS DE GAZE
    // ─────────────────────────────────────────────────────────────────────────

    public void OnPointerEnter()
    {
        _isGazing = true;
        if (_outline) _outline.enabled = true;

        // Solo mostrar hover si el objeto está libre (no inspeccionado)
        if (!_isNear && _currentUIState == UIState.None)
        {
            CambiarUIState(UIState.Hover);
            PlayHoverSound();
        }
    }

    public void OnPointerExit()
    {
        _isGazing = false;

        // Si no está cerca, solo ocultar hover
        if (!_isNear)
            CambiarUIState(UIState.None);

        StartCoroutine(EsperaRegreso());
    }

    private IEnumerator EsperaRegreso()
    {
        yield return new WaitForSeconds(0.1f);

        if (!_isGazing)
        {
            if (_isNear)
            {
                // Soltar el objeto → mostrar objetivo final
                _isNear = false;
                PlaySound(soundExit);
                CambiarUIState(UIState.ObjectiveFinal);
            }

            if (_outline) _outline.enabled = false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SISTEMA DE UI CON ESTADOS
    // ─────────────────────────────────────────────────────────────────────────

    private void CambiarUIState(UIState newState)
    {
        if (_currentUIState == newState) return;

        if (_uiTransitionCoroutine != null)
            StopCoroutine(_uiTransitionCoroutine);

        _uiTransitionCoroutine = StartCoroutine(TransicionUI(_currentUIState, newState));
    }

    private IEnumerator TransicionUI(UIState fromState, UIState toState)
    {
        // 1. Fade OUT del panel actualmente visible
        yield return StartCoroutine(FadeOutSegunEstado(fromState));

        // 2. Actualizar estado solo después del fade out
        _currentUIState = toState;

        // 3. Fade IN del nuevo panel
        switch (toState)
        {
            case UIState.None:
                break;

            case UIState.Hover:
                yield return StartCoroutine(FadeCanvasGroup(
                    _hoverCanvasGroup, 0f, 1f, fadeDuration, hoverPanelRoot, false));
                break;

            case UIState.ObjectiveInitial:
                yield return StartCoroutine(FadeCanvasGroup(
                    _objectiveCanvasGroup, 0f, 1f, fadeDuration, objectivePanelRoot, false));
                yield return new WaitForSeconds(displayDuration);
                CambiarUIState(UIState.None);
                break;

            case UIState.ObjectiveFinal:
                yield return StartCoroutine(FadeCanvasGroup(
                    _objectiveFinalCanvasGroup, 0f, 1f, fadeDurationFinal, objectiveFinalPanelRoot, false));
                yield return new WaitForSeconds(displayDurationFinal);
                CambiarUIState(UIState.None);
                break;
        }
    }

    private IEnumerator FadeOutSegunEstado(UIState state)
    {
        switch (state)
        {
            case UIState.Hover:
                yield return StartCoroutine(FadeCanvasGroup(
                    _hoverCanvasGroup, _hoverCanvasGroup.alpha, 0f,
                    fadeDuration, hoverPanelRoot, true));
                break;

            case UIState.ObjectiveInitial:
                yield return StartCoroutine(FadeCanvasGroup(
                    _objectiveCanvasGroup, _objectiveCanvasGroup.alpha, 0f,
                    fadeDuration, objectivePanelRoot, true));
                break;

            case UIState.ObjectiveFinal:
                yield return StartCoroutine(FadeCanvasGroup(
                    _objectiveFinalCanvasGroup, _objectiveFinalCanvasGroup.alpha, 0f,
                    fadeDurationFinal, objectiveFinalPanelRoot, true));
                break;

            case UIState.None:
            default:
                yield break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UTILIDADES
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration,
                                         GameObject panel = null, bool desactivarAlFinal = false)
    {
        if (panel != null) panel.SetActive(true);

        float elapsed = 0f;
        cg.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        cg.alpha = to;

        if (desactivarAlFinal && to == 0f && panel != null)
            panel.SetActive(false);
    }

    private void InicializarPanel(GameObject panel, ref CanvasGroup canvasGroup)
    {
        if (panel == null) return;

        canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = panel.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        panel.SetActive(false);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    private void PlayHoverSound()
    {
        if (audioSource != null && soundHover != null)
            audioSource.PlayOneShot(soundHover);
    }
}