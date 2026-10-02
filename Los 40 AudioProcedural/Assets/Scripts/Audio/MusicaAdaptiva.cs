using System;
using UnityEngine;
using UnityEngine.Audio;
using DialogueEditor;

// Música de fondo adaptativa de la escena principal.
//  - Cada etapa del juego (según las interacciones clave de GameManagerNew) tiene su pista; al
//    cambiar de etapa se hace un fundido cruzado de igual potencia entre dos fuentes.
//  - Las pistas terminan con varios segundos de silencio: en lugar de dejar ese hueco al repetir,
//    la pista vuelve a empezar en la otra fuente antes de la cola y se cruza con la que termina.
//  - Baja la música (ducking) durante los diálogos y con la radio encendida cerca.
//  - Se desvanece junto con el fundido a negro al cambiar de escena.
public class MusicaAdaptiva : MonoBehaviour
{
    [Serializable]
    public class Etapa
    {
        [Tooltip("Interacciones clave (GameManagerNew) a partir de las que suena esta pista")]
        public int desdeInteracciones;
        public AudioClip pista;
        [Tooltip("Segundos de silencio al inicio del archivo que se saltan")]
        public float silencioInicial;
        [Tooltip("Segundos de silencio al final del archivo que no se usan al repetir la pista")]
        public float silencioFinal;
        [Range(0f, 1f)] public float volumen = 1f;
    }

    [Header("Pistas por etapa")]
    public Etapa[] etapas = new Etapa[0];

    [Header("Mezcla")]
    [Tooltip("Grupo del mixer (Musica), para que responda al slider de volumen de música")]
    public AudioMixerGroup grupoMixer;
    [Range(0f, 1f)] public float volumenGeneral = 0.25f;
    [Tooltip("Segundos del fundido cruzado al cambiar de pista o al repetirla")]
    public float fundido = 4f;

    [Header("Atenuación (ducking)")]
    [Tooltip("Decibeles que baja la música durante un diálogo")]
    public float duckingDialogoDb = -6f;
    [Tooltip("Decibeles que baja la música con la radio encendida cerca, para que no choquen dos músicas")]
    public float duckingRadioDb = -15f;
    [Tooltip("Hasta esta distancia de la radio la atenuación es completa; desaparece al doble")]
    public float distanciaRadio = 8f;
    [Tooltip("Segundos que tarda en bajar o recuperar el volumen")]
    public float tiempoDucking = 0.6f;

    private readonly AudioSource[] fuentes = new AudioSource[2];
    private readonly Etapa[] etapaDeFuente = new Etapa[2];
    private readonly float[] nivel = new float[2];     // posición del fundido de cada fuente (0..1)
    private readonly float[] objetivo = new float[2];
    private int activa = -1;
    private Etapa etapaActual;
    private float duckingActualDb;
    private float salida = 1f;                         // baja a 0 al cambiar de escena
    private float velocidadSalida;
    private RadioController radio;
    private Transform oyente;

    void Awake()
    {
        for (int i = 0; i < fuentes.Length; i++)
        {
            var fuente = gameObject.AddComponent<AudioSource>();
            fuente.playOnAwake = false;
            fuente.loop = false;          // la repetición la maneja este script, sin el silencio final
            fuente.spatialBlend = 0f;
            fuente.priority = 0;          // la música nunca debe quedar sin voz de audio
            fuente.volume = 0f;
            fuente.outputAudioMixerGroup = grupoMixer;
            fuentes[i] = fuente;
        }
    }

    void OnEnable() => SceneFadeTransition.AlSalirDeEscena += Desvanecer;
    void OnDisable() => SceneFadeTransition.AlSalirDeEscena -= Desvanecer;

    void Start()
    {
        radio = FindAnyObjectByType<RadioController>();
        if (Camera.main != null) oyente = Camera.main.transform;
    }

    void Update()
    {
        Etapa deseada = EtapaSegunProgreso();
        if (deseada != null && deseada != etapaActual)
            Reproducir(deseada);

        // Repetición sin hueco: antes de la cola en silencio, la misma pista arranca en la otra fuente
        if (activa >= 0 && etapaActual != null && fuentes[activa].isPlaying)
        {
            float finUtil = etapaActual.pista.length - etapaActual.silencioFinal;
            if (fuentes[activa].time >= Mathf.Max(finUtil - fundido, etapaActual.silencioInicial + 1f))
                Reproducir(etapaActual);
        }

        float dt = Time.unscaledDeltaTime;
        ActualizarDucking(dt);
        if (velocidadSalida > 0f)
            salida = Mathf.MoveTowards(salida, 0f, velocidadSalida * dt);

        float ganancia = volumenGeneral * Mathf.Pow(10f, duckingActualDb / 20f) * salida;
        for (int i = 0; i < fuentes.Length; i++)
        {
            if (fuentes[i].clip == null) continue;

            nivel[i] = Mathf.MoveTowards(nivel[i], objetivo[i], dt / Mathf.Max(fundido, 0.01f));
            // Curva de igual potencia: el cruce entre pistas no tiene un "bajón" a mitad del fundido
            float curva = Mathf.Sin(nivel[i] * Mathf.PI * 0.5f);
            fuentes[i].volume = ganancia * curva * (etapaDeFuente[i] != null ? etapaDeFuente[i].volumen : 1f);

            if (nivel[i] <= 0f && objetivo[i] <= 0f && fuentes[i].isPlaying)
                fuentes[i].Stop();
        }
    }

    Etapa EtapaSegunProgreso()
    {
        int progreso = GameManagerNew.Instance != null ? GameManagerNew.Instance.interaccionesClave : 0;

        Etapa elegida = null;
        foreach (Etapa e in etapas)
        {
            if (e == null || e.pista == null || progreso < e.desdeInteracciones) continue;
            if (elegida == null || e.desdeInteracciones >= elegida.desdeInteracciones) elegida = e;
        }
        return elegida;
    }

    void Reproducir(Etapa etapa)
    {
        int nueva = activa < 0 ? 0 : 1 - activa;
        AudioSource fuente = fuentes[nueva];

        fuente.Stop();
        fuente.clip = etapa.pista;
        fuente.time = Mathf.Clamp(etapa.silencioInicial, 0f, Mathf.Max(0f, etapa.pista.length - 0.1f));
        fuente.Play();

        etapaDeFuente[nueva] = etapa;
        nivel[nueva] = 0f;
        objetivo[nueva] = 1f;
        if (activa >= 0) objetivo[activa] = 0f; // la anterior se desvanece mientras entra la nueva

        activa = nueva;
        etapaActual = etapa;
    }

    void ActualizarDucking(float dt)
    {
        float objetivoDb = 0f;

        if (ConversationManager.Instance != null && ConversationManager.Instance.IsConversationActive)
            objetivoDb += duckingDialogoDb;

        if (radio != null && radio.EstaEncendida)
        {
            float distancia = oyente != null ? Vector3.Distance(oyente.position, radio.transform.position) : 0f;
            objetivoDb += duckingRadioDb * (1f - Mathf.InverseLerp(distanciaRadio, distanciaRadio * 2f, distancia));
        }

        // Velocidad en dB/s para recorrer todo el rango de atenuación en tiempoDucking
        float rango = Mathf.Max(1f, Mathf.Abs(duckingDialogoDb) + Mathf.Abs(duckingRadioDb));
        duckingActualDb = Mathf.MoveTowards(duckingActualDb, objetivoDb, rango / Mathf.Max(tiempoDucking, 0.01f) * dt);
    }

    void Desvanecer(float segundos)
    {
        velocidadSalida = 1f / Mathf.Max(segundos, 0.01f);
    }
}
