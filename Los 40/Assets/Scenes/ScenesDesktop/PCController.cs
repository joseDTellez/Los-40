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

    [Header("Movimiento gradual (aceleracion/desaceleracion)")]
    [Tooltip("Que tan rapido llega a la velocidad maxima al presionar una tecla")]
    public float aceleracion = 8f;
    [Tooltip("Que tan rapido frena al soltar la tecla. Mientras mas bajo, mas se 'desliza'")]
    public float desaceleracion = 4f;

    // Velocidad actual, interpolada suavemente hacia la velocidad objetivo
    private float currentSpeed = 0f;
    // Direccion de movimiento suavizada (para que tambien gire suave, no solo frene)
    private Vector3 currentDirection = Vector3.zero;

    [Header("Mouse Look")]
    public float MouseSensitivity = 100f;
    float xRotation = 0f;

    [Header("Interaccion")]
    public float rayDistance = 10f;
    public LayerMask interactLayer;

    [Header("Pasos (Footsteps) - Sintesis Aditiva")]
    [Tooltip("Referencia al componente que genera el sonido del paso por sintesis aditiva")]
    public FootstepSynth footstepSynth;
    [Tooltip("Distancia recorrida entre cada paso")]
    public float stepDistance = 2f;

    // Acumulador de distancia recorrida desde el ultimo paso
    private float distanceAccumulator = 0f;

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
        else
        {
            // Si no puede moverse, igual dejamos que la velocidad decaiga a 0 suavemente
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, desaceleracion * Time.deltaTime);
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
        //Cambia los valores de input si la tecla esta presionada
        if (Keyboard.current.upArrowKey.isPressed)
            input.y += 1;
        if (Keyboard.current.downArrowKey.isPressed)
            input.y -= 1;
        if (Keyboard.current.leftArrowKey.isPressed)
            input.x -= 1;
        if (Keyboard.current.rightArrowKey.isPressed)
            input.x += 1;

        bool hayInput = input.sqrMagnitude > 0.01f;

        Vector3 targetDirection = Vector3.zero;
        if (hayInput)
        {
            targetDirection = new Vector3(input.x, 0, input.y).normalized; //Sin Rotacion
            // Movimiento relativo a la camara
            targetDirection = cameraTransform.TransformDirection(targetDirection);
            targetDirection.y = 0;
            targetDirection.Normalize();
        }

        // Velocidad objetivo: la maxima si hay input, 0 si no hay (soltaste la tecla)
        float targetSpeed = hayInput ? speed : 0f;

        // Elegimos la tasa de cambio segun si estamos acelerando o frenando
        float tasa = (targetSpeed > currentSpeed) ? aceleracion : desaceleracion;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, tasa * Time.deltaTime);

        // Suavizamos tambien la direccion, para que al girar no se sienta un "corte"
        if (hayInput)
        {
            currentDirection = targetDirection;
        }
        // Si no hay input, mantenemos la ultima direccion mientras la velocidad decae a 0

        if (currentSpeed > 0.001f)
        {
            Vector3 movimiento = currentDirection * currentSpeed * Time.deltaTime;
            transform.Translate(movimiento, Space.World);

            // Acumulamos distancia real recorrida para disparar los pasos
            distanceAccumulator += movimiento.magnitude;
            if (distanceAccumulator >= stepDistance)
            {
                distanceAccumulator = 0f;
                PlayFootstep();
            }
        }
    }

    void PlayFootstep()
    {
        if (footstepSynth == null) return;

        // Cada paso dispara la sintesis aditiva; la variacion (pitch/volumen/timbre)
        // se calcula dentro de FootstepSynth.TriggerStep(), no aqui.
        footstepSynth.TriggerStep();
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