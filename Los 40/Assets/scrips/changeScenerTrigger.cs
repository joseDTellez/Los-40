using UnityEngine;

public class ChangeScenerTrigger : MonoBehaviour 
{
    public ChangeScener TriggerController;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Niña"))
        {
            TriggerController.TriggerOutro();
        }
    }
    void OnCollisionEnter(Collision other) 
    {
        if(other.gameObject.CompareTag("Niña"))
        {
                TriggerController.TriggerOutro();    
        }
        
    }






}
