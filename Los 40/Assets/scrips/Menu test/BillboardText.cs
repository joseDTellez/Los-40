using UnityEngine;

public class BillboardText : MonoBehaviour
{
    Transform cam;

    void Start()
    {
        cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        transform.LookAt(cam);
        transform.Rotate(0, 180, 0);
    }
}