using UnityEngine;
using System;

[RequireComponent(typeof(AudioSource))]
public class PuertaAudioProcedural : MonoBehaviour
{
    [Header("Chirrido (bisagra) - modulado por velocidad angular")]
    [SerializeField] private double qFiltroBanda = 3.5;
    [Tooltip("Qué tan errático cambia el tono del chirrido (simula el vaivén de una bisagra, no un vibrato limpio). Más chico = cambia más lento, más grande = más nervioso.")]
    [SerializeField] private double suavizadoFrecuenciaChirrido = 0.0008;
    [Tooltip("Repeticiones por segundo del 'tic' de fricción (stick-slip) cuando la puerta se mueve LENTO.")]
    [SerializeField] private double frecuenciaPulsoMin = 40.0;
    [Tooltip("Repeticiones por segundo del 'tic' de fricción (stick-slip) cuando la puerta se mueve RÁPIDO. Más alto = chirrido más 'metralleta', casi tono continuo.")]
    [SerializeField] private double frecuenciaPulsoMax = 200.0;
    [Tooltip("Cuánto ruido de fondo (roce/arrastre) se mezcla entre los impulsos, además del chirrido. 0 = solo 'tics' limpios, valores más altos = más textura de arrastre.")]
    [Range(0f, 1f)][SerializeField] private double intensidadRuidoFondo = 0.15;
    [Tooltip("Velocidad angular (grados/seg) que se considera 'chirrido a máximo volumen'. Ajusta a oído según el Speed de tu DoorController.")]
    [SerializeField] private float velocidadReferenciaGradosSeg = 90f;
    [Tooltip("Qué tan rápido reacciona el volumen del chirrido a cambios de velocidad. Valores más chicos = más suave, más grandes = más inmediato (riesgo de clicks).")]
    [SerializeField] private double suavizadoGanancia = 0.001;

    [Header("Reverb corta (room)")]
    [SerializeField] private double sendReverb = 0.18;
    [Range(0f, 1f)][SerializeField] private double roomSize = 0.20;
    [Range(0f, 1f)][SerializeField] private double damp = 0.70;

    // --- Estado del chirrido (continuo, dirigido por la velocidad angular) ---
    // "volatile" garantiza lectura/escritura atómica de estos dos campos
    // entre el hilo principal (donde escribe DoorController) y el hilo de
    // audio (donde se leen). Es una simplificación deliberada: para un
    // parámetro continuo de modulación un valor levemente desactualizado
    // no se nota.
    private volatile bool estaMoviendose = false;
    private volatile float velocidadAngularActual = 0f;
    private double gananciaSuavizada = 0.0; // solo se toca en el hilo de audio

    private double sampleRate;
    private double faseImpulso = 0.0;

    // Cola de la reverb: muestras que seguimos procesando despues de la ultima senal audible
    private const double SegundosColaReverb = 0.5;
    private int muestrasColaMax;
    private int muestrasCola = 0;

    // Los coeficientes de los filtros se recalculan cada N muestras: la frecuencia central
    // se mueve muy despacio y recalcular seno/coseno en cada muestra era el mayor coste.
    private const int MuestrasPorActualizacionFiltro = 8;
    private int contadorFiltro = MuestrasPorActualizacionFiltro - 1; // configura en la primera muestra

    private System.Random randomChirrido;

    [Header("Parciales inarmónicos (lo que da el carácter 'desafinado' metálico)")]
    [Tooltip("Proporciones de frecuencia de cada 'parcial' metálico. Usa números NO enteros y sin relación simple entre sí (nada de octavas/quintas) para que suene desafinado en vez de armónico/musical.")]
    [SerializeField] private double[] proporcionesInarmonicas = { 1.0, 1.37, 2.63 };
    [Tooltip("Volumen relativo de cada parcial (debe tener el mismo tamaño que proporcionesInarmonicas). Los parciales más agudos normalmente van más bajos.")]
    [SerializeField] private double[] pesosParciales = { 1.0, 0.6, 0.35 };

