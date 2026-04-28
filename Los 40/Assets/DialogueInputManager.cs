using UnityEngine;
using UnityEngine.InputSystem;
using DialogueEditor;

public class DialogueInputManager : MonoBehaviour
{
    public static DialogueInputManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (ConversationManager.Instance == null) return;
        if (!ConversationManager.Instance.IsConversationActive) return;

        HandleKeyboard();
        HandleGamepad();
    }

    void HandleKeyboard()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.upArrowKey.wasPressedThisFrame)
            ConversationManager.Instance.SelectPreviousOption();
        else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
            ConversationManager.Instance.SelectNextOption();
        else if (Keyboard.current.enterKey.wasPressedThisFrame)
            ConversationManager.Instance.PressSelectedOption();
    }

    void HandleGamepad()
    {
        if (Gamepad.current == null) return;

        if (Gamepad.current.dpad.up.wasPressedThisFrame ||
            Gamepad.current.leftStick.right.wasPressedThisFrame)
            ConversationManager.Instance.SelectPreviousOption();
        else if (Gamepad.current.dpad.down.wasPressedThisFrame ||
                 Gamepad.current.leftStick.left.wasPressedThisFrame)
            ConversationManager.Instance.SelectNextOption();
        else if (Gamepad.current.buttonSouth.wasPressedThisFrame ||
                 Gamepad.current.rightShoulder.wasPressedThisFrame)
            ConversationManager.Instance.PressSelectedOption();
    }
}