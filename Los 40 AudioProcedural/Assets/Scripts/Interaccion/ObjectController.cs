using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ObjectController : MonoBehaviour
{
    [Header("Gaze Interaction Settings")]
    public Image loadingCircle;
    public GameObject textToShow;
    [Tooltip("Tiempo de gracia para evitar parpadeos del sensor")]
    public float graceTime = 0.2f;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip openClip;
    public AudioClip closeClip;
    public AudioClip soundHover; // NUEVO: Clip de audio para cuando se mira el objeto

    [Header("Door Settings")]
    public DoorController doorController;

    private Outline _outline;
    private bool _isGazingAtObject = false;
    private bool _interactionTriggered = false;

    // NUEVO: Variable para controlar si la puerta está abierta o cerrada
    private bool _isDoorOpen = false;

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
        if (!_isGazingAtObject) return;

        // Clic izquierdo, tecla K o R1 del mando (una sola vez aunque se pulsen varios a la vez)
        bool pulsado = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                       (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame) ||
                       (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame);
        if (pulsado)
            ToggleDoorState();
    }

    // NUEVO: Método que alterna el estado de la puerta (Abrir/Cerrar)
    private void ToggleDoorState()
    {
        if (_outline != null) _outline.enabled = false;

        // Invertimos el estado actual
        _isDoorOpen = !_isDoorOpen;

        if (_isDoorOpen)
        {
            // --- ACCIÓN DE ABRIR ---
            if (textToShow != null) textToShow.SetActive(true);

            if (audioSource != null && openClip != null)
            {
                audioSource.PlayOneShot(openClip);
            }

            if (doorController != null)
                doorController.OpenDoor();

            _interactionTriggered = true; // Marca que ya se interactuó
        }
        else
        {
            // --- ACCIÓN DE CERRAR ---
            if (textToShow != null) textToShow.SetActive(false);

            if (audioSource != null && closeClip != null)
            {
                audioSource.PlayOneShot(closeClip);
            }

            if (doorController != null)
                doorController.CloseDoor();

            _interactionTriggered = false; // Reinicia la interacción para que vuelva a aparecer el Outline al mirar
        }
    }

    // NUEVO: Método para reproducir el sonido sin interrumpir los demás
    private void PlayHoverSound()
    {
        if (audioSource != null && soundHover != null)
        {
            audioSource.PlayOneShot(soundHover);
        }
    }

    // --- MÉTODOS DE ENTRADA CORREGIDOS ---

    public void OnPointerEnter()
    {
        _isGazingAtObject = true;

        // Cancelamos el reset por parpadeo
        if (_resetRoutine != null) StopCoroutine(_resetRoutine);

        if (_outline != null && !_interactionTriggered) _outline.enabled = true;

        // NUEVO: Reproducimos el sonido al mirar el objeto, solo si no está ya activo
        if (!_interactionTriggered)
        {
            PlayHoverSound();
        }
    }

    public void OnPointerExit()
    {
        _isGazingAtObject = false;

        // En lugar de resetear a 0, iniciamos el tiempo de gracia
        if (!gameObject.activeInHierarchy) return;
        if (_resetRoutine != null) StopCoroutine(_resetRoutine);
        _resetRoutine = StartCoroutine(GracePeriodRoutine());
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
                if (loadingCircle != null) loadingCircle.fillAmount = 0f;
                if (_outline != null) _outline.enabled = false;
            }
        }
    }
}