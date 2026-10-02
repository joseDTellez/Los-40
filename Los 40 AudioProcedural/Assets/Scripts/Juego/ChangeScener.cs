using UnityEngine;
using System.Collections;

// Espera unos segundos y pasa a la siguiente escena con fundido.
public class ChangeScener : MonoBehaviour
{
    public float sceneDuration = 5f;
    public string nextScene = "PCFinalScene";

    private bool _iniciado = false;

    // Se llama desde el último diálogo del niño y desde GameManagerNew; solo cuenta la primera vez
    public void TriggerOutro()
    {
        if (_iniciado) return;
        _iniciado = true;

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError($"ChangeScener en '{name}' está inactivo: no se puede cambiar a '{nextScene}'.", this);
            return;
        }

        StartCoroutine(PlayCredits());
    }

    private IEnumerator PlayCredits()
    {
        yield return new WaitForSeconds(sceneDuration);
        Debug.Log($"Cambiando a la escena '{nextScene}'...");
        SceneFadeTransition.Cargar(nextScene);
    }
}
