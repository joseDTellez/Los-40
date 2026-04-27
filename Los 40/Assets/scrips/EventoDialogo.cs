using UnityEngine;

public class EventoDialogo : MonoBehaviour
{
    [SerializeField] private string idInteraccion;

    public void Registrar()
    {
        GameManager.Instance.RegistrarInteraccionClave(idInteraccion);
    }
}