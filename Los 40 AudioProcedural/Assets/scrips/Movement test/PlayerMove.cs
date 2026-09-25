using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 3f;
    public Transform cameraTransform;

    void Update()
    {
        Vector2 input = Gamepad.current.leftStick.ReadValue();

        float h = input.x;
        float v = input.y;

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0;
        right.y = 0;

        Vector3 move = forward * v + right * h;

        transform.position += move * speed * Time.deltaTime;
    }
}