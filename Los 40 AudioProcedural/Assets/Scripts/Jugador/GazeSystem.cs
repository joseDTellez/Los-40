using UnityEngine;

// Unico sistema de "mirada" del jugador: lanza un rayo desde la camara y avisa a los
// objetos interactivos con OnPointerEnter / OnPointerExit (via SendMessage).
public class GazeSystem : MonoBehaviour
{
    public Transform cameraTransform;
    public float rayDistance = 10f;
    public LayerMask interactLayer;

    private GameObject currentObject;
    private float currentDistance;
    private PCController player;
    private readonly RaycastHit[] hits = new RaycastHit[8];

    // Lo usa la reticula para crecer cuando hay algo interactivo enfrente
    public bool HayObjetivo => currentObject != null;
    public float DistanciaObjetivo => currentDistance;

    void Awake()
    {
        player = GetComponent<PCController>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        // Durante dialogos o con el menu abierto no se interactua con nada
        bool bloqueado = player != null && (!player.enabled || !player.canInteract);

        if (!bloqueado && cameraTransform != null && BuscarObjetivo(out RaycastHit hit))
        {
            GameObject hitObj = hit.collider.gameObject;
            currentDistance = hit.distance;

            if (currentObject != hitObj)
            {
                // Salir del anterior y entrar al nuevo
                Salir();
                currentObject = hitObj;
                currentObject.SendMessage("OnPointerEnter", SendMessageOptions.DontRequireReceiver);
            }
        }
        else
        {
            Salir();
        }
    }

    void OnDisable()
    {
        Salir();
    }

    void Salir()
    {
        if (currentObject != null)
            currentObject.SendMessage("OnPointerExit", SendMessageOptions.DontRequireReceiver);
        currentObject = null;
    }

    bool BuscarObjetivo(out RaycastHit mejor)
    {
        mejor = default;
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        int n = Physics.RaycastNonAlloc(ray, hits, rayDistance, interactLayer, QueryTriggerInteraction.Collide);
        if (n == 0) return false;

        int cercano = 0;
        for (int i = 1; i < n; i++)
            if (hits[i].distance < hits[cercano].distance) cercano = i;
        mejor = hits[cercano];

        if (!mejor.collider.isTrigger) return true;

        // Si lo mas cercano es un volumen trigger (p. ej. el cuerpo de la radio) y dentro de el
        // hay una pieza solida (p. ej. una perilla), se prioriza la pieza para que sea alcanzable.
        Bounds volumen = mejor.collider.bounds;
        int solido = -1;
        for (int i = 0; i < n; i++)
        {
            if (hits[i].collider.isTrigger || !volumen.Contains(hits[i].point)) continue;
            if (solido < 0 || hits[i].distance < hits[solido].distance) solido = i;
        }
        if (solido >= 0) mejor = hits[solido];
        return true;
    }
}
