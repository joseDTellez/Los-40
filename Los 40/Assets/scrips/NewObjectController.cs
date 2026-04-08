using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider))]
public class NewObjectController : MonoBehaviour
{
    [Header("Gaze Interaction")]
    public float gazeTimeToInteract = 1.5f;
    public Image loadingCircle;
    public Transform cameraTransform;
    [Tooltip("Tiempo que perdonamos si el sensor parpadea (segundos)")]
    public float graceTime = 0.2f;

    [Header("Ajustes de Inspección")]
    public float distanceInFront = 0.7f;
    public float transitionSpeed = 5f;

    [Header("Sonidos")]
    public AudioSource audioSource;
    public AudioClip soundEntry;
    public AudioClip soundExit;

    private Outline _outline;
    private float _gazeTimer = 0f;
    private bool _isGazing = false;

    private Vector3 _origPos;
    private Quaternion _origRot;
    private bool _isNear = false;
    private Vector3 _inspectPos;
    private Quaternion _inspectRot;

    // Corrutina para manejar el buffer de salida
    private Coroutine _exitRoutine;

    void Start()
    {
        _outline = GetComponent<Outline>();
        if (_outline != null) _outline.enabled = false;

        _origPos = transform.position;
        _origRot = transform.rotation;

        if (cameraTransform == null) cameraTransform = Camera.main.transform;
        if (loadingCircle != null) loadingCircle.fillAmount = 0f;
    }

    void Update()
    {
        MoverObjeto();

        // Solo sumamos al progreso si estamos mirando el objeto
        if (_isGazing)
        {
            _gazeTimer += Time.deltaTime;
            if (loadingCircle != null)
                loadingCircle.fillAmount = Mathf.Clamp01(_gazeTimer / gazeTimeToInteract);

            if (_gazeTimer >= gazeTimeToInteract)
            {
                AlternarInspeccion();
                _gazeTimer = 0f;
                if (loadingCircle != null) loadingCircle.fillAmount = 0f;
            }
        }
        // Eliminamos el 'else' que reseteaba el timer a 0 para que lo maneje la corrutina
    }

    private void AlternarInspeccion()
    {
        if (!_isNear)
        {
            // 1. Calculamos la posición frente a la cámara
            _inspectPos = cameraTransform.position + (cameraTransform.forward * distanceInFront);

            // 2. Hacemos que el objeto mire a la cámara
            _inspectRot = Quaternion.LookRotation(cameraTransform.position - _inspectPos);

            _isNear = true;
            PlaySound(soundEntry);
        }
    }

    private void MoverObjeto()
    {
        transform.position = Vector3.Lerp(transform.position, _isNear ? _inspectPos : _origPos, Time.deltaTime * transitionSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, _isNear ? _inspectRot : _origRot, Time.deltaTime * transitionSpeed);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    // --- MÉTODOS DE PUNTERO CORREGIDOS ---

    public void OnPointerEnter()
    {
        _isGazing = true;
        if (_outline) _outline.enabled = true;

        // Si el usuario vuelve antes de que expire el tiempo de gracia, cancelamos el reset
        if (_exitRoutine != null) StopCoroutine(_exitRoutine);
    }

    public void OnPointerExit()
    {
        _isGazing = false;

        // Iniciamos la espera antes de resetear el progreso y alejar el objeto
        if (_exitRoutine != null) StopCoroutine(_exitRoutine);
        _exitRoutine = StartCoroutine(GracePeriodExitRoutine());
    }

    private IEnumerator GracePeriodExitRoutine()
    {
        // 1. Tiempo de gracia para ignorar parpadeos del sensor
        yield return new WaitForSeconds(graceTime);

        if (!_isGazing)
        {
            // Si después de la espera seguimos sin mirar, limpiamos el círculo
            _gazeTimer = 0f;
            if (loadingCircle != null) loadingCircle.fillAmount = 0f;

            // 2. Un pequeño retraso extra antes de devolver el objeto a su sitio original
            yield return new WaitForSeconds(0.2f);

            if (!_isGazing)
            {
                if (_isNear)
                {
                    _isNear = false;
                    PlaySound(soundExit);
                }
                if (_outline) _outline.enabled = false;
            }
        }
    }
}