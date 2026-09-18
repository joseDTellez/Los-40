using UnityEngine;

[RequireComponent(typeof(PuertaAudioProcedural))]
public class DoorController : MonoBehaviour
{
    public Transform door;
    public float openAngle = 90f;
    public float speed = 2f;

    [Tooltip("Diferencia angular (en grados) por debajo de la cual consideramos que la puerta ya llegó a su destino.")]
    public float umbralLlegada = 0.5f;

    bool abrir = false;
    Quaternion rotacionAbierta;
    PuertaAudioProcedural audioProcedural;

    float anguloRestanteAnterior;
    bool moviendoPrevio = false;

    void Awake()
    {
        audioProcedural = GetComponent<PuertaAudioProcedural>();
    }

    void Start()
    {
        rotacionAbierta = Quaternion.Euler(door.eulerAngles + new Vector3(0, openAngle, 0));
        anguloRestanteAnterior = Quaternion.Angle(door.rotation, rotacionAbierta);
    }

    void Update()
    {
        if (!abrir) return;

        door.rotation = Quaternion.Slerp(door.rotation, rotacionAbierta, Time.deltaTime * speed);

        // Cuánto le falta a la puerta para llegar a su rotación final
        float anguloRestante = Quaternion.Angle(door.rotation, rotacionAbierta);

        // Velocidad angular real de este frame: cuánto se redujo la
        // diferencia respecto al frame anterior, en grados/segundo.
        float velocidadAngular = (anguloRestanteAnterior - anguloRestante) / Time.deltaTime;
        anguloRestanteAnterior = anguloRestante;

        bool sigueMoviendose = anguloRestante > umbralLlegada;

        if (sigueMoviendose)
        {
            audioProcedural.ActualizarMovimiento(velocidadAngular);
        }
        else if (moviendoPrevio)
        {
            // La puerta acaba de llegar a su ángulo final: apaga el
            // chirrido y dispara el golpe grave, una sola vez.
            audioProcedural.DetenerChirridoYGolpear();
        }

        moviendoPrevio = sigueMoviendose;
    }

    public void OpenDoor()
    {
        abrir = true;
    }
}