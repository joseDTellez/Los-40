using System.Collections.Generic;
using UnityEngine;

// Pasos del jugador a partir de un audio en bucle (Assets/Audios/Audio Pasos Bucle.wav).
[RequireComponent(typeof(AudioSource))]
public class PasosBucle : MonoBehaviour
{
    [Header("Audio")]
    [Tooltip("Audio de pasos en bucle")]
    public AudioClip clipPasos;
    [Range(0f, 1f)] public float volumen = 0.8f;

    [Header("Suavizado")]
    [Tooltip("Segundos que tarda en llegar al volumen completo al empezar a caminar")]
    public float tiempoEntrada = 0.12f;
    [Tooltip("Segundos que tarda en apagarse al detenerse, para que el paso no se corte en seco")]
    public float tiempoSalida = 0.35f;
    [Tooltip("Duración del fundido cruzado en la unión del bucle, en milisegundos")]
    [Range(5f, 200f)] public float fundidoBucleMs = 40f;

    [Header("Tono")]
    [Tooltip("Tono al empezar a moverse (velocidad baja)")]
    public float pitchLento = 0.9f;
    [Tooltip("Tono a la velocidad normal de caminata")]
    public float pitchNormal = 1f;
    [Tooltip("Variación aleatoria de tono en cada vuelta del bucle, para que no suene repetitivo")]
    [Range(0f, 0.1f)] public float variacionPitch = 0.03f;

    [Header("Madera (puente)")]
    [Tooltip("Segundos del fundido al pasar de una superficie a otra")]
    public float tiempoCambioSuperficie = 0.15f;
    [Tooltip("Frecuencia del modo principal del tablón, en Hz")]
    public float frecuenciaMadera = 260f;
    [Tooltip("Cuánto del paso original (filtrado) se conserva bajo el golpe de madera")]
    [Range(0f, 1f)] public float mezclaOriginalMadera = 0.55f;
    [Tooltip("Volumen de los pasos sobre madera respecto a los normales")]
    [Range(0f, 2f)] public float volumenMadera = 1f;

    static readonly float[] ProporcionModos = { 0.6f, 1f, 2.3f, 3.9f, 6.4f, 10.7f };
    static readonly float[] DecaimientoModos = { 0.08f, 0.06f, 0.04f, 0.03f, 0.02f, 0.012f };
    static readonly float[] GananciaModos = { 0.25f, 1f, 0.9f, 0.75f, 0.55f, 0.35f };

    private AudioSource fuenteNormal;
    private AudioSource fuenteMadera;
    private AudioClip clipNormalGenerado;
    private AudioClip clipMaderaGenerado;

    private float velocidadObjetivo;   // Ahora puede superar 1.0 cuando corre
    private float intensidad;
    private float objetivoMadera;
    private float mezclaMadera;
    private float variacionActual;
    private int muestraAnterior;
    private bool sonando;
    private bool pausado;

    void Awake()
    {
        fuenteNormal = GetComponent<AudioSource>();
        fuenteNormal.Stop();
        Configurar(fuenteNormal);

        if (clipPasos == null)
            return;

        fuenteNormal.clip = clipPasos;
        if (!PrepararClips(clipPasos))
            return;

        fuenteNormal.clip = clipNormalGenerado;
        fuenteMadera = gameObject.AddComponent<AudioSource>();
        fuenteMadera.outputAudioMixerGroup = fuenteNormal.outputAudioMixerGroup;
        Configurar(fuenteMadera);
        fuenteMadera.clip = clipMaderaGenerado;
    }

    static void Configurar(AudioSource fuente)
    {
        fuente.playOnAwake = false;
        fuente.loop = true;
        fuente.spatialBlend = 0f;
        fuente.dopplerLevel = 0f;
        fuente.volume = 0f;
    }

    // MODIFICADO: Cambiamos Clamp01 por Max para permitir valores > 1 al correr
    public void ActualizarMovimiento(float velocidadNormalizada)
    {
        velocidadObjetivo = Mathf.Max(0f, velocidadNormalizada);
    }

