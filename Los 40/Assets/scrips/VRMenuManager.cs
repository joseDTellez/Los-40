using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class VRMenuManager : MonoBehaviour
{
    // =========================
    // ENUM DE ESTADOS
    // =========================
    private enum MenuState
    {
        Main,
        Options,
        Controls,
        Accessibility
    }

    private MenuState currentState;

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject optionsControlsPanel;
    [SerializeField] private GameObject optionsAcsesibilityPanel;

    [Header("First Selected")]
    [SerializeField] private GameObject firstSelectedMain;
    [SerializeField] private GameObject firstSelectedOptions;
    [SerializeField] private GameObject firstControlsPanel;
    [SerializeField] private GameObject firstAcsesibilityPanel;

    [Header("Menú")]
    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private Transform cameraTransform;

    [Header("Jugador")]
    [SerializeField] private VRBoxController playerMovementScript;
    [SerializeField] private Rigidbody playerRigidbody;

    [Header("Simulador")]
    [SerializeField] private GameObject xrSimulator;

    [Header("Input")]
    [SerializeField] private Key menuKey = Key.Escape;

    private bool isMenuOpen = false;
    public bool IsMenuOpen => isMenuOpen;

    void Update()
    {
        // Teclado
        if (Keyboard.current != null && Keyboard.current[menuKey].wasPressedThisFrame)
        {
            ToggleMenu();
        }

        // Gamepad
        if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
        {
            ToggleMenu();
        }
    }

    // =========================
    // TOGGLE
    // =========================
    public void ToggleMenu()
    {
        if (isMenuOpen)
            CloseMenu();
        else
            OpenMenu();
    }

    // =========================
    // OPEN
    // =========================
    void OpenMenu()
    {
        isMenuOpen = true;

        menuCanvas.SetActive(true);

        // Posicionar frente a la cámara
        menuCanvas.transform.position =
            cameraTransform.position + cameraTransform.forward * 1.1f;

        menuCanvas.transform.LookAt(cameraTransform);
        menuCanvas.transform.Rotate(0, 180, 0);

        // Bloquear jugador
        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.isKinematic = true;
        }

#if UNITY_EDITOR
        if (xrSimulator != null)
            xrSimulator.SetActive(false);
#endif

        // Estado inicial
        SetState(MenuState.Main);
    }

    // =========================
    // CLOSE
    // =========================
    public void CloseMenu()
    {
        isMenuOpen = false;

        menuCanvas.SetActive(false);

        // Restaurar jugador
        if (playerMovementScript != null)
            playerMovementScript.enabled = true;

        if (playerRigidbody != null)
            playerRigidbody.isKinematic = false;

#if UNITY_EDITOR
        if (xrSimulator != null)
            xrSimulator.SetActive(true);
#endif

        EventSystem.current.SetSelectedGameObject(null);
    }

    // =========================
    // CORE: CAMBIO DE ESTADO
    // =========================
    void SetState(MenuState newState)
    {
        currentState = newState;

        // Apagar todos
        mainPanel.SetActive(false);
        optionsPanel.SetActive(false);
        optionsControlsPanel.SetActive(false);
        optionsAcsesibilityPanel.SetActive(false);

        // Limpiar selección
        EventSystem.current.SetSelectedGameObject(null);

        // Activar según estado
        switch (newState)
        {
            case MenuState.Main:
                mainPanel.SetActive(true);
                EventSystem.current.SetSelectedGameObject(firstSelectedMain);
                break;

            case MenuState.Options:
                optionsPanel.SetActive(true);
                EventSystem.current.SetSelectedGameObject(firstSelectedOptions);
                break;

            case MenuState.Controls:
                optionsControlsPanel.SetActive(true);
                EventSystem.current.SetSelectedGameObject(firstControlsPanel);
                break;

            case MenuState.Accessibility:
                optionsAcsesibilityPanel.SetActive(true);
                EventSystem.current.SetSelectedGameObject(firstAcsesibilityPanel);
                break;
        }
    }

    // =========================
    // BOTONES
    // =========================

    public void OnClickContinue()
    {
        CloseMenu();
    }

    public void OnClickOptions()
    {
        SetState(MenuState.Options);
    }

    public void OnClickControls()
    {
        SetState(MenuState.Controls);
    }

    public void OnClickAccessibility()
    {
        SetState(MenuState.Accessibility);
    }

    public void OnClickBack()
    {
        SetState(MenuState.Main);
    }

    public void OnClickExit()
    {
        Debug.Log("Salir del juego");
        Application.Quit();
    }
}