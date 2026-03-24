using UnityEngine;
using UnityEngine.InputSystem;

public class DoorInteraction : MonoBehaviour
{
    public GameObject textoAbrir;
    public DoorController puerta;

    public float distancia = 5f;

    bool mirando = false;

    void Update()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;

        mirando = false;

        if (Physics.Raycast(ray, out hit, distancia))
        {
            if (hit.collider.gameObject == gameObject)
            {
                mirando = true;
                textoAbrir.SetActive(true);

                // BOTÓN A DEL MANDO XBOX
                if (Gamepad.current != null && Gamepad.current.aButton.wasPressedThisFrame)
                {
                    puerta.OpenDoor();
                }
            }
        }

        if (!mirando)
        {
            textoAbrir.SetActive(false);
        }
    }
}