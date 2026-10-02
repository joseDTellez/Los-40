using System.Threading.Tasks;
using UnityEngine;

// Sistema de lluvia: sonido (algoritmo de RainSynthesizer -> wavetables -> ReproductorWavetable) y gotas visibles.
// La intensidad, la presencia (si el jugador está en el área) y el viento los fija AreaClima.
[RequireComponent(typeof(ReproductorWavetable))]
public class SistemaLluvia : MonoBehaviour
{
    [Header("Control (lo maneja AreaClima)")]
    [Range(0f, 1f)] public float intensidad;
    [Range(0f, 1f)] public float presencia = 1f;
    [Tooltip("Velocidad del viento en m/s; inclina las gotas")]
    public Vector3 viento;

    [Header("Sonido (RainSynthesizer -> wavetables)")]
    [Range(0f, 1f)] public float volumen = 0.8f;
    public GeneradorLluvia.Parametros lluviaSuave = GeneradorLluvia.Parametros.Suave();
    public GeneradorLluvia.Parametros lluviaFuerte = GeneradorLluvia.Parametros.Fuerte();
    [Tooltip("Tablas distintas por capa: más tablas, menos repetición")]
    [Range(1, 6)] public int tablasPorCapa = 3;
    public float segundosPorTabla = 5f;

    [Header("Gotas visibles")]
    [Tooltip("Material de partículas (URP Particles/Unlit, transparente)")]
    public Material materialGota;
    public int gotasPorSegundoMax = 4000;
    public float alturaEmisor = 14f;
    public float radioLluvia = 20f;
    public float velocidadCaida = 18f;

    // Volumen de cada capa una vez normalizada (la fuerte suena más)
    private const float RmsSuave = 0.012f, RmsFuerte = 0.03f;

    private ReproductorWavetable reproductor;
    private Task<float[][][]> generacion;
    private ParticleSystem gotas;
    private Transform camara;

    void Awake()
    {
        reproductor = GetComponent<ReproductorWavetable>();
    }

    void Start()
    {
        Regenerar();
        CrearGotas();
    }

    // Vuelve a sintetizar las tablas con los parámetros actuales (útil al ajustar en Play)
    [ContextMenu("Regenerar sonidos")]
    public void Regenerar()
    {
        int sr = AudioSettings.outputSampleRate;
        var suave = lluviaSuave;
        var fuerte = lluviaFuerte;
        int cantidad = tablasPorCapa;
        float segundos = segundosPorTabla;

        // La síntesis es matemática pura: se hace en un hilo de fondo para no congelar el juego
        generacion = Task.Run(() => new[]
        {
            Generar(suave, sr, segundos, cantidad, RmsSuave, 100),
            Generar(fuerte, sr, segundos, cantidad, RmsFuerte, 200)
        });
    }

    public static float[][] Generar(GeneradorLluvia.Parametros p, int sr, float segundos, int cantidad, float rms, int semilla)
    {
        var tablas = new float[cantidad][];
        for (int i = 0; i < cantidad; i++)
        {
            float[] toma = GeneradorLluvia.Renderizar(p, sr, segundos + 0.5f, semilla + i);
            float[] bucle = Wavetable.HacerBucle(toma, sr, 0.5f);
            Wavetable.NormalizarRms(bucle, rms);
            tablas[i] = bucle;
        }
        return tablas;
    }

    void Update()
    {
        if (generacion != null && generacion.IsCompleted)
        {
            if (generacion.IsFaulted) Debug.LogException(generacion.Exception, this);
            else reproductor.CargarTablas(generacion.Result[0], generacion.Result[1]);
            generacion = null;
        }

        reproductor.Intensidad = intensidad;
        reproductor.Ganancia = volumen * presencia;
        ActualizarGotas();
    }

    private void CrearGotas()
    {
        if (materialGota == null) return;

        var objeto = new GameObject("Gotas de lluvia");
        objeto.transform.SetParent(transform, false);
        gotas = objeto.AddComponent<ParticleSystem>();
        gotas.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = gotas.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = (alturaEmisor + 4f) / velocidadCaida;
        main.startSpeed = 0f;
        main.startSize = 0.02f;
        main.startColor = new Color(0.78f, 0.82f, 0.9f, 0.5f);
        main.maxParticles = Mathf.CeilToInt(gotasPorSegundoMax * main.startLifetime.constant * 1.2f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emision = gotas.emission;
        emision.rateOverTime = 0f;

        var forma = gotas.shape;
        forma.shapeType = ParticleSystemShapeType.Box;
        forma.scale = new Vector3(radioLluvia * 2f, 0.1f, radioLluvia * 2f);

        var velocidad = gotas.velocityOverLifetime;
        velocidad.enabled = true;
        velocidad.space = ParticleSystemSimulationSpace.World;
        velocidad.x = 0f;
        velocidad.y = -velocidadCaida;
        velocidad.z = 0f;

        // Gotas como trazos estirados en la dirección de caída
        var render = objeto.GetComponent<ParticleSystemRenderer>();
        render.renderMode = ParticleSystemRenderMode.Stretch;
        render.velocityScale = 0.04f;
        render.lengthScale = 1f;
        render.material = materialGota;
        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        render.receiveShadows = false;

        gotas.Play();
    }

    private void ActualizarGotas()
    {
        if (gotas == null) return;
        if (camara == null)
        {
            if (Camera.main == null) return;
            camara = Camera.main.transform;
        }

        // El emisor sigue al jugador, adelantado contra el viento para que las gotas inclinadas le caigan encima
        float tiempoCaida = alturaEmisor / velocidadCaida;
        gotas.transform.SetPositionAndRotation(camara.position + Vector3.up * alturaEmisor - viento * tiempoCaida, Quaternion.identity);

        // Curva cuadrática: la lluvia suave lleva pocas gotas y la fuerte muchas
        var emision = gotas.emission;
        emision.rateOverTime = gotasPorSegundoMax * intensidad * intensidad * presencia;

        var velocidad = gotas.velocityOverLifetime;
        velocidad.x = viento.x;
        velocidad.z = viento.z;
    }
}
