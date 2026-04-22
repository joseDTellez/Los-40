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
    public float distanceInFront = 0.7f;   // Distancia frente a la cámara al inspeccionar
    public float transitionSpeed = 5f;    // Velocidad de lerp al mover el objeto

    [Header("Sonidos")]
    public AudioSource audioSource;
    public AudioClip soundEntry;         // Sonido al acercar el objeto
    public AudioClip soundExit;          // Sonido al alejar el objeto
    public AudioClip soundHover;         // NUEVO: Sonido al mirar el objeto (hover)

    [Header("Outline (hover visual)")]
    public Image loadingCircle;            // Reservado para uso futuro (gaze timer)

    [Header("UI — Hover")]
    public GameObject hoverPanelRoot;      // Panel que aparece al mirar el objeto

    [Header("UI — Objetivo inicial")]
    public GameObject objectivePanelRoot;  // Panel que aparece al interactuar por primera vez
    public float fadeDuration = 0.8f;   // Duración del fade in/out
    public float displayDuration = 3f;     // Tiempo visible antes del fade out

    [Header("UI — Objetivo final")]
    public GameObject objectiveFinalPanelRoot; // Panel que aparece al soltar el objeto
    public float fadeDurationFinal = 0.8f;
    public float displayDurationFinal = 3f;

    // ─────────────────────────────────────────────────────────────────────────
    // PRIVADOS
    // ─────────────────────────────────────────────────────────────────────────

    // Outline
    private Outline _outline;

    // Estado del gaze
    private bool _isGazing = false;

    // Estado de inspección
    private bool _isNear = false;
    private bool _hasInteracted = false;   // Evita mostrar el objetivo inicial más de una vez

    // Posición y rotación originales del objeto
    private Vector3 _origPos;
    private Quaternion _origRot;

    // Posición y rotación de inspección (frente a la cámara)
    private Vector3 _inspectPos;
    private Quaternion _inspectRot;

    // Canvas Groups para controlar alpha de cada panel
    private CanvasGroup _hoverCanvasGroup;
    private CanvasGroup _objectiveCanvasGroup;
    private CanvasGroup _objectiveFinalCanvasGroup;

    // Referencias a coroutines activas para poder cancelarlas
    private Coroutine _hoverCoroutine;
    private Coroutine _fadeCoroutine;
    private Coroutine _fadeFinalCoroutine;

    // ─────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        // Outline: desactivar al inicio
        _outline = GetComponent<Outline>();
        if (_outline != null) _outline.enabled = false;

        // Guardar transform original del objeto
        _origPos = transform.position;
        _origRot = transform.rotation;

        // Cámara principal como fallback
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        // Inicializar los tres paneles UI
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

        // Teclado (pruebas en editor)
        if (Keyboard.current.kKey.wasPressedThisFrame)
            AlternarInspeccion();

        // Gamepad
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
            AlternarInspeccion();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // INSPECCIÓN DEL OBJETO
    // ─────────────────────────────────────────────────────────────────────────

    private void AlternarInspeccion()
    {
        if (_isNear) return;

        // Calcular posición y rotación frente a la cámara
        _inspectPos = cameraTransform.position + (cameraTransform.forward * distanceInFront);
        _inspectRot = Quaternion.LookRotation(cameraTransform.position - _inspectPos);

        _isNear = true;
        PlaySound(soundEntry);
        OcultarHover();

        // Mostrar objetivo inicial solo la primera vez
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
    // EVENTOS DE GAZE (llamados desde el sistema de detección externa)
    // ─────────────────────────────────────────────────────────────────────────

    public void OnPointerEnter()
    {
        _isGazing = true;
        if (_outline) _outline.enabled = true;

        // Solo mostrar hover si el objeto no está siendo inspeccionado
        if (!_isNear)
        {
            MostrarHover();
            PlayHoverSound(); // NUEVO: Dispara el sonido al mirar el objeto
        }
    }

    public void OnPointerExit()
    {
        _isGazing = false;
        if (!_isNear) OcultarHover();
        StartCoroutine(EsperaRegreso());
    }

    // Pequeña espera para evitar flickers al salir del gaze
    private IEnumerator EsperaRegreso()
    {
        yield return new WaitForSeconds(0.1f);

        if (!_isGazing)
        {
            if (_isNear)
            {
                _isNear = false;
                PlaySound(soundExit);

                // Mostrar objetivo final al soltar el objeto
                MostrarPanel(objectiveFinalPanelRoot, _objectiveFinalCanvasGroup,
                             fadeDurationFinal, displayDurationFinal, ref _fadeFinalCoroutine);
            }

            if (_outline) _outline.enabled = false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UI — HOVER
    // ─────────────────────────────────────────────────────────────────────────

    private void MostrarHover()
    {
        if (_hoverCanvasGroup == null) return;
        if (_hoverCoroutine != null) StopCoroutine(_hoverCoroutine);
        _hoverCoroutine = StartCoroutine(FadeCanvasGroup(_hoverCanvasGroup, 0f, 1f, fadeDuration,
                                                          hoverPanelRoot, desactivarAlFinal: false));
    }

    private void OcultarHover()
    {
        if (_hoverCanvasGroup == null) return;
        if (_hoverCoroutine != null) StopCoroutine(_hoverCoroutine);
        _hoverCoroutine = StartCoroutine(FadeCanvasGroup(_hoverCanvasGroup, 1f, 0f, fadeDuration,
                                                          hoverPanelRoot, desactivarAlFinal: true));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UI — PANELES CON FADE (objetivo inicial y final)
    // ─────────────────────────────────────────────────────────────────────────

    // Método genérico para mostrar cualquier panel con fade in → espera → fade out
    private void MostrarPanel(GameObject panel, CanvasGroup cg,
                               float fadeDur, float displayDur, ref Coroutine coroutine)
    {
        if (cg == null) return;
        if (coroutine != null) StopCoroutine(coroutine);
        coroutine = StartCoroutine(FadePanelCompleto(panel, cg, fadeDur, displayDur));
    }

    private IEnumerator FadePanelCompleto(GameObject panel, CanvasGroup cg,
                                           float fadeDur, float displayDur)
    {
        panel.SetActive(true);
        yield return StartCoroutine(FadeCanvasGroup(cg, 0f, 1f, fadeDur));
        yield return new WaitForSeconds(displayDur);
        yield return StartCoroutine(FadeCanvasGroup(cg, 1f, 0f, fadeDur));
        panel.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UTILIDADES
    // ─────────────────────────────────────────────────────────────────────────

    // Fade genérico de un CanvasGroup. El panel y desactivarAlFinal son opcionales.
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

    // Inicializa un panel: agrega CanvasGroup si no existe, alpha 0 y desactiva
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

    // NUEVO: Método para reproducir el sonido de hover usando PlayOneShot
    private void PlayHoverSound()
    {
        if (audioSource != null && soundHover != null)
        {
            audioSource.PlayOneShot(soundHover);
        }
    }
}