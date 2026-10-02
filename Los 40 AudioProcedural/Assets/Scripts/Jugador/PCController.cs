using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PCController : MonoBehaviour
{
    [Header("Estado")]
    public bool canMove = true;      // Controla si el jugador puede caminar
    public bool canInteract = true;  // Controla si el jugador puede interactuar con la mirada

    [Header("Movimiento")]
    public float speed = 2f;
    public Transform cameraTransform;

    [Header("Movimiento gradual (aceleracion/desaceleracion)")]
    [Tooltip("Que tan rapido llega a la velocidad maxima al presionar una tecla")]
    public float aceleracion = 8f;
    [Tooltip("Que tan rapido frena al soltar la tecla. Mientras mas bajo, mas se 'desliza'")]
    public float desaceleracion = 4f;

    [Header("Colisiones")]
    [Tooltip("Capsula del jugador que se usa para detectar paredes antes de moverse")]
    public CapsuleCollider capsulaJugador;
    [Tooltip("Capas que bloquean el paso del jugador")]
    public LayerMask capasBloqueo = ~0;
    [Tooltip("Superficies con normal.y mayor a este valor son suelo o rampa y no bloquean")]
    [Range(0f, 1f)] public float normalMinimaSuelo = 0.6f;
    [Tooltip("Distancia que se deja entre el jugador y la pared")]
    public float margenColision = 0.05f;

    [Header("Mouse Look")]
    [Tooltip("Grados que gira la camara por cada pixel que se mueve el mouse")]
    public float sensibilidadMouse = 0.12f;
    [Tooltip("Angulo maximo para mirar arriba/abajo (evita que la camara se voltee)")]
    [Range(45f, 89f)] public float limiteVertical = 85f;

    [Header("Pasos")]
    [Tooltip("Audio de pasos en bucle que sigue la velocidad real del jugador")]
    [FormerlySerializedAs("footstepSynth")]
    public PasosBucle pasos;
    [Tooltip("Capas que cuentan como suelo para saber qué superficie pisa el jugador (madera, etc.)")]
    public LayerMask capasSuelo = ~0;

    // Velocidad actual, interpolada suavemente hacia la velocidad objetivo
    private float currentSpeed = 0f;
    // Ultima direccion de movimiento (se mantiene mientras el jugador frena)
    private Vector3 currentDirection = Vector3.zero;
    private float xRotation = 0f;
    private TipoSuperficie superficieActual = TipoSuperficie.Normal;

    void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (capsulaJugador == null)
            capsulaJugador = GetComponentInChildren<CapsuleCollider>();

        if (pasos == null)
            pasos = FindAnyObjectByType<PasosBucle>();
    }

    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        // Con el menu abierto el jugador no camina: los pasos se desvanecen
        if (pasos != null) pasos.ActualizarMovimiento(0f);
    }

    void Update()
    {
        if (cameraTransform == null) return;

        float distanciaRecorrida = 0f;
        if (canMove)
            distanciaRecorrida = Move();
        else
            // Si no puede moverse, la velocidad decae a 0 para no arrancar de golpe al volver
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, desaceleracion * Time.deltaTime);

        // Los pasos siguen la velocidad real (contra una pared no suenan) y la superficie pisada
        if (pasos != null && speed > 0f && Time.deltaTime > 0f)
        {
            if (distanciaRecorrida > 0f) superficieActual = DetectarSuperficie();
            pasos.ActualizarMovimiento(distanciaRecorrida / Time.deltaTime / speed, superficieActual);
        }

        MouseLook();
    }

    // Devuelve la distancia que realmente se recorrio este frame
    float Move()
    {
        Vector2 input = LeerMovimiento();
        bool hayInput = input.sqrMagnitude > 0.01f;

        if (hayInput)
        {
            // Movimiento relativo a la camara, sin componente vertical
            Vector3 targetDirection = cameraTransform.TransformDirection(new Vector3(input.x, 0f, input.y).normalized);
            targetDirection.y = 0f;
            if (targetDirection.sqrMagnitude > 0.0001f)
                currentDirection = targetDirection.normalized;
        }

        // Elegimos la tasa de cambio segun si estamos acelerando o frenando
        float targetSpeed = hayInput ? speed : 0f;
        float tasa = (targetSpeed > currentSpeed) ? aceleracion : desaceleracion;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, tasa * Time.deltaTime);

        if (currentSpeed <= 0.001f) return 0f;

        Vector3 movimiento = LimitarPorColisiones(currentDirection * currentSpeed * Time.deltaTime);
        if (movimiento.sqrMagnitude < 1e-10f) return 0f;

        transform.position += movimiento;
        return movimiento.magnitude;
    }

    static Vector2 LeerMovimiento()
    {
        Keyboard k = Keyboard.current;
        if (k == null) return Vector2.zero;

        Vector2 input = Vector2.zero;
        if (k.upArrowKey.isPressed || k.wKey.isPressed) input.y += 1;
        if (k.downArrowKey.isPressed || k.sKey.isPressed) input.y -= 1;
        if (k.leftArrowKey.isPressed || k.aKey.isPressed) input.x -= 1;
        if (k.rightArrowKey.isPressed || k.dKey.isPressed) input.x += 1;
        return input;
    }

    // Recorta el desplazamiento si una pared se interpone y desliza el resto a lo largo de ella.
    // El suelo y las rampas no bloquean: de eso se sigue encargando la fisica del Rigidbody.
    Vector3 LimitarPorColisiones(Vector3 delta)
    {
        if (capsulaJugador == null) return delta;

        if (!BuscarPared(delta, out RaycastHit hit)) return delta;

        Vector3 dir = delta.normalized;
        Vector3 hastaPared = dir * Mathf.Max(0f, hit.distance - margenColision);

        Vector3 resto = Vector3.ProjectOnPlane(delta - hastaPared, hit.normal);
        resto.y = 0f;
        if (resto.sqrMagnitude > 1e-10f && BuscarPared(resto, out RaycastHit hit2))
            resto = resto.normalized * Mathf.Max(0f, hit2.distance - margenColision);

        return hastaPared + resto;
    }

    bool BuscarPared(Vector3 delta, out RaycastHit hit)
    {
        Transform t = capsulaJugador.transform;
        Vector3 escala = t.lossyScale;
        float radio = capsulaJugador.radius * Mathf.Max(Mathf.Abs(escala.x), Mathf.Abs(escala.z));
        float alto = Mathf.Max(capsulaJugador.height * Mathf.Abs(escala.y), radio * 2f);
        Vector3 centro = t.TransformPoint(capsulaJugador.center);
        Vector3 eje = t.up * (alto * 0.5f - radio);

        float distancia = delta.magnitude;
        bool golpe = Physics.CapsuleCast(centro + eje, centro - eje, radio, delta / distancia, out hit,
                                         distancia + margenColision, capasBloqueo, QueryTriggerInteraction.Ignore);

        if (!golpe || hit.normal.y >= normalMinimaSuelo) return false;

        // Ignoramos los colliders que pertenecen al propio jugador
        Rigidbody otro = hit.collider.attachedRigidbody;
        return otro == null || otro.transform != transform;
    }

    // Lanza un rayo hacia abajo desde el centro del jugador y mira si lo que pisa (o alguno de
    // sus padres) tiene SuperficieSonora, por ejemplo el puente de madera.
    TipoSuperficie DetectarSuperficie()
    {
        if (capsulaJugador == null) return TipoSuperficie.Normal;

        Transform t = capsulaJugador.transform;
        Vector3 centro = t.TransformPoint(capsulaJugador.center);
        float mitad = capsulaJugador.height * Mathf.Abs(t.lossyScale.y) * 0.5f;

        if (!Physics.Raycast(centro, Vector3.down, out RaycastHit hit, mitad + 0.6f, capasSuelo, QueryTriggerInteraction.Ignore))
            return superficieActual; // sin suelo debajo (p. ej. un pequeño salto): se mantiene la última

        SuperficieSonora superficie = hit.collider.GetComponentInParent<SuperficieSonora>();
        return superficie != null ? superficie.tipo : TipoSuperficie.Normal;
    }

    void MouseLook()
    {
        if (Mouse.current == null) return;

        // El delta del mouse ya es "por frame": no se multiplica por deltaTime
        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * sensibilidadMouse;

        xRotation = Mathf.Clamp(xRotation - mouseDelta.y, -limiteVertical, limiteVertical);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseDelta.x);
    }
}
