using System.IO;
using UnityEditor;
using UnityEngine;

// Exporta a WAV los bucles de pasos que PasosBucle genera al iniciar (normal suavizado y madera),
// para escucharlos o analizarlos sin entrar en Play. Se guardan en ComandosClaude/ (fuera de git).
public static class ExportarPasos
{
    [MenuItem("Los 40/Audio/Exportar pasos generados (WAV)")]
    public static void ExportarDesdeMenu()
    {
        string resultado = Exportar();
        Debug.Log(resultado);
        EditorUtility.RevealInFinder(Path.Combine(Carpeta, "pasos_normal.wav"));
    }

    static string Carpeta => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ComandosClaude"));

    public static string Exportar()
    {
        PasosBucle pasos = Object.FindAnyObjectByType<PasosBucle>();
        if (pasos == null) return "No hay ningún PasosBucle en la escena abierta.";
        if (!pasos.GenerarDatos(out float[] normal, out float[] madera, out int canales, out int frecuencia))
            return "No se pudieron leer las muestras del clip de pasos.";

        Directory.CreateDirectory(Carpeta);
        string rutaNormal = Path.Combine(Carpeta, "pasos_normal.wav");
        string rutaMadera = Path.Combine(Carpeta, "pasos_madera.wav");
        EscribirWav(rutaNormal, normal, canales, frecuencia);
        EscribirWav(rutaMadera, madera, canales, frecuencia);
        return $"Pasos exportados ({canales} canales, {frecuencia} Hz): {rutaNormal} | {rutaMadera}";
    }

    // WAV PCM de 16 bits
    public static void EscribirWav(string ruta, float[] datos, int canales, int frecuencia)
    {
        using (var w = new BinaryWriter(File.Create(ruta)))
        {
            int bytesDatos = datos.Length * 2;
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            w.Write(36 + bytesDatos);
            w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            w.Write(16);
            w.Write((short)1);
            w.Write((short)canales);
            w.Write(frecuencia);
            w.Write(frecuencia * canales * 2);
            w.Write((short)(canales * 2));
            w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            w.Write(bytesDatos);
            foreach (float s in datos)
                w.Write((short)Mathf.RoundToInt(Mathf.Clamp(s, -1f, 1f) * 32767f));
        }
    }
}
