using UnityEngine;

[RequireComponent(typeof(PuertaAudioProcedural))]
public class DoorController : MonoBehaviour
{
    public Transform door;
    public float openAngle = 90f;
    public float speed = 2f;

    [Tooltip("Diferencia angular (en grados) por debajo de la cual consideramos que la puerta ya llegó a su destino.")]
    public float umbralLlegada = 0.5f;

    private Quaternion rotacionCerrada;
    private Quaternion rotacionAbierta;
    private Quaternion rotacionDestino; // Hacia dónde debe girar actualmente

    private bool enMovimiento = false; // Controla si el Update debe calcular la rotación
    private PuertaAudioProcedural audioProcedural;

    private float anguloRestanteAnterior;
    private bool moviendoPrevio = false;

    void Awake()
    {
        audioProcedural = GetComponent<PuertaAudioProcedural>();
    }

    void Start()
    {
        // Guardamos las dos posiciones: la inicial (cerrada) y la calculada (abierta)
        rotacionCerrada = door.rotation;
        rotacionAbierta = Quaternion.Euler(door.eulerAngles + new Vector3(0, openAngle, 0));

        // El destino inicial es estar cerrada
        rotacionDestino = rotacionCerrada;
    }

    void Update()
    {
        // Si no hay orden de movimiento, no hacemos nada
        if (!enMovimiento) return;

        // Rotamos suavemente hacia el destino actual (sea abierto o cerrado)
        door.rotation = Quaternion.Slerp(door.rotation, rotacionDestino, Time.deltaTime * speed);

        // Cuánto le falta a la puerta para llegar a su rotación final
        float anguloRestante = Quaternion.Angle(door.rotation, rotacionDestino);

        // Velocidad angular real de este frame
        float velocidadAngular = (anguloRestanteAnterior - anguloRestante) / Time.deltaTime;
        anguloRestanteAnterior = anguloRestante;

        bool sigueMoviendose = anguloRestante > umbralLlegada;

        if (sigueMoviendose)
        {
            audioProcedural.ActualizarMovimiento(velocidadAngular);
        }
        else if (moviendoPrevio)
        {
            // La puerta acaba de llegar a su ángulo final
            // Aseguramos que quede exactamente en la rotación final
            door.rotation = rotacionDestino;

            audioProcedural.DetenerChirridoYGolpear();

            // Detenemos los cálculos del Update hasta que se dé una nueva orden
            enMovimiento = false;
        }

        moviendoPrevio = sigueMoviendose;
    }

    // --- NUEVOS MÉTODOS DE APERTURA Y CIERRE ---

    public void OpenDoor()
    {
        rotacionDestino = rotacionAbierta;
        anguloRestanteAnterior = Quaternion.Angle(door.rotation, rotacionDestino);
        enMovimiento = true;
    }

    public void CloseDoor()
    {
        rotacionDestino = rotacionCerrada;
        anguloRestanteAnterior = Quaternion.Angle(door.rotation, rotacionDestino);
        enMovimiento = true;
    }
}