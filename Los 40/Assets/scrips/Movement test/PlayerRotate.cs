using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRotate : MonoBehaviour
{
    public float rotationSpeed = 120f;

    void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE

        if (Gamepad.current == null) return;

        Vector2 look = Gamepad.current.rightStick.ReadValue();

        float yaw = look.x;
        float pitch = look.y;

        transform.Rotate(-pitch * rotationSpeed * Time.deltaTime,
                         yaw * rotationSpeed * Time.deltaTime,
                         0);

#endif
    }
}