    public void ActualizarMovimiento(float velocidadNormalizada, TipoSuperficie superficie)
    {
        velocidadObjetivo = Mathf.Max(0f, velocidadNormalizada);
        objetivoMadera = superficie == TipoSuperficie.Madera ? 1f : 0f;
    }

    void Update()
    {
        if (fuenteNormal.clip == null) return;

        float tiempo = velocidadObjetivo > intensidad ? tiempoEntrada : tiempoSalida;
        intensidad = Mathf.MoveTowards(intensidad, velocidadObjetivo, Time.deltaTime / Mathf.Max(tiempo, 0.01f));
        mezclaMadera = Mathf.MoveTowards(mezclaMadera, objetivoMadera, Time.deltaTime / Mathf.Max(tiempoCambioSuperficie, 0.01f));

        if (intensidad <= 0.001f)
        {
            Pausar();
            return;
        }
        Reanudar();

        int muestra = fuenteNormal.timeSamples;
        if (muestra < muestraAnterior)
            variacionActual = Random.Range(-variacionPitch, variacionPitch);
        muestraAnterior = muestra;

        // MODIFICADO: Cálculo de Pitch que soporta correr
        // Si caminamos (<=1), interpolamos suavemente desde lento a normal.
        // Si corremos (>1), multiplicamos el pitch para sincronizar la velocidad del audio con los pasos reales.
        float pitchBase = intensidad <= 1f
            ? Mathf.Lerp(pitchLento, pitchNormal, intensidad)
            : pitchNormal * intensidad;

        // El AudioSource de Unity permite un pitch máximo de 3.0, lo limitamos por seguridad.
        float pitch = Mathf.Clamp(pitchBase + variacionActual, 0.1f, 3f);

        // MODIFICADO: Curva de volumen protegida
        // Limitamos el valor a 1.0 exclusivamente para el cálculo del volumen, 
        // así la matemática del smoothstep no se rompe cuando corremos.
        float intVol = Mathf.Clamp01(intensidad);
        float v = volumen * intVol * intVol * (3f - 2f * intVol);

        if (fuenteMadera == null)
        {
            fuenteNormal.volume = v;
            fuenteNormal.pitch = pitch;
            return;
        }

        float angulo = mezclaMadera * Mathf.PI * 0.5f;
        fuenteNormal.volume = v * Mathf.Cos(angulo);
        fuenteMadera.volume = v * Mathf.Sin(angulo) * volumenMadera;
        fuenteNormal.pitch = pitch;
        fuenteMadera.pitch = pitch;
    }

    void Pausar()
    {
        if (!sonando) return;
        fuenteNormal.Pause();
        if (fuenteMadera != null) fuenteMadera.Pause();
        sonando = false;
        pausado = true;
    }

    void Reanudar()
    {
        if (sonando) return;

        if (pausado)
        {
            fuenteNormal.UnPause();
            if (fuenteMadera != null) fuenteMadera.UnPause();
        }
        else
        {
            double inicio = AudioSettings.dspTime + 0.05;
            fuenteNormal.PlayScheduled(inicio);
            if (fuenteMadera != null) fuenteMadera.PlayScheduled(inicio);
        }
        sonando = true;
    }

    void OnDisable()
    {
        intensidad = 0f;
        velocidadObjetivo = 0f;
        if (fuenteNormal != null) fuenteNormal.Stop();
        if (fuenteMadera != null) fuenteMadera.Stop();
        sonando = false;
        pausado = false;
    }

    void OnDestroy()
    {
        if (clipNormalGenerado != null) Destroy(clipNormalGenerado);
        if (clipMaderaGenerado != null) Destroy(clipMaderaGenerado);
    }

    // ─── PREPARACIÓN DE LOS CLIPS (Se mantiene igual) ─────────────────────────────
    bool PrepararClips(AudioClip origen)
    {
        if (!GenerarDatos(out float[] bucle, out float[] madera, out int canales, out int frecuencia)) return false;

        clipNormalGenerado = CrearClip(origen.name + " (bucle suave)", bucle, canales, frecuencia);
        clipMaderaGenerado = CrearClip(origen.name + " (madera)", madera, canales, frecuencia);
        return true;
    }

