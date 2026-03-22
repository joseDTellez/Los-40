using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        // Encuentra la cámara principal del jugador automáticamente
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        // Hace que el Canvas rote para mirar siempre hacia el jugador
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                             mainCamera.transform.rotation * Vector3.up);
        }
    }
}