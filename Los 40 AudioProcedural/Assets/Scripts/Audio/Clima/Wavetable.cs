using UnityEngine;

// Utilidades para tratar audio sintetizado como wavetable (tabla que se lee en bucle).
// Todo es matemática pura: se puede usar desde el hilo de audio o desde un hilo de fondo.
public static class Wavetable
{
    // Convierte una toma en un bucle continuo: los últimos 'segundosCruce' se funden (igual
    // potencia) con los primeros, así el final empalma sin salto con el principio.
    public static float[] HacerBucle(float[] muestras, int frecuencia, float segundosCruce)
    {
        int n = muestras.Length;
        int f = Mathf.Clamp((int)(segundosCruce * frecuencia), 1, n / 3);
        int m = n - f;
        var bucle = new float[m];

        for (int i = 0; i < m; i++)
        {
            float s = muestras[i];
            if (i < f)
            {
                float t = (float)i / f * Mathf.PI * 0.5f;
                s = s * Mathf.Sin(t) + muestras[m + i] * Mathf.Cos(t);
            }
            bucle[i] = s;
        }
        return bucle;
    }

    public static void NormalizarRms(float[] muestras, float rmsObjetivo)
    {
        double suma = 0;
        foreach (float s in muestras) suma += s * s;
        float rms = Mathf.Sqrt((float)(suma / Mathf.Max(1, muestras.Length)));
        if (rms < 1e-9f) return;

        float escala = rmsObjetivo / rms;
        for (int i = 0; i < muestras.Length; i++) muestras[i] *= escala;
    }

    // Lectura con interpolación lineal; la tabla es un bucle, así que la posición da la vuelta
    public static float Leer(float[] tabla, double posicion)
    {
        int n = tabla.Length;
        int i = (int)posicion;
        float fraccion = (float)(posicion - i);
        i %= n;
        int j = i + 1 < n ? i + 1 : 0;
        return tabla[i] + (tabla[j] - tabla[i]) * fraccion;
    }
}

// Valor aleatorio suave entre 0 y 1: viaja de un objetivo al azar al siguiente con curva coseno.
// Sirve para derivas de tono, ráfagas, amplitudes... sin saltos audibles.
public class ModuladorAleatorio
{
    private readonly System.Random azar;
    private readonly float periodoMin, periodoMax; // en muestras
    private float desde, hacia;
    private int largo, posicion;

    public float Valor { get; private set; }

    public ModuladorAleatorio(System.Random azar, float frecuenciaHz, int frecuenciaMuestreo, float irregularidad = 0.5f)
    {
        this.azar = azar;
        float periodo = frecuenciaMuestreo / Mathf.Max(frecuenciaHz, 0.001f);
        periodoMin = periodo * (1f - irregularidad);
        periodoMax = periodo * (1f + irregularidad);
        desde = hacia = Valor = (float)azar.NextDouble();
        NuevoTramo();
    }

    public float Siguiente(int pasos = 1)
    {
        posicion += pasos;
        if (posicion >= largo) NuevoTramo();

        float t = (float)posicion / largo;
        Valor = desde + (hacia - desde) * (0.5f - 0.5f * Mathf.Cos(t * Mathf.PI));
        return Valor;
    }

    private void NuevoTramo()
    {
        desde = Valor;
        hacia = (float)azar.NextDouble();
        largo = Mathf.Max(1, (int)(periodoMin + (periodoMax - periodoMin) * (float)azar.NextDouble()));
        posicion = 0;
    }
}
