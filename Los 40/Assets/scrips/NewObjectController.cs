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
        // Iniciamos en estado Idle para que el objeto respire
        if (_outline != null) _outline.SetState(Outline.InteractionState.Idle);

        _origPos = transform.position;
        _origRot = transform.rotation;

        if (cameraTransform == null) cameraTransform = Camera.main.transform;
        if (loadingCircle != null) loadingCircle.fillAmount = 0f;
    }

    void Update()
    {
        MoverObjeto();

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
    }

    private void AlternarInspeccion()
    {
        if (!_isNear)
        {
            _inspectPos = cameraTransform.position + (cameraTransform.forward * distanceInFront);
            _inspectRot = Quaternion.LookRotation(cameraTransform.position - _inspectPos);

            _isNear = true;
            PlaySound(soundEntry);

            // Al estar en inspección, podemos poner el outline en Interacting (invisible)
            // para que no tape la lectura del objeto (como el periódico)
            if (_outline) _outline.SetState(Outline.InteractionState.Interacting);
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

        // Si no está cerca, ponemos el outline fijo (Hover)
        if (_outline && !_isNear) _outline.SetState(Outline.InteractionState.Hover);

        if (_exitRoutine != null) StopCoroutine(_exitRoutine);
    }

    public void OnPointerExit()
    {
        _isGazing = false;

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

            yield return new WaitForSeconds(0.2f);

            if (!_isGazing)
            {
                if (_isNear)
                {
                    _isNear = false;
                    PlaySound(soundExit);
                }

                // Al perder el foco, vuelve a respirar (Idle)
                if (_outline) _outline.SetState(Outline.InteractionState.Idle);
            }
        }
    }
}