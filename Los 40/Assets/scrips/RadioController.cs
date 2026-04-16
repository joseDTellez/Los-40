using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RadioController : MonoBehaviour
{
    [Header("Gaze Interaction")]
    //public float gazeTimeToInteract = 1.5f;
    //public Image loadingCircle;
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
        if (_outline != null) _outline.enabled = false;

        if (leftKnob) _leftOutline = leftKnob.GetComponent<Outline>();
        if (rightKnob) _rightOutline = rightKnob.GetComponent<Outline>();

        _origPos = transform.position;
        _origRot = transform.rotation;

        if (cameraTransform == null) cameraTransform = Camera.main.transform;

        //if (loadingCircle != null) loadingCircle.fillAmount = 0f;
    }

    void Update()
    {
        MoverRadio();

        //// Solo sumamos al timer si efectivamente estamos mirando el objeto
        //if (_isGazing)
        //{
        //    _gazeTimer += Time.deltaTime;
        //    if (loadingCircle != null)
        //        loadingCircle.fillAmount = Mathf.Clamp01(_gazeTimer / gazeTimeToInteract);

        //    if (_gazeTimer >= gazeTimeToInteract)
        //    {
        //        Interactuar();
        //        _gazeTimer = 0f;
        //        if (loadingCircle != null) loadingCircle.fillAmount = 0f;
        //    }
        //}
        if (_isGazing)
        {
            //Entrada por teclado para pruebas
            if (Keyboard.current.kKey.wasPressedThisFrame)
            {
                Debug.Log("Se presionó k");
                Interactuar();
            }

            // GAMEPAD (gatillo / botón)
            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
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
            // Opcional: Animar giro perilla derecha
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

        if (_isNear)
        {
            if (Vector3.Distance(transform.position, _inspectPos) < 0.1f)
            {
                if (_leftOutline) _leftOutline.enabled = true;
                if (_rightOutline) _rightOutline.enabled = true;
            }
        }
        else
        {
            if (_leftOutline) _leftOutline.enabled = false;
            if (_rightOutline) _rightOutline.enabled = false;
        }
    }

    private void Play(AudioClip c) { if (audioSource && c) { audioSource.clip = c; audioSource.Play(); } }

    // --- MÉTODOS DE ENTRADA ---
    public void OnPointerEnter() { StartGazing("Radio"); if (_outline) _outline.enabled = true; }
    public void OnPointerEnterLeft() { StartGazing("Left"); }
    public void OnPointerEnterRight() { StartGazing("Right"); }

    private void StartGazing(string part)
    {
        _isGazing = true;
        _partName = part;
        // Si el usuario vuelve a mirar antes de que expire el tiempo de gracia, cancelamos el reset
        if (_exitRoutine != null) StopCoroutine(_exitRoutine);
    }

    // --- MÉTODO DE SALIDA CON BUFFER ---
    public void OnPointerExit()
    {
        _isGazing = false;
        if (_exitRoutine != null) StopCoroutine(_exitRoutine);
        _exitRoutine = StartCoroutine(GracePeriodExitRoutine());
    }

    private IEnumerator GracePeriodExitRoutine()
    {
        // 1. Esperamos el tiempo de gracia para ver si el usuario vuelve a mirar (evita el parpadeo)
        yield return new WaitForSeconds(graceTime);

        if (!_isGazing)
        {
            // Si después del tiempo de gracia sigue sin mirar, reseteamos el círculo
            _gazeTimer = 0f;
            //if (loadingCircle != null) loadingCircle.fillAmount = 0f;

            // 2. Esperamos un poco más para decidir si alejamos la radio del usuario
            yield return new WaitForSeconds(0.3f);

            if (!_isGazing)
            {
                if (_isNear) { _isNear = false; Play(soundOFF); }
                _partName = "";
                if (_outline) _outline.enabled = false;
            }
        }
    }
}