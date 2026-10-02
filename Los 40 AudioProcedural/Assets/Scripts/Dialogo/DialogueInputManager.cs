using UnityEngine;
using UnityEngine.InputSystem;
using DialogueEditor;

// Unico punto que traduce teclado/mando a la navegacion de los dialogos.
// Ningun otro script debe llamar a SelectNext/SelectPrevious/PressSelectedOption,
// o cada pulsacion avanzaria varias opciones.
public class DialogueInputManager : MonoBehaviour
{
    [Header("Stick del mando")]
    [Tooltip("Inclinacion minima del stick para cambiar de opcion")]
    [Range(0.1f, 0.9f)] public float deadzone = 0.5f;
    [Tooltip("Segundos entre cambios de opcion mientras se mantiene el stick inclinado")]
    public float scrollDelay = 0.3f;

    private static DialogueInputManager activo;
    private float siguienteMovimientoStick;

    void OnEnable()
    {
        // Si por error hay dos en la escena, solo uno lee la entrada
        if (activo != null && activo != this)
        {
            enabled = false;
            return;
        }
        activo = this;
    }

    void OnDisable()
    {
        if (activo == this) activo = null;
    }

    void Update()
    {
        ConversationManager cm = ConversationManager.Instance;
        if (cm == null || !cm.IsConversationActive) return;

        int direccion = 0;
        bool confirmar = false;

        Keyboard k = Keyboard.current;
        if (k != null)
        {
            if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) direccion = -1;
            else if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) direccion = 1;

            confirmar |= k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame;
        }

        Gamepad g = Gamepad.current;
        if (g != null)
        {
            if (g.dpad.up.wasPressedThisFrame) direccion = -1;
            else if (g.dpad.down.wasPressedThisFrame) direccion = 1;
            else if (direccion == 0) direccion = LeerStick(g.leftStick.ReadValue().y);

            confirmar |= g.buttonSouth.wasPressedThisFrame || g.rightShoulder.wasPressedThisFrame;
        }

        if (direccion < 0) cm.SelectPreviousOption();
        else if (direccion > 0) cm.SelectNextOption();

        // PressSelectedOption se ignora solo si el dialogo aun esta en transicion
        if (confirmar) cm.PressSelectedOption();
    }

    int LeerStick(float y)
    {
        if (Mathf.Abs(y) < deadzone)
        {
            siguienteMovimientoStick = 0f;
            return 0;
        }

        if (Time.unscaledTime < siguienteMovimientoStick) return 0;

        siguienteMovimientoStick = Time.unscaledTime + scrollDelay;
        return y > 0f ? -1 : 1;
    }
}
