// EventoDialogo.cs — con verificación defensiva
using UnityEngine;

public class EventoDialogoNew : MonoBehaviour
{
    [SerializeField] private string idInteraccion;

    public void Registrar()
    {
        // ? Verificación antes de llamar para evitar NullReferenceException
        if (GameManagerNew.Instance == null)
        {
            Debug.LogError("GameManagerNew no encontrado en la escena. " +
                           "Asegúrate de que el GameObject con GameManagerNew existe.");
            return;
        }

        GameManagerNew.Instance.RegistrarInteraccionClave(idInteraccion);
    }

}