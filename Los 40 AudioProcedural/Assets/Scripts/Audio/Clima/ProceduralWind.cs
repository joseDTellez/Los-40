using System.Threading.Tasks;
using UnityEngine;

// Viento procedural: sonido (ProceduralWindSound -> wavetables -> ReproductorWavetable), ráfagas y
// una WindZone para que los árboles y las partículas compatibles se muevan con él.
// La intensidad y la presencia (si el jugador está en el área) las fija AreaClima.
[RequireComponent(typeof(ReproductorWavetable))]
public class ProceduralWind : MonoBehaviour
{
    [Header("Control (lo maneja AreaClima)")]
    [Range(0f, 1f)] public float intensidad;
    [Range(0f, 1f)] public float presencia = 1f;

    [Header("Dirección y fuerza")]
    [Tooltip("Hacia dónde sopla el viento")]
    public Vector3 direccion = new Vector3(1f, 0f, 0.35f);
    [Tooltip("Velocidad en m/s con intensidad 1 (inclina la lluvia)")]
    public float velocidadMaxima = 9f;

    [Header("Ráfagas")]
    [Tooltip("Qué tan seguido llegan las ráfagas, en Hz")]
    public float frecuenciaRafagas = 0.12f;
    [Range(0f, 1f)] public float profundidadRafagas = 0.5f;

    [Header("Sonido (ProceduralWindSound -> wavetables)")]
    [Range(0f, 1f)] public float volumen = 0.7f;
    public GeneradorViento.Parametros vientoSuave = GeneradorViento.Parametros.Suave();
    public GeneradorViento.Parametros vientoFuerte = GeneradorViento.Parametros.Fuerte();
    [Range(1, 6)] public int tablasPorCapa = 3;
    public float segundosPorTabla = 10f;

    [Header("Árboles y partículas")]
    [Tooltip("Crea una WindZone que mueve los árboles del terreno según el viento")]
    public bool usarWindZone = true;

    private const float RmsSuave = 0.01f, RmsFuerte = 0.028f;

    // 0..1, cambia lento; lo usa la lluvia para inclinarse más en cada ráfaga
    public float Rafaga { get; private set; }

    public Vector3 Velocidad
    {
        get
        {
            Vector3 d = direccion.sqrMagnitude > 0.0001f ? direccion.normalized : Vector3.right;
            return d * velocidadMaxima * intensidad * (1f - profundidadRafagas + profundidadRafagas * Rafaga);
        }
    }

    private ReproductorWavetable reproductor;
    private Task<float[][][]> generacion;
    private WindZone zona;
    private float semilla;

    void Awake()
    {
        reproductor = GetComponent<ReproductorWavetable>();
        semilla = Random.Range(0f, 100f);
    }

    void Start()
    {
        Regenerar();

        if (usarWindZone)
        {
            var objeto = new GameObject("WindZone");
            objeto.transform.SetParent(transform, false);
            zona = objeto.AddComponent<WindZone>();
            zona.mode = WindZoneMode.Directional;
        }
    }

    [ContextMenu("Regenerar sonidos")]
    public void Regenerar()
    {
        int sr = AudioSettings.outputSampleRate;
        var suave = vientoSuave;
        var fuerte = vientoFuerte;
        int cantidad = tablasPorCapa;
        float segundos = segundosPorTabla;

        generacion = Task.Run(() => new[]
        {
            Generar(suave, sr, segundos, cantidad, RmsSuave, 300),
            Generar(fuerte, sr, segundos, cantidad, RmsFuerte, 400)
        });
    }

    public static float[][] Generar(GeneradorViento.Parametros p, int sr, float segundos, int cantidad, float rms, int semilla)
    {
        var tablas = new float[cantidad][];
        // Un número entero de ráfagas (ciclos del LFO): al repetirse la tabla, la ráfaga sigue sin salto
        float largo = GeneradorViento.SegundosEnCiclosCompletos(p, segundos);
        for (int i = 0; i < cantidad; i++)
        {
            // Se renderiza 1 s de más: HacerBucle lo funde con el principio y la tabla queda de 'largo' segundos
            float[] toma = GeneradorViento.Renderizar(p, sr, largo + 1f, semilla + i);
            float[] bucle = Wavetable.HacerBucle(toma, sr, 1f);
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

        Rafaga = Mathf.Clamp01(Mathf.PerlinNoise(Time.time * frecuenciaRafagas, semilla));

        // Las ráfagas también empujan el volumen, sincronizadas con la lluvia y los árboles
        float empuje = 1f - profundidadRafagas * 0.6f + profundidadRafagas * 0.6f * Rafaga;
        reproductor.Intensidad = intensidad;
        reproductor.Ganancia = volumen * presencia * empuje;

        if (zona != null)
        {
            if (direccion.sqrMagnitude > 0.0001f)
                zona.transform.rotation = Quaternion.LookRotation(direccion.normalized);
            zona.windMain = intensidad * (0.6f + 0.8f * Rafaga) * 1.5f;
            zona.windTurbulence = 0.2f + intensidad * 0.8f;
            zona.windPulseMagnitude = 0.3f + intensidad;
            zona.windPulseFrequency = 0.05f + intensidad * 0.25f;
        }
    }
}
