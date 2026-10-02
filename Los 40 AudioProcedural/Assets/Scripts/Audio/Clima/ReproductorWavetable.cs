using UnityEngine;

// Reproduce sonidos sintetizados tratándolos como wavetables, para que nunca suenen repetitivos:
//  - Dos capas (suave y fuerte), cada una con varias tablas. La intensidad (0..1) decide cuánto
//    suena cada capa con un fundido de igual potencia: así cambia el timbre, no solo el volumen.
//  - En cada capa dos cabezales leen las tablas; cada cierto tiempo (al azar) uno salta a otra
//    tabla y a otra posición y se cruza con el anterior (crossfading aleatorio).
//  - El tono (velocidad de lectura) y el volumen se modulan con valores aleatorios suaves.
//  - El canal derecho lee otra parte de la misma tabla: estéreo ancho sin duplicar memoria.
// El audio se genera en OnAudioFilterRead (hilo de audio); el AudioSource no lleva clip.
[RequireComponent(typeof(AudioSource))]
public class ReproductorWavetable : MonoBehaviour
{
    [Header("Modulación aleatoria")]
    [Tooltip("Variación máxima de tono (0.05 = ±5 %)")]
    [Range(0f, 0.3f)] public float variacionPitch = 0.05f;
    [Tooltip("Qué tan rápido deriva el tono, en Hz")]
    public float velocidadPitch = 0.1f;
    [Tooltip("Profundidad de la modulación aleatoria de volumen")]
    [Range(0f, 1f)] public float variacionVolumen = 0.3f;
    [Tooltip("Qué tan rápido cambia el volumen, en Hz")]
    public float velocidadVolumen = 0.15f;

    [Header("Crossfading aleatorio")]
    [Tooltip("Segundos entre un cruce y el siguiente (mínimo y máximo)")]
    public Vector2 intervaloCruce = new Vector2(4f, 9f);
    [Tooltip("Duración de cada cruce en segundos (mínimo y máximo)")]
    public Vector2 duracionCruce = new Vector2(1.5f, 3.5f);
    [Tooltip("Segundos que tardan en seguirse los cambios de intensidad y de ganancia")]
    public float suavizado = 0.5f;

    // Los fija SistemaLluvia / ProceduralWind desde el hilo principal
    public float Intensidad { set => intensidadObjetivo = Mathf.Clamp01(value); }
    public float Ganancia { set => gananciaObjetivo = Mathf.Max(0f, value); }
    public bool TablasCargadas => banco != null;

    private class Banco { public float[][][] capas; } // [capa][tabla][muestras]

    private class Cabezal
    {
        public int tabla;
        public double posicion;
        public float fase;     // 0 = en silencio, 1 = sonando del todo (fundido de igual potencia)
        public float sentido;  // +1 entrando, -1 saliendo, 0 quieto
        public float ganancia;
    }

    private const int Control = 64; // muestras entre actualizaciones de modulación
    private const float OffsetEstereo = 0.37f;

    private volatile Banco banco;
    private volatile float intensidadObjetivo, gananciaObjetivo;

    // Estado del hilo de audio
    private readonly Cabezal[,] cabezales = new Cabezal[2, 2];
    private readonly int[] cabezalActivo = new int[2];
    private readonly int[] muestrasHastaCruce = new int[2];
    private readonly float[] velocidadFase = new float[2];
    private readonly float[] pesoCapa = new float[2];
    private System.Random azar;
    private ModuladorAleatorio moduladorPitch, moduladorVolumen;
    private float intensidad, ganancia, pitch = 1f, volumenModulado = 1f;
    private int contador;
    private int frecuencia;

    void Awake()
    {
        for (int c = 0; c < 2; c++)
            for (int v = 0; v < 2; v++)
                cabezales[c, v] = new Cabezal();
    }

    void Start()
    {
        frecuencia = AudioSettings.outputSampleRate;
        azar = new System.Random(GetInstanceID());
        moduladorPitch = new ModuladorAleatorio(azar, velocidadPitch, frecuencia);
        moduladorVolumen = new ModuladorAleatorio(azar, velocidadVolumen, frecuencia);

        // Sin clip: reproducirlo en bucle mantiene viva la cadena DSP para OnAudioFilterRead
        var fuente = GetComponent<AudioSource>();
        fuente.clip = null;
        fuente.loop = true;
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f; // el clima rodea al jugador
        fuente.Play();
    }

