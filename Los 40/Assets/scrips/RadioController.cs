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
    public Transform leftKnobMesh;  // Perilla Emisora
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
    private float[] _volumeLevels = { 0.3f, 0.2f, 0.1f };

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

        // Interacción por Teclado, Gamepad o Mouse
        if (_isGazing)
        {
            bool interactPressed = (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame) ||
                                   (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame) ||
                                   (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

            if (interactPressed)
            {
                Interactuar();
            }
        }
    }

    private void Interactuar()
    {
        // Si no está cerca, calculamos la posición para inspeccionarla y la marcamos como cercana
        if (!_isNear)
        {
            _inspectPos = cameraTransform.position + (cameraTransform.forward * distanceInFront);
            _inspectRot = Quaternion.LookRotation(cameraTransform.position - _inspectPos);
            _isNear = true;
        }

        // Ejecutamos la acción dependiendo de qué parte de la radio se está seleccionando
        if (_gazedPart == "Radio")
        {
            AlternarOnOff();
        }
        else if (_gazedPart == "Left")
        {
            CambiarEmisora();
        }
        else if (_gazedPart == "Right")
        {
            CambiarVolumen();
        }
    }

    private void AlternarOnOff()
    {
        _radioIsOn = !_radioIsOn;
        if (commonAudioSource) commonAudioSource.PlayOneShot(_radioIsOn ? soundON : soundOFF);

        ActualizarEmisoras();
    }

    private void CambiarEmisora()
    {
        // Solo cambia emisora si está encendido y hay emisoras configuradas
        if (!_radioIsOn || stationSources == null || stationSources.Length == 0) return;

        _currentStation = (_currentStation + 1) % stationSources.Length;

        // Gira -30 grados por cada cambio de emisora (puedes ajustar el valor)
        _leftTargetAngle -= 30f;

        if (commonAudioSource && soundHover) commonAudioSource.PlayOneShot(soundHover);
        ActualizarEmisoras();
    }

    private void CambiarVolumen()
    {
        if (!_radioIsOn) return;

        _currentVolumeIndex = (_currentVolumeIndex + 1) % _volumeLevels.Length;

        // Gira -50 grados por cada nivel de volumen
        _rightTargetAngle -= 50f;

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
                // Si la radio está prendida y es la estación actual, aplica el volumen. De lo contrario, volumen 0.
                float targetVol = (_radioIsOn && i == _currentStation) ? _volumeLevels[_currentVolumeIndex] : 0f;
                stationSources[i].volume = targetVol;

                // Nos aseguramos de que estén reproduciéndose
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
            // Si el jugador realmente dejó de mirar la radio, la devolvemos a su posición original
            if (!_isGazing)
            {
                _isNear = false;
                if (commonAudioSource) commonAudioSource.PlayOneShot(soundOFF);
            }
        }
    }
}