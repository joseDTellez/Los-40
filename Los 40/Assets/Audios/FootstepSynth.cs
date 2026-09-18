using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FootstepSynth : MonoBehaviour
{
    [Header("Sintesis Aditiva - Parciales")]
    [Tooltip("Frecuencia fundamental base del paso, en Hz")]
    public float baseFrequency = 90f;

    [Tooltip("Relacion de frecuencia de cada parcial respecto a la fundamental. " +
             "Usamos relaciones INARMONICAS (no 1,2,3,4) para que suene a golpe/impacto " +
             "de un cuerpo solido, no a una nota musical afinada")]
    public float[] partialRatios = { 1f, 1.8f, 2.6f, 3.9f, 5.3f };

    [Tooltip("Amplitud inicial de cada parcial (mismo tamano que partialRatios)")]
    public float[] partialAmplitudes = { 1f, 0.6f, 0.35f, 0.2f, 0.1f };

    [Tooltip("Tiempo de decaimiento en segundos de cada parcial. Los parciales agudos " +
             "decaen mas rapido que los graves, igual que en un impacto real")]
    public float[] partialDecayTimes = { 0.18f, 0.09f, 0.06f, 0.04f, 0.03f };

    [Header("Variacion entre pasos")]
    [Range(0f, 0.5f)] public float pitchVariation = 0.12f;      // variacion de tono
    [Range(0f, 0.5f)] public float amplitudeVariation = 0.15f;  // variacion de volumen
    [Range(0f, 0.3f)] public float ratioJitter = 0.05f;         // desafina un poco cada parcial

    [Header("Volumen general")]
    [Range(0f, 2f)] public float masterGain = 0.8f;

    // --- Estado interno (leido/escrito tambien desde el hilo de audio) ---
    private double[] phase;
    private float[] currentRatios;
    private float[] currentAmps;
    private float[] currentDecays;
    private float elapsedTime = 0f;
    private bool active = false;
    private float sampleRate;
    private readonly object lockObj = new object();

    void Awake()
    {
        sampleRate = AudioSettings.outputSampleRate;
        int n = partialRatios.Length;
        phase = new double[n];
        currentRatios = new float[n];
        currentAmps = new float[n];
        currentDecays = new float[n];

        // El AudioSource no necesita ningun clip: Unity llama a OnAudioFilterRead
        // automaticamente mientras el componente este activo en la escena.
        var src = GetComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f; // pon 1 si quieres que el paso sea 3D/posicional
    }

    /// <summary>
    /// Llama a este metodo cada vez que el personaje da un paso.
    /// surfaceGain y surfacePitch permiten adaptar el sonido segun la superficie
    /// (ej: pasto = mas grave y suave, metal = mas agudo y fuerte).
    /// </summary>
    public void TriggerStep(float surfaceGain = 1f, float surfacePitch = 1f)
    {
        lock (lockObj)
        {
            int n = partialRatios.Length;
            float pitchMul = surfacePitch * (1f + Random.Range(-pitchVariation, pitchVariation));
            float ampMul = surfaceGain * (1f + Random.Range(-amplitudeVariation, amplitudeVariation));

            for (int i = 0; i < n; i++)
            {
                float jitter = 1f + Random.Range(-ratioJitter, ratioJitter);
                currentRatios[i] = partialRatios[i] * jitter * pitchMul;
                currentAmps[i] = partialAmplitudes[i] * ampMul;
                currentDecays[i] = partialDecayTimes[i];
            }
            elapsedTime = 0f;
            active = true;
        }
    }

    // Aqui se genera el audio, muestra por muestra, sumando los parciales.
    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!active) return;

        lock (lockObj)
        {
            int n = currentRatios.Length;
            float dt = 1f / sampleRate;

            for (int i = 0; i < data.Length; i += channels)
            {
                float sample = 0f;

                // --- Suma de osciladores (esto es la sintesis aditiva) ---
                for (int p = 0; p < n; p++)
                {
                    float freq = baseFrequency * currentRatios[p];

                    // Envolvente exponencial: cada parcial se apaga a su propio ritmo
                    float env = currentAmps[p] * Mathf.Exp(-elapsedTime / Mathf.Max(currentDecays[p], 0.001f));

                    phase[p] += 2.0 * Mathf.PI * freq * dt;
                    if (phase[p] > 2.0 * Mathf.PI) phase[p] -= 2.0 * Mathf.PI;

                    sample += env * Mathf.Sin((float)phase[p]);
                }

                sample *= masterGain;
                elapsedTime += dt;

                // Sumamos (no reemplazamos) el buffer, por si hay otro sonido en el mismo canal
                for (int c = 0; c < channels; c++)
                {
                    data[i + c] += sample;
                }
            }

            // Apagamos el proceso cuando ya no aporta nada audible (ahorra CPU)
            if (elapsedTime > 1.0f) active = false;
        }
    }
}