    public bool GenerarDatos(out float[] bucleNormal, out float[] bucleMadera, out int canales, out int frecuencia)
    {
        bucleNormal = bucleMadera = null;
        canales = frecuencia = 0;
        if (clipPasos == null) return false;

        if (clipPasos.loadState != AudioDataLoadState.Loaded) clipPasos.LoadAudioData();
        canales = clipPasos.channels;
        frecuencia = clipPasos.frequency;

        var datos = new float[clipPasos.samples * canales];
        if (!clipPasos.GetData(datos, 0)) return false;

        bucleNormal = FundirUnion(datos, canales, frecuencia);
        bucleMadera = SintetizarMadera(bucleNormal, canales, frecuencia);
        return true;
    }

    float[] FundirUnion(float[] d, int canales, int frecuencia)
    {
        int n = d.Length / canales;
        int f = Mathf.Clamp(Mathf.RoundToInt(fundidoBucleMs * 0.001f * frecuencia), 1, n / 4);
        int m = n - f;
        var salida = new float[m * canales];

        for (int i = 0; i < m; i++)
        {
            float entra = 1f, sale = 0f;
            if (i < f)
            {
                float t = (float)i / f * Mathf.PI * 0.5f;
                entra = Mathf.Sin(t);
                sale = Mathf.Cos(t);
            }
            for (int c = 0; c < canales; c++)
            {
                float s = d[i * canales + c] * entra;
                if (i < f) s += d[(m + i) * canales + c] * sale;
                salida[i * canales + c] = s;
            }
        }
        return salida;
    }

    float[] SintetizarMadera(float[] d, int canales, int frecuencia)
    {
        int n = d.Length / canales;
        var mono = new float[n];
        for (int i = 0; i < n; i++)
        {
            float s = 0f;
            for (int c = 0; c < canales; c++) s += d[i * canales + c];
            mono[i] = s / canales;
        }
        List<int> pisadas = DetectarPisadas(mono, frecuencia, out List<float> fuerzas);

        var salida = new float[d.Length];
        for (int c = 0; c < canales; c++)
        {
            var pasaBajos = new BiquadFilter(frecuencia);
            pasaBajos.Configurar(BiquadFilter.Modo.PasaBajos, 3500.0, 0.707);
            for (int pasada = 0; pasada < 2; pasada++)
                for (int i = 0; i < n; i++)
                {
                    float y = (float)pasaBajos.Procesar(d[i * canales + c]);
                    if (pasada == 1) salida[i * canales + c] = y * mezclaOriginalMadera;
                }
        }

        var azar = new System.Random(40);
        var golpe = new float[n];
        int largo = Mathf.Min(n, (int)(frecuencia * 0.25f));
        int muestrasAtaque = Mathf.Max(1, (int)(frecuencia * 0.0015f));
        int muestrasChasquido = (int)(frecuencia * 0.005f);

        for (int k = 0; k < pisadas.Count; k++)
        {
            float desafino = 1f + (float)(azar.NextDouble() * 2.0 - 1.0) * 0.06f;
            float fuerza = fuerzas[k] * (1f + (float)(azar.NextDouble() * 2.0 - 1.0) * 0.15f);

            for (int j = 0; j < largo; j++)
            {
                float t = (float)j / frecuencia;
                float s = 0f;
                for (int p = 0; p < ProporcionModos.Length; p++)
                {
                    float f = frecuenciaMadera * ProporcionModos[p] * desafino;
                    s += GananciaModos[p] * Mathf.Exp(-t / DecaimientoModos[p]) * Mathf.Sin(2f * Mathf.PI * f * t);
                }

                if (j < muestrasChasquido)
                    s += (float)(azar.NextDouble() * 2.0 - 1.0) * 0.6f * (1f - (float)j / muestrasChasquido);

                if (j < muestrasAtaque)
                    s *= 0.5f - 0.5f * Mathf.Cos(Mathf.PI * j / muestrasAtaque);

                golpe[(pisadas[k] + j) % n] += s * fuerza;
            }
        }

        int retardo = (int)(frecuencia * 0.011f);
        for (int i = 0; i < n; i++)
        {
            float g = golpe[i] + 0.25f * golpe[(i - retardo + n) % n];
            for (int c = 0; c < canales; c++) salida[i * canales + c] += g;
        }

        float sonoridadNormal = Sonoridad(d, canales, frecuencia), sonoridadMadera = Sonoridad(salida, canales, frecuencia);
        if (sonoridadMadera > 1e-7f)
        {
            float escala = sonoridadNormal / sonoridadMadera;
            for (int i = 0; i < salida.Length; i++) salida[i] *= escala;
        }

        return salida;
    }

