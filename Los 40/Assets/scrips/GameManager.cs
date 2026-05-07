using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Progreso")]
    public int interaccionesClave = 0;
    public int interaccionesNecesarias = 3;
    // 🔑 Control de interacciones únicas
    private HashSet<string> interaccionesRegistradas = new HashSet<string>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void RegistrarInteraccionClave(string id)
    {
        // ⚠️ Si ya se registró, NO hace nada
        if (interaccionesRegistradas.Contains(id))
        {
            Debug.Log("Interacción ya registrada: " + id);
            return;
        }

        // ✅ Registrar nueva
        interaccionesRegistradas.Add(id);
        interaccionesClave++;

        Debug.Log("Nueva interacción: " + id + " | Total: " + interaccionesClave);

        VerificarProgreso();
    }

    private void VerificarProgreso()
    {
        if (interaccionesClave >= interaccionesNecesarias)
        {
            SceneManager.LoadScene("FinalScene");
        }
    }
}
