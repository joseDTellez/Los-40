using UnityEngine;

public class BreathingManager : MonoBehaviour
{
    public enum EstadoRespiracion { Silencio, Suave, Acelerada }

    [Header("Clips de Respiración")]
    public AudioClip respiracionSuave;
    public AudioClip respiracionAcelerada;

    [Header("Configuración de Movimiento")]
    [Tooltip("Distancia/Velocidad mínima para detectar que el personaje está caminando")]
    public float umbralMovimiento = 0.1f;

    [Header("Tiempos de Agitación (En Segundos)")]
    [Tooltip("Cuántos segundos de caminata continua toma llegar a la respiración ACELERADA máxima")]
    public float tiempoParaAgitarse = 6.0f;
    [Tooltip("Cuántos segundos de descanso toma volver al SILENCIO total desde la agitación máxima")]
    public float tiempoParaCalmarse = 4.0f;

    [Header("Configuración de Audio")]
    public float volumenMaximo = 1f;
    [Tooltip("Qué tan rápido ocurre la transición (fade) entre los audios para que no se sobrepongan")]
    public float velocidadFade = 2.0f;

    private AudioSource sourceSuave;
    private AudioSource sourceAcelerada;

    private float nivelEsfuerzo = 0f;
    private Vector3 posicionAnterior;

    // Controladores de transición
    private EstadoRespiracion estadoActual = EstadoRespiracion.Silencio;
    private EstadoRespiracion estadoObjetivo = EstadoRespiracion.Silencio;

    void Start()
    {
        sourceSuave = CrearAudioSource(respiracionSuave, 0f);
        sourceAcelerada = CrearAudioSource(respiracionAcelerada, 0f);
        posicionAnterior = transform.position;
    }

    private AudioSource CrearAudioSource(AudioClip clip, float volumenInicial)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.volume = volumenInicial;
        if (clip != null) source.Play();
        return source;
    }

    void Update()
    {
        // 1. Medir movimiento
        float distanciaMovida = Vector3.Distance(transform.position, posicionAnterior);
        float velocidadActual = distanciaMovida / Time.deltaTime;
        bool estaMoviendose = velocidadActual >= umbralMovimiento;

        // 2. Acumular esfuerzo en el tiempo
        if (estaMoviendose)
        {
            nivelEsfuerzo += (2.0f / tiempoParaAgitarse) * Time.deltaTime;
        }
        else
        {
            nivelEsfuerzo -= (2.0f / tiempoParaCalmarse) * Time.deltaTime;
        }
        nivelEsfuerzo = Mathf.Clamp(nivelEsfuerzo, 0f, 2f);

        // 3. Definir cuál debería ser el audio que suene (Estado Objetivo)
        if (nivelEsfuerzo < 0.1f)
        {
            estadoObjetivo = EstadoRespiracion.Silencio;
        }
        else if (nivelEsfuerzo < 1.5f)
        {
            // Suena la respiración suave entre 0.1 y 1.5 de esfuerzo
            estadoObjetivo = EstadoRespiracion.Suave;
        }
        else
        {
            // Superando el 1.5 de esfuerzo, pasa a agitado
            estadoObjetivo = EstadoRespiracion.Acelerada;
        }

        // 4. Procesar la transición limpiamente
        TransicionarAudios();

        posicionAnterior = transform.position;
    }

    private void TransicionarAudios()
    {
        // Identificar qué fuente corresponde al estado actual
        AudioSource audioActual = null;
        if (estadoActual == EstadoRespiracion.Suave) audioActual = sourceSuave;
        if (estadoActual == EstadoRespiracion.Acelerada) audioActual = sourceAcelerada;

        // Si el jugador superó un umbral y necesita cambiar de respiración...
        if (estadoActual != estadoObjetivo)
        {
            // Primero apagamos progresivamente el audio que estaba sonando
            if (audioActual != null && audioActual.volume > 0f)
            {
                audioActual.volume = Mathf.MoveTowards(audioActual.volume, 0f, velocidadFade * Time.deltaTime);
            }
            else
            {
                // Una vez el volumen es 0 (silencio total), autorizamos el cambio al nuevo estado
                estadoActual = estadoObjetivo;
            }
        }
        else
        {
            // Si ya estamos en el estado correcto, simplemente subimos el volumen del audio activo
            if (audioActual != null && audioActual.volume < volumenMaximo)
            {
                audioActual.volume = Mathf.MoveTowards(audioActual.volume, volumenMaximo, velocidadFade * Time.deltaTime);
            }
        }

        // Medida de seguridad: Forzar a 0 cualquier audio que no sea el estadoActual
        if (estadoActual != EstadoRespiracion.Suave && sourceSuave.volume > 0f)
        {
            sourceSuave.volume = Mathf.MoveTowards(sourceSuave.volume, 0f, velocidadFade * Time.deltaTime);
        }
        if (estadoActual != EstadoRespiracion.Acelerada && sourceAcelerada.volume > 0f)
        {
            sourceAcelerada.volume = Mathf.MoveTowards(sourceAcelerada.volume, 0f, velocidadFade * Time.deltaTime);
        }
    }
}