using System;
using UnityEngine;

// Generador de viento para wavetables: port del algoritmo de ProceduralWindSound (ProceduralWindSound.txt).
//   1. LFO senoidal (gustSpeed) que produce las ráfagas.
//   2. La frecuencia de corte va de baseFrequency a baseFrequency + gustRange * windIntensity.
//   3. Ruido blanco por dos filtros de un polo en cascada; la diferencia entre las dos etapas
//      deja una banda enfocada (pasa-banda): el silbido del viento.
//   4. Ganancia (1 + windIntensity).
// Diferencias con el original: se renderiza a un buffer (wavetable) en lugar de sonar en tiempo real,
// el azar y la fase inicial del LFO dependen de una semilla (cada tabla es distinta), y el parámetro
// volume no se usa porque cada tabla se normaliza y el volumen lo pone ReproductorWavetable.
public class GeneradorViento
{
    [Serializable]
    public struct Parametros
    {
        [Header("Control de Viento")]
        [Range(0f, 1f)] public float windIntensity;

        [Header("Parámetros Acústicos")]
        [Tooltip("Tono base del viento")]
        [Range(100f, 1000f)] public float baseFrequency;
        [Tooltip("Variación de tono por ráfaga")]
        [Range(50f, 500f)] public float gustRange;
        [Tooltip("Velocidad con la que cambia el viento (Hz del LFO)")]
        [Range(0.1f, 2f)] public float gustSpeed;

        // Brisa: tono bajo, ráfagas lentas y poco profundas
        public static Parametros Suave() => new Parametros
        {
            windIntensity = 0.35f, baseFrequency = 300f, gustRange = 150f, gustSpeed = 0.25f
        };

        // Viento fuerte: más agudo, ráfagas rápidas y amplias
        public static Parametros Fuerte() => new Parametros
        {
            windIntensity = 1f, baseFrequency = 420f, gustRange = 450f, gustSpeed = 0.6f
        };
    }

    private readonly Parametros p;
    private readonly float sampleRate;
    private readonly System.Random systemRandom;
    private float filterState1, filterState2, lfoPhase;

    public GeneradorViento(Parametros parametros, int frecuenciaMuestreo, int semilla)
    {
        p = parametros;
        sampleRate = frecuenciaMuestreo;
        systemRandom = new System.Random(semilla);
        lfoPhase = (float)(systemRandom.NextDouble() * 2.0 * Math.PI);
    }

    public float Siguiente()
    {
        // 1. LFO para crear ráfagas naturales
        lfoPhase += (2.0f * Mathf.PI * p.gustSpeed) / sampleRate;
        if (lfoPhase > 2.0f * Mathf.PI) lfoPhase -= 2.0f * Mathf.PI;

        // Modulación suave de la frecuencia central según el LFO y la intensidad
        float lfoValue = (Mathf.Sin(lfoPhase) + 1.0f) * 0.5f;
        float currentCutoff = p.baseFrequency + (lfoValue * p.gustRange * p.windIntensity);

        // 2. Coeficiente del filtro
        float rc = 1.0f / (2.0f * Mathf.PI * currentCutoff);
        float dt = 1.0f / sampleRate;
        float alpha = dt / (rc + dt);

        // 3. Ruido blanco
        float whiteNoise = (float)(systemRandom.NextDouble() * 2.0 - 1.0);

        // 4. Doble filtro: la diferencia entre etapas crea la banda del silbido
        filterState1 = filterState1 + alpha * (whiteNoise - filterState1);
        filterState2 = filterState2 + alpha * (filterState1 - filterState2);

        return (filterState1 - filterState2) * (1.0f + p.windIntensity);
    }

    // Duración ajustada a un número entero de ciclos del LFO, para que la ráfaga continúe sin salto
    // cuando la tabla vuelve a empezar
    public static float SegundosEnCiclosCompletos(Parametros p, float segundos)
    {
        float velocidad = Mathf.Max(p.gustSpeed, 0.01f);
        return Mathf.Max(1f, Mathf.Round(segundos * velocidad)) / velocidad;
    }

    public static float[] Renderizar(Parametros p, int frecuenciaMuestreo, float segundos, int semilla)
    {
        var generador = new GeneradorViento(p, frecuenciaMuestreo, semilla);
        var salida = new float[Mathf.Max(1, (int)(segundos * frecuenciaMuestreo))];
        for (int i = 0; i < salida.Length; i++) salida[i] = generador.Siguiente();
        return salida;
    }
}
