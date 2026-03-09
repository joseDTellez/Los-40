using UnityEngine;

public class ReticleFeedback : MonoBehaviour
{
    public Transform reticle;
    public float normalSize = 0.02f;
    public float hoverSize = 0.04f;

    void Update()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 5f))
        {
            if (hit.collider.GetComponent<DoorInteraction>())
            {
                reticle.localScale = Vector3.one * hoverSize;
                return;
            }
        }

        reticle.localScale = Vector3.one * normalSize;
    }
}