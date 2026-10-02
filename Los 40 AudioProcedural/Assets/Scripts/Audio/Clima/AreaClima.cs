using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum EstadoClima { Calma, VientoSuave, VientoFuerte, LluviaSuave, LluviaFuerte, Tormenta }

// Área de clima: un trigger dentro del cual el clima cambia solo entre seis estados.
//  - Cada estado define la intensidad objetivo del viento (ProceduralWind) y de la lluvia
//    (SistemaLluvia); el paso de un estado a otro es gradual (SmoothDamp).
//  - El ciclo automático solo salta a estados "vecinos" (de calma no se pasa a tormenta de golpe)
//    y cada estado dura un tiempo al azar dentro de su rango.
//  - Dentro de un estado los valores varían lentamente (ruido Perlin) para que nunca quede estático.
//  - Al entrar o salir del área el sonido y las gotas aparecen o se desvanecen.
[RequireComponent(typeof(BoxCollider))]
public class AreaClima : MonoBehaviour
{
    [Serializable]
    public class PerfilClima
    {
        public EstadoClima estado;
        [Range(0f, 1f)] public float viento;
        [Range(0f, 1f)] public float lluvia;
        [Tooltip("Segundos que dura este estado en el ciclo automático (mínimo y máximo)")]
        public Vector2 duracion = new Vector2(45f, 90f);

        public PerfilClima() { }

        public PerfilClima(EstadoClima estado, float viento, float lluvia, float minimo, float maximo)
        {
            this.estado = estado;
            this.viento = viento;
            this.lluvia = lluvia;
            duracion = new Vector2(minimo, maximo);
        }
    }

    [Header("Elementos controlados")]
    public SistemaLluvia lluvia;
    public ProceduralWind viento;

    [Header("Ciclo del clima")]
    public EstadoClima estadoInicial = EstadoClima.Calma;
    public bool cicloAutomatico = true;
    [Tooltip("Segundos aproximados que tarda en pasar de un estado a otro")]
    public float tiempoTransicion = 12f;
    [Tooltip("Variación lenta dentro de cada estado, para que nunca quede estático")]
    [Range(0f, 0.3f)] public float variacionInterna = 0.08f;
    public PerfilClima[] perfiles = PerfilesPorDefecto();

    [Header("Zona")]
    [Tooltip("Segundos del fundido al entrar o salir del área")]
    public float tiempoEntradaZona = 3f;

    [Header("Pruebas (solo Editor y builds de desarrollo)")]
    [Tooltip("F1..F6 fuerzan cada estado y se muestra el clima actual en pantalla")]
    public bool atajosDePrueba = true;

    [Header("Estado actual (solo lectura)")]
    [SerializeField] private EstadoClima estadoActual;
    [SerializeField] private float vientoActual;
    [SerializeField] private float lluviaActual;
    [SerializeField] private float presenciaActual;

    // Desde cada estado solo se puede ir a estos (en el orden del enum)
    private static readonly EstadoClima[][] Vecinos =
    {
        new[] { EstadoClima.VientoSuave, EstadoClima.LluviaSuave },                         // Calma
        new[] { EstadoClima.Calma, EstadoClima.VientoFuerte, EstadoClima.LluviaSuave },     // VientoSuave
        new[] { EstadoClima.VientoSuave, EstadoClima.Tormenta },                            // VientoFuerte
        new[] { EstadoClima.Calma, EstadoClima.VientoSuave, EstadoClima.LluviaFuerte },     // LluviaSuave
        new[] { EstadoClima.LluviaSuave, EstadoClima.Tormenta },                            // LluviaFuerte
        new[] { EstadoClima.LluviaFuerte, EstadoClima.VientoFuerte },                       // Tormenta
    };

    public EstadoClima EstadoActual => estadoActual;

    private int collidersDentro;
    private float finEstado;
    private float velocidadViento, velocidadLluvia;
    private float semilla;

    public static PerfilClima[] PerfilesPorDefecto() => new[]
    {
        new PerfilClima(EstadoClima.Calma,        0f,    0f,    40f, 80f),
        new PerfilClima(EstadoClima.VientoSuave,  0.4f,  0f,    40f, 80f),
        new PerfilClima(EstadoClima.VientoFuerte, 0.85f, 0f,    30f, 60f),
        new PerfilClima(EstadoClima.LluviaSuave,  0.25f, 0.4f,  40f, 90f),
        new PerfilClima(EstadoClima.LluviaFuerte, 0.35f, 0.85f, 30f, 70f),
        new PerfilClima(EstadoClima.Tormenta,     1f,    1f,    25f, 50f),
    };

