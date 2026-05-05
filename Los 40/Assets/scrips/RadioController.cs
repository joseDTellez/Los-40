using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class RadioController : MonoBehaviour
{
    [Header("Cámara e Inspección")]
    public Transform cameraTransform;
    public float distanceInFront = 0.7f;
    public float transitionSpeed = 5f;

    [Header("Perillas (Mesh)")]
    public Transform leftKnobMesh;  // On/Off
    public Transform rightKnobMesh; // Volumen
    public float knobSmoothSpeed = 10f;

    [Header("Audio")]
    public AudioSource commonAudioSource;
    public AudioSource[] stationSources;
    public AudioClip soundON, soundOFF, soundHover;

    [Header("UI — Overlay (Igual al Periódico)")]
    public GameObject hoverPanelRoot;
    public GameObject objectivePanelRoot;
    public float fadeDuration = 0.8f;
    public float displayDuration = 3f;

    // UI Interno
    private enum UIState { None, Hover, Objective }
    private UIState _currentUIState = UIState.None;
    private CanvasGroup _hoverCanvasGroup;
    private CanvasGroup _objectiveCanvasGroup;
    private Coroutine _uiTransitionCoroutine;

    // Estados
    private bool _isGazing = false;
    private bool _isNear = false;
    private bool _radioIsOn = false;
    private string _gazedPart = "";

    private Vector3 _origPos, _inspectPos;
    private Quaternion _origRot, _inspectRot;

    private int _currentStation = 0;
    private int _currentVolumeIndex = 0;
    private float[] _volumeLevels = { 1.0f, 0.6f, 0.4f };
    private float _leftTargetAngle = 0f;
    private float _rightTargetAngle = 0f;

    void Start()
    {
        _origPos = transform.position;
        _origRot = transform.rotation;
        if (cameraTransform == null) cameraTransform = Camera.main.transform;

        InicializarPanel(hoverPanelRoot, ref _hoverCanvasGroup);
        InicializarPanel(objectivePanelRoot, ref _objectiveCanvasGroup);

        _radioIsOn = false;
        ActualizarEmisoras();
    }

    void Update()
    {
        MoverObjeto();
        ActualizarRotacionFisicaPerillas();
        LeerInput();
    }
    private void LeerInput()
    {
        if (!_isGazing) return;

        bool interactPressed = false;

        // Utilizamos ÚNICAMENTE el Nuevo Input System para no hacer enojar a Unity
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame) interactPressed = true;
        if (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame) interactPressed = true;

        if (interactPressed) AlternarInspeccion();
    }

    private void AlternarInspeccion()
    {
        if (!_isNear)
        {
            // Acercar a la cámara
            _inspectPos = cameraTransform.position + (cameraTransform.forward * distanceInFront);
            _inspectRot = Quaternion.LookRotation(cameraTransform.position - _inspectPos);
            _isNear = true;

            CambiarUIState(UIState.Objective); // Mostrar panel de objetivo
            if (!_radioIsOn) AlternarOnOff();  // Encender automáticamente
        }
        else
        {
            // Si ya la tienes en la mano y oprimes K, revisa qué perilla miras
            if (_gazedPart == "Izquierda") AlternarOnOff();
            else if (_gazedPart == "Derecha") CambiarVolumen();
        }
    }

    // --- ENLACES PARA LAS PARTES HIJAS ---
    public void MirarCuerpo() { IniciarGaze("Cuerpo"); }
    public void MirarIzquierda() { IniciarGaze("Izquierda"); }
    public void MirarDerecha() { IniciarGaze("Derecha"); }

    private void IniciarGaze(string parte)
    {
        _isGazing = true;
        _gazedPart = parte;

        // Solo muestra el Hover si la radio está en la mesa
        if (!_isNear && _currentUIState == UIState.None)
        {
            CambiarUIState(UIState.Hover);
            if (commonAudioSource && soundHover) commonAudioSource.PlayOneShot(soundHover);
        }
    }

    public void OnPointerExit()
    {
        _isGazing = false;
        _gazedPart = "";

        if (!_isNear) CambiarUIState(UIState.None);
        StartCoroutine(EsperaRegreso());
    }

    private IEnumerator EsperaRegreso()
    {
        // Este retraso evita que se caiga si miras de la perilla al cuerpo rápidamente
        yield return new WaitForSeconds(0.1f);
        if (!_isGazing && _isNear)
        {
            // Soltar la radio si dejas de verla por completo
            _isNear = false;
            CambiarUIState(UIState.None);
            if (commonAudioSource && soundOFF) commonAudioSource.PlayOneShot(soundOFF);
        }
    }

    // --- LÓGICA DE RADIO ---
    private void AlternarOnOff()
    {
        _radioIsOn = !_radioIsOn;
        if (commonAudioSource) commonAudioSource.PlayOneShot(_radioIsOn ? soundON : soundOFF);
        _leftTargetAngle = _radioIsOn ? 60f : 0f;
        ActualizarEmisoras();
    }

    private void CambiarVolumen()
    {
        if (!_radioIsOn) return;
        _currentVolumeIndex = (_currentVolumeIndex + 1) % _volumeLevels.Length;
        _rightTargetAngle -= 45f;
        if (commonAudioSource && soundHover) commonAudioSource.PlayOneShot(soundHover);
        ActualizarEmisoras();
    }

    private void ActualizarEmisoras()
    {
        if (stationSources == null) return;
        for (int i = 0; i < stationSources.Length; i++)
        {
            if (stationSources[i] != null)
            {
                float targetVol = (_radioIsOn && i == _currentStation) ? _volumeLevels[_currentVolumeIndex] : 0f;
                stationSources[i].volume = targetVol;
                if (_radioIsOn && !stationSources[i].isPlaying) stationSources[i].Play();
            }
        }
    }

    private void MoverObjeto()
    {
        var targetPos = _isNear ? _inspectPos : _origPos;
        var targetRot = _isNear ? _inspectRot : _origRot;
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * transitionSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * transitionSpeed);
    }

    private void ActualizarRotacionFisicaPerillas()
    {
        if (leftKnobMesh) leftKnobMesh.localRotation = Quaternion.Slerp(leftKnobMesh.localRotation, Quaternion.Euler(0, 0, _leftTargetAngle), Time.deltaTime * knobSmoothSpeed);
        if (rightKnobMesh) rightKnobMesh.localRotation = Quaternion.Slerp(rightKnobMesh.localRotation, Quaternion.Euler(0, 0, _rightTargetAngle), Time.deltaTime * knobSmoothSpeed);
    }

    // --- LÓGICA DE UI (Importada de tu Periódico) ---
    private void CambiarUIState(UIState newState)
    {
        if (_currentUIState == newState) return;
        if (_uiTransitionCoroutine != null) StopCoroutine(_uiTransitionCoroutine);
        _uiTransitionCoroutine = StartCoroutine(TransicionUI(_currentUIState, newState));
    }

    private IEnumerator TransicionUI(UIState fromState, UIState toState)
    {
        yield return StartCoroutine(FadeOutSegunEstado(fromState));
        _currentUIState = toState;

        if (toState == UIState.Hover && _hoverCanvasGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(_hoverCanvasGroup, 0f, 1f, fadeDuration, hoverPanelRoot, false));
        else if (toState == UIState.Objective)
        {
            yield return StartCoroutine(FadeCanvasGroup(_objectiveCanvasGroup, 0f, 1f, fadeDuration, objectivePanelRoot, false));
            yield return new WaitForSeconds(displayDuration);
            CambiarUIState(UIState.None);
        }
    }

    private IEnumerator FadeOutSegunEstado(UIState state)
    {
        if (state == UIState.Hover && _hoverCanvasGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(_hoverCanvasGroup, _hoverCanvasGroup.alpha, 0f, fadeDuration, hoverPanelRoot, true));
        else if (state == UIState.Objective && _objectiveCanvasGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(_objectiveCanvasGroup, _objectiveCanvasGroup.alpha, 0f, fadeDuration, objectivePanelRoot, true));
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration, GameObject panel = null, bool desactivarAlFinal = false)
    {
        if (cg == null) yield break;
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
        if (desactivarAlFinal && to == 0f && panel != null) panel.SetActive(false);
    }

    private void InicializarPanel(GameObject panel, ref CanvasGroup canvasGroup)
    {
        if (panel == null) { canvasGroup = null; return; }
        canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = panel.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        panel.SetActive(false);
    }
}