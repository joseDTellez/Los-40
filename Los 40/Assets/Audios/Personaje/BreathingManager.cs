using UnityEngine;

public class BreathingManager : MonoBehaviour
{
    [Header("Clips de Respiración")]
    public AudioClip respiracionCalmada;
    public AudioClip respiracionAgitada;

    [Header("Configuración de Esfuerzo")]
    [Tooltip("Tiempo continuo en movimiento (segundos) para agitarse")]
    public float tiempoParaAgitarse = 4f;
    [Tooltip("Qué tan rápido se recupera el aliento al estar quieto (multiplicador)")]
    public float velocidadRecuperacion = 1.5f;

    [Header("Configuración de Audio")]
    [Tooltip("Velocidad de la transición (fade) entre los audios. Mayor = más rápido")]
    public float velocidadFade = 1.5f;
    [Tooltip("Volumen máximo de la respiración")]
    public float volumenMaximo = 1f;

    private AudioSource sourceCalmada;
    private AudioSource sourceAgitada;

    private float tiempoCaminando = 0f;
    private Vector3 posicionAnterior;
    private bool estabaCaminando = false;
    private bool estaAgitado = false; // Controla qué audio debe escucharse

    void Start()
    {
        // 1. Creamos y configuramos los dos AudioSources automáticamente vía código
        sourceCalmada = gameObject.AddComponent<AudioSource>();
        sourceCalmada.clip = respiracionCalmada;
        sourceCalmada.loop = true;
        sourceCalmada.volume = volumenMaximo; // Empieza sonando a tope
        sourceCalmada.Play();

        sourceAgitada = gameObject.AddComponent<AudioSource>();
        sourceAgitada.clip = respiracionAgitada;
        sourceAgitada.loop = true;
        sourceAgitada.volume = 0f; // Empieza en silencio total (muteado)
        sourceAgitada.Play();

        posicionAnterior = transform.position;
    }

    void Update()
    {
        // 2. Detectar movimiento comparando la posición
        float distanciaMovida = Vector3.Distance(transform.position, posicionAnterior);
        bool estaCaminando = distanciaMovida > 0.005f;

        // 3. Lógica de acumulación de esfuerzo
        if (estaCaminando)
        {
            tiempoCaminando += Time.deltaTime;
            tiempoCaminando = Mathf.Clamp(tiempoCaminando, 0f, tiempoParaAgitarse + 1f);
        }
        else
        {
            tiempoCaminando -= Time.deltaTime * velocidadRecuperacion;
            tiempoCaminando = Mathf.Max(0f, tiempoCaminando);
        }

        // 4. Determinar si el jugador está agitado al detenerse
        if (!estaCaminando && estabaCaminando)
        {
            if (tiempoCaminando >= tiempoParaAgitarse)
            {
                estaAgitado = true;
            }
            else
            {
                estaAgitado = false;
            }
        }

        // Si ya descansó el tiempo suficiente, vuelve a calmarse
        if (!estaCaminando && tiempoCaminando <= 0f)
        {
            estaAgitado = false;
        }

        // 5. Aplicar el Crossfade progresivo con Mathf.MoveTowards
        float targetCalmada = estaAgitado ? 0f : volumenMaximo;
        float targetAgitada = estaAgitado ? volumenMaximo : 0f;

        sourceCalmada.volume = Mathf.MoveTowards(sourceCalmada.volume, targetCalmada, velocidadFade * Time.deltaTime);
        sourceAgitada.volume = Mathf.MoveTowards(sourceAgitada.volume, targetAgitada, velocidadFade * Time.deltaTime);

        // Actualizar variables para el siguiente frame
        estabaCaminando = estaCaminando;
        posicionAnterior = transform.position;
    }
}