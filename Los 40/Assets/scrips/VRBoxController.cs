using UnityEngine;
using UnityEngine.InputSystem;

public class VRBoxController : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 2f;
    public Transform cameraTransform;

    [Header("Interacción")]
    public float rayDistance = 10f;
    public LayerMask interactLayer;

    void Update()
    {
        if (Gamepad.current == null) return;

        Move();
        Interact();
    }

    void Move()
    {
        Vector2 input = Gamepad.current.leftStick.ReadValue();

        Vector3 direction = new Vector3(input.y, 0, -input.x); //Rotacion 90 grados del joystick

        // Movimiento relativo a la cámara (importante en VR)
        direction = cameraTransform.TransformDirection(direction);
        direction.y = 0;

        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    void Interact()
    {
        // Botón B (segón tu mapeo principal)
        if (Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, rayDistance, interactLayer))
            {
                Debug.Log("Interactuando con: " + hit.collider.name);

                // Si quieres interfaz tipo botón
                hit.collider.SendMessage("OnInteract", SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}