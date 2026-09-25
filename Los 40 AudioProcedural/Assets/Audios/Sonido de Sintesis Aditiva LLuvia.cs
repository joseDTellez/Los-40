using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Collider))]
public class RainSynthesizer : MonoBehaviour
{
    [Header("--- Control de Área / Trigger ---")]
    [Tooltip("Si está activado, el audio solo sonará al entrar en el Collider (Trigger)")]
    public bool useTriggerArea = true;
    [Tooltip("Tag del objeto que activa la lluvia")]
    public string playerTag = "Player";
    [Tooltip("Velocidad de transición al entrar/salir del área (Fade In / Fade Out)")]
    [Range(0.1f, 10f)] public float fadeSpeed = 2f;

    [Header("--- Parámetros de Gotas (Síntesis Aditiva) ---")]
    [Range(1, 20)] public int dropDensity = 8;
    [Range(200f, 2000f)] public float baseFrequency = 800f;
    [Tooltip("Volumen individual de la resonancia de cada gota")]
    [Range(0.01f, 0.2f)] public float dropletAmplitude = 0.05f;
    [Tooltip("Probabilidad de variación estocástica por muestra")]
    [Range(0.0001f, 0.01f)] public float modulationProbability = 0.001f;

    [Header("--- Rangos de Frecuencia (Desplazamiento) ---")]
    public float startFreqOffsetMin = -400f;
    public float startFreqOffsetMax = 600f;
    public float modFreqOffsetMin = -500f;
    public float modFreqOffsetMax = 800f;

    [Header("--- Filtro y Mezcla General ---")]
    [Tooltip("Velocidad de respuesta del filtro pasa-bajos del ruido")]
    [Range(0.001f, 0.2f)] public float filterSpeed = 0.05f;
    [Range(0f, 1f)] public float noiseWeight = 0.7f;
    [Range(0f, 1f)] public float additiveWeight = 0.3f;
    [Range(0f, 1f)] public float masterVolume = 0.2f;

    private float sampleRate;
    private float[] phase;
    private float[] frequencies;
    private float currentFilterState = 0f;

    private float currentFadeVolume = 0f;
    private float targetFadeVolume = 0f;

    private System.Random rng = new System.Random();

    void Start()
    {
        sampleRate = AudioSettings.outputSampleRate;
        ReinitializeBuffers();

        if (!useTriggerArea)
        {
            currentFadeVolume = 1f;
            targetFadeVolume = 1f;
        }
    }

    void Update()
    {
        // Transición suave de volumen al entrar/salir del área
        currentFadeVolume = Mathf.MoveTowards(currentFadeVolume, targetFadeVolume, Time.deltaTime * fadeSpeed);
    }

    private void ReinitializeBuffers()
    {
        phase = new float[dropDensity];
        frequencies = new float[dropDensity];

        for (int i = 0; i < dropDensity; i++)
        {
            frequencies[i] = baseFrequency + Random.Range(startFreqOffsetMin, startFreqOffsetMax);
            phase[i] = Random.Range(0f, Mathf.PI * 2);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (useTriggerArea && other.CompareTag(playerTag))
        {
            targetFadeVolume = 1f;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (useTriggerArea && other.CompareTag(playerTag))
        {
            targetFadeVolume = 0f;
        }
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (currentFadeVolume <= 0f && useTriggerArea)
        {
            for (int i = 0; i < data.Length; i++) data[i] = 0f;
            return;
        }

        int activeDensity = (phase != null) ? Mathf.Min(dropDensity, phase.Length) : 0;

        for (int i = 0; i < data.Length; i += channels)
        {
            // 1. Ruido blanco base
            float whiteNoise = (float)(rng.NextDouble() * 2.0 - 1.0);

            // 2. Filtro pasa-bajos
            currentFilterState = currentFilterState + filterSpeed * (whiteNoise - currentFilterState);
            float additiveLayer = 0f;

            // 3. Capa aditiva para resonancias
            for (int j = 0; j < activeDensity; j++)
            {
                phase[j] += 2f * Mathf.PI * frequencies[j] / sampleRate;
                if (phase[j] > Mathf.PI * 2) phase[j] -= Mathf.PI * 2;

                additiveLayer += Mathf.Sin(phase[j]) * dropletAmplitude;

                // Modulación estocástica
                if (rng.NextDouble() < modulationProbability)
                {
                    float range = modFreqOffsetMax - modFreqOffsetMin;
                    float randomOffset = (float)(rng.NextDouble() * range + modFreqOffsetMin);
                    frequencies[j] = baseFrequency + randomOffset;
                }
            }

            // 4. Mezcla final con pesos configurables
            float finalSample = (currentFilterState * noiseWeight) + (additiveLayer * additiveWeight);
            float outputSample = finalSample * masterVolume * currentFadeVolume;

            for (int c = 0; c < channels; c++)
            {
                data[i + c] = outputSample;
            }
        }
    }
}