using UnityEngine;

public class DoorController : MonoBehaviour
{
    public Transform door;
    public float openAngle = 90f;
    public float speed = 2f;

    bool abrir = false;
    Quaternion rotacionAbierta;

    void Start()
    {
        rotacionAbierta = Quaternion.Euler(door.eulerAngles + new Vector3(0, openAngle, 0));
    }

    void Update()
    {
        if (abrir)
        {
            door.rotation = Quaternion.Slerp(door.rotation, rotacionAbierta, Time.deltaTime * speed);
        }
    }

    public void OpenDoor()
    {
        abrir = true;
    }
}