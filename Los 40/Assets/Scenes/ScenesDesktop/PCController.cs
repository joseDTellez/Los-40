using UnityEngine;
using UnityEngine.InputSystem;

public class PCController : MonoBehaviour
{
    [Header("Estado")]
    public bool canMove = true;      // Controla si el jugador puede caminar
    public bool canInteract = true;  // Controla si el jugador puede usar el rayo

    [Header("Movimiento")]
    public float speed = 2f;
    public Transform cameraTransform;

    [Header("Mouse Look")]
    public float MouseSensitivity = 100f;
    float xRotation = 0f;

    [Header("Interacción")]
    public float rayDistance = 10f;
    public LayerMask interactLayer;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }


    void Update()
    {
        //if (Gamepad.current == null) return;

        // Solo ejecuta las funciones si los booleanos son verdaderos
        if (canMove)
        {
            Move();
        }
        MouseLook();
        //if (canInteract)
        //{
        //    Interact();
        //}
    }

    void Move()
    {
        Vector2 input = Vector2.zero;
        //Cambia los valores de input si la tecla está presionada
        if (Keyboard.current.upArrowKey.isPressed)
            input.y += 1;
        if (Keyboard.current.downArrowKey.isPressed)
            input.y -= 1;
        if (Keyboard.current.leftArrowKey.isPressed)
            input.x -= 1;
        if (Keyboard.current.rightArrowKey.isPressed)
            input.x += 1;

        Vector3 direction = new Vector3(input.x, 0, input.y); //Sin Rotacion

        // Movimiento relativo a la cámara
        direction = cameraTransform.TransformDirection(direction);
        direction.y = 0;

        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    void MouseLook()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * Time.deltaTime * MouseSensitivity;

        xRotation -= mouseDelta.y;
        xRotation = Mathf.Clamp(xRotation, -120f, 120f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseDelta.x);

        Debug.Log("MouseLook activo");
    }

    void Interact()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, rayDistance, interactLayer))
            {
                Debug.Log("Interactuando con: " + hit.collider.name);
                hit.collider.SendMessage("OnInteract", SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}

