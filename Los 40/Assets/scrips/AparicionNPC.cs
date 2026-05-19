using UnityEngine;

public class AparicionNPC : MonoBehaviour
{
    [Header("Visuales del NPC")]
    [Tooltip("Arrastra aquí el GameObject o modelo 3D del NPC")]
    public GameObject npcModel;

    [Header("Condiciones de Aparición")]
    public int dialogosRequeridos = 2; // Cantidad exacta de interacciones necesarias

    private void Start()
    {
        // Asegura que el NPC inicie completamente oculto en el escenario
        if (npcModel != null)
        {
            npcModel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[AparicionNPC] No has asignado el modelo del NPC en el Inspector.");
        }
    }

    // Este método evaluará el progreso y activará el NPC si se cumple la condición
    public void EvaluarEstadoNPC()
    {
        if (GameManagerNew.Instance != null)
        {
            // Si las interacciones clave llegan al número requerido, mostramos el modelo 3D
            if (GameManagerNew.Instance.interaccionesClave == dialogosRequeridos)
            {
                if (npcModel != null && !npcModel.activeSelf)
                {
                    npcModel.SetActive(true);
                    Debug.Log("✨ [AparicionNPC] Condición cumplida: El NPC ha aparecido en el mapa.");
                }
            }
        }
    }
}