    private BiquadFilter[] filtrosParciales;
    private double[] estadosRuidoFrecuenciaParciales;
    private BiquadFilter filtroGravesChirrido;

    // --- Estado del reverb (Schroeder simplificado) ---
    private readonly int[] combDelaysMs = { 29, 37, 41, 43 };
    private double[][] combBuffers;
    private int[] combIndices;
    private double[] combFeedback;

    private readonly int[] allpassDelaysMs = { 5, 1 };
    private double[][] allpassBuffers;
    private int[] allpassIndices;
    private const double allpassFeedback = 0.5;

    void Awake()
    {
        sampleRate = AudioSettings.outputSampleRate;
        muestrasColaMax = (int)(sampleRate * SegundosColaReverb);

        randomChirrido = new System.Random(1);

        int nParciales = proporcionesInarmonicas.Length;
        filtrosParciales = new BiquadFilter[nParciales];
        estadosRuidoFrecuenciaParciales = new double[nParciales];
        for (int p = 0; p < nParciales; p++)
        {
            filtrosParciales[p] = new BiquadFilter(sampleRate);
        }

        filtroGravesChirrido = new BiquadFilter(sampleRate);
        filtroGravesChirrido.Configurar(BiquadFilter.Modo.PasaAltos, 80.0, 0.707);

        InicializarReverb();
    }

    void Start()
    {
        var source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.Play();
    }

    /// <summary>
    /// Llamar cada frame (desde Update de DoorController) mientras la
    /// puerta está girando, pasando su velocidad angular actual en
    /// grados/segundo. Seguro de llamar desde el hilo principal.
    /// </summary>
    public void ActualizarMovimiento(float velocidadAngularGradosPorSegundo)
    {
        estaMoviendose = true;
        velocidadAngularActual = velocidadAngularGradosPorSegundo;
    }

    /// <summary>
    /// Llamar una vez cuando la puerta llega a su ángulo final: el chirrido
    /// se desvanece con el suavizado de ganancia, sin ningún golpe al final.
    /// Seguro de llamar desde el hilo principal.
    /// </summary>
    public void DetenerChirrido()
    {
        estaMoviendose = false;
        velocidadAngularActual = 0f;
    }

    // ------------------------------------------------------------------
    // HILO DE AUDIO. No llamar Debug.Log, Transform, ni la mayoría de
    // APIs de Unity aquí dentro.
    // ------------------------------------------------------------------
    void OnAudioFilterRead(float[] data, int channels)
    {
        // Reposo: sin chirrido y con la cola de reverb ya apagada -> silencio sin calcular nada
        if (!estaMoviendose && gananciaSuavizada < 0.0001 && muestrasCola <= 0)
        {
            System.Array.Clear(data, 0, data.Length);
            return;
        }

        int muestrasPorCanal = data.Length / channels;

        for (int i = 0; i < muestrasPorCanal; i++)
        {
            double seco = ProcesarMuestraChirrido();

            if (seco != 0.0) muestrasCola = muestrasColaMax;
            else if (muestrasCola > 0) muestrasCola--;

            double humedo = ProcesarReverb(seco);
            double final = seco + humedo * sendReverb;
            final = Math.Clamp(final, -1.0, 1.0);

            float muestraFloat = (float)final;
            for (int ch = 0; ch < channels; ch++)
            {
                data[i * channels + ch] = muestraFloat;
            }
        }
    }

