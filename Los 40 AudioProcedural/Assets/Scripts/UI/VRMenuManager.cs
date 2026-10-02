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
    [Tooltip("Si se deja vacío se busca el PCController de la escena")]
    [SerializeField] private PCController playerMovementScript;
    [SerializeField] private Rigidbody playerRigidbody;

    [Header("Input")]
    [SerializeField] private Key menuKey = Key.Escape;

    private bool isMenuOpen = false;
    private bool rigidbodyEraKinematic;
    public bool IsMenuOpen => isMenuOpen;

    void Awake()
    {
        if (playerMovementScript == null)
            playerMovementScript = FindAnyObjectByType<PCController>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        // Durante un diálogo el menú no se abre (Escape no interrumpe la conversación)
        if (ConversationManager.Instance != null && ConversationManager.Instance.IsConversationActive)
            return;

        bool teclado = Keyboard.current != null && Keyboard.current[menuKey].wasPressedThisFrame;
        // Botón Start/Menu del mando: el botón A (Sur) ya se usa para confirmar en la UI
        bool mando = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;

        if (teclado || mando)
            ToggleMenu();
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
        if (menuCanvas == null) return;

        isMenuOpen = true;
        menuCanvas.SetActive(true);

        // Liberamos el puntero para poder hacer clic en los botones
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Posicionar frente a la cámara
        if (cameraTransform != null)
        {
            menuCanvas.transform.position = cameraTransform.position + cameraTransform.forward * 1.1f;
            menuCanvas.transform.LookAt(cameraTransform);
            menuCanvas.transform.Rotate(0, 180, 0);
        }

        // Bloquear movimiento y cámara del jugador
        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        if (playerRigidbody != null)
        {
            rigidbodyEraKinematic = playerRigidbody.isKinematic;
            if (!rigidbodyEraKinematic)
            {
                playerRigidbody.linearVelocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
            }
            playerRigidbody.isKinematic = true;
        }

        SetState(MenuState.Main);
    }

    public void CloseMenu()
    {
        if (!isMenuOpen) return;

        isMenuOpen = false;
        if (menuCanvas != null) menuCanvas.SetActive(false);

        // Volvemos a capturar el mouse para controlar la cámara
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Restaurar jugador
        if (playerMovementScript != null)
            playerMovementScript.enabled = true;

        if (playerRigidbody != null)
            playerRigidbody.isKinematic = rigidbodyEraKinematic;

        Seleccionar(null);
    }

    void SetState(MenuState newState)
    {
        if (mainPanel != null) mainPanel.SetActive(newState == MenuState.Main);
        if (optionsPanel != null) optionsPanel.SetActive(newState == MenuState.Options);
        if (optionsControlsPanel != null) optionsControlsPanel.SetActive(newState == MenuState.Controls);

        switch (newState)
        {
            case MenuState.Main: Seleccionar(firstSelectedMain); break;
            case MenuState.Options: Seleccionar(firstSelectedOptions); break;
            case MenuState.Controls: Seleccionar(firstControlsPanel); break;
        }
    }

    static void Seleccionar(GameObject objeto)
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(objeto);
    }

    // --- MÉTODOS DE BOTONES ---
    public void OnClickContinue() { CloseMenu(); }
    public void OnClickOptions() { SetState(MenuState.Options); }
    public void OnClickControls() { SetState(MenuState.Controls); }
    public void OnClickBack() { SetState(MenuState.Main); }

    public void OnClickExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

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
