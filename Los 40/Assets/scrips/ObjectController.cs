using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ObjectController : MonoBehaviour
{
    [Header("Gaze Interaction Settings")]
    public float gazeTimeToInteract = 2f;
    public Image loadingCircle;
    public GameObject textToShow;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip openClip;   // Sonido al abrir (entrada)
    public AudioClip closeClip;  // Sonido al cerrar (salida)

    private Outline _outline;
    private bool _isGazingAtObject = false;
    private bool _isGazingAtPanel = false;
    private float _gazeTimer = 0f;
    private bool _interactionTriggered = false;

    void Start()
    {
        _outline = GetComponent<Outline>();
        if (_outline != null) _outline.enabled = false;

        if (textToShow != null) textToShow.SetActive(false);
        if (loadingCircle != null) loadingCircle.fillAmount = 0f;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (_isGazingAtObject && !_interactionTriggered)
        {
            _gazeTimer += Time.deltaTime;
            if (loadingCircle != null)
                loadingCircle.fillAmount = _gazeTimer / gazeTimeToInteract;

            if (_gazeTimer >= gazeTimeToInteract)
            {
                _interactionTriggered = true;
                ShowInformation();
            }
        }
        else if (!_isGazingAtObject && !_interactionTriggered)
        {
            _gazeTimer = 0f;
            if (loadingCircle != null) loadingCircle.fillAmount = 0f;
        }

        if (!_isGazingAtObject && !_isGazingAtPanel && _interactionTriggered)
        {
            if (!IsInvoking("ClosePanel")) Invoke("ClosePanel", 5f);
        }
        else
        {
            CancelInvoke("ClosePanel");
        }
    }

    private void ShowInformation()
    {
        if (_outline != null) _outline.enabled = false;

        if (textToShow != null)
        {
            textToShow.SetActive(true);
        }

        if (audioSource != null && openClip != null)
        {
            audioSource.PlayOneShot(openClip);
        }
    }

    private void ClosePanel()
    {
        if (!_isGazingAtObject && !_isGazingAtPanel)
        {
            if (textToShow != null) textToShow.SetActive(false);

            // REPRODUCIR SONIDO DE SALIDA
            if (audioSource != null && closeClip != null)
            {
                audioSource.PlayOneShot(closeClip);
            }

            _interactionTriggered = false;
            _gazeTimer = 0f;
            if (loadingCircle != null) loadingCircle.fillAmount = 0f;
        }
    }

    public void OnPointerEnter() { _isGazingAtObject = true; CancelInvoke("ClosePanel"); if (_outline != null && !_interactionTriggered) _outline.enabled = true; 
    }
    public void OnPointerExit() { _isGazingAtObject = false; if (_outline != null) _outline.enabled = false; }
    public void OnPanelEnter() { _isGazingAtPanel = true; CancelInvoke("ClosePanel"); }
    public void OnPanelExit() { _isGazingAtPanel = false; }
}