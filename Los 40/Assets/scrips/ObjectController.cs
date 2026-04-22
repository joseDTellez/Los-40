using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ObjectController : MonoBehaviour
{
    [Header("Gaze Interaction Settings")]
    public float gazeTimeToInteract = 2f;
    public Image loadingCircle;
    public GameObject textToShow;
    [Tooltip("Tiempo de gracia para evitar parpadeos del sensor")]
    public float graceTime = 0.2f;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip openClip;
    public AudioClip closeClip;

    [Header("Door Settings")]
    public DoorController doorController;

    private Outline _outline;
    private bool _isGazingAtObject = false;
    private bool _isGazingAtPanel = false;
    private float _gazeTimer = 0f;
    private bool _interactionTriggered = false;

    // Corrutina para manejar el buffer de salida
    private Coroutine _resetRoutine;

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
        //// GESTIÓN DE LA CARGA
        //if (_isGazingAtObject && !_interactionTriggered)
        //{
        //    _gazeTimer += Time.deltaTime;
        //    if (loadingCircle != null)
        //        loadingCircle.fillAmount = Mathf.Clamp01(_gazeTimer / gazeTimeToInteract);

        //    if (_gazeTimer >= gazeTimeToInteract)
        //    {
        //        _interactionTriggered = true;
        //        ShowInformation();
        //        _gazeTimer = 0f;
        //        if (loadingCircle != null) loadingCircle.fillAmount = 0f;
        //    }
        //}

        //// GESTIÓN DEL CIERRE AUTOMÓTICO (Invoke)
        //// Si no estamos mirando nada y ya se activó la información
        //if (!_isGazingAtObject && !_isGazingAtPanel && _interactionTriggered)
        //{
        //    if (!IsInvoking("ClosePanel")) Invoke("ClosePanel", 5f);
        //}
        //else
        //{
        //    // Si volvemos a mirar, cancelamos el cierre
        //    CancelInvoke("ClosePanel");
        //}

        if (_isGazingAtObject)
        {
            //Entrada por teclado para pruebas
            if (Keyboard.current.kKey.wasPressedThisFrame)
            {
                Debug.Log("Se presiona k");
                ShowInformation();
            }

            // GAMEPAD (gatillo / botón)
            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
            {
                ShowInformation();
            }
        }

    }
    //Debug de abrir la puerta
    private void ShowInformation()
    {
        if (_outline != null) _outline.enabled = false;

        if (textToShow != null) textToShow.SetActive(true);

        if (audioSource != null && openClip != null)
        {
            audioSource.PlayOneShot(openClip);
        }

        if (doorController != null)
        {
            Debug.Log("Abriendo puerta");
            doorController.OpenDoor();
        }
    }

    private void ClosePanel()
    {
        // Doble verificación de que realmente no estamos mirando antes de cerrar
        if (!_isGazingAtObject && !_isGazingAtPanel)
        {
            if (textToShow != null) textToShow.SetActive(false);

            if (audioSource != null && closeClip != null)
            {
                audioSource.PlayOneShot(closeClip);
            }

            _interactionTriggered = false;
            _gazeTimer = 0f;
            if (loadingCircle != null) loadingCircle.fillAmount = 0f;
        }
    }

    // --- MÉTODOS DE ENTRADA CORREGIDOS ---

    public void OnPointerEnter()
    {
        _isGazingAtObject = true;
        CancelInvoke("ClosePanel");

        // Cancelamos el reset por parpadeo
        if (_resetRoutine != null) StopCoroutine(_resetRoutine);

        if (_outline != null && !_interactionTriggered) _outline.enabled = true;
    }

    public void OnPointerExit()
    {
        _isGazingAtObject = false;

        // En lugar de resetear a 0, iniciamos el tiempo de gracia
        if (_resetRoutine != null) StopCoroutine(_resetRoutine);
        _resetRoutine = StartCoroutine(GracePeriodRoutine());
    }

    public void OnPanelEnter()
    {
        _isGazingAtPanel = true;
        CancelInvoke("ClosePanel");
    }

    public void OnPanelExit()
    {
        _isGazingAtPanel = false;
    }

    // --- RUTINA DE ESTABILIDAD ---
    private IEnumerator GracePeriodRoutine()
    {
        yield return new WaitForSeconds(graceTime);

        // Solo si después del tiempo de gracia seguimos sin mirar, reseteamos el progreso
        if (!_isGazingAtObject)
        {
            if (!_interactionTriggered)
            {
                _gazeTimer = 0f;
                if (loadingCircle != null) loadingCircle.fillAmount = 0f;
                if (_outline != null) _outline.enabled = false;
            }
        }
    }
}