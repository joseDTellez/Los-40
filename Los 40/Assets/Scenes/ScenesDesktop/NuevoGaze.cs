using UnityEngine;

public class GazeSystem : MonoBehaviour
{
    public Transform cameraTransform;
    public float rayDistance = 10f;
    public LayerMask interactLayer;

    private GameObject currentObject;

    void Update()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayDistance, interactLayer))
        {
            GameObject hitObj = hit.collider.gameObject;

            if (currentObject != hitObj)
            {
                // Salir del anterior
                if (currentObject != null)
                {
                    currentObject.SendMessage("OnPointerExit", SendMessageOptions.DontRequireReceiver);
                }

                // Entrar al nuevo
                currentObject = hitObj;
                currentObject.SendMessage("OnPointerEnter", SendMessageOptions.DontRequireReceiver);
            }
        }
        else
        {
            if (currentObject != null)
            {
                currentObject.SendMessage("OnPointerExit", SendMessageOptions.DontRequireReceiver);
                currentObject = null;
            }
        }
    }
}