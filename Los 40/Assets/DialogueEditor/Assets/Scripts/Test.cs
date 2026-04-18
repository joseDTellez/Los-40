using UnityEngine;
using UnityEngine.InputSystem;
using DialogueEditor;

public class DialogueInputController : MonoBehaviour
{
    void Update()
    {
        if (ConversationManager.Instance == null) return;

        if (!ConversationManager.Instance.IsConversationActive) return;

        Debug.Log("Input detectado");

        // TECLADO
        if (Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
                ConversationManager.Instance.SelectPreviousOption();

            if (Keyboard.current.downArrowKey.wasPressedThisFrame)
                ConversationManager.Instance.SelectNextOption();

            if (Keyboard.current.enterKey.wasPressedThisFrame)
                ConversationManager.Instance.PressSelectedOption();

            if (Keyboard.current.anyKey.wasPressedThisFrame)
            {
                Debug.Log("Tecla presionada");
            }

        }



        // GAMEPAD
        if (Gamepad.current != null)
        {
            // Navegación con stick o d-pad
            if (Gamepad.current.dpad.up.wasPressedThisFrame ||
                Gamepad.current.leftStick.up.wasPressedThisFrame)
            {
                ConversationManager.Instance.SelectPreviousOption();
            }

            if (Gamepad.current.dpad.down.wasPressedThisFrame ||
                Gamepad.current.leftStick.down.wasPressedThisFrame)
            {
                ConversationManager.Instance.SelectNextOption();
            }

            // Seleccionar opción (botón A o gatillo)
            if (Gamepad.current.buttonSouth.wasPressedThisFrame ||
                Gamepad.current.rightTrigger.wasPressedThisFrame)
            {
                ConversationManager.Instance.PressSelectedOption();
            }
            if (Keyboard.current.anyKey.wasPressedThisFrame)
            {
                Debug.Log("Tecla presionada");
            }
        }
    }
}