using UnityEngine;

public class DebugRay : MonoBehaviour
{
    void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 10))
        {
            Debug.Log("Mirando: " + hit.collider.name);
        }
    }
}