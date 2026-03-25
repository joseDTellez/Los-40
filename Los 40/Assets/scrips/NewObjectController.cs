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
        MoverObjeto();

        if (_isGazing)
        {
            _gazeTimer += Time.deltaTime;
            if (loadingCircle != null)
                loadingCircle.fillAmount = _gazeTimer / gazeTimeToInteract;

            if (_gazeTimer >= gazeTimeToInteract)
            {
                AlternarInspeccion();
                _gazeTimer = 0f;
            }
        }
        else
        {
            _gazeTimer = 0f;
            if (loadingCircle != null) loadingCircle.fillAmount = 0f;
        }
    }

    private void AlternarInspeccion()
    {
        if (!_isNear)
        {
            // 1. Calculamos la posición frente a la cámara
            _inspectPos = cameraTransform.position + (cameraTransform.forward * distanceInFront);

            // 2. SOLUCIÓN: Hacemos que el objeto mire a la cámara
            // Esto hará que el eje Z (azul) del periódico apunte a tus ojos
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

    public void OnPointerEnter()
    {
        _isGazing = true;
        if (_outline) _outline.enabled = true;
    }

    public void OnPointerExit()
    {
        _isGazing = false;
        StartCoroutine(EsperaRegreso());
    }

    private IEnumerator EsperaRegreso()
    {
        yield return new WaitForSeconds(0.1f);
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