    // -------------------------------------------------------------
    // CHIRRIDO CONTINUO: ruido blanco -> pasa-banda resonante con
    // centro modulado por LFO -> waveshaper -> recorte de graves.
    // La AMPLITUD depende de la velocidad angular real de la puerta,
    // suavizada para evitar clicks cuando la velocidad cambia brusco
    // de un frame de Update a otro (el audio corre a 44100+ Hz, Update
    // corre a ~60 Hz, así que hay que interpolar entre esos valores).
    // -------------------------------------------------------------
    private double ProcesarMuestraChirrido()
    {
        double velocidadAbs = Math.Abs(velocidadAngularActual);
        double gananciaObjetivo = estaMoviendose
            ? Math.Clamp(velocidadAbs / velocidadReferenciaGradosSeg, 0.0, 1.0)
            : 0.0;

        gananciaSuavizada += (gananciaObjetivo - gananciaSuavizada) * suavizadoGanancia;

        if (gananciaSuavizada < 0.0001)
        {
            return 0.0; // silencio real, no generamos ruido/filtro innecesariamente
        }

        // --- EXCITACIÓN: tren de impulsos tipo "stick-slip" ---
        // En vez de ruido blanco CONTINUO (que suena a arrastre/roce
        // constante), el metal real se atasca y "brinca" repetidamente:
        // cada brinco (impulso) sacude los resonadores de abajo y estos
        // suenan brevemente hasta el siguiente brinco. Eso es lo que
        // convierte una textura de arrastre en una serie de "tics"
        // reconocibles como chirrido de bisagra.
        //
        // La velocidad angular controla qué tan seguido ocurren los
        // "brincos": más rápido el movimiento, más brincos por segundo
        // (más parecido a un tono continuo); más lento, se oyen como
        // tics separados.
        double frecuenciaPulso = frecuenciaPulsoMin +
            Math.Clamp(velocidadAbs / velocidadReferenciaGradosSeg, 0.0, 1.0) * (frecuenciaPulsoMax - frecuenciaPulsoMin);

        faseImpulso += frecuenciaPulso / sampleRate;

        double impulso = 0.0;
        if (faseImpulso >= 1.0)
        {
            faseImpulso -= 1.0;
            // jitter para que el tren de impulsos no sea perfectamente
            // periódico (un tren perfecto suena a zumbido/láser, no a fricción)
            faseImpulso += (randomChirrido.NextDouble() - 0.5) * 0.2;
            impulso = 1.0 + (randomChirrido.NextDouble() - 0.5) * 0.6; // cada "brinco" varía un poco de fuerza
        }

        // Textura de roce de fondo, mucho más tenue que los impulsos:
        // le da algo de "cuerpo" entre tic y tic sin dominar el sonido.
        double ruidoFondo = (randomChirrido.NextDouble() * 2.0 - 1.0) * intensidadRuidoFondo;
        double excitacion = impulso + ruidoFondo;

        // --- Suma de parciales INARMÓNICOS ---
        // Cada parcial vive en una proporción de frecuencia NO entera
        // respecto al primero (proporcionesInarmonicas), y cada uno se
        // mueve de forma errática e independiente. La combinación de
        // frecuencias que no encajan entre sí es lo que produce la
        // disonancia/"desafinación" característica del metal, en vez de
        // un tono limpio y musical. Todos los parciales son excitados
        // por el MISMO tren de impulsos (como un objeto real: un solo
        // "golpe" hace sonar todos sus modos de vibración a la vez).
        bool actualizarFiltros = ++contadorFiltro >= MuestrasPorActualizacionFiltro;
        if (actualizarFiltros) contadorFiltro = 0;

        double filtradoTotal = 0.0;
        for (int p = 0; p < filtrosParciales.Length; p++)
        {
            double muestraRuidoFrec = randomChirrido.NextDouble() * 2.0 - 1.0;
            estadosRuidoFrecuenciaParciales[p] += (muestraRuidoFrec - estadosRuidoFrecuenciaParciales[p]) * suavizadoFrecuenciaChirrido;

            if (actualizarFiltros)
            {
                double frecuenciaBaseParcial = 1400.0 * proporcionesInarmonicas[p];
                double frecuenciaCentral = frecuenciaBaseParcial + estadosRuidoFrecuenciaParciales[p] * 250.0;
                frecuenciaCentral = Math.Clamp(frecuenciaCentral, 300.0, 9000.0);

                filtrosParciales[p].Configurar(BiquadFilter.Modo.PasaBanda, frecuenciaCentral, qFiltroBanda);
            }

            double peso = p < pesosParciales.Length ? pesosParciales[p] : 1.0;
            filtradoTotal += filtrosParciales[p].Procesar(excitacion) * peso;
        }

        double aspero = Math.Tanh(filtradoTotal * 1.8);
        double salida = aspero * gananciaSuavizada * 0.5;
        salida = filtroGravesChirrido.Procesar(salida);

        return salida;
    }

