using System;
using UnityEngine;

// Generador de lluvia para wavetables: port del algoritmo de RainSynthesizer (lluvia.txt).
//   1. Ruido blanco.
//   2. Filtro pasa-bajos de un polo (filterSpeed).
//   3. Capa aditiva: dropDensity senos (resonancias de las gotas) cuyas frecuencias saltan al azar
//      con probabilidad modulationProbability por muestra (modulación estocástica).
//   4. Mezcla con noiseWeight y additiveWeight.
// Diferencias con el original: aquí se renderiza a un buffer (para usarlo como wavetable) en lugar
// de sonar en tiempo real, y el azar usa una semilla para que cada tabla sea distinta y repetible.
// El volumen general y el fundido del trigger los aplican ReproductorWavetable y AreaClima.
public class GeneradorLluvia
{
    [Serializable]
    public struct Parametros
    {
        [Header("Parámetros de Gotas (Síntesis Aditiva)")]
        [Range(1, 20)] public int dropDensity;
        [Range(200f, 2000f)] public float baseFrequency;
        [Range(0.01f, 0.2f)] public float dropletAmplitude;
        [Range(0.0001f, 0.01f)] public float modulationProbability;

        [Header("Rangos de Frecuencia (Desplazamiento)")]
        public float startFreqOffsetMin, startFreqOffsetMax;
        public float modFreqOffsetMin, modFreqOffsetMax;

        [Header("Filtro y Mezcla")]
        [Range(0.001f, 0.2f)] public float filterSpeed;
        [Range(0f, 1f)] public float noiseWeight;
        [Range(0f, 1f)] public float additiveWeight;

        // Lluvia suave: pocas resonancias, ruido más apagado y más peso de las gotas
        public static Parametros Suave() => new Parametros
        {
            dropDensity = 6, baseFrequency = 1000f, dropletAmplitude = 0.03f, modulationProbability = 0.002f,
            startFreqOffsetMin = -400f, startFreqOffsetMax = 600f, modFreqOffsetMin = -500f, modFreqOffsetMax = 800f,
            filterSpeed = 0.12f, noiseWeight = 0.6f, additiveWeight = 0.25f
        };

        // Lluvia fuerte: muchas resonancias que cambian a menudo y un ruido más brillante y dominante
        public static Parametros Fuerte() => new Parametros
        {
            dropDensity = 16, baseFrequency = 800f, dropletAmplitude = 0.04f, modulationProbability = 0.005f,
            startFreqOffsetMin = -400f, startFreqOffsetMax = 600f, modFreqOffsetMin = -500f, modFreqOffsetMax = 800f,
            filterSpeed = 0.2f, noiseWeight = 0.9f, additiveWeight = 0.2f
        };
    }

    private readonly Parametros p;
    private readonly float sampleRate;
    private readonly System.Random rng;
    private readonly float[] phase;
    private readonly float[] frequencies;
    private float currentFilterState;

    public GeneradorLluvia(Parametros parametros, int frecuenciaMuestreo, int semilla)
    {
        p = parametros;
        sampleRate = frecuenciaMuestreo;
        rng = new System.Random(semilla);

        // ReinitializeBuffers del original
        int densidad = Mathf.Max(1, p.dropDensity);
        phase = new float[densidad];
        frequencies = new float[densidad];
        for (int i = 0; i < densidad; i++)
        {
            frequencies[i] = p.baseFrequency + Rango(p.startFreqOffsetMin, p.startFreqOffsetMax);
            phase[i] = Rango(0f, Mathf.PI * 2);
        }
    }

    public float Siguiente()
    {
        // 1. Ruido blanco base
        float whiteNoise = (float)(rng.NextDouble() * 2.0 - 1.0);

        // 2. Filtro pasa-bajos
        currentFilterState = currentFilterState + p.filterSpeed * (whiteNoise - currentFilterState);
        float additiveLayer = 0f;

        // 3. Capa aditiva para resonancias
        for (int j = 0; j < phase.Length; j++)
        {
            phase[j] += 2f * Mathf.PI * frequencies[j] / sampleRate;
            if (phase[j] > Mathf.PI * 2) phase[j] -= Mathf.PI * 2;

            additiveLayer += Mathf.Sin(phase[j]) * p.dropletAmplitude;

            // Modulación estocástica
            if (rng.NextDouble() < p.modulationProbability)
            {
                float range = p.modFreqOffsetMax - p.modFreqOffsetMin;
                float randomOffset = (float)(rng.NextDouble() * range + p.modFreqOffsetMin);
                frequencies[j] = p.baseFrequency + randomOffset;
            }
        }

        // 4. Mezcla final con pesos configurables
        return (currentFilterState * p.noiseWeight) + (additiveLayer * p.additiveWeight);
    }

    private float Rango(float min, float max) => min + (max - min) * (float)rng.NextDouble();

    public static float[] Renderizar(Parametros p, int frecuenciaMuestreo, float segundos, int semilla)
    {
        var generador = new GeneradorLluvia(p, frecuenciaMuestreo, semilla);
        var salida = new float[Mathf.Max(1, (int)(segundos * frecuenciaMuestreo))];
        for (int i = 0; i < salida.Length; i++) salida[i] = generador.Siguiente();
        return salida;
    }
}
