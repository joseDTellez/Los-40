using System.Collections;
using System.Collections.Generic;
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
        float height = head.position.y - florReference.position.y;
        myCollider.height = height;
        transform.position = head.position - Vector3.up * height / 2;
    }
}
