using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RadioController : MonoBehaviour
{
    [Header("Gaze Interaction")]
    public Transform cameraTransform;
    [Tooltip("Tiempo que perdonamos si el sensor parpadea (segundos)")]
    public float graceTime = 0.2f;

    [Header("Perillas (Hijos)")]
    public Transform leftKnob;
    public Transform rightKnob;

    [Header("Ajustes de Inspección")]
    public float distanceInFront = 0.7f;
    public float transitionSpeed = 5f;
    public float knobRotationSpeed = 200f;

    [Header("Sonidos")]
    public AudioSource audioSource;
    public AudioClip soundON, soundOFF, station1, station2, station3;

    // --- REEMPLAZO REALIZADO: Ahora usa OutlineVR ---
    private OutlineVR _outline;
    private OutlineVR _leftOutline, _rightOutline;
    // ------------------------------------------------

    private bool _isGazing = false;
    private string _partName = "";

    private Vector3 _origPos;
    private Quaternion _origRot;
    private bool _isNear = false;
    private Vector3 _inspectPos;
    private Quaternion _inspectRot;

    private int _stationIdx = 0;
    private int _volIdx = 1;

    private Coroutine _exitRoutine;

    void Start()
    {
        // Buscamos el componente OutlineVR
        _outline = GetComponent<OutlineVR>();

        if (leftKnob) _leftOutline = leftKnob.GetComponent<OutlineVR>();
        if (rightKnob) _rightOutline = rightKnob.GetComponent<OutlineVR>();

        // Al inicio desactivamos los brillos por rendimiento en móvil
        SetOutlinesEnabled(false);

        _origPos = transform.position;
        _origRot = transform.rotation;

        if (cameraTransform == null) cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        MoverRadio();

        if (_isGazing)
        {
            // Entrada por teclado (K) o Botón Sur del Gamepad (A en Xbox / X en PS)
            if (Keyboard.current.kKey.wasPressedThisFrame || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame))
            {
                Interactuar();
            }
        }
    }

    private void Interactuar()
    {
        if (!_isNear)
        {
            _inspectPos = cameraTransform.position + (cameraTransform.forward * distanceInFront);
            _inspectRot = Quaternion.LookRotation(cameraTransform.position - _inspectPos);
            _isNear = true;
            Play(soundON);
        }
        else if (_partName == "Left")
        {
            _stationIdx = (_stationIdx % 3) + 1;
            if (leftKnob) StartCoroutine(AnimarGiroPerillaOffset(leftKnob, 60f));

            if (_stationIdx == 1) Play(station1);
            else if (_stationIdx == 2) Play(station2);
            else if (_stationIdx == 3) Play(station3);
        }
        else if (_partName == "Right")
        {
            _volIdx = (_volIdx + 1) % 3;
            if (audioSource != null)
            {
                audioSource.volume = (_volIdx == 0) ? 0.2f : (_volIdx == 1) ? 0.6f : 1.0f;
            }
            if (rightKnob) StartCoroutine(AnimarGiroPerillaOffset(rightKnob, 30f));
        }
    }

    private void MoverRadio()
    {
        transform.position = Vector3.Lerp(transform.position, _isNear ? _inspectPos : _origPos, Time.deltaTime * transitionSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, _isNear ? _inspectRot : _origRot, Time.deltaTime * transitionSpeed);

        // Activamos los brillos solo si la radio está cerca para no gastar batería
        if (_isNear)
        {
            SetOutlinesEnabled(true);
        }
        else
        {
            SetOutlinesEnabled(false);
        }
    }

    private void SetOutlinesEnabled(bool state)
    {
        // Solo habilitamos el componente, el modo (Idle/Hover) lo maneja el Gaze
        if (_leftOutline) _leftOutline.enabled = state;
        if (_rightOutline) _rightOutline.enabled = state;
    }

    private void Play(AudioClip c) { if (audioSource && c) { audioSource.clip = c; audioSource.Play(); } }

    // --- MÉTODOS PARA CARDBOARD ---
    public void OnPointerEnter()
    {
        StartGazing("Radio");
        if (_outline) { _outline.enabled = true; _outline.SetState(OutlineVR.InteractionState.Hover); }
    }

    public void OnPointerEnterLeft()
    {
        StartGazing("Left");
        if (_leftOutline) _leftOutline.SetState(OutlineVR.InteractionState.Hover);
    }

    public void OnPointerEnterRight()
    {
        StartGazing("Right");
        if (_rightOutline) _rightOutline.SetState(OutlineVR.InteractionState.Hover);
    }

    public void OnPointerExit()
    {
        _isGazing = false;

        // Al quitar la mirada, regresamos al modo "Respirar" (Idle)
        if (_leftOutline) _leftOutline.SetState(OutlineVR.InteractionState.Idle);
        if (_rightOutline) _rightOutline.SetState(OutlineVR.InteractionState.Idle);
        if (_outline) _outline.SetState(OutlineVR.InteractionState.Idle);

        if (_exitRoutine != null) StopCoroutine(_exitRoutine);
        _exitRoutine = StartCoroutine(GracePeriodExitRoutine());
    }

    private void StartGazing(string part)
    {
        _isGazing = true;
        _partName = part;
        if (_exitRoutine != null) StopCoroutine(_exitRoutine);
    }

    private IEnumerator GracePeriodExitRoutine()
    {
        yield return new WaitForSeconds(graceTime);
        if (!_isGazing)
        {
            yield return new WaitForSeconds(0.3f);
            if (!_isGazing)
            {
                if (_isNear) { _isNear = false; Play(soundOFF); }
                _partName = "";
                if (_outline) _outline.enabled = false;
            }
        }
    }

    private IEnumerator AnimarGiroPerillaOffset(Transform perilla, float angulo)
    {
        Quaternion rotInicialLocal = perilla.localRotation;
        Quaternion rotObjetivoLocal = rotInicialLocal * Quaternion.Euler(0, 0, angulo);
        float tiempo = 0f;
        float duracion = angulo / knobRotationSpeed;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            perilla.localRotation = Quaternion.Slerp(rotInicialLocal, rotObjetivoLocal, tiempo / duracion);
            yield return null;
        }
        perilla.localRotation = rotObjetivoLocal;
    }
}