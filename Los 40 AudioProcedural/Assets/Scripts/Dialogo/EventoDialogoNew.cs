using UnityEngine;

// Se llama desde los eventos de Dialogue Editor para contar una interacción clave.
public class EventoDialogoNew : MonoBehaviour
{
    [SerializeField] private string idInteraccion;

    public void Registrar()
    {
        // En escenas sin GameManagerNew (p. ej. la final) simplemente no se cuenta progreso
        if (GameManagerNew.Instance == null)
        {
            Debug.LogWarning($"'{idInteraccion}': no hay GameManagerNew en esta escena, no se registra progreso.", this);
            return;
        }

        GameManagerNew.Instance.RegistrarInteraccionClave(idInteraccion);
    }
}
