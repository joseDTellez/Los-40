using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class GameManagerNew : MonoBehaviour
{
    // Una instancia por escena. No usa DontDestroyOnLoad: este objeto también lleva el menú y la
    // entrada de diálogos, y arrastrarlo a la siguiente escena dejaba referencias destruidas.
    public static GameManagerNew Instance { get; private set; }

    [Header("Progreso")]
    public int interaccionesClave = 0;
    public int interaccionesNecesarias = 3;  // configurable desde el Inspector
    public GestorPistasUI gestorPistasUI;

    [Header("Outro")]
    [FormerlySerializedAs("ChangeScener")]
    public ChangeScener changeScener; // Cambia de escena al completar el progreso

    [Header("Eventos de Mapa")]
    public AparicionNPC aparicionNPC; // Hace aparecer al niño cuando corresponde

    private readonly HashSet<string> interaccionesRegistradas = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Hay más de un GameManagerNew en la escena; se ignora este.", this);
            enabled = false;
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void RegistrarInteraccionClave(string id)
    {
        if (!interaccionesRegistradas.Add(id))
            return; // Ya contada (p. ej. al repetir una conversación)

        interaccionesClave++;
        Debug.Log($"[GameManager] Registrado: '{id}' | Progreso: {interaccionesClave}/{interaccionesNecesarias}");

        // Avisamos al controlador del NPC para que revise si ya debe aparecer
        if (aparicionNPC != null)
            aparicionNPC.EvaluarEstadoNPC();

        // Actualizamos la pista visible
        if (gestorPistasUI != null)
            gestorPistasUI.ActualizarTextos();

        VerificarProgreso();
    }

    private void VerificarProgreso()
    {
        if (interaccionesClave < interaccionesNecesarias) return;

        if (changeScener != null)
            changeScener.TriggerOutro(); // ChangeScener ignora llamadas repetidas
        else
            Debug.LogError("ChangeScener no asignado en el Inspector", this);
    }
}
