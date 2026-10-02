using UnityEngine;

public enum TipoSuperficie { Normal, Madera }

// Marca un objeto (y todos sus hijos) como superficie con sonido propio de pasos.
// PCController mira qué hay bajo los pies del jugador y avisa a PasosBucle.
public class SuperficieSonora : MonoBehaviour
{
    public TipoSuperficie tipo = TipoSuperficie.Madera;
}