    static List<int> DetectarPisadas(float[] mono, int frecuencia, out List<float> fuerzas)
    {
        int ventana = Mathf.Max(1, frecuencia / 200);
        int cuadros = mono.Length / ventana;
        var energia = new float[cuadros];
        float maximo = 0f;
        for (int k = 0; k < cuadros; k++)
        {
            double suma = 0;
            for (int i = k * ventana; i < (k + 1) * ventana; i++) suma += mono[i] * mono[i];
            energia[k] = Mathf.Sqrt((float)(suma / ventana));
            maximo = Mathf.Max(maximo, energia[k]);
        }

        var pisadas = new List<int>();
        fuerzas = new List<float>();
        int separacion = (int)(0.25f * frecuencia / ventana);
        int ultima = -separacion;

        for (int k = 1; k < cuadros - 1; k++)
        {
            if (energia[k] < 0.35f * maximo || energia[k] < energia[k - 1] || energia[k] < energia[k + 1]) continue;
            if (k - ultima < separacion) continue;

            int inicio = k;
            while (inicio > 0 && k - inicio < 6 && energia[inicio - 1] > energia[k] * 0.5f) inicio--;

            pisadas.Add(inicio * ventana);
            fuerzas.Add(energia[k]);
            ultima = k;
        }
        return pisadas;
    }

    static float Sonoridad(float[] datos, int canales, int frecuencia)
    {
        int n = datos.Length / canales;
        var x = new float[n];
        for (int i = 0; i < n; i++)
        {
            float s = 0f;
            for (int c = 0; c < canales; c++) s += datos[i * canales + c];
            x[i] = s / canales;
        }

        foreach (float fc in new[] { 20.6f, 20.6f, 107.7f, 737.9f })
            PrimerOrden(x, fc, frecuencia, pasaAltos: true);
        foreach (float fc in new[] { 12194f, 12194f })
            PrimerOrden(x, fc, frecuencia, pasaAltos: false);

        int ventana = Mathf.Max(1, (int)(0.05f * frecuencia));
        double suma = 0;
        float maximo = 0f;
        for (int i = 0; i < n; i++)
        {
            suma += x[i] * x[i];
            if (i >= ventana) suma -= x[i - ventana] * x[i - ventana];
            if (i >= ventana - 1) maximo = Mathf.Max(maximo, Mathf.Sqrt((float)System.Math.Max(0.0, suma / ventana)));
        }
        return maximo;
    }

    static void PrimerOrden(float[] x, float fc, int frecuencia, bool pasaAltos)
    {
        double k = System.Math.Tan(System.Math.PI * System.Math.Min(fc, frecuencia * 0.49) / frecuencia);
        double a0 = pasaAltos ? 1.0 / (1.0 + k) : k / (1.0 + k);
        double b1 = (1.0 - k) / (1.0 + k);
        double xAnterior = 0, yAnterior = 0;
        for (int i = 0; i < x.Length; i++)
        {
            double entrada = x[i];
            double y = pasaAltos ? a0 * (entrada - xAnterior) + b1 * yAnterior
                                 : a0 * (entrada + xAnterior) + b1 * yAnterior;
            xAnterior = entrada;
            yAnterior = y;
            x[i] = (float)y;
        }
    }

    static AudioClip CrearClip(string nombre, float[] datos, int canales, int frecuencia)
    {
        var clip = AudioClip.Create(nombre, datos.Length / canales, canales, frecuencia, false);
        clip.SetData(datos, 0);
        return clip;
    }
}