    // -------------------------------------------------------------
    // REVERB CORTA (room)
    // -------------------------------------------------------------
    private void InicializarReverb()
    {
        combBuffers = new double[combDelaysMs.Length][];
        combIndices = new int[combDelaysMs.Length];
        combFeedback = new double[combDelaysMs.Length];

        for (int c = 0; c < combDelaysMs.Length; c++)
        {
            int tam = Math.Max(1, (int)(combDelaysMs[c] / 1000.0 * sampleRate));
            combBuffers[c] = new double[tam];
            combFeedback[c] = roomSize * (0.84 - damp * 0.15);
        }

        allpassBuffers = new double[allpassDelaysMs.Length][];
        allpassIndices = new int[allpassDelaysMs.Length];

        for (int a = 0; a < allpassDelaysMs.Length; a++)
        {
            int tam = Math.Max(1, (int)(allpassDelaysMs[a] / 1000.0 * sampleRate));
            allpassBuffers[a] = new double[tam];
        }
    }

    private double ProcesarReverb(double entrada)
    {
        double sumaComb = 0.0;

        for (int c = 0; c < combBuffers.Length; c++)
        {
            var buf = combBuffers[c];
            int idx = combIndices[c];
            double valorRetardado = buf[idx];
            buf[idx] = entrada + valorRetardado * combFeedback[c];
            combIndices[c] = (idx + 1) % buf.Length;
            sumaComb += valorRetardado;
        }

        double senal = sumaComb / combBuffers.Length;

        for (int a = 0; a < allpassBuffers.Length; a++)
        {
            var buf = allpassBuffers[a];
            int idx = allpassIndices[a];
            double bufOut = buf[idx];
            double vIn = senal + bufOut * allpassFeedback;
            buf[idx] = vIn;
            allpassIndices[a] = (idx + 1) % buf.Length;
            senal = bufOut - vIn * allpassFeedback;
        }

        return senal;
    }
}

/// <summary>
/// Filtro Biquad genérico (fórmulas RBJ "Audio EQ Cookbook").
/// </summary>
public class BiquadFilter
{
    public enum Modo { PasaBanda, PasaBajos, PasaAltos }

    private double a1, a2, b0, b1, b2;
    private double x1, x2, y1, y2;
    private readonly double sampleRate;

    public BiquadFilter(double sampleRate)
    {
        this.sampleRate = sampleRate;
    }

    public void Configurar(Modo modo, double frecuenciaHz, double q)
    {
        double omega = 2.0 * Math.PI * frecuenciaHz / sampleRate;
        double sinO = Math.Sin(omega);
        double cosO = Math.Cos(omega);
        double alpha = sinO / (2.0 * q);
        double a0;

        switch (modo)
        {
            case Modo.PasaBanda:
                b0 = alpha; b1 = 0; b2 = -alpha;
                a0 = 1 + alpha; a1 = -2 * cosO; a2 = 1 - alpha;
                break;

            case Modo.PasaBajos:
                b0 = (1 - cosO) / 2; b1 = 1 - cosO; b2 = (1 - cosO) / 2;
                a0 = 1 + alpha; a1 = -2 * cosO; a2 = 1 - alpha;
                break;

            default: // PasaAltos
                b0 = (1 + cosO) / 2; b1 = -(1 + cosO); b2 = (1 + cosO) / 2;
                a0 = 1 + alpha; a1 = -2 * cosO; a2 = 1 - alpha;
                break;
        }

        b0 /= a0; b1 /= a0; b2 /= a0;
        a1 /= a0; a2 /= a0;
    }

    public double Procesar(double muestraEntrada)
    {
        double salida = b0 * muestraEntrada + b1 * x1 + b2 * x2
                                             - a1 * y1 - a2 * y2;
        x2 = x1; x1 = muestraEntrada;
        y2 = y1; y1 = salida;
        return salida;
    }
}