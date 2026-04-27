using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RadioController : MonoBehaviour
{
    [Header("Gaze Interaction")]
    public Transform cameraTransform;
    public float graceTime = 0.25f;

    [Header("Ajustes de Inspección")]
    public float transitionSpeed = 5f;
    public float distanceInFront = 0.7f;

    [Header("Perillas (Solo el Mesh)")]
    public Transform leftKnobMesh;  // Perilla On/Off
    public Transform rightKnobMesh; // Perilla Volumen
    public float knobSmoothSpeed = 10f;

    [Header("Audio")]
    public AudioSource[] stationSources;
    public AudioSource commonAudioSource;
    public AudioClip soundON, soundOFF, soundHover;

    // Estados internos
    private bool _isGazing = false;
    private bool _isExiting = false;
    private bool _isNear = false;
    private bool _radioIsOn = false;
    private string _gazedPart = "Radio";

    private Vector3 _origPos, _inspectPos;
    private Quaternion _origRot, _inspectRot;

    private int _currentStation = 0;
    private int _currentVolumeIndex = 0;
    private float[] _volumeLevels = { 1.0f, 0.6f, 0.4f };

    // Variables de rotación para perillas
    private float _leftTargetAngle = 0f;
    private float _rightTargetAngle = 0f;
    private Coroutine _exitRoutine;

    void Start()
    {
        _origPos = transform.position;
        _origRot = transform.rotation;
        if (cameraTransform == null) cameraTransform = Camera.main.transform;

        _radioIsOn = false;
        ActualizarEmisoras();
    }

    void Update()
    {
        MoverRadioHaciaCamara();
        ActualizarRotacionFisicaPerillas();

        // Interacción por Teclado o Gamepad
        if (_isGazing)
        {
            bool interactPressed = (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame) ||
                                   (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

            if (interactPressed)
            {
                Interactuar();
            }
        }
    }

    private void Interactuar()
    {
        if (!_isNear)
        {
            // Acercar la radio para inspección
            _inspectPos = cameraTransform.position + (cameraTransform.forward * distanceInFront);
            _inspectRot = Quaternion.LookRotation(cameraTransform.position - _inspectPos);
            _isNear = true;

            // Opcional: Encender al agarrar
            if (!_radioIsOn) AlternarOnOff();
        }
        else
        {
            // Si ya está cerca, interactuamos con las partes específicas
            if (_gazedPart == "Left") AlternarOnOff();
            else if (_gazedPart == "Right") CambiarVolumen();
        }
    }

    private void AlternarOnOff()
    {
        _radioIsOn = !_radioIsOn;
        if (commonAudioSource) commonAudioSource.PlayOneShot(_radioIsOn ? soundON : soundOFF);

        // Rotación: 0 grados si OFF, 60 grados si ON
        _leftTargetAngle = _radioIsOn ? 60f : 0f;

        ActualizarEmisoras();
    }

    private void CambiarVolumen()
    {
        if (!_radioIsOn) return;

        _currentVolumeIndex = (_currentVolumeIndex + 1) % _volumeLevels.Length;

        // Gira -45 grados por cada nivel de volumen
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

    private void MoverRadioHaciaCamara()
    {
        transform.position = Vector3.Lerp(transform.position, _isNear ? _inspectPos : _origPos, Time.deltaTime * transitionSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, _isNear ? _inspectRot : _origRot, Time.deltaTime * transitionSpeed);
    }

    private void ActualizarRotacionFisicaPerillas()
    {
        // IMPORTANTE: Si la perilla gira en el eje equivocado, cambia el eje en Euler(0, 0, ángulo)

        if (leftKnobMesh)
        {
            Quaternion targetRot = Quaternion.Euler(0, 0, _leftTargetAngle);
            leftKnobMesh.localRotation = Quaternion.Slerp(leftKnobMesh.localRotation, targetRot, Time.deltaTime * knobSmoothSpeed);
        }

        if (rightKnobMesh)
        {
            Quaternion targetRot = Quaternion.Euler(0, 0, _rightTargetAngle);
            rightKnobMesh.localRotation = Quaternion.Slerp(rightKnobMesh.localRotation, targetRot, Time.deltaTime * knobSmoothSpeed);
        }
    }

    // --- Métodos de Gaze ---
    public void OnPointerEnter() => IniciarGaze("Radio");
    public void OnPointerEnterLeft() => IniciarGaze("Left");
    public void OnPointerEnterRight() => IniciarGaze("Right");

    private void IniciarGaze(string part)
    {
        _gazedPart = part;
        if (_isExiting)
        {
            if (_exitRoutine != null) StopCoroutine(_exitRoutine);
            _isExiting = false;
        }
        if (!_isGazing)
        {
            _isGazing = true;
            if (!_isNear && commonAudioSource) commonAudioSource.PlayOneShot(soundHover);
        }
    }

    public void OnPointerExit()
    {
        if (!gameObject.activeInHierarchy || !_isGazing) return;
        if (_exitRoutine != null) StopCoroutine(_exitRoutine);
        _exitRoutine = StartCoroutine(RutinaSalidaGracia());
    }

    private IEnumerator RutinaSalidaGracia()
    {
        _isExiting = true;
        yield return new WaitForSeconds(graceTime);
        _isExiting = false;
        _isGazing = false;
        if (_isNear)
        {
            yield return new WaitForSeconds(0.5f);
            if (!_isGazing)
            {
                _isNear = false;
                if (commonAudioSource) commonAudioSource.PlayOneShot(soundOFF);
            }
        }
    }
}