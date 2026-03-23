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

    void Start()
    {
        _outline = GetComponent<Outline>();
        if (_outline != null) _outline.enabled = false;
        _origPos = transform.position;
        _origRot = transform.rotation;
        if (cameraTransform == null) cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        MoverRadio();

        if (_isGazing)
        {
            _gazeTimer += Time.deltaTime;
            if (loadingCircle != null)
                loadingCircle.fillAmount = _gazeTimer / gazeTimeToInteract;

            if (_gazeTimer >= gazeTimeToInteract)
            {
                Interactuar();
                _gazeTimer = 0f;
            }
        }
        else
        {
            _gazeTimer = 0f;
            if (loadingCircle != null) loadingCircle.fillAmount = 0f;
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
            // AHORA USA LA NUEVA CORRUTINA CON OFFSET
            if (rightKnob) StartCoroutine(AnimarGiroPerillaOffset(rightKnob, 60f));

            if (audioSource != null)
            {
                audioSource.volume = (_volIdx == 0) ? 0.2f : (_volIdx == 1) ? 0.6f : 1.0f;
            }
        }
    }

    // --- NUEVA CORRUTINA: FIJA LA PERILLA SOBRE SU CENTRO VISUAL (CON OFFSET) ---
    private IEnumerator AnimarGiroPerillaOffset(Transform perilla, float angulo)
    {
        // 1. Guardamos la rotación local inicial exacta
        Quaternion rotInicialLocal = perilla.localRotation;

        // 2. Calculamos la posición del centro visual de la perilla en coordenadas locales de su PADRE
        //    (esto compensa cualquier desfasaje del pivot point)
        Vector3 posCentroVisual = perilla.parent.InverseTransformPoint(perilla.position);

        // 3. Calculamos la rotación objetivo final (sumando el ángulo al eje Z local)
        Quaternion rotObjetivoLocal = rotInicialLocal * Quaternion.Euler(0, 0, angulo);

        float tiempo = 0f;
        float duracion = angulo / knobRotationSpeed;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float t = tiempo / duracion; // Factor de interpolación (0 a 1)

            // 4. FORZAMOS LA POSICIÓN FIJA (evita que la imagen vuele)
            //    Usamos InverseTransformPoint para mantener el centro visual quieto
            //    incluso si el pivot está en la "antártida".
            perilla.localPosition = perilla.parent.InverseTransformPoint(perilla.parent.TransformPoint(posCentroVisual));

            // 5. ROTAMOS SUAVEMENTE SOBRE EL PUNTO FIJO
            perilla.localRotation = Quaternion.Slerp(rotInicialLocal, rotObjetivoLocal, t);

            yield return null;
        }

        // 6. Aseguramos que termine en la posición y rotación exactas
        perilla.localPosition = perilla.parent.InverseTransformPoint(perilla.parent.TransformPoint(posCentroVisual));
        perilla.localRotation = rotObjetivoLocal;
    }

    private void MoverRadio()
    {
        transform.position = Vector3.Lerp(transform.position, _isNear ? _inspectPos : _origPos, Time.deltaTime * transitionSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, _isNear ? _inspectRot : _origRot, Time.deltaTime * transitionSpeed);
    }

    private void Play(AudioClip c) { if (audioSource && c) { audioSource.clip = c; audioSource.Play(); } }

    public void OnPointerEnter() { _isGazing = true; _partName = "Radio"; if (_outline) _outline.enabled = true; StopAllCoroutines(); }
    public void OnPointerEnterLeft() { _isGazing = true; _partName = "Left"; if (_outline) _outline.enabled = false; StopAllCoroutines(); }
    public void OnPointerEnterRight() { _isGazing = true; _partName = "Right"; if (_outline) _outline.enabled = false; StopAllCoroutines(); }
    public void OnPointerExit() { _isGazing = false; StartCoroutine(EsperaRegreso()); }

    private IEnumerator EsperaRegreso()
    {
        yield return new WaitForSeconds(0.1f);
        if (!_isGazing)
        {
            if (_isNear) { _isNear = false; Play(soundOFF); }
            _partName = ""; if (_outline) _outline.enabled = false;
        }
    }
}