    void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
        if (lluvia == null) lluvia = GetComponentInChildren<SistemaLluvia>();
        if (viento == null) viento = GetComponentInChildren<ProceduralWind>();
        semilla = UnityEngine.Random.Range(0f, 100f);
    }

    void Start()
    {
        CambiarEstado(estadoInicial);
        PerfilClima p = Perfil(estadoInicial);
        vientoActual = p.viento;
        lluviaActual = p.lluvia;
    }

    void Update()
    {
        if (cicloAutomatico && Time.time >= finEstado)
            CambiarEstado(SiguienteEstado(estadoActual));

        PerfilClima perfil = Perfil(estadoActual);
        float t = Time.time;
        float objetivoViento = ConVariacion(perfil.viento, Mathf.PerlinNoise(t * 0.05f, semilla));
        float objetivoLluvia = ConVariacion(perfil.lluvia, Mathf.PerlinNoise(semilla, t * 0.04f));

        float suavizado = Mathf.Max(tiempoTransicion / 3f, 0.01f);
        vientoActual = Mathf.SmoothDamp(vientoActual, objetivoViento, ref velocidadViento, suavizado);
        lluviaActual = Mathf.SmoothDamp(lluviaActual, objetivoLluvia, ref velocidadLluvia, suavizado);
        presenciaActual = Mathf.MoveTowards(presenciaActual, collidersDentro > 0 ? 1f : 0f,
                                            Time.deltaTime / Mathf.Max(tiempoEntradaZona, 0.01f));

        if (viento != null)
        {
            viento.intensidad = vientoActual;
            viento.presencia = presenciaActual;
        }
        if (lluvia != null)
        {
            lluvia.intensidad = lluviaActual;
            lluvia.presencia = presenciaActual;
            lluvia.viento = viento != null ? viento.Velocidad : Vector3.zero;
        }

        LeerAtajos();
    }

    // Un estado con valor 0 (sin viento o sin lluvia) se queda en 0; los demás oscilan un poco
    private float ConVariacion(float valor, float ruido)
    {
        if (valor <= 0f) return 0f;
        return Mathf.Clamp01(valor + (ruido - 0.5f) * 2f * variacionInterna);
    }

    public void ForzarEstado(EstadoClima estado) => CambiarEstado(estado);

    private void CambiarEstado(EstadoClima estado)
    {
        estadoActual = estado;
        PerfilClima p = Perfil(estado);
        finEstado = Time.time + UnityEngine.Random.Range(p.duracion.x, Mathf.Max(p.duracion.x, p.duracion.y));
    }

    private PerfilClima Perfil(EstadoClima estado)
    {
        foreach (PerfilClima p in perfiles)
            if (p != null && p.estado == estado) return p;
        return new PerfilClima(estado, 0f, 0f, 30f, 60f);
    }

    private static EstadoClima SiguienteEstado(EstadoClima estado)
    {
        EstadoClima[] opciones = Vecinos[(int)estado];
        return opciones[UnityEngine.Random.Range(0, opciones.Length)];
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) collidersDentro++;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) collidersDentro = Mathf.Max(0, collidersDentro - 1);
    }

    // ─── PRUEBAS ─────────────────────────────

    private void LeerAtajos()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Keyboard k = Keyboard.current;
        if (!atajosDePrueba || k == null) return;

        if (k.f1Key.wasPressedThisFrame) ForzarEstado(EstadoClima.Calma);
        if (k.f2Key.wasPressedThisFrame) ForzarEstado(EstadoClima.VientoSuave);
        if (k.f3Key.wasPressedThisFrame) ForzarEstado(EstadoClima.VientoFuerte);
        if (k.f4Key.wasPressedThisFrame) ForzarEstado(EstadoClima.LluviaSuave);
        if (k.f5Key.wasPressedThisFrame) ForzarEstado(EstadoClima.LluviaFuerte);
        if (k.f6Key.wasPressedThisFrame) ForzarEstado(EstadoClima.Tormenta);
#endif
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void OnGUI()
    {
        if (!atajosDePrueba) return;
        string zona = presenciaActual > 0f ? "dentro del área" : "fuera del área";
        GUI.Label(new Rect(10, 10, 600, 22),
            $"Clima: {estadoActual}  |  viento {vientoActual:0.00}  lluvia {lluviaActual:0.00}  |  {zona}  |  F1-F6 cambian el estado");
    }
#endif
}
