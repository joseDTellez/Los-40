using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Progreso")]
    public int interaccionesClave = 0;
    public int interaccionesNecesarias = 2;

    [Header("Referencias")]
    [SerializeField] private GameObject barrera;
    [SerializeField] private GameObject dialogoVecinosEstado1;
    [SerializeField] private GameObject dialogoVecinosEstado2;

    private bool vecinosActualizados = false;

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
            DesbloquearZona();
        }
        // 🔥 NUEVA lógica: cambiar diálogo de vecinos
        if (!vecinosActualizados && interaccionesClave >= 4)
        {
            CambiarDialogoVecinos();
        }
    }

    private void DesbloquearZona()
    {
        if (barrera != null)
        {
            barrera.SetActive(false);
            Debug.Log("Zona desbloqueada");
        }
    }
    private void CambiarDialogoVecinos()
    {
        vecinosActualizados = true;

        Debug.Log("🟡 Vecinos cambian a diálogo estado 2");

        if (dialogoVecinosEstado1 != null)
            dialogoVecinosEstado1.SetActive(false);

        if (dialogoVecinosEstado2 != null)
            dialogoVecinosEstado2.SetActive(true);
    }
}
