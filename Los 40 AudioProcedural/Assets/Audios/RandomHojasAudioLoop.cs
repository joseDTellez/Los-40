using UnityEngine;

public class RandomHojasAudioLoop : MonoBehaviour
{
    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Secciones (Inicio - Fin en segundos)")]
    // Sección 1: 0 a 5.44
    private float start1 = 0f;
    private float end1 = 5.44f;

    // Sección 2: 6 a 12.72
    private float start2 = 6f;
    private float end2 = 12.72f;

    // Sección 3: 13 a 24.37
    private float start3 = 13f;
    private float end3 = 24.37f;

    private float currentEndTime;
    private bool isPlayingSection = false;

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // Reproducir la primera sección aleatoria al arrancar
        PlayRandomSection();
    }

    void Update()
    {
        // Si se está reproduciendo una sección y el audio llega al tiempo límite de esa sección
        if (isPlayingSection && audioSource.time >= currentEndTime)
        {
            PlayRandomSection();
        }
    }

    void PlayRandomSection()
    {
        // Probabilidad de 1/3 para cada sección usando Random.Range (0, 1, o 2)
        int choice = Random.Range(0, 3);

        float startTime = 0f;

        switch (choice)
        {
            case 0:
                startTime = start1;
                currentEndTime = end1;
                break;
            case 1:
                startTime = start2;
                currentEndTime = end2;
                break;
            case 2:
                startTime = start3;
                currentEndTime = end3;
                break;
        }

        // Asignar el tiempo de inicio y reproducir
        audioSource.time = startTime;
        audioSource.Play();
        isPlayingSection = true;
    }
}