    public void CargarTablas(float[][] suaves, float[][] fuertes)
    {
        banco = new Banco { capas = new[] { suaves, fuertes } };
    }

    void OnAudioFilterRead(float[] data, int canales)
    {
        Banco b = banco;
        if (b == null || frecuencia == 0) return;

        // En silencio total no se calcula nada
        if (gananciaObjetivo <= 0f && ganancia < 1e-5f)
        {
            ganancia = 0f;
            return;
        }

        int cuadros = data.Length / canales;
        for (int i = 0; i < cuadros; i++)
        {
            if (--contador <= 0) ActualizarControl(b);

            float izquierda = 0f, derecha = 0f;
            for (int c = 0; c < 2; c++)
            {
                float[][] tablas = b.capas[c];
                if (tablas == null || tablas.Length == 0) continue;

                for (int v = 0; v < 2; v++)
                {
                    Cabezal h = cabezales[c, v];
                    if (h.ganancia <= 0f) continue;

                    float[] t = tablas[h.tabla % tablas.Length];
                    izquierda += Wavetable.Leer(t, h.posicion) * h.ganancia;
                    derecha += Wavetable.Leer(t, h.posicion + t.Length * OffsetEstereo) * h.ganancia;

                    h.posicion += pitch;
                    if (h.posicion >= t.Length) h.posicion -= t.Length;
                }
            }

            float k = ganancia * volumenModulado;
            if (canales == 1)
            {
                data[i] += (izquierda + derecha) * 0.5f * k;
            }
            else
            {
                data[i * canales] += izquierda * k;
                data[i * canales + 1] += derecha * k;
            }
        }
    }

    private void ActualizarControl(Banco b)
    {
        contador = Control;
        float dt = (float)Control / frecuencia;
        float seguir = 1f - Mathf.Exp(-dt / Mathf.Max(suavizado, 0.01f));
        intensidad += (intensidadObjetivo - intensidad) * seguir;
        ganancia += (gananciaObjetivo - ganancia) * seguir;

        // Capa suave: entra de 0 a 0.5; capa fuerte: la sustituye de 0.5 a 1 (igual potencia)
        float subida = Mathf.Clamp01(intensidad / 0.5f), cambio = Mathf.Clamp01((intensidad - 0.5f) / 0.5f);
        pesoCapa[0] = Mathf.Sin(subida * Mathf.PI * 0.5f) * Mathf.Cos(cambio * Mathf.PI * 0.5f);
        pesoCapa[1] = Mathf.Sin(cambio * Mathf.PI * 0.5f);

        pitch = 1f + (moduladorPitch.Siguiente(Control) * 2f - 1f) * variacionPitch;
        volumenModulado = 1f - variacionVolumen * moduladorVolumen.Siguiente(Control);

        for (int c = 0; c < 2; c++)
        {
            float[][] tablas = b.capas[c];
            if (tablas == null || tablas.Length == 0) continue;

            muestrasHastaCruce[c] -= Control;
            if (muestrasHastaCruce[c] <= 0) IniciarCruce(c, tablas);

            for (int v = 0; v < 2; v++)
            {
                Cabezal h = cabezales[c, v];
                if (h.sentido != 0f)
                {
                    h.fase = Mathf.Clamp01(h.fase + h.sentido * velocidadFase[c] * Control);
                    if (h.fase <= 0f || h.fase >= 1f) h.sentido = 0f;
                }
                h.ganancia = Mathf.Sin(h.fase * Mathf.PI * 0.5f) * pesoCapa[c];
            }
        }
    }

    private void IniciarCruce(int capa, float[][] tablas)
    {
        int nuevo = 1 - cabezalActivo[capa];
        Cabezal entra = cabezales[capa, nuevo];
        entra.tabla = azar.Next(tablas.Length);
        entra.posicion = azar.NextDouble() * tablas[entra.tabla].Length;
        entra.sentido = 1f;
        cabezales[capa, cabezalActivo[capa]].sentido = -1f;
        cabezalActivo[capa] = nuevo;

        float duracion = Mathf.Lerp(duracionCruce.x, duracionCruce.y, (float)azar.NextDouble());
        velocidadFase[capa] = 1f / (Mathf.Max(duracion, 0.05f) * frecuencia);
        muestrasHastaCruce[capa] = (int)(Mathf.Lerp(intervaloCruce.x, intervaloCruce.y, (float)azar.NextDouble()) * frecuencia);
    }
}
