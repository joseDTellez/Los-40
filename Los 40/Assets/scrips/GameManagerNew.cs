using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManagerNew : MonoBehaviour
{
    public static GameManagerNew Instance; // ✅ tipo corregido

    [Header("Progreso")]
    public int interaccionesClave = 0;
    public int interaccionesNecesarias = 3;  // ✅ configurable desde el Inspector
    public string escenaDestino = "EscenaSiguiente"; // ✅ nombre de la escena destino
    [Header("Outro")]
    public OutroController outroController; // Arrastra el GameObject con OutroController

    private HashSet<string> interaccionesRegistradas = new HashSet<string>();

    // GameManagerNew.cs — agrega DontDestroyOnLoad
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // ✅ sobrevive al cambio de escena
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegistrarInteraccionClave(string id)
    {
        if (interaccionesRegistradas.Contains(id))
        {
            Debug.Log($"[GameManager] '{id}' ya registrado. Total: {interaccionesClave}/{interaccionesNecesarias}");
            return;
        }

        interaccionesRegistradas.Add(id);
        interaccionesClave++;
        Debug.Log($"[GameManager] ✅ Registrado: '{id}' | Progreso: {interaccionesClave}/{interaccionesNecesarias}");

        VerificarProgreso();
    }

    private void VerificarProgreso()
    {
        if (interaccionesClave >= interaccionesNecesarias)
        {
            Debug.Log("¡Progreso completo! Iniciando outro...");

            if (outroController != null)
                outroController.PlayOutro();
            else
                Debug.LogError("OutroController no asignado en el Inspector");
        }
    }
}