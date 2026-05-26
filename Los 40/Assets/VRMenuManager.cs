using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using DialogueEditor;

public class VRMenuManager : MonoBehaviour
{
    private enum MenuState
    {
        Main,
        Options,
        Controls
    }

    private MenuState currentState;

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject optionsControlsPanel;

    [Header("First Selected")]
    [SerializeField] private GameObject firstSelectedMain;
    [SerializeField] private GameObject firstSelectedOptions;
    [SerializeField] private GameObject firstControlsPanel;

    [Header("Menú")]
    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private Transform cameraTransform;

    [Header("Jugador")]
    [SerializeField] private PCController playerMovementScript;
    [SerializeField] private Rigidbody playerRigidbody;

    [Header("Simulador")]
    [SerializeField] private GameObject xrSimulator;

    [Header("Input")]
    [SerializeField] private Key menuKey = Key.Escape;

    private bool isMenuOpen = false;
    public bool IsMenuOpen => isMenuOpen;

    void Update()
    {
        if (ConversationManager.Instance != null &&
        ConversationManager.Instance.IsConversationActive)
        {
            return;
        }

        // Teclado (Escape)
        if (Keyboard.current != null && Keyboard.current[menuKey].wasPressedThisFrame)
        {
            ToggleMenu();
        }

        // Gamepad (Botón Norte / Triángulo / Y)
        if (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame)
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        if (isMenuOpen)
            CloseMenu();
        else
            OpenMenu();
    }

    void OpenMenu()
    {
        isMenuOpen = true;
        menuCanvas.SetActive(true);

        // --- LÓGICA DEL MOUSE (PARA QUE FUNCIONE) ---
        Cursor.visible = true;                          // Hace que el puntero se vea
        Cursor.lockState = CursorLockMode.None;         // Desbloquea el mouse del centro de la pantalla

        // Posicionar frente a la cámara
        menuCanvas.transform.position = cameraTransform.position + cameraTransform.forward * 1.1f;
        menuCanvas.transform.LookAt(cameraTransform);
        menuCanvas.transform.Rotate(0, 180, 0);

        // Bloquear movimiento del jugador
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

        SetState(MenuState.Main);
    }

    public void CloseMenu()
    {
        isMenuOpen = false;
        menuCanvas.SetActive(false);

        // --- LÓGICA DEL MOUSE (PARA VOLVER AL JUEGO) ---
        Cursor.visible = false;                         // Oculta el puntero
        Cursor.lockState = CursorLockMode.Locked;       // Bloquea el mouse para que controle la cámara

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

    void SetState(MenuState newState)
    {
        currentState = newState;

        mainPanel.SetActive(false);
        optionsPanel.SetActive(false);
        optionsControlsPanel.SetActive(false);

        EventSystem.current.SetSelectedGameObject(null);

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
        }
    }

    // --- MÉTODOS DE BOTONES ---
    public void OnClickContinue() { CloseMenu(); }
    public void OnClickOptions() { SetState(MenuState.Options); }
    public void OnClickControls() { SetState(MenuState.Controls); }
    public void OnClickBack() { SetState(MenuState.Main); }
    public void OnClickExit() { Application.Quit(); }

    private void OnEnable()
    {
        ConversationManager.OnConversationStarted += OnConversationStart;
    }

    private void OnDisable()
    {
        ConversationManager.OnConversationStarted -= OnConversationStart;
    }

    private void OnConversationStart()
    {
        if (isMenuOpen)
            CloseMenu();
    }
}