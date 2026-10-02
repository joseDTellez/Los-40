using UnityEngine;

// En la escena final: cuando la niña llega a este punto se inicia el cambio de escena.
public class ChangeScenerTrigger : MonoBehaviour
{
    public ChangeScener TriggerController;

    [Tooltip("Tag del personaje que dispara el final")]
    public string tagObjetivo = "Niña";

    void OnTriggerEnter(Collider other)
    {
        Comprobar(other.gameObject);
    }

    void OnCollisionEnter(Collision other)
    {
        Comprobar(other.gameObject);
    }

    void Comprobar(GameObject otro)
    {
        if (TriggerController != null && otro.CompareTag(tagObjetivo))
            TriggerController.TriggerOutro();
    }
}
