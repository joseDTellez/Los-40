using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RadioController : MonoBehaviour
{
    [Header("Gaze Interaction")]
    public Transform cameraTransform;
    public float graceTime = 0.2f;

    [Header("Perillas (Arrastra los Pivotes aquí)")]
    public Transform leftKnob;
    public Transform rightKnob;

    [Header("Ajustes de Inspección")]
    public float distanceInFront = 0.7f;
    public float transitionSpeed = 5f;
    public float knobRotationSpeed = 200f;

    [Header("Sonidos")]
    public AudioSource audioSource;
    public AudioClip soundON, soundOFF, station1, station2, station3;

    private OutlineVR _outline;
    private OutlineVR _leftOutline, _rightOutline;
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
        _outline = GetComponent<OutlineVR>();
        if (leftKnob) _leftOutline = leftKnob.GetComponent<OutlineVR>();
        if (rightKnob) _rightOutline = rightKnob.GetComponent<OutlineVR>();

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
            if (leftKnob) StartCoroutine(AnimarGiroPerilla(leftKnob, 60f));
            PlayStationSound();
        }
        else if (_partName == "Right")
        {
            _volIdx = (_volIdx + 1) % 3;
            if (audioSource) audioSource.volume = (_volIdx == 0) ? 0.2f : (_volIdx == 1) ? 0.6f : 1.0f;
            // Aquí giramos la perilla derecha sobre su propio eje
            if (rightKnob) StartCoroutine(AnimarGiroPerilla(rightKnob, 30f));
        }
    }

    private IEnumerator AnimarGiroPerilla(Transform perilla, float angulo)
    {
        Quaternion startRot = perilla.localRotation;

        // NOTA: Si gira hacia donde no es, cambia (0, 0, angulo) por (0, angulo, 0)
        Quaternion endRot = startRot * Quaternion.Euler(0, 0, angulo);

        float duration = Mathf.Abs(angulo) / knobRotationSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // Solo rotamos, NO tocamos la posición
            perilla.localRotation = Quaternion.Slerp(startRot, endRot, elapsed / duration);
            yield return null;
        }
        perilla.localRotation = endRot;
    }

    private void PlayStationSound()
    {
        if (_stationIdx == 1) Play(station1);
        else if (_stationIdx == 2) Play(station2);
        else if (_stationIdx == 3) Play(station3);
    }

    private void MoverRadio()
    {
        transform.position = Vector3.Lerp(transform.position, _isNear ? _inspectPos : _origPos, Time.deltaTime * transitionSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, _isNear ? _inspectRot : _origRot, Time.deltaTime * transitionSpeed);
        SetOutlinesEnabled(_isNear);
    }

    private void SetOutlinesEnabled(bool state)
    {
        if (_leftOutline) _leftOutline.enabled = state;
        if (_rightOutline) _rightOutline.enabled = state;
    }

    private void Play(AudioClip c) { if (audioSource && c) { audioSource.clip = c; audioSource.Play(); } }

    public void OnPointerEnter() { StartGazing("Radio"); if (_outline) { _outline.enabled = true; _outline.SetState(OutlineVR.InteractionState.Hover); } }
    public void OnPointerEnterLeft() { StartGazing("Left"); if (_leftOutline) _leftOutline.SetState(OutlineVR.InteractionState.Hover); }
    public void OnPointerEnterRight() { StartGazing("Right"); if (_rightOutline) _rightOutline.SetState(OutlineVR.InteractionState.Hover); }

    public void OnPointerExit()
    {
        _isGazing = false;
        if (_leftOutline) _leftOutline.SetState(OutlineVR.InteractionState.Idle);
        if (_rightOutline) _rightOutline.SetState(OutlineVR.InteractionState.Idle);
        if (_outline) _outline.SetState(OutlineVR.InteractionState.Idle);
        if (_exitRoutine != null) StopCoroutine(_exitRoutine);
        _exitRoutine = StartCoroutine(GracePeriodExitRoutine());
    }

    private void StartGazing(string part) { _isGazing = true; _partName = part; if (_exitRoutine != null) StopCoroutine(_exitRoutine); }

    private IEnumerator GracePeriodExitRoutine()
    {
        yield return new WaitForSeconds(graceTime);
        if (!_isGazing)
        {
            yield return new WaitForSeconds(0.3f);
            if (!_isGazing && _isNear) { _isNear = false; Play(soundOFF); if (_outline) _outline.enabled = false; }
        }
    }
}