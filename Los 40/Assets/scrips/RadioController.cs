using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RadioController : MonoBehaviour
{
    [Header("Gaze Interaction")]
    public float gazeTimeToInteract = 1.5f;
    public Image loadingCircle;
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

    private Outline _outline;
    private Outline _leftOutline, _rightOutline;
    private float _gazeTimer = 0f;
    private bool _isGazing = false;
    private string _partName = "";

    private Vector3 _origPos;
    private Quaternion _origRot;
    private bool _isNear = false;
    private Vector3 _inspectPos;
    private Quaternion _inspectRot;

    private int _stationIdx = 0;
    private int _volIdx = 1;

    // Corrutina para manejar la salida y el reset del timer
    private Coroutine _exitRoutine;

    void Start()
    {
        _outline = GetComponent<Outline>();
        // IMPORTANTE: El Outline ahora se maneja por estados, no por .enabled
        if (_outline != null) _outline.SetState(Outline.InteractionState.Idle);

        if (leftKnob) _leftOutline = leftKnob.GetComponent<Outline>();
        if (rightKnob) _rightOutline = rightKnob.GetComponent<Outline>();

        _origPos = transform.position;
        _origRot = transform.rotation;

        if (cameraTransform == null) cameraTransform = Camera.main.transform;

        if (loadingCircle != null) loadingCircle.fillAmount = 0f;
    }

    void Update()
    {
        MoverRadio();

        if (_isGazing)
        {
            _gazeTimer += Time.deltaTime;
            if (loadingCircle != null)
                loadingCircle.fillAmount = Mathf.Clamp01(_gazeTimer / gazeTimeToInteract);

            if (_gazeTimer >= gazeTimeToInteract)
            {
                Interactuar();
                _gazeTimer = 0f;
                if (loadingCircle != null) loadingCircle.fillAmount = 0f;
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

            // Cambiamos a estado Interacting para que el outline no estorbe la vista de cerca
            if (_outline) _outline.SetState(Outline.InteractionState.Interacting);
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

    private IEnumerator AnimarGiroPerillaOffset(Transform perilla, float angulo)
    {
        Quaternion rotInicialLocal = perilla.localRotation;
        Vector3 posCentroVisual = perilla.parent.InverseTransformPoint(perilla.position);
        Quaternion rotObjetivoLocal = rotInicialLocal * Quaternion.Euler(0, 0, angulo);

        float tiempo = 0f;
        float duracion = angulo / knobRotationSpeed;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float t = tiempo / duracion;
            perilla.localPosition = perilla.parent.InverseTransformPoint(perilla.parent.TransformPoint(posCentroVisual));
            perilla.localRotation = Quaternion.Slerp(rotInicialLocal, rotObjetivoLocal, t);
            yield return null;
        }
        perilla.localRotation = rotObjetivoLocal;
    }

    private void MoverRadio()
    {
        transform.position = Vector3.Lerp(transform.position, _isNear ? _inspectPos : _origPos, Time.deltaTime * transitionSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, _isNear ? _inspectRot : _origRot, Time.deltaTime * transitionSpeed);

        // Controlamos los outlines de las perillas solo cuando la radio está cerca
        if (_isNear)
        {
            if (Vector3.Distance(transform.position, _inspectPos) < 0.1f)
            {
                // En modo inspección, las perillas "respiran" para indicar que puedes tocarlas
                if (_leftOutline && _leftOutline.currentState == Outline.InteractionState.Interacting)
                    _leftOutline.SetState(Outline.InteractionState.Idle);
                if (_rightOutline && _rightOutline.currentState == Outline.InteractionState.Interacting)
                    _rightOutline.SetState(Outline.InteractionState.Idle);
            }
        }
        else
        {
            if (_leftOutline) _leftOutline.SetState(Outline.InteractionState.Interacting);
            if (_rightOutline) _rightOutline.SetState(Outline.InteractionState.Interacting);
        }
    }

    private void Play(AudioClip c) { if (audioSource && c) { audioSource.clip = c; audioSource.Play(); } }

    // --- MÉTODOS DE ENTRADA CORREGIDOS ---
    public void OnPointerEnter()
    {
        StartGazing("Radio");
        // Si no estamos ya en modo inspección, ponemos el outline fijo
        if (_outline && !_isNear) _outline.SetState(Outline.InteractionState.Hover);
    }

    public void OnPointerEnterLeft()
    {
        StartGazing("Left");
        if (_leftOutline) _leftOutline.SetState(Outline.InteractionState.Hover);
    }

    public void OnPointerEnterRight()
    {
        StartGazing("Right");
        if (_rightOutline) _rightOutline.SetState(Outline.InteractionState.Hover);
    }

    private void StartGazing(string part)
    {
        _isGazing = true;
        _partName = part;
        if (_exitRoutine != null) StopCoroutine(_exitRoutine);
    }

    public void OnPointerExit()
    {
        _isGazing = false;

        // Al salir del foco de una parte específica, regresamos su outline a Idle
        if (_partName == "Left" && _leftOutline) _leftOutline.SetState(Outline.InteractionState.Idle);
        if (_partName == "Right" && _rightOutline) _rightOutline.SetState(Outline.InteractionState.Idle);

        if (_exitRoutine != null) StopCoroutine(_exitRoutine);
        _exitRoutine = StartCoroutine(GracePeriodExitRoutine());
    }

    private IEnumerator GracePeriodExitRoutine()
    {
        yield return new WaitForSeconds(graceTime);

        if (!_isGazing)
        {
            _gazeTimer = 0f;
            if (loadingCircle != null) loadingCircle.fillAmount = 0f;

            yield return new WaitForSeconds(0.3f);

            if (!_isGazing)
            {
                if (_isNear)
                {
                    _isNear = false;
                    Play(soundOFF);
                }
                _partName = "";
                // Al perder el foco total, la radio vuelve a "Respirar" (Idle)
                if (_outline) _outline.SetState(Outline.InteractionState.Idle);
            }
        }
    }
}