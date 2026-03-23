using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    public Transform head;
    public Transform floorReference;

    CapsuleCollider myCollider;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        myCollider = GetComponent<CapsuleCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        float height = head.position.y - floorReference.position.y;
        myCollider.height = height;
        transform.position = head.position - Vector3.up * height / 2;
    }
}
