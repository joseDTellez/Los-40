using UnityEngine;
using System.Collections;

public class ChangeScener : MonoBehaviour
{
    public float sceneDuration = 5f;
    public string nextScene = "OutroScene";
    public bool contact = false;    

    void Start()
    {
        enabled = false;        
    }
    public void TriggerOutro() 
    {
        enabled = true;
        StartCoroutine(PlayCredits());

    }
    public IEnumerator PlayCredits()
    {
        yield return new WaitForSeconds(sceneDuration);

        ChangeScene();
        
    }

    void ChangeScene() 
    {
    
        SceneFadeTransition.Instance.LoadScene(nextScene);
        Debug.Log("Cambiando de escena. Iniciando outro...");
    }
        
    

}