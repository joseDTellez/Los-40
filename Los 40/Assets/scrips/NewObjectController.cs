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

    [Header("Gaze Settings (Anti-Flicker)")]
    [Tooltip("Tiempo en segundos antes de considerar que el usuario dejó de mirar el objeto. Absorbe temblores.")]
    public float graceTime = 0.25f;

    [Header("Sonidos")]
    public AudioSource audioSource;
    public AudioClip soundEntry;         // Sonido al acercar el objeto
    public AudioClip soundExit;          // Sonido al alejar el objeto
    public AudioClip soundHover;         // Sonido al mirar el objeto (hover)

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

    private Outline _outline;

    // Estados
    private bool _isGazing = false;
    private bool _isExiting = false;       // MEJORA: Bandera para saber si estamos en el "tiempo de gracia"
    private bool _isNear = false;
    private bool _hasInteracted = false;

    // Transformaciones
    private Vector3 _origPos;
    private Quaternion _origRot;
    private Vector3 _inspectPos;
    private Quaternion _inspectRot;

    // Canvas Groups
    private CanvasGroup _hoverCanvasGroup;
    private CanvasGroup _objectiveCanvasGroup;
    private CanvasGroup _objectiveFinalCanvasGroup;

    // Coroutines
    private Coroutine _hoverCoroutine;
    private Coroutine _fadeCoroutine;
    private Coroutine _fadeFinalCoroutine;
    private Coroutine _exitRoutine;

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

        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
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

        if (!_hasInteracted)
        {
            _hasInteracted = true;
            MostrarPanel(objectivePanelRoot, _objectiveCanvasGroup,
                         fadeDuration, displayDuration, ref _fadeCoroutine);
        }
    }

    private void MoverObjeto()
    {
        var targetPos = _isNear ? _inspectPos : _origPos;
        var targetRot = _isNear ? _inspectRot : _origRot;

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * transitionSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * transitionSpeed);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // EVENTOS DE GAZE (Sistema Anti-Flicker Mejorado)
    // ─────────────────────────────────────────────────────────────────────────

    public void OnPointerEnter()
    {
        // 1. Si estábamos a punto de salir (en el tiempo de gracia), interceptamos y cancelamos la salida.
        if (_isExiting)
        {
            if (_exitRoutine != null) StopCoroutine(_exitRoutine);
            _isExiting = false;
            return; // Retornamos para no reiniciar animaciones de hover ni sonidos.
        }

        // 2. Si ya lo estábamos mirando de forma estable, ignoramos activaciones duplicadas del visor.
        if (_isGazing) return;

        // 3. Inicio limpio del Gaze
        _isGazing = true;
        if (_outline) _outline.enabled = true;

        if (!_isNear)
        {
            MostrarHover();
            PlayHoverSound();
        }
    }

    public void OnPointerExit()
    {
        if (!gameObject.activeInHierarchy || !_isGazing) return;

        // En vez de salir de golpe, iniciamos la rutina de gracia
        if (_exitRoutine != null) StopCoroutine(_exitRoutine);
        _exitRoutine = StartCoroutine(GracePeriodRoutine());
    }

    private IEnumerator GracePeriodRoutine()
    {
        _isExiting = true;

        // Esperamos para ver si fue un temblor temporal del jugador
        yield return new WaitForSeconds(graceTime);

        // Si llegamos a esta línea, el jugador realmente dejó de mirar el objeto. Ejecutamos la salida real.
        _isExiting = false;
        _isGazing = false;

        if (!_isNear) OcultarHover();

        if (_isNear)
        {
            _isNear = false;
            PlaySound(soundExit);

            MostrarPanel(objectiveFinalPanelRoot, _objectiveFinalCanvasGroup,
                         fadeDurationFinal, displayDurationFinal, ref _fadeFinalCoroutine);
        }

        if (_outline) _outline.enabled = false;
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

    // ─────────────────────────────────────────────────────────────────────────
    // UI — PANELES CON FADE
    // ─────────────────────────────────────────────────────────────────────────

    private void MostrarPanel(GameObject panel, CanvasGroup cg,
                               float fadeDur, float displayDur, ref Coroutine coroutine)
    {
        if (cg == null) return;
        if (coroutine != null) StopCoroutine(coroutine);
        coroutine = StartCoroutine(FadePanelCompleto(panel, cg, fadeDur, displayDur));
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