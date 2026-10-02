using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    public Transform head;
    public Transform florReference;

    CapsuleCollider myCollider;

    void Start()
    {
        myCollider = GetComponent<CapsuleCollider>();
    }


    void Update()
    {
        if (head == null || florReference == null || myCollider == null) return;

        // La capsula va de los pies a la cabeza (nunca mas baja que su propio diametro)
        float height = head.position.y - florReference.position.y;
        myCollider.height = Mathf.Max(height, myCollider.radius * 2f);
        transform.position = head.position - Vector3.up * height / 2;
    }
}
